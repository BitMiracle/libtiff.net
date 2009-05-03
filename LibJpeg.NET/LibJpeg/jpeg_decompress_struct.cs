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
using System.IO;

namespace LibJpeg.NET
{
    /// <summary>
    /// Master record for a decompression instance
    /// </summary>
    public class jpeg_decompress_struct : jpeg_common_struct
    {
        /* Source of compressed data */
        public jpeg_source_mgr m_src;

        /* Basic description of image --- filled in by jpeg_read_header(). */
        /* Application may inspect these values to decide how to process image. */

        public uint m_image_width; /* nominal image width (from SOF marker) */
        public uint m_image_height;    /* nominal image height */
        public int m_num_components;     /* # of color components in JPEG image */
        public J_COLOR_SPACE m_jpeg_color_space; /* colorspace of JPEG image */

        /* Decompression processing parameters --- these fields must be set before
         * calling jpeg_start_decompress().  Note that jpeg_read_header() initializes
         * them to default values.
         */

        public J_COLOR_SPACE m_out_color_space; /* colorspace for output */

        public uint m_scale_num, m_scale_denom; /* fraction by which to scale image */

        public bool m_buffered_image;    /* true=multiple output passes */
        public bool m_raw_data_out;      /* true=downsampled data wanted */

        public J_DCT_METHOD m_dct_method;    /* IDCT algorithm selector */
        public bool m_do_fancy_upsampling;   /* true=apply fancy upsampling */
        public bool m_do_block_smoothing;    /* true=apply interblock smoothing */

        public bool m_quantize_colors;   /* true=colormapped output wanted */
        /* the following are ignored if not quantize_colors: */
        public J_DITHER_MODE m_dither_mode;  /* type of color dithering to use */
        public bool m_two_pass_quantize; /* true=use two-pass color quantization */
        public int m_desired_number_of_colors;   /* max # colors to use in created colormap */
        /* these are significant only in buffered-image mode: */
        public bool m_enable_1pass_quant;    /* enable future use of 1-pass quantizer */
        public bool m_enable_external_quant;/* enable future use of external colormap */
        public bool m_enable_2pass_quant;    /* enable future use of 2-pass quantizer */

        /* Description of actual output image that will be returned to application.
         * These fields are computed by jpeg_start_decompress().
         * You can also use jpeg_calc_output_dimensions() to determine these values
         * in advance of calling jpeg_start_decompress().
         */

        public uint m_output_width;    /* scaled image width */
        public uint m_output_height;   /* scaled image height */
        public int m_out_color_components;   /* # of color components in out_color_space */
        public int m_output_components;  /* # of color components returned */
        /* output_components is 1 (a colormap index) when quantizing colors;
         * otherwise it equals out_color_components.
         */
        public int m_rec_outbuf_height;  /* min recommended height of scanline buffer */
        /* If the buffer passed to jpeg_read_scanlines() is less than this many rows
         * high, space and time will be wasted due to unnecessary data copying.
         * Usually rec_outbuf_height will be 1 or 2, at most 4.
         */

        /* When quantizing colors, the output colormap is described by these fields.
         * The application can supply a colormap by setting colormap non-NULL before
         * calling jpeg_start_decompress; otherwise a colormap is created during
         * jpeg_start_decompress or jpeg_start_output.
         * The map has out_color_components rows and actual_number_of_colors columns.
         */
        public int m_actual_number_of_colors;    /* number of entries in use */
        public byte[][] m_colormap;     /* The color map as a 2-D pixel array */

        /* State variables: these variables indicate the progress of decompression.
         * The application may examine these but must not modify them.
         */

        /* Row index of next scanline to be read from jpeg_read_scanlines().
         * Application may use this to control its processing loop, e.g.,
         * "while (output_scanline < output_height)".
         */
        public uint m_output_scanline; /* 0 .. output_height-1  */

        /* Current input scan number and number of iMCU rows completed in scan.
         * These indicate the progress of the decompressor input side.
         */
        public int m_input_scan_number;  /* Number of SOS markers seen so far */
        public uint m_input_iMCU_row;  /* Number of iMCU rows completed */

        /* The "output scan number" is the notional scan being displayed by the
         * output side.  The decompressor will not allow output scan/row number
         * to get ahead of input scan/row, but it can fall arbitrarily far behind.
         */
        public int m_output_scan_number; /* Nominal scan number being displayed */
        public uint m_output_iMCU_row; /* Number of iMCU rows read */

