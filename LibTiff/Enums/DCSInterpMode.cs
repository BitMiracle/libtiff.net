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


namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// DCSInterpMode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum DCSInterpMode
    {
        /// <summary>
        /// Whole image, default
        /// </summary>
        NORMAL = 0x0,
        /// <summary>
        /// Preview of image (384x256)
        /// </summary>
        PREVIEW = 0x1,
    }
}
