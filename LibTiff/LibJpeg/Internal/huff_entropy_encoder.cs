/*
 * This file contains Huffman entropy encoding routines.
 *
 * Much of the complexity here has to do with supporting output suspension.
 * If the data destination module demands suspension, we want to be able to
 * back up to the start of the current MCU.  To do this, we copy state
 * variables into local working storage, and update them back to the
 * permanent JPEG objects only upon successful completion of an MCU.
 *
 * We do not support output suspension for the progressive JPEG mode, since
 * the library currently does not allow multiple-scan files to be written
 * with output suspension.
 */

using System;

namespace BitMiracle.LibJpeg.Classic.Internal
{
    /// <summary>
    /// Expanded entropy encoder object for Huffman encoding.
    /// </summary>
    class huff_entropy_encoder : jpeg_entropy_encoder
    {
        /* The legal range of a DCT coefficient is
         *  -1024 .. +1023  for 8-bit data;
         * -16384 .. +16383 for 12-bit data.
         * Hence the magnitude should always fit in 10 or 14 bits respectively.
         */
        private const int MAX_COEF_BITS = 10;

        /* MAX_CORR_BITS is the number of bits the AC refinement correction-bit
         * buffer can hold.  Larger sizes may slightly improve compression, but
         * 1000 is already well into the realm of overkill.
         * The minimum safe size is 64 bits.
         */
        private const int MAX_CORR_BITS = 1000;	/* Max # of correction bits I can buffer */

        /* Derived data constructed for each Huffman table */
        private class c_derived_tbl
        {
            public int[] ehufco = new int[256];   /* code for each symbol */
            public char[] ehufsi = new char[256];       /* length of code for each symbol */
            /* If no code has been allocated for a symbol S, ehufsi[S] contains 0 */
        }

        /* The savable_state subrecord contains fields that change within an MCU,
        * but must not be updated permanently until we complete the MCU.
        */
        private class savable_state
        {
            public int put_buffer;       /* current bit-accumulation buffer */
            public int put_bits;           /* # of bits now in it */
            public int[] last_dc_val = new int[JpegConstants.MAX_COMPS_IN_SCAN]; /* last DC coef for each component */

            public void ASSIGN_STATE(savable_state src)
            {
                put_buffer = src.put_buffer;
                put_bits = src.put_bits;

                for (int i = 0; i < last_dc_val.Length; i++)
                    last_dc_val[i] = src.last_dc_val[i];
            }
        }

        private savable_state m_saved = new savable_state();        /* Bit buffer & DC state at start of MCU */

        /* These fields are NOT loaded into local working state. */
        private int m_restarts_to_go;    /* MCUs left in this restart interval */
        private int m_next_restart_num;       /* next restart number to write (0-7) */

        /* Pointers to derived tables (these workspaces have image lifespan) */
        private c_derived_tbl[] m_dc_derived_tbls = new c_derived_tbl[JpegConstants.NUM_HUFF_TBLS];
        private c_derived_tbl[] m_ac_derived_tbls = new c_derived_tbl[JpegConstants.NUM_HUFF_TBLS];

        /* Statistics tables for optimization */
        private long[][] m_dc_count_ptrs = new long[JpegConstants.NUM_HUFF_TBLS][];
        private long[][] m_ac_count_ptrs = new long[JpegConstants.NUM_HUFF_TBLS][];

        /* Following fields used only in progressive mode */

        /* Mode flag: TRUE for optimization, FALSE for actual data output */
        private bool m_gather_statistics;

        private jpeg_compress_struct m_cinfo;

        /* Coding status for AC components */
        private int ac_tbl_no;      /* the table number of the single component */
        private uint EOBRUN;        /* run length of EOBs */
        private uint BE;        /* # of buffered correction bits before MCU */
        private char[] bit_buffer;       /* buffer for correction bits (1 per char) */
                                         /* packing correction bits tightly would save some space but cost time... */

        public huff_entropy_encoder(jpeg_compress_struct cinfo)
        {
            m_cinfo = cinfo;

            /* Mark tables unallocated */
            for (int i = 0; i < JpegConstants.NUM_HUFF_TBLS; i++)
            {
                m_dc_derived_tbls[i] = m_ac_derived_tbls[i] = null;
                m_dc_count_ptrs[i] = m_ac_count_ptrs[i] = null;
            }

            if (m_cinfo.m_progressive_mode)
            {
                /* needed only in AC refinement scan */
                bit_buffer = null;
            }
        }

