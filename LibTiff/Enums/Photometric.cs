﻿namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Photometric interpretation.<br/>
    /// Possible values for <see cref="TiffTag"/>.PHOTOMETRIC tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Photometric
    {
        /// <summary>
        /// Min value is white.
        /// </summary>
        MINISWHITE = 0,

        /// <summary>
        /// Min value is black.
        /// </summary>
        MINISBLACK = 1,

        /// <summary>
        /// RGB color model.
        /// </summary>
        RGB = 2,

        /// <summary>
        /// Color map indexed.
        /// </summary>
        PALETTE = 3,

        /// <summary>
        /// [obsoleted by TIFF rev. 6.0] Holdout mask.
        /// </summary>
        MASK = 4,

        /// <summary>
        /// Color separations.
        /// </summary>
        SEPARATED = 5,

        /// <summary>
        /// CCIR 601.
        /// </summary>
        YCBCR = 6,

        /// <summary>
        /// 1976 CIE L*a*b*.
        /// </summary>
        CIELAB = 8,

        /// <summary>
        /// ICC L*a*b*. Introduced post TIFF rev 6.0 by Adobe TIFF Technote 4.
        /// </summary>
        ICCLAB = 9,

        /// <summary>
        /// ITU L*a*b*.
        /// </summary>
        ITULAB = 10,

        /// <summary>
        /// CIE Log2(L).
        /// </summary>
        LOGL = 32844,

        /// <summary>
        /// CIE Log2(L) (u',v').
        /// </summary>
        LOGLUV = 32845,
    }
}
