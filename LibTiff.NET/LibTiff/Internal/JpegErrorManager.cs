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
        private JpegCodec m_sp;

        public JpegErrorManager(JpegCodec sp)
            : base()
        {
            m_sp = sp;
        }

        /*
        * Error handling routines (these replace corresponding
        * IJG routines).  These are used for both
        * compression and decompression.
        */
        public override void error_exit()
        {
            string buffer = m_sp.m_err.format_message();
            Tiff.ErrorExt(m_sp.GetTiff(), m_sp.GetTiff().m_clientdata, "JPEGLib", buffer); /* display the error message */
            m_sp.m_common.jpeg_abort(); /* clean up libjpeg state */

            throw new Exception(buffer);
        }

        /*
        * This routine is invoked only for warning messages,
        * since error_exit does its own thing and trace_level
        * is never set > 0.
        */
        public override void output_message()
        {
            string buffer = format_message();
            Tiff.WarningExt(m_sp.GetTiff(), m_sp.GetTiff().m_clientdata, "JPEGLib", buffer);
        }
    }
}
