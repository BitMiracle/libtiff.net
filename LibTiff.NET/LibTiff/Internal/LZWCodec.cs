/*
 * TIFF Library.
 * Rev 5.0 Lempel-Ziv & Welch Compression Support
 *
 * This code is derived from the compress program whose code is
 * derived from software contributed to Berkeley by James A. Woods,
 * derived from original work by Spencer Thomas and Joseph Orost.
 *
 * The original Berkeley copyright notice appears below in its entirety.
 */

/*
 * NB: The 5.0 spec describes a different algorithm than Aldus
 *     implements.  Specifically, Aldus does code length transitions
 *     one code earlier than should be done (for real LZW).
 *     Earlier versions of this library implemented the correct
 *     LZW algorithm, but emitted codes in a bit order opposite
 *     to the TIFF spec.  Thus, to maintain compatibility w/ Aldus
 *     we interpret MSB-LSB ordered codes to be images written w/
 *     old versions of this library, but otherwise adhere to the
 *     Aldus "off by one" algorithm.
 *
 * Future revisions to the TIFF spec are expected to "clarify this issue".
 */

using System;
using System.Collections.Generic;
using System.Text;

using hcode_t = System.UInt16; /* codes fit in 16 bits */

namespace BitMiracle.LibTiff.Internal
{
    class LZWCodec : CodecWithPredictor
    {
        /*
        * Each strip of data is supposed to be terminated by a CODE_EOI.
        * If the following #define is included, the decoder will also
        * check for end-of-strip w/o seeing this code.  This makes the
        * library more robust, but also slower.
        */
        private const bool LZW_CHECKEOS = true; /* include checks for strips w/o EOI code */

        /*
        * The TIFF spec specifies that encoded bit
        * strings range from 9 to 12 bits.
        */
        private const ushort BITS_MIN = 9;       /* start with 9 bits */
        private const ushort BITS_MAX = 12;      /* max of 12 bit strings */

        /* predefined codes */
        private const hcode_t CODE_CLEAR = 256;     /* code to clear string table */
        private const hcode_t CODE_EOI = 257;     /* end-of-information code */
        private const hcode_t CODE_FIRST = 258;     /* first free code entry */
        private const hcode_t CODE_MAX = ((1 << BITS_MAX) - 1);
        private const hcode_t CODE_MIN = ((1 << BITS_MIN) - 1);

        private const int HSIZE = 9001;       /* 91% occupancy */
        private const int HSHIFT = (13 - 8);
        /* NB: +1024 is for compatibility with old files */
        private const int CSIZE = (((1 << BITS_MAX) - 1) + 1024);

        private const int CHECK_GAP = 10000;       /* enc_ratio check interval */

        /*
        * Decoding-specific state.
        */
        private struct code_t
        {
            int next;
            ushort length; /* string len, including this token */
            byte value; /* data value */
            byte firstchar; /* first token of string */
        };

        /*
        * Encoding-specific state.
        */
        private struct hash_t
        {
            int hash;
            hcode_t code;
        };

        private bool m_compatDecode;

        private ushort m_nbits; /* # of bits/code */
        private ushort m_maxcode; /* maximum code for base.nbits */
        private ushort m_free_ent; /* next free entry in hash table */
        private int m_nextdata; /* next bits of i/o */
        private int m_nextbits; /* # of valid bits in base.nextdata */

        private int m_rw_mode; /* preserve rw_mode from init */

        /* Decoding specific data */
        private int m_dec_nbitsmask; /* lzw_nbits 1 bits, right adjusted */
        private int m_dec_restart; /* restart count */
        private int m_dec_bitsleft; /* available bits in raw data */
        private bool m_oldStyleCodeFound; /* if true, old style LZW code found*/
        private int m_dec_codep; /* current recognized code */
        private int m_dec_oldcodep; /* previously recognized code */
        private int m_dec_free_entp; /* next free entry */
        private int m_dec_maxcodep; /* max available entry */
        private code_t[] m_dec_codetab; /* kept separate for small machines */

