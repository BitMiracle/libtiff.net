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

namespace BitMiracle.LibTiff
{

    /// <summary>
    /// Flags to pass to PrintDirectory to control printing of data structures 
    /// that are potentially very large. Bit-or these flags to enable printing
    /// multiple items.
    /// </summary>
    [Flags]
#if EXPOSE_LIBTIFF
    public
#endif
    enum TiffPrintDirectoryFlags
    {
        TIFFPRINT_NONE = 0x0,  /* no extra info */
        TIFFPRINT_STRIPS = 0x1,  /* strips/tiles info */
        TIFFPRINT_CURVES = 0x2,  /* color/gray response curves */
        TIFFPRINT_COLORMAP = 0x4,  /* colormap */
        TIFFPRINT_JPEGQTABLES = 0x100,  /* JPEG Q matrices */
        TIFFPRINT_JPEGACTABLES = 0x200,  /* JPEG AC tables */
        TIFFPRINT_JPEGDCTABLES = 0x200,  /* JPEG DC tables */
    }
}
