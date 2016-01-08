namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// One block of coefficients.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class JBLOCK
    {
        internal short[] data = new short[JpegConstants.DCTSIZE2];

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index of required element.</param>
        /// <value>The required element.</value>
        public short this[int index]
        {
            get
            {
                return data[index];
            }
            set
            {
                data[index] = value;
            }
        }
    }
}
