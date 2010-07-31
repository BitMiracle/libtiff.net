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
    /// InkSet
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum InkSet
    {
        /// <summary>
        /// Cyan-magenta-yellow-black color
        /// </summary>
        CMYK = 1,// !
        /// <summary>
        /// Multi-ink or hi-fi color
        /// </summary>
        MULTIINK = 2,// !
    }
}