        /* Encoding specific data */
        private int m_enc_oldcode; /* last code encountered */
        private int m_enc_checkpoint; /* point at which to clear table */
        private int m_enc_ratio; /* current compression ratio */
        private int m_enc_incount; /* (input) data bytes encoded */
        private int m_enc_outcount; /* encoded (output) bytes */
        private int m_enc_rawlimit; /* bound on tif_rawdata buffer */
        private hash_t[] m_enc_hashtab; /* kept separate for small machines */

        public LZWCodec(Tiff tif, COMPRESSION scheme, string name)
            : base(tif, scheme, name)
        {
        }

        public override bool Init()
        {
            assert(m_scheme == COMPRESSION_LZW);

            m_dec_codetab = NULL;
            m_oldStyleCodeFound = false;
            m_enc_hashtab = NULL;
            m_rw_mode = m_tif.m_mode;

            /*
             * Install codec methods.
             */
            m_compatDecode = false;

            /*
             * Setup predictor setup.
             */
            TIFFPredictorInit(NULL);
            return true;
        }

        public override bool CanEncode()
        {
            return true;
        }

        public override bool CanDecode()
        {
            return true;
        }

        public override bool tif_predecode(UInt16 s)
        {
            return LZWPreDecode(s);
        }

        public override bool tif_preencode(UInt16 s)
        {
            return LZWPreEncode(s);
        }

        public override bool tif_postencode()
        {
            return LZWPostEncode();
        }

        public override void tif_cleanup()
        {
            return LZWCleanup();
        }

        // CodecWithPredictor overrides

        public override bool predictor_setupdecode()
        {
            return LZWSetupDecode();
        }

        public override bool predictor_decoderow(byte[] pp, int cc, UInt16 s)
        {
            if (m_compatDecode)
                return LZWDecodeCompat(pp, cc, s);

            return LZWDecode(pp, cc, s);
        }

        public override bool predictor_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            if (m_compatDecode)
                return LZWDecodeCompat(pp, cc, s);

            return LZWDecode(pp, cc, s);
        }

        public override bool predictor_decodetile(byte[] pp, int cc, UInt16 s)
        {
            if (m_compatDecode)
                return LZWDecodeCompat(pp, cc, s);

            return LZWDecode(pp, cc, s);
        }

        public override bool predictor_setupencode()
        {
            return LZWSetupEncode();
        }

        public override bool predictor_encoderow(byte[] pp, int cc, UInt16 s)
        {
            return LZWEncode(pp, cc, s);
        }

