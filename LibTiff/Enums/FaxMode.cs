using System;
using System.Diagnostics.CodeAnalysis;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Group 3/4 format control.<br/>
    /// Possible values for <see cref="TiffTag"/>.FAXMODE tag.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
    [Flags]
#if EXPOSE_LIBTIFF
    public
#endif
    enum FaxMode
    {
        /// <summary>
        /// Default, include RTC.
        /// </summary>
        CLASSIC = 0x0000,

        /// <summary>
        /// No RTC at end of data.
        /// </summary>
        NORTC = 0x0001,

        /// <summary>
        /// No EOL code at end of row.
        /// </summary>
        NOEOL = 0x0002,

        /// <summary>
        /// Byte align row.
        /// </summary>
        BYTEALIGN = 0x0004,

        /// <summary>
        /// Word align row.
        /// </summary>
        WORDALIGN = 0x0008,

        /// <summary>
        /// TIFF Class F.
        /// </summary>
        CLASSF = NORTC,
    }
}
