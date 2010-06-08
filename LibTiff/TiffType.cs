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

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Tag data type information.
    /// Note: RATIONALs are the ratio of two 32-bit integer values.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum TiffType : short
    {
        NOTYPE = 0,     /* placeholder */
        ANY = NOTYPE,   /* for field descriptor searching */
        BYTE = 1,       /* 8-bit unsigned integer */
        ASCII = 2,      /* 8-bit bytes w/ last byte null */
        SHORT = 3,      /* 16-bit unsigned integer */
        LONG = 4,       /* 32-bit unsigned integer */
        RATIONAL = 5,   /* 64-bit unsigned fraction */
        SBYTE = 6,      /* !8-bit signed integer */
        UNDEFINED = 7,  /* !8-bit untyped data */
        SSHORT = 8,     /* !16-bit signed integer */
        SLONG = 9,      /* !32-bit signed integer */
        SRATIONAL = 10, /* !64-bit signed fraction */
        FLOAT = 11,     /* !32-bit IEEE floating point */
        DOUBLE = 12,    /* !64-bit IEEE floating point */
        IFD = 13        /* %32-bit unsigned integer (offset) */
    }
}
