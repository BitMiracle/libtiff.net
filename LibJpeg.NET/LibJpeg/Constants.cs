using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.NET
{
    static class Constants
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
    }
}
