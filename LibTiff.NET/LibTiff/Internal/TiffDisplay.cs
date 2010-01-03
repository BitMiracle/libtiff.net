/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// Structure for holding information about a display device.
    /// </summary>
    class TiffDisplay
    {
        /* XYZ -> luminance matrix */
        internal float[][] d_mat;
        internal float d_YCR; /* Light o/p for reference white */
        internal float d_YCG;
        internal float d_YCB;
        internal int d_Vrwr; /* Pixel values for ref. white */
        internal int d_Vrwg;
        internal int d_Vrwb;
        internal float d_Y0R; /* Residual light for black pixel */
        internal float d_Y0G;
        internal float d_Y0B;
        internal float d_gammaR; /* Gamma values for the three guns */
        internal float d_gammaG;
        internal float d_gammaB;

        public TiffDisplay()
        {
            d_mat = null;
            d_YCR = 0;
            d_YCG = 0;
            d_YCB = 0;
            d_Vrwr = 0;
            d_Vrwg = 0;
            d_Vrwb = 0;
            d_Y0R = 0;
            d_Y0G = 0;
            d_Y0B = 0;
            d_gammaR = 0;
            d_gammaG = 0;
            d_gammaB = 0;
        }

        public TiffDisplay(float[] mat0, float[] mat1, float[] mat2,
            float YCR, float YCG, float YCB, int Vrwr, int Vrwg,
            int Vrwb, float Y0R, float Y0G, float Y0B,
            float gammaR, float gammaG, float gammaB)
        {
            d_mat = new float[3][] { mat0, mat1, mat2 };
            d_YCR = YCR;
            d_YCG = YCG;
            d_YCB = YCB;
            d_Vrwr = Vrwr;
            d_Vrwg = Vrwg;
            d_Vrwb = Vrwb;
            d_Y0R = Y0R;
            d_Y0G = Y0G;
            d_Y0B = Y0B;
            d_gammaR = gammaR;
            d_gammaG = gammaG;
            d_gammaB = gammaB;
        }
    }
}