        /* Current progression status.  coef_bits[c][i] indicates the precision
         * with which component c's DCT coefficient i (in zigzag order) is known.
         * It is -1 when no data has yet been received, otherwise it is the point
         * transform (shift) value for the most recent scan of the coefficient
         * (thus, 0 at completion of the progression).
         * This pointer is NULL when reading a non-progressive file.
         */
        public int[][] m_coef_bits; /* -1 or current Al value for each coef */

        /* Internal JPEG parameters --- the application usually need not look at
         * these fields.  Note that the decompressor output side may not use
         * any parameters that can change between scans.
         */

        /* Quantization and Huffman tables are carried forward across input
         * datastreams when processing abbreviated JPEG datastreams.
         */

        internal JQUANT_TBL[] m_quant_tbl_ptrs = new JQUANT_TBL[JpegConstants.NUM_QUANT_TBLS];
        /* ptrs to coefficient quantization tables, or NULL if not defined */

        internal JHUFF_TBL[] m_dc_huff_tbl_ptrs = new JHUFF_TBL[JpegConstants.NUM_HUFF_TBLS];
        internal JHUFF_TBL[] m_ac_huff_tbl_ptrs = new JHUFF_TBL[JpegConstants.NUM_HUFF_TBLS];
        /* ptrs to Huffman coding tables, or NULL if not defined */

        /* These parameters are never carried across datastreams, since they
         * are given in SOF/SOS markers or defined to be reset by SOI.
         */

        internal int m_data_precision;     /* bits of precision in image data */

        internal jpeg_component_info[] m_comp_info;
        /* comp_info[i] describes component that appears i'th in SOF */

        internal bool m_progressive_mode;  /* true if SOFn specifies progressive mode */

        internal uint m_restart_interval; /* MCUs per restart interval, or 0 for no restart */

        /* These fields record data obtained from optional markers recognized by
         * the JPEG library.
         */
        internal bool m_saw_JFIF_marker;   /* true iff a JFIF APP0 marker was found */
        /* Data copied from JFIF marker; only valid if saw_JFIF_marker is true: */
        internal byte m_JFIF_major_version;   /* JFIF version number */
        internal byte m_JFIF_minor_version;
        internal byte m_density_unit;     /* JFIF code for pixel size units */
        internal ushort m_X_density;       /* Horizontal pixel density */
        internal ushort m_Y_density;       /* Vertical pixel density */
        internal bool m_saw_Adobe_marker;  /* true iff an Adobe APP14 marker was found */
        internal byte m_Adobe_transform;  /* Color transform code from Adobe marker */

        internal bool m_CCIR601_sampling;  /* true=first samples are cosited */

        /* Aside from the specific data retained from APPn markers known to the
         * library, the uninterpreted contents of any or all APPn and COM markers
         * can be saved in a list for examination by the application.
         */
        internal jpeg_marker_struct m_marker_list; /* Head of list of saved markers */

        /* Remaining fields are known throughout decompressor, but generally
         * should not be touched by a surrounding application.
         */

        /*
         * These fields are computed during decompression startup
         */
        internal int m_max_h_samp_factor;  /* largest h_samp_factor */
        internal int m_max_v_samp_factor;  /* largest v_samp_factor */

        internal int m_min_DCT_scaled_size;    /* smallest DCT_scaled_size of any component */

        internal uint m_total_iMCU_rows; /* # of iMCU rows in image */
        /* The coefficient controller's input and output progress is measured in
         * units of "iMCU" (interleaved MCU) rows.  These are the same as MCU rows
         * in fully interleaved JPEG scans, but are used whether the scan is
         * interleaved or not.  We define an iMCU row as v_samp_factor DCT block
         * rows of each component.  Therefore, the IDCT output contains
         * v_samp_factor*DCT_scaled_size sample rows of a component per iMCU row.
         */

        internal byte[] m_sample_range_limit; /* table for fast range-limiting */
        internal int m_sampleRangeLimitOffset;

        /*
         * These fields are valid during any one scan.
         * They describe the components and MCUs actually appearing in the scan.
         * Note that the decompressor output side must not use these fields.
         */
        internal int m_comps_in_scan;      /* # of JPEG components in this scan */
        internal jpeg_component_info[] m_cur_comp_info = new jpeg_component_info[JpegConstants.MAX_COMPS_IN_SCAN];
        /* *cur_comp_info[i] describes component that appears i'th in SOS */

