using System;
using System.Diagnostics.CodeAnalysis;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Options for CCITT Group 3/4 fax encoding.<br/>
    /// Possible values for <see cref="TiffTag"/>.GROUP3OPTIONS / TiffTag.T4OPTIONS and
    /// TiffTag.GROUP4OPTIONS / TiffTag.T6OPTIONS tags.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
    [Flags]
#if EXPOSE_LIBTIFF
    public
#endif
    enum Group3Opt
    {
        /// <summary>
        /// Unknown (uninitialized).
        /// </summary>
        UNKNOWN = -1,

        /// <summary>
        /// 2-dimensional coding.
        /// </summary>
        ENCODING2D = 0x1,

        /// <summary>
        /// Data not compressed.
        /// </summary>
        UNCOMPRESSED = 0x2,

        /// <summary>
        /// Fill to byte boundary.
        /// </summary>
        FILLBITS = 0x4,
    }
}
