using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
    class Jpeg
    {
        private jpeg_compress_struct m_compressor = new jpeg_compress_struct(new jpeg_error_mgr());
        private jpeg_decompress_struct m_decompressor = new jpeg_decompress_struct(new jpeg_error_mgr());

        private DecompressionParameters m_decompressionParameters = new DecompressionParameters();

        public void Compress(Stream input, CompressionParameters parameters, Stream output)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (parameters == null)
                throw new ArgumentNullException("parameters");

            if (output == null)
                throw new ArgumentNullException("output");

            /* Initialize JPEG parameters.
             * Much of this may be overridden later.
             * In particular, we don't yet know the input file's color space,
             * but we need to provide some value for jpeg_set_defaults() to work.
             */

            m_compressor.In_color_space = J_COLOR_SPACE.JCS_RGB; /* arbitrary guess */
            m_compressor.jpeg_set_defaults();

            /* Figure out the input file format, and set up to read it. */
            BitmapSource src_mgr = new BitmapSource(m_compressor);
            src_mgr.InputFile = input;

            /* Read the input file header to obtain file size & colorspace. */
            src_mgr.StartInput();

            applyParameters(parameters);

            /* Specify data destination for compression */
            m_compressor.jpeg_stdio_dest(output);

            /* Start compressor */
            m_compressor.jpeg_start_compress(true);

            /* Process data */
            while (m_compressor.Next_scanline < m_compressor.Image_height)
            {
                int num_scanlines = src_mgr.GetPixelRows();
                m_compressor.jpeg_write_scanlines(src_mgr.m_buffer, num_scanlines);
            }

            /* Finish compression and release memory */
            src_mgr.FinishInput();
            m_compressor.jpeg_finish_compress();
        }

        public DecompressionParameters DecompressionParameters
        {
            get
            {
                return m_decompressionParameters;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                m_decompressionParameters = value;
            }
        }

        public void Decompress(Stream jpeg, Stream output)
        {
            /* Initialize the output module now to let it override any crucial
             * option settings (for instance, GIF wants to force color quantization).
             */
            IDecompressDestination destination = new BitmapDestination(output, m_decompressionParameters.OutputImageFormat == ImageFormat.BMP_OS2);
            Decompress(jpeg, destination);
        }

        public void Decompress(Stream jpeg, IDecompressDestination destination)
        {
            if (jpeg == null)
                throw new ArgumentNullException("jpeg");

            if (destination == null)
                throw new ArgumentNullException("destination");

            beforeDecompress(jpeg);

            /* Start decompressor */
            m_decompressor.jpeg_start_decompress();

            ImageParameters parameters = createOutputImageParameters();
            destination.SetImageParameters(parameters);
            /* Write output file header */
            destination.Start();

            /* Process data */
            while (m_decompressor.Output_scanline < m_decompressor.Output_height)
            {
                byte[][] row = jpeg_common_struct.AllocJpegSamples(OutputWidth * OutputComponents, 1);
                m_decompressor.jpeg_read_scanlines(row, 1);
                destination.ProcessPixelsRow(row[0]);
            }

            /* Finish decompression and release memory.
             * I must do it in this order because output module has allocated memory
             * of lifespan JPOOL_IMAGE; it needs to finish before releasing memory.
             */
            destination.Finish();
            m_decompressor.jpeg_finish_decompress();
        }

        private void beforeDecompress(Stream jpeg)
        {
            m_decompressor.jpeg_stdio_src(jpeg);
            /* Read file header, set default decompression parameters */
            m_decompressor.jpeg_read_header(true);

            applyParameters(m_decompressionParameters);
            m_decompressor.jpeg_calc_output_dimensions();
        }

        private ImageParameters createOutputImageParameters()
        {
            ImageParameters result = new ImageParameters(m_decompressor);
            result.Colorspace = (Colorspace)m_decompressor.Jpeg_color_space;
            result.OutColorspace = (Colorspace)m_decompressor.Out_color_space;
            result.QuantizeColors = m_decompressor.Quantize_colors;
            result.Width = m_decompressor.Output_width;
            result.Height = m_decompressor.Output_height;
            result.ComponentsPerSample = m_decompressor.Out_color_components;
            result.Components = m_decompressor.Output_components;
            result.ActualNumberOfColors = m_decompressor.Actual_number_of_colors;
            result.Colormap = m_decompressor.Colormap;
            result.DensityUnit = m_decompressor.Density_unit;
            result.DensityX = m_decompressor.X_density;
            result.DensityY = m_decompressor.Y_density;
            return result;
        }

        public jpeg_compress_struct ClassicCompressor
        {
            get
            {
                return m_compressor;
            }
        }

        public jpeg_decompress_struct ClassicDecompressor
        {
            get
            {
                return m_decompressor;
            }
        }

        /// <summary>
        /// Delegate for application-supplied marker processing methods.
        /// Need not pass marker code since it is stored in cinfo.unread_marker.
        /// </summary>
        public delegate bool MarkerParser(Jpeg decompressor);

        /* Install a special processing method for COM or APPn markers. */
        public void SetMarkerProcessor(int markerCode, MarkerParser routine)
        {
            jpeg_decompress_struct.jpeg_marker_parser_method f = delegate { return routine(this); };
            m_decompressor.jpeg_set_marker_processor(markerCode, f);
        }

        /* Control saving of COM and APPn markers into marker_list. */
        public void SaveMarkers(int markerCode, int lengthLimit)
        {
            m_decompressor.jpeg_save_markers(markerCode, lengthLimit);
        }

        // colorspace of JPEG image
        internal Colorspace Colorspace
        {
            get
            {
                return (Colorspace)m_decompressor.Jpeg_color_space;
            }
        }

        /* Decompression processing parameters --- these fields must be set before
         * calling jpeg_start_decompress().  Note that jpeg_read_header() initializes
         * them to default values.
         */

        // colorspace for output
        internal Colorspace OutColorspace
        {
            get
            {
                return (Colorspace)m_decompressor.Out_color_space;
            }
            set
            {
                m_decompressor.Out_color_space = (J_COLOR_SPACE)value;
            }
        }

        // true=colormapped output wanted
        internal bool QuantizeColors
        {
            get
            {
                return m_decompressor.Quantize_colors;
            }
            set
            {
                m_decompressor.Quantize_colors = value;
            }
        }

        /* Description of actual output image that will be returned to application.
         * These fields are computed by jpeg_start_decompress().
         * You can also use jpeg_calc_output_dimensions() to determine these values
         * in advance of calling jpeg_start_decompress().
         */

        // scaled image width
        internal int OutputWidth
        {
            get
            {
                return m_decompressor.Output_width;
            }
        }

        // scaled image height
        internal int OutputHeight
        {
            get
            {
                return m_decompressor.Output_height;
            }
        }

        // # of color components in out_color_space
        internal int OutComponentsPerSample
        {
            get
            {
                return m_decompressor.Out_color_components;
            }
        }

        // # of color components returned. it is 1 (a colormap index) when 
        // quantizing colors; otherwise it equals out_color_components.
        internal int OutputComponents
        {
            get
            {
                return m_decompressor.Output_components;
            }
        }

        /* When quantizing colors, the output colormap is described by these fields.
         * The application can supply a colormap by setting colormap non-null before
         * calling jpeg_start_decompress; otherwise a colormap is created during
         * jpeg_start_decompress or jpeg_start_output.
         * The map has out_color_components rows and actual_number_of_colors columns.
         */

        // number of entries in use
        internal int ActualNumberOfColors
        {
            get
            {
                return m_decompressor.Actual_number_of_colors;
            }
            set
            {
                m_decompressor.Actual_number_of_colors = value;
            }
        }

        // The color map as a 2-D pixel array
        internal byte[][] Colormap
        {
            get
            {
                return m_decompressor.Colormap;
            }
            set
            {
                m_decompressor.Colormap = value;
            }
        }

        // These fields record data obtained from optional markers 
        // recognized by the JPEG library.

        // JFIF code for pixel size units
        internal byte DensityUnit
        {
            get
            {
                return m_decompressor.Density_unit;
            }
        }

        // Horizontal pixel density
        internal int DensityX
        {
            get
            {
                return m_decompressor.X_density;
            }
        }

        // Vertical pixel density
        internal int DensityY
        {
            get
            {
                return m_decompressor.Y_density;
            }
        }

        private void applyParameters(DecompressionParameters parameters)
        {
            Debug.Assert(parameters != null);

            if (parameters.OutColorspace != Colorspace.Unknown)
                m_decompressor.Out_color_space = (J_COLOR_SPACE)parameters.OutColorspace;

            m_decompressor.Scale_num = parameters.ScaleNumerator;
            m_decompressor.Scale_denom = parameters.ScaleDenominator;
            m_decompressor.Buffered_image = parameters.BufferedImage;
            m_decompressor.Raw_data_out = parameters.RawDataOut;
            m_decompressor.Dct_method = (J_DCT_METHOD)parameters.DCTMethod;
            m_decompressor.Dither_mode = (J_DITHER_MODE)parameters.DitherMode;
            m_decompressor.Do_fancy_upsampling = parameters.DoFancyUpsampling;
            m_decompressor.Do_block_smoothing = parameters.DoBlockSmoothing;
            m_decompressor.Quantize_colors = parameters.QuantizeColors;
            m_decompressor.Two_pass_quantize = parameters.TwoPassQuantize;
            m_decompressor.Desired_number_of_colors = parameters.DesiredNumberOfColors;
            m_decompressor.Enable_1pass_quant = parameters.EnableOnePassQuantizer;
            m_decompressor.Enable_external_quant = parameters.EnableExternalQuant;
            m_decompressor.Enable_2pass_quant = parameters.EnableTwoPassQuantizer;
            m_decompressor.Err.Trace_level = parameters.TraceLevel;
        }

        private void applyParameters(CompressionParameters parameters)
        {
            Debug.Assert(parameters != null);

            if (parameters.Colorspace != Colorspace.Unknown)
                m_compressor.jpeg_set_colorspace((J_COLOR_SPACE)parameters.Colorspace);

            m_compressor.Optimize_coding = parameters.OptimizeCoding;
            m_compressor.Restart_interval = parameters.RestartInterval;
            m_compressor.Restart_in_rows = parameters.RestartInRows;
            m_compressor.Smoothing_factor = parameters.SmoothingFactor;
            m_compressor.Dct_method = (J_DCT_METHOD)parameters.DCTMethod;
            m_compressor.Err.Trace_level = parameters.TraceLevel;

            m_compressor.jpeg_set_quality(parameters.Quality, parameters.ForceBaseline);

            if (parameters.SimpleProgressive)
                m_compressor.jpeg_simple_progression();
        }
    }
}