        internal uint m_MCUs_per_row;    /* # of MCUs across the image */
        internal uint m_MCU_rows_in_scan;    /* # of MCU rows in the image */

        internal int m_blocks_in_MCU;      /* # of DCT blocks per MCU */
        internal int[] m_MCU_membership = new int[D_MAX_BLOCKS_IN_MCU];
        /* MCU_membership[i] is index in cur_comp_info of component owning */
        /* i'th block in an MCU */

        /* progressive JPEG parameters for scan */
        internal int m_Ss;
        internal int m_Se;
        internal int m_Ah;
        internal int m_Al;

        /* This field is shared between entropy decoder and marker parser.
         * It is either zero or the code of a JPEG marker that has been
         * read from the data source, but has not yet been processed.
         */
        internal int m_unread_marker;

        internal bool use_merged_upsample();

        /*
         * Links to decompression subobjects (methods, private variables of modules)
         */
        internal jpeg_decomp_master m_master;
        internal jpeg_d_main_controller m_main;
        internal jpeg_d_coef_controller m_coef;
        internal jpeg_d_post_controller m_post;
        internal jpeg_input_controller m_inputctl;
        internal jpeg_marker_reader m_marker;
        internal jpeg_entropy_decoder m_entropy;
        internal jpeg_inverse_dct m_idct;
        internal jpeg_upsampler m_upsample;
        internal jpeg_color_deconverter m_cconvert;
        internal jpeg_color_quantizer m_cquantize;

        public jpeg_decompress_struct();
        public jpeg_decompress_struct(jpeg_error_mgr errorManager);

        public void SetErrorManager(jpeg_error_mgr errorManager);

        /* Standard data source manager: stdio stream. */
        /* Caller is responsible for opening the file before and closing after. */
        public void jpeg_stdio_src(FileStream infile);

        /* Decompression startup: read start of JPEG datastream to see what's there */
        /* If you pass require_image = true (normal case), you need not check for
        * a TABLES_ONLY return code; an abbreviated file will cause an error exit.
        * JPEG_SUSPENDED is only possible if you use a data source module that can
        * give a suspension return (the stdio source module doesn't).
        */
        public ReadResult jpeg_read_header(bool require_image);

        /* Main entry points for decompression */
        public bool jpeg_start_decompress();
        public uint jpeg_read_scanlines(byte[][] scanlines, uint max_lines);
        public bool jpeg_finish_decompress();

        /* Replaces jpeg_read_scanlines when reading raw downsampled data. */
        public uint jpeg_read_raw_data(byte[][][] data, uint max_lines);

        /* Additional entry points for buffered-image mode. */
        public bool jpeg_has_multiple_scans();
        public bool jpeg_start_output(int scan_number);
        public bool jpeg_finish_output();
        public bool jpeg_input_complete();

        public ReadResult jpeg_consume_input();

        /* Precalculate output dimensions for current decompression parameters. */
        public void jpeg_calc_output_dimensions();

        /* Read or write raw DCT coefficients --- useful for lossless transcoding. */
        public jvirt_barray_control[] jpeg_read_coefficients();
        public void jpeg_copy_critical_parameters(jpeg_compress_struct dstinfo);

        /* If you choose to abort compression or decompression before completing
        * jpeg_finish_(de)compress, then you need to clean up to release memory,
        * temporary files, etc.  You can just call jpeg_destroy_(de)compress
        * if you're done with the JPEG object, but if you want to clean it up and
        * reuse it, call this:
        */
        public void jpeg_abort_decompress();

        /// <summary>
        /// Delegate for application-supplied marker processing methods.
        /// Need not pass marker code since it is stored in cinfo->unread_marker.
        /// </summary>
        public delegate bool jpeg_marker_parser_method(jpeg_decompress_struct cinfo);

        /* Install a special processing method for COM or APPn markers. */
        public void jpeg_set_marker_processor(int marker_code, jpeg_marker_parser_method routine);

        /* Control saving of COM and APPn markers into marker_list. */
        public void jpeg_save_markers(int marker_code, uint length_limit);

        /* Initialization of JPEG compression objects.
        */
        private void initialize();

        private void transdecode_master_selection();
        private bool output_pass_setup();
        private void default_decompress_parms();

        private void jpeg_consume_input_start();
        private ReadResult jpeg_consume_input_inHeader();
    }
}
