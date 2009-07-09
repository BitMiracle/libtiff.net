using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LibJpeg
{
    // see also http://en.wikipedia.org/wiki/BMP_file_format
    class BitmapImage
    {
        // Bitmap file marker ("BM")
        private static byte[] m_fileMarker = { 0x42, 0x4D };

        // Image types
        private const int c_VERSION2_1BIT = 0;
        private const int c_VERSION2_4BIT = 1;
        private const int c_VERSION2_8BIT = 2;
        private const int c_VERSION2_24BIT = 3;

        private const int c_VERSION3_1BIT = 4;
        private const int c_VERSION3_4BIT = 5;
        private const int c_VERSION3_8BIT = 6;
        private const int c_VERSION3_24BIT = 7;
        private const int c_VERSION3NT_16BIT = 8;
        private const int c_VERSION3NT_32BIT = 9;

        private const int c_VERSION4_1BIT = 10;
        private const int c_VERSION4_4BIT = 11;
        private const int c_VERSION4_8BIT = 12;
        private const int c_VERSION4_16BIT = 13;
        private const int c_VERSION4_24BIT = 14;
        private const int c_VERSION4_32BIT = 15;

        // Compression types
        private const int BI_RGB = 0; // no compression
        private const int BI_RLE8 = 1; // 8-bit RLE compression
        private const int BI_RLE4 = 2; // 4-bit RLE compression
        private const int BI_BITFIELDS = 3; // uncompressed. valid when used with 16- and 32-bpp bitmaps.

        // Colorspace types
        private const int LCS_CALIBRATED_RGB = 0;
        private const int LCS_sRGB = 1;
        private const int LCS_CMYK = 2;


        private int m_width;
        private int m_height;
        private int m_dpiX;
        private int m_dpiY;
        private int m_bitsPerComponent;
        private int m_componentsPerPixel;
        private MemoryStream m_bytes;

        // offset from beginning to a first data byte
        private int m_bitmapDataOffset;
        private int m_fileSize;
        private int m_imageSizeBytes;

        private int m_bitsPerPixel;
        private int m_imageType;
        private byte[] m_palette;
        private int m_compression;

        private int m_redMask;
        private int m_greenMask;
        private int m_blueMask;
        private int m_alphaMask;

        private bool m_bottomUpImage;

        public BitmapImage()
        {
        }

        public static bool ProbeFileMarker(byte[] marker)
        {
            return Utils.AreEqual(marker, m_fileMarker, m_fileMarker.Length);
        }

        public int Width
        {
            get { return m_width; }
        }

        public int Height
        {
            get { return m_height; }
        }

        public void Load(Stream data)
        {
            data.Seek(0, SeekOrigin.Begin);
            cleanUp();

            readFileHeader(data);
            readInfoHeader(data);

            processImageData(data);
        }

        protected void cleanUp()
        {
            m_fileSize = 0;
            m_imageSizeBytes = 0;

            m_bitmapDataOffset = 0;
            m_bitsPerPixel = 8;
            m_imageType = 0;
            m_palette = null;
            m_compression = 0;

            m_redMask = 0;
            m_greenMask = 0;
            m_blueMask = 0;
            m_alphaMask = 0;

            m_bottomUpImage = true;
        }

        private void readFileHeader(Stream data)
        {
            byte[] fileMarker = new byte[m_fileMarker.Length];
            data.Read(fileMarker, 0, fileMarker.Length);

            if (!ProbeFileMarker(fileMarker))
                throw new InvalidDataException("Invalid BMP image");

            m_fileSize = Utils.ReadInt(data);
            if (data.Length < m_fileSize)
                throw new InvalidDataException("Invalid BMP image");

            // skip two reserved shorts
            Utils.ReadShort(data);
            Utils.ReadShort(data);

            m_bitmapDataOffset = Utils.ReadInt(data);
        }

        private void readInfoHeader(Stream data)
        {
            int headerLength = Utils.ReadInt(data);

            if (headerLength == 12)
            {
                m_width = Utils.ReadShort(data);
                m_height = Utils.ReadShort(data);
            }
            else
            {
                m_width = Utils.ReadInt(data);
                m_height = Utils.ReadInt(data);
            }

            // number of planes for the target device
            Utils.ReadShort(data);

            m_bitsPerPixel = Utils.ReadShort(data);

            if (m_bitmapDataOffset == 0)
            {
                // loading DIB
                m_bitmapDataOffset = headerLength;
            }

            if (headerLength == 12)
            {
                // V2 (Windows 2.x or OS/2 1.x bitmap)
                readV2InfoHeader(data, headerLength);
                return;
            }

            m_compression = Utils.ReadInt(data);
            m_imageSizeBytes = Utils.ReadInt(data);

            int temp = Utils.ReadInt(data);
            m_dpiX = (int)(temp * 0.0254 + 0.5);

            temp = Utils.ReadInt(data);
            m_dpiY = (int)(temp * 0.0254 + 0.5);

            int colorsUsed = Utils.ReadInt(data);
            Utils.ReadInt(data); // colorsImportant

            if (headerLength == 40)
            {
                // V3 (Windows 3.x or Windows NT bitmap)
                readV3InfoHeader(data, headerLength, colorsUsed);
            }
            else if (headerLength == 108)
            {
                // V4 (Windows 4.x bitmap)
                readV4InfoHeader(data, headerLength, colorsUsed);
            }
            else
            {
                // V5 (Windows 2000 and later bitmap)
                throw new InvalidDataException("Unsupported BMP image");
            }

            checkHeight();
        }

        private void checkHeight() 
        {
            if (m_height > 0)
            {
                // bottom up image
                m_bottomUpImage = true;
            }
            else
            {
                // top down image
                m_bottomUpImage = false;
                m_height = -m_height;
            }
        }

        private void readV2InfoHeader(Stream data, int headerLength)
        {
            if (m_bitsPerPixel == 1)
                m_imageType = c_VERSION2_1BIT;
            else if (m_bitsPerPixel == 4)
                m_imageType = c_VERSION2_4BIT;
            else if (m_bitsPerPixel == 8)
                m_imageType = c_VERSION2_8BIT;
            else if (m_bitsPerPixel == 24)
                m_imageType = c_VERSION2_24BIT;

            int numberOfEntries = (int)((m_bitmapDataOffset - 14 - headerLength) / 3);
            int sizeOfPalette = numberOfEntries * 3;
            if (m_bitmapDataOffset == headerLength)
            {
                switch (m_imageType)
                {
                    case c_VERSION2_1BIT:
                        sizeOfPalette = 2 * 3;
                        break;
                    case c_VERSION2_4BIT:
                        sizeOfPalette = 16 * 3;
                        break;
                    case c_VERSION2_8BIT:
                        sizeOfPalette = 256 * 3;
                        break;
                    case c_VERSION2_24BIT:
                        sizeOfPalette = 0;
                        break;
                }
                
                m_bitmapDataOffset = headerLength + sizeOfPalette;
            }

            readPalette(data, sizeOfPalette);
        }

        private void readPalette(Stream data, int sizeOfPalette)
        {
            if (sizeOfPalette == 0)
                return;

            m_palette = new byte[sizeOfPalette];
            int bytesRead = data.Read(m_palette, 0, sizeOfPalette);
            if (bytesRead <= 0)
                throw new InvalidDataException("Invalid BMP image");
        }

        private void readV3InfoHeader(Stream data, int headerLength, int colorsUsed)
        {
            switch (m_compression)
            {
                case BI_RGB:
                case BI_RLE8:
                case BI_RLE4:
                    if (m_bitsPerPixel == 1)
                        m_imageType = c_VERSION3_1BIT;
                    else if (m_bitsPerPixel == 4)
                        m_imageType = c_VERSION3_4BIT;
                    else if (m_bitsPerPixel == 8)
                        m_imageType = c_VERSION3_8BIT;
                    else if (m_bitsPerPixel == 24)
                        m_imageType = c_VERSION3_24BIT;
                    else if (m_bitsPerPixel == 16)
                    {
                        m_imageType = c_VERSION3NT_16BIT;
                        m_redMask = 0x7C00;
                        m_greenMask = 0x3E0;
                        m_blueMask = 0x1F;
                    }
                    else if (m_bitsPerPixel == 32)
                    {
                        m_imageType = c_VERSION3NT_32BIT;
                        m_redMask = 0x00FF0000;
                        m_greenMask = 0x0000FF00;
                        m_blueMask = 0x000000FF;
                    }

                    int numberOfEntries = (int)((m_bitmapDataOffset - 14 - headerLength) / 4);
                    int sizeOfPalette = numberOfEntries * 4;
                    if (m_bitmapDataOffset == headerLength)
                    {
                        switch (m_imageType)
                        {
                            case c_VERSION3_1BIT:
                                sizeOfPalette = (colorsUsed == 0 ? 2 : colorsUsed) * 4;
                                break;
                            case c_VERSION3_4BIT:
                                sizeOfPalette = (colorsUsed == 0 ? 16 : colorsUsed) * 4;
                                break;
                            case c_VERSION3_8BIT:
                                sizeOfPalette = (colorsUsed == 0 ? 256 : colorsUsed) * 4;
                                break;
                            default:
                                sizeOfPalette = 0;
                                break;
                        }
                        
                        m_bitmapDataOffset = headerLength + sizeOfPalette;
                    }

                    readPalette(data, sizeOfPalette);
                    break;

                case BI_BITFIELDS:
                    if (m_bitsPerPixel == 16)
                        m_imageType = c_VERSION3NT_16BIT;
                    else if (m_bitsPerPixel == 32)
                        m_imageType = c_VERSION3NT_32BIT;

                    m_redMask = Utils.ReadInt(data);
                    m_greenMask = Utils.ReadInt(data);
                    m_blueMask = Utils.ReadInt(data);

                    if (colorsUsed != 0)
                    {
                        sizeOfPalette = colorsUsed * 4;
                        readPalette(data, sizeOfPalette);
                    }

                    break;

                default:
                    throw new InvalidDataException("Invalid BMP image");
            }
        }

        private void readV4InfoHeader(Stream data, int headerLength, int colorsUsed)
        {
            // rgb masks, valid only if compression is BI_BITFIELDS
            m_redMask = Utils.ReadInt(data);
            m_greenMask = Utils.ReadInt(data);
            m_blueMask = Utils.ReadInt(data);

            // Only supported for 32bpp BI_RGB argb
            m_alphaMask = Utils.ReadInt(data);
            
            int csType = Utils.ReadInt(data);
            switch (csType)
            {
                case LCS_CALIBRATED_RGB:
                    throw new InvalidDataException("Unsupported BMP image");

                case LCS_sRGB:
                    // Default Windows color space
                    break;

                case LCS_CMYK:
                    throw new InvalidDataException("Unsupported BMP image");
            }

            /*int redX = */Utils.ReadInt(data);
            /*int redY = */Utils.ReadInt(data);
            /*int redZ = */Utils.ReadInt(data);
            /*int greenX = */Utils.ReadInt(data);
            /*int greenY = */Utils.ReadInt(data);
            /*int greenZ = */Utils.ReadInt(data);
            /*int blueX = */Utils.ReadInt(data);
            /*int blueY = */Utils.ReadInt(data);
            /*int blueZ = */Utils.ReadInt(data);
            /*int gammaRed = */Utils.ReadInt(data);
            /*int gammaGreen = */Utils.ReadInt(data);
            /*int gammaBlue = */Utils.ReadInt(data);
            
            if (m_bitsPerPixel == 1)
                m_imageType = c_VERSION4_1BIT;
            else if (m_bitsPerPixel == 4)
                m_imageType = c_VERSION4_4BIT;
            else if (m_bitsPerPixel == 8)
                m_imageType = c_VERSION4_8BIT;
            else if (m_bitsPerPixel == 16)
            {
                m_imageType = c_VERSION4_16BIT;
                if (m_compression == BI_RGB)
                {
                    m_redMask = 0x7C00;
                    m_greenMask = 0x3E0;
                    m_blueMask = 0x1F;
                }
            }
            else if (m_bitsPerPixel == 24)
                m_imageType = c_VERSION4_24BIT;
            else if (m_bitsPerPixel == 32)
            {
                m_imageType = c_VERSION4_32BIT;
                if (m_compression == BI_RGB)
                {
                    m_redMask = 0x00FF0000;
                    m_greenMask = 0x0000FF00;
                    m_blueMask = 0x000000FF;
                }
            }

            // Read in the palette
            int numberOfEntries = (int)((m_bitmapDataOffset - 14 - headerLength) / 4);
            int sizeOfPalette = numberOfEntries * 4;
            if (m_bitmapDataOffset == headerLength)
            {
                switch (m_imageType)
                {
                    case c_VERSION4_1BIT:
                        sizeOfPalette = (int)(colorsUsed == 0 ? 2 : colorsUsed) * 4;
                        break;
                    case c_VERSION4_4BIT:
                        sizeOfPalette = (int)(colorsUsed == 0 ? 16 : colorsUsed) * 4;
                        break;
                    case c_VERSION4_8BIT:
                        sizeOfPalette = (int)(colorsUsed == 0 ? 256 : colorsUsed) * 4;
                        break;
                    default:
                        sizeOfPalette = 0;
                        break;
                }

                m_bitmapDataOffset = headerLength + sizeOfPalette;
            }

            readPalette(data, sizeOfPalette);
        }

        private void processImageData(Stream data)
        {
            switch (m_imageType)
            {
                case c_VERSION2_1BIT:
                    process1Bit(data, 3);
                    break;

                case c_VERSION2_4BIT:
                    process4Bit(data, 3);
                    break;

                case c_VERSION2_8BIT:
                    process8Bit(data, 3);
                    break;

                case c_VERSION2_24BIT:
                    process24Bit(data);
                    break;

                case c_VERSION3_1BIT:
                    process1Bit(data, 4);
                    break;

                case c_VERSION3_4BIT:
                    switch (m_compression)
                    {
                        case BI_RGB:
                            process4Bit(data, 4);
                            break;

                        case BI_RLE4:
                            processRLE4(data);
                            break;

                        default:
                            throw new InvalidDataException("Unsupported BMP image");
                    }
                    break;

                case c_VERSION3_8BIT:
                    switch (m_compression)
                    {
                        case BI_RGB:
                            process8Bit(data, 4);
                            break;

                        case BI_RLE8:
                            processRLE8(data);
                            break;

                        default:
                            throw new InvalidDataException("Unsupported BMP image");
                    }
                    break;

                case c_VERSION3_24BIT:
                    process24Bit(data);
                    break;

                case c_VERSION3NT_16BIT:
                    process16or32Bit(data, false);
                    break;

                case c_VERSION3NT_32BIT:
                    process16or32Bit(data, true);
                    break;

                case c_VERSION4_1BIT:
                    process1Bit(data, 4);
                    break;

                case c_VERSION4_4BIT:
                    switch (m_compression)
                    {
                        case BI_RGB:
                            process4Bit(data, 4);
                            break;

                        case BI_RLE4:
                            processRLE4(data);
                            break;

                        default:
                            throw new InvalidDataException("Unsupported BMP image");
                    }
                    break;

                case c_VERSION4_8BIT:
                    switch (m_compression)
                    {
                        case BI_RGB:
                            process8Bit(data, 4);
                            break;

                        case BI_RLE8:
                            processRLE8(data);
                            break;

                        default:
                            throw new InvalidDataException("Unsupported BMP image");
                    }
                    break;

                case c_VERSION4_16BIT:
                    process16or32Bit(data, false);
                    break;

                case c_VERSION4_24BIT:
                    process24Bit(data);
                    break;

                case c_VERSION4_32BIT:
                    process16or32Bit(data, true);
                    break;

                default:
                    throw new InvalidDataException("Unsupported BMP image");
            }
        }

        private void process1Bit(Stream data, int paletteEntries)
        {
            byte[] bytes = new byte[((m_width + 7) / 8) * m_height];

            int padding = 0;
            int bytesPerScanline = (int)Math.Ceiling((double)m_width / 8.0);
            int remainder = bytesPerScanline % 4;
            if (remainder != 0)
                padding = 4 - remainder;

            int imageSize = (bytesPerScanline + padding) * m_height;
            byte[] values = new byte[imageSize];
            int bytesRead = data.Read(values, 0, imageSize);
            if (bytesRead != imageSize)
                throw new InvalidDataException("Invalid BMP image");

            if (m_bottomUpImage)
            {
                // Convert the bottom up image to a top down format by copying
                // one scanline from the bottom to the top at a time.
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(values, imageSize - (i + 1) * (bytesPerScanline + padding),
                        bytes, i * bytesPerScanline, bytesPerScanline);
                }
            }
            else
            {
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(values, i * (bytesPerScanline + padding),
                        bytes, i * bytesPerScanline, bytesPerScanline);
                }
            }

            fillIndexedImageProperties(bytes, 1, paletteEntries);
        }

        private void process4Bit(Stream data, int paletteEntries)
        {
            byte[] bytes = new byte[((m_width + 1) / 2) * m_height];

            int padding = 0;
            int bytesPerScanline = (int)Math.Ceiling((double)m_width / 2.0);
            int remainder = bytesPerScanline % 4;
            if (remainder != 0)
                padding = 4 - remainder;

            int imageSize = (bytesPerScanline + padding) * m_height;
            byte[] values = new byte[imageSize];
            int bytesRead = data.Read(values, 0, imageSize);
            if (bytesRead != imageSize)
                throw new InvalidDataException("Invalid BMP image");

            if (m_bottomUpImage)
            {
                // Convert the bottom up image to a top down format by copying
                // one scanline from the bottom to the top at a time.
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(values, imageSize - (i + 1) * (bytesPerScanline + padding),
                        bytes, i * bytesPerScanline, bytesPerScanline);
                }
            }
            else
            {
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(values, i * (bytesPerScanline + padding),
                        bytes, i * bytesPerScanline, bytesPerScanline);
                }
            }

            fillIndexedImageProperties(bytes, 4, paletteEntries);
        }

        private void process8Bit(Stream data, int paletteEntries)
        {
            byte[] bytes = new byte[m_width * m_height];

            int padding = 0;
            int bitsPerScanline = m_width * 8;
            if (bitsPerScanline % 32 != 0)
            {
                // width * bitsPerPixel should be divisible by 32
                padding = (bitsPerScanline / 32 + 1) * 32 - bitsPerScanline;
                padding = (int)Math.Ceiling(padding / 8.0);
            }

            int imageSize = (m_width + padding) * m_height;
            byte[] values = new byte[imageSize];
            int bytesRead = data.Read(values, 0, imageSize);
            if (bytesRead != imageSize)
                throw new InvalidDataException("Invalid BMP image");

            if (m_bottomUpImage)
            {
                // Convert the bottom up image to a top down format by copying
                // one scanline from the bottom to the top at a time.
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(values, imageSize - (i + 1) * (m_width + padding),
                        bytes, i * m_width, m_width);
                }
            }
            else
            {
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(values, i * (m_width + padding),
                        bytes, i * m_width, m_width);
                }
            }

            fillIndexedImageProperties(bytes, 8, paletteEntries);
        }

        private void process24Bit(Stream data)
        {
            byte[] bytes = new byte[m_width * m_height * 3];

            int padding = 0;
            int bitsPerScanline = m_width * 24;
            if (bitsPerScanline % 32 != 0)
            {
                // width * bitsPerPixel should be divisible by 32
                padding = (bitsPerScanline / 32 + 1) * 32 - bitsPerScanline;
                padding = (int)Math.Ceiling(padding / 8.0);
            }

            int imageSize = ((m_width * 3 + 3) / 4 * 4) * m_height;
            byte[] values = new byte[imageSize];
            int bytesRead = data.Read(values, 0, imageSize);
            if (bytesRead != imageSize)
                throw new InvalidDataException("Invalid BMP image");

            if (m_bottomUpImage)
            {
                int max = m_width * m_height * 3 - 1;

                int count = -padding;
                for (int i = 0; i < m_height; i++)
                {
                    int l = max - (i + 1) * m_width * 3 + 1;
                    count += padding;
                    for (int j = 0; j < m_width; j++)
                    {
                        bytes[l + 2] = values[count++];
                        bytes[l + 1] = values[count++];
                        bytes[l] = values[count++];
                        l += 3;
                    }
                }
            }
            else
            {
                int count = -padding;
                int l = 0;
                for (int i = 0; i < m_height; i++)
                {
                    count += padding;
                    for (int j = 0; j < m_width; j++)
                    {
                        bytes[l + 2] = values[count++];
                        bytes[l + 1] = values[count++];
                        bytes[l] = values[count++];
                        l += 3;
                    }
                }
            }

            fillImageProperties(bytes, 3, 8);
        }

        private void processRLE4(Stream data)
        {
            int encodedSize = m_imageSizeBytes;
            if (encodedSize == 0)
                encodedSize = m_fileSize - m_bitmapDataOffset;

            byte[] encodedBytes = new byte[encodedSize];
            int bytesRead = data.Read(encodedBytes, 0, encodedSize);
            if (bytesRead != encodedSize)
                throw new InvalidDataException("Invalid BMP image");

            byte[] decodedBytes = decodeRLE(false, encodedBytes);

            if (m_bottomUpImage)
            {
                byte[] inverted = decodedBytes;
                decodedBytes = new byte[m_width * m_height];
                
                int l = 0;
                for (int i = m_height - 1; i >= 0; i--)
                {
                    int index = i * m_width;
                    int lineEnd = l + m_width;
                    while (l != lineEnd)
                        decodedBytes[l++] = inverted[index++];
                }
            }

            int stride = ((m_width + 1) / 2);
            byte[] bytes = new byte[stride * m_height];
            int ptr = 0;
            int sh = 0;
            for (int h = 0; h < m_height; ++h)
            {
                for (int w = 0; w < m_width; ++w)
                {
                    if ((w & 1) == 0)
                        bytes[sh + w / 2] = (byte)(decodedBytes[ptr++] << 4);
                    else
                        bytes[sh + w / 2] |= (byte)(decodedBytes[ptr++] & 0x0f);
                }
                
                sh += stride;
            }

            fillIndexedImageProperties(bytes, 4, 4);
        }

        private void processRLE8(Stream data)
        {
            int encodedSize = m_imageSizeBytes;
            if (encodedSize == 0)
                encodedSize = m_fileSize - m_bitmapDataOffset;

            byte[] encodedBytes = new byte[encodedSize];
            int bytesRead = data.Read(encodedBytes, 0, encodedSize);
            if (bytesRead != encodedSize)
                throw new InvalidDataException("Invalid BMP image");

            byte[] decodedBytes = decodeRLE(true, encodedBytes);

            // uncompressed data does not have any padding
            encodedSize = m_width * m_height;

            if (m_bottomUpImage)
            {
                // Convert the bottom up image to a top down format by copying
                // one scanline from the bottom to the top at a time.
                byte[] temp = new byte[decodedBytes.Length];
                int bytesPerScanline = m_width;
                for (int i = 0; i < m_height; i++)
                {
                    Array.Copy(decodedBytes, encodedSize - (i + 1) * (bytesPerScanline),
                        temp, i * bytesPerScanline, bytesPerScanline);
                }

                decodedBytes = temp;
            }

            fillIndexedImageProperties(decodedBytes, 8, 4);
        }

        private void process16or32Bit(Stream data, bool read32Bit)
        {
            int redMask = findMask(m_redMask);
            int redShift = findShift(m_redMask);
            int redFactor = redMask + 1;

            int greenMask = findMask(m_greenMask);
            int greenShift = findShift(m_greenMask);
            int greenFactor = greenMask + 1;
            
            int blueMask = findMask(m_blueMask);
            int blueShift = findShift(m_blueMask);
            int blueFactor = blueMask + 1;

            byte[] bytes = new byte[m_width * m_height * 3];
            
            int padding = 0;
            if (!read32Bit)
            {
                int bitsPerScanline = m_width * 16;
                if (bitsPerScanline % 32 != 0)
                {
                    // width * bitsPerPixel should be divisible by 32
                    padding = (bitsPerScanline / 32 + 1) * 32 - bitsPerScanline;
                    padding = (int)Math.Ceiling(padding / 8.0);
                }
            }

            int imageSize = m_imageSizeBytes;
            if (imageSize == 0)
                imageSize = m_fileSize - m_bitmapDataOffset;

            int l = 0;
            int v;
            if (m_bottomUpImage)
            {
                for (int i = m_height - 1; i >= 0; --i)
                {
                    l = m_width * 3 * i;
                    for (int j = 0; j < m_width; j++)
                    {
                        if (read32Bit)
                            v = Utils.ReadInt(data);
                        else
                            v = Utils.ReadShort(data);

                        bytes[l++] = (byte)((Utils.UShiftRight(v, redShift) & redMask) * 256 / redFactor);
                        bytes[l++] = (byte)((Utils.UShiftRight(v, greenShift) & greenMask) * 256 / greenFactor);
                        bytes[l++] = (byte)((Utils.UShiftRight(v, blueShift) & blueMask) * 256 / blueFactor);
                    }

                    for (int m = 0; m < padding; m++)
                        data.ReadByte();
                }
            }
            else
            {
                for (int i = 0; i < m_height; i++)
                {
                    for (int j = 0; j < m_width; j++)
                    {
                        if (read32Bit)
                            v = Utils.ReadInt(data);
                        else
                            v = Utils.ReadShort(data);

                        bytes[l++] = (byte)((Utils.UShiftRight(v, redShift) & redMask) * 256 / redFactor);
                        bytes[l++] = (byte)((Utils.UShiftRight(v, greenShift) & greenMask) * 256 / greenFactor);
                        bytes[l++] = (byte)((Utils.UShiftRight(v, blueShift) & blueMask) * 256 / blueFactor);
                    }

                    for (int m = 0; m < padding; m++)
                        data.ReadByte();
                }
            }

            fillImageProperties(bytes, 3, 8);
        }

        private void fillIndexedImageProperties(byte[] bytes, int bpc, int paletteEntries)
        {
            m_componentsPerPixel = 1;
            m_bitsPerComponent = bpc;

            m_bytes = new MemoryStream(bytes);
        }

        private void fillImageProperties(byte[] bytes, int componentCount, int bpc)
        {
            m_componentsPerPixel = componentCount;
            m_bitsPerComponent = bpc;

            m_bytes = new MemoryStream(bytes);
        }

        private byte[] getPalette(int groupLength)
        {
            if (m_palette == null)
                return null;

            byte[] paletteBytes = new byte[m_palette.Length / groupLength * 3];
            int groupCount = m_palette.Length / groupLength;

            for (int i = 0; i < groupCount; ++i)
            {
                int src = i * groupLength;
                int dest = i * 3;
                paletteBytes[dest + 2] = m_palette[src++];
                paletteBytes[dest + 1] = m_palette[src++];
                paletteBytes[dest] = m_palette[src];
            }

            return paletteBytes;
        }

        private byte[] decodeRLE(bool decodeAsRLE8, byte[] encodedBytes)
        {
            byte[] decodedBytes = new byte[m_width * m_height];

            int ptr = 0;
            int x = 0;
            int q = 0;
            for (int y = 0; y < m_height && ptr < encodedBytes.Length; )
            {
                int count = encodedBytes[ptr++] & 0xff;
                if (count != 0)
                {
                    // encoded mode
                    int bt = encodedBytes[ptr++] & 0xff;
                    if (decodeAsRLE8)
                    {
                        for (int i = count; i != 0; --i)
                        {
                            if (q >= decodedBytes.Length)
                                break;

                            decodedBytes[q++] = (byte)bt;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            if (q >= decodedBytes.Length)
                                break;

                            decodedBytes[q++] = (byte)((i & 1) == 1 ? (bt & 0x0f) : ((bt >> 4) & 0x0f));
                        }
                    }
                    
                    x += count;
                }
                else
                {
                    // escape mode
                    count = encodedBytes[ptr++] & 0xff;
                    if (count == 1)
                        break;
                    
                    switch (count)
                    {
                        case 0:
                            x = 0;
                            ++y;
                            q = y * m_width;
                            break;
                        
                        case 2:
                            // delta mode
                            x += encodedBytes[ptr++] & 0xff;
                            y += encodedBytes[ptr++] & 0xff;
                            q = y * m_width + x;
                            break;
                        
                        default:
                            // absolute mode
                            if (decodeAsRLE8)
                            {
                                for (int i = count; i != 0; --i)
                                    decodedBytes[q++] = (byte)(encodedBytes[ptr++] & 0xff);
                            }
                            else
                            {
                                int bt = 0;
                                for (int i = 0; i < count; ++i)
                                {
                                    if ((i & 1) == 0)
                                        bt = encodedBytes[ptr++] & 0xff;
                                    
                                    decodedBytes[q++] = (byte)((i & 1) == 1 ? (bt & 0x0f) : ((bt >> 4) & 0x0f));
                                }
                            }

                            x += count;
                            
                            // read pad byte
                            if (decodeAsRLE8)
                            {
                                if ((count & 1) == 1)
                                    ++ptr;
                            }
                            else
                            {
                                if ((count & 3) == 1 || (count & 3) == 2)
                                    ++ptr;
                            }
                            break;
                    }
                }
            }

            return decodedBytes;
        }

        private int findMask(int mask)
        {
            int k = 0;
            for (; k < 32; ++k)
            {
                if ((mask & 1) == 1)
                    break;

                mask = Utils.UShiftRight(mask, 1);
            }

            return mask;
        }

        private int findShift(int mask)
        {
            int k = 0;
            for (; k < 32; ++k)
            {
                if ((mask & 1) == 1)
                    break;

                mask = Utils.UShiftRight(mask, 1);
            }

            return k;
        }
    }

    class Utils
    {
        public static MemoryStream CopyStream(Stream imageData)
        {
            if (imageData == null)
                throw new ArgumentNullException("imageData");

            long positionBefore = imageData.Position;
            imageData.Seek(0, SeekOrigin.Begin);

            MemoryStream result = new MemoryStream((int)imageData.Length);

            byte[] block = new byte[2048];
            for (; ; )
            {
                int bytesRead = imageData.Read(block, 0, 2048);
                result.Write(block, 0, bytesRead);
                if (bytesRead < 2048)
                    break;
            }

            imageData.Seek(positionBefore, SeekOrigin.Begin);
            return result;
        }

        public static int memcmp(byte[] left, byte[] right, int length)
        {
            return memcmp(left, 0, right, length);
        }

        public static int memcmp(byte[] left, int offset, byte[] right, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (left[offset + i] != right[i])
                    return left[offset + i] - right[i];
            }

            return 0;
        }

        public static bool AreEqual(byte[] left, byte[] right, int length)
        {
            return (memcmp(left, right, length) == 0);
        }

        public static int ReadInt(Stream stream)
        {
            return ReadInt(stream, true);
        }

        public static int ReadInt(Stream stream, bool littleEndian)
        {
            byte[] b = new byte[4];
            stream.Read(b, 0, 4);

            if (littleEndian)
                return (int)(b[3] << 24) | (b[2] << 16) | (b[1] << 8) | b[0];

            return (int)(b[0] << 24 | (b[1] & 0xff) << 16 | (b[2] & 0xff) << 8 | (b[3] & 0xff));
        }

        public static int ReadShort(Stream stream)
        {
            return ReadShort(stream, true);
        }

        public static int ReadShort(Stream stream, bool littleEndian)
        {
            byte[] b = new byte[2];
            int read = stream.Read(b, 0, 2);

            if (read < 2)
                return -1;

            if (littleEndian)
                return (b[0] + (b[1] << 8));

            return ((b[0] << 8) + b[1]);
        }

        public static int UShiftRight(int left, int right)
        {
            if (right < 1)
                return left;

            return unchecked((int)((uint)left >> right));
        }
    }
}
