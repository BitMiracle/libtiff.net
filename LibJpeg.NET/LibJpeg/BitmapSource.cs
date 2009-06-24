using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
    class BitmapSource
    {
        private enum PixelRowsMethod
        {
            preload,
            use8bit,
            use24bit
        }

        public Stream m_inputFile;
        public byte[][] m_buffer;
        public int m_bufferHeight;

        private jpeg_compress_struct m_compressor;
        private PixelRowsMethod m_pixelRowsMethod;

        // BMP colormap (converted to my format)
        private byte[][] m_colorMap;

        // Needed to reverse row order
        private jvirt_sarray_control m_wholeImage;

        // Current source row number
        private int m_sourceRow;

        // Physical width of scanlines in file
        private int m_rowWidth;

        // remembers 8- or 24-bit format
        private int m_bitsPerPixel;

        public BitmapSource(jpeg_compress_struct cinfo)
        {
            m_compressor = cinfo;
        }

        public Stream InputFile
        {
            get
            {
                return m_inputFile;
            }
            set
            {
                m_inputFile = value;
            }
        }

        /// <summary>
        /// Read the file header; detects image size and component count.
        /// </summary>
        public void StartInput()
        {
            byte[] bmpfileheader = new byte[14];
            /* Read and verify the bitmap file header */
            if (!readOK(m_inputFile, bmpfileheader, 0, 14))
                m_compressor.ERREXIT(J_MESSAGE_CODE.JERR_INPUT_EOF);

            if (get2bytes(bmpfileheader, 0) != 0x4D42) /* 'BM' */
                m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_NOT);

            int bfOffBits = get4bytes(bmpfileheader, 10);
            /* We ignore the remaining fileheader fields */

            /* The infoheader might be 12 bytes (OS/2 1.x), 40 bytes (Windows),
             * or 64 bytes (OS/2 2.x).  Check the first 4 bytes to find out which.
             */
            byte[] bmpinfoheader = new byte[64];
            if (!readOK(m_inputFile, bmpinfoheader, 0, 4))
                m_compressor.ERREXIT(J_MESSAGE_CODE.JERR_INPUT_EOF);

            int headerSize = get4bytes(bmpinfoheader, 0);
            if (headerSize < 12 || headerSize > 64)
                m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADHEADER);

            if (!readOK(m_inputFile, bmpinfoheader, 4, headerSize - 4))
                m_compressor.ERREXIT(J_MESSAGE_CODE.JERR_INPUT_EOF);

            int biWidth = 0;      /* initialize to avoid compiler warning */
            int biHeight = 0;
            int biPlanes;
            int biCompression;
            int biXPelsPerMeter;
            int biYPelsPerMeter;
            int biClrUsed = 0;
            int mapentrysize = 0;       /* 0 indicates no colormap */
            switch (headerSize)
            {
                case 12:
                    /* Decode OS/2 1.x header (Microsoft calls this a BITMAPCOREHEADER) */
                    biWidth = get2bytes(bmpinfoheader, 4);
                    biHeight = get2bytes(bmpinfoheader, 6);
                    biPlanes = get2bytes(bmpinfoheader, 8);
                    m_bitsPerPixel = get2bytes(bmpinfoheader, 10);

                    switch (m_bitsPerPixel)
                    {
                        case 8:
                            /* colormapped image */
                            mapentrysize = 3;       /* OS/2 uses RGBTRIPLE colormap */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP_OS2_MAPPED, biWidth, biHeight);
                            break;
                        case 24:
                            /* RGB image */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP_OS2, biWidth, biHeight);
                            break;
                        default:
                            m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADDEPTH);
                            break;
                    }
                    if (biPlanes != 1)
                        m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADPLANES);
                    break;
                case 40:
                case 64:
                    /* Decode Windows 3.x header (Microsoft calls this a BITMAPINFOHEADER) */
                    /* or OS/2 2.x header, which has additional fields that we ignore */
                    biWidth = get4bytes(bmpinfoheader, 4);
                    biHeight = get4bytes(bmpinfoheader, 8);
                    biPlanes = get2bytes(bmpinfoheader, 12);
                    m_bitsPerPixel = get2bytes(bmpinfoheader, 14);
                    biCompression = get4bytes(bmpinfoheader, 16);
                    biXPelsPerMeter = get4bytes(bmpinfoheader, 24);
                    biYPelsPerMeter = get4bytes(bmpinfoheader, 28);
                    biClrUsed = get4bytes(bmpinfoheader, 32);
                    /* biSizeImage, biClrImportant fields are ignored */

                    switch (m_bitsPerPixel)
                    {
                        case 8:
                            /* colormapped image */
                            mapentrysize = 4;       /* Windows uses RGBQUAD colormap */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP_MAPPED, biWidth, biHeight);
                            break;
                        case 24:
                            /* RGB image */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP, biWidth, biHeight);
                            break;
                        default:
                            m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADDEPTH);
                            break;
                    }
                    if (biPlanes != 1)
                        m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADPLANES);
                    if (biCompression != 0)
                        m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_COMPRESSED);

                    if (biXPelsPerMeter > 0 && biYPelsPerMeter > 0)
                    {
                        /* Set JFIF density parameters from the BMP data */
                        m_compressor.X_density = (short)(biXPelsPerMeter / 100); /* 100 cm per meter */
                        m_compressor.Y_density = (short)(biYPelsPerMeter / 100);
                        m_compressor.Density_unit = 2;  /* dots/cm */
                    }
                    break;
                default:
                    m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADHEADER);
                    break;
            }

            /* Compute distance to bitmap data --- will adjust for colormap below */
            int bPad = bfOffBits - (headerSize + 14);

            /* Read the colormap, if any */
            if (mapentrysize > 0)
            {
                if (biClrUsed <= 0)
                    biClrUsed = 256;        /* assume it's 256 */
                else if (biClrUsed > 256)
                    m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADCMAP);
                /* Allocate space to store the colormap */
                m_colorMap = jpeg_common_struct.AllocJpegSamples(biClrUsed, 3);
                /* and read it from the file */
                readColormap(biClrUsed, mapentrysize);
                /* account for size of colormap */
                bPad -= biClrUsed * mapentrysize;
            }

            /* Skip any remaining pad bytes */
            if (bPad < 0)           /* incorrect bfOffBits value? */
                m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADHEADER);

            while (--bPad >= 0)
            {
                readByte();
            }

            /* Compute row width in file, including padding to 4-byte boundary */
            if (m_bitsPerPixel == 24)
                m_rowWidth = biWidth * 3;
            else
                m_rowWidth = biWidth;

            while ((m_rowWidth & 3) != 0)
                m_rowWidth++;

            /* Allocate space for inversion array, prepare for preload pass */
            m_wholeImage = new jvirt_sarray_control(m_compressor, m_rowWidth, biHeight);
            m_pixelRowsMethod = PixelRowsMethod.preload;

            /* Allocate one-row buffer for returned data */
            m_buffer = jpeg_common_struct.AllocJpegSamples(biWidth * 3, 1);
            m_bufferHeight = 1;

            m_compressor.In_color_space = J_COLOR_SPACE.JCS_RGB;
            m_compressor.Input_components = 3;
            m_compressor.Data_precision = 8;
            m_compressor.Image_width = biWidth;
            m_compressor.Image_height = biHeight;
        }

        public int GetPixelRows()
        {
            if (m_pixelRowsMethod == PixelRowsMethod.preload)
                return preloadImage();
            else if (m_pixelRowsMethod == PixelRowsMethod.use8bit)
                return get8bitRow();

            return get24bitRow();
        }

        // Finish up at the end of the file.
        public void FinishInput()
        {
            // no work
        }

        /// <summary>
        /// Read one row of pixels. 
        /// The image has been read into the whole_image array, but is otherwise
        /// unprocessed.  We must read it out in top-to-bottom row order, and if
        /// it is an 8-bit image, we must expand colormapped pixels to 24bit format.
        /// This version is for reading 8-bit colormap indexes.
        /// </summary>
        private int get8bitRow()
        {
            /* Fetch next row from virtual array */
            m_sourceRow--;

            byte[][] image_ptr = m_wholeImage.access_virt_sarray(m_sourceRow, 1);

            /* Expand the colormap indexes to real data */
            int imageIndex = 0;
            int outIndex = 0;
            for (int col = m_compressor.Image_width; col > 0; col--)
            {
                int t = image_ptr[0][imageIndex];
                imageIndex++;

                m_buffer[0][outIndex] = m_colorMap[0][t]; /* can omit GETbyte() safely */
                outIndex++;
                m_buffer[0][outIndex] = m_colorMap[1][t];
                outIndex++;
                m_buffer[0][outIndex] = m_colorMap[2][t];
                outIndex++;
            }

            return 1;
        }

        /// <summary>
        /// Read one row of pixels. 
        /// The image has been read into the whole_image array, but is otherwise
        /// unprocessed.  We must read it out in top-to-bottom row order, and if
        /// it is an 8-bit image, we must expand colormapped pixels to 24bit format.
        /// This version is for reading 24-bit pixels.
        /// </summary>
        private int get24bitRow()
        {
            /* Fetch next row from virtual array */
            m_sourceRow--;
            byte[][] image_ptr = m_wholeImage.access_virt_sarray(m_sourceRow, 1);

            /* Transfer data.  Note source values are in BGR order
             * (even though Microsoft's own documents say the opposite).
             */
            int imageIndex = 0;
            int outIndex = 0;

            for (int col = m_compressor.Image_width; col > 0; col--)
            {
                m_buffer[0][outIndex + 2] = image_ptr[0][imageIndex];   /* can omit GETbyte() safely */
                imageIndex++;
                m_buffer[0][outIndex + 1] = image_ptr[0][imageIndex];
                imageIndex++;
                m_buffer[0][outIndex] = image_ptr[0][imageIndex];
                imageIndex++;
                outIndex += 3;
            }

            return 1;
        }

        /// <summary>
        /// This method loads the image into whole_image during the first call on
        /// get_pixel_rows. 
        /// </summary>
        private int preloadImage()
        {
            /* Read the data into a virtual array in input-file row order. */
            for (int row = 0; row < m_compressor.Image_height; row++)
            {
                byte[][] image_ptr = m_wholeImage.access_virt_sarray(row, 1);
                int imageIndex = 0;
                for (int col = m_rowWidth; col > 0; col--)
                {
                    /* inline copy of read_byte() for speed */
                    int c = m_inputFile.ReadByte();
                    if (c == -1)
                        m_compressor.ERREXIT(J_MESSAGE_CODE.JERR_INPUT_EOF);

                    image_ptr[0][imageIndex] = (byte)c;
                    imageIndex++;
                }
            }

            /* Set up to read from the virtual array in top-to-bottom order */
            switch (m_bitsPerPixel)
            {
                case 8:
                    m_pixelRowsMethod = PixelRowsMethod.use8bit;
                    break;
                case 24:
                    m_pixelRowsMethod = PixelRowsMethod.use24bit;
                    break;
                default:
                    m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADDEPTH);
                    break;
            }

            m_sourceRow = m_compressor.Image_height;

            /* And read the first row */
            return GetPixelRows();
        }

        // Read next byte from BMP file
        private int readByte()
        {
            int c = m_inputFile.ReadByte();
            if (c == -1)
                m_compressor.ERREXIT(J_MESSAGE_CODE.JERR_INPUT_EOF);

            return c;
        }

        // Read the colormap from a BMP file
        private void readColormap(int cmaplen, int mapentrysize)
        {
            switch (mapentrysize)
            {
                case 3:
                    /* BGR format (occurs in OS/2 files) */
                    for (int i = 0; i < cmaplen; i++)
                    {
                        m_colorMap[2][i] = (byte)readByte();
                        m_colorMap[1][i] = (byte)readByte();
                        m_colorMap[0][i] = (byte)readByte();
                    }
                    break;
                case 4:
                    /* BGR0 format (occurs in MS Windows files) */
                    for (int i = 0; i < cmaplen; i++)
                    {
                        m_colorMap[2][i] = (byte)readByte();
                        m_colorMap[1][i] = (byte)readByte();
                        m_colorMap[0][i] = (byte)readByte();
                        readByte();
                    }
                    break;
                default:
                    m_compressor.ERREXIT((int)ADDON_MESSAGE_CODE.JERR_BMP_BADCMAP);
                    break;
            }
        }

        private static bool readOK(Stream file, byte[] buffer, int offset, int len)
        {
            int read = file.Read(buffer, offset, len);
            return (read == len);
        }

        private static int get2bytes(byte[] array, int offset)
        {
            return (int)array[offset] + ((int)array[offset + 1] << 8);
        }

        private static int get4bytes(byte[] array, int offset)
        {
            return (int)array[offset] + ((int)array[offset + 1] << 8) + ((int)array[offset + 2] << 16) + ((int)array[offset + 3] << 24);
        }
    }
}
