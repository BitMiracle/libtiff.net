using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
 class JpegImage
    {
        private Bitmap m_bitmap;
        private MemoryStream m_compressedData = new MemoryStream();

        private List<SampleRow> m_rows = new List<SampleRow>();

        private byte m_bitsPerComponent;
        private byte m_componentsPerSample;
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

        public JpegImage(SampleRow[] sampleData, Colorspace colorspace)
        {
            if (sampleData == null)
                throw new ArgumentNullException("sampleData");

            if (sampleData.Length == 0)
                throw new ArgumentException("sampleData must be no empty");

            if (colorspace == Colorspace.Unknown)
                throw new ArgumentException("Unknown colorspace");

            m_rows = new List<SampleRow>(sampleData);
            Sample firstSample = m_rows[0][0];
            m_bitsPerComponent = firstSample.BitsPerComponent;
            m_componentsPerSample = firstSample.ComponentCount;
            m_colorspace = colorspace;

            compressFromSamples();
            m_bitmap = new Bitmap(m_compressedData);
        }

        public static JpegImage FromBitmap(Bitmap bitmap)
        {
            return new JpegImage(bitmap);
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

        public byte BitsPerComponent
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

        public byte ComponentsPerSample
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

        public SampleRow GetRow(int rowNumber)
        {
            return m_rows[rowNumber];
        }

        public void WriteJpeg(Stream output)
        {
            m_compressedData.WriteTo(output);
        }

        public void WriteBitmap(Stream output)
        {
            m_bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Bmp);
        }

        public System.Drawing.Bitmap ToBitmap()
        {
            return m_bitmap;
        }


        internal List<SampleRow> samples
        {
            get
            {
                return m_rows;
            }
        }

        internal void addRowOfSamples(SampleRow row)
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
            compressFromBitmap();
        }

        private void initializeFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            m_bitmap = bitmap;
            processPixelFormat(m_bitmap.PixelFormat);
            fillSamplesFromBitmap();
        }

        private void compressFromBitmap()
        {
            Debug.Assert(m_bitmap != null);

            DotNetBitmapSource bitmapSource = new DotNetBitmapSource(m_bitmap);
            Jpeg jpeg = new Jpeg();
            jpeg.Compress(bitmapSource, m_compressedData);
        }

        private void compressFromSamples()
        {
            Debug.Assert(m_rows != null);
            Debug.Assert(m_rows.Count != 0);

            RawImage source = new RawImage(m_rows, m_colorspace);
            Jpeg jpeg = new Jpeg();
            jpeg.Compress(source, m_compressedData);
        }

        private void decompress()
        {
            Debug.Assert(m_compressedData != null);
            Debug.Assert(m_compressedData.Length != 0);

            m_bitmap = new Bitmap(m_compressedData);

            Jpeg jpeg = new Jpeg();
            jpeg.Decompress(m_compressedData, new DecompressorToJpegImage(this));
        }

        private void processPixelFormat(PixelFormat pixelFormat)
        {
            //See GdiPlusPixelFormats.h for details

            if (pixelFormat == PixelFormat.Format16bppGrayScale)
            {
                m_bitsPerComponent = 16;
                m_componentsPerSample = 1;
                m_colorspace = Colorspace.Grayscale;
                return;
            }

            byte formatIndexByte = (byte)((int)pixelFormat & 0x000000FF);
            byte pixelSizeByte = (byte)((int)pixelFormat & 0x0000FF00);

            if (pixelSizeByte == 32 && formatIndexByte == 15) //PixelFormat32bppCMYK (15 | (32 << 8))
            {
                m_bitsPerComponent = 8;
                m_componentsPerSample = 4;
                m_colorspace = Colorspace.CMYK;
                return;
            }

            m_bitsPerComponent = 8;
            m_componentsPerSample = 3;
            m_colorspace = Colorspace.RGB;

            
            if (pixelSizeByte == 16)
                m_bitsPerComponent = 6;
            else if (pixelSizeByte == 24 || pixelSizeByte == 32)
                m_bitsPerComponent = 8;
            else if (pixelSizeByte == 48 || pixelSizeByte == 64)
                m_bitsPerComponent = 16;
        }

        private void fillSamplesFromBitmap()
        {
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
                m_rows.Add(new SampleRow(samples, m_bitsPerComponent, m_componentsPerSample));
            }
        }
    }
}
