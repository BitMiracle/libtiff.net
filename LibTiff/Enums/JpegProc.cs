namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// JPEG processing algorithm.<br/>
    /// Possible values for <see cref="TiffTag"/>.JPEGPROC tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum JpegProc
    {
        /// <summary>
        /// Baseline sequential.
        /// </summary>
        BASELINE = 1,

        /// <summary>
        /// Huffman coded lossless.
        /// </summary>
        LOSSLESS = 14,
    }
}
