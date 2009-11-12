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
 * "Null" Compression Algorithm Support.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace BitMiracle.LibTiff.Internal
{
    class DumpModeCodec : TiffCodec
    {
        public DumpModeCodec(Tiff tif, Compression scheme, string name)
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
            return DumpModeDecode(pp, cc, s);
        }

        public override bool tif_decodestrip(byte[] pp, int cc, short s)
        {
            return DumpModeDecode(pp, cc, s);
        }

        public override bool tif_decodetile(byte[] pp, int cc, short s)
        {
            return DumpModeDecode(pp, cc, s);
        }

        public override bool tif_encoderow(byte[] pp, int cc, short s)
        {
            return DumpModeEncode(pp, cc, s);
        }

        public override bool tif_encodestrip(byte[] pp, int cc, short s)
        {
            return DumpModeEncode(pp, cc, s);
        }

        public override bool tif_encodetile(byte[] pp, int cc, short s)
        {
            return DumpModeEncode(pp, cc, s);
        }

        public override bool tif_seek(int off)
        {
            m_tif.m_rawcp += off * m_tif.m_scanlinesize;
            m_tif.m_rawcc -= off * m_tif.m_scanlinesize;
            return true;
        }
        
        /*
        * Encode a hunk of pixels.
        */
        private bool DumpModeEncode(byte[] pp, int cc, short s)
        {
            int ppPos = 0;
            while (cc > 0)
            {
                int n;

                n = cc;
                if (m_tif.m_rawcc + n > m_tif.m_rawdatasize)
                    n = m_tif.m_rawdatasize - m_tif.m_rawcc;

                Debug.Assert(n > 0);

                Array.Copy(pp, ppPos, m_tif.m_rawdata, m_tif.m_rawcp, n);
                m_tif.m_rawcp += n;
                m_tif.m_rawcc += n;

                ppPos += n;
                cc -= n;
                if (m_tif.m_rawcc >= m_tif.m_rawdatasize && !m_tif.flushData1())
                    return false;
            }

            return true;
        }

        /*
        * Decode a hunk of pixels.
        */
        private bool DumpModeDecode(byte[] buf, int cc, short s)
        {
            if (m_tif.m_rawcc < cc)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                    "DumpModeDecode: Not enough data for scanline {0}", m_tif.m_row);
                return false;
            }

            Array.Copy(m_tif.m_rawdata, m_tif.m_rawcp, buf, 0, cc);
            m_tif.m_rawcp += cc;
            m_tif.m_rawcc -= cc;
            return true;
        }
    }
}
