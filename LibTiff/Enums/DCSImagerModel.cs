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
    /// DCSImagerModel
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum DCSImagerModel
    {
        /// <summary>
        /// M3 chip (1280 x 1024)
        /// </summary>
        M3 = 0,
        /// <summary>
        /// M5 chip (1536 x 1024)
        /// </summary>
        M5 = 1,
        /// <summary>
        /// M6 chip (3072 x 2048)
        /// </summary>
        M6 = 2,
    }
}
