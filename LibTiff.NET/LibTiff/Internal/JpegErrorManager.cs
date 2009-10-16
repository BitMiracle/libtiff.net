using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// libjpeg interface layer.
    /// 
    /// We handle fatal errors when they are encountered within the JPEG
    /// library.  We also direct libjpeg error and warning
    /// messages through the appropriate libtiff handlers.
    /// </summary>
    class JpegErrorManager : jpeg_error_mgr
    {
        private Tiff m_tif;

        public JpegErrorManager(Tiff tif)
            : base()
        {
            m_tif = tif;
        }

        /*
        * Error handling routines (these replace corresponding
        * IJG routines).  These are used for both
        * compression and decompression.
        */
        public override void error_exit()
        {
            char buffer[JMSG_LENGTH_MAX];

            cinfo->m_err->format_message(buffer);
            Tiff::ErrorExt(m_tif, m_tif->m_clientdata, "JPEGLib", buffer); /* display the error message */
            cinfo->jpeg_abort(); /* clean up libjpeg state */

            throw Exception(buffer);
        }

        /*
        * This routine is invoked only for warning messages,
        * since error_exit does its own thing and trace_level
        * is never set > 0.
        */
        public override void output_message()
        {
            char buffer[JMSG_LENGTH_MAX];

            format_message(buffer);
            Tiff::WarningExt(m_tif, m_tif->m_clientdata, "JPEGLib", buffer);
        }
    }
}
