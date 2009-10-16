/*
 * TIFF Library
 *
 * JPEG Compression support per TIFF Technical Note #2
 * (*not* per the original TIFF 6.0 spec).
 *
 * This file is simply an interface to the libjpeg library written by
 * the Independent JPEG Group.  You need release 5 or later of the IJG
 * code, which you can find on the Internet at ftp.uu.net:/graphics/jpeg/.
 *
 * Contributed by Tom Lane <tgl@sss.pgh.pa.us>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Internal
{
    class JpegCodec : TiffCodec
    {        
        public const int FIELD_JPEGTABLES = (FIELD.FIELD_CODEC + 0);
        public const int FIELD_RECVPARAMS = (FIELD.FIELD_CODEC + 1);
        public const int FIELD_SUBADDRESS = (FIELD.FIELD_CODEC + 2);
        public const int FIELD_RECVTIME = (FIELD.FIELD_CODEC + 3);
        public const int FIELD_FAXDCS = (FIELD.FIELD_CODEC + 4);

        internal jpeg_compress_struct m_compression;
        internal jpeg_decompress_struct m_decompression;
        internal jpeg_common_struct m_common;

        internal UInt16 m_h_sampling; /* luminance sampling factors */
        internal UInt16 m_v_sampling;

        /* pseudo-tag fields */
        internal byte[] m_jpegtables; /* JPEGTables tag value, or null */
        internal uint m_jpegtables_length; /* number of bytes in same */
        internal int m_jpegquality; /* Compression quality level */
        internal JPEGCOLORMODE m_jpegcolormode; /* Auto RGB<=>YCbCr convert? */
        internal JPEGTABLESMODE m_jpegtablesmode; /* What to put in JPEGTables */

        internal bool m_ycbcrsampling_fetched;

        internal uint m_recvparams; /* encoded Class 2 session params */
        internal string m_subaddress; /* subaddress string */
        internal uint m_recvtime; /* time spent receiving (secs) */
        internal string m_faxdcs; /* encoded fax parameters (DCS, Table 2/T.30) */

        private static TiffFieldInfo[] jpegFieldInfo = 
        {
            new TiffFieldInfo(TIFFTAG.TIFFTAG_JPEGTABLES, -3, -3, TiffDataType.TIFF_UNDEFINED, FIELD_JPEGTABLES, false, true, "JPEGTables"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_JPEGQUALITY, 0, 0, TiffDataType.TIFF_ANY, FIELD.FIELD_PSEUDO, true, false, ""), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_JPEGCOLORMODE, 0, 0, TiffDataType.TIFF_ANY, FIELD.FIELD_PSEUDO, false, false, ""), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_JPEGTABLESMODE, 0, 0, TiffDataType.TIFF_ANY, FIELD.FIELD_PSEUDO, false, false, ""), 
            /* Specific for JPEG in faxes */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FAXRECVPARAMS, 1, 1, TiffDataType.TIFF_LONG, FIELD_RECVPARAMS, true, false, "FaxRecvParams"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FAXSUBADDRESS, -1, -1, TiffDataType.TIFF_ASCII, FIELD_SUBADDRESS, true, false, "FaxSubAddress"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FAXRECVTIME, 1, 1, TiffDataType.TIFF_LONG, FIELD_RECVTIME, true, false, "FaxRecvTime"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FAXDCS, -1, -1, TiffDataType.TIFF_ASCII, FIELD_FAXDCS, true, false, "FaxDcs"), 
        };

        private bool m_rawDecode;
        private bool m_rawEncode;

        private TiffTagMethods m_tagMethods;
        private TiffTagMethods m_parentTagMethods;
        
        private bool m_cinfo_initialized;

        private jpeg_error_mgr m_err; /* libjpeg error manager */
        private PHOTOMETRIC m_photometric; /* copy of PhotometricInterpretation */
        
        private int m_bytesperline; /* decompressed bytes per scanline */
        /* pointers to intermediate buffers when processing downsampled data */
        private byte[][][] m_ds_buffer = new byte [JpegConstants.MAX_COMPONENTS][][];
        private int m_scancount; /* number of "scanlines" accumulated */
        private int m_samplesperclump;

        public JpegCodec(Tiff tif, COMPRESSION scheme, string name)
            : base(tif, scheme, name)
        {
            m_tagMethods = new JpegCodecTagMethods();
        }

        public override bool Init()
        {
            Debug.Assert(m_scheme == COMPRESSION.COMPRESSION_JPEG);

            /*
            * Merge codec-specific tag information and override parent get/set
            * field methods.
            */
            m_tif.MergeFieldInfo(jpegFieldInfo, jpegFieldInfo.Length);

            /*
             * Allocate state block so tag methods have storage to record values.
             */
            m_compression = null;
            m_decompression = null;
            m_photometric = 0;
            m_h_sampling = 0;
            m_v_sampling = 0;
            m_bytesperline = 0;
            m_scancount = 0;
            m_samplesperclump = 0;
            m_recvtime = 0;
            m_err = new JpegErrorManager(m_tif);

            m_parentTagMethods = m_tif.m_tagmethods;
            m_tif.m_tagmethods = m_tagMethods;

            /* Default values for codec-specific fields */
            m_jpegtables = null;
            m_jpegtables_length = 0;
            m_jpegquality = 75; /* Default IJG quality */
            m_jpegcolormode = JPEGCOLORMODE.JPEGCOLORMODE_RAW;
            m_jpegtablesmode = JPEGTABLESMODE.JPEGTABLESMODE_QUANT |JPEGTABLESMODE.JPEGTABLESMODE_HUFF;

            m_recvparams = 0;
            m_subaddress = null;
            m_faxdcs = null;

            m_ycbcrsampling_fetched = false;

            m_rawDecode = false;
            m_rawEncode = false;
            m_tif.m_flags |= Tiff.TIFF_NOBITREV; /* no bit reversal, please */

            m_cinfo_initialized = false;

            /*
             ** Create a JPEGTables field if no directory has yet been created. 
             ** We do this just to ensure that sufficient space is reserved for
             ** the JPEGTables field.  It will be properly created the right
             ** size later. 
             */
            if (m_tif.m_diroff == 0)
            {
                uint SIZE_OF_JPEGTABLES = 2000;
                m_tif.setFieldBit(FIELD_JPEGTABLES);
                m_jpegtables_length = SIZE_OF_JPEGTABLES;
                m_jpegtables = new byte [m_jpegtables_length];
            }

            /*
             * Mark the TIFFTAG_YCBCRSAMPLES as present even if it is not
             * see: JPEGFixupTestSubsampling().
             */
            m_tif.setFieldBit(FIELD.FIELD_YCBCRSUBSAMPLING);
            return true;
        }

        public override bool CanEncode()
        {
            return true;
        }

        public override bool CanDecode()
        {
            return true;
        }

        public override bool tif_setupdecode()
        {
            return JPEGSetupDecode();
        }

        public override bool tif_predecode(UInt16 s)
        {
            return JPEGPreDecode(s);
        }

        public override bool tif_decoderow(byte[] pp, int cc, UInt16 s)
        {
            if (m_rawDecode)
                return JPEGDecodeRaw(pp, cc, s);

            return JPEGDecode(pp, cc, s);
        }

        public override bool tif_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            if (m_rawDecode)
                return JPEGDecodeRaw(pp, cc, s);

            return JPEGDecode(pp, cc, s);
        }

        public override bool tif_decodetile(byte[] pp, int cc, UInt16 s)
        {
            if (m_rawDecode)
                return JPEGDecodeRaw(pp, cc, s);

            return JPEGDecode(pp, cc, s);
        }

        public override bool tif_setupencode()
        {
            return JPEGSetupEncode();
        }

        public override bool tif_preencode(UInt16 s)
        {
            return JPEGPreEncode(s);
        }

        public override bool tif_postencode()
        {
            return JPEGPostEncode();
        }

        public override bool tif_encoderow(byte[] pp, int cc, UInt16 s)
        {
            if (m_rawEncode)
                return JPEGEncodeRaw(pp, cc, s);

            return JPEGEncode(pp, cc, s);
        }

        public override bool tif_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            if (m_rawEncode)
                return JPEGEncodeRaw(pp, cc, s);

            return JPEGEncode(pp, cc, s);
        }

        public override bool tif_encodetile(byte[] pp, int cc, UInt16 s)
        {
            if (m_rawEncode)
                return JPEGEncodeRaw(pp, cc, s);

            return JPEGEncode(pp, cc, s);
        }

        public override void tif_cleanup()
        {
            JPEGCleanup();
        }

        public override int tif_defstripsize(int s)
        {
            return JPEGDefaultStripSize(s);
        }

        public override void tif_deftilesize(ref int tw, ref int th)
        {
            JPEGDefaultTileSize(ref tw, ref th);
        }

        /*
         * The JPEG library initialized used to be done in TIFFInitJPEG(), but
         * now that we allow a TIFF file to be opened in update mode it is necessary
         * to have some way of deciding whether compression or decompression is
         * desired other than looking at tif.tif_mode.  We accomplish this by 
         * examining {TILE/STRIP}BYTECOUNTS to see if there is a non-zero entry.
         * If so, we assume decompression is desired. 
         *
         * This is tricky, because TIFFInitJPEG() is called while the directory is
         * being read, and generally speaking the BYTECOUNTS tag won't have been read
         * at that point.  So we try to defer jpeg library initialization till we
         * do have that tag ... basically any access that might require the compressor
         * or decompressor that occurs after the reading of the directory. 
         *
         * In an ideal world compressors or decompressors would be setup
         * at the point where a single tile or strip was accessed (for read or write)
         * so that stuff like update of missing tiles, or replacement of tiles could
         * be done. However, we aren't trying to crack that nut just yet ...
         *
         * NFW, Feb 3rd, 2003.
         */
        public bool InitializeLibJPEG(bool force_encode, bool force_decode)
        {
            uint[] byte_counts = null;
            bool data_is_empty = true;
            bool decompress;

            if (m_cinfo_initialized)
            {
                if (force_encode && m_common.m_is_decompressor)
                    TIFFjpeg_destroy();
                else if (force_decode && !m_common.m_is_decompressor)
                    TIFFjpeg_destroy();
                else
                    return true;

                m_cinfo_initialized = false;
            }

            /*
             * Do we have tile data already?  Make sure we initialize the
             * the state in decompressor mode if we have tile data, even if we
             * are not in read-only file access mode. 
             */
            object[] result = m_tif.GetField(TIFFTAG.TIFFTAG_TILEBYTECOUNTS);
            if (m_tif.IsTiled() && result != null)
            {
                byte_counts = result[0] as uint[];
                if (byte_counts != null)
                    data_is_empty = byte_counts[0] == 0;
            }

            result = m_tif.GetField(TIFFTAG.TIFFTAG_STRIPBYTECOUNTS);
            if (!m_tif.IsTiled() && result != null)
            {
                byte_counts = result[0] as uint[];
                if (byte_counts != null)
                    data_is_empty = byte_counts[0] == 0;
            }

            if (force_decode)
                decompress = true;
            else if (force_encode)
                decompress = false;
            else if (m_tif.m_mode == Tiff.O_RDONLY)
                decompress = true;
            else if (data_is_empty)
                decompress = false;
            else
                decompress = true;

            /*
             * Initialize libjpeg.
             */
            if (decompress)
            {
                if (!TIFFjpeg_create_decompress())
                    return false;
            }
            else
            {
                if (!TIFFjpeg_create_compress())
                    return false;
            }

            m_cinfo_initialized = true;
            return true;
        }

        public Tiff GetTiff()
        {
            return m_tif;
        }

        public void JPEGResetUpsampled()
        {
            /*
            * Mark whether returned data is up-sampled or not so TIFFStripSize
            * and TIFFTileSize return values that reflect the true amount of
            * data.
            */
            m_tif.m_flags &= ~Tiff.TIFF_UPSAMPLED;
            if (m_tif.m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
            {
                if (m_tif.m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && m_jpegcolormode == JPEGCOLORMODE.JPEGCOLORMODE_RGB)
                    m_tif.m_flags |= Tiff.TIFF_UPSAMPLED;
            }

            /*
            * Must recalculate cached tile size in case sampling state changed.
            * Should we really be doing this now if image size isn't set? 
            */
            m_tif.m_tilesize = m_tif.IsTiled() ? m_tif.TileSize() : (int)-1;
        }

        /*
         * Set encoding state at the start of a strip or tile.
         */
        private bool JPEGPreEncode(UInt16 s)
        {
            const string module = "JPEGPreEncode";
            int segment_width;
            int segment_height;
            bool downsampled_input;

            Debug.Assert(!m_common.m_is_decompressor);
            /*
             * Set encoding parameters for this strip/tile.
             */
            if (m_tif.IsTiled())
            {
                segment_width = m_tif.m_dir.td_tilewidth;
                segment_height = m_tif.m_dir.td_tilelength;
                m_bytesperline = m_tif.TileRowSize();
            }
            else
            {
                segment_width = m_tif.m_dir.td_imagewidth;
                segment_height = m_tif.m_dir.td_imagelength - m_tif.m_row;
                if (segment_height > m_tif.m_dir.td_rowsperstrip)
                    segment_height = m_tif.m_dir.td_rowsperstrip;
                m_bytesperline = m_tif.oldScanlineSize();
            }
            if (m_tif.m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE && s > 0)
            {
                /* for PC 2, scale down the strip/tile size
                 * to match a downsampled component
                 */
                segment_width = Tiff.howMany(segment_width, m_h_sampling);
                segment_height = Tiff.howMany(segment_height, m_v_sampling);
            }

            if (segment_width > 65535 || segment_height > 65535)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Strip/tile too large for JPEG");
                return false;
            }
            
            m_compression.Image_width = segment_width;
            m_compression.Image_height = segment_height;
            downsampled_input = false;
            
            if (m_tif.m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
            {
                m_compression.Input_components = m_tif.m_dir.td_samplesperpixel;
                if (m_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR)
                {
                    if (m_jpegcolormode == JPEGCOLORMODE.JPEGCOLORMODE_RGB)
                    {
                        m_compression.In_color_space = J_COLOR_SPACE.JCS_RGB;
                    }
                    else
                    {
                        m_compression.In_color_space = J_COLOR_SPACE.JCS_YCbCr;
                        if (m_h_sampling != 1 || m_v_sampling != 1)
                            downsampled_input = true;
                    }

                    if (!TIFFjpeg_set_colorspace(J_COLOR_SPACE.JCS_YCbCr))
                        return false;
                    
                    /*
                     * Set Y sampling factors;
                     * we assume jpeg_set_colorspace() set the rest to 1
                     */
                    m_compression.m_comp_info[0].h_samp_factor = m_h_sampling;
                    m_compression.m_comp_info[0].v_samp_factor = m_v_sampling;
                }
                else
                {
                    m_compression.In_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                    if (!TIFFjpeg_set_colorspace(J_COLOR_SPACE.JCS_UNKNOWN))
                        return false;
                    /* jpeg_set_colorspace set all sampling factors to 1 */
                }
            }
            else
            {
                m_compression.Input_components = 1;
                m_compression.In_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                if (!TIFFjpeg_set_colorspace(J_COLOR_SPACE.JCS_UNKNOWN))
                    return false;

                m_compression.m_comp_info[0].component_id = s;
                /* jpeg_set_colorspace() set sampling factors to 1 */
                if (m_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && s > 0)
                {
                    m_compression.m_comp_info[0].quant_tbl_no = 1;
                    m_compression.m_comp_info[0].dc_tbl_no = 1;
                    m_compression.m_comp_info[0].ac_tbl_no = 1;
                }
            }

            /* ensure libjpeg won't write any extraneous markers */
            m_compression.Write_JFIF_header = false;
            m_compression.Write_Adobe_marker = false;
            
            /* set up table handling correctly */
            if ((m_jpegtablesmode &JPEGTABLESMODE.JPEGTABLESMODE_QUANT) == 0)
            {
                if (!TIFFjpeg_set_quality(m_jpegquality, false))
                    return false;

                unsuppress_quant_table(0);
                unsuppress_quant_table(1);
            }

            if ((m_jpegtablesmode &JPEGTABLESMODE.JPEGTABLESMODE_HUFF) != 0)
                m_compression.Optimize_coding = false;
            else
                m_compression.Optimize_coding = true;
            
            if (downsampled_input)
            {
                /* Need to use raw-data interface to libjpeg */
                m_compression.Raw_data_in = true;
                m_rawEncode = true;
            }
            else
            {
                /* Use normal interface to libjpeg */
                m_compression.Raw_data_in = false;
                m_rawEncode = false;
            }
            
            /* Start JPEG compressor */
            if (!TIFFjpeg_start_compress(false))
                return false;
            
            /* Allocate downsampled-data buffers if needed */
            if (downsampled_input)
            {
                if (!alloc_downsampled_buffers(m_compression.m_comp_info, m_compression.Num_components))
                    return false;
            }

            m_scancount = 0;
            return true;
        }

        private bool JPEGSetupEncode()
        {
            const string module = "JPEGSetupEncode";

            InitializeLibJPEG(true, false);

            Debug.Assert(!m_common.m_is_decompressor);

            /*
             * Initialize all JPEG parameters to default values.
             * Note that jpeg_set_defaults needs legal values for
             * in_color_space and input_components.
             */
            m_compression.In_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
            m_compression.Input_components = 1;
            if (!TIFFjpeg_set_defaults())
                return false;

            /* Set per-file parameters */
            m_photometric = m_tif.m_dir.td_photometric;
            switch (m_photometric)
            {
                case PHOTOMETRIC.PHOTOMETRIC_YCBCR:
                    m_h_sampling = m_tif.m_dir.td_ycbcrsubsampling[0];
                    m_v_sampling = m_tif.m_dir.td_ycbcrsubsampling[1];
                    /*
                     * A ReferenceBlackWhite field *must* be present since the
                     * default value is inappropriate for YCbCr.  Fill in the
                     * proper value if application didn't set it.
                     */
                    object[] result = m_tif.GetField(TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE);
                    if (result == null)
                    {
                        float[] refbw = new float [6];
                        int top = 1 << m_tif.m_dir.td_bitspersample;
                        refbw[0] = 0;
                        refbw[1] = (float)(top - 1L);
                        refbw[2] = (float)(top >> 1);
                        refbw[3] = refbw[1];
                        refbw[4] = refbw[2];
                        refbw[5] = refbw[1];
                        m_tif.SetField(TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE, refbw);
                    }
                    break;
                case PHOTOMETRIC.PHOTOMETRIC_PALETTE:
                    /* disallowed by Tech Note */
                case PHOTOMETRIC.PHOTOMETRIC_MASK:
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "PhotometricInterpretation %d not allowed for JPEG", m_photometric);
                    return false;
                default:
                    /* TIFF 6.0 forbids subsampling of all other color spaces */
                    m_h_sampling = 1;
                    m_v_sampling = 1;
                    break;
            }

            /* Verify miscellaneous parameters */

            /*
             * This would need work if libtiff ever supports different
             * depths for different components, or if libjpeg ever supports
             * run-time selection of depth.  Neither is imminent.
             */
            if (m_tif.m_dir.td_bitspersample != JpegConstants.BITS_IN_JSAMPLE)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "BitsPerSample %d not allowed for JPEG", m_tif.m_dir.td_bitspersample);
                return false;
            }
            
            m_compression.Data_precision = m_tif.m_dir.td_bitspersample;
            if (m_tif.IsTiled())
            {
                if ((m_tif.m_dir.td_tilelength % (m_v_sampling * JpegConstants.DCTSIZE)) != 0)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG tile height must be multiple of %d", m_v_sampling * JpegConstants.DCTSIZE);
                    return false;
                }

                if ((m_tif.m_dir.td_tilewidth % (m_h_sampling * JpegConstants.DCTSIZE)) != 0)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG tile width must be multiple of %d", m_h_sampling * JpegConstants.DCTSIZE);
                    return false;
                }
            }
            else
            {
                if (m_tif.m_dir.td_rowsperstrip < m_tif.m_dir.td_imagelength && (m_tif.m_dir.td_rowsperstrip % (m_v_sampling * JpegConstants.DCTSIZE)) != 0)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "RowsPerStrip must be multiple of %d for JPEG", m_v_sampling * JpegConstants.DCTSIZE);
                    return false;
                }
            }

            /* Create a JPEGTables field if appropriate */
            if ((m_jpegtablesmode & (JPEGTABLESMODE.JPEGTABLESMODE_QUANT | JPEGTABLESMODE.JPEGTABLESMODE_HUFF)) != 0)
            {
                if (!prepare_JPEGTables())
                    return false;

                /* Mark the field present */
                /* Can't use SetField since BEENWRITING is already set! */
                m_tif.setFieldBit(FIELD_JPEGTABLES);
                m_tif.m_flags |= Tiff.TIFF_DIRTYDIRECT;
            }
            else
            {
                /* We do not support application-supplied JPEGTables, */
                /* so mark the field not present */
                m_tif.clearFieldBit(FIELD_JPEGTABLES);
            }

            /* Direct libjpeg output to libtiff's output buffer */
            TIFFjpeg_data_dest();

            return true;
        }

        /*
        * Finish up at the end of a strip or tile.
        */
        private bool JPEGPostEncode()
        {
            if (m_scancount > 0)
            {
                /*
                 * Need to emit a partial bufferload of downsampled data.
                 * Pad the data vertically.
                 */
                for (int ci = 0; ci < m_compression.Num_components; ci++)
                {
                    int vsamp = m_compression.m_comp_info[ci].v_samp_factor;
                    int row_width = m_compression.m_comp_info[ci].width_in_blocks * JpegConstants.DCTSIZE * sizeof(byte);
                    for (int ypos = m_scancount * vsamp; ypos < JpegConstants.DCTSIZE * vsamp; ypos++)
                    {
                        Array.Copy(m_ds_buffer[ci][ypos - 1], m_ds_buffer[ci][ypos], row_width);
                    }
                }

                int n = m_compression.Max_v_samp_factor * JpegConstants.DCTSIZE;
                if (TIFFjpeg_write_raw_data(m_ds_buffer, n) != n)
                    return false;
            }

            return TIFFjpeg_finish_compress();
        }

        private void JPEGCleanup()
        {
            m_tif.m_tagmethods = m_parentTagMethods;

            if (m_cinfo_initialized)
            {
                TIFFjpeg_destroy();
                /* release libjpeg resources */
            }
        }
        
        /*
        * JPEG Decoding.
        */

        /*
        * Set up for decoding a strip or tile.
        */
        private bool JPEGPreDecode(UInt16 s)
        {
            TiffDirectory td = m_tif.m_dir;
            const string module = "JPEGPreDecode";
            int segment_width;
            int segment_height;
            bool downsampled_output;
            int ci;

            Debug.Assert(m_common.m_is_decompressor);
            /*
             * Reset decoder state from any previous strip/tile,
             * in case application didn't read the whole strip.
             */
            if (!TIFFjpeg_abort())
                return false;
            /*
             * Read the header for this strip/tile.
             */
            if (TIFFjpeg_read_header(true) != ReadResult.JPEG_HEADER_OK)
                return false;
            /*
             * Check image parameters and set decompression parameters.
             */
            segment_width = td.td_imagewidth;
            segment_height = td.td_imagelength - m_tif.m_row;
            if (m_tif.IsTiled())
            {
                segment_width = td.td_tilewidth;
                segment_height = td.td_tilelength;
                m_bytesperline = m_tif.TileRowSize();
            }
            else
            {
                if (segment_height > td.td_rowsperstrip)
                    segment_height = td.td_rowsperstrip;
                m_bytesperline = m_tif.oldScanlineSize();
            }
            
            if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE && s > 0)
            {
                /*
                 * For PC 2, scale down the expected strip/tile size
                 * to match a downsampled component
                 */
                segment_width = Tiff.howMany(segment_width, m_h_sampling);
                segment_height = Tiff.howMany(segment_height, m_v_sampling);
            }
            
            if (m_decompression.Image_width < segment_width || m_decompression.Image_height < segment_height)
            {
                Tiff.WarningExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG strip/tile size, expected %dx%d, got %dx%d", segment_width, segment_height, m_decompression.Image_width, m_decompression.Image_height);
            }

            if (m_decompression.Image_width > segment_width || m_decompression.Image_height > segment_height)
            {
                /*
                * This case could be dangerous, if the strip or tile size has
                * been reported as less than the amount of data jpeg will
                * return, some potential security issues arise. Catch this
                * case and error out.
                */
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG strip/tile size exceeds expected dimensions, expected %dx%d, got %dx%d", segment_width, segment_height, m_decompression.Image_width, m_decompression.Image_height);
                return false;
            }

            if (m_decompression.Num_components != (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG ? td.td_samplesperpixel : 1))
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG component count");
                return false;
            }

            if (m_decompression.Data_precision != td.td_bitspersample)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG data precision");
                return false;
            }

            if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
            {
                /* Component 0 should have expected sampling factors */
                if (m_decompression.m_comp_info[0].h_samp_factor != m_h_sampling || m_decompression.m_comp_info[0].v_samp_factor != m_v_sampling)
                {
                    Tiff.WarningExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG sampling factors %d,%d\nApparently should be %d,%d.", m_decompression.m_comp_info[0].h_samp_factor, m_decompression.m_comp_info[0].v_samp_factor, m_h_sampling, m_v_sampling);

                    /*
                    * There are potential security issues here
                    * for decoders that have already allocated
                    * buffers based on the expected sampling
                    * factors. Lets check the sampling factors
                    * dont exceed what we were expecting.
                    */
                    if (m_decompression.m_comp_info[0].h_samp_factor > m_h_sampling || m_decompression.m_comp_info[0].v_samp_factor > m_v_sampling)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Cannot honour JPEG sampling factors that exceed those specified.");
                        return false;
                    }

                    /*
                     * XXX: Files written by the Intergraph software
                     * has different sampling factors stored in the
                     * TIFF tags and in the JPEG structures. We will
                     * try to deduce Intergraph files by the presense
                     * of the tag 33918.
                     */
                    if (m_tif.FindFieldInfo((TIFFTAG)33918, TiffDataType.TIFF_ANY) == null)
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module, "Decompressor will try reading with sampling %d,%d.", m_decompression.m_comp_info[0].h_samp_factor, m_decompression.m_comp_info[0].v_samp_factor);

                        m_h_sampling = (UInt16)m_decompression.m_comp_info[0].h_samp_factor;
                        m_v_sampling = (UInt16)m_decompression.m_comp_info[0].v_samp_factor;
                    }
                }
                /* Rest should have sampling factors 1,1 */
                for (ci = 1; ci < m_decompression.Num_components; ci++)
                {
                    if (m_decompression.m_comp_info[ci].h_samp_factor != 1 || m_decompression.m_comp_info[ci].v_samp_factor != 1)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG sampling factors");
                        return false;
                    }
                }
            }
            else
            {
                /* PC 2's single component should have sampling factors 1,1 */
                if (m_decompression.m_comp_info[0].h_samp_factor != 1 || m_decompression.m_comp_info[0].v_samp_factor != 1)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG sampling factors");
                    return false;
                }
            }
            downsampled_output = false;
            if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && m_jpegcolormode == JPEGCOLORMODE.JPEGCOLORMODE_RGB)
            {
                /* Convert YCbCr to RGB */
                m_decompression.Jpeg_color_space = J_COLOR_SPACE.JCS_YCbCr;
                m_decompression.Out_color_space = J_COLOR_SPACE.JCS_RGB;
            }
            else
            {
                /* Suppress colorspace handling */
                m_decompression.Jpeg_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                m_decompression.Out_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && (m_h_sampling != 1 || m_v_sampling != 1))
                    downsampled_output = true;
                /* XXX what about up-sampling? */
            }
            if (downsampled_output)
            {
                /* Need to use raw-data interface to libjpeg */
                m_decompression.Raw_data_out = true;
                m_rawDecode = true;
            }
            else
            {
                /* Use normal interface to libjpeg */
                m_decompression.Raw_data_out = false;
                m_rawDecode = false;
            }

            /* Start JPEG decompressor */
            if (!TIFFjpeg_start_decompress())
                return false;
            
            /* Allocate downsampled-data buffers if needed */
            if (downsampled_output)
            {
                if (!alloc_downsampled_buffers(m_decompression.m_comp_info, m_decompression.Num_components))
                    return false;

                m_scancount = JpegConstants.DCTSIZE; /* mark buffer empty */
            }

            return true;
        }

        private bool prepare_JPEGTables()
        {
            InitializeLibJPEG(false, false);

            /* Initialize quant tables for current quality setting */
            if (!TIFFjpeg_set_quality(m_jpegquality, false))
                return false;

            /* Mark only the tables we want for output */
            /* NB: chrominance tables are currently used only with YCbCr */
            if (!TIFFjpeg_suppress_tables(true))
                return false;

            if ((m_jpegtablesmode & JPEGTABLESMODE.JPEGTABLESMODE_QUANT) != 0)
            {
                unsuppress_quant_table(0);
                if (m_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR)
                    unsuppress_quant_table(1);
            }

            if ((m_jpegtablesmode & JPEGTABLESMODE.JPEGTABLESMODE_HUFF) != 0)
            {
                unsuppress_huff_table(0);
                if (m_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR)
                    unsuppress_huff_table(1);
            }

            /* Direct libjpeg output into jpegtables */
            if (!TIFFjpeg_tables_dest())
                return false;

            /* Emit tables-only datastream */
            if (!TIFFjpeg_write_tables())
                return false;

            return true;
        }

        private bool JPEGSetupDecode()
        {
            TiffDirectory td = m_tif.m_dir;

            InitializeLibJPEG(false, true);

            Debug.Assert(m_common.m_is_decompressor);

            /* Read JPEGTables if it is present */
            if (m_tif.fieldSet(FIELD_JPEGTABLES))
            {
                m_decompression.Src = new JpegTablesSource(this);
                if (TIFFjpeg_read_header(false) != ReadResult.JPEG_HEADER_TABLES_ONLY)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, "JPEGSetupDecode", "Bogus JPEGTables field");
                    return false;
                }
            }

            /* Grab parameters that are same for all strips/tiles */
            m_photometric = td.td_photometric;
            switch (m_photometric)
            {
                case PHOTOMETRIC.PHOTOMETRIC_YCBCR:
                    m_h_sampling = td.td_ycbcrsubsampling[0];
                    m_v_sampling = td.td_ycbcrsubsampling[1];
                    break;
                default:
                    /* TIFF 6.0 forbids subsampling of all other color spaces */
                    m_h_sampling = 1;
                    m_v_sampling = 1;
                    break;
            }

            /* Set up for reading normal data */
            m_decompression.Src = new JpegStdSource(this);
            m_tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmNone; /* override byte swapping */
            return true;
        }

        /*
        * Decode a chunk of pixels.
        * "Standard" case: returned data is not downsampled.
        */
        private bool JPEGDecode(byte[] buf, int cc, UInt16 s)
        {
            TiffDirectory td = m_tif.m_dir;
            const string module = "JPEGPreDecode";
            int segment_width;
            int segment_height;
            bool downsampled_output;
            int ci;

            Debug.Assert(m_common.m_is_decompressor);
            /*
             * Reset decoder state from any previous strip/tile,
             * in case application didn't read the whole strip.
             */
            if (!TIFFjpeg_abort())
                return false;
            /*
             * Read the header for this strip/tile.
             */
            if (TIFFjpeg_read_header(true) != ReadResult.JPEG_HEADER_OK)
                return false;
            /*
             * Check image parameters and set decompression parameters.
             */
            segment_width = td.td_imagewidth;
            segment_height = td.td_imagelength - m_tif.m_row;
            if (m_tif.IsTiled())
            {
                segment_width = td.td_tilewidth;
                segment_height = td.td_tilelength;
                m_bytesperline = m_tif.TileRowSize();
            }
            else
            {
                if (segment_height > td.td_rowsperstrip)
                    segment_height = td.td_rowsperstrip;
                m_bytesperline = m_tif.oldScanlineSize();
            }
            
            if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE && s > 0)
            {
                /*
                 * For PC 2, scale down the expected strip/tile size
                 * to match a downsampled component
                 */
                segment_width = Tiff.howMany(segment_width, m_h_sampling);
                segment_height = Tiff.howMany(segment_height, m_v_sampling);
            }
            
            if (m_decompression.Image_width < segment_width || m_decompression.Image_height < segment_height)
            {
                Tiff.WarningExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG strip/tile size, expected %dx%d, got %dx%d", segment_width, segment_height, m_decompression.Image_width, m_decompression.Image_height);
            }

            if (m_decompression.Image_width > segment_width || m_decompression.Image_height > segment_height)
            {
                /*
                * This case could be dangerous, if the strip or tile size has
                * been reported as less than the amount of data jpeg will
                * return, some potential security issues arise. Catch this
                * case and error out.
                */
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG strip/tile size exceeds expected dimensions, expected %dx%d, got %dx%d", segment_width, segment_height, m_decompression.Image_width, m_decompression.Image_height);
                return false;
            }

            if (m_decompression.Num_components != (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG ? td.td_samplesperpixel : 1))
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG component count");
                return false;
            }

            if (m_decompression.Data_precision != td.td_bitspersample)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG data precision");
                return false;
            }

            if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
            {
                /* Component 0 should have expected sampling factors */
                if (m_decompression.m_comp_info[0].h_samp_factor != m_h_sampling || m_decompression.m_comp_info[0].v_samp_factor != m_v_sampling)
                {
                    Tiff.WarningExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG sampling factors %d,%d\nApparently should be %d,%d.", m_decompression.m_comp_info[0].h_samp_factor, m_decompression.m_comp_info[0].v_samp_factor, m_h_sampling, m_v_sampling);

                    /*
                    * There are potential security issues here
                    * for decoders that have already allocated
                    * buffers based on the expected sampling
                    * factors. Lets check the sampling factors
                    * dont exceed what we were expecting.
                    */
                    if (m_decompression.m_comp_info[0].h_samp_factor > m_h_sampling || m_decompression.m_comp_info[0].v_samp_factor > m_v_sampling)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Cannot honour JPEG sampling factors that exceed those specified.");
                        return false;
                    }

                    /*
                     * XXX: Files written by the Intergraph software
                     * has different sampling factors stored in the
                     * TIFF tags and in the JPEG structures. We will
                     * try to deduce Intergraph files by the presense
                     * of the tag 33918.
                     */
                    if (m_tif.FindFieldInfo((TIFFTAG)33918, TiffDataType.TIFF_ANY) == null)
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module, "Decompressor will try reading with sampling %d,%d.", m_decompression.m_comp_info[0].h_samp_factor, m_decompression.m_comp_info[0].v_samp_factor);

                        m_h_sampling = (UInt16)m_decompression.m_comp_info[0].h_samp_factor;
                        m_v_sampling = (UInt16)m_decompression.m_comp_info[0].v_samp_factor;
                    }
                }
                /* Rest should have sampling factors 1,1 */
                for (ci = 1; ci < m_decompression.Num_components; ci++)
                {
                    if (m_decompression.m_comp_info[ci].h_samp_factor != 1 || m_decompression.m_comp_info[ci].v_samp_factor != 1)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG sampling factors");
                        return false;
                    }
                }
            }
            else
            {
                /* PC 2's single component should have sampling factors 1,1 */
                if (m_decompression.m_comp_info[0].h_samp_factor != 1 || m_decompression.m_comp_info[0].v_samp_factor != 1)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Improper JPEG sampling factors");
                    return false;
                }
            }
            downsampled_output = false;
            if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && m_jpegcolormode == JPEGCOLORMODE.JPEGCOLORMODE_RGB)
            {
                /* Convert YCbCr to RGB */
                m_decompression.Jpeg_color_space = J_COLOR_SPACE.JCS_YCbCr;
                m_decompression.Out_color_space = J_COLOR_SPACE.JCS_RGB;
            }
            else
            {
                /* Suppress colorspace handling */
                m_decompression.Jpeg_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                m_decompression.Out_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                if (td.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && (m_h_sampling != 1 || m_v_sampling != 1))
                    downsampled_output = true;
                /* XXX what about up-sampling? */
            }
            if (downsampled_output)
            {
                /* Need to use raw-data interface to libjpeg */
                m_decompression.Raw_data_out = true;
                m_rawDecode = true;
            }
            else
            {
                /* Use normal interface to libjpeg */
                m_decompression.Raw_data_out = false;
                m_rawDecode = false;
            }

            /* Start JPEG decompressor */
            if (!TIFFjpeg_start_decompress())
                return false;
            
            /* Allocate downsampled-data buffers if needed */
            if (downsampled_output)
            {
                if (!alloc_downsampled_buffers(m_decompression.m_comp_info, m_decompression.Num_components))
                    return false;

                m_scancount = JpegConstants.DCTSIZE; /* mark buffer empty */
            }

            return true;
        }
        
        /*
        * Decode a chunk of pixels.
        * Returned data is downsampled per sampling factors.
        */
        private bool JPEGDecodeRaw(byte[] buf, int cc, UInt16 s)
        {
            /* data is expected to be read in multiples of a scanline */
            int nrows = m_decompression.Image_height;
            if (nrows != 0)
            {
                /* Cb,Cr both have sampling factors 1, so this is correct */
                int clumps_per_line = m_decompression.m_comp_info[1].Downsampled_width;

                int bufOffset = 0;
                do
                {
                    /* Reload downsampled-data buffer if needed */
                    if (m_scancount >= JpegConstants.DCTSIZE)
                    {
                        int n = m_decompression.Max_v_samp_factor * JpegConstants.DCTSIZE;
                        if (TIFFjpeg_read_raw_data(m_ds_buffer, n) != n)
                            return false;

                        m_scancount = 0;
                    }

                    /*
                     * Fastest way to unseparate data is to make one pass
                     * over the scanline for each row of each component.
                     */
                    int clumpoffset = 0; /* first sample in clump */
                    for (int ci = 0; ci < m_decompression.Num_components; ci++)
                    {
                        int hsamp = m_decompression.m_comp_info[ci].h_samp_factor;
                        int vsamp = m_decompression.m_comp_info[ci].v_samp_factor;

                        for (int ypos = 0; ypos < vsamp; ypos++)
                        {
                            byte[] inBuf = m_ds_buffer[ci][m_scancount * vsamp + ypos];
                            int inptr = 0;

                            int outptr = bufOffset + clumpoffset;

                            if (hsamp == 1)
                            {
                                /* fast path for at least Cb and Cr */
                                for (int nclump = clumps_per_line; nclump-- > 0; )
                                {
                                    buf[outptr] = inBuf[inptr];
                                    inptr++;
                                    outptr += m_samplesperclump;
                                }
                            }
                            else
                            {
                                /* general case */
                                for (int nclump = clumps_per_line; nclump-- > 0; )
                                {
                                    for (int xpos = 0; xpos < hsamp; xpos++)
                                    {
                                        buf[outptr + xpos] = inBuf[inptr];
                                        inptr++;
                                    }

                                    outptr += m_samplesperclump;
                                }
                            }

                            clumpoffset += hsamp;
                        }
                    }

                    ++m_scancount;
                    m_tif.m_row += m_v_sampling;

                    /* increment/decrement of buf and cc is still incorrect, but should not matter
                    * TODO: resolve this */
                    bufOffset += m_bytesperline;
                    cc -= m_bytesperline;
                    nrows -= m_v_sampling;
                }
                while (nrows > 0);
            }

            /* Close down the decompressor if done. */
            return m_decompression.Output_scanline < m_decompression.Output_height || TIFFjpeg_finish_decompress();
        }

        /*
        * Encode a chunk of pixels.
        * "Standard" case: incoming data is not downsampled.
        */
        private bool JPEGEncode(byte[] buf, int cc, UInt16 s)
        {
            /* data is expected to be supplied in multiples of a scanline */
            int nrows = cc / m_bytesperline;
            if ((cc % m_bytesperline) != 0)
                Tiff.WarningExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "fractional scanline discarded");

            /* The last strip will be limited to image size */
            if (!m_tif.IsTiled() && m_tif.m_row + nrows > m_tif.m_dir.td_imagelength)
                nrows = m_tif.m_dir.td_imagelength - m_tif.m_row;

            byte[][] bufptr = new byte[1][];
            bufptr[0] = new byte [m_bytesperline];
            int bufOffset = 0;
            while (nrows-- > 0)
            {
                Array.Copy(buf, bufOffset, bufptr, 0, m_bytesperline);
                if (TIFFjpeg_write_scanlines(bufptr, 1) != 1)
                    return false;

                if (nrows > 0)
                    m_tif.m_row++;
                
                bufOffset += m_bytesperline;
            }

            return true;
        }

        /*
        * Encode a chunk of pixels.
        * Incoming data is expected to be downsampled per sampling factors.
        */
        private bool JPEGEncodeRaw(byte[] buf, int cc, UInt16 s)
        {
            /* data is expected to be supplied in multiples of a clumpline */
            /* a clumpline is equivalent to v_sampling desubsampled scanlines */
            /* TODO: the following calculation of bytesperclumpline, should substitute 
             * calculation of bytesperline, except that it is per v_sampling lines */
            int bytesperclumpline = (((m_compression.Image_width + m_h_sampling - 1) / m_h_sampling) * (m_h_sampling * m_v_sampling + 2) * m_compression.Data_precision + 7) / 8;
            int nrows = (cc / bytesperclumpline) * m_v_sampling;
            if ((cc % bytesperclumpline) != 0)
                Tiff.WarningExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "fractional scanline discarded");

            /* Cb,Cr both have sampling factors 1, so this is correct */
            int clumps_per_line = m_compression.m_comp_info[1].Downsampled_width;

            int bufOffset = 0;
            while (nrows > 0)
            {
                /*
                 * Fastest way to separate the data is to make one pass
                 * over the scanline for each row of each component.
                 */
                int clumpoffset = 0; /* first sample in clump */
                for (int ci = 0; ci < m_compression.Num_components; ci++)
                {
                    jpeg_component_info compptr = m_compression.m_comp_info[ci];
                    int hsamp = compptr.h_samp_factor;
                    int vsamp = compptr.v_samp_factor;
                    int padding = (int)(compptr.width_in_blocks * JpegConstants.DCTSIZE - clumps_per_line * hsamp);
                    for (int ypos = 0; ypos < vsamp; ypos++)
                    {
                        int inptr = bufOffset + clumpoffset;

                        byte[] outbuf = m_ds_buffer[ci][m_scancount * vsamp + ypos];
                        int outptr = 0;

                        if (hsamp == 1)
                        {
                            /* fast path for at least Cb and Cr */
                            for (int nclump = clumps_per_line; nclump-- > 0;)
                            {
                                outbuf[outptr] = buf[inptr];
                                outptr++;
                                inptr += m_samplesperclump;
                            }
                        }
                        else
                        {
                            /* general case */
                            for (int nclump = clumps_per_line; nclump-- > 0;)
                            {
                                for (int xpos = 0; xpos < hsamp; xpos++)
                                {
                                    outbuf[outptr] = buf[inptr + xpos];
                                    outptr++;
                                }

                                inptr += m_samplesperclump;
                            }
                        }

                        /* pad each scanline as needed */
                        for (int xpos = 0; xpos < padding; xpos++)
                        {
                            outbuf[outptr] = outbuf[outptr - 1];
                            outptr++;
                        }

                        clumpoffset += hsamp;
                    }
                }

                m_scancount++;
                if (m_scancount >= JpegConstants.DCTSIZE)
                {
                    int n = m_compression.Max_v_samp_factor * JpegConstants.DCTSIZE;
                    if (TIFFjpeg_write_raw_data(m_ds_buffer, n) != n)
                        return false;

                    m_scancount = 0;
                }

                m_tif.m_row += m_v_sampling;
                bufOffset += m_bytesperline;
                nrows -= m_v_sampling;
            }

            return true;
        }

        private int JPEGDefaultStripSize(int s)
        {
            base.tif_defstripsize(s);
            if (s < m_tif.m_dir.td_imagelength)
                s = Tiff.roundUp(s, m_tif.m_dir.td_ycbcrsubsampling[1] * JpegConstants.DCTSIZE);

            return s;
        }

        private void JPEGDefaultTileSize(ref int tw, ref int th)
        {
            base.tif_deftilesize(ref tw, ref th);
            tw = Tiff.roundUp(tw, m_tif.m_dir.td_ycbcrsubsampling[0] * JpegConstants.DCTSIZE);
            th = Tiff.roundUp(th, m_tif.m_dir.td_ycbcrsubsampling[1] * JpegConstants.DCTSIZE);
        }

        /*
        * Interface routines.  This layer of routines exists
        * primarily to limit side-effects from libjpeg exceptions.
        * Also, normal/error returns are converted into return
        * values per libtiff practice.
        */
        private bool TIFFjpeg_create_compress()
        {
            /* initialize JPEG error handling */
            try
            {
                m_compression = new jpeg_compress_struct(new JpegErrorManager(m_tif));
                m_common = m_compression;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_create_decompress()
        {
            /* initialize JPEG error handling */
            try
            {
                m_decompression = new jpeg_decompress_struct(new JpegErrorManager(m_tif));
                m_common = m_decompression;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_set_defaults()
        {
            try
            {
                m_compression.jpeg_set_defaults();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_set_colorspace(J_COLOR_SPACE colorspace)
        {
            try
            {
                m_compression.jpeg_set_colorspace(colorspace);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_set_quality(int quality, bool force_baseline)
        {
            try
            {
                m_compression.jpeg_set_quality(quality, force_baseline);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_suppress_tables(bool suppress)
        {
            try
            {
                m_compression.jpeg_suppress_tables(suppress);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_start_compress(bool write_all_tables)
        {
            try
            {
                m_compression.jpeg_start_compress(write_all_tables);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private int TIFFjpeg_write_scanlines(byte[][] scanlines, int num_lines)
        {
            int n = 0;
            try
            {
                n = m_compression.jpeg_write_scanlines(scanlines, (int)num_lines);
            }
            catch (Exception)
            {
                return -1;
            }

            return n;
        }

        private int TIFFjpeg_write_raw_data(byte[][][] data, int num_lines)
        {
            int n = 0;
            try
            {
                n = m_compression.jpeg_write_raw_data(data, (int)num_lines);
            }
            catch (Exception)
            {
                return -1;
            }

            return n;
        }

        private bool TIFFjpeg_finish_compress()
        {
            try
            {
                m_compression.jpeg_finish_compress();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_write_tables()
        {
            try
            {
                m_compression.jpeg_write_tables();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private ReadResult TIFFjpeg_read_header(bool require_image)
        {
            ReadResult res = ReadResult.JPEG_SUSPENDED;
            try
            {
                res = m_decompression.jpeg_read_header(require_image);
            }
            catch (Exception)
            {
                return ReadResult.JPEG_SUSPENDED;
            }

            return res;
        }

        private bool TIFFjpeg_start_decompress()
        {
            try
            {
                m_decompression.jpeg_start_decompress();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        //private int TIFFjpeg_read_scanlines(byte[][] scanlines, int max_lines)
        //{
        //    int n = 0;
        //    try
        //    {
        //        n = m_decompression.jpeg_read_scanlines(scanlines, (int)max_lines);
        //    }
        //    catch (Exception)
        //    {
        //        return -1;
        //    }

        //    return n;
        //}

        private int TIFFjpeg_read_raw_data(byte[][][] data, int max_lines)
        {
            int n = 0;
            try
            {
                n = m_decompression.jpeg_read_raw_data(data, (int)max_lines);
            }
            catch (Exception)
            {
                return -1;
            }

            return n;
        }

        private bool TIFFjpeg_finish_decompress()
        {
            bool res = true;
            try
            {
                res = m_decompression.jpeg_finish_decompress();
            }
            catch (Exception)
            {
                return false;
            }

            return res;
        }

        private bool TIFFjpeg_abort()
        {
            try
            {
                m_common.jpeg_abort();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool TIFFjpeg_destroy()
        {
            try
            {
                m_common.jpeg_destroy();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private byte[][] TIFFjpeg_alloc_sarray(int samplesperrow, int numrows)
        {
            byte[][] result = new byte [numrows][];
            for (int i = 0; i < numrows; i++)
                result[i] = new byte[samplesperrow];

            return result;
        }

        /*
        * Allocate downsampled-data buffers needed for downsampled I/O.
        * We use values computed in jpeg_start_compress or jpeg_start_decompress.
        * We use libjpeg's allocator so that buffers will be released automatically
        * when done with strip/tile.
        * This is also a handy place to compute samplesperclump, bytesperline.
        */
        private bool alloc_downsampled_buffers(jpeg_component_info[] comp_info, int num_components)
        {
            int samples_per_clump = 0;
            for (int ci = 0; ci < num_components; ci++)
            {
                jpeg_component_info compptr = comp_info[ci];
                samples_per_clump += compptr.h_samp_factor * compptr.v_samp_factor;
                byte[][] buf = TIFFjpeg_alloc_sarray(compptr.width_in_blocks * JpegConstants.DCTSIZE, (int)(compptr.v_samp_factor * JpegConstants.DCTSIZE));
                if (buf == null)
                    return false;

                m_ds_buffer[ci] = buf;
            }

            m_samplesperclump = samples_per_clump;
            return true;
        }

        private void unsuppress_quant_table(int tblno)
        {
            JQUANT_TBL qtbl = m_compression.Quant_tbl_ptrs[tblno];
            if (qtbl != null)
                qtbl.sent_table = false;
        }

        private void unsuppress_huff_table(int tblno)
        {
            JHUFF_TBL htbl = m_compression.Dc_huff_tbl_ptrs[tblno];

            if (htbl != null)
                htbl.sent_table = false;

            htbl = m_compression.Ac_huff_tbl_ptrs[tblno];
            if (htbl != null)
                htbl.sent_table = false;
        }

        private void TIFFjpeg_data_dest()
        {
            m_compression.Dest = new JpegStdDestination(m_tif);
        }

        private bool TIFFjpeg_tables_dest()
        {
            /*
             * Allocate a working buffer for building tables.
             * Initial size is 1000 bytes, which is usually adequate.
             */
            m_jpegtables_length = 1000;
            m_jpegtables = new byte [m_jpegtables_length];
            if (m_jpegtables == null)
            {
                m_jpegtables_length = 0;
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, "TIFFjpeg_tables_dest", "No space for JPEGTables");
                return false;
            }

            m_compression.Dest = new JpegTablesDestination(this);
            return true;
        }
    }
}