        /// <summary>
        /// Initialize for a Huffman-compressed scan.
        /// If gather_statistics is true, we do not output anything during the scan,
        /// just count the Huffman symbols used and generate Huffman code tables.
        /// </summary>
        public override void start_pass(bool gather_statistics)
        {
            m_gather_statistics = gather_statistics;

            if (gather_statistics)
                finish_pass = finish_pass_gather;
            else
                finish_pass = finish_pass_huff;

            if (m_cinfo.m_progressive_mode)
            {
                /* We assume the scan parameters are already validated. */

                /* Select execution routine */
                if (m_cinfo.m_Ah == 0)
                {
                    if (m_cinfo.m_Ss == 0)
                        encode_mcu = encode_mcu_DC_first;
                    else
                        encode_mcu = encode_mcu_AC_first;
                }
                else
                {
                    if (m_cinfo.m_Ss == 0)
                        encode_mcu = encode_mcu_DC_refine;
                    else
                    {
                        encode_mcu = encode_mcu_AC_refine;
                        /* AC refinement needs a correction bit buffer */
                        if (bit_buffer == null)
                            bit_buffer = new char[MAX_CORR_BITS];
                    }
                }

                /* Initialize AC stuff */
                ac_tbl_no = m_cinfo.Component_info[m_cinfo.m_cur_comp_info[0]].Ac_tbl_no;
                EOBRUN = 0;
                BE = 0;
            }
            else
            {
                if (gather_statistics)
                    encode_mcu = encode_mcu_gather;
                else
                    encode_mcu = encode_mcu_huff;
            }

            for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
            {
                jpeg_component_info compptr = m_cinfo.Component_info[m_cinfo.m_cur_comp_info[ci]];

                /* DC needs no table for refinement scan */
                if (m_cinfo.m_Ss == 0 && m_cinfo.m_Ah == 0)
                {
                    int tbl = compptr.Dc_tbl_no;
                    if (gather_statistics)
                    {
                        /* Check for invalid table index */
                        /* (make_c_derived_tbl does this in the other path) */
                        if (tbl < 0 || tbl >= JpegConstants.NUM_HUFF_TBLS)
                            m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_NO_HUFF_TABLE, tbl);

                        /* Allocate and zero the statistics tables */
                        /* Note that jpeg_gen_optimal_table expects 257 entries in each table! */
                        if (m_dc_count_ptrs[tbl] == null)
                            m_dc_count_ptrs[tbl] = new long[257];
                        else
                            Array.Clear(m_dc_count_ptrs[tbl], 0, m_dc_count_ptrs[tbl].Length);
                    }
                    else
                    {
                        /* Compute derived values for Huffman tables */
                        /* We may do this more than once for a table, but it's not expensive */
                        jpeg_make_c_derived_tbl(true, tbl, ref m_dc_derived_tbls[tbl]);
                    }

                    /* Initialize DC predictions to 0 */
                    m_saved.last_dc_val[ci] = 0;
                }

                /* AC needs no table when not present */
                if (m_cinfo.m_Se != 0)
                {
                    int tbl = compptr.Ac_tbl_no;
                    if (gather_statistics)
                    {
                        if (tbl < 0 || tbl >= JpegConstants.NUM_HUFF_TBLS)
                            m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_NO_HUFF_TABLE, tbl);

                        if (m_ac_count_ptrs[tbl] == null)
                            m_ac_count_ptrs[tbl] = new long[257];
                        else
                            Array.Clear(m_ac_count_ptrs[tbl], 0, m_ac_count_ptrs[tbl].Length);
                    }
                    else
                    {
                        jpeg_make_c_derived_tbl(false, tbl, ref m_ac_derived_tbls[tbl]);
                    }
                }
            }

            /* Initialize bit buffer to empty */
            m_saved.put_buffer = 0;
            m_saved.put_bits = 0;

