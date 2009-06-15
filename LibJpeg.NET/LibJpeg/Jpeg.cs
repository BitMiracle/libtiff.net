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
        private jpeg_decompress_struct m_classicDecompressor = new jpeg_decompress_struct(new jpeg_error_mgr());

        public void Decompress(FileStream jpeg, DecompressionParameters parameters, Stream output)
        {
            if (jpeg == null)
                throw new ArgumentNullException("jpeg");

            if (parameters == null)
                throw new ArgumentNullException("parameters");

            if (output == null)
                throw new ArgumentNullException("output");

            m_classicDecompressor.jpeg_stdio_src(jpeg);
            /* Read file header, set default decompression parameters */
            m_classicDecompressor.jpeg_read_header(true);

            applyParameters(parameters);

            /* Initialize the output module now to let it override any crucial
             * option settings (for instance, GIF wants to force color quantization).
             */
            BitmapDestination dest_mgr = new BitmapDestination(this, parameters.ImageFormat == ImageFormat.BMP_OS2);
            dest_mgr.OutputFile = output;

            /* Start decompressor */
            m_classicDecompressor.jpeg_start_decompress();

            /* Write output file header */
            dest_mgr.start_output();

            /* Process data */
            while (m_classicDecompressor.Output_scanline < m_classicDecompressor.Output_height)
            {
                int num_scanlines = m_classicDecompressor.jpeg_read_scanlines(dest_mgr.buffer, dest_mgr.buffer_height);
                dest_mgr.put_pixel_rows(num_scanlines);
            }

            /* Finish decompression and release memory.
             * I must do it in this order because output module has allocated memory
             * of lifespan JPOOL_IMAGE; it needs to finish before releasing memory.
             */
            dest_mgr.finish_output();
            m_classicDecompressor.jpeg_finish_decompress();
        }

        public jpeg_decompress_struct ClassicDecompressor
        {
            get
            {
                return m_classicDecompressor;
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
            m_classicDecompressor.jpeg_set_marker_processor(markerCode, f);
        }

        /* Control saving of COM and APPn markers into marker_list. */
        public void SaveMarkers(int markerCode, int lengthLimit)
        {
            m_classicDecompressor.jpeg_save_markers(markerCode, lengthLimit);
        }

        // colorspace of JPEG image
        internal Colorspace Colorspace
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
        internal Colorspace OutColorspace
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

        // true=colormapped output wanted
        internal bool QuantizeColors
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
                return m_classicDecompressor.Output_width;
            }
        }

        // scaled image height
        internal int OutputHeight
        {
            get
            {
                return m_classicDecompressor.Output_height;
            }
        }

        // # of color components in out_color_space
        internal int OutComponentsPerSample
        {
            get
            {
                return m_classicDecompressor.Out_color_components;
            }
        }

        // # of color components returned. it is 1 (a colormap index) when 
        // quantizing colors; otherwise it equals out_color_components.
        internal int OutputComponents
        {
            get
            {
                return m_classicDecompressor.Output_components;
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
                return m_classicDecompressor.Actual_number_of_colors;
            }
            set
            {
                m_classicDecompressor.Actual_number_of_colors = value;
            }
        }

        // The color map as a 2-D pixel array
        internal byte[][] Colormap
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

        // These fields record data obtained from optional markers 
        // recognized by the JPEG library.

        // JFIF code for pixel size units
        internal byte DensityUnit
        {
            get
            {
                return m_classicDecompressor.Density_unit;
            }
        }

        // Horizontal pixel density
        internal int DensityX
        {
            get
            {
                return m_classicDecompressor.X_density;
            }
        }

        // Vertical pixel density
        internal int DensityY
        {
            get
            {
                return m_classicDecompressor.Y_density;
            }
        }

        private void applyParameters(DecompressionParameters parameters)
        {
            Debug.Assert(parameters != null);

            if (parameters.OutColorspace != Colorspace.Unknown)
                m_classicDecompressor.Out_color_space = (J_COLOR_SPACE)parameters.OutColorspace;

            m_classicDecompressor.Scale_num = parameters.ScaleNumerator;
            m_classicDecompressor.Scale_denom = parameters.ScaleDenominator;
            m_classicDecompressor.Buffered_image = parameters.BufferedImage;
            m_classicDecompressor.Raw_data_out = parameters.RawDataOut;
            m_classicDecompressor.Dct_method = (J_DCT_METHOD)parameters.DCTMethod;
            m_classicDecompressor.Dither_mode = (J_DITHER_MODE)parameters.DitherMode;
            m_classicDecompressor.Do_fancy_upsampling = parameters.DoFancyUpsampling;
            m_classicDecompressor.Do_block_smoothing = parameters.DoBlockSmoothing;
            m_classicDecompressor.Quantize_colors = parameters.QuantizeColors;
            m_classicDecompressor.Two_pass_quantize = parameters.TwoPassQuantize;
            m_classicDecompressor.Desired_number_of_colors = parameters.DesiredNumberOfColors;
            m_classicDecompressor.Enable_1pass_quant = parameters.EnableOnePassQuantizer;
            m_classicDecompressor.Enable_external_quant = parameters.EnableExternalQuant;
            m_classicDecompressor.Enable_2pass_quant = parameters.EnableTwoPassQuantizer;
            m_classicDecompressor.Err.Trace_level = parameters.TraceLevel;
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

        private Jpeg decompressor;
        private bool m_putGrayRows;
        private bool is_os2;        /* saves the OS2 format request flag */

        private jvirt_sarray_control whole_image;  /* needed to reverse row order */
        private int data_width;  /* bytes per row */
        private int row_width;       /* physical width of one row in the BMP file */
        private int pad_bytes;      /* number of padding bytes needed per row */
        private int cur_output_row;  /* next row# to write to virtual array */

        public BitmapDestination(Jpeg decompressor, bool is_os2)
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
            decompressor.ClassicDecompressor.jpeg_calc_output_dimensions();

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
