namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Image orientation.<br/>
    /// Possible values for <see cref="TiffTag"/>.ORIENTATION tag.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Orientation
    {
        /// <summary>
        /// Row 0 top, Column 0 lhs.
        /// </summary>
        TOPLEFT = 1,

        /// <summary>
        /// Row 0 top, Column 0 rhs.
        /// </summary>
        TOPRIGHT = 2,

        /// <summary>
        /// Row 0 bottom, Column 0 rhs.
        /// </summary>
        BOTRIGHT = 3,

        /// <summary>
        /// Row 0 bottom, Column 0 lhs.
        /// </summary>
        BOTLEFT = 4,

        /// <summary>
        /// Row 0 lhs, Column 0 top.
        /// </summary>
        LEFTTOP = 5,

        /// <summary>
        /// Row 0 rhs, Column 0 top.
        /// </summary>
        RIGHTTOP = 6,

        /// <summary>
        /// Row 0 rhs, Column 0 bottom.
        /// </summary>
        RIGHTBOT = 7,

        /// <summary>
        /// Row 0 lhs, Column 0 bottom.
        /// </summary>
        LEFTBOT = 8,
    }
}
