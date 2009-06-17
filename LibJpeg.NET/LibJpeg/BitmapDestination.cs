using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
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
                decompressor.ClassicDecompressor.ERREXIT((J_MESSAGE_CODE)ADDON_MESSAGE_CODE.JERR_BMP_COLORSPACE);
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
