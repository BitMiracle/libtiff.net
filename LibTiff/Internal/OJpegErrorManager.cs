using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Classic.Internal
{
    class OJpegErrorManager : jpeg_error_mgr
    {
        private OJpegCodec m_sp;

        public OJpegErrorManager(OJpegCodec sp)
            : base()
        {
            m_sp = sp;
        }

        /* Error handling routines (these replace corresponding IJG routines).
         * These are used for both compression and decompression.
         */
        public override void error_exit()
        {
            string buffer = format_message();
            Tiff.ErrorExt(m_sp.GetTiff(), m_sp.GetTiff().m_clientdata, "LibJpeg", "{0}", buffer); /* display the error message */

            // clean up LibJpeg.Net state
            m_sp.m_libjpeg_jpeg_decompress_struct.jpeg_abort();

            throw new Exception(buffer);
        }

        /* This routine is invoked only for warning messages, since error_exit
         * does its own thing and trace_level is never set > 0.
         */
        public override void output_message()
        {
            string buffer = format_message();
            Tiff.WarningExt(m_sp.GetTiff(), m_sp.GetTiff().m_clientdata, "LibJpeg", "{0}", buffer);
        }
    }
}
