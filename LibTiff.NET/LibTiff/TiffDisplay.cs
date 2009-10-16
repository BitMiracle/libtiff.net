using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff
{
    /// <summary>
    /// Structure for holding information about a display device.
    /// </summary>
    class TiffDisplay
    {
        /* XYZ -> luminance matrix */
        public float[][] d_mat = new float[][] 
        { 
            new float[3], new float[3], new float[3] 
        };

        public float d_YCR; /* Light o/p for reference white */
        public float d_YCG;
        public float d_YCB;
        public uint d_Vrwr; /* Pixel values for ref. white */
        public uint d_Vrwg;
        public uint d_Vrwb;
        public float d_Y0R; /* Residual light for black pixel */
        public float d_Y0G;
        public float d_Y0B;
        public float d_gammaR; /* Gamma values for the three guns */
        public float d_gammaG;
        public float d_gammaB;
    }
}
