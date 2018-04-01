using System;
using System.Diagnostics.CodeAnalysis;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Jpeg Tables Mode.<br/>
    /// Possible values for <see cref="TiffTag"/>.JPEGTABLESMODE tag.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
    [Flags]
#if EXPOSE_LIBTIFF
    public
#endif
    enum JpegTablesMode
    {
        /// <summary>
        /// None.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Include quantization tables.
        /// </summary>
        QUANT = 0x0001,

        /// <summary>
        /// Include Huffman tables.
        /// </summary>
        HUFF = 0x0002,
    }
}
