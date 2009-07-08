using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

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
            initializeFromBitmap(bitmap);
            compress();
        }

        public JpegImage(System.Drawing.Bitmap bitmap, CompressionParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            initializeFromBitmap(bitmap);
            compress(parameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageData">Only jpeg compressed data. Theoretically this may be arbitrary image data, but really
        ///                         we can only process here only images which are supported by System.Drawing.Bitmap.
        ///                         So let's use first constructor for this case.
        /// </param>
        public JpegImage(Stream imageData)
        {
            if (imageData == null)
                throw new ArgumentNullException("imageData");

            copyCompressedData(imageData);
            decompress();
        }

        public JpegImage(Stream imageData, DecompressionParameters parameters)
        {
            if (imageData == null)
                throw new ArgumentNullException("imageData");

            if (parameters == null)
                throw new ArgumentNullException("parameters");

            copyCompressedData(imageData);
            decompress(parameters);
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
        }

        public short ComponentsPerSample
        {
            get
            {
                return m_componentsPerSample;
            }
        }

        public Colorspace Colorspace
        {
            get
            {
                return m_colorspace;
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
            compress(null);
        }

        private void compress(CompressionParameters parameters)
        {
            Debug.Assert(m_bitmap != null);

            Jpeg jpeg = new Jpeg();
            if (parameters != null)
                jpeg.CompressionParameters = parameters;
            DotNetBitmapSource bitmapSource = new DotNetBitmapSource(m_bitmap);
            jpeg.Compress(bitmapSource, m_compressedData);
        }

        private void decompress()
        {
            decompress(null);
        }

        private void decompress(DecompressionParameters parameters)
        {
            Debug.Assert(m_compressedData != null);
            Debug.Assert(m_compressedData.Length != 0);

            Jpeg jpeg = new Jpeg();
            if (parameters != null)
                jpeg.DecompressionParameters = parameters;

            MemoryStream decompressed = new MemoryStream();
            jpeg.DecompressToBitmap(m_compressedData, decompressed, BitmapFormat.Windows);

            m_bitmap = new Bitmap(decompressed);
        }

        private void copyCompressedData(Stream imageData)
        {
            long positionBefore = imageData.Position;
            imageData.Seek(0, SeekOrigin.Begin);

            m_compressedData = new MemoryStream((int)imageData.Length);
            byte[] block = new byte[2048];
            for (; ; )
            {
                int bytesRead = imageData.Read(block, 0, 2048);
                m_compressedData.Write(block, 0, bytesRead);
                if (bytesRead < 2048)
                    break;
            }
            imageData.Seek(positionBefore, SeekOrigin.Begin);
        }

        private void loadRowsFromBitmap()
        {
            Debug.Assert(m_bitmap != null);
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
}
