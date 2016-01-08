namespace BitMiracle.LibJpeg.Classic.Internal
{
    /// <summary>
    /// The script for encoding a multiple-scan file is an array of these:
    /// </summary>
    class jpeg_scan_info
    {
        public int comps_in_scan;      /* number of components encoded in this scan */
        public int[] component_index = new int[JpegConstants.MAX_COMPS_IN_SCAN]; /* their SOF/comp_info[] indexes */
        public int Ss;
        public int Se;         /* progressive JPEG spectral selection parms */
        public int Ah;
        public int Al;         /* progressive JPEG successive approx. parms */
    }
}