        public override bool predictor_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            return LZWEncode(pp, cc, s);
        }

        public override bool predictor_encodetile(byte[] pp, int cc, UInt16 s)
        {
            return LZWEncode(pp, cc, s);
        }

        private bool LZWSetupDecode()
        {
            static const char module[] = " LZWSetupDecode";
            if (m_dec_codetab == NULL)
            {
                m_dec_codetab = new code_t [CSIZE];
                if (m_dec_codetab == NULL)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "No space for LZW code table");
                    return false;
                }

                /*
                 * Pre-load the table.
                 */
                int code = 255;
                do
                {
                    m_dec_codetab[code].value = (byte)code;
                    m_dec_codetab[code].firstchar = (byte)code;
                    m_dec_codetab[code].length = 1;
                    m_dec_codetab[code].next = -1;
                }
                while (code--);

                /*
                * Zero-out the unused entries
                */
                memset(&m_dec_codetab[CODE_CLEAR], 0, (CODE_FIRST - CODE_CLEAR) * sizeof(code_t));
            }

            return true;
        }

        /*
         * Setup state for decoding a strip.
         */
        private bool LZWPreDecode(UInt16 s)
        {
            if (m_dec_codetab == NULL)
                tif_setupdecode();

            /*
             * Check for old bit-reversed codes.
             */
            if (m_tif.m_rawdata[0] == 0 && (m_tif.m_rawdata[1] & 0x1) != 0)
            {
                if (!m_oldStyleCodeFound)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "Old-style LZW codes, convert file");
                    m_compatDecode = true;

                    /*
                     * If doing horizontal differencing, must
                     * re-setup the predictor logic since we
                     * switched the basic decoder methods...
                     */
                    tif_setupdecode();
                    m_oldStyleCodeFound = true;
                }
                m_maxcode = CODE_MIN;
            }
            else
            {
                m_maxcode = CODE_MIN - 1;
                m_oldStyleCodeFound = false;
            }

            m_nbits = BITS_MIN;
            m_nextbits = 0;
            m_nextdata = 0;

            m_dec_restart = 0;
            m_dec_nbitsmask = CODE_MIN;
            m_dec_bitsleft = m_tif.m_rawcc << 3;
            m_dec_free_entp = CODE_FIRST;

            /*
             * Zero entries that are not yet filled in.  We do
             * this to guard against bogus input data that causes
             * us to index into undefined entries.  If you can
             * come up with a way to safely bounds-check input codes
             * while decoding then you can remove this operation.
             */
            memset(&m_dec_codetab[m_dec_free_entp], 0, (CSIZE - CODE_FIRST) * sizeof(code_t));
            m_dec_oldcodep = -1;
            m_dec_maxcodep = m_dec_nbitsmask - 1;
            return true;
        }

        private bool LZWDecode(byte[] op0, int occ0, UInt16 s)
        {
            assert(m_dec_codetab != NULL);

            int occ = occ0;
            int op = 0;

            /*
             * Restart interrupted output operation.
             */
            if (m_dec_restart != 0)
            {
                int codep = m_dec_codep;
                int residue = m_dec_codetab[codep].length - m_dec_restart;
                if (residue > occ)
                {
                    /*
                     * Residue from previous decode is sufficient
                     * to satisfy decode request.  Skip to the
                     * start of the decoded string, place decoded
                     * values in the output buffer, and return.
                     */
                    m_dec_restart += occ;
                    do
                    {
                        codep = m_dec_codetab[codep].next;
                    }
                    while (--residue > occ && codep != -1);

                    if (codep != -1)
                    {
                        int tp = occ;
                        do
                        {
                            tp--;
                            op0[op + tp] = m_dec_codetab[codep].value;
                            codep = m_dec_codetab[codep].next;
                        }
                        while (--occ && codep != -1);
                    }

                    return true;
                }

                /*
                 * Residue satisfies only part of the decode request.
                 */
                op += residue;
                occ -= residue;
                int tp = 0;
                do
                {
                    --tp;
                    int t = m_dec_codetab[codep].value;
                    codep = m_dec_codetab[codep].next;
                    op0[op + tp] = (byte)t;
                }
                while (--residue && codep != -1);

                m_dec_restart = 0;
            }

            while (occ > 0)
            {
                hcode_t code;
                NextCode(code, false);
                if (code == CODE_EOI)
                    break;

                if (code == CODE_CLEAR)
                {
                    m_dec_free_entp = CODE_FIRST;
                    memset(&m_dec_codetab[m_dec_free_entp], 0, (CSIZE - CODE_FIRST) * sizeof(code_t));

                    m_nbits = BITS_MIN;
                    m_dec_nbitsmask = CODE_MIN;
                    m_dec_maxcodep = m_dec_nbitsmask - 1;
                    NextCode(code, false);
                    
                    if (code == CODE_EOI)
                        break;
                    
                    if (code == CODE_CLEAR)
                    {
                        Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Corrupted LZW table at scanline %d", m_tif.m_row);
                        return false;
                    }

                    op0[op] = (byte)code;
                    op++;
                    occ--;
                    m_dec_oldcodep = code;
                    continue;
                }

                int codep = code;

                /*
                 * Add the new entry to the code table.
                 */
                if (m_dec_free_entp < 0 || m_dec_free_entp >= CSIZE)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Corrupted LZW table at scanline %d", m_tif.m_row);
                    return false;
                }

                m_dec_codetab[m_dec_free_entp].next = m_dec_oldcodep;
                if (m_dec_codetab[m_dec_free_entp].next < 0 || m_dec_codetab[m_dec_free_entp].next >= CSIZE)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Corrupted LZW table at scanline %d", m_tif.m_row);
                    return false;
                }

                m_dec_codetab[m_dec_free_entp].firstchar = m_dec_codetab[m_dec_codetab[m_dec_free_entp].next].firstchar;
                m_dec_codetab[m_dec_free_entp].length = m_dec_codetab[m_dec_codetab[m_dec_free_entp].next].length + 1;
                m_dec_codetab[m_dec_free_entp].value = (codep < m_dec_free_entp) ? m_dec_codetab[codep].firstchar : m_dec_codetab[m_dec_free_entp].firstchar;

                if (++m_dec_free_entp > m_dec_maxcodep)
                {
                    if (++m_nbits > BITS_MAX)
                    {
                        /* should not happen */
                        m_nbits = BITS_MAX;
                    }

                    m_dec_nbitsmask = MAXCODE(m_nbits);
                    m_dec_maxcodep = m_dec_nbitsmask - 1;
                }

                m_dec_oldcodep = code;
                if (code >= 256)
                {
                    /*
                     * Code maps to a string, copy string
                     * value to output (written in reverse).
                     */
                    if (m_dec_codetab[codep].length == 0)
                    {
                        Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Wrong length of decoded string: ""data probably corrupted at scanline %d", m_tif.m_row);
                        return false;
                    }

                    if (m_dec_codetab[codep].length > occ)
                    {
                        /*
                         * String is too long for decode buffer,
                         * locate portion that will fit, copy to
                         * the decode buffer, and setup restart
                         * logic for the next decoding call.
                         */
                        m_dec_codep = code;
                        do
                        {
                            codep = m_dec_codetab[codep].next;
                        }
                        while (codep != -1 && m_dec_codetab[codep].length > occ);

                        if (codep != -1)
                        {
                            m_dec_restart = occ;
                            int tp = occ;
                            do
                            {
                                tp--;
                                op0[op + tp] = m_dec_codetab[codep].value;
                                codep = m_dec_codetab[codep].next;
                            }
                            while (--occ && codep != -1);

                            if (codep != -1)
                                codeLoop();
                        }
                        break;
                    }

                    int len = m_dec_codetab[codep].length;
                    int tp = len;
                    do
                    {
                        --tp;
                        int t = m_dec_codetab[codep].value;
                        codep = m_dec_codetab[codep].next;
                        op0[op + tp] = (char)t;
                    }
                    while (codep != -1 && tp > 0);

                    if (codep != -1)
                    {
                        codeLoop();
                        break;
                    }
                    
                    op += len;
                    occ -= len;
                }
                else
                {
                    op0[op] = (byte)code;
                    op++;
                    occ--;
                }
            }

            if (occ > 0)
            {
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Not enough data at scanline %d (short %ld bytes)", m_tif.m_row, occ);
                return false;
            }

            return true;
        }

        private bool LZWDecodeCompat(byte[] op0, int occ0, UInt16 s)
        {
            int occ = occ0;
            int op = 0;

            /*
             * Restart interrupted output operation.
             */
            if (m_dec_restart)
            {
                int residue;

                int codep = m_dec_codep;
                residue = m_dec_codetab[codep].length - m_dec_restart;
                if (residue > occ)
                {
                    /*
                     * Residue from previous decode is sufficient
                     * to satisfy decode request.  Skip to the
                     * start of the decoded string, place decoded
                     * values in the output buffer, and return.
                     */
                    m_dec_restart += occ;
                    do
                    {
                        codep = m_dec_codetab[codep].next;
                    }
                    while (--residue > occ);
                    
                    int tp = occ;
                    do
                    {
                        --tp;
                        op0[op + tp] = m_dec_codetab[codep].value;
                        codep = m_dec_codetab[codep].next;
                    }
                    while (--occ);

                    return true;
                }

                /*
                 * Residue satisfies only part of the decode request.
                 */
                op += residue;
                occ -= residue;
                int tp = 0;
                do
                {
                    --tp;
                    op0[op + tp] = m_dec_codetab[codep].value;
                    codep = m_dec_codetab[codep].next;
                }
                while (--residue);

                m_dec_restart = 0;
            }

            while (occ > 0)
            {
                UInt16 code;
                NextCode(code, true);
                if (code == CODE_EOI)
                    break;
                
                if (code == CODE_CLEAR)
                {
                    m_dec_free_entp = CODE_FIRST;
                    memset(&m_dec_codetab[m_dec_free_entp], 0, (CSIZE - CODE_FIRST) * sizeof(code_t));

                    m_nbits = BITS_MIN;
                    m_dec_nbitsmask = CODE_MIN;
                    m_dec_maxcodep = m_dec_nbitsmask;
                    NextCode(code, true);
                    
                    if (code == CODE_EOI)
                        break;

                    if (code == CODE_CLEAR)
                    {
                        Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Corrupted LZW table at scanline %d", m_tif.m_row);
                        return false;
                    }

                    op0[op] = (byte)code;
                    op++;
                    occ--;
                    m_dec_oldcodep = code;
                    continue;
                }

                int codep = code;

                /*
                 * Add the new entry to the code table.
                 */
                if (m_dec_free_entp < 0 || m_dec_free_entp >= CSIZE)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecodeCompat: Corrupted LZW table at scanline %d", m_tif.m_row);
                    return false;
                }

                m_dec_codetab[m_dec_free_entp].next = m_dec_oldcodep;
                if (m_dec_codetab[m_dec_free_entp].next < 0 || m_dec_codetab[m_dec_free_entp].next >= CSIZE)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecodeCompat: Corrupted LZW table at scanline %d", m_tif.m_row);
                    return false;
                }

                m_dec_codetab[m_dec_free_entp].firstchar = m_dec_codetab[m_dec_codetab[m_dec_free_entp].next].firstchar;
                m_dec_codetab[m_dec_free_entp].length = m_dec_codetab[m_dec_codetab[m_dec_free_entp].next].length + 1;
                m_dec_codetab[m_dec_free_entp].value = (codep < m_dec_free_entp) ? m_dec_codetab[codep].firstchar : m_dec_codetab[m_dec_free_entp].firstchar;
                if (++m_dec_free_entp > m_dec_maxcodep)
                {
                    if (++m_nbits > BITS_MAX)
                    {
                        /* should not happen */
                        m_nbits = BITS_MAX;
                    }
                    m_dec_nbitsmask = LZWCodec::MAXCODE(m_nbits);
                    m_dec_maxcodep = m_dec_nbitsmask;
                }

                m_dec_oldcodep = code;
                if (code >= 256)
                {
                    int op_orig = op;

                    /*
                     * Code maps to a string, copy string
                     * value to output (written in reverse).
                     */
                    if (m_dec_codetab[codep].length == 0)
                    {
                        Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecodeCompat: Wrong length of decoded ""string: data probably corrupted at scanline %d", m_tif.m_row);
                        return false;
                    }

                    if (m_dec_codetab[codep].length > occ)
                    {
                        /*
                         * String is too long for decode buffer,
                         * locate portion that will fit, copy to
                         * the decode buffer, and setup restart
                         * logic for the next decoding call.
                         */
                        m_dec_codep = code;
                        do
                        {
                            codep = m_dec_codetab[codep].next;
                        }
                        while (m_dec_codetab[codep].length > occ);

                        m_dec_restart = occ;
                        int tp = occ;
                        do
                        {
                            --tp;
                            op0[op + tp] = m_dec_codetab[codep].value;
                            codep = m_dec_codetab[codep].next;
                        }
                        while (--occ);

                        break;
                    }

                    op += m_dec_codetab[codep].length;
                    occ -= m_dec_codetab[codep].length;
                    int tp = op;
                    do
                    {
                        --tp;
                        op0[tp] = m_dec_codetab[codep].value;
                        codep = m_dec_codetab[codep].next;
                    }
                    while (codep != -1 && tp > op_orig);
                }
                else
                {
                    op0[op] = (char)code;
                    op++;
                    occ--;
                }
            }

            if (occ > 0)
            {
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecodeCompat: Not enough data at scanline %d (short %ld bytes)", m_tif.m_row, occ);
                return false;
            }

            return true;
        }
        
        private bool LZWSetupEncode()
        {
            static const char module[] = "LZWSetupEncode";

            m_enc_hashtab = new hash_t [HSIZE];
            if (m_enc_hashtab == NULL)
            {
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "No space for LZW hash table");
                return false;
            }

            return true;
        }

        /*
         * Reset encoding state at the start of a strip.
         */
        private bool LZWPreEncode(UInt16 s)
        {
            if (m_enc_hashtab == NULL)
                tif_setupencode();

            m_nbits = BITS_MIN;
            m_maxcode = CODE_MIN;
            m_free_ent = CODE_FIRST;
            m_nextbits = 0;
            m_nextdata = 0;
            m_enc_checkpoint = CHECK_GAP;
            m_enc_ratio = 0;
            m_enc_incount = 0;
            m_enc_outcount = 0;

            /*
             * The 4 here insures there is space for 2 max-sized
             * codes in LZWEncode and LZWPostDecode.
             */
            m_enc_rawlimit = m_tif.m_rawdatasize - 1 - 4;
            cl_hash(); /* clear hash table */
            m_enc_oldcode = (hcode_t) - 1; /* generates CODE_CLEAR in LZWEncode */
            return true;
        }

        /*
         * Finish off an encoded strip by flushing the last
         * string and tacking on an End Of Information code.
         */
        private bool LZWPostEncode()
        {
            if (m_tif.m_rawcp > m_enc_rawlimit)
            {
                m_tif.m_rawcc = (int)m_tif.m_rawcp;
                m_tif.flushData1();
                m_tif.m_rawcp = 0;
            }

            if (m_enc_oldcode != (hcode_t) - 1)
            {
                PutNextCode(m_enc_oldcode);
                m_enc_oldcode = (hcode_t) - 1;
            }

            PutNextCode(CODE_EOI);

            if (m_nextbits > 0)
            {
                m_tif.m_rawdata[m_tif.m_rawcp] = (byte)(m_nextdata << (8 - m_nextbits));
                m_tif.m_rawcp++;
            }

            m_tif.m_rawcc = (int)m_tif.m_rawcp;
            return true;
        }

        /*
        * Encode a chunk of pixels.
        *
        * Uses an open addressing double hashing (no chaining) on the 
        * prefix code/next character combination.  We do a variant of
        * Knuth's algorithm D (vol. 3, sec. 6.4) along with G. Knott's
        * relatively-prime secondary probe.  Here, the modular division
        * first probe is gives way to a faster exclusive-or manipulation. 
        * Also do block compression with an adaptive reset, whereby the
        * code table is cleared when the compression ratio decreases,
        * but after the table fills.  The variable-length output codes
        * are re-sized at this point, and a CODE_CLEAR is generated
        * for the decoder. 
        */
        private bool LZWEncode(byte[] bp, int cc, UInt16 s)
        {
            assert(m_enc_hashtab != NULL);
            int bpPos = 0;
            if (m_enc_oldcode == (hcode_t)-1 && cc > 0)
            {
                /*
                 * NB: This is safe because it can only happen
                 *     at the start of a strip where we know there
                 *     is space in the data buffer.
                 */
                PutNextCode(CODE_CLEAR);
                m_enc_oldcode = bp[bpPos];
                bpPos++;
                cc--;
                m_enc_incount++;
            }

            while (cc > 0)
            {
                int c = bp[bpPos];
                bpPos++;
                cc--;
                m_enc_incount++;
                int fcode = (c << BITS_MAX) + m_enc_oldcode;
                int h = (c << HSHIFT) ^ m_enc_oldcode; /* xor hashing */

                /*
                 * Check hash index for an overflow.
                 */
                if (h >= HSIZE)
                    h -= HSIZE;

                if (m_enc_hashtab[h].hash == fcode)
                {
                    m_enc_oldcode = m_enc_hashtab[h].code;
                    continue;
                }

                bool hit = false;

                if (m_enc_hashtab[h].hash >= 0)
                {
                    /*
                     * Primary hash failed, check secondary hash.
                     */
                    int disp = HSIZE - h;
                    if (h == 0)
                        disp = 1;
                    do
                    {
                        h -= disp;
                        if (h < 0)
                            h += HSIZE;

                        if (m_enc_hashtab[h].hash == fcode)
                        {
                            m_enc_oldcode = m_enc_hashtab[h].code;
                            hit = true;
                            break;
                        }
                    }
                    while (m_enc_hashtab[h].hash >= 0);
                }

                if (!hit)
                {
                    /*
                     * New entry, emit code and add to table.
                     */
                    /*
                     * Verify there is space in the buffer for the code
                     * and any potential Clear code that might be emitted
                     * below.  The value of limit is setup so that there
                     * are at least 4 bytes free--room for 2 codes.
                     */
                    if (m_tif.m_rawcp > m_enc_rawlimit)
                    {
                        m_tif.m_rawcc = (int)m_tif.m_rawcp;
                        m_tif.flushData1();
                        m_tif.m_rawcp = 0;
                    }

                    PutNextCode(m_enc_oldcode);
                    m_enc_oldcode = (hcode_t)c;
                    m_enc_hashtab[h].code = (hcode_t)m_free_ent;
                    m_free_ent++;
                    m_enc_hashtab[h].hash = fcode;
                    if (m_free_ent == CODE_MAX - 1)
                    {
                        /* table is full, emit clear code and reset */
                        cl_hash();
                        m_enc_ratio = 0;
                        m_enc_incount = 0;
                        m_enc_outcount = 0;
                        m_free_ent = CODE_FIRST;
                        PutNextCode(CODE_CLEAR);
                        m_nbits = BITS_MIN;
                        m_maxcode = CODE_MIN;
                    }
                    else
                    {
                        /*
                         * If the next entry is going to be too big for
                         * the code size, then increase it, if possible.
                         */
                        if (m_free_ent > m_maxcode)
                        {
                            m_nbits++;
                            assert(m_nbits <= BITS_MAX);
                            m_maxcode = (unsigned short)MAXCODE(m_nbits);
                        }
                        else if (m_enc_incount >= m_enc_checkpoint)
                        {
                            /*
                             * Check compression ratio and, if things seem
                             * to be slipping, clear the hash table and
                             * reset state.  The compression ratio is a
                             * 24+8-bit fractional number.
                             */
                            m_enc_checkpoint = m_enc_incount + CHECK_GAP;

                            int rat;
                            if (m_enc_incount > 0x007fffff)
                            {
                                /* NB: shift will overflow */
                                rat = m_enc_outcount >> 8;
                                rat = (rat == 0 ? 0x7fffffff : m_enc_incount / rat);
                            }
                            else
                                rat = (m_enc_incount << 8) / m_enc_outcount;

                            if (rat <= m_enc_ratio)
                            {
                                cl_hash();
                                m_enc_ratio = 0;
                                m_enc_incount = 0;
                                m_enc_outcount = 0;
                                m_free_ent = CODE_FIRST;
                                PutNextCode(CODE_CLEAR);
                                m_nbits = BITS_MIN;
                                m_maxcode = CODE_MIN;
                            }
                            else
                                m_enc_ratio = rat;
                        }
                    }
                }
            }

            return true;
        }

        private void LZWCleanup()
        {
            delete m_dec_codetab;
            m_dec_codetab = NULL;

            delete m_enc_hashtab;
            m_enc_hashtab = NULL;
        }

        private static int MAXCODE(int n)
        {
            return ((1 << n) - 1);
        }

        private void PutNextCode(int c)
        {
            m_nextdata = (m_nextdata << m_nbits) | c;
            m_nextbits += m_nbits;
            m_tif.m_rawdata[m_tif.m_rawcp] = (byte)(m_nextdata >> (m_nextbits - 8));
            m_tif.m_rawcp++;
            m_nextbits -= 8;
            if (m_nextbits >= 8)
            {
                m_tif.m_rawdata[m_tif.m_rawcp] = (byte)(m_nextdata >> (m_nextbits - 8));
                m_tif.m_rawcp++;
                m_nextbits -= 8;
            }

            m_enc_outcount += m_nbits;
        }

        /*
         * Reset encoding hash table.
         */
        private void cl_hash()
        {
            int hp = HSIZE - 1;
            int i = HSIZE - 8;

            do
            {
                i -= 8;
                m_enc_hashtab[hp - 7].hash = -1;
                m_enc_hashtab[hp - 6].hash = -1;
                m_enc_hashtab[hp - 5].hash = -1;
                m_enc_hashtab[hp - 4].hash = -1;
                m_enc_hashtab[hp - 3].hash = -1;
                m_enc_hashtab[hp - 2].hash = -1;
                m_enc_hashtab[hp - 1].hash = -1;
                m_enc_hashtab[hp].hash = -1;
                hp -= 8;
            }
            while (i >= 0);

            for (i += 8; i > 0; i--, hp--)
                m_enc_hashtab[hp].hash = -1;
        }

        private void NextCode(out UInt16 _code, bool compat)
        {
            if (LZW_CHECKEOS)
            {
                if (m_dec_bitsleft < m_nbits)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Strip %d not terminated with EOI code", m_tif.m_curstrip);
                    _code = CODE_EOI;
                }
                else
                {
                    if (compat)
                        GetNextCodeCompat(_code);
                    else
                        GetNextCode(_code);

                    m_dec_bitsleft -= m_nbits;
                }
            }
            else
            {
                if (compat)
                    GetNextCodeCompat(_code);
                else
                    GetNextCode(_code);
            }
        }

        private void GetNextCode(out UInt16 code)
        {
            m_nextdata = (m_nextdata << 8) | m_tif.m_rawdata[m_tif.m_rawcp];
            m_tif.m_rawcp++;
            m_nextbits += 8;
            if (m_nextbits < m_nbits)
            {
                m_nextdata = (m_nextdata << 8) | m_tif.m_rawdata[m_tif.m_rawcp];
                m_tif.m_rawcp++;
                m_nextbits += 8;
            }
            code = (hcode_t)((m_nextdata >> (m_nextbits - m_nbits)) & m_dec_nbitsmask);
            m_nextbits -= m_nbits;
        }

        private void GetNextCodeCompat(out UInt16 code)
        {
            m_nextdata |= (unsigned int)m_tif.m_rawdata[m_tif.m_rawcp] << m_nextbits;
            m_tif.m_rawcp++;
            m_nextbits += 8;
            if (m_nextbits < m_nbits)
            {
                m_nextdata |= (unsigned int)m_tif.m_rawdata[m_tif.m_rawcp] << m_nextbits;
                m_tif.m_rawcp++;
                m_nextbits += 8;
            }
            code = (hcode_t)(m_nextdata & m_dec_nbitsmask);
            m_nextdata >>= m_nbits;
            m_nextbits -= m_nbits;
        }

        private void codeLoop()
        {
            Tiff::ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "LZWDecode: Bogus encoding, loop in the code table; scanline %d", m_tif.m_row);
        }
    }
}

/*
 * Copyright (c) 1985, 1986 The Regents of the University of California.
 * All rights reserved.
 *
 * This code is derived from software contributed to Berkeley by
 * James A. Woods, derived from original work by Spencer Thomas
 * and Joseph Orost.
 *
 * Redistribution and use in source and binary forms are permitted
 * provided that the above copyright notice and this paragraph are
 * duplicated in all such forms and that any documentation,
 * advertising materials, and other materials related to such
 * distribution and use acknowledge that the software was developed
 * by the University of California, Berkeley.  The name of the
 * University may not be used to endorse or promote products derived
 * from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND WITHOUT ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, WITHOUT LIMITATION, THE IMPLIED
 * WARRANTIES OF MERCHANTIBILITY AND FITNESS FOR A PARTICULAR PURPOSE.
 */
