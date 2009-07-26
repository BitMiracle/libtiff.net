using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibJpeg
{
    class BitmapDestination : IDecompressDestination
    {
        /* Target file spec; filled in by djpeg.c after object is created. */
        private Stream m_output;

        /* Output pixel-row buffer.  Created by module init or start_output.
         * Width is cinfo.output_width * cinfo.output_components;
         * height is buffer_height.
         */
        private byte[][] m_buffer = null;

        private bool m_putGrayRows = false;
        private bool m_isOS2 = false;        /* saves the OS2 format request flag */

        private jvirt_sarray_control m_wholeImage = null;  /* needed to reverse row order */
        private int m_rowWidth = 0;       /* physical width of one row in the BMP file */
        private int m_padBytes = 0;      /* number of padding bytes needed per row */
        private int m_currentOutputRow = 0;  /* next row# to write to virtual array */
        private ImageParameters m_parameters;

        public BitmapDestination(Stream output, bool is_os2)
        {
            m_output = output;
            m_isOS2 = is_os2;
        }

        public Stream Output
        {
            get
            {
                return m_output;
            }
        }

        public void SetImageParameters(ImageParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            m_parameters = parameters;
        }

        /// <summary>
        /// Startup: normally writes the file header.
        /// In this module we may as well postpone everything until finish_output.
        /// </summary>
        public void Start()
        {
            if (m_parameters.Colorspace == Colorspace.Grayscale)
                m_putGrayRows = true;
            else
                m_putGrayRows = m_parameters.QuantizeColors;

            //Determine width of rows in the BMP file (padded to 4-byte boundary).
            m_rowWidth = m_parameters.Width * m_parameters.Components;
            int dataWidth = m_rowWidth;
            while ((m_rowWidth & 3) != 0)
                m_rowWidth++;

            m_padBytes = (int)(m_rowWidth - dataWidth);

            //Allocate space for inversion array, prepare for write pass
            m_wholeImage = new jvirt_sarray_control(m_rowWidth, m_parameters.Height);
            m_currentOutputRow = 0;

            //Create decompressor output buffer.
            m_buffer = jpeg_common_struct.AllocJpegSamples(m_rowWidth, 1);
        }

        /// <summary>
        /// Write some pixel data.
        /// </summary>
        public void ProcessPixelsRow(byte[] row)
        {
            for (int i = 0; i < row.Length; ++i)
                m_buffer[0][i] = row[i];

            if (m_putGrayRows)
                put_gray_row();
            else
                put_24bit_row();
        }

        /// <summary>
        /// Finish up at the end of the file.
        /// Here is where we really output the BMP file.
        /// </summary>
        public void Finish()
        {
            /* Write the header and colormap */
            if (m_isOS2)
                write_os2_header();
            else
                write_bmp_header();

            /* Write the file body from our virtual array */
            for (int row = m_parameters.Height; row > 0; row--)
            {
                byte[][] image_ptr = m_wholeImage.access_virt_sarray(row - 1, 1);
                int imageIndex = 0;
                for (int col = m_rowWidth; col > 0; col--)
                {
                    m_output.WriteByte(image_ptr[0][imageIndex]);
                    imageIndex++;
                }
            }

            /* Make sure we wrote the output file OK */
            m_output.Flush();
        }

        /// <summary>
        /// Write some pixel data.
        /// 
        /// This version is for writing 24-bit pixels
        /// </summary>
        private void put_24bit_row()
        {
            /* Access next row in virtual array */
            byte[][] image_ptr = m_wholeImage.access_virt_sarray(m_currentOutputRow, 1);
            m_currentOutputRow++;

            /* Transfer data.  Note destination values must be in BGR order
             * (even though Microsoft's own documents say the opposite).
             */
            int bufferIndex = 0;
            int imageIndex = 0;
            for (int col = m_parameters.Width; col > 0; --col, imageIndex += 3, bufferIndex += 3)
            {
                byte red = m_buffer[0][bufferIndex];
                byte green = m_buffer[0][bufferIndex + 1];
                byte blue = m_buffer[0][bufferIndex + 2];
                image_ptr[0][imageIndex] = blue;    /* can omit GETJSAMPLE() safely */
                image_ptr[0][imageIndex + 1] = green;
                image_ptr[0][imageIndex + 2] = red;
            }

            /* Zero out the pad bytes. */
            int pad = m_padBytes;
            while (--pad >= 0)
            {
                image_ptr[0][imageIndex] = 0;
                imageIndex++;
            }
        }

        /// <summary>
        /// Write some pixel data.
        /// 
        /// This version is for grayscale OR quantized color output
        /// </summary>
        private void put_gray_row()
        {
            /* Access next row in virtual array */
            byte[][] image_ptr = m_wholeImage.access_virt_sarray(m_currentOutputRow, 1);
            m_currentOutputRow++;

            /* Transfer data. */
            int index = 0;
            for (int col = m_parameters.Width; col > 0; col--)
            {
                image_ptr[0][index] = m_buffer[0][index];/* can omit GETJSAMPLE() safely */
                index++;
            }

            /* Zero out the pad bytes. */
            int pad = m_padBytes;
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
            if (m_parameters.Colorspace == Colorspace.RGB)
            {
                if (m_parameters.QuantizeColors)
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
            int bfSize = headersize + (int)m_rowWidth * (int)m_parameters.Height;

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
            PUT_4B(bmpinfoheader, 4, m_parameters.Width); /* biWidth */
            PUT_4B(bmpinfoheader, 8, m_parameters.Height); /* biHeight */
            PUT_2B(bmpinfoheader, 12, 1);   /* biPlanes - must be 1 */
            PUT_2B(bmpinfoheader, 14, bits_per_pixel); /* biBitCount */
            /* we leave biCompression = 0, for none */
            /* we leave biSizeImage = 0; this is correct for uncompressed data */

            if (m_parameters.DensityUnit == 2)
            {
                /* if have density in dots/cm, then */
                PUT_4B(bmpinfoheader, 24, m_parameters.DensityX * 100); /* XPels/M */
                PUT_4B(bmpinfoheader, 28, m_parameters.DensityY * 100); /* XPels/M */
            }
            PUT_2B(bmpinfoheader, 32, cmap_entries); /* biClrUsed */
            /* we leave biClrImportant = 0 */

            m_output.Write(bmpfileheader, 0, 14);
            m_output.Write(bmpinfoheader, 0, 40);

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
            if (m_parameters.Colorspace == Colorspace.RGB)
            {
                if (m_parameters.QuantizeColors)
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
            int bfSize = headersize + (int)m_rowWidth * m_parameters.Height;

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
            PUT_2B(bmpcoreheader, 4, m_parameters.Width); /* bcWidth */
            PUT_2B(bmpcoreheader, 6, m_parameters.Height); /* bcHeight */
            PUT_2B(bmpcoreheader, 8, 1);    /* bcPlanes - must be 1 */
            PUT_2B(bmpcoreheader, 10, bits_per_pixel); /* bcBitCount */

            m_output.Write(bmpfileheader, 0, 14);
            m_output.Write(bmpcoreheader, 0, 12);

            if (cmap_entries > 0)
                write_colormap(cmap_entries, 3);
        }

        /// <summary>
        /// Write the colormap.
        /// Windows uses BGR0 map entries; OS/2 uses BGR entries.
        /// </summary>
        private void write_colormap(int map_colors, int map_entry_size)
        {
            byte[][] colormap = m_parameters.Colormap;
            int num_colors = m_parameters.ActualNumberOfColors;

            int i = 0;
            if (colormap != null)
            {
                if (m_parameters.ComponentsPerSample == 3)
                {
                    /* Normal case with RGB colormap */
                    for (i = 0; i < num_colors; i++)
                    {
                        m_output.WriteByte(colormap[2][i]);
                        m_output.WriteByte(colormap[1][i]);
                        m_output.WriteByte(colormap[0][i]);
                        if (map_entry_size == 4)
                            m_output.WriteByte(0);
                    }
                }
                else
                {
                    /* Grayscale colormap (only happens with grayscale quantization) */
                    for (i = 0; i < num_colors; i++)
                    {
                        m_output.WriteByte(colormap[0][i]);
                        m_output.WriteByte(colormap[0][i]);
                        m_output.WriteByte(colormap[0][i]);
                        if (map_entry_size == 4)
                            m_output.WriteByte(0);
                    }
                }
            }
            else
            {
                /* If no colormap, must be grayscale data.  Generate a linear "map". */
                for (i = 0; i < 256; i++)
                {
                    m_output.WriteByte((byte)i);
                    m_output.WriteByte((byte)i);
                    m_output.WriteByte((byte)i);
                    if (map_entry_size == 4)
                        m_output.WriteByte(0);
                }
            }

            /* Pad colormap with zeros to ensure specified number of colormap entries */
            if (i > map_colors)
                throw new InvalidOperationException("Too many colors");

            for (; i < map_colors; i++)
            {
                m_output.WriteByte(0);
                m_output.WriteByte(0);
                m_output.WriteByte(0);
                if (map_entry_size == 4)
                    m_output.WriteByte(0);
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
