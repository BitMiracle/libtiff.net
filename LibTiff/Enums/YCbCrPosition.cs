namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Subsample positioning.<br/>
    /// Possible values for <see cref="TiffTag"/>.YCBCRPOSITIONING tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum YCbCrPosition
    {
        /// <summary>
        /// As in PostScript Level 2
        /// </summary>
        CENTERED = 1,

        /// <summary>
        /// As in CCIR 601-1
        /// </summary>
        COSITED = 2,
    }
}
