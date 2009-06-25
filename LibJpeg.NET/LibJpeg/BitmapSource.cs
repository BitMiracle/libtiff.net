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
    interface ICompressSource
    {
        void Start();
        byte[] GetPixelRow();
        void Finish();
    }

#if EXPOSE_LIBJPEG
    public
#endif
    class BitmapSource : ICompressSource
    {
        private enum PixelRowsMethod
        {
            preload,
            use8bit,
            use24bit
        }

        private Stream m_inputFile;
        private byte[] m_buffer;

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

        private int m_imageWidth = 0;
        private int m_imageHeight = 0;

        // remembers 8- or 24-bit format
        private int m_bitsPerPixel;

        public BitmapSource(jpeg_compress_struct cinfo, Stream input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            m_compressor = cinfo;
            m_inputFile = input;
        }

        /// <summary>
        /// Read the file header; detects image size and component count.
        /// </summary>
        public void Start()
        {
            byte[] bmpfileheader = new byte[14];
            /* Read and verify the bitmap file header */
            read(bmpfileheader, 0, 14);
            if (get2bytes(bmpfileheader, 0) != 0x4D42) /* 'BM' */
                throw new InvalidDataException("Need BMP image");

            int bfOffBits = get4bytes(bmpfileheader, 10);
            /* We ignore the remaining fileheader fields */

            /* The infoheader might be 12 bytes (OS/2 1.x), 40 bytes (Windows),
             * or 64 bytes (OS/2 2.x).  Check the first 4 bytes to find out which.
             */
            byte[] bmpinfoheader = new byte[64];
            read(bmpinfoheader, 0, 4);

            int headerSize = get4bytes(bmpinfoheader, 0);
            if (headerSize < 12 || headerSize > 64)
                throw new InvalidDataException("Bad BMP header");

            read(bmpinfoheader, 4, headerSize - 4);

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
                    m_imageWidth = get2bytes(bmpinfoheader, 4);
                    m_imageHeight = get2bytes(bmpinfoheader, 6);
                    biPlanes = get2bytes(bmpinfoheader, 8);
                    m_bitsPerPixel = get2bytes(bmpinfoheader, 10);

                    switch (m_bitsPerPixel)
                    {
                        case 8:
                            /* colormapped image */
                            mapentrysize = 3;       /* OS/2 uses RGBTRIPLE colormap */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP_OS2_MAPPED, m_imageWidth, m_imageHeight);
                            break;
                        case 24:
                            /* RGB image */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP_OS2, m_imageWidth, m_imageHeight);
                            break;
                        default:
                            throw new InvalidDataException("Unsupported color depth");
                    }
                    if (biPlanes != 1)
                        throw new InvalidDataException("Unsupported number of planes");
                    break;
                case 40:
                case 64:
                    /* Decode Windows 3.x header (Microsoft calls this a BITMAPINFOHEADER) */
                    /* or OS/2 2.x header, which has additional fields that we ignore */
                    m_imageWidth = get4bytes(bmpinfoheader, 4);
                    m_imageHeight = get4bytes(bmpinfoheader, 8);
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
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP_MAPPED, m_imageWidth, m_imageHeight);
                            break;
                        case 24:
                            /* RGB image */
                            m_compressor.TRACEMS(1, (int)ADDON_MESSAGE_CODE.JTRC_BMP, m_imageWidth, m_imageHeight);
                            break;
                        default:
                            throw new InvalidDataException("Unsupported color depth");
                    }
                    if (biPlanes != 1)
                        throw new InvalidDataException("Unsupported number of planes");
                    if (biCompression != 0)
                        throw new InvalidDataException("Compressed BMP is not supported");

                    if (biXPelsPerMeter > 0 && biYPelsPerMeter > 0)
                    {
                        /* Set JFIF density parameters from the BMP data */
                        m_compressor.X_density = (short)(biXPelsPerMeter / 100); /* 100 cm per meter */
                        m_compressor.Y_density = (short)(biYPelsPerMeter / 100);
                        m_compressor.Density_unit = 2;  /* dots/cm */
                    }
                    break;
                default:
                    throw new InvalidDataException("Wrong bitmap header");
            }

            /* Compute distance to bitmap data --- will adjust for colormap below */
            int bPad = bfOffBits - (headerSize + 14);

            /* Read the colormap, if any */
            if (mapentrysize > 0)
            {
                if (biClrUsed <= 0)
                    biClrUsed = 256;        /* assume it's 256 */
                else if (biClrUsed > 256)
                    throw new InvalidDataException("Bad BMP cmap");

                /* Allocate space to store the colormap */
                m_colorMap = jpeg_common_struct.AllocJpegSamples(biClrUsed, 3);
                /* and read it from the file */
                readColormap(biClrUsed, mapentrysize);
                /* account for size of colormap */
                bPad -= biClrUsed * mapentrysize;
            }

            /* Skip any remaining pad bytes */
            if (bPad < 0)           /* incorrect bfOffBits value? */
                throw new InvalidDataException("Bad BMP header");

            while (--bPad >= 0)
                readByte();

            /* Compute row width in file, including padding to 4-byte boundary */
            if (m_bitsPerPixel == 24)
                m_rowWidth = m_imageWidth * 3;
            else
                m_rowWidth = m_imageWidth;

            while ((m_rowWidth & 3) != 0)
                m_rowWidth++;

            /* Allocate space for inversion array, prepare for preload pass */
            m_wholeImage = new jvirt_sarray_control(m_rowWidth, m_imageHeight);
            m_pixelRowsMethod = PixelRowsMethod.preload;

            /* Allocate one-row buffer for returned data */
            m_buffer = new byte[m_imageWidth * 3];

            m_compressor.In_color_space = J_COLOR_SPACE.JCS_RGB;
            m_compressor.Input_components = 3;
            m_compressor.Data_precision = 8;
            m_compressor.Image_width = m_imageWidth;
            m_compressor.Image_height = m_imageHeight;
        }

        public byte[] GetPixelRow()
        {
            if (m_pixelRowsMethod == PixelRowsMethod.preload)
                preloadImage();

            if (m_pixelRowsMethod == PixelRowsMethod.use8bit)
                get8bitRow();
            else if (m_pixelRowsMethod == PixelRowsMethod.use24bit)
                get24bitRow();
            else
                Debug.Fail("");

            return m_buffer;
        }

        // Finish up at the end of the file.
        public void Finish()
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
        private void get8bitRow()
        {
            /* Fetch next row from virtual array */
            m_sourceRow--;

            byte[][] image_ptr = m_wholeImage.access_virt_sarray(m_sourceRow, 1);

            /* Expand the colormap indexes to real data */
            int imageIndex = 0;
            int outIndex = 0;
            for (int col = m_imageWidth; col > 0; col--)
            {
                int t = image_ptr[0][imageIndex];
                imageIndex++;

                m_buffer[outIndex] = m_colorMap[0][t]; /* can omit GETbyte() safely */
                outIndex++;
                m_buffer[outIndex] = m_colorMap[1][t];
                outIndex++;
                m_buffer[outIndex] = m_colorMap[2][t];
                outIndex++;
            }
        }

        /// <summary>
        /// Read one row of pixels. 
        /// The image has been read into the whole_image array, but is otherwise
        /// unprocessed.  We must read it out in top-to-bottom row order, and if
        /// it is an 8-bit image, we must expand colormapped pixels to 24bit format.
        /// This version is for reading 24-bit pixels.
        /// </summary>
        private void get24bitRow()
        {
            /* Fetch next row from virtual array */
            m_sourceRow--;
            byte[][] image_ptr = m_wholeImage.access_virt_sarray(m_sourceRow, 1);

            /* Transfer data.  Note source values are in BGR order
             * (even though Microsoft's own documents say the opposite).
             */
            int imageIndex = 0;
            int outIndex = 0;

            for (int col = m_imageWidth; col > 0; col--)
            {
                m_buffer[outIndex + 2] = image_ptr[0][imageIndex];   /* can omit GETbyte() safely */
                imageIndex++;
                m_buffer[outIndex + 1] = image_ptr[0][imageIndex];
                imageIndex++;
                m_buffer[outIndex] = image_ptr[0][imageIndex];
                imageIndex++;
                outIndex += 3;
            }
        }

        /// <summary>
        /// This method loads the image into whole_image during the first call on
        /// get_pixel_rows. 
        /// </summary>
        private void preloadImage()
        {
            /* Read the data into a virtual array in input-file row order. */
            for (int row = 0; row < m_imageHeight; row++)
            {
                byte[][] image_ptr = m_wholeImage.access_virt_sarray(row, 1);
                int imageIndex = 0;
                for (int col = m_rowWidth; col > 0; col--)
                {
                    /* inline copy of read_byte() for speed */
                    int c = m_inputFile.ReadByte();
                    if (c == -1)
                        throw new EndOfStreamException();

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
                    throw new InvalidDataException("Unsupported color depth");
            }

            m_sourceRow = m_imageHeight;
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
                    throw new InvalidDataException("Bad BMP cmap");
            }
        }

        // Read next byte from BMP file
        private int readByte()
        {
            int c = m_inputFile.ReadByte();
            if (c == -1)
                throw new EndOfStreamException();

            return c;
        }

        private void read(byte[] buffer, int offset, int len)
        {
            int read = m_inputFile.Read(buffer, offset, len);
            if (read != len)
                throw new EndOfStreamException();
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
