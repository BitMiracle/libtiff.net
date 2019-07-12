namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Data order within a byte.<br/>
    /// Possible values for <see cref="TiffTag"/>.FILLORDER tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum FillOrder
    {
        /// <summary>
        /// Most significant -> least.
        /// </summary>
        MSB2LSB = 1,

        /// <summary>
        /// Least significant -> most.
        /// </summary>
        LSB2MSB = 2,
    }
}
