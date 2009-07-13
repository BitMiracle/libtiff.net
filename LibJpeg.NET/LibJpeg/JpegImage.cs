using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
 class JpegImage
    {
        private Bitmap m_bitmap;
        private MemoryStream m_compressedData = new MemoryStream();

        private List<RowOfSamples> m_rows = new List<RowOfSamples>();

        private short m_bitsPerComponent;
        private short m_componentsPerSample;
        private Colorspace m_colorspace;

        public JpegImage(System.Drawing.Bitmap bitmap)
        {
            createFromBitmap(bitmap);
        }

        public JpegImage(Stream imageData)
        {
            if (imageData == null)
                throw new ArgumentNullException("imageData");

            if (isCompressed(imageData))
            {
                m_compressedData = Utils.CopyStream(imageData);
                decompress();
            }
            else
            {
                createFromBitmap(new Bitmap(imageData));
            }
        }

        public int Width
        {
            get
            {
                return m_bitmap.Width;
            }
        }

        public int Height
        {
            get
            {
                return m_bitmap.Height;
            }
        }

        public short BitsPerComponent
        {
            get
            {
                return m_bitsPerComponent;
            }
            internal set
            {
                m_bitsPerComponent = value;
            }
        }

        public short ComponentsPerSample
        {
            get
            {
                return m_componentsPerSample;
            }
            internal set
            {
                m_componentsPerSample = value;
            }
        }

        public Colorspace Colorspace
        {
            get
            {
                return m_colorspace;
            }
            internal set
            {
                m_colorspace = value;
            }
        }

        public RowOfSamples GetRow(int rowNumber)
        {
            return m_rows[rowNumber];
        }

        public void WriteCompressed(Stream output)
        {
            m_compressedData.WriteTo(output);
        }

        public void WriteDecompressed(Stream output)
        {
            m_bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        public System.Drawing.Bitmap ToBitmap()
        {
            return m_bitmap;
        }


        internal void addRowOfSamples(RowOfSamples row)
        {
            if (row == null)
                throw new ArgumentNullException("row");

            m_rows.Add(row);
        }

        private static bool isCompressed(Stream imageData)
        {
            if (imageData == null)
                return false;

            if (imageData.Length <= 2)
                return false;

            imageData.Seek(0, SeekOrigin.Begin);
            int first = imageData.ReadByte();
            int second = imageData.ReadByte();
            return (first == 0xFF && second == (int)JPEG_MARKER.M_SOI);
        }

        private void createFromBitmap(System.Drawing.Bitmap bitmap)
        {
            initializeFromBitmap(bitmap);
            compress();
        }

        private void initializeFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            m_bitmap = bitmap;
            processPixelFormat(m_bitmap.PixelFormat);
            fillSamplesFromBitmap();
        }

        private void compress()
        {
            Debug.Assert(m_bitmap != null);

            DotNetBitmapSource bitmapSource = new DotNetBitmapSource(m_bitmap);
            Jpeg jpeg = new Jpeg();
            jpeg.Compress(bitmapSource, m_compressedData);
        }

        private void decompress()
        {
            Debug.Assert(m_compressedData != null);
            Debug.Assert(m_compressedData.Length != 0);

            m_bitmap = new Bitmap(m_compressedData);

            Jpeg jpeg = new Jpeg();
            jpeg.Decompress(m_compressedData, new DecompressDestination(this));
        }

        private void processPixelFormat(PixelFormat pixelFormat)
        {
            if (pixelFormat == PixelFormat.Format16bppGrayScale)
            {
                m_bitsPerComponent = 16;
                m_componentsPerSample = 1;
                m_colorspace = Colorspace.Grayscale;
                return;
            }

            m_colorspace = Colorspace.RGB;
            m_componentsPerSample = 3;

            switch (pixelFormat)
            {
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format16bppArgb1555:
                    m_bitsPerComponent = 6;
                    break;

                case PixelFormat.Format1bppIndexed:
                case PixelFormat.Format4bppIndexed:
                case PixelFormat.Format8bppIndexed:
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    m_bitsPerComponent = 8;
                    break;

                case PixelFormat.Format48bppRgb:
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    m_bitsPerComponent = 16;
                    break;

                default:
                    throw new ArgumentException("Unsupported pixel format");
            }
        }

        private void fillSamplesFromBitmap()
        {
            Debug.Assert(m_componentsPerSample == 3);

            for (int y = 0; y < Height; ++y)
            {
                short[] samples = new short[Width * 3];
                for (int x = 0; x < Width; ++x)
                {
                    Color color = m_bitmap.GetPixel(x, y);
                    samples[x * 3] = color.R;
                    samples[x * 3 + 1] = color.G;
                    samples[x * 3 + 2] = color.B;
                }
                m_rows.Add(new RowOfSamples(samples, m_bitsPerComponent, m_componentsPerSample));
            }
        }
    }

    class DecompressDestination : IDecompressDestination
    {
        private JpegImage m_jpegImage;

        internal DecompressDestination(JpegImage jpegImage)
        {
            m_jpegImage = jpegImage;
        }

        public Stream Output
        {
            get
            {
                return null;
            }
        }

        public void SetImageParameters(ImageParameters parameters)
        {
            m_jpegImage.BitsPerComponent = 8;
            m_jpegImage.ComponentsPerSample = (short)parameters.ComponentsPerSample;
            m_jpegImage.Colorspace = parameters.Colorspace;
        }

        public void Start()
        {
        }

        public void ProcessPixelsRow(byte[] row)
        {
            RowOfSamples samplesRow = new RowOfSamples(row, m_jpegImage.Width, m_jpegImage.BitsPerComponent, m_jpegImage.ComponentsPerSample);
            m_jpegImage.addRowOfSamples(samplesRow);
        }

        public void Finish()
        {
        }
    }
}