            /* Initialize restart stuff */
            m_restarts_to_go = m_cinfo.m_restart_interval;
            m_next_restart_num = 0;
        }

        /// <summary>
        /// Encode and output one MCU's worth of Huffman-compressed coefficients.
        /// </summary>
        private bool encode_mcu_huff(JBLOCK[][] MCU_data)
        {
            /* Load up working state */
            savable_state state = new savable_state();
            state.ASSIGN_STATE(m_saved);

            /* Emit restart marker if needed */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    if (!emit_restart_s(state, m_next_restart_num))
                        return false;
                }
            }

            /* Encode the MCU data blocks */
            for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
            {
                int ci = m_cinfo.m_MCU_membership[blkn];
                jpeg_component_info compptr = m_cinfo.Component_info[m_cinfo.m_cur_comp_info[ci]];
                if (!encode_one_block(state, MCU_data[blkn][0].data, state.last_dc_val[ci],
                    m_dc_derived_tbls[compptr.Dc_tbl_no],
                    m_ac_derived_tbls[compptr.Ac_tbl_no]))
                {
                    return false;
                }

                /* Update last_dc_val */
                state.last_dc_val[ci] = MCU_data[blkn][0][0];
            }

            /* Completed MCU, so update state */
            m_saved.ASSIGN_STATE(state);

            /* Update restart-interval state too */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    m_restarts_to_go = m_cinfo.m_restart_interval;
                    m_next_restart_num++;
                    m_next_restart_num &= 7;
                }

                m_restarts_to_go--;
            }

            return true;
        }

        /// <summary>
        /// Finish up at the end of a Huffman-compressed scan.
        /// </summary>
        private void finish_pass_huff()
        {
            if (m_cinfo.m_progressive_mode)
            {
                /* Flush out any buffered data */
                emit_eobrun();
                flush_bits_e();
            }
            else
            {
                /* Load up working state ... flush_bits needs it */
                savable_state state = new savable_state();
                state.ASSIGN_STATE(m_saved);

                /* Flush out the last data */
                if (!flush_bits_s(state))
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_CANT_SUSPEND);

                /* Update state */
                m_saved.ASSIGN_STATE(state);
            }
        }

        /// <summary>
        /// Trial-encode one MCU's worth of Huffman-compressed coefficients.
        /// No data is actually output, so no suspension return is possible.
        /// </summary>
        private bool encode_mcu_gather(JBLOCK[][] MCU_data)
        {
            /* Take care of restart intervals if needed */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    /* Re-initialize DC predictions to 0 */
                    for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                        m_saved.last_dc_val[ci] = 0;

                    /* Update restart state */
                    m_restarts_to_go = m_cinfo.m_restart_interval;
                }

                m_restarts_to_go--;
            }

            for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
            {
                int ci = m_cinfo.m_MCU_membership[blkn];
                jpeg_component_info compptr = m_cinfo.Component_info[m_cinfo.m_cur_comp_info[ci]];
                htest_one_block(MCU_data[blkn][0].data, m_saved.last_dc_val[ci],
                    m_dc_count_ptrs[compptr.Dc_tbl_no],
                    m_ac_count_ptrs[compptr.Ac_tbl_no]);
                m_saved.last_dc_val[ci] = MCU_data[blkn][0][0];
            }

            return true;
        }

        /// <summary>
        /// Finish up a statistics-gathering pass and create the new Huffman tables.
        /// </summary>
        private void finish_pass_gather()
        {
            if (m_cinfo.m_progressive_mode)
            {
                /* Flush out buffered data (all we care about is counting the EOB symbol) */
                emit_eobrun();
            }

            /* It's important not to apply jpeg_gen_optimal_table more than once
             * per table, because it clobbers the input frequency counts!
             */
            bool[] did_dc = new bool[JpegConstants.NUM_HUFF_TBLS];
            bool[] did_ac = new bool[JpegConstants.NUM_HUFF_TBLS];

            for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
            {
                jpeg_component_info compptr = m_cinfo.Component_info[m_cinfo.m_cur_comp_info[ci]];
                /* DC needs no table for refinement scan */
                if (m_cinfo.m_Ss == 0 && m_cinfo.m_Ah == 0)
                {
                    int dctbl = compptr.Dc_tbl_no;
                    if (!did_dc[dctbl])
                    {
                        if (m_cinfo.m_dc_huff_tbl_ptrs[dctbl] == null)
                            m_cinfo.m_dc_huff_tbl_ptrs[dctbl] = new JHUFF_TBL();

                        jpeg_gen_optimal_table(m_cinfo.m_dc_huff_tbl_ptrs[dctbl], m_dc_count_ptrs[dctbl]);
                        did_dc[dctbl] = true;
                    }
                }

                /* AC needs no table when not present */
                if (m_cinfo.m_Se != 0)
                {
                    int actbl = compptr.Ac_tbl_no;
                    if (!did_ac[actbl])
                    {
                        if (m_cinfo.m_ac_huff_tbl_ptrs[actbl] == null)
                            m_cinfo.m_ac_huff_tbl_ptrs[actbl] = new JHUFF_TBL();

                        jpeg_gen_optimal_table(m_cinfo.m_ac_huff_tbl_ptrs[actbl], m_ac_count_ptrs[actbl]);
                        did_ac[actbl] = true;
                    }
                }
            }
        }

        /// <summary>
        /// Encode a single block's worth of coefficients
        /// </summary>
        private bool encode_one_block(savable_state state, short[] block, int last_dc_val, c_derived_tbl dctbl, c_derived_tbl actbl)
        {
            /* Encode the DC coefficient difference per section F.1.2.1 */
            int temp = block[0] - last_dc_val;
            int temp2 = temp;
            if (temp < 0)
            {
                temp = -temp;       /* temp is abs value of input */
                /* For a negative input, want temp2 = bitwise complement of abs(input) */
                /* This code assumes we are on a two's complement machine */
                temp2--;
            }

            /* Find the number of bits needed for the magnitude of the coefficient */
            int nbits = 0;
            while (temp != 0)
            {
                nbits++;
                temp >>= 1;
            }

            /* Check for out-of-range coefficient values.
             * Since we're encoding a difference, the range limit is twice as much.
             */
            if (nbits > MAX_COEF_BITS + 1)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCT_COEF);

            /* Emit the Huffman-coded symbol for the number of bits */
            if (!emit_bits_s(state, dctbl.ehufco[nbits], dctbl.ehufsi[nbits]))
                return false;

            /* Emit that number of bits of the value, if positive, */
            /* or the complement of its magnitude, if negative. */
            if (nbits != 0)
            {
                /* emit_bits rejects calls with size 0 */
                if (!emit_bits_s(state, temp2, nbits))
                    return false;
            }

            /* Encode the AC coefficients per section F.1.2.2 */
            int r = 0;          /* r = run length of zeros */
            int[] natural_order = m_cinfo.natural_order;
            int Se = m_cinfo.lim_Se;
            for (int k = 1; k <= Se; k++)
            {
                temp2 = block[natural_order[k]];
                if (temp2 == 0)
                {
                    r++;
                }
                else
                {
                    /* if run length > 15, must emit special run-length-16 codes (0xF0) */
                    while (r > 15)
                    {
                        if (!emit_bits_s(state, actbl.ehufco[0xF0], actbl.ehufsi[0xF0]))
                            return false;
                        r -= 16;
                    }

                    temp = temp2;
                    if (temp < 0)
                    {
                        temp = -temp;       /* temp is abs value of input */
                        /* This code assumes we are on a two's complement machine */
                        temp2--;
                    }

                    /* Find the number of bits needed for the magnitude of the coefficient */
                    nbits = 1;      /* there must be at least one 1 bit */
                    while ((temp >>= 1) != 0)
                        nbits++;

                    /* Check for out-of-range coefficient values */
                    if (nbits > MAX_COEF_BITS)
                        m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCT_COEF);

                    /* Emit Huffman symbol for run length / number of bits */
                    temp = (r << 4) + nbits;
                    if (!emit_bits_s(state, actbl.ehufco[temp], actbl.ehufsi[temp]))
                        return false;

                    /* Emit that number of bits of the value, if positive, */
                    /* or the complement of its magnitude, if negative. */
                    if (!emit_bits_s(state, temp2, nbits))
                        return false;

                    r = 0;
                }
            }

            /* If the last coef(s) were zero, emit an end-of-block code */
            if (r > 0)
            {
                if (!emit_bits_s(state, actbl.ehufco[0], actbl.ehufsi[0]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Huffman coding optimization.
        /// 
        /// We first scan the supplied data and count the number of uses of each symbol
        /// that is to be Huffman-coded. (This process MUST agree with the code above.)
        /// Then we build a Huffman coding tree for the observed counts.
        /// Symbols which are not needed at all for the particular image are not
        /// assigned any code, which saves space in the DHT marker as well as in
        /// the compressed data.
        /// </summary>
        private void htest_one_block(short[] block, int last_dc_val, long[] dc_counts, long[] ac_counts)
        {
            /* Encode the DC coefficient difference per section F.1.2.1 */
            int temp = block[0] - last_dc_val;
            if (temp < 0)
                temp = -temp;

            /* Find the number of bits needed for the magnitude of the coefficient */
            int nbits = 0;
            while (temp != 0)
            {
                nbits++;
                temp >>= 1;
            }

            /* Check for out-of-range coefficient values.
             * Since we're encoding a difference, the range limit is twice as much.
             */
            if (nbits > MAX_COEF_BITS + 1)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCT_COEF);

            /* Count the Huffman symbol for the number of bits */
            dc_counts[nbits]++;

            /* Encode the AC coefficients per section F.1.2.2 */
            int r = 0;          /* r = run length of zeros */
            int Se = m_cinfo.lim_Se;
            int[] natural_order = m_cinfo.natural_order;
            for (int k = 1; k <= Se; k++)
            {
                temp = block[natural_order[k]];
                if (temp == 0)
                {
                    r++;
                }
                else
                {
                    /* if run length > 15, must emit special run-length-16 codes (0xF0) */
                    while (r > 15)
                    {
                        ac_counts[0xF0]++;
                        r -= 16;
                    }

                    /* Find the number of bits needed for the magnitude of the coefficient */
                    if (temp < 0)
                        temp = -temp;

                    /* Find the number of bits needed for the magnitude of the coefficient */
                    nbits = 1;      /* there must be at least one 1 bit */
                    while ((temp >>= 1) != 0)
                        nbits++;

                    /* Check for out-of-range coefficient values */
                    if (nbits > MAX_COEF_BITS)
                        m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCT_COEF);

                    /* Count Huffman symbol for run length / number of bits */
                    ac_counts[(r << 4) + nbits]++;

                    r = 0;
                }
            }

            /* If the last coef(s) were zero, emit an end-of-block code */
            if (r > 0)
                ac_counts[0]++;
        }

        //////////////////////////////////////////////////////////////////////////
        // Outputting bytes to the file.
        // NB: these must be called only when actually outputting,
        // that is, entropy.gather_statistics == false.

        private bool emit_byte_s(int val)
        {
            return m_cinfo.m_dest.emit_byte(val);
        }

        private void emit_byte_e(int val)
        {
            m_cinfo.m_dest.emit_byte(val);
        }

        private bool dump_buffer_s()
        {
            // TODO: remove this method
            // do nothing.
            return true;
        }

        private bool dump_buffer_e()
        {
            // TODO: remove this method
            // do nothing.
            return true;
        }

        /// <summary>
        /// Only the right 24 bits of put_buffer are used; the valid bits are
        /// left-justified in this part.  At most 16 bits can be passed to emit_bits
        /// in one call, and we never retain more than 7 bits in put_buffer
        /// between calls, so 24 bits are sufficient.
        /// </summary>
        /// Emit some bits; return true if successful, false if must suspend
        private bool emit_bits_s(savable_state state, int code, int size)
        {
            /* This routine is heavily used, so it's worth coding tightly. */

            /* if size is 0, caller used an invalid Huffman table entry */
            if (size == 0)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_HUFF_MISSING_CODE);

            /* mask off any extra bits in code */
            int put_buffer = code & ((1 << size) - 1);

            /* new number of bits in buffer */
            int put_bits = size + state.put_bits;

            put_buffer <<= 24 - put_bits; /* align incoming bits */

            /* and merge with old buffer contents */
            put_buffer |= state.put_buffer;

            while (put_bits >= 8)
            {
                int c = (put_buffer >> 16) & 0xFF;
                if (!emit_byte_s(c))
                    return false;

                if (c == 0xFF)
                {
                    /* need to stuff a zero byte? */
                    if (!emit_byte_s(0))
                        return false;
                }

                put_buffer <<= 8;
                put_bits -= 8;
            }

            state.put_buffer = put_buffer; /* update state variables */
            state.put_bits = put_bits;

            return true;
        }

        /// <summary>
        /// Outputting bits to the file
        /// 
        /// Only the right 24 bits of put_buffer are used; the valid bits are
        /// left-justified in this part.  At most 16 bits can be passed to emit_bits
        /// in one call, and we never retain more than 7 bits in put_buffer
        /// between calls, so 24 bits are sufficient.
        /// </summary>
        /// Emit some bits, unless we are in gather mode
        private void emit_bits_e(int code, int size)
        {
            /* This routine is heavily used, so it's worth coding tightly. */

            /* if size is 0, caller used an invalid Huffman table entry */
            if (size == 0)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_HUFF_MISSING_CODE);

            if (m_gather_statistics)
            {
                /* do nothing if we're only getting stats */
                return;
            }

            int local_put_buffer = code & ((1 << size) - 1); /* mask off any extra bits in code */

            int put_bits = size + m_saved.put_bits;       /* new number of bits in buffer */

            local_put_buffer <<= 24 - put_bits; /* align incoming bits */

            local_put_buffer |= m_saved.put_buffer; /* and merge with old buffer contents */

            while (put_bits >= 8)
            {
                int c = (local_put_buffer >> 16) & 0xFF;

                emit_byte_e(c);
                if (c == 0xFF)
                {
                    /* need to stuff a zero byte? */
                    emit_byte_e(0);
                }
                local_put_buffer <<= 8;
                put_bits -= 8;
            }

            m_saved.put_buffer = local_put_buffer; /* update variables */
            m_saved.put_bits = put_bits;
        }

        private bool flush_bits_s(savable_state state)
        {
            if (!emit_bits_s(state, 0x7F, 7)) /* fill any partial byte with ones */
                return false;

            state.put_buffer = 0;  /* and reset bit-buffer to empty */
            state.put_bits = 0;
            return true;
        }

        private void flush_bits_e()
        {
            emit_bits_e(0x7F, 7); /* fill any partial byte with ones */
            m_saved.put_buffer = 0;     /* and reset bit-buffer to empty */
            m_saved.put_bits = 0;
        }

        // Emit (or just count) a Huffman symbol.
        private void emit_dc_symbol(int tbl_no, int symbol)
        {
            if (m_gather_statistics)
            {
                m_dc_count_ptrs[tbl_no][symbol]++;
            }
            else
            {
                c_derived_tbl tbl = m_dc_derived_tbls[tbl_no];
                emit_bits_e(tbl.ehufco[symbol], tbl.ehufsi[symbol]);
            }
        }

        private void emit_ac_symbol(int tbl_no, int symbol)
        {
            if (m_gather_statistics)
            {
                m_ac_count_ptrs[tbl_no][symbol]++;
            }
            else
            {
                c_derived_tbl tbl = m_ac_derived_tbls[tbl_no];
                emit_bits_e(tbl.ehufco[symbol], tbl.ehufsi[symbol]);
            }
        }

        // Emit bits from a correction bit buffer.
        private void emit_buffered_bits(uint offset, uint nbits)
        {
            if (m_gather_statistics)
            {
                /* no real work */
                return;
            }

            for (int i = 0; i < nbits; i++)
                emit_bits_e(bit_buffer[offset + i], 1);
        }

        // Emit any pending EOBRUN symbol.
        private void emit_eobrun()
        {
            if (EOBRUN > 0)
            {
                /* if there is any pending EOBRUN */
                uint temp = EOBRUN;
                int nbits = 0;
                while ((temp >>= 1) != 0)
                    nbits++;

                /* safety check: shouldn't happen given limited correction-bit buffer */
                if (nbits > 14)
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_HUFF_MISSING_CODE);

                emit_ac_symbol(ac_tbl_no, nbits << 4);
                if (nbits != 0)
                    emit_bits_e((int)EOBRUN, nbits);

                EOBRUN = 0;

                /* Emit any buffered correction bits */
                emit_buffered_bits(0, BE);
                BE = 0;
            }
        }

        /// <summary>
        /// Emit a restart marker and resynchronize predictions.
        /// </summary>
        private bool emit_restart_s(savable_state state, int restart_num)
        {
            if (!flush_bits_s(state))
                return false;

            if (!emit_byte_s(0xFF))
                return false;

            if (!emit_byte_s((int)(JPEG_MARKER.RST0 + restart_num)))
                return false;

            /* Re-initialize DC predictions to 0 */
            for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                state.last_dc_val[ci] = 0;

            /* The restart counter is not updated until we successfully write the MCU. */
            return true;
        }

        // Emit a restart marker & resynchronize predictions.
        private void emit_restart_e(int restart_num)
        {
            emit_eobrun();

            if (!m_gather_statistics)
            {
                flush_bits_e();
                emit_byte_e(0xFF);
                emit_byte_e((int)(JPEG_MARKER.RST0 + restart_num));
            }

            if (m_cinfo.m_Ss == 0)
            {
                /* Re-initialize DC predictions to 0 */
                for (int ci = 0; ci < m_cinfo.m_comps_in_scan; ci++)
                    m_saved.last_dc_val[ci] = 0;
            }
            else
            {
                /* Re-initialize all AC-related fields to 0 */
                EOBRUN = 0;
                BE = 0;
            }
        }

        /// <summary>
        /// IRIGHT_SHIFT is like RIGHT_SHIFT, but works on int rather than int.
        /// We assume that int right shift is unsigned if int right shift is,
        /// which should be safe.
        /// </summary>
        private static int IRIGHT_SHIFT(int x, int shft)
        {
            if (x < 0)
                return (x >> shft) | (~0) << (16 - shft);

            return (x >> shft);
        }

        /// <summary>
        /// MCU encoding for DC initial scan (either spectral selection,
        /// or first pass of successive approximation).
        /// </summary>
        private bool encode_mcu_DC_first(JBLOCK[][] MCU_data)
        {
            /* Emit restart marker if needed */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                    emit_restart_e(m_next_restart_num);
            }

            /* Encode the MCU data blocks */
            for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
            {
                int ci = m_cinfo.m_MCU_membership[blkn];
                int tbl = m_cinfo.Component_info[m_cinfo.m_cur_comp_info[ci]].Dc_tbl_no;

                /* Compute the DC value after the required point transform by Al.
                 * This is simply an arithmetic right shift.
                 */
                int temp = IRIGHT_SHIFT(MCU_data[blkn][0][0], m_cinfo.m_Al);

                /* DC differences are figured on the point-transformed values. */
                
                int temp2 = temp - m_saved.last_dc_val[ci];
                m_saved.last_dc_val[ci] = temp;

                /* Encode the DC coefficient difference per section G.1.2.1 */
                temp = temp2;
                if (temp < 0)
                {
                    /* temp is abs value of input */
                    temp = -temp;

                    /* For a negative input, want temp2 = bitwise complement of abs(input) */
                    /* This code assumes we are on a two's complement machine */
                    temp2--;
                }

                /* Find the number of bits needed for the magnitude of the coefficient */
                int nbits = 0;
                while (temp != 0)
                {
                    nbits++;
                    temp >>= 1;
                }

                /* Check for out-of-range coefficient values.
                 * Since we're encoding a difference, the range limit is twice as much.
                 */
                if (nbits > MAX_COEF_BITS + 1)
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCT_COEF);

                /* Count/emit the Huffman-coded symbol for the number of bits */
                emit_dc_symbol(tbl, nbits);

                /* Emit that number of bits of the value, if positive, */
                /* or the complement of its magnitude, if negative. */
                if (nbits != 0)
                {
                    /* emit_bits rejects calls with size 0 */
                    emit_bits_e(temp2, nbits);
                }
            }

            /* Update restart-interval state too */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    m_restarts_to_go = m_cinfo.m_restart_interval;
                    m_next_restart_num++;
                    m_next_restart_num &= 7;
                }

                m_restarts_to_go--;
            }

            return true;
        }

        /// <summary>
        /// MCU encoding for AC initial scan (either spectral selection,
        /// or first pass of successive approximation).
        /// </summary>
        private bool encode_mcu_AC_first(JBLOCK[][] MCU_data)
        {
            /* Emit restart marker if needed */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                    emit_restart_e(m_next_restart_num);
            }

            int[] natural_order = m_cinfo.natural_order;
            /* Encode the AC coefficients per section G.1.2.2, fig. G.3 */
            /* r = run length of zeros */
            int r = 0;
            for (int k = m_cinfo.m_Ss; k <= m_cinfo.m_Se; k++)
            {
                int temp = MCU_data[0][0][natural_order[k]];
                if (temp == 0)
                {
                    r++;
                    continue;
                }

                /* We must apply the point transform by Al.  For AC coefficients this
                 * is an integer division with rounding towards 0.  To do this portably
                 * in C, we shift after obtaining the absolute value; so the code is
                 * interwoven with finding the abs value (temp) and output bits (temp2).
                 */
                int temp2;
                if (temp < 0)
                {
                    temp = -temp;       /* temp is abs value of input */
                    temp >>= m_cinfo.m_Al;        /* apply the point transform */
                    /* For a negative coef, want temp2 = bitwise complement of abs(coef) */
                    temp2 = ~temp;
                }
                else
                {
                    temp >>= m_cinfo.m_Al;        /* apply the point transform */
                    temp2 = temp;
                }

                /* Watch out for case that nonzero coef is zero after point transform */
                if (temp == 0)
                {
                    r++;
                    continue;
                }

                /* Emit any pending EOBRUN */
                if (EOBRUN > 0)
                    emit_eobrun();

                /* if run length > 15, must emit special run-length-16 codes (0xF0) */
                while (r > 15)
                {
                    emit_ac_symbol(ac_tbl_no, 0xF0);
                    r -= 16;
                }

                /* Find the number of bits needed for the magnitude of the coefficient */
                int nbits = 1;          /* there must be at least one 1 bit */
                while ((temp >>= 1) != 0)
                    nbits++;

                /* Check for out-of-range coefficient values */
                if (nbits > MAX_COEF_BITS)
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCT_COEF);

                /* Count/emit Huffman symbol for run length / number of bits */
                emit_ac_symbol(ac_tbl_no, (r << 4) + nbits);

                /* Emit that number of bits of the value, if positive, */
                /* or the complement of its magnitude, if negative. */
                emit_bits_e(temp2, nbits);

                r = 0;          /* reset zero run length */
            }

            if (r > 0)
            {
                /* If there are trailing zeroes, */
                EOBRUN++;      /* count an EOB */
                if (EOBRUN == 0x7FFF)
                    emit_eobrun();   /* force it out to avoid overflow */
            }

            /* Update restart-interval state too */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    m_restarts_to_go = m_cinfo.m_restart_interval;
                    m_next_restart_num++;
                    m_next_restart_num &= 7;
                }
                m_restarts_to_go--;
            }

            return true;
        }

        /// <summary>
        /// MCU encoding for DC successive approximation refinement scan.
        /// Note: we assume such scans can be multi-component, although the spec
        /// is not very clear on the point.
        /// </summary>
        private bool encode_mcu_DC_refine(JBLOCK[][] MCU_data)
        {
            /* Emit restart marker if needed */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                    emit_restart_e(m_next_restart_num);
            }

            /* Encode the MCU data blocks */
            for (int blkn = 0; blkn < m_cinfo.m_blocks_in_MCU; blkn++)
            {
                /* We simply emit the Al'th bit of the DC coefficient value. */
                int temp = MCU_data[blkn][0][0];
                emit_bits_e(temp >> m_cinfo.m_Al, 1);
            }

            /* Update restart-interval state too */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    m_restarts_to_go = m_cinfo.m_restart_interval;
                    m_next_restart_num++;
                    m_next_restart_num &= 7;
                }
                m_restarts_to_go--;
            }

            return true;
        }

        /// <summary>
        /// MCU encoding for AC successive approximation refinement scan.
        /// </summary>
        private bool encode_mcu_AC_refine(JBLOCK[][] MCU_data)
        {
            /* Emit restart marker if needed */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                    emit_restart_e(m_next_restart_num);
            }

            /* Encode the MCU data block */

            /* It is convenient to make a pre-pass to determine the transformed
             * coefficients' absolute values and the EOB position.
             */
            int EOB = 0;
            int[] natural_order = m_cinfo.natural_order;
            int[] absvalues = new int[JpegConstants.DCTSIZE2];
            for (int k = m_cinfo.m_Ss; k <= m_cinfo.m_Se; k++)
            {
                int temp = MCU_data[0][0][natural_order[k]];

                /* We must apply the point transform by Al.  For AC coefficients this
                 * is an integer division with rounding towards 0.  To do this portably
                 * in C, we shift after obtaining the absolute value.
                 */
                if (temp < 0)
                    temp = -temp;       /* temp is abs value of input */

                temp >>= m_cinfo.m_Al;        /* apply the point transform */
                absvalues[k] = temp;    /* save abs value for main pass */

                if (temp == 1)
                {
                    /* EOB = index of last newly-nonzero coef */
                    EOB = k;
                }
            }

            /* Encode the AC coefficients per section G.1.2.3, fig. G.7 */

            int r = 0;          /* r = run length of zeros */
            uint BR = 0;         /* BR = count of buffered bits added now */
            uint bitBufferOffset = BE; /* Append bits to buffer */

            for (int k = m_cinfo.m_Ss; k <= m_cinfo.m_Se; k++)
            {
                int temp = absvalues[k];
                if (temp == 0)
                {
                    r++;
                    continue;
                }

                /* Emit any required ZRLs, but not if they can be folded into EOB */
                while (r > 15 && k <= EOB)
                {
                    /* emit any pending EOBRUN and the BE correction bits */
                    emit_eobrun();

                    /* Emit ZRL */
                    emit_ac_symbol(ac_tbl_no, 0xF0);
                    r -= 16;

                    /* Emit buffered correction bits that must be associated with ZRL */
                    emit_buffered_bits(bitBufferOffset, BR);
                    bitBufferOffset = 0;/* BE bits are gone now */
                    BR = 0;
                }

                /* If the coef was previously nonzero, it only needs a correction bit.
                 * NOTE: a straight translation of the spec's figure G.7 would suggest
                 * that we also need to test r > 15.  But if r > 15, we can only get here
                 * if k > EOB, which implies that this coefficient is not 1.
                 */
                if (temp > 1)
                {
                    /* The correction bit is the next bit of the absolute value. */
                    bit_buffer[bitBufferOffset + BR] = (char)(temp & 1);
                    BR++;
                    continue;
                }

                /* Emit any pending EOBRUN and the BE correction bits */
                emit_eobrun();

                /* Count/emit Huffman symbol for run length / number of bits */
                emit_ac_symbol(ac_tbl_no, (r << 4) + 1);

                /* Emit output bit for newly-nonzero coef */
                temp = (MCU_data[0][0][natural_order[k]] < 0) ? 0 : 1;
                emit_bits_e(temp, 1);

                /* Emit buffered correction bits that must be associated with this code */
                emit_buffered_bits(bitBufferOffset, BR);
                bitBufferOffset = 0;/* BE bits are gone now */
                BR = 0;
                r = 0;          /* reset zero run length */
            }

            if (r > 0 || BR > 0)
            {
                /* If there are trailing zeroes, */
                EOBRUN++;      /* count an EOB */
                BE += BR;      /* concat my correction bits to older ones */

                /* We force out the EOB if we risk either:
                 * 1. overflow of the EOB counter;
                 * 2. overflow of the correction bit buffer during the next MCU.
                 */
                if (EOBRUN == 0x7FFF || BE > (MAX_CORR_BITS - JpegConstants.DCTSIZE2 + 1))
                    emit_eobrun();
            }

            /* Update restart-interval state too */
            if (m_cinfo.m_restart_interval != 0)
            {
                if (m_restarts_to_go == 0)
                {
                    m_restarts_to_go = m_cinfo.m_restart_interval;
                    m_next_restart_num++;
                    m_next_restart_num &= 7;
                }
                m_restarts_to_go--;
            }

            return true;
        }

        /// <summary>
        /// Expand a Huffman table definition into the derived format
        /// Compute the derived values for a Huffman table.
        /// This routine also performs some validation checks on the table.
        /// </summary>
        private void jpeg_make_c_derived_tbl(bool isDC, int tblno, ref c_derived_tbl dtbl)
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
                dtbl = new c_derived_tbl();

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
            int lastp = p;

            /* Figure C.2: generate the codes themselves */
            /* We also validate that the counts represent a legal Huffman code tree. */

            int code = 0;
            int si = huffsize[0];
            p = 0;
            int[] huffcode = new int[257];
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

            /* Figure C.3: generate encoding tables */
            /* These are code and size indexed by symbol value */

            /* Set all codeless symbols to have code length 0;
            * this lets us detect duplicate VAL entries here, and later
            * allows emit_bits to detect any attempt to emit such symbols.
            */
            Array.Clear(dtbl.ehufsi, 0, dtbl.ehufsi.Length);

            /* This is also a convenient place to check for out-of-range
            * and duplicated VAL entries.  We allow 0..255 for AC symbols
            * but only 0..15 for DC.  (We could constrain them further
            * based on data depth and mode, but this seems enough.)
            */
            int maxsymbol = isDC ? 15 : 255;

            for (p = 0; p < lastp; p++)
            {
                int i = htbl.Huffval[p];
                if (i > maxsymbol || dtbl.ehufsi[i] != 0)
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_HUFF_TABLE);

                dtbl.ehufco[i] = huffcode[p];
                dtbl.ehufsi[i] = huffsize[p];
            }
        }

        /// <summary>
        /// Generate the best Huffman code table for the given counts, fill htbl.
        /// 
        /// The JPEG standard requires that no symbol be assigned a codeword of all
        /// one bits (so that padding bits added at the end of a compressed segment
        /// can't look like a valid code).  Because of the canonical ordering of
        /// codewords, this just means that there must be an unused slot in the
        /// longest codeword length category.  Section K.2 of the JPEG spec suggests
        /// reserving such a slot by pretending that symbol 256 is a valid symbol
        /// with count 1.  In theory that's not optimal; giving it count zero but
        /// including it in the symbol set anyway should give a better Huffman code.
        /// But the theoretically better code actually seems to come out worse in
        /// practice, because it produces more all-ones bytes (which incur stuffed
        /// zero bytes in the final file).  In any case the difference is tiny.
        /// 
        /// The JPEG standard requires Huffman codes to be no more than 16 bits long.
        /// If some symbols have a very small but nonzero probability, the Huffman tree
        /// must be adjusted to meet the code length restriction.  We currently use
        /// the adjustment method suggested in JPEG section K.2.  This method is *not*
        /// optimal; it may not choose the best possible limited-length code.  But
        /// typically only very-low-frequency symbols will be given less-than-optimal
        /// lengths, so the code is almost optimal.  Experimental comparisons against
        /// an optimal limited-length-code algorithm indicate that the difference is
        /// microscopic --- usually less than a hundredth of a percent of total size.
        /// So the extra complexity of an optimal algorithm doesn't seem worthwhile.
        /// </summary>
        protected void jpeg_gen_optimal_table(JHUFF_TBL htbl, long[] freq)
        {
            const int MAX_CLEN = 32;     /* assumed maximum initial code length */

            byte[] bits = new byte[MAX_CLEN + 1];   /* bits[k] = # of symbols with code length k */
            int[] codesize = new int[257];      /* codesize[k] = code length of symbol k */
            int[] others = new int[257];        /* next symbol in current branch of tree */
            int c1, c2;
            int p, i, j;
            long v;

            /* This algorithm is explained in section K.2 of the JPEG standard */
            for (i = 0; i < 257; i++)
                others[i] = -1;     /* init links to empty */

            freq[256] = 1;      /* make sure 256 has a nonzero count */
            /* Including the pseudo-symbol 256 in the Huffman procedure guarantees
            * that no real symbol is given code-value of all ones, because 256
            * will be placed last in the largest codeword category.
            */

            /* Huffman's basic algorithm to assign optimal code lengths to symbols */

            for (;;)
            {
                /* Find the smallest nonzero frequency, set c1 = its symbol */
                /* In case of ties, take the larger symbol number */
                c1 = -1;
                v = 1000000000L;
                for (i = 0; i <= 256; i++)
                {
                    if (freq[i] != 0 && freq[i] <= v)
                    {
                        v = freq[i];
                        c1 = i;
                    }
                }

                /* Find the next smallest nonzero frequency, set c2 = its symbol */
                /* In case of ties, take the larger symbol number */
                c2 = -1;
                v = 1000000000L;
                for (i = 0; i <= 256; i++)
                {
                    if (freq[i] != 0 && freq[i] <= v && i != c1)
                    {
                        v = freq[i];
                        c2 = i;
                    }
                }

                /* Done if we've merged everything into one frequency */
                if (c2 < 0)
                    break;

                /* Else merge the two counts/trees */
                freq[c1] += freq[c2];
                freq[c2] = 0;

                /* Increment the codesize of everything in c1's tree branch */
                codesize[c1]++;
                while (others[c1] >= 0)
                {
                    c1 = others[c1];
                    codesize[c1]++;
                }

                others[c1] = c2;        /* chain c2 onto c1's tree branch */

                /* Increment the codesize of everything in c2's tree branch */
                codesize[c2]++;
                while (others[c2] >= 0)
                {
                    c2 = others[c2];
                    codesize[c2]++;
                }
            }

            /* Now count the number of symbols of each code length */
            for (i = 0; i <= 256; i++)
            {
                if (codesize[i] != 0)
                {
                    /* The JPEG standard seems to think that this can't happen, */
                    /* but I'm paranoid... */
                    if (codesize[i] > MAX_CLEN)
                        m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_HUFF_CLEN_OVERFLOW);

                    bits[codesize[i]]++;
                }
            }

            /* JPEG doesn't allow symbols with code lengths over 16 bits, so if the pure
            * Huffman procedure assigned any such lengths, we must adjust the coding.
            * Here is what the JPEG spec says about how this next bit works:
            * Since symbols are paired for the longest Huffman code, the symbols are
            * removed from this length category two at a time.  The prefix for the pair
            * (which is one bit shorter) is allocated to one of the pair; then,
            * skipping the BITS entry for that prefix length, a code word from the next
            * shortest nonzero BITS entry is converted into a prefix for two code words
            * one bit longer.
            */

            for (i = MAX_CLEN; i > 16; i--)
            {
                while (bits[i] > 0)
                {
                    j = i - 2;      /* find length of new prefix to be used */
                    while (bits[j] == 0)
                        j--;

                    bits[i] -= 2;       /* remove two symbols */
                    bits[i - 1]++;      /* one goes in this length */
                    bits[j + 1] += 2;       /* two new symbols in this length */
                    bits[j]--;      /* symbol of this length is now a prefix */
                }
            }

            /* Remove the count for the pseudo-symbol 256 from the largest codelength */
            while (bits[i] == 0)        /* find largest codelength still in use */
                i--;
            bits[i]--;

            /* Return final symbol counts (only for lengths 0..16) */
            Buffer.BlockCopy(bits, 0, htbl.Bits, 0, htbl.Bits.Length);

            /* Return a list of the symbols sorted by code length */
            /* It's not real clear to me why we don't need to consider the codelength
            * changes made above, but the JPEG spec seems to think this works.
            */
            p = 0;
            for (i = 1; i <= MAX_CLEN; i++)
            {
                for (j = 0; j <= 255; j++)
                {
                    if (codesize[j] == i)
                    {
                        htbl.Huffval[p] = (byte)j;
                        p++;
                    }
                }
            }

            /* Set sent_table false so updated table will be written to JPEG file. */
            htbl.Sent_table = false;
        }
    }
}
