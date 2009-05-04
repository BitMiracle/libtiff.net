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

namespace LibJpeg.NET
{
    /// <summary>
    /// The decompressor can save APPn and COM markers in a list of these:
    /// </summary>
    class jpeg_marker_struct
    {
        jpeg_marker_struct next;   /* next in list, or null */
        byte marker;           /* marker code: JPEG_COM, or JPEG_APP0+n */
        uint original_length;   /* # bytes of data in the file */
        uint data_length;   /* # bytes of data saved at data[] */
        byte[] data;       /* the data contained in the marker */
        /* the marker length word is not counted in data_length or original_length */
    }
}
