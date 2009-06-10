/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.Classic
{
    /// <summary>
    /// Dithering options for decompression.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum J_DITHER_MODE
    {
        JDITHER_NONE,       /* no dithering */
        JDITHER_ORDERED,    /* simple ordered dither */
        JDITHER_FS          /* Floyd-Steinberg error diffusion dither */
    }
}
