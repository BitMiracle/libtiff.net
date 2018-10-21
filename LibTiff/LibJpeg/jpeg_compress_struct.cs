using System;
using System.IO;

using BitMiracle.LibJpeg.Classic.Internal;

namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// JPEG compression routine.
    /// </summary>
    /// <seealso cref="jpeg_decompress_struct"/>
#if EXPOSE_LIBJPEG
    public
#endif
    class jpeg_compress_struct : jpeg_common_struct
    {
        /* These are the sample quantization tables given in JPEG spec section K.1.
         * The spec says that the values given produce "good" quality, and
         * when divided by 2, "very good" quality.
         */
        private static readonly int[] std_luminance_quant_tbl = {
            16,  11,  10,  16,  24,  40,  51,  61,
            12,  12,  14,  19,  26,  58,  60,  55,
            14,  13,  16,  24,  40,  57,  69,  56,
            14,  17,  22,  29,  51,  87,  80,  62,
            18,  22,  37,  56,  68, 109, 103,  77,
            24,  35,  55,  64,  81, 104, 113,  92,
            49,  64,  78,  87, 103, 121, 120, 101,
            72,  92,  95,  98, 112, 100, 103,  99
        };

        private static readonly int[] std_chrominance_quant_tbl =
        {
            17,  18,  24,  47,  99,  99,  99,  99,
            18,  21,  26,  66,  99,  99,  99,  99,
            24,  26,  56,  99,  99,  99,  99,  99,
            47,  66,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99
        };

        // Standard Huffman tables (cf. JPEG standard section K.3)
        // 
        // IMPORTANT: these are only valid for 8-bit data precision!
        private static readonly byte[] bits_dc_luminance =
        { /* 0-base */ 0, 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };

        private static readonly byte[] val_dc_luminance = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        private static readonly byte[] bits_dc_chrominance =
        { /* 0-base */ 0, 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 };

        private static readonly byte[] val_dc_chrominance = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        private static readonly byte[] bits_ac_luminance =
        { /* 0-base */ 0, 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d };

        private static readonly byte[] val_ac_luminance =
        {
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
            0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
            0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xa1, 0x08,
            0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0,
            0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0a, 0x16,
            0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2a, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
            0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
            0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
            0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
            0x7a, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
            0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7,
            0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6,
            0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3, 0xc4, 0xc5,
            0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4,
            0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
            0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea,
            0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
            0xf9, 0xfa
        };

        private static readonly byte[] bits_ac_chrominance =
        { /* 0-base */ 0, 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77 };

        private static readonly byte[] val_ac_chrominance =
        {
            0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
            0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
            0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
            0xa1, 0xb1, 0xc1, 0x09, 0x23, 0x33, 0x52, 0xf0,
            0x15, 0x62, 0x72, 0xd1, 0x0a, 0x16, 0x24, 0x34,
            0xe1, 0x25, 0xf1, 0x17, 0x18, 0x19, 0x1a, 0x26,
            0x27, 0x28, 0x29, 0x2a, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            0x59, 0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
            0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
            0x79, 0x7a, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96,
            0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3, 0xa4, 0xa5,
            0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4,
            0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3,
            0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2,
            0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda,
            0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9,
            0xea, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
            0xf9, 0xfa
        };

        /* Destination for compressed data */
        internal jpeg_destination_mgr m_dest;

        internal int m_image_width; /* input image width */
        internal int m_image_height;    /* input image height */
        internal int m_input_components;       /* # of color components in input image */
        internal J_COLOR_SPACE m_in_color_space;   /* colorspace of input image */

        /// <summary>
        /// The scale numerator
        /// </summary>
        public int scale_num;

        /// <summary>
        /// The scale denomenator
        /// </summary>
        public int scale_denom; /* fraction by which to scale image */

        internal int jpeg_width;  /* scaled JPEG image width */
        internal int jpeg_height; /* scaled JPEG image height */
        /* Dimensions of actual JPEG image that will be written to file,
         * derived from input dimensions by scaling factors above.
         * These fields are computed by jpeg_start_compress().
         * You can also use jpeg_calc_jpeg_dimensions() to determine these values
         * in advance of calling jpeg_start_compress().
         */

        internal int m_data_precision;     /* bits of precision in image data */
        internal int m_num_components;     /* # of color components in JPEG image */
        internal J_COLOR_SPACE m_jpeg_color_space; /* colorspace of JPEG image */

        /* comp_info[i] describes component that appears i'th in SOF */
        private jpeg_component_info[] m_comp_info;

        /* ptrs to coefficient quantization tables, or null if not defined */
        internal JQUANT_TBL[] m_quant_tbl_ptrs = new JQUANT_TBL[JpegConstants.NUM_QUANT_TBLS];

        /// <summary>
        /// corresponding scale factors (percentage, initialized 100).
        /// </summary>
        public int[] q_scale_factor = new int[JpegConstants.NUM_QUANT_TBLS];

        /* ptrs to Huffman coding tables, or null if not defined */
        internal JHUFF_TBL[] m_dc_huff_tbl_ptrs = new JHUFF_TBL[JpegConstants.NUM_HUFF_TBLS];
        internal JHUFF_TBL[] m_ac_huff_tbl_ptrs = new JHUFF_TBL[JpegConstants.NUM_HUFF_TBLS];

        internal byte[] arith_dc_L = new byte[JpegConstants.NUM_ARITH_TBLS]; /* L values for DC arith-coding tables */
        internal byte[] arith_dc_U = new byte[JpegConstants.NUM_ARITH_TBLS]; /* U values for DC arith-coding tables */
        internal byte[] arith_ac_K = new byte[JpegConstants.NUM_ARITH_TBLS]; /* Kx values for AC arith-coding tables */

        /* The default value of scan_info is null, which causes a single-scan
         * sequential JPEG file to be emitted.  To create a multi-scan file,
         * set num_scans and scan_info to point to an array of scan definitions.
         */
        internal int m_num_scans;      /* # of entries in scan_info array */

        /* script for multi-scan file, or null */
        internal jpeg_scan_info[] m_scan_info;

        internal bool m_raw_data_in;       /* true=caller supplies downsampled data */
        internal bool arith_code;		   /* true=arithmetic coding, false=Huffman */
        internal bool m_optimize_coding;   /* true=optimize entropy encoding parms */
        internal bool m_CCIR601_sampling;  /* true=first samples are cosited */

        /// <summary>
        /// TRUE=apply fancy downsampling
        /// </summary>
        public bool do_fancy_downsampling;
        internal int m_smoothing_factor;       /* 1..100, or 0 for no input smoothing */
        internal J_DCT_METHOD m_dct_method;    /* DCT algorithm selector */

        internal int m_restart_interval; /* MCUs per restart, or 0 for no restart */
        internal int m_restart_in_rows;        /* if > 0, MCU rows per restart interval */

        internal bool m_write_JFIF_header; /* should a JFIF marker be written? */
        internal byte m_JFIF_major_version;   /* What to write for the JFIF version number */
        internal byte m_JFIF_minor_version;

        internal DensityUnit m_density_unit;     /* JFIF code for pixel size units */
        internal short m_X_density;       /* Horizontal pixel density */
        internal short m_Y_density;       /* Vertical pixel density */
        internal bool m_write_Adobe_marker;    /* should an Adobe marker be written? */

        /// <summary>
        /// Color transform identifier, writes LSE marker if nonzero
        /// </summary>
        public J_COLOR_TRANSFORM color_transform;

        internal int m_next_scanline;   /* 0 .. image_height-1  */

        /* Remaining fields are known throughout compressor, but generally
         * should not be touched by a surrounding application.
         */

        /*
         * These fields are computed during compression startup
         */
        internal bool m_progressive_mode;  /* true if scan script uses progressive mode */
        internal int m_max_h_samp_factor;  /* largest h_samp_factor */
        internal int m_max_v_samp_factor;  /* largest v_samp_factor */

        internal int min_DCT_h_scaled_size;  /* smallest DCT_h_scaled_size of any component */
        internal int min_DCT_v_scaled_size;	/* smallest DCT_v_scaled_size of any component */

        internal int m_total_iMCU_rows; /* # of iMCU rows to be input to coef ctlr */
        /* The coefficient controller receives data in units of MCU rows as defined
         * for fully interleaved scans (whether the JPEG file is interleaved or not).
         * There are v_samp_factor * DCTSIZE sample rows of each component in an
         * "iMCU" (interleaved MCU) row.
         */

        /*
         * These fields are valid during any one scan.
         * They describe the components and MCUs actually appearing in the scan.
         */
        internal int m_comps_in_scan;      /* # of JPEG components in this scan */
        internal int[] m_cur_comp_info = new int[JpegConstants.MAX_COMPS_IN_SCAN];
        /* *cur_comp_info[i] is index of m_comp_info that describes component that appears i'th in SOS */

        internal int m_MCUs_per_row;    /* # of MCUs across the image */
        internal int m_MCU_rows_in_scan;    /* # of MCU rows in the image */

        internal int m_blocks_in_MCU;      /* # of DCT blocks per MCU */
        internal int[] m_MCU_membership = new int[JpegConstants.C_MAX_BLOCKS_IN_MCU];
        /* MCU_membership[i] is index in cur_comp_info of component owning */
        /* i'th block in an MCU */

        /* progressive JPEG parameters for scan */
        internal int m_Ss;
        internal int m_Se;
        internal int m_Ah;
        internal int m_Al;

        /// <summary>
        /// the basic DCT block size: 1..16
        /// </summary>
        public int block_size;

        internal int[] natural_order;   /* natural-order position array */
        internal int lim_Se;			/* min( Se, DCTSIZE2-1 ) */

        /*
         * Links to compression subobjects (methods and private variables of modules)
         */
        internal jpeg_comp_master m_master;
        internal jpeg_c_main_controller m_main;
        internal jpeg_c_prep_controller m_prep;
        internal jpeg_c_coef_controller m_coef;
        internal jpeg_marker_writer m_marker;
        internal jpeg_color_converter m_cconvert;
        internal jpeg_downsampler m_downsample;
        internal jpeg_forward_dct m_fdct;
        internal jpeg_entropy_encoder m_entropy;
        internal jpeg_scan_info[] m_script_space; /* workspace for jpeg_simple_progression */
        internal int m_script_space_size;

        /// <summary>
        /// Initializes a new instance of the <see cref="jpeg_compress_struct"/> class.
        /// </summary>
        public jpeg_compress_struct()
        {
            initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="jpeg_compress_struct"/> class.
        /// </summary>
        /// <param name="errorManager">The error manager.</param>
        public jpeg_compress_struct(jpeg_error_mgr errorManager)
            : base(errorManager)
        {
            initialize();
        }

        /// <summary>
        /// Retrieves <c>false</c> because this is not decompressor.
        /// </summary>
        /// <value><c>false</c></value>
        public override bool IsDecompressor
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the destination for compressed data
        /// </summary>
        /// <value>The destination for compressed data.</value>
        public LibJpeg.Classic.jpeg_destination_mgr Dest
        {
            get { return m_dest; }
            set { m_dest = value; }
        }

        /* Description of source image --- these fields must be filled in by
         * outer application before starting compression.  in_color_space must
         * be correct before you can even call jpeg_set_defaults().
         */

        /// <summary>
        /// Gets or sets the width of image, in pixels.
        /// </summary>
        /// <value>The width of image.</value>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        public int Image_width
        {
            get { return m_image_width; }
            set { m_image_width = value; }
        }

        /// <summary>
        /// Gets or sets the height of image, in pixels.
        /// </summary>
        /// <value>The height of image.</value>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        public int Image_height
        {
            get { return m_image_height; }
            set { m_image_height = value; }
        }

        /// <summary>
        /// Gets or sets the number of color channels (components per pixel)
        /// </summary>
        /// <value>The number of color channels.</value>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        public int Input_components
        {
            get { return m_input_components; }
            set { m_input_components = value; }
        }

        /// <summary>
        /// Gets or sets the color space of source image.
        /// </summary>
        /// <value>The color space.</value>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        /// <seealso href="../articles/KB/special-color-spaces.html">Special color spaces</seealso>
        public LibJpeg.Classic.J_COLOR_SPACE In_color_space
        {
            get { return m_in_color_space; }
            set { m_in_color_space = value; }
        }

        /* Compression parameters --- these fields must be set before calling
         * jpeg_start_compress().  We recommend calling jpeg_set_defaults() to
         * initialize everything to reasonable defaults, then changing anything
         * the application specifically wants to change.  That way you won't get
         * burnt when new parameters are added.  Also note that there are several
         * helper routines to simplify changing parameters.
         */

        // bits of precision in image data


        /// <summary>
        /// Gets or sets the number of bits of precision in image data.
        /// </summary>
        /// <remarks>Default value: 8<br/>
        /// The number of bits.
        /// </remarks>
        /// <value>The data precision.</value>
        public int Data_precision
        {
            get { return m_data_precision; }
            set { m_data_precision = value; }
        }

        /// <summary>
        /// Gets or sets the number of color components for JPEG color space.
        /// </summary>
        /// <value>The number of color components for JPEG color space.</value>
        public int Num_components
        {
            get { return m_num_components; }
            set { m_num_components = value; }
        }

        /// <summary>
        /// Gets or sets the JPEG color space.
        /// </summary>
        /// <remarks>We recommend to use <see cref="jpeg_set_colorspace"/> if you want to change this.</remarks>
        /// <value>The JPEG color space.</value>
        public J_COLOR_SPACE Jpeg_color_space
        {
            get { return m_jpeg_color_space; }
            set { m_jpeg_color_space = value; }
        }

        // true=caller supplies downsampled data


        /// <summary>
        /// Gets or sets a value indicating whether you will be supplying raw data.
        /// </summary>
        /// <remarks>Default value: <c>false</c></remarks>
        /// <value><c>true</c> if you will be supplying raw data; otherwise, <c>false</c>.</value>
        /// <seealso cref="jpeg_compress_struct.jpeg_write_raw_data"/>
        public bool Raw_data_in
        {
            get { return m_raw_data_in; }
            set { m_raw_data_in = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating a way of using Huffman coding tables.
        /// </summary>
        /// <remarks>When this is <c>true</c>, you need not supply Huffman tables at all, and any you do supply will be overwritten.</remarks>
        /// <value><c>true</c> causes the compressor to compute optimal Huffman coding tables 
        /// for the image. This requires an extra pass over the data and therefore costs a good 
        /// deal of space and time. The default is <c>false</c>, which tells the compressor to use the 
        /// supplied or default Huffman tables. In most cases optimal tables save only a few 
        /// percent of file size compared to the default tables.</value>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public bool Optimize_coding
        {
            get { return m_optimize_coding; }
            set { m_optimize_coding = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether first samples are cosited.
        /// </summary>
        /// <value><c>true</c> if first samples are cosited; otherwise, <c>false</c>.</value>
        public bool CCIR601_sampling
        {
            get { return m_CCIR601_sampling; }
            set { m_CCIR601_sampling = value; }
        }

        /// <summary>
        /// Gets or sets the coefficient of image smoothing.
        /// </summary>
        /// <remarks>Default value: 0<br/>
        /// If non-zero, the input image is smoothed; the value should be 1 for minimal smoothing 
        /// to 100 for maximum smoothing.</remarks>
        /// <value>The coefficient of image smoothing.</value>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public int Smoothing_factor
        {
            get { return m_smoothing_factor; }
            set { m_smoothing_factor = value; }
        }

        /// <summary>
        /// Gets or sets the algorithm used for the DCT step.
        /// </summary>
        /// <value>The DCT algorithm.</value>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public J_DCT_METHOD Dct_method
        {
            get { return m_dct_method; }
            set { m_dct_method = value; }
        }

        /* The restart interval can be specified in absolute MCUs by setting
         * restart_interval, or in MCU rows by setting restart_in_rows
         * (in which case the correct restart_interval will be figured
         * for each scan).
         */

        /// <summary>
        /// Gets or sets the exact interval in MCU blocks.
        /// </summary>
        /// <remarks>Default value: 0<br/>
        /// One restart marker per MCU row is often a good choice. The overhead of restart markers 
        /// is higher in grayscale JPEG files than in color files, and MUCH higher in progressive JPEGs. 
        /// If you use restarts, you may want to use larger intervals in those cases.</remarks>
        /// <value>The restart interval.</value>
        /// <seealso cref="jpeg_compress_struct.Restart_in_rows"/>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public int Restart_interval
        {
            get { return m_restart_interval; }
            set { m_restart_interval = value; }
        }

        /// <summary>
        /// Gets or sets the interval in MCU rows.
        /// </summary>
        /// <remarks>Default value: 0<br/>
        /// If Restart_in_rows is not 0, then <see cref="jpeg_compress_struct.Restart_interval"/> is set 
        /// after the image width in MCUs is computed.<br/>
        /// One restart marker per MCU row is often a good choice. 
        /// The overhead of restart markers is higher in grayscale JPEG files than in color files, and MUCH higher in progressive JPEGs. If you use restarts, you may want to use larger intervals in those cases.
        /// </remarks>
        /// <value>The restart interval in MCU rows.</value>
        /// <seealso cref="jpeg_compress_struct.Restart_interval"/>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public int Restart_in_rows
        {
            get { return m_restart_in_rows; }
            set { m_restart_in_rows = value; }
        }

        /* Parameters controlling emission of special markers. */

        /// <summary>
        /// Gets or sets a value indicating whether the JFIF APP0 marker is emitted.
        /// </summary>
        /// <remarks><see cref="jpeg_compress_struct.jpeg_set_defaults"/> and 
        /// <see cref="jpeg_compress_struct.jpeg_set_colorspace"/> set this <c>true</c> 
        /// if a JFIF-legal JPEG color space (i.e., YCbCr or grayscale) is selected, otherwise <c>false</c>.</remarks>
        /// <value><c>true</c> if JFIF APP0 marker is emitted; otherwise, <c>false</c>.</value>
        /// <seealso cref="jpeg_compress_struct.JFIF_major_version"/>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public bool Write_JFIF_header
        {
            get { return m_write_JFIF_header; }
            set { m_write_JFIF_header = value; }
        }

        /// <summary>
        /// Gets or sets the version number to be written into the JFIF marker.
        /// </summary>
        /// <remarks><see cref="jpeg_compress_struct.jpeg_set_defaults"/> initializes the version to 
        /// 1.01 (major=minor=1). You should set it to 1.02 (major=1, minor=2) if you plan to write any 
        /// JFIF 1.02 extension markers.</remarks>
        /// <value>The version number to be written into the JFIF marker.</value>
        /// <seealso cref="jpeg_compress_struct.JFIF_minor_version"/>
        /// <seealso cref="jpeg_compress_struct.Write_JFIF_header"/>
        public byte JFIF_major_version
        {
            get { return m_JFIF_major_version; }
            set { m_JFIF_major_version = value; }
        }

        /// <summary>
        /// Gets or sets the version number to be written into the JFIF marker.
        /// </summary>
        /// <remarks><see cref="jpeg_compress_struct.jpeg_set_defaults"/> initializes the version to 
        /// 1.01 (major=minor=1). You should set it to 1.02 (major=1, minor=2) if you plan to write any 
        /// JFIF 1.02 extension markers.</remarks>
        /// <value>The version number to be written into the JFIF marker.</value>
        /// <seealso cref="jpeg_compress_struct.JFIF_major_version"/>
        /// <seealso cref="jpeg_compress_struct.Write_JFIF_header"/>
        public byte JFIF_minor_version
        {
            get { return m_JFIF_minor_version; }
            set { m_JFIF_minor_version = value; }
        }

        /* These three values are not used by the JPEG code, merely copied */
        /* into the JFIF APP0 marker.  density_unit can be 0 for unknown, */
        /* 1 for dots/inch, or 2 for dots/cm.  Note that the pixel aspect */
        /* ratio is defined by X_density/Y_density even when density_unit=0. */

        /// <summary>
        /// Gets or sets the resolution information to be written into the JFIF marker; not used otherwise.
        /// </summary>
        /// <remarks>Default value: <see cref="F:BitMiracle.LibJpeg.Classic.DensityUnit.Unknown"/><br/>
        /// The pixel aspect ratio is defined by 
        /// <see cref="jpeg_compress_struct.X_density"/>/<see cref="jpeg_compress_struct.Y_density"/> 
        /// even when Density_unit is <see cref="F:BitMiracle.LibJpeg.Classic.DensityUnit.Unknown">Unknown</see>.</remarks>
        /// <value>The density unit.</value>
        /// <seealso cref="jpeg_compress_struct.X_density"/>
        /// <seealso cref="jpeg_compress_struct.Y_density"/>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public DensityUnit Density_unit
        {
            get { return m_density_unit; }
            set { m_density_unit = value; }
        }

        // Horizontal pixel density

        /// <summary>
        /// Gets or sets the horizontal component of pixel ratio.
        /// </summary>
        /// <remarks>Default value: 1</remarks>
        /// <value>The horizontal density.</value>
        /// <seealso cref="jpeg_compress_struct.Density_unit"/>
        /// <seealso cref="jpeg_compress_struct.Y_density"/>
        public short X_density
        {
            get { return m_X_density; }
            set { m_X_density = value; }
        }

        /// <summary>
        /// Gets or sets the vertical component of pixel ratio.
        /// </summary>
        /// <remarks>Default value: 1</remarks>
        /// <value>The vertical density.</value>
        /// <seealso cref="jpeg_compress_struct.Density_unit"/>
        /// <seealso cref="jpeg_compress_struct.X_density"/>
        public short Y_density
        {
            get { return m_Y_density; }
            set { m_Y_density = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to emit Adobe APP14 marker.
        /// </summary>
        /// <remarks><see cref="jpeg_compress_struct.jpeg_set_defaults"/> and <see cref="jpeg_compress_struct.jpeg_set_colorspace"/> 
        /// set this <c>true</c> if JPEG color space RGB, CMYK, or YCCK is selected, otherwise <c>false</c>. 
        /// It is generally a bad idea to set both <see cref="jpeg_compress_struct.Write_JFIF_header"/> and 
        /// <see cref="jpeg_compress_struct.Write_Adobe_marker"/>. 
        /// In fact, you probably shouldn't change the default settings at all - the default behavior ensures that the JPEG file's 
        /// color space can be recognized by the decoder.</remarks>
        /// <value>If <c>true</c> an Adobe APP14 marker is emitted; <c>false</c>, otherwise.</value>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public bool Write_Adobe_marker
        {
            get { return m_write_Adobe_marker; }
            set { m_write_Adobe_marker = value; }
        }

        /// <summary>
        /// Gets the largest vertical sample factor.
        /// </summary>
        /// <value>The largest vertical sample factor.</value>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public int Max_v_samp_factor
        {
            get { return m_max_v_samp_factor; }
        }

        /// <summary>
        /// Gets the components that appears in SOF.
        /// </summary>
        /// <value>The component info array.</value>
        public jpeg_component_info[] Component_info
        {
            get { return m_comp_info; }
        }

        /* ptrs to coefficient quantization tables, or null if not defined */
        /* ptrs to coefficient quantization tables, or null if not defined */
        /// <summary>
        /// Gets the coefficient quantization tables.
        /// </summary>
        /// <value>The coefficient quantization tables or null if not defined.</value>
        public JQUANT_TBL[] Quant_tbl_ptrs
        {
            get { return m_quant_tbl_ptrs; }
        }

        /// <summary>
        /// Gets the Huffman coding tables.
        /// </summary>
        /// <value>The Huffman coding tables or null if not defined.</value>
        public JHUFF_TBL[] Dc_huff_tbl_ptrs
        {
            get { return m_dc_huff_tbl_ptrs; }
        }

        /// <summary>
        /// Gets the Huffman coding tables.
        /// </summary>
        /// <value>The Huffman coding tables or null if not defined.</value>
        public JHUFF_TBL[] Ac_huff_tbl_ptrs
        {
            get { return m_ac_huff_tbl_ptrs; }
        }

        /// <summary>
        /// Gets the index of next scanline to be written to <see cref="jpeg_compress_struct.jpeg_write_scanlines"/>.
        /// </summary>
        /// <remarks>Application may use this to control its processing loop, 
        /// e.g., "while (Next_scanline &lt; Image_height)"</remarks>
        /// <value>Range: from 0 to (Image_height - 1)</value>
        /// <seealso cref="jpeg_compress_struct.jpeg_write_scanlines"/>
        public int Next_scanline
        {
            get { return m_next_scanline; }
        }

        /// <summary>
        /// Abort processing of a JPEG compression operation.
        /// </summary>
        public void jpeg_abort_compress()
        {
            // use common routine
            jpeg_abort();
        }

        /// <summary>
        /// Forcibly suppress or un-suppress all quantization and Huffman tables.
        /// </summary>
        /// <remarks>Marks all currently defined tables as already written (if suppress)
        /// or not written (if !suppress). This will control whether they get 
        /// emitted by a subsequent <see cref="jpeg_compress_struct.jpeg_start_compress"/> call.<br/>
        /// 
        /// This routine is exported for use by applications that want to produce
        /// abbreviated JPEG datastreams.</remarks>
        /// <param name="suppress">if set to <c>true</c> then suppress tables; 
        /// otherwise unsuppress.</param>
        public void jpeg_suppress_tables(bool suppress)
        {
            for (int i = 0; i < JpegConstants.NUM_QUANT_TBLS; i++)
            {
                if (m_quant_tbl_ptrs[i] != null)
                    m_quant_tbl_ptrs[i].Sent_table = suppress;
            }

            for (int i = 0; i < JpegConstants.NUM_HUFF_TBLS; i++)
            {
                if (m_dc_huff_tbl_ptrs[i] != null)
                    m_dc_huff_tbl_ptrs[i].Sent_table = suppress;

                if (m_ac_huff_tbl_ptrs[i] != null)
                    m_ac_huff_tbl_ptrs[i].Sent_table = suppress;
            }
        }

        /// <summary>
        /// Finishes JPEG compression.
        /// </summary>
        /// <remarks>If a multipass operating mode was selected, this may do a great 
        /// deal of work including most of the actual output.</remarks>
        public void jpeg_finish_compress()
        {
            int iMCU_row;

            if (m_global_state == JpegState.CSTATE_SCANNING || m_global_state == JpegState.CSTATE_RAW_OK)
            {
                /* Terminate first pass */
                if (m_next_scanline < m_image_height)
                    ERREXIT(J_MESSAGE_CODE.JERR_TOO_LITTLE_DATA);
                m_master.finish_pass();
            }
            else if (m_global_state != JpegState.CSTATE_WRCOEFS)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            /* Perform any remaining passes */
            while (!m_master.IsLastPass())
            {
                m_master.prepare_for_pass();
                for (iMCU_row = 0; iMCU_row < m_total_iMCU_rows; iMCU_row++)
                {
                    if (m_progress != null)
                    {
                        m_progress.Pass_counter = iMCU_row;
                        m_progress.Pass_limit = m_total_iMCU_rows;
                        m_progress.Updated();
                    }

                    /* We bypass the main controller and invoke coef controller directly;
                    * all work is being done from the coefficient buffer.
                    */
                    if (!m_coef.compress_data(null))
                        ERREXIT(J_MESSAGE_CODE.JERR_CANT_SUSPEND);
                }

                m_master.finish_pass();
            }

            /* Write EOI, do final cleanup */
            m_marker.write_file_trailer();
            m_dest.term_destination();

            /* We can use jpeg_abort to release memory and reset global_state */
            jpeg_abort();
        }


        /// <summary>
        /// Write a special marker.
        /// </summary>
        /// <remarks>This is only recommended for writing COM or APPn markers. 
        /// Must be called after <see cref="jpeg_compress_struct.jpeg_start_compress"/> and before first call to 
        /// <see cref="jpeg_compress_struct.jpeg_write_scanlines"/> or <see cref="jpeg_compress_struct.jpeg_write_raw_data"/>.
        /// </remarks>
        /// <param name="marker">Specify the marker type parameter as <see cref="JPEG_MARKER"/>.COM for COM or 
        /// <see cref="JPEG_MARKER"/>.APP0 + n for APPn. (Actually, jpeg_write_marker will let you write any marker type, 
        /// but we don't recommend writing any other kinds of marker)</param>
        /// <param name="data">The data associated with the marker.</param>
        /// <seealso href="../articles/KB/special-markers.html">Special markers</seealso>
        /// <seealso cref="JPEG_MARKER"/>
        public void jpeg_write_marker(int marker, byte[] data)
        {
            if (m_next_scanline != 0 || (m_global_state != JpegState.CSTATE_SCANNING && m_global_state != JpegState.CSTATE_RAW_OK && m_global_state != JpegState.CSTATE_WRCOEFS))
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            m_marker.write_marker_header(marker, data.Length);

            for (int i = 0; i < data.Length; i++)
                m_marker.write_marker_byte(data[i]);
        }

        /// <summary>
        /// Writes special marker's header.
        /// </summary>
        /// <param name="marker">Special marker.</param>
        /// <param name="datalen">Length of data associated with the marker.</param>
        /// <remarks>After calling this method you need to call <see cref="jpeg_compress_struct.jpeg_write_m_byte"/>
        /// exactly the number of times given in the length parameter.<br/>
        /// This method lets you empty the output buffer partway through a marker, which might be important when 
        /// using a suspending data destination module. In any case, if you are using a suspending destination, 
        /// you should flush its buffer after inserting any special markers.</remarks>
        /// <seealso cref="jpeg_compress_struct.jpeg_write_m_byte"/>
        /// <seealso cref="jpeg_compress_struct.jpeg_write_marker"/>
        /// <seealso href="../articles/KB/special-markers.html">Special markers</seealso>
        public void jpeg_write_m_header(int marker, int datalen)
        {
            if (m_next_scanline != 0 || (m_global_state != JpegState.CSTATE_SCANNING && m_global_state != JpegState.CSTATE_RAW_OK && m_global_state != JpegState.CSTATE_WRCOEFS))
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            m_marker.write_marker_header(marker, datalen);
        }

        /// <summary>
        /// Writes a byte of special marker's data.
        /// </summary>
        /// <param name="val">The byte of data.</param>
        /// <seealso cref="jpeg_compress_struct.jpeg_write_m_header"/>
        public void jpeg_write_m_byte(byte val)
        {
            m_marker.write_marker_byte(val);
        }

        /// <summary>
        /// Alternate compression function: just write an abbreviated table file.
        /// </summary>
        /// <remarks>Before calling this, all parameters and a data destination must be set up.<br/>
        /// 
        /// To produce a pair of files containing abbreviated tables and abbreviated
        /// image data, one would proceed as follows:<br/>
        /// 
        /// <c>Initialize JPEG object<br/>
        /// Set JPEG parameters<br/>
        /// Set destination to table file<br/>
        /// <see cref="jpeg_compress_struct.jpeg_write_tables">jpeg_write_tables();</see><br/>
        /// Set destination to image file<br/>
        /// <see cref="jpeg_compress_struct.jpeg_start_compress">jpeg_start_compress(false);</see><br/>
        /// Write data...<br/>
        /// <see cref="jpeg_compress_struct.jpeg_finish_compress">jpeg_finish_compress();</see><br/>
        /// </c><br/>
        /// 
        /// jpeg_write_tables has the side effect of marking all tables written
        /// (same as <see cref="jpeg_compress_struct.jpeg_suppress_tables">jpeg_suppress_tables(true)</see>).
        /// Thus a subsequent <see cref="jpeg_compress_struct.jpeg_start_compress">jpeg_start_compress</see> 
        /// will not re-emit the tables unless it is passed <c>write_all_tables=true</c>.
        /// </remarks>
        public void jpeg_write_tables()
        {
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            /* (Re)initialize error mgr and destination modules */
            m_err.reset_error_mgr();
            m_dest.init_destination();

            /* Initialize the marker writer ... bit of a crock to do it here. */
            m_marker = new jpeg_marker_writer(this);

            /* Write them tables! */
            m_marker.write_tables_only();

            /* And clean up. */
            m_dest.term_destination();
        }

        /// <summary>
        /// Sets output stream.
        /// </summary>
        /// <param name="outfile">The output stream.</param>
        /// <remarks>The caller must have already opened the stream, and is responsible
        /// for closing it after finishing compression.</remarks>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        public void jpeg_stdio_dest(Stream outfile)
        {
            m_dest = new my_destination_mgr(this, outfile);
        }

        /// <summary>
        /// Jpeg_set_defaultses this instance.
        /// </summary>
        /// <remarks>Uses only the input image's color space (property <see cref="jpeg_compress_struct.In_color_space"/>, 
        /// which must already be set in <see cref="jpeg_compress_struct"/>). Many applications will only need 
        /// to use this routine and perhaps <see cref="jpeg_compress_struct.jpeg_set_quality"/>.
        /// </remarks>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public void jpeg_set_defaults()
        {
            /* Safety check to ensure start_compress not called yet. */
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            /* Allocate comp_info array large enough for maximum component count.
            * Array is made permanent in case application wants to compress
            * multiple images at same param settings.
            */
            if (m_comp_info == null)
            {
                m_comp_info = jpeg_component_info.createArrayOfComponents(JpegConstants.MAX_COMPONENTS);
            }

            /* Initialize everything not dependent on the color space */
            scale_num = 1;       /* 1:1 scaling */
            scale_denom = 1;
            m_data_precision = JpegConstants.BITS_IN_JSAMPLE;

            /* Set up two quantization tables using default quality of 75 */
            jpeg_set_quality(75, true);

            /* Set up two Huffman tables */
            std_huff_tables();

            /* Default is no multiple-scan output */
            m_scan_info = null;
            m_num_scans = 0;

            /* Expect normal source image, not raw downsampled data */
            m_raw_data_in = false;

            /* The standard Huffman tables are only valid for 8-bit data precision.
             * If the precision is higher, use arithmetic coding.
             * (Alternatively, using Huffman coding would be possible with forcing
             * optimization on so that usable tables will be computed, or by
             * supplying default tables that are valid for the desired precision.)
             * Otherwise, use Huffman coding by default.
             */
            arith_code = (m_data_precision > 8);

            /* By default, don't do extra passes to optimize entropy coding */
            m_optimize_coding = false;

            /* By default, use the simpler non-cosited sampling alignment */
            m_CCIR601_sampling = false;

            /* By default, apply fancy downsampling */
            do_fancy_downsampling = true;

            /* No input smoothing */
            m_smoothing_factor = 0;

            /* DCT algorithm preference */
            m_dct_method = JpegConstants.JDCT_DEFAULT;

            /* No restart markers */
            m_restart_interval = 0;
            m_restart_in_rows = 0;

            /* Fill in default JFIF marker parameters.  Note that whether the marker
            * will actually be written is determined by jpeg_set_colorspace.
            *
            * By default, the library emits JFIF version code 1.01.
            * An application that wants to emit JFIF 1.02 extension markers should set
            * JFIF_minor_version to 2.  We could probably get away with just defaulting
            * to 1.02, but there may still be some decoders in use that will complain
            * about that; saying 1.01 should minimize compatibility problems.
            *
            * For wide gamut colorspaces (BG_RGB and BG_YCC), the major version will be
            * overridden by jpeg_set_colorspace and set to 2.
            */
            m_JFIF_major_version = 1; /* Default JFIF version = 1.01 */
            m_JFIF_minor_version = 1;
            m_density_unit = DensityUnit.Unknown;    /* Pixel size is unknown by default */
            m_X_density = 1;       /* Pixel aspect ratio is square by default */
            m_Y_density = 1;

            /* No color transform */
            color_transform = J_COLOR_TRANSFORM.JCT_NONE;

            /* Choose JPEG colorspace based on input space, set defaults accordingly */
            jpeg_default_colorspace();
        }

        // Compression parameter setup aids

        /// <summary>
        /// Set the JPEG colorspace (property <see cref="jpeg_compress_struct.Jpeg_color_space"/>,
        /// and choose colorspace-dependent parameters appropriately.
        /// </summary>
        /// <param name="colorspace">The required colorspace.</param>
        /// <remarks>See <see href="../articles/KB/special-color-spaces.html">Special color spaces</see>, 
        /// below, before using this. A large number of parameters, including all per-component parameters, 
        /// are set by this routine; if you want to twiddle individual parameters you should call 
        /// <c>jpeg_set_colorspace</c> before rather than after.</remarks>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        /// <seealso href="../articles/KB/special-color-spaces.html">Special color spaces</seealso>
        public void jpeg_set_colorspace(J_COLOR_SPACE colorspace)
        {
            int ci;

            /* Safety check to ensure start_compress not called yet. */
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            /* For all colorspaces, we use Q and Huff tables 0 for luminance components,
            * tables 1 for chrominance components.
            */

            m_jpeg_color_space = colorspace;

            m_write_JFIF_header = false; /* No marker for non-JFIF colorspaces */
            m_write_Adobe_marker = false; /* write no Adobe marker by default */

            switch (colorspace)
            {
                case J_COLOR_SPACE.JCS_UNKNOWN:
                    m_num_components = m_input_components;
                    if (m_num_components < 1 || m_num_components > JpegConstants.MAX_COMPONENTS)
                        ERREXIT(J_MESSAGE_CODE.JERR_COMPONENT_COUNT, m_num_components, JpegConstants.MAX_COMPONENTS);

                    for (ci = 0; ci < m_num_components; ci++)
                        jpeg_set_colorspace_SET_COMP(ci, ci, 1, 1, 0, 0, 0);

                    break;

                case J_COLOR_SPACE.JCS_GRAYSCALE:
                    m_write_JFIF_header = true; /* Write a JFIF marker */
                    m_num_components = 1;
                    /* JFIF specifies component ID 1 */
                    jpeg_set_colorspace_SET_COMP(0, 0x01, 1, 1, 0, 0, 0);
                    break;

                case J_COLOR_SPACE.JCS_RGB:
                    m_write_Adobe_marker = true; /* write Adobe marker to flag RGB */
                    m_num_components = 3;
                    jpeg_set_colorspace_SET_COMP(0, 0x52 /* 'R' */, 1, 1, 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0);
                    jpeg_set_colorspace_SET_COMP(1, 0x47 /* 'G' */, 1, 1, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(2, 0x42 /* 'B' */, 1, 1, 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0);
                    break;

                case J_COLOR_SPACE.JCS_YCbCr:
                    m_write_JFIF_header = true; /* Write a JFIF marker */
                    m_num_components = 3;
                    /* JFIF specifies component IDs 1,2,3 */
                    /* We default to 2x2 subsamples of chrominance */
                    jpeg_set_colorspace_SET_COMP(0, 0x01, 2, 2, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(1, 0x02, 1, 1, 1, 1, 1);
                    jpeg_set_colorspace_SET_COMP(2, 0x03, 1, 1, 1, 1, 1);
                    break;

                case J_COLOR_SPACE.JCS_CMYK:
                    m_write_Adobe_marker = true; /* write Adobe marker to flag CMYK */
                    m_num_components = 4;
                    jpeg_set_colorspace_SET_COMP(0, 0x43 /* 'C' */, 1, 1, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(1, 0x4D /* 'M' */, 1, 1, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(2, 0x59 /* 'Y' */, 1, 1, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(3, 0x4B /* 'K' */, 1, 1, 0, 0, 0);
                    break;

                case J_COLOR_SPACE.JCS_YCCK:
                    m_write_Adobe_marker = true; /* write Adobe marker to flag YCCK */
                    m_num_components = 4;
                    jpeg_set_colorspace_SET_COMP(0, 0x01, 2, 2, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(1, 0x02, 1, 1, 1, 1, 1);
                    jpeg_set_colorspace_SET_COMP(2, 0x03, 1, 1, 1, 1, 1);
                    jpeg_set_colorspace_SET_COMP(3, 0x04, 2, 2, 0, 0, 0);
                    break;

                case J_COLOR_SPACE.JCS_BG_RGB:
                    m_write_JFIF_header = true; /* Write a JFIF marker */
                    JFIF_major_version = 2;   /* Set JFIF major version = 2 */
                    m_num_components = 3;
                    /* Add offset 0x20 to the normal R/G/B component IDs */
                    jpeg_set_colorspace_SET_COMP(0, 0x72 /* 'r' */, 1, 1, 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0);
                    jpeg_set_colorspace_SET_COMP(1, 0x67 /* 'g' */, 1, 1, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(2, 0x62 /* 'b' */, 1, 1, 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0,
                        color_transform == J_COLOR_TRANSFORM.JCT_SUBTRACT_GREEN ? 1 : 0);
                    break;

                case J_COLOR_SPACE.JCS_BG_YCC:
                    m_write_JFIF_header = true; /* Write a JFIF marker */
                    JFIF_major_version = 2;   /* Set JFIF major version = 2 */
                    m_num_components = 3;
                    /* Add offset 0x20 to the normal Cb/Cr component IDs */
                    /* We default to 2x2 subsamples of chrominance */
                    jpeg_set_colorspace_SET_COMP(0, 0x01, 2, 2, 0, 0, 0);
                    jpeg_set_colorspace_SET_COMP(1, 0x22, 1, 1, 1, 1, 1);
                    jpeg_set_colorspace_SET_COMP(2, 0x23, 1, 1, 1, 1, 1);
                    break;

                default:
                    ERREXIT(J_MESSAGE_CODE.JERR_BAD_J_COLORSPACE);
                    break;
            }
        }

        /// <summary>
        /// Select an appropriate JPEG colorspace based on <see cref="jpeg_compress_struct.In_color_space"/>,
        /// and calls <see cref="jpeg_compress_struct.jpeg_set_colorspace"/>
        /// </summary>
        /// <remarks>This is actually a subroutine of <see cref="jpeg_set_defaults"/>. 
        /// It's broken out in case you want to change just the colorspace-dependent JPEG parameters.</remarks>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public void jpeg_default_colorspace()
        {
            switch (m_in_color_space)
            {
                case J_COLOR_SPACE.JCS_UNKNOWN:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_UNKNOWN);
                    break;

                case J_COLOR_SPACE.JCS_GRAYSCALE:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_GRAYSCALE);
                    break;
                case J_COLOR_SPACE.JCS_RGB:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_YCbCr);
                    break;
                case J_COLOR_SPACE.JCS_YCbCr:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_YCbCr);
                    break;
                case J_COLOR_SPACE.JCS_CMYK:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_CMYK); /* By default, no translation */
                    break;
                case J_COLOR_SPACE.JCS_YCCK:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_YCCK);
                    break;
                case J_COLOR_SPACE.JCS_BG_RGB:
                    /* No translation for now -- conversion to BG_YCC not yet supportet */
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_BG_RGB);
                    break;
                case J_COLOR_SPACE.JCS_BG_YCC:
                    jpeg_set_colorspace(J_COLOR_SPACE.JCS_BG_YCC);
                    break;
                default:
                    ERREXIT(J_MESSAGE_CODE.JERR_BAD_IN_COLORSPACE);
                    break;
            }
        }

        /// <summary>
        /// Constructs JPEG quantization tables appropriate for the indicated quality setting.
        /// </summary>
        /// <param name="quality">The quality value is expressed on the 0..100 scale recommended by IJG.</param>
        /// <param name="force_baseline">If <c>true</c>, then the quantization table entries are constrained 
        /// to the range 1..255 for full JPEG baseline compatibility. In the current implementation, 
        /// this only makes a difference for quality settings below 25, and it effectively prevents 
        /// very small/low quality files from being generated. The IJG decoder is capable of reading 
        /// the non-baseline files generated at low quality settings when <c>force_baseline</c> is <c>false</c>,
        /// but other decoders may not be.</param>
        /// <remarks>Note that the exact mapping from quality values to tables may change in future IJG releases 
        /// as more is learned about DCT quantization.</remarks>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public void jpeg_set_quality(int quality, bool force_baseline)
        {
            /* Convert user 0-100 rating to percentage scaling */
            quality = jpeg_quality_scaling(quality);

            /* Set up standard quality tables */
            jpeg_set_linear_quality(quality, force_baseline);
        }

        /// <summary>
        /// Set or change the 'quality' (quantization) setting, using default tables
        /// and straight percentage-scaling quality scales.
        /// This entry point allows different scalings for luminance and chrominance.
        /// </summary>
        /// <param name="force_baseline">if set to <c>true</c> then baseline version is forced.</param>
        public void jpeg_default_qtables(bool force_baseline)
        {
            /* Set up two quantization tables using the specified scaling */
            jpeg_add_quant_table(0, std_luminance_quant_tbl, q_scale_factor[0], force_baseline);
            jpeg_add_quant_table(1, std_chrominance_quant_tbl, q_scale_factor[1], force_baseline);
        }

        /// <summary>
        /// Same as <see cref="jpeg_set_quality"/> except that the generated tables are the 
        /// sample tables given in the JPEG specification section K.1, multiplied by 
        /// the specified scale factor.
        /// </summary>
        /// <param name="scale_factor">The scale_factor.</param>
        /// <param name="force_baseline">If <c>true</c>, then the quantization table entries are 
        /// constrained to the range 1..255 for full JPEG baseline compatibility. In the current 
        /// implementation, this only makes a difference for quality settings below 25, and it 
        /// effectively prevents very small/low quality files from being generated. The IJG decoder 
        /// is capable of reading the non-baseline files generated at low quality settings when 
        /// <c>force_baseline</c> is <c>false</c>, but other decoders may not be.</param>
        /// <remarks>Note that larger scale factors give lower quality. This entry point is 
        /// useful for conforming to the Adobe PostScript DCT conventions, but we do not 
        /// recommend linear scaling as a user-visible quality scale otherwise.
        /// </remarks>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public void jpeg_set_linear_quality(int scale_factor, bool force_baseline)
        {
            /* Set up two quantization tables using the specified scaling */
            jpeg_add_quant_table(0, std_luminance_quant_tbl, scale_factor, force_baseline);
            jpeg_add_quant_table(1, std_chrominance_quant_tbl, scale_factor, force_baseline);
        }

        /// <summary>
        /// Allows an arbitrary quantization table to be created.
        /// </summary>
        /// <param name="which_tbl">Indicates which table slot to fill.</param>
        /// <param name="basic_table">An array of 64 unsigned integers given in normal array order.
        /// These values are multiplied by <c>scale_factor/100</c> and then clamped to the range 1..65535 
        /// (or to 1..255 if <c>force_baseline</c> is <c>true</c>).<br/>
        /// The basic table should be given in JPEG zigzag order.
        /// </param>
        /// <param name="scale_factor">Multiplier for values in <c>basic_table</c>.</param>
        /// <param name="force_baseline">Defines range of values in <c>basic_table</c>. 
        /// If <c>true</c> - 1..255, otherwise - 1..65535.</param>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public void jpeg_add_quant_table(int which_tbl, int[] basic_table, int scale_factor, bool force_baseline)
        {
            /* Safety check to ensure start_compress not called yet. */
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            if (which_tbl < 0 || which_tbl >= JpegConstants.NUM_QUANT_TBLS)
                ERREXIT(J_MESSAGE_CODE.JERR_DQT_INDEX, which_tbl);

            if (m_quant_tbl_ptrs[which_tbl] == null)
                m_quant_tbl_ptrs[which_tbl] = new JQUANT_TBL();

            for (int i = 0; i < JpegConstants.DCTSIZE2; i++)
            {
                int temp = (basic_table[i] * scale_factor + 50) / 100;

                /* limit the values to the valid range */
                if (temp <= 0)
                    temp = 1;

                /* max quantizer needed for 12 bits */
                if (temp > 32767)
                    temp = 32767;

                /* limit to baseline range if requested */
                if (force_baseline && temp > 255)
                    temp = 255;

                m_quant_tbl_ptrs[which_tbl].quantval[i] = (short)temp;
            }

            /* Initialize sent_table false so table will be written to JPEG file. */
            m_quant_tbl_ptrs[which_tbl].Sent_table = false;
        }

        /// <summary>
        /// Converts a value on the IJG-recommended quality scale to a linear scaling percentage.
        /// </summary>
        /// <param name="quality">The IJG-recommended quality scale. Should be 0 (terrible) to 100 (very good).</param>
        /// <returns>The linear scaling percentage.</returns>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public static int jpeg_quality_scaling(int quality)
        {
            /* Safety limit on quality factor.  Convert 0 to 1 to avoid zero divide. */
            if (quality <= 0)
                quality = 1;

            if (quality > 100)
                quality = 100;

            /* The basic table is used as-is (scaling 100) for a quality of 50.
            * Qualities 50..100 are converted to scaling percentage 200 - 2*Q;
            * note that at Q=100 the scaling is 0, which will cause jpeg_add_quant_table
            * to make all the table entries 1 (hence, minimum quantization loss).
            * Qualities 1..50 are converted to scaling percentage 5000/Q.
            */
            if (quality < 50)
                quality = 5000 / quality;
            else
                quality = 200 - quality * 2;

            return quality;
        }

        /// <summary>
        /// Generates a default scan script for writing a progressive-JPEG file.
        /// </summary>
        /// <remarks>This is the recommended method of creating a progressive file, unless you want 
        /// to make a custom scan sequence. You must ensure that the JPEG color space is 
        /// set correctly before calling this routine.</remarks>
        /// <seealso href="../articles/KB/compression-parameter-selection.html">Compression parameter selection</seealso>
        public void jpeg_simple_progression()
        {
            /* Safety check to ensure start_compress not called yet. */
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            /* Figure space needed for script.  Calculation must match code below! */
            int nscans;
            if (m_num_components == 3 &&
                (m_jpeg_color_space == J_COLOR_SPACE.JCS_YCbCr ||
                m_jpeg_color_space == J_COLOR_SPACE.JCS_BG_YCC))
            {
                /* Custom script for YCC color images. */
                nscans = 10;
            }
            else
            {
                /* All-purpose script for other color spaces. */
                if (m_num_components > JpegConstants.MAX_COMPS_IN_SCAN)
                {
                    /* 2 DC + 4 AC scans per component */
                    nscans = 6 * m_num_components;
                }
                else
                {
                    /* 2 DC scans; 4 AC scans per component */
                    nscans = 2 + 4 * m_num_components;
                }
            }

            /* Allocate space for script.
            * We need to put it in the permanent pool in case the application performs
            * multiple compressions without changing the settings.  To avoid a memory
            * leak if jpeg_simple_progression is called repeatedly for the same JPEG
            * object, we try to re-use previously allocated space, and we allocate
            * enough space to handle YCC even if initially asked for grayscale.
            */
            if (m_script_space == null || m_script_space_size < nscans)
            {
                m_script_space_size = Math.Max(nscans, 10);
                m_script_space = new jpeg_scan_info[m_script_space_size];
                for (int i = 0; i < m_script_space_size; i++)
                    m_script_space[i] = new jpeg_scan_info();
            }

            m_scan_info = m_script_space;
            m_num_scans = nscans;

            int scanIndex = 0;
            if (m_num_components == 3 &&
                (m_jpeg_color_space == J_COLOR_SPACE.JCS_YCbCr ||
                m_jpeg_color_space == J_COLOR_SPACE.JCS_BG_YCC))
            {
                /* Custom script for YCC color images. */
                /* Initial DC scan */
                fill_dc_scans(ref scanIndex, m_num_components, 0, 1);

                /* Initial AC scan: get some luma data out in a hurry */
                fill_a_scan(ref scanIndex, 0, 1, 5, 0, 2);

                /* Chroma data is too small to be worth expending many scans on */
                fill_a_scan(ref scanIndex, 2, 1, 63, 0, 1);
                fill_a_scan(ref scanIndex, 1, 1, 63, 0, 1);

                /* Complete spectral selection for luma AC */
                fill_a_scan(ref scanIndex, 0, 6, 63, 0, 2);

                /* Refine next bit of luma AC */
                fill_a_scan(ref scanIndex, 0, 1, 63, 2, 1);

                /* Finish DC successive approximation */
                fill_dc_scans(ref scanIndex, m_num_components, 1, 0);

                /* Finish AC successive approximation */
                fill_a_scan(ref scanIndex, 2, 1, 63, 1, 0);
                fill_a_scan(ref scanIndex, 1, 1, 63, 1, 0);

                /* Luma bottom bit comes last since it's usually largest scan */
                fill_a_scan(ref scanIndex, 0, 1, 63, 1, 0);
            }
            else
            {
                /* All-purpose script for other color spaces. */
                /* Successive approximation first pass */
                fill_dc_scans(ref scanIndex, m_num_components, 0, 1);
                fill_scans(ref scanIndex, m_num_components, 1, 5, 0, 2);
                fill_scans(ref scanIndex, m_num_components, 6, 63, 0, 2);

                /* Successive approximation second pass */
                fill_scans(ref scanIndex, m_num_components, 1, 63, 2, 1);

                /* Successive approximation final pass */
                fill_dc_scans(ref scanIndex, m_num_components, 1, 0);
                fill_scans(ref scanIndex, m_num_components, 1, 63, 1, 0);
            }
        }

        // Main entry points for compression

        /// <summary>
        /// Starts JPEG compression.
        /// </summary>
        /// <param name="write_all_tables">Write or not write all quantization and Huffman tables.</param>
        /// <remarks>Before calling this, all parameters and a data destination must be set up.</remarks>
        /// <seealso cref="jpeg_compress_struct.jpeg_suppress_tables"/>
        /// <seealso cref="jpeg_compress_struct.jpeg_write_tables"/>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        public void jpeg_start_compress(bool write_all_tables)
        {
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            if (write_all_tables)
                jpeg_suppress_tables(false); /* mark all tables to be written */

            /* (Re)initialize error mgr and destination modules */
            m_err.reset_error_mgr();
            m_dest.init_destination();

            /* Perform master selection of active modules */
            jinit_compress_master();

            /* Set up for the first pass */
            m_master.prepare_for_pass();

            /* Ready for application to drive first pass through jpeg_write_scanlines
            * or jpeg_write_raw_data.
            */
            m_next_scanline = 0;
            m_global_state = (m_raw_data_in ? JpegState.CSTATE_RAW_OK : JpegState.CSTATE_SCANNING);
        }

        /// <summary>
        /// Write some scanlines of data to the JPEG compressor.
        /// </summary>
        /// <param name="scanlines">The array of scanlines.</param>
        /// <param name="num_lines">The number of scanlines for writing.</param>
        /// <returns>The return value will be the number of lines actually written.<br/>
        /// This should be less than the supplied <c>num_lines</c> only in case that 
        /// the data destination module has requested suspension of the compressor, 
        /// or if more than image_height scanlines are passed in.
        /// </returns>
        /// <remarks>We warn about excess calls to <c>jpeg_write_scanlines()</c> since this likely 
        /// signals an application programmer error. However, excess scanlines passed in the last 
        /// valid call are "silently" ignored, so that the application need not adjust <c>num_lines</c>
        /// for end-of-image when using a multiple-scanline buffer.</remarks>
        /// <seealso href="../articles/KB/compression-details.html">Compression details</seealso>
        public int jpeg_write_scanlines(byte[][] scanlines, int num_lines)
        {
            if (m_global_state != JpegState.CSTATE_SCANNING)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            if (m_next_scanline >= m_image_height)
                WARNMS(J_MESSAGE_CODE.JWRN_TOO_MUCH_DATA);

            /* Call progress monitor hook if present */
            if (m_progress != null)
            {
                m_progress.Pass_counter = m_next_scanline;
                m_progress.Pass_limit = m_image_height;
                m_progress.Updated();
            }

            /* Give master control module another chance if this is first call to
            * jpeg_write_scanlines.  This lets output of the frame/scan headers be
            * delayed so that application can write COM, etc, markers between
            * jpeg_start_compress and jpeg_write_scanlines.
            */
            if (m_master.MustCallPassStartup())
                m_master.pass_startup();

            /* Ignore any extra scanlines at bottom of image. */
            int rows_left = m_image_height - m_next_scanline;
            if (num_lines > rows_left)
                num_lines = rows_left;

            int row_ctr = 0;
            m_main.process_data(scanlines, ref row_ctr, num_lines);
            m_next_scanline += row_ctr;
            return row_ctr;
        }

        /// <summary>
        /// Alternate entry point to write raw data.
        /// </summary>
        /// <param name="data">The raw data.</param>
        /// <param name="num_lines">The number of scanlines for writing.</param>
        /// <returns>The number of lines actually written.</returns>
        /// <remarks>Processes exactly one iMCU row per call, unless suspended.
        /// Replaces <see cref="jpeg_write_scanlines"/> when writing raw downsampled data.</remarks>
        public int jpeg_write_raw_data(byte[][][] data, int num_lines)
        {
            if (m_global_state != JpegState.CSTATE_RAW_OK)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            if (m_next_scanline >= m_image_height)
            {
                WARNMS(J_MESSAGE_CODE.JWRN_TOO_MUCH_DATA);
                return 0;
            }

            /* Call progress monitor hook if present */
            if (m_progress != null)
            {
                m_progress.Pass_counter = m_next_scanline;
                m_progress.Pass_limit = m_image_height;
                m_progress.Updated();
            }

            /* Give master control module another chance if this is first call to
            * jpeg_write_raw_data.  This lets output of the frame/scan headers be
            * delayed so that application can write COM, etc, markers between
            * jpeg_start_compress and jpeg_write_raw_data.
            */
            if (m_master.MustCallPassStartup())
                m_master.pass_startup();

            /* Verify that at least one iMCU row has been passed. */
            int lines_per_iMCU_row = m_max_v_samp_factor * min_DCT_v_scaled_size;
            if (num_lines < lines_per_iMCU_row)
                ERREXIT(J_MESSAGE_CODE.JERR_BUFFER_SIZE);

            /* Directly compress the row. */
            if (!m_coef.compress_data(data))
            {
                /* If compressor did not consume the whole row, suspend processing. */
                return 0;
            }

            /* OK, we processed one iMCU row. */
            m_next_scanline += lines_per_iMCU_row;
            return lines_per_iMCU_row;
        }

        /// <summary>
        /// Compression initialization for writing raw-coefficient data. Useful for lossless transcoding.
        /// </summary>
        /// <param name="coef_arrays">The virtual arrays need not be filled or even realized at the time 
        /// <c>jpeg_write_coefficients</c> is called; indeed, the virtual arrays typically will be realized 
        /// during this routine and filled afterwards.
        /// </param>
        /// <remarks>Before calling this, all parameters and a data destination must be set up.
        /// Call <see cref="jpeg_finish_compress"/> to actually write the data.
        /// </remarks>
        public void jpeg_write_coefficients(jvirt_array<JBLOCK>[] coef_arrays)
        {
            if (m_global_state != JpegState.CSTATE_START)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_STATE, (int)m_global_state);

            /* Mark all tables to be written */
            jpeg_suppress_tables(false);

            /* (Re)initialize error mgr and destination modules */
            m_err.reset_error_mgr();
            m_dest.init_destination();

            /* Perform master selection of active modules */
            transencode_master_selection(coef_arrays);

            /* Wait for jpeg_finish_compress() call */
            m_next_scanline = 0;   /* so jpeg_write_marker works */
            m_global_state = JpegState.CSTATE_WRCOEFS;
        }

        // Compression module initialization routines

        /*
         * Compute JPEG image dimensions and related values.
         * NOTE: this is exported for possible use by application.
         * Hence it mustn't do anything that can't be done twice.
         */
        /* Do computations that are needed before master selection phase */
        private void jpeg_calc_jpeg_dimensions()
        {
            /* Sanity check on input image dimensions to prevent overflow in
             * following calculation.
             * We do check jpeg_width and jpeg_height in initial_setup below,
             * but image_width and image_height can come from arbitrary data,
             * and we need some space for multiplication by block_size.
             */
            if (((long)m_image_width >> 24) != 0 || ((long)m_image_height >> 24) != 0)
                ERREXIT(J_MESSAGE_CODE.JERR_IMAGE_TOO_BIG, (uint)JpegConstants.JPEG_MAX_DIMENSION);

            /* Compute actual JPEG image dimensions and DCT scaling choices. */
            if (scale_num >= scale_denom * block_size)
            {
                /* Provide block_size/1 scaling */
                jpeg_width = m_image_width * block_size;
                jpeg_height = m_image_height * block_size;
                min_DCT_h_scaled_size = 1;
                min_DCT_v_scaled_size = 1;
            }
            else if (scale_num * 2 >= scale_denom * block_size)
            {
                /* Provide block_size/2 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 2L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 2L);
                min_DCT_h_scaled_size = 2;
                min_DCT_v_scaled_size = 2;
            }
            else if (scale_num * 3 >= scale_denom * block_size)
            {
                /* Provide block_size/3 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 3L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 3L);
                min_DCT_h_scaled_size = 3;
                min_DCT_v_scaled_size = 3;
            }
            else if (scale_num * 4 >= scale_denom * block_size)
            {
                /* Provide block_size/4 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 4L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 4L);
                min_DCT_h_scaled_size = 4;
                min_DCT_v_scaled_size = 4;
            }
            else if (scale_num * 5 >= scale_denom * block_size)
            {
                /* Provide block_size/5 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 5L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 5L);
                min_DCT_h_scaled_size = 5;
                min_DCT_v_scaled_size = 5;
            }
            else if (scale_num * 6 >= scale_denom * block_size)
            {
                /* Provide block_size/6 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 6L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 6L);
                min_DCT_h_scaled_size = 6;
                min_DCT_v_scaled_size = 6;
            }
            else if (scale_num * 7 >= scale_denom * block_size)
            {
                /* Provide block_size/7 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 7L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 7L);
                min_DCT_h_scaled_size = 7;
                min_DCT_v_scaled_size = 7;
            }
            else if (scale_num * 8 >= scale_denom * block_size)
            {
                /* Provide block_size/8 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 8L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 8L);
                min_DCT_h_scaled_size = 8;
                min_DCT_v_scaled_size = 8;
            }
            else if (scale_num * 9 >= scale_denom * block_size)
            {
                /* Provide block_size/9 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 9L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 9L);
                min_DCT_h_scaled_size = 9;
                min_DCT_v_scaled_size = 9;
            }
            else if (scale_num * 10 >= scale_denom * block_size)
            {
                /* Provide block_size/10 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 10L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 10L);
                min_DCT_h_scaled_size = 10;
                min_DCT_v_scaled_size = 10;
            }
            else if (scale_num * 11 >= scale_denom * block_size)
            {
                /* Provide block_size/11 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 11L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 11L);
                min_DCT_h_scaled_size = 11;
                min_DCT_v_scaled_size = 11;
            }
            else if (scale_num * 12 >= scale_denom * block_size)
            {
                /* Provide block_size/12 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 12L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 12L);
                min_DCT_h_scaled_size = 12;
                min_DCT_v_scaled_size = 12;
            }
            else if (scale_num * 13 >= scale_denom * block_size)
            {
                /* Provide block_size/13 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 13L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 13L);
                min_DCT_h_scaled_size = 13;
                min_DCT_v_scaled_size = 13;
            }
            else if (scale_num * 14 >= scale_denom * block_size)
            {
                /* Provide block_size/14 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 14L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 14L);
                min_DCT_h_scaled_size = 14;
                min_DCT_v_scaled_size = 14;
            }
            else if (scale_num * 15 >= scale_denom * block_size)
            {
                /* Provide block_size/15 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 15L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 15L);
                min_DCT_h_scaled_size = 15;
                min_DCT_v_scaled_size = 15;
            }
            else
            {
                /* Provide block_size/16 scaling */
                jpeg_width = (int)JpegUtils.jdiv_round_up((long)m_image_width * block_size, 16L);
                jpeg_height = (int)JpegUtils.jdiv_round_up((long)m_image_height * block_size, 16L);
                min_DCT_h_scaled_size = 16;
                min_DCT_v_scaled_size = 16;
            }
        }

        private void jpeg_calc_trans_dimensions()
        {
            if (min_DCT_h_scaled_size != min_DCT_v_scaled_size)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCTSIZE, min_DCT_h_scaled_size, min_DCT_v_scaled_size);

            block_size = min_DCT_h_scaled_size;
        }

        /// <summary>
        /// Initialization of a JPEG compression object
        /// </summary>
        private void initialize()
        {
            /* Zero out pointers to permanent structures. */
            m_progress = null;
            m_dest = null;
            m_comp_info = null;

            for (int i = 0; i < JpegConstants.NUM_QUANT_TBLS; i++)
            {
                m_quant_tbl_ptrs[i] = null;
                q_scale_factor[i] = 100;
            }

            for (int i = 0; i < JpegConstants.NUM_HUFF_TBLS; i++)
            {
                m_dc_huff_tbl_ptrs[i] = null;
                m_ac_huff_tbl_ptrs[i] = null;
            }

            /* Must do it here for emit_dqt in case jpeg_write_tables is used */
            block_size = JpegConstants.DCTSIZE;
            natural_order = JpegUtils.jpeg_natural_order;
            lim_Se = JpegConstants.DCTSIZE2 - 1;

            m_script_space = null;

            /* OK, I'm ready */
            m_global_state = JpegState.CSTATE_START;
        }

        /// <summary>
        /// Master selection of compression modules.
        /// This is done once at the start of processing an image.  We determine
        /// which modules will be used and give them appropriate initialization calls.
        /// This routine is in charge of selecting the modules to be executed and
        /// making an initialization call to each one.
        /// </summary>
        private void jinit_compress_master()
        {
            /* Sanity check on image dimensions */
            if (m_image_height <= 0 || m_image_width <= 0 || m_input_components <= 0)
                ERREXIT(J_MESSAGE_CODE.JERR_EMPTY_IMAGE);

            /* Initialize master control (includes parameter checking/processing) */
            jinit_c_master_control(false /* full compression */);

            /* Preprocessing */
            if (!m_raw_data_in)
            {
                m_cconvert = new jpeg_color_converter(this);
                m_downsample = new jpeg_downsampler(this);
                m_prep = new jpeg_c_prep_controller(this);
            }

            /* Forward DCT */
            m_fdct = new jpeg_forward_dct(this);

            /* Entropy encoding: either Huffman or arithmetic coding. */
            if (arith_code)
                m_entropy = new arith_entropy_encoder(this);
            else
                m_entropy = new huff_entropy_encoder(this);

            /* Need a full-image coefficient buffer in any multi-pass mode. */
            m_coef = new my_c_coef_controller(this, (bool)(m_num_scans > 1 || m_optimize_coding));
            jinit_c_main_controller(false /* never need full buffer here */);
            m_marker = new jpeg_marker_writer(this);

            /* Write the datastream header (SOI) immediately.
            * Frame and scan headers are postponed till later.
            * This lets application insert special markers after the SOI.
            */
            m_marker.write_file_header();
        }

        /// <summary>
        /// Initialize master compression control.
        /// </summary>
        private void jinit_c_master_control(bool transcode_only)
        {
            /* Validate parameters, determine derived values */
            initial_setup(transcode_only);

            if (m_scan_info != null)
            {
                validate_script();
                if (block_size < JpegConstants.DCTSIZE)
                    reduce_script();
            }
            else
            {
                m_progressive_mode = false;
                m_num_scans = 1;
            }

            if (m_optimize_coding)
            {
                arith_code = false; /* disable arithmetic coding */
            }
            else if (!arith_code &&
                (m_progressive_mode || (block_size > 1 && block_size < JpegConstants.DCTSIZE)))
            {
                /* TEMPORARY HACK ??? */
                /* assume default tables no good for progressive or reduced AC mode */
                m_optimize_coding = true; /* force Huffman optimization */
            }

            m_master = new jpeg_comp_master(this, transcode_only);
        }

        /// <summary>
        /// Initialize main buffer controller.
        /// </summary>
        private void jinit_c_main_controller(bool need_full_buffer)
        {
            /* We don't need to create a buffer in raw-data mode. */
            if (m_raw_data_in)
                return;

            /* Create the buffer.  It holds downsampled data, so each component
            * may be of a different size.
            */
            if (need_full_buffer)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_BUFFER_MODE);
            else
                m_main = new jpeg_c_main_controller(this);
        }

        /// <summary>
        /// Master selection of compression modules for transcoding.
        /// </summary>
        private void transencode_master_selection(jvirt_array<JBLOCK>[] coef_arrays)
        {
            /* Initialize master control (includes parameter checking/processing) */
            jinit_c_master_control(true /* transcode only */);

            /* Entropy encoding: only Huffman or arithmetic coding. */
            if (arith_code)
                m_entropy = new arith_entropy_encoder(this);
            else
                m_entropy = new huff_entropy_encoder(this);

            /* We need a special coefficient buffer controller. */
            m_coef = new my_trans_c_coef_controller(this, coef_arrays);
            m_marker = new jpeg_marker_writer(this);

            /* Write the datastream header (SOI, JFIF) immediately.
            * Frame and scan headers are postponed till later.
            * This lets application insert special markers after the SOI.
            */
            m_marker.write_file_header();
        }

        /// <summary>
        /// Do computations that are needed before master selection phase
        /// </summary>
        private void initial_setup(bool transcode_only)
        {
            if (transcode_only)
                jpeg_calc_trans_dimensions();
            else
                jpeg_calc_jpeg_dimensions();

            /* Sanity check on block_size */
            if (block_size < 1 || block_size > 16)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_DCTSIZE, block_size, block_size);

            /* Derive natural_order from block_size */
            switch (block_size)
            {
                case 2:
                    natural_order = JpegUtils.jpeg_natural_order2;
                    break;

                case 3:
                    natural_order = JpegUtils.jpeg_natural_order3;
                    break;

                case 4:
                    natural_order = JpegUtils.jpeg_natural_order4;
                    break;

                case 5:
                    natural_order = JpegUtils.jpeg_natural_order5;
                    break;

                case 6:
                    natural_order = JpegUtils.jpeg_natural_order6;
                    break;

                case 7:
                    natural_order = JpegUtils.jpeg_natural_order7;
                    break;

                default:
                    natural_order = JpegUtils.jpeg_natural_order;
                    break;
            }

            /* Derive lim_Se from block_size */
            lim_Se = block_size < JpegConstants.DCTSIZE ?
                block_size * block_size - 1 : JpegConstants.DCTSIZE2 - 1;

            /* Sanity check on image dimensions */
            if (jpeg_height <= 0 || jpeg_width <= 0 || m_num_components <= 0)
                ERREXIT(J_MESSAGE_CODE.JERR_EMPTY_IMAGE);

            /* Make sure image isn't bigger than I can handle */
            if (jpeg_height > JpegConstants.JPEG_MAX_DIMENSION ||
                jpeg_width > JpegConstants.JPEG_MAX_DIMENSION)
            {
                ERREXIT(J_MESSAGE_CODE.JERR_IMAGE_TOO_BIG, (uint)JpegConstants.JPEG_MAX_DIMENSION);
            }

            /* Only 8 to 12 bits data precision are supported for DCT based JPEG */
            if (m_data_precision < 8 || m_data_precision > 12)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_PRECISION, m_data_precision);

            /* Check that number of components won't exceed internal array sizes */
            if (m_num_components > JpegConstants.MAX_COMPONENTS)
                ERREXIT(J_MESSAGE_CODE.JERR_COMPONENT_COUNT, m_num_components, JpegConstants.MAX_COMPONENTS);

            /* Compute maximum sampling factors; check factor validity */
            m_max_h_samp_factor = 1;
            m_max_v_samp_factor = 1;
            for (int ci = 0; ci < m_num_components; ci++)
            {
                if (m_comp_info[ci].H_samp_factor <= 0 || m_comp_info[ci].H_samp_factor > JpegConstants.MAX_SAMP_FACTOR ||
                    m_comp_info[ci].V_samp_factor <= 0 || m_comp_info[ci].V_samp_factor > JpegConstants.MAX_SAMP_FACTOR)
                {
                    ERREXIT(J_MESSAGE_CODE.JERR_BAD_SAMPLING);
                }

                m_max_h_samp_factor = Math.Max(m_max_h_samp_factor, m_comp_info[ci].H_samp_factor);
                m_max_v_samp_factor = Math.Max(m_max_v_samp_factor, m_comp_info[ci].V_samp_factor);
            }

            /* Compute dimensions of components */
            for (int ci = 0; ci < m_num_components; ci++)
            {
                jpeg_component_info compptr = m_comp_info[ci];
                /* Fill in the correct component_index value; don't rely on application */
                compptr.Component_index = ci;

                /* In selecting the actual DCT scaling for each component, we try to
                 * scale down the chroma components via DCT scaling rather than downsampling.
                 * This saves time if the downsampler gets to use 1:1 scaling.
                 * Note this code adapts subsampling ratios which are powers of 2.
                 */
                int ssize = 1;
                while (min_DCT_h_scaled_size * ssize <=
                   (do_fancy_downsampling ? JpegConstants.DCTSIZE : JpegConstants.DCTSIZE / 2) &&
                   (m_max_h_samp_factor % (compptr.H_samp_factor * ssize * 2)) == 0)
                {
                    ssize = ssize * 2;
                }

                compptr.DCT_h_scaled_size = min_DCT_h_scaled_size * ssize;
                ssize = 1;
                while (min_DCT_v_scaled_size * ssize <=
                   (do_fancy_downsampling ? JpegConstants.DCTSIZE : JpegConstants.DCTSIZE / 2) &&
                   (m_max_v_samp_factor % (compptr.V_samp_factor * ssize * 2)) == 0)
                {
                    ssize = ssize * 2;
                }

                compptr.DCT_v_scaled_size = min_DCT_v_scaled_size * ssize;

                /* We don't support DCT ratios larger than 2. */
                if (compptr.DCT_h_scaled_size > compptr.DCT_v_scaled_size * 2)
                    compptr.DCT_h_scaled_size = compptr.DCT_v_scaled_size * 2;
                else if (compptr.DCT_v_scaled_size > compptr.DCT_h_scaled_size * 2)
                    compptr.DCT_v_scaled_size = compptr.DCT_h_scaled_size * 2;

                /* Size in DCT blocks */
                compptr.Width_in_blocks = (int)JpegUtils.jdiv_round_up(
                    (long)jpeg_width * compptr.H_samp_factor, (long)m_max_h_samp_factor * block_size);

                compptr.height_in_blocks = (int)JpegUtils.jdiv_round_up(
                    (long)jpeg_height * compptr.V_samp_factor, (long)m_max_v_samp_factor * block_size);

                /* Size in samples */
                compptr.downsampled_width = (int)JpegUtils.jdiv_round_up(
                    (long)jpeg_width * compptr.H_samp_factor * compptr.DCT_h_scaled_size,
                    (long)m_max_h_samp_factor * block_size);

                compptr.downsampled_height = (int)JpegUtils.jdiv_round_up(
                    (long)jpeg_height * compptr.V_samp_factor * compptr.DCT_v_scaled_size,
                    (long)m_max_v_samp_factor * block_size);

                /* Don't need quantization scale after DCT,
                 * until color conversion says otherwise.
                 */
                compptr.component_needed = false;
            }

            /* Compute number of fully interleaved MCU rows (number of times that
             * main controller will call coefficient controller).
             */
            m_total_iMCU_rows = (int)JpegUtils.jdiv_round_up(
                jpeg_height, (long)m_max_v_samp_factor * block_size);
        }

        /// <summary>
        /// Verify that the scan script in scan_info[] is valid; 
        /// also determine whether it uses progressive JPEG, and set progressive_mode.
        /// </summary>
        private void validate_script()
        {
            if (m_num_scans <= 0)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_SCAN_SCRIPT, 0);

            /* For sequential JPEG, all scans must have Ss=0, Se=DCTSIZE2-1;
            * for progressive JPEG, no scan can have this.
            */
            int[][] last_bitpos = new int[JpegConstants.MAX_COMPONENTS][];
            for (int i = 0; i < JpegConstants.MAX_COMPONENTS; i++)
                last_bitpos[i] = new int[JpegConstants.DCTSIZE2];

            bool[] component_sent = new bool[JpegConstants.MAX_COMPONENTS];

            /* -1 until that coefficient has been seen; then last Al for it */
            if (m_scan_info[0].Ss != 0 || m_scan_info[0].Se != JpegConstants.DCTSIZE2 - 1)
            {
                m_progressive_mode = true;
                for (int ci = 0; ci < m_num_components; ci++)
                {
                    for (int coefi = 0; coefi < JpegConstants.DCTSIZE2; coefi++)
                        last_bitpos[ci][coefi] = -1;
                }
            }
            else
            {
                m_progressive_mode = false;
                for (int ci = 0; ci < m_num_components; ci++)
                    component_sent[ci] = false;
            }

            for (int scanno = 1; scanno <= m_num_scans; scanno++)
            {
                jpeg_scan_info scanInfo = m_scan_info[scanno - 1];

                /* Validate component indexes */
                int ncomps = scanInfo.comps_in_scan;
                if (ncomps <= 0 || ncomps > JpegConstants.MAX_COMPS_IN_SCAN)
                    ERREXIT(J_MESSAGE_CODE.JERR_COMPONENT_COUNT, ncomps, JpegConstants.MAX_COMPS_IN_SCAN);

                for (int ci = 0; ci < ncomps; ci++)
                {
                    int thisi = scanInfo.component_index[ci];
                    if (thisi < 0 || thisi >= m_num_components)
                        ERREXIT(J_MESSAGE_CODE.JERR_BAD_SCAN_SCRIPT, scanno);

                    /* Components must appear in SOF order within each scan */
                    if (ci > 0 && thisi <= scanInfo.component_index[ci - 1])
                        ERREXIT(J_MESSAGE_CODE.JERR_BAD_SCAN_SCRIPT, scanno);
                }

                /* Validate progression parameters */
                int Ss = scanInfo.Ss;
                int Se = scanInfo.Se;
                int Ah = scanInfo.Ah;
                int Al = scanInfo.Al;
                if (m_progressive_mode)
                {
                    /* The JPEG spec simply gives the ranges 0..13 for Ah and Al, but that
                    * seems wrong: the upper bound ought to depend on data precision.
                    * Perhaps they really meant 0..N+1 for N-bit precision.
                    * Here we allow 0..10 for 8-bit data; Al larger than 10 results in
                    * out-of-range reconstructed DC values during the first DC scan,
                    * which might cause problems for some decoders.
                    */
                    const int MAX_AH_AL = 10;
                    if (Ss < 0 || Ss >= JpegConstants.DCTSIZE2 || Se < Ss || Se >= JpegConstants.DCTSIZE2 ||
                        Ah < 0 || Ah > MAX_AH_AL || Al < 0 || Al > MAX_AH_AL)
                    {
                        ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);
                    }

                    if (Ss == 0)
                    {
                        if (Se != 0)        /* DC and AC together not OK */
                            ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);
                    }
                    else
                    {
                        if (ncomps != 1)    /* AC scans must be for only one component */
                            ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);
                    }

                    for (int ci = 0; ci < ncomps; ci++)
                    {
                        int lastBitComponentIndex = scanInfo.component_index[ci];
                        if (Ss != 0 && last_bitpos[lastBitComponentIndex][0] < 0) /* AC without prior DC scan */
                            ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);

                        for (int coefi = Ss; coefi <= Se; coefi++)
                        {
                            if (last_bitpos[lastBitComponentIndex][coefi] < 0)
                            {
                                /* first scan of this coefficient */
                                if (Ah != 0)
                                    ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);
                            }
                            else
                            {
                                /* not first scan */
                                if (Ah != last_bitpos[lastBitComponentIndex][coefi] || Al != Ah - 1)
                                    ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);
                            }

                            last_bitpos[lastBitComponentIndex][coefi] = Al;
                        }
                    }
                }
                else
                {
                    /* For sequential JPEG, all progression parameters must be these: */
                    if (Ss != 0 || Se != JpegConstants.DCTSIZE2 - 1 || Ah != 0 || Al != 0)
                        ERREXIT(J_MESSAGE_CODE.JERR_BAD_PROG_SCRIPT, scanno);

                    /* Make sure components are not sent twice */
                    for (int ci = 0; ci < ncomps; ci++)
                    {
                        int thisi = scanInfo.component_index[ci];
                        if (component_sent[thisi])
                            ERREXIT(J_MESSAGE_CODE.JERR_BAD_SCAN_SCRIPT, scanno);

                        component_sent[thisi] = true;
                    }
                }
            }

            /* Now verify that everything got sent. */
            if (m_progressive_mode)
            {
                /* For progressive mode, we only check that at least some DC data
                * got sent for each component; the spec does not require that all bits
                * of all coefficients be transmitted.  Would it be wiser to enforce
                * transmission of all coefficient bits??
                */
                for (int ci = 0; ci < m_num_components; ci++)
                {
                    if (last_bitpos[ci][0] < 0)
                        ERREXIT(J_MESSAGE_CODE.JERR_MISSING_DATA);
                }
            }
            else
            {
                for (int ci = 0; ci < m_num_components; ci++)
                {
                    if (!component_sent[ci])
                        ERREXIT(J_MESSAGE_CODE.JERR_MISSING_DATA);
                }
            }
        }

        /* Adapt scan script for use with reduced block size;
         * assume that script has been validated before.
         */
        private void reduce_script()
        {
            int idxout = 0;
            for (int idxin = 0; idxin < m_num_scans; idxin++)
            {
                /* After skipping, idxout becomes smaller than idxin */
                if (idxin != idxout)
                {
                    /* Copy rest of data;
                     * note we stay in given chunk of allocated memory.
                     */
                    m_scan_info[idxout] = m_scan_info[idxin];
                }

                if (m_scan_info[idxout].Ss > lim_Se)
                {
                    /* Entire scan out of range - skip this entry */
                    continue;
                }

                if (m_scan_info[idxout].Se > lim_Se)
                {
                    /* Limit scan to end of block */
                    m_scan_info[idxout].Se = lim_Se;
                }

                idxout++;
            }

            m_num_scans = idxout;
        }

        // Huffman table setup routines

        /// <summary>
        /// Set up the standard Huffman tables (cf. JPEG standard section K.3)
        /// 
        /// IMPORTANT: these are only valid for 8-bit data precision!
        /// </summary>
        private void std_huff_tables()
        {
            add_huff_table(ref m_dc_huff_tbl_ptrs[0], bits_dc_luminance, val_dc_luminance);
            add_huff_table(ref m_ac_huff_tbl_ptrs[0], bits_ac_luminance, val_ac_luminance);
            add_huff_table(ref m_dc_huff_tbl_ptrs[1], bits_dc_chrominance, val_dc_chrominance);
            add_huff_table(ref m_ac_huff_tbl_ptrs[1], bits_ac_chrominance, val_ac_chrominance);
        }

        /// <summary>
        /// Define a Huffman table
        /// </summary>
        private void add_huff_table(ref JHUFF_TBL htblptr, byte[] bits, byte[] val)
        {
            if (htblptr == null)
                htblptr = new JHUFF_TBL();

            /* Copy the number-of-symbols-of-each-code-length counts */
            Buffer.BlockCopy(bits, 0, htblptr.Bits, 0, htblptr.Bits.Length);

            /* Validate the counts.  We do this here mainly so we can copy the right
            * number of symbols from the val[] array, without risking marching off
            * the end of memory. huff_entropy_encoder will do a more thorough test later.
            */
            int nsymbols = 0;
            for (int len = 1; len <= 16; len++)
                nsymbols += bits[len];

            if (nsymbols < 1 || nsymbols > 256)
                ERREXIT(J_MESSAGE_CODE.JERR_BAD_HUFF_TABLE);

            Buffer.BlockCopy(val, 0, htblptr.Huffval, 0, nsymbols);

            /* Initialize sent_table false so table will be written to JPEG file. */
            htblptr.Sent_table = false;
        }

        /// <summary>
        /// Support routine: generate one scan for specified component
        /// </summary>
        private void fill_a_scan(ref int scanIndex, int ci, int Ss, int Se, int Ah, int Al)
        {
            m_script_space[scanIndex].comps_in_scan = 1;
            m_script_space[scanIndex].component_index[0] = ci;
            m_script_space[scanIndex].Ss = Ss;
            m_script_space[scanIndex].Se = Se;
            m_script_space[scanIndex].Ah = Ah;
            m_script_space[scanIndex].Al = Al;
            scanIndex++;
        }

        /// <summary>
        /// Support routine: generate interleaved DC scan if possible, else N scans
        /// </summary>
        private void fill_dc_scans(ref int scanIndex, int ncomps, int Ah, int Al)
        {
            if (ncomps <= JpegConstants.MAX_COMPS_IN_SCAN)
            {
                /* Single interleaved DC scan */
                m_script_space[scanIndex].comps_in_scan = ncomps;
                for (int ci = 0; ci < ncomps; ci++)
                    m_script_space[scanIndex].component_index[ci] = ci;

                m_script_space[scanIndex].Ss = 0;
                m_script_space[scanIndex].Se = 0;
                m_script_space[scanIndex].Ah = Ah;
                m_script_space[scanIndex].Al = Al;
                scanIndex++;
            }
            else
            {
                /* Noninterleaved DC scan for each component */
                fill_scans(ref scanIndex, ncomps, 0, 0, Ah, Al);
            }
        }

        /// <summary>
        /// Support routine: generate one scan for each component
        /// </summary>
        private void fill_scans(ref int scanIndex, int ncomps, int Ss, int Se, int Ah, int Al)
        {
            for (int ci = 0; ci < ncomps; ci++)
            {
                m_script_space[scanIndex].comps_in_scan = 1;
                m_script_space[scanIndex].component_index[0] = ci;
                m_script_space[scanIndex].Ss = Ss;
                m_script_space[scanIndex].Se = Se;
                m_script_space[scanIndex].Ah = Ah;
                m_script_space[scanIndex].Al = Al;
                scanIndex++;
            }
        }

        private void jpeg_set_colorspace_SET_COMP(int index, int id, int hsamp, int vsamp, int quant, int dctbl, int actbl)
        {
            m_comp_info[index].Component_id = id;
            m_comp_info[index].H_samp_factor = hsamp;
            m_comp_info[index].V_samp_factor = vsamp;
            m_comp_info[index].Quant_tbl_no = quant;
            m_comp_info[index].Dc_tbl_no = dctbl;
            m_comp_info[index].Ac_tbl_no = actbl;
        }
    }
}
