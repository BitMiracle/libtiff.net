/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

/*
 * PackBits Compression Algorithm Support
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    class PackBitsCodec : TiffCodec
    {
        private enum EncodingState
        {
            BASE,
            LITERAL,
            RUN,
            LITERAL_RUN
        };

        private int m_rowsize;

        public PackBitsCodec(Tiff tif, Compression scheme, string name)
            : base(tif, scheme, name)
        {
        }

        public override bool Init()
        {
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

        public override bool tif_decoderow(byte[] pp, int cc, short s)
        {
            return PackBitsDecode(pp, cc, s);
        }

        public override bool tif_decodestrip(byte[] pp, int cc, short s)
        {
            return PackBitsDecode(pp, cc, s);
        }

        public override bool tif_decodetile(byte[] pp, int cc, short s)
        {
            return PackBitsDecode(pp, cc, s);
        }

        public override bool tif_preencode(short s)
        {
            return PackBitsPreEncode(s);
        }

        public override bool tif_encoderow(byte[] pp, int cc, short s)
        {
            return PackBitsEncode(pp, 0, cc, s);
        }

        public override bool tif_encodestrip(byte[] pp, int cc, short s)
        {
            return PackBitsEncodeChunk(pp, cc, s);
        }

        public override bool tif_encodetile(byte[] pp, int cc, short s)
        {
            return PackBitsEncodeChunk(pp, cc, s);
        }

        private bool PackBitsPreEncode(short s)
        {
            /*
             * Calculate the scanline/tile-width size in bytes.
             */
            if (m_tif.IsTiled())
                m_rowsize = m_tif.TileRowSize();
            else
                m_rowsize = m_tif.ScanlineSize();
            return true;
        }

        /*
        * Encode a run of pixels.
        */
        private bool PackBitsEncode(byte[] buf, int offset, int cc, short s)
        {
            int op = m_tif.m_rawcp;
            EncodingState state = EncodingState.BASE;
            int lastliteral = 0;
            int bp = offset;
            while (cc > 0)
            {
                /*
                 * Find the longest string of identical bytes.
                 */
                int b = buf[bp];
                bp++;
                cc--;
                int n = 1;
                for (; cc > 0 && b == buf[bp]; cc--, bp++)
                    n++;

                bool stop = false;
                while (!stop)
                {
                    if (op + 2 >= m_tif.m_rawdatasize)
                    {
                        /* insure space for new data */
                        /*
                         * Be careful about writing the last
                         * literal.  Must write up to that point
                         * and then copy the remainder to the
                         * front of the buffer.
                         */
                        if (state == EncodingState.LITERAL || state == EncodingState.LITERAL_RUN)
                        {
                            int slop = op - lastliteral;
                            m_tif.m_rawcc += lastliteral - m_tif.m_rawcp;
                            if (!m_tif.flushData1())
                                return false;
                            op = m_tif.m_rawcp;
                            while (slop-- > 0)
                            {
                                m_tif.m_rawdata[op] = m_tif.m_rawdata[lastliteral];
                                lastliteral++;
                                op++;
                            }

                            lastliteral = m_tif.m_rawcp;
                        }
                        else
                        {
                            m_tif.m_rawcc += op - m_tif.m_rawcp;
                            if (!m_tif.flushData1())
                                return false;
                            op = m_tif.m_rawcp;
                        }
                    }

                    switch (state)
                    {
                        case EncodingState.BASE:
                            /* initial state, set run/literal */
                            if (n > 1)
                            {
                                state = EncodingState.RUN;
                                if (n > 128)
                                {
                                    int temp = -127;
                                    m_tif.m_rawdata[op] = (byte)temp;
                                    op++;
                                    m_tif.m_rawdata[op] = (byte)b;
                                    op++;
                                    n -= 128;
                                    continue;
                                }

                                m_tif.m_rawdata[op] = (byte)(-n + 1);
                                op++;
                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                            }
                            else
                            {
                                lastliteral = op;
                                m_tif.m_rawdata[op] = 0;
                                op++;
                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                                state = EncodingState.LITERAL;
                            }
                            stop = true;
                            break;

                        case EncodingState.LITERAL:
                            /* last object was literal string */
                            if (n > 1)
                            {
                                state = EncodingState.LITERAL_RUN;
                                if (n > 128)
                                {
                                    int temp = -127;
                                    m_tif.m_rawdata[op] = (byte)temp;
                                    op++;
                                    m_tif.m_rawdata[op] = (byte)b;
                                    op++;
                                    n -= 128;
                                    continue;
                                }

                                m_tif.m_rawdata[op] = (byte)(-n + 1); /* encode run */
                                op++;
                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                            }
                            else
                            {
                                /* extend literal */
                                m_tif.m_rawdata[lastliteral]++;
                                if (m_tif.m_rawdata[lastliteral] == 127)
                                    state = EncodingState.BASE;

                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                            }
                            stop = true;
                            break;

                        case EncodingState.RUN:
                            /* last object was run */
                            if (n > 1)
                            {
                                if (n > 128)
                                {
                                    int temp = -127;
                                    m_tif.m_rawdata[op] = (byte)temp;
                                    op++;
                                    m_tif.m_rawdata[op] = (byte)b;
                                    op++;
                                    n -= 128;
                                    continue;
                                }

                                m_tif.m_rawdata[op] = (byte)(-n + 1);
                                op++;
                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                            }
                            else
                            {
                                lastliteral = op;
                                m_tif.m_rawdata[op] = 0;
                                op++;
                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                                state = EncodingState.LITERAL;
                            }
                            stop = true;
                            break;

                        case EncodingState.LITERAL_RUN:
                            /* literal followed by a run */
                            /*
                             * Check to see if previous run should
                             * be converted to a literal, in which
                             * case we convert literal-run-literal
                             * to a single literal.
                             */
                            int atemp = -1;
                            if (n == 1 && m_tif.m_rawdata[op - 2] == (byte)atemp && m_tif.m_rawdata[lastliteral] < 126)
                            {
                                m_tif.m_rawdata[lastliteral] += 2;
                                state = (m_tif.m_rawdata[lastliteral] == 127 ? EncodingState.BASE : EncodingState.LITERAL);
                                m_tif.m_rawdata[op - 2] = m_tif.m_rawdata[op - 1]; /* replicate */
                            }
                            else
                                state = EncodingState.RUN;
                            continue;
                    }
                }
            }

            m_tif.m_rawcc += op - m_tif.m_rawcp;
            m_tif.m_rawcp = op;
            return true;
        }

        /*
        * Encode a rectangular chunk of pixels.  We break it up
        * into row-sized pieces to insure that encoded runs do
        * not span rows.  Otherwise, there can be problems with
        * the decoder if data is read, for example, by scanlines
        * when it was encoded by strips.
        */
        private bool PackBitsEncodeChunk(byte[] bp, int cc, short s)
        {
            int offset = 0;
            while (cc > 0)
            {
                int chunk = m_rowsize;
                if (cc < chunk)
                    chunk = cc;

                if (!PackBitsEncode(bp, offset, chunk, s))
                    return false;

                offset += chunk;
                cc -= chunk;
            }

            return true;
        }

        private bool PackBitsDecode(byte[] op, int occ, short s)
        {
            int bp = m_tif.m_rawcp;
            int cc = m_tif.m_rawcc;
            int opPos = 0;
            while (cc > 0 && occ > 0)
            {
                int n = m_tif.m_rawdata[bp];
                bp++;
                cc--;

                /*
                 * Watch out for compilers that
                 * don't sign extend chars...
                 */
                if (n >= 128)
                    n -= 256;

                if (n < 0)
                {
                    /* replicate next byte (-n + 1) times */
                    if (n == -128)
                    {
                        /* nop */
                        continue;
                    }

                    n = -n + 1;
                    if (occ < n)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                            "PackBitsDecode: discarding {0} bytes to avoid buffer overrun",
                            n - occ);

                        n = occ;
                    }
                    occ -= n;
                    int b = m_tif.m_rawdata[bp];
                    bp++;
                    cc--;
                    while (n-- > 0)
                    {
                        op[opPos] = (byte)b;
                        opPos++;
                    }
                }
                else
                {
                    /* copy next (n + 1) bytes literally */
                    if (occ < n + 1)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                            "PackBitsDecode: discarding {0} bytes to avoid buffer overrun",
                            n - occ + 1);

                        n = occ - 1;
                    }

                    Array.Copy(m_tif.m_rawdata, bp, op, opPos, ++n);
                    opPos += n;
                    occ -= n;
                    bp += n;
                    cc -= n;
                }
            }

            m_tif.m_rawcp = bp;
            m_tif.m_rawcc = cc;
            if (occ > 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                    "PackBitsDecode: Not enough data for scanline {0}", m_tif.m_row);
                return false;
            }

            return true;
        }
    }
}
