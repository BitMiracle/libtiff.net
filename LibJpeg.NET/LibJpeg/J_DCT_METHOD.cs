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
    /// DCT/IDCT algorithm options.
    /// </summary>
    public enum J_DCT_METHOD
    {
        JDCT_ISLOW,
        /* slow but accurate integer algorithm */
        JDCT_IFAST,
        /* faster, less accurate integer method */
        JDCT_FLOAT      /* floating-point: accurate, fast on fast HW */
    }
}
