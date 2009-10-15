/* Copyright (C) 2008-2009, Bit Miracle
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

using thandle_t = System.Object;

namespace BitMiracle.LibTiff
{
    public class TiffErrorHandler
    {
        public virtual void ErrorHandler(Tiff tif, string module, string fmt, params object[] ap)
        {
            if (module != NULL)
                fprintf(stderr, "%s: ", module);

            vfprintf(stderr, fmt, ap);
            fprintf(stderr, ".\n");
        }

        public virtual void ErrorHandlerExt(Tiff tif, thandle_t fd, string module, string fmt, params object[] ap)
        {
        }

        public virtual void WarningHandler(Tiff tif, string module, string fmt, params object[] ap)
        {
            if (module != NULL)
                fprintf(stderr, "%s: ", module);

            fprintf(stderr, "Warning, ");
            vfprintf(stderr, fmt, ap);
            fprintf(stderr, ".\n");
        }

        public virtual void WarningHandlerExt(Tiff tif, thandle_t fd, string module, string fmt, params object[] ap)
        {
        }
    }
}
