/*
 * This file contains tables and miscellaneous utility routines needed
 * for both compression and decompression.
 * Note we prefix all global names with "j" to minimize conflicts with
 * a surrounding application.
 */

using System;

namespace BitMiracle.LibJpeg.Classic.Internal
{
    class JpegUtils
    {
        /*
        * jpeg_natural_order[i] is the natural-order position of the i'th element
        * of zigzag order.
        *
        * When reading corrupted data, the Huffman decoders could attempt
        * to reference an entry beyond the end of this array (if the decoded
        * zero run length reaches past the end of the block).  To prevent
        * wild stores without adding an inner-loop test, we put some extra
        * "63"s after the real entries.  This will cause the extra coefficient
        * to be stored in location 63 of the block, not somewhere random.
        * The worst case would be a run-length of 15, which means we need 16
        * fake entries.
        */
        public static readonly int[] jpeg_natural_order = 
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63,
            63, 63, 63, 63, 63, 63, 63, 63, 
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// zz to natural order for 7x7 block
        public static readonly int[] jpeg_natural_order7 =
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6, 14, 21, 28, 35,
            42, 49, 50, 43, 36, 29, 22, 30,
            37, 44, 51, 52, 45, 38, 46, 53,
            54,
            63, 63, 63, 63, 63, 63, 63, 63,
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// zz to natural order for 6x6 block
        public static readonly int[] jpeg_natural_order6 =
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 41, 34, 27,
            20, 13, 21, 28, 35, 42, 43, 36,
            29, 37, 44, 45,
            63, 63, 63, 63, 63, 63, 63, 63,
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// zz to natural order for 5x5 block
        public static readonly int[] jpeg_natural_order5 =
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4, 12,
            19, 26, 33, 34, 27, 20, 28, 35,
            36,
            63, 63, 63, 63, 63, 63, 63, 63,
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// zz to natural order for 4x4 block
        public static readonly int[] jpeg_natural_order4 =
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 25, 18, 11, 19, 26, 27,
            63, 63, 63, 63, 63, 63, 63, 63,
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// zz to natural order for 3x3 block
        public static readonly int[] jpeg_natural_order3 =
        {
             0,  1,  8, 16,  9,  2, 10, 17,
            18,
            63, 63, 63, 63, 63, 63, 63, 63,
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// zz to natural order for 2x2 block
        public static readonly int[] jpeg_natural_order2 =
        {
             0,  1,  8,  9,
            63, 63, 63, 63, 63, 63, 63, 63,
            /* extra entries for safety in decoder */
            63, 63, 63, 63, 63, 63, 63, 63
        };

        /// Arithmetic coding probability estimation tables
        public static readonly int[] jpeg_aritab =
        {
        };

        /* Descale and correctly round an int value that's scaled by N bits.
        * We assume right shift rounds towards minus infinity, so adding
        * the fudge factor is correct for either sign of X.
        */
        public static int DESCALE(int x, int n)
        {
            return (x + (1 << (n - 1))) >> n;
        }

        //////////////////////////////////////////////////////////////////////////
        // Arithmetic utilities

        /// <summary>
        /// Compute a/b rounded up to next integer, ie, ceil(a/b)
        /// Assumes a >= 0, b > 0
        /// </summary>
        public static long jdiv_round_up(long a, long b)
        {
            return (a + b - 1L) / b;
        }

        /// <summary>
        /// Compute a rounded up to next multiple of b, ie, ceil(a/b)*b
        /// Assumes a >= 0, b > 0
        /// </summary>
        public static int jround_up(int a, int b)
        {
            a += b - 1;
            return a - (a % b);
        }

        /// <summary>
        /// Copy some rows of samples from one place to another.
        /// num_rows rows are copied from input_array[source_row++]
        /// to output_array[dest_row++]; these areas may overlap for duplication.
        /// The source and destination arrays must be at least as wide as num_cols.
        /// </summary>
        public static void jcopy_sample_rows(ComponentBuffer input_array, int source_row, byte[][] output_array, int dest_row, int num_rows, int num_cols)
        {
            for (int row = 0; row < num_rows; row++)
                Buffer.BlockCopy(input_array[source_row + row], 0, output_array[dest_row + row], 0, num_cols);
        }

        public static void jcopy_sample_rows(ComponentBuffer input_array, int source_row, ComponentBuffer output_array, int dest_row, int num_rows, int num_cols)
        {
            for (int row = 0; row < num_rows; row++)
                Buffer.BlockCopy(input_array[source_row + row], 0, output_array[dest_row + row], 0, num_cols);
        }

        public static void jcopy_sample_rows(byte[][] input_array, int source_row, byte[][] output_array, int dest_row, int num_rows, int num_cols)
        {
            for (int row = 0; row < num_rows; row++)
                Buffer.BlockCopy(input_array[source_row++], 0, output_array[dest_row++], 0, num_cols);
        }
    }
}
