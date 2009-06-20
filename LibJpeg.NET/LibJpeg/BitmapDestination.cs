using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
    interface IDecompressDestination
    {
        Stream Output
        {
            get;
        }

        void Start();
        void ProcessPixelsRow(byte[] row);
        void Finish();
    }

    internal class BitmapDestination : IDecompressDestination
    {
        private Jpeg m_decompressor;

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
        private int m_dataWidth = 0;  /* bytes per row */
        private int m_rowWidth = 0;       /* physical width of one row in the BMP file */
        private int m_padBytes = 0;      /* number of padding bytes needed per row */
        private int m_currentOutputRow = 0;  /* next row# to write to virtual array */

        public BitmapDestination(Jpeg decompressor, Stream output, bool is_os2)
        {
            m_decompressor = decompressor;
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

        /// <summary>
        /// Startup: normally writes the file header.
        /// In this module we may as well postpone everything until finish_output.
        /// </summary>
        public void Start()
        {
            if (m_decompressor.OutColorspace == Colorspace.Grayscale)
                m_putGrayRows = true;
            else if (m_decompressor.OutColorspace == Colorspace.RGB)
                m_putGrayRows = m_decompressor.QuantizeColors;
            else
                m_decompressor.ClassicDecompressor.ERREXIT((J_MESSAGE_CODE)ADDON_MESSAGE_CODE.JERR_BMP_COLORSPACE);

            /* Determine width of rows in the BMP file (padded to 4-byte boundary). */
            m_rowWidth = m_decompressor.OutputWidth * m_decompressor.OutputComponents;
            m_dataWidth = m_rowWidth;
            while ((m_rowWidth & 3) != 0)
                m_rowWidth++;

            m_padBytes = (int)(m_rowWidth - m_dataWidth);

            /* Allocate space for inversion array, prepare for write pass */
            jpeg_decompress_struct cinfo = m_decompressor.ClassicDecompressor;
            m_wholeImage = new jvirt_sarray_control(cinfo, false, m_rowWidth, m_decompressor.OutputHeight);
            m_currentOutputRow = 0;

            /* Create decompressor output buffer. */
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

            jpeg_decompress_struct cinfo = m_decompressor.ClassicDecompressor;
            /* Write the file body from our virtual array */
            for (int row = cinfo.Output_height; row > 0; row--)
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
            for (int col = m_decompressor.OutputWidth; col > 0; col--)
            {
                image_ptr[0][imageIndex + 2] = m_buffer[0][bufferIndex];   /* can omit GETJSAMPLE() safely */
                bufferIndex++;
                image_ptr[0][imageIndex + 1] = m_buffer[0][bufferIndex];
                bufferIndex++;
                image_ptr[0][imageIndex] = m_buffer[0][bufferIndex];
                bufferIndex++;
                imageIndex += 3;
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
            for (int col = m_decompressor.OutputWidth; col > 0; col--)
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
            if (m_decompressor.OutColorspace == Colorspace.RGB)
            {
                if (m_decompressor.QuantizeColors)
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
            int bfSize = headersize + (int)m_rowWidth * (int)m_decompressor.OutputHeight;

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
            PUT_4B(bmpinfoheader, 4, m_decompressor.OutputWidth); /* biWidth */
            PUT_4B(bmpinfoheader, 8, m_decompressor.OutputHeight); /* biHeight */
            PUT_2B(bmpinfoheader, 12, 1);   /* biPlanes - must be 1 */
            PUT_2B(bmpinfoheader, 14, bits_per_pixel); /* biBitCount */
            /* we leave biCompression = 0, for none */
            /* we leave biSizeImage = 0; this is correct for uncompressed data */

            if (m_decompressor.DensityUnit == 2)
            {
                /* if have density in dots/cm, then */
                PUT_4B(bmpinfoheader, 24, m_decompressor.DensityX * 100); /* XPels/M */
                PUT_4B(bmpinfoheader, 28, m_decompressor.DensityY * 100); /* XPels/M */
            }
            PUT_2B(bmpinfoheader, 32, cmap_entries); /* biClrUsed */
            /* we leave biClrImportant = 0 */

            try
            {
                m_output.Write(bmpfileheader, 0, 14);
            }
            catch (Exception e)
            {
                m_decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                m_decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
            }

            try
            {
                m_output.Write(bmpinfoheader, 0, 40);
            }
            catch (Exception e)
            {
                m_decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                m_decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
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
            if (m_decompressor.OutColorspace == Colorspace.RGB)
            {
                if (m_decompressor.QuantizeColors)
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
            int bfSize = headersize + (int)m_rowWidth * m_decompressor.OutputHeight;

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
            PUT_2B(bmpcoreheader, 4, m_decompressor.OutputWidth); /* bcWidth */
            PUT_2B(bmpcoreheader, 6, m_decompressor.OutputHeight); /* bcHeight */
            PUT_2B(bmpcoreheader, 8, 1);    /* bcPlanes - must be 1 */
            PUT_2B(bmpcoreheader, 10, bits_per_pixel); /* bcBitCount */

            try
            {
                m_output.Write(bmpfileheader, 0, 14);
            }
            catch (Exception e)
            {
                m_decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                m_decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
            }

            try
            {
                m_output.Write(bmpcoreheader, 0, 12);
            }
            catch (Exception e)
            {
                m_decompressor.ClassicDecompressor.TRACEMS(0, J_MESSAGE_CODE.JERR_FILE_WRITE, e.Message);
                m_decompressor.ClassicDecompressor.ERREXIT(J_MESSAGE_CODE.JERR_FILE_WRITE);
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
            byte[][] colormap = m_decompressor.Colormap;
            int num_colors = m_decompressor.ActualNumberOfColors;

            int i = 0;
            if (colormap != null)
            {
                if (m_decompressor.OutComponentsPerSample == 3)
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
            {
                int errCode = 1026;//JERR_TOO_MANY_COLORS
                m_decompressor.ClassicDecompressor.ERREXIT(errCode, i);
            }

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
