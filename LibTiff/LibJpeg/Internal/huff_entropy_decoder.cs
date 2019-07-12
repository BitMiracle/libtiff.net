/*
 * This file contains Huffman entropy decoding routines.
 * Both sequential and progressive modes are supported in this single module.
 *
 * Much of the complexity here has to do with supporting input suspension.
 * If the data source module demands suspension, we want to be able to back
 * up to the start of the current MCU.  To do this, we copy state variables
 * into local working storage, and update them back to the permanent
 * storage only upon successful completion of an MCU.
 */

using System;

namespace BitMiracle.LibJpeg.Classic.Internal
{
    /// <summary>
    /// Expanded entropy decoder object for Huffman decoding.
    /// 
    /// The savable_state subrecord contains fields that change within an MCU,
    /// but must not be updated permanently until we complete the MCU.
    /// </summary>
    class huff_entropy_decoder : jpeg_entropy_decoder
    {
        /* Fetching the next N bits from the input stream is a time-critical operation
        * for the Huffman decoders.  We implement it with a combination of inline
        * macros and out-of-line subroutines.  Note that N (the number of bits
        * demanded at one time) never exceeds 15 for JPEG use.
        *
        * We read source bytes into get_buffer and dole out bits as needed.
        * If get_buffer already contains enough bits, they are fetched in-line
        * by the macros CHECK_BIT_BUFFER and GET_BITS.  When there aren't enough
        * bits, jpeg_fill_bit_buffer is called; it will attempt to fill get_buffer
        * as full as possible (not just to the number of bits needed; this
        * prefetching reduces the overhead cost of calling jpeg_fill_bit_buffer).
        * Note that jpeg_fill_bit_buffer may return false to indicate suspension.
        * On true return, jpeg_fill_bit_buffer guarantees that get_buffer contains
        * at least the requested number of bits --- dummy zeroes are inserted if
        * necessary.
        */
        private const int BIT_BUF_SIZE = 32;    /* size of buffer in bits */

        /*
        * Out-of-line code for bit fetching.
        * Note: current values of get_buffer and bits_left are passed as parameters,
        * but are returned in the corresponding fields of the state struct.
        *
        * On most machines MIN_GET_BITS should be 25 to allow the full 32-bit width
        * of get_buffer to be used.  (On machines with wider words, an even larger
        * buffer could be used.)  However, on some machines 32-bit shifts are
        * quite slow and take time proportional to the number of places shifted.
        * (This is true with most PC compilers, for instance.)  In this case it may
        * be a win to set MIN_GET_BITS to the minimum value of 15.  This reduces the
        * average shift distance at the cost of more calls to jpeg_fill_bit_buffer.
        */
        private const int MIN_GET_BITS = BIT_BUF_SIZE - 7;

        // Figure F.12: extend sign bit.

        /* bmask[n] is mask for n rightmost bits */
        private static readonly int[] bmask =
        {
            0, 0x0001, 0x0003, 0x0007, 0x000F, 0x001F, 0x003F, 0x007F, 0x00FF,
            0x01FF, 0x03FF, 0x07FF, 0x0FFF, 0x1FFF, 0x3FFF, 0x7FFF
        };

        private static readonly int[][] jpeg_zigzag_order =
        {
            new int[] {  0,  1,  5,  6, 14, 15, 27, 28 },
            new int[] {  2,  4,  7, 13, 16, 26, 29, 42 },
            new int[] {  3,  8, 12, 17, 25, 30, 41, 43 },
            new int[] {  9, 11, 18, 24, 31, 40, 44, 53 },
            new int[] { 10, 19, 23, 32, 39, 45, 52, 54 },
            new int[] { 20, 22, 33, 38, 46, 51, 55, 60 },
            new int[] { 21, 34, 37, 47, 50, 56, 59, 61 },
            new int[] { 35, 36, 48, 49, 57, 58, 62, 63 }
        };

        private static readonly int[][] jpeg_zigzag_order7 =
        {
            new int[] {  0,  1,  5,  6, 14, 15, 27 },
            new int[] {  2,  4,  7, 13, 16, 26, 28 },
            new int[] {  3,  8, 12, 17, 25, 29, 38 },
            new int[] {  9, 11, 18, 24, 30, 37, 39 },
            new int[] { 10, 19, 23, 31, 36, 40, 45 },
            new int[] { 20, 22, 32, 35, 41, 44, 46 },
            new int[] { 21, 33, 34, 42, 43, 47, 48 }
        };

        private static readonly int[][] jpeg_zigzag_order6 =
        {
            new int[] {  0,  1,  5,  6, 14, 15 },
            new int[] {  2,  4,  7, 13, 16, 25 },
            new int[] {  3,  8, 12, 17, 24, 26 },
            new int[] {  9, 11, 18, 23, 27, 32 },
            new int[] { 10, 19, 22, 28, 31, 33 },
            new int[] { 20, 21, 29, 30, 34, 35 }
        };

        private static readonly int[][] jpeg_zigzag_order5 =
        {
            new int[] {  0,  1,  5,  6, 14 },
            new int[] {  2,  4,  7, 13, 15 },
            new int[] {  3,  8, 12, 16, 21 },
            new int[] {  9, 11, 17, 20, 22 },
            new int[] { 10, 18, 19, 23, 24 }
        };

        private static readonly int[][] jpeg_zigzag_order4 =
        {
            new int[] { 0,  1,  5,  6 },
            new int[] { 2,  4,  7, 12 },
            new int[] { 3,  8, 11, 13 },
            new int[] { 9, 10, 14, 15 }
        };

        private static readonly int[][] jpeg_zigzag_order3 =
        {
            new int[] { 0, 1, 5 },
            new int[] { 2, 4, 6 },
            new int[] { 3, 7, 8 }
        };

        private static readonly int[][] jpeg_zigzag_order2 =
        {
            new int[] { 0, 1 },
            new int[] { 2, 3 }
        };

        private class savable_state
        {
            public uint EOBRUN;			/* remaining EOBs in EOBRUN */
            public int[] last_dc_val = new int[JpegConstants.MAX_COMPS_IN_SCAN]; /* last DC coef for each component */

            public void Assign(savable_state ss)
            {
                EOBRUN = ss.EOBRUN;
                Buffer.BlockCopy(ss.last_dc_val, 0, last_dc_val, 0, last_dc_val.Length * sizeof(int));
            }
        }

        /* These fields are loaded into local variables at start of each MCU.
        * In case of suspension, we exit WITHOUT updating them.
        */
        private bitread_perm_state m_bitstate;    /* Bit buffer at start of MCU */
        private savable_state m_saved = new savable_state();        /* Other state at start of MCU */

        /* These fields are NOT loaded into local working state. */
        bool m_insufficient_data;	/* set TRUE after emitting warning */
        private int m_restarts_to_go;    /* MCUs left in this restart interval */

