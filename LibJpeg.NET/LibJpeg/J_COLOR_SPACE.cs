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
    /// Known color spaces.
    /// </summary>
    public enum J_COLOR_SPACE
    {
        JCS_UNKNOWN,
        /* error/unspecified */
        JCS_GRAYSCALE,
        /* monochrome */
        JCS_RGB,
        /* red/green/blue */
        JCS_YCbCr,
        /* Y/Cb/Cr (also known as YUV) */
        JCS_CMYK,
        /* C/M/Y/K */
        JCS_YCCK        /* Y/Cb/Cr/K */
    }
}
