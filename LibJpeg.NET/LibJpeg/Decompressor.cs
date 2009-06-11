using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
    /// <summary>
    /// Master record for a decompression instance
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class Decompressor
    {
        private jpeg_decompress_struct m_classicDecompressor = new jpeg_decompress_struct(new jpeg_error_mgr());


        public jpeg_decompress_struct ClassicDecompressor
        {
            get
            {
                return m_classicDecompressor;
            }
        }

        //public LibJpeg.Classic.jpeg_source_mgr Src
        //{
        //    get { return m_src; }
        //    set { m_src = value; }
        //}

        /* Basic description of image --- filled in by jpeg_read_header(). */
        /* Application may inspect these values to decide how to process image. */

        // nominal image width (from SOF marker)
        public int Width
        {
            get
            {
                return m_classicDecompressor.Image_width;
            }
        }

        // nominal image height
        public int Height
        {
            get
            {
                return m_classicDecompressor.Image_height;
            }
        }

        // # of color components in JPEG image
        public int ComponentsPerSample
        {
            get
            {
                return m_classicDecompressor.Num_components;
            }
        }

        // colorspace of JPEG image
        public Colorspace Colorspace
        {
            get
            { 
                return (Colorspace)m_classicDecompressor.Jpeg_color_space;
            }
        }

        /* Decompression processing parameters --- these fields must be set before
         * calling jpeg_start_decompress().  Note that jpeg_read_header() initializes
         * them to default values.
         */

        // colorspace for output
        public Colorspace OutColorspace
        {
            get
            {
                return (Colorspace)m_classicDecompressor.Out_color_space;
            }
            set
            {
                m_classicDecompressor.Out_color_space = (J_COLOR_SPACE)value;
            }
        }

        // fraction by which to scale image
        public int ScaleNumerator
        {
            get
            {
                return m_classicDecompressor.Scale_num;
            }
            set
            {
                m_classicDecompressor.Scale_num = value;
            }
        }

        public int ScaleDenominator
        {
            get
            {
                return m_classicDecompressor.Scale_denom;
            }
            set
            {
                m_classicDecompressor.Scale_denom = value;
            }
        }

        // true=multiple output passes
        public bool BufferedImage
        {
            get
            {
                return m_classicDecompressor.Buffered_image;
            }
            set
            {
                m_classicDecompressor.Buffered_image = value;
            }
        }

        // true=downsampled data wanted
        public bool RawDataOut
        {
            get
            {
                return m_classicDecompressor.Raw_data_out;
            }
            set
            {
                m_classicDecompressor.Raw_data_out = value;
            }
        }

        // IDCT algorithm selector
        public DCTMethod DCTMethod
        {
            get
            {
                return (DCTMethod)m_classicDecompressor.Dct_method;
            }
            set
            {
                m_classicDecompressor.Dct_method = (J_DCT_METHOD)value;
            }
        }

        // true=apply fancy upsampling
        public bool DoFancyUpsampling
        {
            get
            {
                return m_classicDecompressor.Do_fancy_upsampling;
            }
            set
            {
                m_classicDecompressor.Do_fancy_upsampling = value;
            }
        }

        // true=apply interblock smoothing
        public bool DoBlockSmoothing
        {
            get
            {
                return m_classicDecompressor.Do_block_smoothing;
            }
            set
            {
                m_classicDecompressor.Do_block_smoothing = value; 
            }
        }

        // true=colormapped output wanted
        public bool QuantizeColors
        {
            get
            {
                return m_classicDecompressor.Quantize_colors; 
            }
            set
            {
                m_classicDecompressor.Quantize_colors = value; 
            }
        }

        /* the following are ignored if not quantize_colors: */

        // type of color dithering to use
        public DitherMode DitherMode
        {
            get 
            {
                return (DitherMode)m_classicDecompressor.Dither_mode; 
            }
            set
            { 
                m_classicDecompressor.Dither_mode = (J_DITHER_MODE)value; 
            }
        }

        // true=use two-pass color quantization
        public bool TwoPassQuantize
        {
            get
            {
                return m_classicDecompressor.Two_pass_quantize; 
            }
            set
            {
                m_classicDecompressor.Two_pass_quantize = value; 
            }
        }

        // max # colors to use in created colormap
        public int DesiredNumberOfColors
        {
            get 
            {
                return m_classicDecompressor.Desired_number_of_colors; 
            }
            set
            {
                m_classicDecompressor.Desired_number_of_colors = value; 
            }
        }

        /* these are significant only in buffered-image mode: */

        // enable future use of 1-pass quantizer
        public bool EnableOnePassQuantizer
        {
            get 
            {
                return m_classicDecompressor.Enable_1pass_quant; 
            }
            set
            {
                m_classicDecompressor.Enable_1pass_quant = value; 
            }
        }

        // enable future use of external colormap
        public bool EnableExternalQuant
        {
            get 
            {
                return m_classicDecompressor.Enable_external_quant; 
            }
            set
            {
                m_classicDecompressor.Enable_external_quant = value; 
            }
        }

        // enable future use of 2-pass quantizer
        public bool EnableTwoPassQuantizer
        {
            get 
            {
                return m_classicDecompressor.Enable_2pass_quant; 
            }
            set 
            {
                m_classicDecompressor.Enable_2pass_quant = value; 
            }
        }

        /* Description of actual output image that will be returned to application.
         * These fields are computed by jpeg_start_decompress().
         * You can also use jpeg_calc_output_dimensions() to determine these values
         * in advance of calling jpeg_start_decompress().
         */

        // scaled image width
        public int OutputWidth
        {
            get
            {
                return m_classicDecompressor.Output_width; 
            }
        }

        // scaled image height
        public int OutputHeight
        {
            get
            {
                return m_classicDecompressor.Output_height; 
            }
        }

        // # of color components in out_color_space
        public int OutComponentsPerSample
        {
            get
            {
                return m_classicDecompressor.Out_color_components; 
            }
        }

        // # of color components returned. it is 1 (a colormap index) when 
        // quantizing colors; otherwise it equals out_color_components.
        public int OutputComponents
        {
            get
            { 
                return m_classicDecompressor.Output_components; 
            }
        }

        // min recommended height of scanline buffer
        // If the buffer passed to jpeg_read_scanlines() is less than this many rows
        // high, space and time will be wasted due to unnecessary data copying.
        // Usually rec_outbuf_height will be 1 or 2, at most 4.
        public int RecommendedOutputBufferHeight
        {
            get
            {
                return m_classicDecompressor.Rec_outbuf_height; 
            }
        }

        /* When quantizing colors, the output colormap is described by these fields.
         * The application can supply a colormap by setting colormap non-null before
         * calling jpeg_start_decompress; otherwise a colormap is created during
         * jpeg_start_decompress or jpeg_start_output.
         * The map has out_color_components rows and actual_number_of_colors columns.
         */

        // number of entries in use
        public int ActualNumberOfColors
        {
            get 
            {
                return m_classicDecompressor.Actual_number_of_colors; 
            }
            set
            {
                m_classicDecompressor.Actual_number_of_colors = value; 
            }
        }

        // The color map as a 2-D pixel array
        public byte[][] Colormap
        {
            get
            {
                return m_classicDecompressor.Colormap;
            }
            set
            {
                m_classicDecompressor.Colormap = value;
            }
        }

        /* State variables: these variables indicate the progress of decompression.
         * The application may examine these but must not modify them.
         */

        /* Row index of next scanline to be read from jpeg_read_scanlines().
         * Application may use this to control its processing loop, e.g.,
         * "while (output_scanline < output_height)".
         */

        // 0 .. output_height-1
        public int OutputScanline
        {
            get 
            {
                return m_classicDecompressor.Output_scanline; 
            }
        }

        /* Current input scan number and number of iMCU rows completed in scan.
         * These indicate the progress of the decompressor input side.
         */

        // Number of SOS markers seen so far
        public int InputScanNumber
        {
            get 
            {
                return m_classicDecompressor.Input_scan_number; 
            }
        }

        // Number of iMCU rows completed
        public int iMCURowsCompleted
        {
            get 
            {
                return m_classicDecompressor.Input_iMCU_row; 
            }
        }

        /* The "output scan number" is the notional scan being displayed by the
         * output side.  The decompressor will not allow output scan/row number
         * to get ahead of input scan/row, but it can fall arbitrarily far behind.
         */

        // Nominal scan number being displayed
        public int OutputScanNumber
        {
            get
            {
                return m_classicDecompressor.Output_scan_number; 
            }
        }

        // Number of iMCU rows read
        public int iMCURowsRead
        {
            get 
            {
                return m_classicDecompressor.Output_iMCU_row; 
            }
        }

        /* Current progression status.  coef_bits[c][i] indicates the precision
         * with which component c's DCT coefficient i (in zigzag order) is known.
         * It is -1 when no data has yet been received, otherwise it is the point
         * transform (shift) value for the most recent scan of the coefficient
         * (thus, 0 at completion of the progression).
         * This is null when reading a non-progressive file.
         */

        // -1 or current Al value for each coef
        public int[][] CoefficientBits
        {
            get 
            {
                return m_classicDecompressor.Coef_bits;
            }
        }

        // These fields record data obtained from optional markers 
        // recognized by the JPEG library.

        // JFIF code for pixel size units
        public byte DensityUnit
        {
            get 
            {
                return m_classicDecompressor.Density_unit; 
            }
        }

        // Horizontal pixel density
        public int DensityX
        {
            get 
            {
                return m_classicDecompressor.X_density; 
            }
        }

        // Vertical pixel density
        public int DensityY
        {
            get 
            {
                return m_classicDecompressor.Y_density; 
            }
        }

        // It is either zero or the code of a JPEG marker that has been
        // read from the data source, but has not yet been processed.
        public int UnreadMarker
        {
            get 
            {
                return m_classicDecompressor.Unread_marker; 
            }
        }

        
        /// <summary>
        /// Prepare for input from a stdio stream.
        /// The caller must have already opened the stream, and is responsible
        /// for closing it after finishing decompression.
        /// </summary>
        public FileStream InputStream
        {
            set
            {
                m_classicDecompressor.jpeg_stdio_src(value);

                /* Read file header, set default decompression parameters */
                ReadHeader();
            }
        }

        public void SaveAsBitmap(Stream output, bool os2Header)
        {
            /* Initialize the output module now to let it override any crucial
             * option settings (for instance, GIF wants to force color quantization).
             */
            BitmapDestination dest_mgr = new BitmapDestination(this, os2Header);
            dest_mgr.OutputFile = output;

            /* Start decompressor */
            StartDecompress();

            /* Write output file header */
            dest_mgr.start_output();

            /* Process data */
            while (OutputScanline < OutputHeight)
            {
                int num_scanlines = ReadScanlines(dest_mgr.buffer, (int)dest_mgr.buffer_height);
                dest_mgr.put_pixel_rows(num_scanlines);
            }

            /* Finish decompression and release memory.
             * I must do it in this order because output module has allocated memory
             * of lifespan JPOOL_IMAGE; it needs to finish before releasing memory.
             */
            dest_mgr.finish_output();
            FinishDecompress();
        }

        /// <summary>
        /// Decompression startup: read start of JPEG datastream to see what's there.
        /// Need only initialize JPEG object and supply a data source before calling.
        /// 
        /// If you pass require_image = true (normal case), you need not check for
        /// a TABLES_ONLY return code; an abbreviated file will cause an error exit.
        /// JPEG_SUSPENDED is only possible if you use a data source module that can
        /// give a suspension return (the stdio source module doesn't).
        /// 
        /// This routine will read as far as the first SOS marker (ie, actual start of
        /// compressed data), and will save all tables and parameters in the JPEG
        /// object.  It will also initialize the decompression parameters to default
        /// values, and finally return JPEG_HEADER_OK.  On return, the application may
        /// adjust the decompression parameters and then call jpeg_start_decompress.
        /// (Or, if the application only wanted to determine the image parameters,
        /// the data need not be decompressed.  In that case, call jpeg_abort or
        /// jpeg_destroy to release any temporary space.)
        /// 
        /// If an abbreviated (tables only) datastream is presented, the routine will
        /// return JPEG_HEADER_TABLES_ONLY upon reaching EOI.  The application may then
        /// re-use the JPEG object to read the abbreviated image datastream(s).
        /// It is unnecessary (but OK) to call jpeg_abort in this case.
        /// The JPEG_SUSPENDED return code only occurs if the data source module
        /// requests suspension of the decompressor.  In this case the application
        /// should load more source data and then re-call jpeg_read_header to resume
        /// processing.
        /// 
        /// If a non-suspending data source is used and require_image is true, then the
        /// return code need not be inspected since only JPEG_HEADER_OK is possible.
        /// 
        /// This routine is now just a front end to jpeg_consume_input, with some
        /// extra error checking.
        /// </summary>
        public Read ReadHeader()
        {
            return (Read)m_classicDecompressor.jpeg_read_header(true);
        }

        //////////////////////////////////////////////////////////////////////////
        // Main entry points for decompression

        /// <summary>
        /// Decompression initialization.
        /// jpeg_read_header must be completed before calling this.
        /// 
        /// If a multipass operating mode was selected, this will do all but the
        /// last pass, and thus may take a great deal of time.
        /// 
        /// Returns false if suspended.  The return value need be inspected only if
        /// a suspending data source is used.
        /// </summary>
        public bool StartDecompress()
        {
            return m_classicDecompressor.jpeg_start_decompress();
        }

        /// <summary>
        /// Read some scanlines of data from the JPEG decompressor.
        /// 
        /// The return value will be the number of lines actually read.
        /// This may be less than the number requested in several cases,
        /// including bottom of image, data source suspension, and operating
        /// modes that emit multiple scanlines at a time.
        /// 
        /// Note: we warn about excess calls to jpeg_read_scanlines() since
        /// this likely signals an application programmer error.  However,
        /// an oversize buffer (max_lines > scanlines remaining) is not an error.
        /// </summary>
        public int ReadScanlines(byte[][] scanlines, int max_lines)
        {
            return m_classicDecompressor.jpeg_read_scanlines(scanlines, max_lines);
        }

        /// <summary>
        /// Finish JPEG decompression.
        /// 
        /// This will normally just verify the file trailer and release temp storage.
        /// 
        /// Returns false if suspended.  The return value need be inspected only if
        /// a suspending data source is used.
        /// </summary>
        public bool FinishDecompress()
        {
            return m_classicDecompressor.jpeg_finish_decompress();
        }

        /// <summary>
        /// Alternate entry point to read raw data.
        /// Replaces jpeg_read_scanlines when reading raw downsampled data.
        /// Processes exactly one iMCU row per call, unless suspended.
        /// </summary>
        public int ReadRawData(byte[][][] data, int max_lines)
        {
            return m_classicDecompressor.jpeg_read_raw_data(data, max_lines);
        }

        //////////////////////////////////////////////////////////////////////////
        // Additional entry points for buffered-image mode.

        /// <summary>
        /// Is there more than one scan?
        /// </summary>
        public bool HasMultipleScans()
        {
            return m_classicDecompressor.jpeg_has_multiple_scans();
        }

        /// <summary>
        /// Initialize for an output pass in buffered-image mode.
        /// </summary>
        public bool StartOutput(int scanNumber)
        {
            return m_classicDecompressor.jpeg_start_output(scanNumber);
        }

        /// <summary>
        /// Finish up after an output pass in buffered-image mode.
        /// 
        /// Returns false if suspended.  The return value need be inspected only if
        /// a suspending data source is used.
        /// </summary>
        public bool FinishOutput()
        {
            return m_classicDecompressor.jpeg_finish_decompress();
        }

        /// <summary>
        /// Have we finished reading the input file?
        /// </summary>
        public bool InputComplete()
        {
            return m_classicDecompressor.jpeg_input_complete();
        }

        /// <summary>
        /// Consume data in advance of what the decompressor requires.
        /// This can be called at any time once the decompressor object has
        /// been created and a data source has been set up.
        /// 
        /// This routine is essentially a state machine that handles a couple
        /// of critical state-transition actions, namely initial setup and
        /// transition from header scanning to ready-for-start_decompress.
        /// All the actual input is done via the input controller's consume_input
        /// method.
        /// </summary>
        public Read ConsumeInput()
        {
            return (Read)m_classicDecompressor.jpeg_consume_input();
        }

        /// <summary>
        /// Precalculate output image dimensions and related values for 
        /// current decompression parameters.
        /// 
        /// NOTE: this is allowed for possible use by application.
        /// Hence it mustn't do anything that can't be done twice.
        /// Also note that it may be called before the master module is initialized!
        /// </summary>
        public void CalculateOutputDimensions()
        {
            m_classicDecompressor.jpeg_calc_output_dimensions();
        }

        /* Read or write raw DCT coefficients --- useful for lossless transcoding. */
        /// <summary>
        /// Read or write the raw DCT coefficient arrays from a JPEG file
        /// (useful for lossless transcoding). 
        /// jpeg_read_header must be completed before calling this.
        /// 
        /// The entire image is read into a set of virtual coefficient-block arrays,
        /// one per component.  The return value is a pointer to the array of
        /// virtual-array descriptors.  These can be manipulated directly via the
        /// JPEG memory manager, or handed off to jpeg_write_coefficients().
        /// To release the memory occupied by the virtual arrays, call
        /// jpeg_finish_decompress() when done with the data.
        /// 
        /// An alternative usage is to simply obtain access to the coefficient arrays
        /// during a buffered-image-mode decompression operation.  This is allowed
        /// after any jpeg_finish_output() call.  The arrays can be accessed until
        /// jpeg_finish_decompress() is called.  (Note that any call to the library
        /// may reposition the arrays, so don't rely on access_virt_barray() results
        /// to stay valid across library calls.)
        /// 
        /// Returns null if suspended.  This case need be checked only if
        /// a suspending data source is used.
        /// </summary>
        public BlockArray2D[] ReadCoefficients()
        {
            jvirt_barray_control[] classicResult = m_classicDecompressor.jpeg_read_coefficients();
            if (classicResult == null)
                return null;

            BlockArray2D[] result = new BlockArray2D[classicResult.Length];
            for (int i = 0; i < classicResult.Length; ++i)
                result[i] = new BlockArray2D(classicResult[i]);

            return result;
        }

        /// <summary>
        /// Initialize the compression object with default parameters,
        /// then copy from the source object all parameters needed for lossless
        /// transcoding.  Parameters that can be varied without loss (such as
        /// scan script and Huffman optimization) are left in their default states.
        /// </summary>
        //public void jpeg_copy_critical_parameters(jpeg_compress_struct dstinfo)
        //{
        //    /* Safety check to ensure start_compress not called yet. */
        //    if (dstinfo.m_global_state != JpegState.CSTATE_START)
        //        ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)dstinfo.m_global_state);

        //    /* Copy fundamental image dimensions */
        //    dstinfo.m_image_width = m_image_width;
        //    dstinfo.m_image_height = m_image_height;
        //    dstinfo.m_input_components = m_num_components;
        //    dstinfo.m_in_color_space = m_jpeg_color_space;

        //    /* Initialize all parameters to default values */
        //    dstinfo.jpeg_set_defaults();

        //    /* jpeg_set_defaults may choose wrong colorspace, eg YCbCr if input is RGB.
        //    * Fix it to get the right header markers for the image colorspace.
        //    */
        //    dstinfo.jpeg_set_colorspace(m_jpeg_color_space);
        //    dstinfo.m_data_precision = m_data_precision;
        //    dstinfo.m_CCIR601_sampling = m_CCIR601_sampling;

        //    /* Copy the source's quantization tables. */
        //    for (int tblno = 0; tblno < JpegConstants.NUM_QUANT_TBLS; tblno++)
        //    {
        //        if (m_quant_tbl_ptrs[tblno] != null)
        //        {
        //            if (dstinfo.m_quant_tbl_ptrs[tblno] == null)
        //                dstinfo.m_quant_tbl_ptrs[tblno] = new JQUANT_TBL();

        //            Array.Copy(m_quant_tbl_ptrs[tblno].quantval, dstinfo.m_quant_tbl_ptrs[tblno].quantval, dstinfo.m_quant_tbl_ptrs[tblno].quantval.Length);
        //            dstinfo.m_quant_tbl_ptrs[tblno].sent_table = false;
        //        }
        //    }

        //    /* Copy the source's per-component info.
        //    * Note we assume jpeg_set_defaults has allocated the dest comp_info array.
        //    */
        //    dstinfo.m_num_components = m_num_components;
        //    if (dstinfo.m_num_components < 1 || dstinfo.m_num_components > JpegConstants.MAX_COMPONENTS)
        //        ERREXIT(J_MESSAGE_CODE.JERR_COMPONENT_COUNT, dstinfo.m_num_components, JpegConstants.MAX_COMPONENTS);

        //    for (int ci = 0; ci < dstinfo.m_num_components; ci++)
        //    {
        //        dstinfo.m_comp_info[ci].component_id = m_comp_info[ci].component_id;
        //        dstinfo.m_comp_info[ci].h_samp_factor = m_comp_info[ci].h_samp_factor;
        //        dstinfo.m_comp_info[ci].v_samp_factor = m_comp_info[ci].v_samp_factor;
        //        dstinfo.m_comp_info[ci].quant_tbl_no = m_comp_info[ci].quant_tbl_no;

        //        /* Make sure saved quantization table for component matches the qtable
        //        * slot.  If not, the input file re-used this qtable slot.
        //        * IJG encoder currently cannot duplicate this.
        //        */
        //        int tblno = dstinfo.m_comp_info[ci].quant_tbl_no;
        //        if (tblno < 0 || tblno >= JpegConstants.NUM_QUANT_TBLS || m_quant_tbl_ptrs[tblno] == null)
        //            ERREXIT(J_MESSAGE_CODE.JERR_NO_QUANT_TABLE, tblno);

        //        JQUANT_TBL c_quant = m_comp_info[ci].quant_table;
        //        if (c_quant != null)
        //        {
        //            JQUANT_TBL slot_quant = m_quant_tbl_ptrs[tblno];
        //            for (int coefi = 0; coefi < JpegConstants.DCTSIZE2; coefi++)
        //            {
        //                if (c_quant.quantval[coefi] != slot_quant.quantval[coefi])
        //                    ERREXIT(J_MESSAGE_CODE.JERR_MISMATCHED_QUANT_TABLE, tblno);
        //            }
        //        }
        //        /* Note: we do not copy the source's Huffman table assignments;
        //        * instead we rely on jpeg_set_colorspace to have made a suitable choice.
        //        */
        //    }

        //    /* Also copy JFIF version and resolution information, if available.
        //    * Strictly speaking this isn't "critical" info, but it's nearly
        //    * always appropriate to copy it if available.  In particular,
        //    * if the application chooses to copy JFIF 1.02 extension markers from
        //    * the source file, we need to copy the version to make sure we don't
        //    * emit a file that has 1.02 extensions but a claimed version of 1.01.
        //    * We will *not*, however, copy version info from mislabeled "2.01" files.
        //    */
        //    if (m_saw_JFIF_marker)
        //    {
        //        if (m_JFIF_major_version == 1)
        //        {
        //            dstinfo.m_JFIF_major_version = m_JFIF_major_version;
        //            dstinfo.m_JFIF_minor_version = m_JFIF_minor_version;
        //        }

        //        dstinfo.m_density_unit = m_density_unit;
        //        dstinfo.m_X_density = m_X_density;
        //        dstinfo.m_Y_density = m_Y_density;
        //    }
        //}

        /// <summary>
        /// Abort processing of a JPEG decompression operation, 
        /// but don't destroy the object itself.
        /// 
        /// If you choose to abort compression or decompression before completing
        /// jpeg_finish_(de)compress, then you need to clean up to release memory,
        /// temporary files, etc.  You can just call jpeg_destroy_(de)compress
        /// if you're done with the JPEG object, but if you want to clean it up and
        /// reuse it, call this:
        /// </summary>
        public void AbortDecompress()
        {
            m_classicDecompressor.jpeg_abort_decompress();
        }

        /// <summary>
        /// Delegate for application-supplied marker processing methods.
        /// Need not pass marker code since it is stored in cinfo.unread_marker.
        /// </summary>
        public delegate bool MarkerParser(Decompressor decompressor);

        /* Install a special processing method for COM or APPn markers. */
        public void SetMarkerProcessor(int markerCode, MarkerParser routine)
        {
            jpeg_decompress_struct.jpeg_marker_parser_method f = delegate { return routine(this); };
            m_classicDecompressor.jpeg_set_marker_processor(markerCode, f);
        }

        /* Control saving of COM and APPn markers into marker_list. */
        public void SaveMarkers(int markerCode, int lengthLimit)
        {
            m_classicDecompressor.jpeg_save_markers(markerCode, lengthLimit);
        }
    }

    public class BitmapDestination
    {
        /* Target file spec; filled in by djpeg.c after object is created. */
        private Stream output_file;

        /* Output pixel-row buffer.  Created by module init or start_output.
         * Width is cinfo.output_width * cinfo.output_components;
         * height is buffer_height.
         */
        public byte[][] buffer;
        public int buffer_height;

        private Decompressor decompressor;
        private bool m_putGrayRows;
        private bool is_os2;        /* saves the OS2 format request flag */

        private jvirt_sarray_control whole_image;  /* needed to reverse row order */
        private int data_width;  /* bytes per row */
        private int row_width;       /* physical width of one row in the BMP file */
        private int pad_bytes;      /* number of padding bytes needed per row */
        private int cur_output_row;  /* next row# to write to virtual array */

        public BitmapDestination(Decompressor decompressor, bool is_os2)
        {
            this.decompressor = decompressor;
            this.is_os2 = is_os2;

            if (decompressor.OutColorspace == Colorspace.Grayscale)
            {
                m_putGrayRows = true;
            }
            else if (decompressor.OutColorspace == Colorspace.RGB)
            {
                if (decompressor.QuantizeColors)
                    m_putGrayRows = true;
                else
                    m_putGrayRows = false;
            }
            else
            {
                int errCode = 1005;//JERR_BMP_COLORSPACE
                decompressor.ClassicDecompressor.ERREXIT(errCode);
            }

            /* Calculate output image dimensions so we can allocate space */
            decompressor.CalculateOutputDimensions();

            /* Determine width of rows in the BMP file (padded to 4-byte boundary). */
            row_width = decompressor.OutputWidth * decompressor.OutputComponents;
            data_width = row_width;
            while ((row_width & 3) != 0)
                row_width++;

            pad_bytes = (int)(row_width - data_width);

            /* Allocate space for inversion array, prepare for write pass */
            jpeg_decompress_struct cinfo = decompressor.ClassicDecompressor;
            whole_image = new jvirt_sarray_control(cinfo, false, row_width, decompressor.OutputHeight);
            cur_output_row = 0;

            /* Create decompressor output buffer. */
            buffer = jpeg_common_struct.AllocJpegSamples(row_width, 1);
            buffer_height = 1;
        }

        public Stream OutputFile
        {
            get
            {
                return output_file;
            }
            set
            {
                output_file = value;
            }
        }

        /// <summary>
        /// Startup: normally writes the file header.
        /// In this module we may as well postpone everything until finish_output.
        /// </summary>
        public void start_output()
        {
            /* no work here */
        }

        /// <summary>
        /// Write some pixel data.
        /// In this module rows_supplied will always be 1.
        /// </summary>
        public void put_pixel_rows(int rows_supplied)
        {
            if (m_putGrayRows)
                put_gray_rows(rows_supplied);
            else
                put_24bit_rows(rows_supplied);
        }

        /// <summary>
        /// Finish up at the end of the file.
        /// Here is where we really output the BMP file.
        /// </summary>
        public void finish_output()
        {
            /* Write the header and colormap */
            if (is_os2)
                write_os2_header();
            else
                write_bmp_header();

            jpeg_decompress_struct cinfo = decompressor.ClassicDecompressor;
            /* Write the file body from our virtual array */
            for (int row = cinfo.Output_height; row > 0; row--)
            {
                byte[][] image_ptr = whole_image.access_virt_sarray(row - 1, 1);
                int imageIndex = 0;
                for (int col = row_width; col > 0; col--)
                {
                    output_file.WriteByte(image_ptr[0][imageIndex]);
                    imageIndex++;
                }
            }

            /* Make sure we wrote the output file OK */
            output_file.Flush();
        }

        /// <summary>
        /// Write some pixel data.
        /// In this module rows_supplied will always be 1.
        /// 
        /// This version is for writing 24-bit pixels
        /// </summary>
        private void put_24bit_rows(int rows_supplied)
        {
            /* Access next row in virtual array */
            byte[][] image_ptr = whole_image.access_virt_sarray(cur_output_row, 1);
            cur_output_row++;

            /* Transfer data.  Note destination values must be in BGR order
             * (even though Microsoft's own documents say the opposite).
             */
            int bufferIndex = 0;
            int imageIndex = 0;
            for (int col = decompressor.OutputWidth; col > 0; col--)
            {
                image_ptr[0][imageIndex + 2] = buffer[0][bufferIndex];   /* can omit GETJSAMPLE() safely */
                bufferIndex++;
                image_ptr[0][imageIndex + 1] = buffer[0][bufferIndex];
                bufferIndex++;
                image_ptr[0][imageIndex] = buffer[0][bufferIndex];
                bufferIndex++;
                imageIndex += 3;
            }

            /* Zero out the pad bytes. */
            int pad = pad_bytes;
            while (--pad >= 0)
            {
                image_ptr[0][imageIndex] = 0;
                imageIndex++;
            }
        }

        /// <summary>
        /// Write some pixel data.
        /// In this module rows_supplied will always be 1.
        /// 
        /// This version is for grayscale OR quantized color output
        /// </summary>
        private void put_gray_rows(int rows_supplied)
        {
            /* Access next row in virtual array */
            byte[][] image_ptr = whole_image.access_virt_sarray(cur_output_row, 1);
            cur_output_row++;

            /* Transfer data. */
            int index = 0;
            for (int col = decompressor.OutputWidth; col > 0; col--)
            {
                image_ptr[0][index] = buffer[0][index];/* can omit GETJSAMPLE() safely */
                index++;
            }

            /* Zero out the pad bytes. */
            int pad = pad_bytes;
            while (--pad >= 0)
            {
                image_ptr[0][index] = 0;
                index++;
            }
        }

        /// <summary>
        /// Write a Windows-style BMP file header, including colormap if needed
        /// </summary>
        private void write_bmp_header()
        {
            int bits_per_pixel;
            int cmap_entries;

            /* Compute colormap size and total file size */
            if (decompressor.OutColorspace == Colorspace.RGB)
            {
                if (decompressor.QuantizeColors)
                {
                    /* Colormapped RGB */
                    bits_per_pixel = 8;
                    cmap_entries = 256;
                }
                else
                {
                    /* Unquantized, full color RGB */
                    bits_per_pixel = 24;
                    cmap_entries = 0;
                }
            }
            else
            {
                /* Grayscale output.  We need to fake a 256-entry colormap. */
                bits_per_pixel = 8;
                cmap_entries = 256;
            }

            /* File size */
            int headersize = 14 + 40 + cmap_entries * 4; /* Header and colormap */
            int bfSize = headersize + (int)row_width * (int)decompressor.OutputHeight;

            /* Set unused fields of header to 0 */
            byte[] bmpfileheader = new byte[14];
            byte[] bmpinfoheader = new byte[40];

            /* Fill the file header */
            bmpfileheader[0] = 0x42;    /* first 2 bytes are ASCII 'B', 'M' */
            bmpfileheader[1] = 0x4D;
            PUT_4B(bmpfileheader, 2, bfSize); /* bfSize */
            /* we leave bfReserved1 & bfReserved2 = 0 */
            PUT_4B(bmpfileheader, 10, headersize); /* bfOffBits */

            /* Fill the info header (Microsoft calls this a BITMAPINFOHEADER) */
            PUT_2B(bmpinfoheader, 0, 40);   /* biSize */
            PUT_4B(bmpinfoheader, 4, decompressor.OutputWidth); /* biWidth */
            PUT_4B(bmpinfoheader, 8, decompressor.OutputHeight); /* biHeight */
            PUT_2B(bmpinfoheader, 12, 1);   /* biPlanes - must be 1 */
            PUT_2B(bmpinfoheader, 14, bits_per_pixel); /* biBitCount */
            /* we leave biCompression = 0, for none */
            /* we leave biSizeImage = 0; this is correct for uncompressed data */

            if (decompressor.DensityUnit == 2)
            {
                /* if have density in dots/cm, then */
                PUT_4B(bmpinfoheader, 24, decompressor.DensityX * 100); /* XPels/M */
                PUT_4B(bmpinfoheader, 28, decompressor.DensityY * 100); /* XPels/M */
            }
            PUT_2B(bmpinfoheader, 32, cmap_entries); /* biClrUsed */
            /* we leave biClrImportant = 0 */

            try
            {
                output_file.Write(bmpfileheader, 0, 14);
            }
            catch (Exception e)
            {
                decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
            }

            try
            {
                output_file.Write(bmpinfoheader, 0, 40);
            }
            catch (Exception e)
            {
                decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
            }

            if (cmap_entries > 0)
                write_colormap(cmap_entries, 4);
        }

        /// <summary>
        /// Write an OS2-style BMP file header, including colormap if needed
        /// </summary>
        private void write_os2_header()
        {
            int bits_per_pixel;
            int cmap_entries;

            /* Compute colormap size and total file size */
            if (decompressor.OutColorspace == Colorspace.RGB)
            {
                if (decompressor.QuantizeColors)
                {
                    /* Colormapped RGB */
                    bits_per_pixel = 8;
                    cmap_entries = 256;
                }
                else
                {
                    /* Unquantized, full color RGB */
                    bits_per_pixel = 24;
                    cmap_entries = 0;
                }
            }
            else
            {
                /* Grayscale output.  We need to fake a 256-entry colormap. */
                bits_per_pixel = 8;
                cmap_entries = 256;
            }

            /* File size */
            int headersize = 14 + 12 + cmap_entries * 3; /* Header and colormap */
            int bfSize = headersize + (int)row_width * decompressor.OutputHeight;

            /* Set unused fields of header to 0 */
            byte[] bmpfileheader = new byte[14];
            byte[] bmpcoreheader = new byte[12];

            /* Fill the file header */
            bmpfileheader[0] = 0x42;    /* first 2 bytes are ASCII 'B', 'M' */
            bmpfileheader[1] = 0x4D;
            PUT_4B(bmpfileheader, 2, bfSize); /* bfSize */
            /* we leave bfReserved1 & bfReserved2 = 0 */
            PUT_4B(bmpfileheader, 10, headersize); /* bfOffBits */

            /* Fill the info header (Microsoft calls this a BITMAPCOREHEADER) */
            PUT_2B(bmpcoreheader, 0, 12);   /* bcSize */
            PUT_2B(bmpcoreheader, 4, decompressor.OutputWidth); /* bcWidth */
            PUT_2B(bmpcoreheader, 6, decompressor.OutputHeight); /* bcHeight */
            PUT_2B(bmpcoreheader, 8, 1);    /* bcPlanes - must be 1 */
            PUT_2B(bmpcoreheader, 10, bits_per_pixel); /* bcBitCount */

            try
            {
                output_file.Write(bmpfileheader, 0, 14);
            }
            catch (Exception e)
            {
                decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
            }

            try
            {
                output_file.Write(bmpcoreheader, 0, 12);
            }
            catch (Exception e)
            {
                decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
            }

            if (cmap_entries > 0)
                write_colormap(cmap_entries, 3);
        }

        /// <summary>
        /// Write the colormap.
        /// Windows uses BGR0 map entries; OS/2 uses BGR entries.
        /// </summary>
        private void write_colormap(int map_colors, int map_entry_size)
        {
            byte[][] colormap = decompressor.Colormap;
            int num_colors = decompressor.ActualNumberOfColors;

            int i = 0;
            if (colormap != null)
            {
                if (decompressor.OutComponentsPerSample == 3)
                {
                    /* Normal case with RGB colormap */
                    for (i = 0; i < num_colors; i++)
                    {
                        output_file.WriteByte(colormap[2][i]);
                        output_file.WriteByte(colormap[1][i]);
                        output_file.WriteByte(colormap[0][i]);
                        if (map_entry_size == 4)
                            output_file.WriteByte(0);
                    }
                }
                else
                {
                    /* Grayscale colormap (only happens with grayscale quantization) */
                    for (i = 0; i < num_colors; i++)
                    {
                        output_file.WriteByte(colormap[0][i]);
                        output_file.WriteByte(colormap[0][i]);
                        output_file.WriteByte(colormap[0][i]);
                        if (map_entry_size == 4)
                            output_file.WriteByte(0);
                    }
                }
            }
            else
            {
                /* If no colormap, must be grayscale data.  Generate a linear "map". */
                for (i = 0; i < 256; i++)
                {
                    output_file.WriteByte((byte)i);
                    output_file.WriteByte((byte)i);
                    output_file.WriteByte((byte)i);
                    if (map_entry_size == 4)
                        output_file.WriteByte(0);
                }
            }

            /* Pad colormap with zeros to ensure specified number of colormap entries */
            if (i > map_colors)
            {
                int errCode = 1026;//JERR_TOO_MANY_COLORS
                decompressor.ClassicDecompressor.ERREXIT(errCode, i);
            }

            for (; i < map_colors; i++)
            {
                output_file.WriteByte(0);
                output_file.WriteByte(0);
                output_file.WriteByte(0);
                if (map_entry_size == 4)
                    output_file.WriteByte(0);
            }
        }

        private static void PUT_2B(byte[] array, int offset, int value)
        {
            array[offset] = (byte)((value) & 0xFF);
            array[offset + 1] = (byte)(((value) >> 8) & 0xFF);
        }

        private static void PUT_4B(byte[] array, int offset, int value)
        {
            array[offset] = (byte)((value) & 0xFF);
            array[offset + 1] = (byte)(((value) >> 8) & 0xFF);
            array[offset + 2] = (byte)(((value) >> 16) & 0xFF);
            array[offset + 3] = (byte)(((value) >> 24) & 0xFF);
        }
    }
}
