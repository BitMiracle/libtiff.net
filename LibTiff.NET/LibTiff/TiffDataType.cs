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
    /// Tag data type information.
    /// Note: RATIONALs are the ratio of two 32-bit integer values.
    /// </summary>
    public enum TiffDataType
    {
        TIFF_NOTYPE = 0, /* placeholder */
        TIFF_BYTE = 1, /* 8-bit unsigned integer */
        TIFF_ASCII = 2, /* 8-bit bytes w/ last byte null */
        TIFF_SHORT = 3, /* 16-bit unsigned integer */
        TIFF_LONG = 4, /* 32-bit unsigned integer */
        TIFF_RATIONAL = 5, /* 64-bit unsigned fraction */
        TIFF_SBYTE = 6, /* !8-bit signed integer */
        TIFF_UNDEFINED = 7, /* !8-bit untyped data */
        TIFF_SSHORT = 8, /* !16-bit signed integer */
        TIFF_SLONG = 9, /* !32-bit signed integer */
        TIFF_SRATIONAL = 10, /* !64-bit signed fraction */
        TIFF_FLOAT = 11, /* !32-bit IEEE floating point */
        TIFF_DOUBLE = 12, /* !64-bit IEEE floating point */
        TIFF_IFD = 13 /* %32-bit unsigned integer (offset) */
    }
}
