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
    /// SGILogDataFmt
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum SGILogDataFmt
    {
        /// <summary>
        /// IEEE float samples
        /// </summary>
        FMTFLOAT = 0,
        /// <summary>
        /// 16-bit samples
        /// </summary>
        FMT16BIT = 1,
        /// <summary>
        /// Uninterpreted data
        /// </summary>
        FMTRAW = 2,
        /// <summary>
        /// 8-bit RGB monitor values
        /// </summary>
        FMT8BIT = 3,
    }
}
