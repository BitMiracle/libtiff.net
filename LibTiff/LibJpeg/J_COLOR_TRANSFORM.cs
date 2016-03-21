namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// Supported color transforms.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum J_COLOR_TRANSFORM
    {
        /// <summary>
        /// No transform
        /// </summary>
        JCT_NONE = 0,

        /// <summary>
        /// Substract green
        /// </summary>
        JCT_SUBTRACT_GREEN = 1
    }
}