        /* Following two fields used only in progressive mode */

        /* Pointers to derived tables (these workspaces have image lifespan) */
        private d_derived_tbl[] derived_tbls = new d_derived_tbl[JpegConstants.NUM_HUFF_TBLS];

        private d_derived_tbl ac_derived_tbl; /* active table during an AC scan */

        /* Following fields used only in sequential mode */

        /* Pointers to derived tables (these workspaces have image lifespan) */
        private d_derived_tbl[] m_dc_derived_tbls = new d_derived_tbl[JpegConstants.NUM_HUFF_TBLS];
        private d_derived_tbl[] m_ac_derived_tbls = new d_derived_tbl[JpegConstants.NUM_HUFF_TBLS];

        /* Precalculated info set up by start_pass for use in decode_mcu: */

        /* Pointers to derived tables to be used for each block within an MCU */
        private d_derived_tbl[] m_dc_cur_tbls = new d_derived_tbl[JpegConstants.D_MAX_BLOCKS_IN_MCU];
        private d_derived_tbl[] m_ac_cur_tbls = new d_derived_tbl[JpegConstants.D_MAX_BLOCKS_IN_MCU];

        /* Whether we care about the DC and AC coefficient values for each block */
        private int[] coef_limit = new int[JpegConstants.D_MAX_BLOCKS_IN_MCU];

        private jpeg_decompress_struct m_cinfo;

        public huff_entropy_decoder(jpeg_decompress_struct cinfo)
        {
            m_cinfo = cinfo;

            finish_pass = finish_pass_huff;

            if (m_cinfo.m_progressive_mode)
            {
                /* Create progression status table */
                cinfo.m_coef_bits = new int[cinfo.m_num_components][];
                for (int i = 0; i < cinfo.m_num_components; i++)
                    cinfo.m_coef_bits[i] = new int[JpegConstants.DCTSIZE2];

                for (int ci = 0; ci < cinfo.m_num_components; ci++)
                {
                    for (int i = 0; i < JpegConstants.DCTSIZE2; i++)
                        cinfo.m_coef_bits[ci][i] = -1;
                }

                /* Mark derived tables unallocated */
                for (int i = 0; i < JpegConstants.NUM_HUFF_TBLS; i++)
                {
                    derived_tbls[i] = null;
                }
            }
            else
            {
                /* Mark tables unallocated */
                for (int i = 0; i < JpegConstants.NUM_HUFF_TBLS; i++)
                {
                    m_dc_derived_tbls[i] = null;
                    m_ac_derived_tbls[i] = null;
                }
            }
        }

