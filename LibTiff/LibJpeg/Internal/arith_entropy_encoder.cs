using System;

namespace BitMiracle.LibJpeg.Classic.Internal
{
    class arith_entropy_encoder : jpeg_entropy_encoder
    {
        public arith_entropy_encoder(jpeg_compress_struct cinfo)
        {
            cinfo.ERREXIT(J_MESSAGE_CODE.JERR_NOTIMPL);
        }

        public override void start_pass(bool gather_statistics)
        {
            throw new NotImplementedException();
        }
    }
}
