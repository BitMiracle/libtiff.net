/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

/*
 * This file contains the coefficient buffer controller for decompression.
 * This controller is the top level of the JPEG decompressor proper.
 * The coefficient buffer lies between entropy decoding and inverse-DCT steps.
 *
 * In buffered-image mode, this controller is the interface between
 * input-oriented processing and output-oriented processing.
 * Also, the input side (only) is used when reading a file for transcoding.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.NET
{
    /// <summary>
    /// Coefficient buffer control
    /// </summary>
    class jpeg_d_coef_controller
    {
        private const int SAVED_COEFS = 6; /* we save coef_bits[0..5] */

        private enum DecompressorType
        {
            Ordinary,
            Smooth,
            OnePass
        }

        private jpeg_decompress_struct m_cinfo;
        private bool m_useDummyConsumeData;
        private DecompressorType m_decompressor;

        /* These variables keep track of the current location of the input side. */
        /* cinfo->input_iMCU_row is also used for this. */
        private uint m_MCU_ctr;     /* counts MCUs processed in current row */
        private int m_MCU_vert_offset;        /* counts MCU rows within iMCU row */
        private int m_MCU_rows_per_iMCU_row;  /* number of such rows needed */

        /* The output side's location is represented by cinfo->output_iMCU_row. */

        /* In single-pass modes, it's sufficient to buffer just one MCU.
        * We allocate a workspace of D_MAX_BLOCKS_IN_MCU coefficient blocks,
        * and let the entropy decoder write into that workspace each time.
        * (On 80x86, the workspace is FAR even though it's not really very big;
        * this is to keep the module interfaces unchanged when a large coefficient
        * buffer is necessary.)
        * In multi-pass modes, this array points to the current MCU's blocks
        * within the virtual arrays; it is used only by the input side.
        */
        private JBLOCK[] m_MCU_buffer = new JBLOCK[D_MAX_BLOCKS_IN_MCU];

        /* In multi-pass modes, we need a virtual block array for each component. */
        private jvirt_barray_control[] m_whole_image = new jvirt_barray_control[MAX_COMPONENTS];
        private jvirt_barray_control[] m_coef_arrays;

        /* When doing block smoothing, we latch coefficient Al values here */
        private int[] m_coef_bits_latch;

        public jpeg_d_coef_controller(jpeg_decompress_struct cinfo, bool need_full_buffer);

        public void start_input_pass();
        public ReadResult consume_data();
        public void start_output_pass();
        public int decompress_data(ComponentBuffer output_buf);

        /* Pointer to array of coefficient virtual arrays, or NULL if none */
        public jvirt_barray_control[] GetCoefArrays();

        private int decompress_onepass(ComponentBuffer output_buf);
        private int decompress_data_ordinary(ComponentBuffer output_buf);
        private int decompress_smooth_data(ComponentBuffer output_buf);

        private bool smoothing_ok();
        private void start_iMCU_row();
    }
}
