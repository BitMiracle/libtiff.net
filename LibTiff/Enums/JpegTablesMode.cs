using System;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Jpeg Tables Mode.<br/>
    /// Possible values for <see cref="TiffTag"/>.JPEGTABLESMODE tag.
    /// </summary>
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
