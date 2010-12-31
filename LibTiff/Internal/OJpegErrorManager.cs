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
            //static void
            //OJPEGLibjpegJpegErrorMgrErrorExit(jpeg_common_struct* cinfo)
            //{
            //    char buffer[JMSG_LENGTH_MAX];
            //    (*cinfo.err.format_message)(cinfo,buffer);
            //    TIFFErrorExt(((TIFF*)(cinfo.client_data)).tif_clientdata,"LibJpeg", "%s", buffer);
            //    jpeg_encap_unwind((TIFF*)(cinfo.client_data));
            //}
        }

        /* This routine is invoked only for warning messages, since error_exit
         * does its own thing and trace_level is never set > 0.
         */
        public override void output_message()
        {
            //static void
            //OJPEGLibjpegJpegErrorMgrOutputMessage(jpeg_common_struct* cinfo)
            //{
            //    char buffer[JMSG_LENGTH_MAX];
            //    (*cinfo.err.format_message)(cinfo,buffer);
            //    TIFFWarningExt(((TIFF*)(cinfo.client_data)).tif_clientdata,"LibJpeg", "%s", buffer);
            //}
        }
    }
}
