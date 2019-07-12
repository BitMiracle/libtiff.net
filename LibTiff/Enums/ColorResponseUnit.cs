namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Color curve accuracy.<br/>
    /// Possible values for <see cref="TiffTag"/>.COLORRESPONSEUNIT tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum ColorResponseUnit
    {
        /// <summary>
        /// Tenths of a unit.
        /// </summary>
        CRU10S = 1,

        /// <summary>
        /// Hundredths of a unit.
        /// </summary>
        CRU100S = 2,

        /// <summary>
        /// Thousandths of a unit.
        /// </summary>
        CRU1000S = 3,

        /// <summary>
        /// Ten-thousandths of a unit.
        /// </summary>
        CRU10000S = 4,

        /// <summary>
        /// Hundred-thousandths.
        /// </summary>
        CRU100000S = 5,
    }
}
