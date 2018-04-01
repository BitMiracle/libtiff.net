using System;
using System.Diagnostics.CodeAnalysis;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Subfile data descriptor.<br/>
    /// Possible values for <see cref="TiffTag"/>.SUBFILETYPE tag.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
    [Flags]
#if EXPOSE_LIBTIFF
    public
#endif
    enum FileType
    {
        /// <summary>
        /// Reduced resolution version.
        /// </summary>
        REDUCEDIMAGE = 0x1,

        /// <summary>
        /// One page of many.
        /// </summary>
        PAGE = 0x2,

        /// <summary>
        /// Transparency mask.
        /// </summary>
        MASK = 0x4
    }
}
