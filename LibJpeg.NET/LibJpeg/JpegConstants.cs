/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.NET
{
    public static class JpegConstants
    {
        //////////////////////////////////////////////////////////////////////////
        // All of these are specified by the JPEG standard, so don't change them
        // if you want to be compatible.
        //

        // The basic DCT block is 8x8 samples
        public const int DCTSIZE = 8;

        // DCTSIZE squared; # of elements in a block
        public const int DCTSIZE2 = 64;

        // Quantization tables are numbered 0..3
        public const int NUM_QUANT_TBLS = 4;

        // Huffman tables are numbered 0..3
        public const int NUM_HUFF_TBLS = 4;

        // JPEG limit on # of components in one scan
        public const int MAX_COMPS_IN_SCAN = 4;

        // compressor's limit on blocks per MCU
        //
        // Unfortunately, some bozo at Adobe saw no reason to be bound by the standard;
        // the PostScript DCT filter can emit files with many more than 10 blocks/MCU.
        // If you happen to run across such a file, you can up D_MAX_BLOCKS_IN_MCU
        // to handle it.  We even let you do this from the jconfig.h file. However,
        // we strongly discourage changing C_MAX_BLOCKS_IN_MCU; just because Adobe
        // sometimes emits noncompliant files doesn't mean you should too.
        public const int C_MAX_BLOCKS_IN_MCU = 10;

        /* decompressor's limit on blocks per MCU */
        public const int D_MAX_BLOCKS_IN_MCU = 10;
        
        // JPEG limit on sampling factors
        public const int MAX_SAMP_FACTOR = 4;


        //////////////////////////////////////////////////////////////////////////
        // implementation-specific constants
        //

        // Maximum number of components (color channels) allowed in JPEG image.
        // To meet the letter of the JPEG spec, set this to 255.  However, darn
        // few applications need more than 4 channels (maybe 5 for CMYK + alpha
        // mask).  We recommend 10 as a reasonable compromise; use 4 if you are
        // really short on memory.  (Each allowed component costs a hundred or so
        // bytes of storage, whether actually used in an image or not.)
        public const int MAX_COMPONENTS = 10;

        // BITS_IN_JSAMPLE are either
        //      8   for 8-bit sample values (the usual setting)
        //      12  for 12-bit sample values (not supported by this version)
        //
        // Only 8 and 12 are legal data precisions for lossy JPEG according to the
        // JPEG standard.
        // Althought original IJG code claims it supports 12 bit images, our code
        // does not support anything except 8-bit images, sorry.
        public const int BITS_IN_JSAMPLE = 8;

        public const J_DCT_METHOD JDCT_DEFAULT = J_DCT_METHOD.JDCT_ISLOW;
        public const J_DCT_METHOD JDCT_FASTEST = J_DCT_METHOD.JDCT_IFAST;

        // a tad under 64K to prevent overflows
        public const int JPEG_MAX_DIMENSION = 65500;

        public const int MAXJSAMPLE = 255;
        public const int CENTERJSAMPLE = 128;

        // Ordering of RGB data in scanlines passed to or from the application.
        // RESTRICTIONS:
        // 1. These macros only affect RGB<=>YCbCr color conversion, so they are not
        // useful if you are using JPEG color spaces other than YCbCr or grayscale.
        // 2. The color quantizer modules will not behave desirably if RGB_PIXELSIZE
        // is not 3 (they don't understand about dummy color components!).  So you
        // can't use color quantization if you change that value.
        public const int RGB_RED = 0;   /* Offset of Red in an RGB scanline element */
        public const int RGB_GREEN = 1;   /* Offset of Green */
        public const int RGB_BLUE = 2;   /* Offset of Blue */
        public const int RGB_PIXELSIZE = 3;   /* JSAMPLEs per RGB scanline element */

    }
}
