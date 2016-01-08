using System;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.Tiff2Pdf
{
    class MyErrorHandler : TiffErrorHandler
    {
        public override void ErrorHandler(Tiff tif, string module, string fmt, params object[] ap)
        {
            using (TextWriter stdout = Console.Out)
            {
                if (module != null)
                    stdout.Write("{0}: ", module);

                stdout.Write(fmt, ap);
                stdout.Write(".\n");
            }
        }

        public override void WarningHandler(Tiff tif, string module, string fmt, params object[] ap)
        {
            using (TextWriter stdout = Console.Out)
            {
                if (module != null)
                    stdout.Write("{0}: ", module);

                stdout.Write("Warning, ");
                stdout.Write(fmt, ap);
                stdout.Write(".\n");
            }
        }
    }
}
