using System;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Flags that can be passed to <see cref="O:BitMiracle.LibTiff.Classic.Tiff.PrintDirectory"/>
    /// method to control printing of data structures that are potentially very large.
    /// </summary>
    /// <remarks>More than one flag can be used. Bit-or these flags to enable printing
    /// multiple items.</remarks>
    [Flags]
#if EXPOSE_LIBTIFF
    public
#endif
    enum TiffPrintFlags
    {
        /// <summary>
        /// no extra info
        /// </summary>
        NONE = 0x0,

        /// <summary>
        /// strips/tiles info
        /// </summary>
        STRIPS = 0x1,

        /// <summary>
        /// color/gray response curves
        /// </summary>
        CURVES = 0x2,

        /// <summary>
        /// colormap
        /// </summary>
        COLORMAP = 0x4,

        /// <summary>
        /// JPEG Q matrices
        /// </summary>
        JPEGQTABLES = 0x100,

        /// <summary>
        /// JPEG AC tables
        /// </summary>
        JPEGACTABLES = 0x200,

        /// <summary>
        /// JPEG DC tables
        /// </summary>
        JPEGDCTABLES = 0x200,
    }
}
