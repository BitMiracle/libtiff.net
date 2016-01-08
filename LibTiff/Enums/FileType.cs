using System;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Subfile data descriptor.<br/>
    /// Possible values for <see cref="TiffTag"/>.SUBFILETYPE tag.
    /// </summary>
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