        /// <summary>
        /// Initialize for a Huffman-compressed scan.
        /// </summary>
        public override void start_pass()
        {
            if (m_cinfo.m_progressive_mode)
            {
                bool bad = false;
                /* Validate progressive scan parameters */
                if (m_cinfo.m_Ss == 0)
                {
                    if (m_cinfo.m_Se != 0)
                        bad = true;
                }
                else
                {
                    /* need not check Ss/Se < 0 since they came from unsigned bytes */
                    if (m_cinfo.m_Se < m_cinfo.m_Ss || m_cinfo.m_Se > m_cinfo.lim_Se)
                        bad = true;

                    /* AC scans may have only one component */
                    if (m_cinfo.m_comps_in_scan != 1)
                        bad = true;
                }

                if (m_cinfo.m_Ah != 0)
                {
                    /* Successive approximation refinement scan: must have Al = Ah-1. */
                    if (m_cinfo.m_Ah - 1 != m_cinfo.m_Al)
                        bad = true;
                }

                if (m_cinfo.m_Al > 13)
                {
                    /* need not check for < 0 */
                    /* Arguably the maximum Al value should be less than 13 for 8-bit precision,
                     * but the spec doesn't say so, and we try to be liberal about what we
                     * accept.  Note: large Al values could result in out-of-range DC
                     * coefficients during early scans, leading to bizarre displays due to
                     * overflows in the IDCT math.  But we won't crash.
                     */
                    bad = true;
                }

                if (bad)
                {
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROGRESSION,
                        m_cinfo.m_Ss, m_cinfo.m_Se, m_cinfo.m_Ah, m_cinfo.m_Al);
                }

                /* Update progression status, and verify that scan order is legal.
                 * Note that inter-scan inconsistencies are treated as warnings
                 * not fatal errors ... not clear if this is right way to behave.
                 */
                for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                {
                    int cindex = m_cinfo.Comp_info[m_cinfo.m_cur_comp_info[ci]].Component_index;
                    if (m_cinfo.m_Ss != 0 && m_cinfo.m_coef_bits[cindex][0] < 0)
                    {
                        /* AC without prior DC scan */
                        m_cinfo.WARNMS(J_MESSAGE_CODE.JWRN_BOGUS_PROGRESSION, cindex, 0);
                    }

                    for (int coefi = m_cinfo.m_Ss; coefi <= m_cinfo.m_Se; coefi++)
                    {
                        int expected = m_cinfo.m_coef_bits[cindex][coefi];
                        if (expected < 0)
                            expected = 0;

                        if (m_cinfo.m_Ah != expected)
                            m_cinfo.WARNMS(J_MESSAGE_CODE.JWRN_BOGUS_PROGRESSION, cindex, coefi);

                        m_cinfo.m_coef_bits[cindex][coefi] = m_cinfo.m_Al;
                    }
                }

                /* Select MCU decoding routine */
                if (m_cinfo.m_Ah == 0)
                {
                    if (m_cinfo.m_Ss == 0)
                        decode_mcu = decode_mcu_DC_first;
                    else
                        decode_mcu = decode_mcu_AC_first;
                }
                else
                {
                    if (m_cinfo.m_Ss == 0)
                        decode_mcu = decode_mcu_DC_refine;
                    else
                        decode_mcu = decode_mcu_AC_refine;
                }

                for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                {
                    jpeg_component_info compptr = m_cinfo.Comp_info[m_cinfo.m_cur_comp_info[ci]];

                    /* Make sure requested tables are present, and compute derived tables.
                     * We may build same derived table more than once, but it's not expensive.
                     */
                    if (m_cinfo.m_Ss == 0)
                    {
                        if (m_cinfo.m_Ah == 0)
                        {
                            /* DC refinement needs no table */
                            int tbl = compptr.Dc_tbl_no;
                            jpeg_make_d_derived_tbl(true, tbl, ref derived_tbls[tbl]);
                        }
                    }
                    else
                    {
                        int tbl = compptr.Ac_tbl_no;
                        jpeg_make_d_derived_tbl(false, tbl, ref derived_tbls[tbl]);

                        /* remember the single active table */
                        ac_derived_tbl = derived_tbls[tbl];
                    }

                    /* Initialize DC predictions to 0 */
                    m_saved.last_dc_val[ci] = 0;
                }

                /* Initialize private state variables */
                m_saved.EOBRUN = 0;
            }
            else
            {
                /* Check that the scan parameters Ss, Se, Ah/Al are OK for sequential JPEG.
                 * This ought to be an error condition, but we make it a warning because
                 * there are some baseline files out there with all zeroes in these bytes.
                 */
                if (m_cinfo.m_Ss != 0 || m_cinfo.m_Ah != 0 || m_cinfo.m_Al != 0 ||
                    ((m_cinfo.is_baseline || m_cinfo.m_Se < JpegConstants.DCTSIZE2) &&
                    m_cinfo.m_Se != m_cinfo.lim_Se))
                {
                    m_cinfo.WARNMS(J_MESSAGE_CODE.JWRN_NOT_SEQUENTIAL);
                }

                /* Select MCU decoding routine */
                /* We retain the hard-coded case for full-size blocks.
                 * This is not necessary, but it appears that this version is slightly
                 * more performant in the given implementation.
                 * With an improved implementation we would prefer a single optimized
                 * function.
                 */
                if (m_cinfo.lim_Se != JpegConstants.DCTSIZE2 - 1)
                    decode_mcu = decode_mcu_sub;
                else
                    decode_mcu = decode_mcu_full;

                for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                {
                    jpeg_component_info componentInfo = m_cinfo.Comp_info[m_cinfo.m_cur_comp_info[ci]];

                    /* Compute derived values for Huffman tables */
                    /* We may do this more than once for a table, but it's not expensive */

                    int tbl = componentInfo.Dc_tbl_no;
                    jpeg_make_d_derived_tbl(true, tbl, ref m_dc_derived_tbls[tbl]);

                    if (m_cinfo.lim_Se != 0)
                    {
                        /* AC needs no table when not present */
                        tbl = componentInfo.Ac_tbl_no;
                        jpeg_make_d_derived_tbl(false, tbl, ref m_ac_derived_tbls[tbl]);
                    }

                    /* Initialize DC predictions to 0 */
                    m_saved.last_dc_val[ci] = 0;
                }

                /* Precalculate decoding info for each block in an MCU of this scan */
                for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
                {
                    int ci = m_cinfo.m_MCU_membership[blkn];
                    jpeg_component_info componentInfo = m_cinfo.Comp_info[m_cinfo.m_cur_comp_info[ci]];

                    /* Precalculate which table to use for each block */
                    m_dc_cur_tbls[blkn] = m_dc_derived_tbls[componentInfo.Dc_tbl_no];
                    m_ac_cur_tbls[blkn] = m_ac_derived_tbls[componentInfo.Ac_tbl_no];

                    /* Decide whether we really care about the coefficient values */
                    if (componentInfo.component_needed)
                    {
                        ci = componentInfo.DCT_v_scaled_size;
                        int i = componentInfo.DCT_h_scaled_size;
                        switch (m_cinfo.lim_Se)
                        {
                            case (1 * 1 - 1):
                                coef_limit[blkn] = 1;
                                break;

                            case (2 * 2 - 1):
                                if (ci <= 0 || ci > 2)
                                    ci = 2;

                                if (i <= 0 || i > 2)
                                    i = 2;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order2[ci - 1][i - 1];
                                break;

                            case (3 * 3 - 1):
                                if (ci <= 0 || ci > 3)
                                    ci = 3;

                                if (i <= 0 || i > 3)
                                    i = 3;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order3[ci - 1][i - 1];
                                break;

                            case (4 * 4 - 1):
                                if (ci <= 0 || ci > 4)
                                    ci = 4;

                                if (i <= 0 || i > 4)
                                    i = 4;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order4[ci - 1][i - 1];
                                break;

                            case (5 * 5 - 1):
                                if (ci <= 0 || ci > 5)
                                    ci = 5;

                                if (i <= 0 || i > 5)
                                    i = 5;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order5[ci - 1][i - 1];
                                break;

                            case (6 * 6 - 1):
                                if (ci <= 0 || ci > 6)
                                    ci = 6;

                                if (i <= 0 || i > 6)
                                    i = 6;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order6[ci - 1][i - 1];
                                break;

                            case (7 * 7 - 1):
                                if (ci <= 0 || ci > 7)
                                    ci = 7;

                                if (i <= 0 || i > 7)
                                    i = 7;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order7[ci - 1][i - 1];
                                break;

                            default:
                                if (ci <= 0 || ci > 8)
                                    ci = 8;

                                if (i <= 0 || i > 8)
                                    i = 8;

                                coef_limit[blkn] = 1 + jpeg_zigzag_order[ci - 1][i - 1];
                                break;
                        }
                    }
                    else
                    {
                        coef_limit[blkn] = 0;
                    }
                }

                /* Initialize bitread state variables */
                m_bitstate.bits_left = 0;
                m_bitstate.get_buffer = 0;
                m_insufficient_data = false;

                /* Initialize restart counter */
                m_restarts_to_go = m_cinfo.m_restart_interval;
            }
        }

