namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Storage organization.<br/>
    /// Possible values for <see cref="TiffTag"/>.PLANARCONFIG tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum PlanarConfig
    {
        /// <summary>
        /// Unknown (uninitialized).
        /// </summary>
        UNKNOWN = 0,

        /// <summary>
        /// Single image plane.
        /// </summary>
        CONTIG = 1,

        /// <summary>
        /// Separate planes of data.
        /// </summary>
        SEPARATE = 2
    }
}
