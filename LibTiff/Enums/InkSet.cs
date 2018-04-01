namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Inks in separated image.<br/>
    /// Possible values for <see cref="TiffTag"/>.INKSET tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum InkSet
    {
        /// <summary>
        /// Cyan-magenta-yellow-black color.
        /// </summary>
        CMYK = 1,

        /// <summary>
        /// Multi-ink or hi-fi color.
        /// </summary>
        MULTIINK = 2,
    }
}