        /// <summary>
        /// Decode one MCU's worth of Huffman-compressed coefficients, full-size blocks.
        /// </summary>
        private bool decode_mcu_full(JBLOCK[] MCU_data)
        {
            /* Process restart marker if needed; may have to suspend */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!process_restart())
                        return false;
                }
            }

            /* If we've run out of data, just leave the MCU set to zeroes.
             * This way, we return uniform gray for the remainder of the segment.
             */
            if (!m_insufficient_data)
            {
                /* Load up working state */
                int get_buffer;
                int bits_left;
                bitread_working_state br_state = new bitread_working_state();
                BITREAD_LOAD_STATE(m_bitstate, out get_buffer, out bits_left, ref br_state);
                savable_state state = new savable_state();
                state.Assign(m_saved);

                /* Outer loop handles each block in the MCU */

                for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
                {
                    /* Decode a single block's worth of coefficients */

                    /* Section F.2.2.1: decode the DC coefficient difference */
                    d_derived_tbl htbl = m_dc_cur_tbls[blkn];
                    int s;
                    HUFF_DECODE(out s, ref br_state, htbl, ref get_buffer, ref bits_left);

                    htbl = m_ac_cur_tbls[blkn];
                    int k = 1;
                    int coef_limit = this.coef_limit[blkn];
                    bool endThisBlock = false;

                    if (coef_limit != 0)
                    {
                        /* Convert DC difference to actual value, update last_dc_val */
                        if (s != 0)
                        {
                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);

                            int r = GET_BITS(s, get_buffer, ref bits_left);
                            s = HUFF_EXTEND(r, s);
                        }

                        int ci = m_cinfo.m_MCU_membership[blkn];
                        s += state.last_dc_val[ci];
                        state.last_dc_val[ci] = s;

                        /* Output the DC coefficient */
                        MCU_data[blkn][0] = (short)s;

                        /* Section F.2.2.2: decode the AC coefficients */
                        /* Since zeroes are skipped, output area must be cleared beforehand */
                        for (; k < coef_limit; k++)
                        {
                            HUFF_DECODE(out s, ref br_state, htbl, ref get_buffer, ref bits_left);

                            int r = s >> 4;
                            s &= 15;

                            if (s != 0)
                            {
                                k += r;
                                CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);
                                r = GET_BITS(s, get_buffer, ref bits_left);
                                s = HUFF_EXTEND(r, s);

                                /* Output coefficient in natural (dezigzagged) order.
                                 * Note: the extra entries in jpeg_natural_order[] will save us
                                 * if k >= DCTSIZE2, which could happen if the data is corrupted.
                                 */
                                MCU_data[blkn][JpegUtils.jpeg_natural_order[k]] = (short)s;
                            }
                            else
                            {
                                if (r != 15)
                                {
                                    endThisBlock = true;
                                    break;
                                }

                                k += 15;
                            }
                        }
                    }
                    else
                    {
                        if (s != 0)
                        {
                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);
                            DROP_BITS(s, ref bits_left);
                        }
                    }

                    if (endThisBlock)
                        continue;

                    /* Section F.2.2.2: decode the AC coefficients */
                    /* In this path we just discard the values */
                    for (; k < JpegConstants.DCTSIZE2; k++)
                    {
                        HUFF_DECODE(out s, ref br_state, htbl, ref get_buffer, ref bits_left);

                        int r = s >> 4;
                        s &= 15;

                        if (s != 0)
                        {
                            k += r;
                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);
                            DROP_BITS(s, ref bits_left);
                        }
                        else
                        {
                            if (r != 15)
                                break;

                            k += 15;
                        }
                    }
                }

                /* Completed MCU, so update state */
                BITREAD_SAVE_STATE(ref m_bitstate, get_buffer, bits_left);
                m_saved.Assign(state);
            }

            /* Account for restart interval (no-op if not using restarts) */
            m_restarts_to_go--;

            return true;
        }

        /// <summary>
        /// Decode one MCU's worth of Huffman-compressed coefficients, partial blocks.
        /// </summary>
        private bool decode_mcu_sub(JBLOCK[] MCU_data)
        {
            /* Process restart marker if needed; may have to suspend */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!process_restart())
                        return false;
                }
            }

            /* If we've run out of data, just leave the MCU set to zeroes.
             * This way, we return uniform gray for the remainder of the segment.
             */
            if (!m_insufficient_data)
            {
                int[] natural_order = m_cinfo.natural_order;
                int Se = m_cinfo.m_Se;

                /* Load up working state */
                int get_buffer;
                int bits_left;
                bitread_working_state br_state = new bitread_working_state();
                BITREAD_LOAD_STATE(m_bitstate, out get_buffer, out bits_left, ref br_state);
                savable_state state = new savable_state();
                state.Assign(m_saved);

                /* Outer loop handles each block in the MCU */

                for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
                {
                    /* Decode a single block's worth of coefficients */

                    /* Section F.2.2.1: decode the DC coefficient difference */
                    d_derived_tbl htbl = m_dc_cur_tbls[blkn];
                    int s;
                    HUFF_DECODE(out s, ref br_state, htbl, ref get_buffer, ref bits_left);

                    htbl = m_ac_cur_tbls[blkn];
                    int k = 1;
                    int coef_limit = this.coef_limit[blkn];

                    if (coef_limit != 0)
                    {
                        /* Convert DC difference to actual value, update last_dc_val */
                        if (s != 0)
                        {
                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);

                            int r = GET_BITS(s, get_buffer, ref bits_left);
                            s = HUFF_EXTEND(r, s);
                        }

                        int ci = m_cinfo.m_MCU_membership[blkn];
                        s += state.last_dc_val[ci];
                        state.last_dc_val[ci] = s;

                        /* Output the DC coefficient */
                        MCU_data[blkn][0] = (short)s;

                        /* Section F.2.2.2: decode the AC coefficients */
                        /* Since zeroes are skipped, output area must be cleared beforehand */
                        for (; k < coef_limit; k++)
                        {
                            HUFF_DECODE(out s, ref br_state, htbl, ref get_buffer, ref bits_left);

                            int r = s >> 4;
                            s &= 15;

                            if (s != 0)
                            {
                                k += r;
                                CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);
                                r = GET_BITS(s, get_buffer, ref bits_left);
                                s = HUFF_EXTEND(r, s);

                                /* Output coefficient in natural (dezigzagged) order.
                                 * Note: the extra entries in natural_order[] will save us
                                 * if k >= Se, which could happen if the data is corrupted.
                                 */
                                MCU_data[blkn][natural_order[k]] = (short)s;
                            }
                            else
                            {
                                if (r != 15)
                                    continue;

                                k += 15;
                            }
                        }
                    }
                    else
                    {
                        if (s != 0)
                        {
                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);
                            DROP_BITS(s, ref bits_left);
                        }
                    }

                    /* Section F.2.2.2: decode the AC coefficients */
                    /* In this path we just discard the values */
                    for (; k <= Se; k++)
                    {
                        HUFF_DECODE(out s, ref br_state, htbl, ref get_buffer, ref bits_left);

                        int r = s >> 4;
                        s &= 15;

                        if (s != 0)
                        {
                            k += r;
                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);

                            DROP_BITS(s, ref bits_left);
                        }
                        else
                        {
                            if (r != 15)
                                break;

                            k += 15;
                        }
                    }
                }

                /* Completed MCU, so update state */
                BITREAD_SAVE_STATE(ref m_bitstate, get_buffer, bits_left);
                m_saved.Assign(state);
            }

            /* Account for restart interval (no-op if not using restarts) */
            m_restarts_to_go--;

            return true;
        }

        /*
         * Huffman MCU decoding.
         * Each of these routines decodes and returns one MCU's worth of
         * Huffman-compressed coefficients. 
         * The coefficients are reordered from zigzag order into natural array order,
         * but are not dequantized.
         *
         * The i'th block of the MCU is stored into the block pointed to by
         * MCU_data[i].  WE ASSUME THIS AREA IS INITIALLY ZEROED BY THE CALLER.
         * (Wholesale zeroing is usually a little faster than retail...)
         *
         * We return false if data source requested suspension.  In that case no
         * changes have been made to permanent state.  (Exception: some output
         * coefficients may already have been assigned.  This is harmless for
         * spectral selection, since we'll just re-assign them on the next call.
         * Successive approximation AC refinement has to be more careful, however.)
         */

        /// <summary>
        /// MCU decoding for DC initial scan (either spectral selection,
        /// or first pass of successive approximation).
        /// </summary>
        private bool decode_mcu_DC_first(JBLOCK[] MCU_data)
        {
            /* Process restart marker if needed; may have to suspend */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!process_restart())
                        return false;
                }
            }

            /* If we've run out of data, just leave the MCU set to zeroes.
             * This way, we return uniform gray for the remainder of the segment.
             */
            if (!m_insufficient_data)
            {
                /* Load up working state */
                int get_buffer;
                int bits_left;
                bitread_working_state br_state = new bitread_working_state();
                BITREAD_LOAD_STATE(m_bitstate, out get_buffer, out bits_left, ref br_state);
                savable_state state = new savable_state();
                state.Assign(m_saved);

                /* Outer loop handles each block in the MCU */
                for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
                {
                    int ci = m_cinfo.m_MCU_membership[blkn];

                    /* Decode a single block's worth of coefficients */

                    /* Section F.2.2.1: decode the DC coefficient difference */
                    int s;
                    HUFF_DECODE(out s, ref br_state, derived_tbls[m_cinfo.Comp_info[m_cinfo.m_cur_comp_info[ci]].Dc_tbl_no], ref get_buffer, ref bits_left);

                    if (s != 0)
                    {
                        CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);

                        int r = GET_BITS(s, get_buffer, ref bits_left);
                        s = HUFF_EXTEND(r, s);
                    }

                    /* Convert DC difference to actual value, update last_dc_val */
                    s += state.last_dc_val[ci];
                    state.last_dc_val[ci] = s;

                    /* Scale and output the coefficient (assumes jpeg_natural_order[0]=0) */
                    MCU_data[blkn][0] = (short)(s << m_cinfo.m_Al);
                }

                /* Completed MCU, so update state */
                BITREAD_SAVE_STATE(ref m_bitstate, get_buffer, bits_left);
                m_saved.Assign(state);
            }

            /* Account for restart interval (no-op if not using restarts) */
            m_restarts_to_go--;

            return true;
        }

        /// <summary>
        /// MCU decoding for AC initial scan (either spectral selection,
        /// or first pass of successive approximation).
        /// </summary>
        private bool decode_mcu_AC_first(JBLOCK[] MCU_data)
        {
            /* Process restart marker if needed; may have to suspend */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!process_restart())
                        return false;
                }
            }

            /* If we've run out of data, just leave the MCU set to zeroes.
             * This way, we return uniform gray for the remainder of the segment.
             */
            if (!m_insufficient_data)
            {
                /* Load up working state.
                 * We can avoid loading/saving bitread state if in an EOB run.
                 */
                uint EOBRUN = m_saved.EOBRUN; /* only part of saved state we need */
                int[] natural_order = m_cinfo.natural_order;

                /* There is always only one block per MCU */

                if (EOBRUN != 0)
                {
                    /* if it's a band of zeroes... */
                    /* ...process it now (we do nothing) */
                    EOBRUN--;
                }
                else
                {
                    int get_buffer;
                    int bits_left;
                    bitread_working_state br_state = new bitread_working_state();
                    BITREAD_LOAD_STATE(m_bitstate, out get_buffer, out bits_left, ref br_state);

                    for (int k = m_cinfo.m_Ss; k <= m_cinfo.m_Se; k++)
                    {
                        int s;
                        HUFF_DECODE(out s, ref br_state, ac_derived_tbl, ref get_buffer, ref bits_left);

                        int r = s >> 4;
                        s &= 15;
                        if (s != 0)
                        {
                            k += r;

                            CHECK_BIT_BUFFER(ref br_state, s, ref get_buffer, ref bits_left);

                            r = GET_BITS(s, get_buffer, ref bits_left);
                            s = HUFF_EXTEND(r, s);

                            /* Scale and output coefficient in natural (dezigzagged) order */
                            MCU_data[0][natural_order[k]] = (short)(s << m_cinfo.m_Al);
                        }
                        else
                        {
                            if (r != 15)
                            {
                                /* EOBr, run length is 2^r + appended bits */
                                if (r != 0)
                                {
                                    /* EOBr, r > 0 */
                                    EOBRUN = (uint)(1 << r);

                                    CHECK_BIT_BUFFER(ref br_state, r, ref get_buffer, ref bits_left);

                                    r = GET_BITS(r, get_buffer, ref bits_left);
                                    EOBRUN += (uint)r;
                                    EOBRUN--;		/* this band is processed at this moment */
                                }
                                break;      /* force end-of-band */
                            }

                            k += 15;		/* ZRL: skip 15 zeroes in band */
                        }
                    }

                    BITREAD_SAVE_STATE(ref m_bitstate, get_buffer, bits_left);
                }

                /* Completed MCU, so update state */
                m_saved.EOBRUN = EOBRUN; /* only part of saved state we need */
            }

            /* Account for restart interval (no-op if not using restarts) */
            m_restarts_to_go--;

            return true;
        }

        /// <summary>
        /// MCU decoding for DC successive approximation refinement scan.
        /// Note: we assume such scans can be multi-component,
        /// although the spec is not very clear on the point.
        /// </summary>
        private bool decode_mcu_DC_refine(JBLOCK[] MCU_data)
        {
            /* Process restart marker if needed; may have to suspend */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!process_restart())
                        return false;
                }
            }

            /* Not worth the cycles to check insufficient_data here,
             * since we will not change the data anyway if we read zeroes.
             */

            /* Load up working state */
            int get_buffer;
            int bits_left;
            bitread_working_state br_state = new bitread_working_state();
            BITREAD_LOAD_STATE(m_bitstate, out get_buffer, out bits_left, ref br_state);

            int p1 = 1 << m_cinfo.m_Al;

            /* Outer loop handles each block in the MCU */

            for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
            {
                /* Encoded data is simply the next bit of the two's-complement DC value */
                CHECK_BIT_BUFFER(ref br_state, 1, ref get_buffer, ref bits_left);

                if (GET_BITS(1, get_buffer, ref bits_left) != 0)
                {
                    /* 1 in the bit position being coded */
                    MCU_data[blkn][0] = (short)((ushort)MCU_data[blkn][0] | p1);
                }

                /* Note: since we use |=, repeating the assignment later is safe */
            }

            /* Completed MCU, so update state */
            BITREAD_SAVE_STATE(ref m_bitstate, get_buffer, bits_left);

            /* Account for restart interval (no-op if not using restarts) */
            m_restarts_to_go--;

            return true;
        }

        // There is always only one block per MCU
        private bool decode_mcu_AC_refine(JBLOCK[] MCU_data)
        {
            /* Process restart marker if needed; may have to suspend */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!process_restart())
                        return false;
                }
            }

            /* If we've run out of data, don't modify the MCU.
             */
            if (!m_insufficient_data)
            {
                int p1 = 1 << m_cinfo.m_Al;    /* 1 in the bit position being coded */
                int m1 = -1 << m_cinfo.m_Al; /* -1 in the bit position being coded */
                int[] natural_order = m_cinfo.natural_order;

                /* Load up working state */
                int get_buffer;
                int bits_left;
                bitread_working_state br_state = new bitread_working_state();
                BITREAD_LOAD_STATE(m_bitstate, out get_buffer, out bits_left, ref br_state);
                uint EOBRUN = m_saved.EOBRUN; /* only part of saved state we need */

                /* If we are forced to suspend, we must undo the assignments to any newly
                 * nonzero coefficients in the block, because otherwise we'd get confused
                 * next time about which coefficients were already nonzero.
                 * But we need not undo addition of bits to already-nonzero coefficients;
                 * instead, we can test the current bit to see if we already did it.
                 */
                int num_newnz = 0;
                int[] newnz_pos = new int[JpegConstants.DCTSIZE2];

                /* initialize coefficient loop counter to start of band */
                int k = m_cinfo.m_Ss;

                if (EOBRUN == 0)
                {
                    do
                    {
                        int s;
                        HUFF_DECODE(out s, ref br_state, ac_derived_tbl, ref get_buffer, ref bits_left);

                        int r = s >> 4;
                        s &= 15;
                        if (s != 0)
                        {
                            if (s != 1)
                            {
                                /* size of new coef should always be 1 */
                                m_cinfo.WARNMS(J_MESSAGE_CODE.JWRN_HUFF_BAD_CODE);
                            }

                            CHECK_BIT_BUFFER(ref br_state, 1, ref get_buffer, ref bits_left);

                            if (GET_BITS(1, get_buffer, ref bits_left) != 0)
                            {
                                /* newly nonzero coef is positive */
                                s = p1;
                            }
                            else
                            {
                                /* newly nonzero coef is negative */
                                s = m1;
                            }
                        }
                        else
                        {
                            if (r != 15)
                            {
                                EOBRUN = (uint)(1 << r);    /* EOBr, run length is 2^r + appended bits */
                                if (r != 0)
                                {
                                    CHECK_BIT_BUFFER(ref br_state, r, ref get_buffer, ref bits_left);

                                    r = GET_BITS(r, get_buffer, ref bits_left);
                                    EOBRUN += (uint)r;
                                }
                                break;      /* rest of block is handled by EOB logic */
                            }
                            /* note s = 0 for processing ZRL */
                        }
                        /* Advance over already-nonzero coefs and r still-zero coefs,
                         * appending correction bits to the nonzeroes.  A correction bit is 1
                         * if the absolute value of the coefficient must be increased.
                         */
                        do
                        {
                            int blockIndex = natural_order[k];
                            short thiscoef = MCU_data[0][blockIndex];
                            if (thiscoef != 0)
                            {
                                CHECK_BIT_BUFFER(ref br_state, 1, ref get_buffer, ref bits_left);

                                if (GET_BITS(1, get_buffer, ref bits_left) != 0)
                                {
                                    if ((thiscoef & p1) == 0)
                                    {
                                        /* do nothing if already set it */
                                        if (thiscoef >= 0)
                                            MCU_data[0][blockIndex] += (short)p1;
                                        else
                                            MCU_data[0][blockIndex] += (short)m1;
                                    }
                                }
                            }
                            else
                            {
                                if (--r < 0)
                                    break;      /* reached target zero coefficient */
                            }

                            k++;
                        }
                        while (k <= m_cinfo.m_Se);

                        if (s != 0)
                        {
                            int pos = natural_order[k];

                            /* Output newly nonzero coefficient */
                            MCU_data[0][pos] = (short)s;

                            /* Remember its position in case we have to suspend */
                            newnz_pos[num_newnz++] = pos;
                        }

                        k++;
                    } while (k <= m_cinfo.m_Se);
                }

                if (EOBRUN != 0)
                {
                    /* Scan any remaining coefficient positions after the end-of-band
                     * (the last newly nonzero coefficient, if any).  Append a correction
                     * bit to each already-nonzero coefficient.  A correction bit is 1
                     * if the absolute value of the coefficient must be increased.
                     */
                    do
                    {
                        int blockIndex = natural_order[k];
                        short thiscoef = MCU_data[0][blockIndex];
                        if (thiscoef != 0)
                        {
                            CHECK_BIT_BUFFER(ref br_state, 1, ref get_buffer, ref bits_left);

                            if (GET_BITS(1, get_buffer, ref bits_left) != 0)
                            {
                                if ((thiscoef & p1) == 0)
                                {
                                    /* do nothing if already changed it */
                                    if (thiscoef >= 0)
                                        MCU_data[0][blockIndex] += (short)p1;
                                    else
                                        MCU_data[0][blockIndex] += (short)m1;
                                }
                            }
                        }

                        k++;
                    } while (k <= m_cinfo.m_Se);

                    /* Count one block completed in EOB run */
                    EOBRUN--;
                }

                /* Completed MCU, so update state */
                BITREAD_SAVE_STATE(ref m_bitstate, get_buffer, bits_left);
                m_saved.EOBRUN = EOBRUN; /* only part of saved state we need */
            }

            /* Account for restart interval (no-op if not using restarts) */
            m_restarts_to_go--;

            return true;
        }

        /// <summary>
        /// MCU decoding for AC successive approximation refinement scan.
        /// </summary>
        private static void undo_decode_mcu_AC_refine(JBLOCK[] block, int[] newnz_pos, int num_newnz)
        {
            /* Re-zero any output coefficients that we made newly nonzero */
            while (num_newnz > 0)
                block[0][newnz_pos[--num_newnz]] = 0;
        }

        /*
         * Finish up at the end of a Huffman-compressed scan.
         */
        public void finish_pass_huff()
        {
            /* Throw away any unused bits remaining in bit buffer; */
            /* include any full bytes in next_marker's count of discarded bytes */
            m_cinfo.m_marker.SkipBytes(m_bitstate.bits_left / 8);
            m_bitstate.bits_left = 0;
        }

        /// <summary>
        /// Check for a restart marker and resynchronize decoder.
        /// Returns false if must suspend.
        /// </summary>
        private bool process_restart()
        {
            finish_pass_huff();

            /* Advance past the RSTn marker */
            if (!m_cinfo.m_marker.read_restart_marker())
                return false;

            /* Re-initialize DC predictions to 0 */
            for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                m_saved.last_dc_val[ci] = 0;

            /* Re-init EOB run count, too */
            m_saved.EOBRUN = 0;

            /* Reset restart counter */
            m_restarts_to_go = m_cinfo.m_restart_interval;

            /* Reset out-of-data flag, unless read_restart_marker left us smack up
             * against a marker.  In that case we will end up treating the next data
             * segment as empty, and we can avoid producing bogus output pixels by
             * leaving the flag set.
             */
            if (m_cinfo.m_unread_marker == 0)
                m_insufficient_data = false;

            return true;
        }

        private static int HUFF_EXTEND(int x, int s)
        {
            return (x <= bmask[s - 1] ? x - bmask[s] : x);
        }

        private void BITREAD_LOAD_STATE(bitread_perm_state bitstate, out int get_buffer, out int bits_left, ref bitread_working_state br_state)
        {
            br_state.cinfo = m_cinfo;
            get_buffer = bitstate.get_buffer;
            bits_left = bitstate.bits_left;
        }

        private static void BITREAD_SAVE_STATE(ref bitread_perm_state bitstate, int get_buffer, int bits_left)
        {
            bitstate.get_buffer = get_buffer;
            bitstate.bits_left = bits_left;
        }

        /*
        * These methods provide the in-line portion of bit fetching.
        * Use CHECK_BIT_BUFFER to ensure there are N bits in get_buffer
        * before using GET_BITS, PEEK_BITS, or DROP_BITS.
        * The variables get_buffer and bits_left are assumed to be locals,
        * but the state struct might not be (jpeg_huff_decode needs this).
        *  CHECK_BIT_BUFFER(state,n,action);
        *      Ensure there are N bits in get_buffer; if suspend, take action.
        *      val = GET_BITS(n);
        *      Fetch next N bits.
        *      val = PEEK_BITS(n);
        *      Fetch next N bits without removing them from the buffer.
        *  DROP_BITS(n);
        *      Discard next N bits.
        * The value N should be a simple variable, not an expression, because it
        * is evaluated multiple times.
        */
        private static void CHECK_BIT_BUFFER(ref bitread_working_state state, int nbits, ref int get_buffer, ref int bits_left)
        {
            if (bits_left < nbits)
            {
                jpeg_fill_bit_buffer(ref state, get_buffer, bits_left, nbits);
                get_buffer = state.get_buffer;
                bits_left = state.bits_left;
            }
        }

        private static int GET_BITS(int nbits, int get_buffer, ref int bits_left)
        {
            return (((int)(get_buffer >> (bits_left -= nbits))) & bmask[nbits]);
        }

        private static int PEEK_BITS(int nbits, int get_buffer, int bits_left)
        {
            return (((int)(get_buffer >> (bits_left - nbits))) & bmask[nbits]);
        }

        private static void DROP_BITS(int nbits, ref int bits_left)
        {
            bits_left -= nbits;
        }

        /*
        * Code for extracting next Huffman-coded symbol from input bit stream.
        * Again, this is time-critical and we make the main paths be macros.
        *
        * We use a lookahead table to process codes of up to HUFF_LOOKAHEAD bits
        * without looping.  Usually, more than 95% of the Huffman codes will be 8
        * or fewer bits long.  The few overlength codes are handled with a loop,
        * which need not be inline code.
        *
        * Notes about the HUFF_DECODE macro:
        * 1. Near the end of the data segment, we may fail to get enough bits
        *    for a lookahead.  In that case, we do it the hard way.
        * 2. If the lookahead table contains no entry, the next code must be
        *    more than HUFF_LOOKAHEAD bits long.
        * 3. jpeg_huff_decode returns -1 if forced to suspend.
        */
        private static void HUFF_DECODE(out int result, ref bitread_working_state state, d_derived_tbl htbl, ref int get_buffer, ref int bits_left)
        {
            int nb = 0;
            bool doSlow = false;

            if (bits_left < JpegConstants.HUFF_LOOKAHEAD)
            {
                jpeg_fill_bit_buffer(ref state, get_buffer, bits_left, 0);

                get_buffer = state.get_buffer;
                bits_left = state.bits_left;
                if (bits_left < JpegConstants.HUFF_LOOKAHEAD)
                {
                    nb = 1;
                    doSlow = true;
                }
            }

            if (!doSlow)
            {
                int look = PEEK_BITS(JpegConstants.HUFF_LOOKAHEAD, get_buffer, bits_left);
                if ((nb = htbl.look_nbits[look]) != 0)
                {
                    DROP_BITS(nb, ref bits_left);
                    result = htbl.look_sym[look];
                    return;
                }

                nb = JpegConstants.HUFF_LOOKAHEAD + 1;
            }

            result = jpeg_huff_decode(ref state, get_buffer, bits_left, htbl, nb);

            get_buffer = state.get_buffer;
            bits_left = state.bits_left;
        }

        /* Load up the bit buffer to a depth of at least nbits */
        private static void jpeg_fill_bit_buffer(ref bitread_working_state state, int get_buffer, int bits_left, int nbits)
        {
            /* Attempt to load at least MIN_GET_BITS bits into get_buffer. */
            /* (It is assumed that no request will be for more than that many bits.) */
            /* We fail to do so only if we hit a marker or are forced to suspend. */

            bool noMoreBytes = false;

            if (state.cinfo.m_unread_marker == 0)
            {
                /* cannot advance past a marker */
                while (bits_left < MIN_GET_BITS)
                {
                    int c;
                    state.cinfo.m_src.GetByte(out c);

                    /* If it's 0xFF, check and discard stuffed zero byte */
                    if (c == 0xFF)
                    {
                        /* Loop here to discard any padding FF's on terminating marker,
                        * so that we can save a valid unread_marker value.  NOTE: we will
                        * accept multiple FF's followed by a 0 as meaning a single FF data
                        * byte.  This data pattern is not valid according to the standard.
                        */
                        do
                        {
                            state.cinfo.m_src.GetByte(out c);
                        }
                        while (c == 0xFF);

                        if (c == 0)
                        {
                            /* Found FF/00, which represents an FF data byte */
                            c = 0xFF;
                        }
                        else
                        {
                            /* Oops, it's actually a marker indicating end of compressed data.
                            * Save the marker code for later use.
                            * Fine point: it might appear that we should save the marker into
                            * bitread working state, not straight into permanent state.  But
                            * once we have hit a marker, we cannot need to suspend within the
                            * current MCU, because we will read no more bytes from the data
                            * source.  So it is OK to update permanent state right away.
                            */
                            state.cinfo.m_unread_marker = c;
                            /* See if we need to insert some fake zero bits. */
                            noMoreBytes = true;
                            break;
                        }
                    }

                    /* OK, load c into get_buffer */
                    get_buffer = (get_buffer << 8) | c;
                    bits_left += 8;
                } /* end while */
            }
            else
                noMoreBytes = true;

            if (noMoreBytes)
            {
                /* We get here if we've read the marker that terminates the compressed
                * data segment.  There should be enough bits in the buffer register
                * to satisfy the request; if so, no problem.
                */
                if (nbits > bits_left)
                {
                    /* Uh-oh.  Report corrupted data to user and stuff zeroes into
                    * the data stream, so that we can produce some kind of image.
                    * We use a nonvolatile flag to ensure that only one warning message
                    * appears per data segment.
                    */
                    huff_entropy_decoder entropy = (huff_entropy_decoder)state.cinfo.m_entropy;
                    if (!entropy.m_insufficient_data)
                    {
                        state.cinfo.WARNMS(J_MESSAGE_CODE.JWRN_HIT_MARKER);
                        entropy.m_insufficient_data = true;
                    }

                    /* Fill the buffer with zero bits */
                    get_buffer <<= MIN_GET_BITS - bits_left;
                    bits_left = MIN_GET_BITS;
                }
            }

            /* Unload the local registers */
            state.get_buffer = get_buffer;
            state.bits_left = bits_left;
        }

        /// <summary>
        /// Expand a Huffman table definition into the derived format
        /// This routine also performs some validation checks on the table.
        /// </summary>
        private void jpeg_make_d_derived_tbl(bool isDC, int tblno, ref d_derived_tbl dtbl)
        {
            /* Note that huffsize[] and huffcode[] are filled in code-length order,
            * paralleling the order of the symbols themselves in htbl.huffval[].
            */

            /* Find the input Huffman table */
            if (tblno < 0 || tblno >= JpegConstants.NUM_HUFF_TBLS)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_NO_HUFF_TABLE, tblno);

            JHUFF_TBL htbl = isDC ? m_cinfo.m_dc_huff_tbl_ptrs[tblno] : m_cinfo.m_ac_huff_tbl_ptrs[tblno];
            if (htbl == null)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_NO_HUFF_TABLE, tblno);

            /* Allocate a workspace if we haven't already done so. */
            if (dtbl == null)
                dtbl = new d_derived_tbl();

            dtbl.pub = htbl;       /* fill in back link */

            /* Figure C.1: make table of Huffman code length for each symbol */

            int p = 0;
            char[] huffsize = new char[257];
            for (int l = 1; l <= 16; l++)
            {
                int i = htbl.Bits[l];
                if (p + i > 256)    /* protect against table overrun */
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_HUFF_TABLE);

                while ((i--) != 0)
                    huffsize[p++] = (char)l;
            }
            huffsize[p] = (char)0;
            int numsymbols = p;

            /* Figure C.2: generate the codes themselves */
            /* We also validate that the counts represent a legal Huffman code tree. */

            int code = 0;
            int si = huffsize[0];
            int[] huffcode = new int[257];
            p = 0;
            while (huffsize[p] != 0)
            {
                while (((int)huffsize[p]) == si)
                {
                    huffcode[p++] = code;
                    code++;
                }

                /* code is now 1 more than the last code used for codelength si; but
                * it must still fit in si bits, since no code is allowed to be all ones.
                */
                if (code >= (1 << si))
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_HUFF_TABLE);
                code <<= 1;
                si++;
            }

            /* Figure F.15: generate decoding tables for bit-sequential decoding */

            p = 0;
            for (int l = 1; l <= 16; l++)
            {
                if (htbl.Bits[l] != 0)
                {
                    /* valoffset[l] = huffval[] index of 1st symbol of code length l,
                    * minus the minimum code of length l
                    */
                    dtbl.valoffset[l] = p - huffcode[p];
                    p += htbl.Bits[l];
                    dtbl.maxcode[l] = huffcode[p - 1]; /* maximum code of length l */
                }
                else
                {
                    /* -1 if no codes of this length */
                    dtbl.maxcode[l] = -1;
                }
            }
            dtbl.maxcode[17] = 0xFFFFF; /* ensures jpeg_huff_decode terminates */

            /* Compute lookahead tables to speed up decoding.
            * First we set all the table entries to 0, indicating "too long";
            * then we iterate through the Huffman codes that are short enough and
            * fill in all the entries that correspond to bit sequences starting
            * with that code.
            */

            Array.Clear(dtbl.look_nbits, 0, dtbl.look_nbits.Length);
            p = 0;
            for (int l = 1; l <= JpegConstants.HUFF_LOOKAHEAD; l++)
            {
                for (int i = 1; i <= htbl.Bits[l]; i++, p++)
                {
                    /* l = current code's length, p = its index in huffcode[] & huffval[]. */
                    /* Generate left-justified code followed by all possible bit sequences */
                    int lookbits = huffcode[p] << (JpegConstants.HUFF_LOOKAHEAD - l);
                    for (int ctr = 1 << (JpegConstants.HUFF_LOOKAHEAD - l); ctr > 0; ctr--)
                    {
                        dtbl.look_nbits[lookbits] = l;
                        dtbl.look_sym[lookbits] = htbl.Huffval[p];
                        lookbits++;
                    }
                }
            }

            /* Validate symbols as being reasonable.
            * For AC tables, we make no check, but accept all byte values 0..255.
            * For DC tables, we require the symbols to be in range 0..15.
            * (Tighter bounds could be applied depending on the data depth and mode,
            * but this is sufficient to ensure safe decoding.)
            */
            if (isDC)
            {
                for (int i = 0; i < numsymbols; i++)
                {
                    int sym = htbl.Huffval[i];
                    if (sym > 15)
                        m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_HUFF_TABLE);
                }
            }
        }

        /* Out-of-line case for Huffman code fetching */
        protected static int jpeg_huff_decode(ref bitread_working_state state, int get_buffer, int bits_left, d_derived_tbl htbl, int min_bits)
        {
            /* HUFF_DECODE has determined that the code is at least min_bits */
            /* bits long, so fetch that many bits in one swoop. */
            int l = min_bits;
            CHECK_BIT_BUFFER(ref state, l, ref get_buffer, ref bits_left);

            int code = GET_BITS(l, get_buffer, ref bits_left);

            /* Collect the rest of the Huffman code one bit at a time. */
            /* This is per Figure F.16 in the JPEG spec. */

            while (code > htbl.maxcode[l])
            {
                code <<= 1;
                CHECK_BIT_BUFFER(ref state, 1, ref get_buffer, ref bits_left);

                code |= GET_BITS(1, get_buffer, ref bits_left);
                l++;
            }

            /* Unload the local registers */
            state.get_buffer = get_buffer;
            state.bits_left = bits_left;

            /* With garbage input we may reach the sentinel value l = 17. */

            if (l > 16)
            {
                state.cinfo.WARNMS(J_MESSAGE_CODE.JWRN_HUFF_BAD_CODE);
                /* fake a zero as the safest result */
                return 0;
            }

            return htbl.pub.Huffval[code + htbl.valoffset[l]];
        }
    }
}
