namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Thresholding used on data.<br/>
    /// Possible values for <see cref="TiffTag"/>.THRESHHOLDING tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Threshold
    {
        /// <summary>
        /// B&amp;W art scan.
        /// </summary>
        BILEVEL = 1,

        /// <summary>
        /// Dithered scan.
        /// </summary>
        HALFTONE = 2,

        /// <summary>
        /// Usually Floyd-Steinberg.
        /// </summary>
        ERRORDIFFUSE = 3,
    }
}
