﻿namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Auto RGB&lt;=&gt;YCbCr convert.<br/>
    /// Possible values for <see cref="TiffTag"/>.JPEGCOLORMODE tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum JpegColorMode
    {
        /// <summary>
        /// No conversion (default).
        /// </summary>
        RAW = 0x0000,

        /// <summary>
        /// Do auto conversion.
        /// </summary>
        RGB = 0x0001,
    }
}
