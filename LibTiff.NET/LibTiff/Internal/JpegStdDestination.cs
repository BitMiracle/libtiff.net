using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// JPEG library destination data manager.
    /// These routines direct compressed data from libjpeg into the
    /// libtiff output buffer.
    /// </summary>
    class JpegStdDestination : jpeg_destination_mgr
    {
        private Tiff m_tif;

        public JpegStdDestination(Tiff tif)
        {
            m_tif = tif;
        }

        public override void init_destination()
        {
            initInternalBuffer(m_tif->m_rawdata, m_tif->m_rawdatasize);
        }

        public override bool empty_output_buffer()
        {
            /* the entire buffer has been filled */
            m_tif->m_rawcc = m_tif->m_rawdatasize;
            m_tif->flushData1();

            initInternalBuffer(m_tif->m_rawdata, m_tif->m_rawdatasize);
            return true;
        }

        public override void term_destination()
        {
            m_tif->m_rawcp = m_tif->m_rawdatasize - freeInBuffer();
            m_tif->m_rawcc = m_tif->m_rawdatasize - freeInBuffer();
            /* NB: libtiff does the final buffer flush */
        }
    }
}
