/* Copyright (C) 2008-2011, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Classic.Internal
{
    /// <summary>
    /// JPEG library source data manager.
    /// These routines supply compressed data to LibJpeg.Net
    /// </summary>
    class JpegStdSource : jpeg_source_mgr
    {
        static byte[] dummy_EOI = { 0xFF, (byte)JPEG_MARKER.EOI };
        protected JpegCodec m_sp;

        public JpegStdSource(JpegCodec sp)
        {
            initInternalBuffer(null, 0);
            m_sp = sp;
        }

        public override void init_source()
        {
            Tiff tif = m_sp.GetTiff();
            initInternalBuffer(tif.m_rawdata, tif.m_rawcc);
        }

        public override bool fill_input_buffer()
        {
            /*
            * Should never get here since entire strip/tile is
            * read into memory before the decompressor is called,
            * and thus was supplied by init_source.
            */
            m_sp.m_decompression.WARNMS(J_MESSAGE_CODE.JWRN_JPEG_EOF);

            /* insert a fake EOI marker */
            initInternalBuffer(dummy_EOI, 2);
            return true;
        }
    }
}
