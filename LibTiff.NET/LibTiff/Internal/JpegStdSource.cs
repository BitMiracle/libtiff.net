using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// JPEG library source data manager.
    /// These routines supply compressed data to libjpeg.
    /// </summary>
    class JpegStdSource : jpeg_source_mgr
    {
        protected JpegCodec m_sp;

        public JpegStdSource(JpegCodec sp)
        {
            initInternalBuffer(NULL, 0);
            m_sp = sp;
        }

        public override void init_source()
        {
            Tiff* tif = m_sp->GetTiff();
            initInternalBuffer(tif->m_rawdata, tif->m_rawcc);
        }

        public override bool fill_input_buffer()
        {
            static const JOCTET dummy_EOI[2] = { 0xFF, M_EOI };

            /*
            * Should never get here since entire strip/tile is
            * read into memory before the decompressor is called,
            * and thus was supplied by init_source.
            */
            m_sp->m_decompression->WARNMS(JWRN_JPEG_EOF);

            /* insert a fake EOI marker */
            initInternalBuffer((JOCTET*)dummy_EOI, 2);
            return true;
        }
    }
}
