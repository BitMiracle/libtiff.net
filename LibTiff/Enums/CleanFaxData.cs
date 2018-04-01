namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Regenerated line info.<br/>
    /// Possible values for <see cref="TiffTag"/>.CLEANFAXDATA tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum CleanFaxData
    {
        /// <summary>
        /// No errors detected.
        /// </summary>
        CLEAN = 0,

        /// <summary>
        /// Receiver regenerated lines.
        /// </summary>
        REGENERATED = 1,

        /// <summary>
        /// Uncorrected errors exist.
        /// </summary>
        UNCLEAN = 2,
    }
}
