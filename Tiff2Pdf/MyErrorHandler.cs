/* Copyright (C) 2008-2010, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

using System;
using System.Collections.Generic;
using System.Text;
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
