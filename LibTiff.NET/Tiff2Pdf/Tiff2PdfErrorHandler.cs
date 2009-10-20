using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff;

namespace BitMiracle.Tiff2Pdf
{
    class Tiff2PdfErrorHandler : TiffErrorHandler
    {
        public override void ErrorHandler(Tiff tif, string module, string fmt, params object[] ap)
        {
            Stream stdout = Console.OpenStandardOutput();
            if (module != null)
                Tiff.fprintf(stdout, "%s: ", module);

            Tiff.fprintf(stdout, fmt, ap);
            Tiff.fprintf(stdout, ".\n");
            stdout.Dispose();
        }

        public override void WarningHandler(Tiff tif, string module, string fmt, params object[] ap)
        {
            Stream stdout = Console.OpenStandardOutput();
            if (module != null)
                Tiff.fprintf(stdout, "%s: ", module);

            Tiff.fprintf(stdout, "Warning, ");
            Tiff.fprintf(stdout, fmt, ap);
            Tiff.fprintf(stdout, ".\n");
            stdout.Dispose();
        }
    }
}
