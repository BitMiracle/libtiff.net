/*
 * TIFF Library.
 *
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

        public PackBitsCodec(Tiff tif, COMPRESSION scheme, string name)
            : base(tif, m_scheme, name)
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

        public override bool tif_decoderow(byte[] pp, int cc, UInt16 s)
        {
            return PackBitsDecode(pp, cc, s);
        }

        public override bool tif_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            return PackBitsDecode(pp, cc, s);
        }

        public override bool tif_decodetile(byte[] pp, int cc, UInt16 s)
        {
            return PackBitsDecode(pp, cc, s);
        }

        public override bool tif_preencode(UInt16 s)
        {
            return PackBitsPreEncode(s);
        }

        public override bool tif_encoderow(byte[] pp, int cc, UInt16 s)
        {
            return PackBitsEncode(pp, 0, cc, s);
        }

        public override bool tif_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            return PackBitsEncodeChunk(pp, cc, s);
        }

        public override bool tif_encodetile(byte[] pp, int cc, UInt16 s)
        {
            return PackBitsEncodeChunk(pp, cc, s);
        }

        private bool PackBitsPreEncode(UInt16 s)
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
        private bool PackBitsEncode(byte[] buf, int offset, int cc, UInt16 s)
        {
            int op = m_tif.m_rawcp;
            EncodingState state = BASE;
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
                        if (state == LITERAL || state == LITERAL_RUN)
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
                        case BASE:
                            /* initial state, set run/literal */
                            if (n > 1)
                            {
                                state = RUN;
                                if (n > 128)
                                {
                                    m_tif.m_rawdata[op] = (byte)-127;
                                    op++;
                                    m_tif.m_rawdata[op] = (byte)b;
                                    op++;
                                    n -= 128;
                                    continue;
                                    break;
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
                                state = LITERAL;
                            }
                            stop = true;
                            break;

                        case LITERAL:
                            /* last object was literal string */
                            if (n > 1)
                            {
                                state = LITERAL_RUN;
                                if (n > 128)
                                {
                                    m_tif.m_rawdata[op] = (byte)-127;
                                    op++;
                                    m_tif.m_rawdata[op] = (byte)b;
                                    op++;
                                    n -= 128;
                                    continue;
                                    break;
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
                                    state = BASE;

                                m_tif.m_rawdata[op] = (byte)b;
                                op++;
                            }
                            stop = true;
                            break;

                        case RUN:
                            /* last object was run */
                            if (n > 1)
                            {
                                if (n > 128)
                                {
                                    m_tif.m_rawdata[op] = (byte)-127;
                                    op++;
                                    m_tif.m_rawdata[op] = (byte)b;
                                    op++;
                                    n -= 128;
                                    continue;
                                    break;
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
                                state = LITERAL;
                            }
                            stop = true;
                            break;

                        case LITERAL_RUN:
                            /* literal followed by a run */
                            /*
                             * Check to see if previous run should
                             * be converted to a literal, in which
                             * case we convert literal-run-literal
                             * to a single literal.
                             */
                            if (n == 1 && m_tif.m_rawdata[op - 2] == (byte)-1 && m_tif.m_rawdata[lastliteral] < 126)
                            {
                                m_tif.m_rawdata[lastliteral] += 2;
                                state = (m_tif.m_rawdata[lastliteral] == 127 ? BASE : LITERAL);
                                m_tif.m_rawdata[op - 2] = m_tif.m_rawdata[op - 1]; /* replicate */
                            }
                            else
                                state = RUN;
                            continue;
                            break;
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
        private bool PackBitsEncodeChunk(byte[] bp, int cc, UInt16 s)
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

        private bool PackBitsDecode(byte[] op, int occ, UInt16 s)
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
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "PackBitsDecode: discarding %ld bytes ""to avoid buffer overrun", n - occ);
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
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "PackBitsDecode: discarding %ld bytes ""to avoid buffer overrun", n - occ + 1);
                        n = occ - 1;
                    }
                    memcpy(&op[opPos], m_tif.m_rawdata + bp, ++n);
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
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "PackBitsDecode: Not enough data for scanline %ld", m_tif.m_row);
                return false;
            }

            return true;
        }
    }
}
