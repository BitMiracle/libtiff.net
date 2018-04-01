namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Information about extra samples.<br/>
    /// Possible values for <see cref="TiffTag"/>.EXTRASAMPLES tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum ExtraSample
    {
        /// <summary>
        /// Unspecified data.
        /// </summary>
        UNSPECIFIED = 0,

        /// <summary>
        /// Associated alpha data.
        /// </summary>
        ASSOCALPHA = 1,

        /// <summary>
        /// Unassociated alpha data.
        /// </summary>
        UNASSALPHA = 2,
    }
}
