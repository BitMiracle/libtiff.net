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
    /// PixarLogDataFmt
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum PixarLogDataFmt
    {
        /// <summary>
        /// Regular u_char samples
        /// </summary>
        FMT8BIT = 0,
        /// <summary>
        /// ABGR-order u_chars
        /// </summary>
        FMT8BITABGR = 1,
        /// <summary>
        /// 11-bit log-encoded (raw)
        /// </summary>
        FMT11BITLOG = 2,
        /// <summary>
        /// As per PICIO (1.0==2048)
        /// </summary>
        FMT12BITPICIO = 3,
        /// <summary>
        /// Signed short samples
        /// </summary>
        FMT16BIT = 4,
        /// <summary>
        /// IEEE float samples
        /// </summary>
        FMTFLOAT = 5,
    }
}
