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
    /// SGILogEncode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum SGILogEncode
    {
        /// <summary>
        /// Do not dither encoded values
        /// </summary>
        NODITHER = 0,
        /// <summary>
        /// Randomly dither encd values
        /// </summary>
        RANDITHER = 1,
    }
}
