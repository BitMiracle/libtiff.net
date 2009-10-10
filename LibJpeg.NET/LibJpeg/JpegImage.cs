using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

using BitMiracle.LibJpeg.Classic;

[assembly: CLSCompliant(true)]

namespace BitMiracle.LibJpeg
{
    /// <summary>
    /// Main class for work with JPEG images.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
 class JpegImage
    {
        private List<SampleRow> m_rows = new List<SampleRow>();         //!< Description of image pixels (samples)

        private int m_width;
        private int m_height;
        private byte m_bitsPerComponent;
        private byte m_componentsPerSample;
        private Colorspace m_colorspace;

        // Fields below (m_compressedData, m_decompressedData, m_bitmap) are not initialized in constructors necessarily.
        // Instead direct access to these field you should use corresponding properties (compressedData, decompressedData, bitmap)
        // Such agreement allows to load required data (e.g. compress image) only by request.

        private MemoryStream m_compressedData;                          //!< Bytes of jpeg image. Refreshed when m_compressionParameters changed.
        private CompressionParameters m_compressionParameters;          //!< Current compression parameters corresponding with compressed data.

        private MemoryStream m_decompressedData;                        //!< Bytes of decompressed image (bitmap)

        private Bitmap m_bitmap;                                        //!< .NET bitmap associated with this image


        /// <summary>
        /// Creates JpegImage from .NET bitmap
        /// </summary>
        public JpegImage(System.Drawing.Bitmap bitmap)
        {
            createFromBitmap(bitmap);
        }

        /// <summary>
        /// Creates JpegImage from stream with an arbitrary image data
        /// </summary>
        public JpegImage(Stream imageData)
        {
            createFromStream(imageData);
        }

        /// <summary>
        /// Creates JpegImage from file with an arbitrary image
        /// </summary>
        public JpegImage(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            using (FileStream input = new FileStream(fileName, FileMode.Open))
                createFromStream(input);
        }

        /// <summary>
        /// Creates JpegImage from pixels
        /// </summary>
        /// <param name="sampleData">Pixels</param>
        /// <param name="colorspace">Colorspace</param>
        public JpegImage(SampleRow[] sampleData, Colorspace colorspace)
        {
            if (sampleData == null)
                throw new ArgumentNullException("sampleData");

            if (sampleData.Length == 0)
                throw new ArgumentException("sampleData must be no empty");

            if (colorspace == Colorspace.Unknown)
                throw new ArgumentException("Unknown colorspace");

            m_rows = new List<SampleRow>(sampleData);

            SampleRow firstRow = m_rows[0];
            m_width = firstRow.Length;
            m_height = m_rows.Count;

            Sample firstSample = firstRow[0];
            m_bitsPerComponent = firstSample.BitsPerComponent;
            m_componentsPerSample = firstSample.ComponentCount;
            m_colorspace = colorspace;
        }

        /// <summary>
        /// Factory method, constructs JpegImage from .NET Bitmap
        /// </summary>
        public static JpegImage FromBitmap(Bitmap bitmap)
        {
            return new JpegImage(bitmap);
        }

        /// <summary>
        /// Gets the width of image in samples.
        /// </summary>
        public int Width
        {
            get
            {
                return m_width;
            }
            internal set
            {
                m_width = value;
            }
        }

        /// <summary>
        /// Gets the height of image in samples.
        /// </summary>
        public int Height
        {
            get
            {
                return m_height;
            }
            internal set
            {
                m_height = value;
            }
        }

        /// <summary>
        /// Gets the number of color components per sample.
        /// </summary>
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

        /// <summary>
        /// Gets the number of bits per color component of sample.
        /// </summary>
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

        /// <summary>
        /// Gets the colorspace of image.
        /// </summary>
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

        /// <summary>
        /// Retrieves the required row of image.
        /// </summary>
        /// <param name="rowNumber">The number of row.</param>
        public SampleRow GetRow(int rowNumber)
        {
            return m_rows[rowNumber];
        }

        /// <summary>
        /// Writes compressed JPEG image to stream.
        /// </summary>
        /// <param name="output">Output stream.</param>
        public void WriteJpeg(Stream output)
        {
            WriteJpeg(output, new CompressionParameters());
        }

        /// <summary>
        /// Compresses image to JPEG with given parameters and writes it to stream.
        /// </summary>
        /// <param name="output">Output stream.</param>
        /// <param name="parameters">The parameters of compression.</param>
        public void WriteJpeg(Stream output, CompressionParameters parameters)
        {
            compress(parameters);
            compressedData.WriteTo(output);
        }

        /// <summary>
        /// Writes decompressed image data as bitmap to stream.
        /// </summary>
        /// <param name="output">Output stream.</param>
        public void WriteBitmap(Stream output)
        {
            decompressedData.WriteTo(output);
        }

        /// <summary>
        /// Retrieves image as .NET Bitmap.
        /// </summary>
        public Bitmap ToBitmap()
        {
            return bitmap;
        }


        private MemoryStream compressedData
        {
            get
            {
                if (m_compressedData == null)
                    compress(new CompressionParameters());

                Debug.Assert(m_compressedData != null);
                Debug.Assert(m_compressedData.Length != 0);

                return m_compressedData;
            }
        }

        private MemoryStream decompressedData
        {
            get
            {
                if (m_decompressedData == null)
                    fillDecompressedData();

                Debug.Assert(m_decompressedData != null);

                return m_decompressedData;
            }
        }

        private Bitmap bitmap
        {
            get
            {
                if (m_bitmap == null)
                {
                    long position = compressedData.Position;
                    m_bitmap = new Bitmap(compressedData);
                    compressedData.Seek(position, SeekOrigin.Begin);
                }

                return m_bitmap;
            }
        }

        /// <summary>
        /// Needs for DecompressorToJpegImage class
        /// </summary>
        internal void addSampleRow(SampleRow row)
        {
            if (row == null)
                throw new ArgumentNullException("row");

            m_rows.Add(row);
        }

        /// <summary>
        /// Checks if imageData contains jpeg image
        /// </summary>
        private static bool isCompressed(Stream imageData)
        {
            if (imageData == null)
                return false;

            if (imageData.Length <= 2)
                return false;

            imageData.Seek(0, SeekOrigin.Begin);
            int first = imageData.ReadByte();
            int second = imageData.ReadByte();
            return (first == 0xFF && second == (int)JPEG_MARKER.SOI);
        }

        private void createFromBitmap(System.Drawing.Bitmap bitmap)
        {
            initializeFromBitmap(bitmap);
            compress(new CompressionParameters());
        }

        private void createFromStream(Stream imageData)
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

        private void initializeFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            m_bitmap = bitmap;
            m_width = m_bitmap.Width;
            m_height = m_bitmap.Height;
            processPixelFormat(bitmap.PixelFormat);
            fillSamplesFromBitmap();
        }

        private void compress(CompressionParameters parameters)
        {
            Debug.Assert(m_rows != null);
            Debug.Assert(m_rows.Count != 0);

            RawImage source = new RawImage(m_rows, m_colorspace);
            compress(source, parameters);
        }

        private void compress(IRawImage source, CompressionParameters parameters)
        {
            Debug.Assert(source != null);

            if (!needCompressWith(parameters))
                return;

            m_compressedData = new MemoryStream();
            m_compressionParameters = new CompressionParameters(parameters);

            Jpeg jpeg = new Jpeg();
            jpeg.CompressionParameters = m_compressionParameters;
            jpeg.Compress(source, m_compressedData);
        }

        private bool needCompressWith(CompressionParameters parameters)
        {
            return m_compressedData == null || 
                   m_compressionParameters == null || 
                   !m_compressionParameters.Equals(parameters);
        }

        private void decompress()
        {
            Jpeg jpeg = new Jpeg();
            jpeg.Decompress(compressedData, new DecompressorToJpegImage(this));
        }

        private void fillDecompressedData()
        {
            Debug.Assert(m_decompressedData == null);

            m_decompressedData = new MemoryStream();
            BitmapDestination dest = new BitmapDestination(m_decompressedData);

            Jpeg jpeg = new Jpeg();
            jpeg.Decompress(compressedData, dest);
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
            Debug.Assert(m_bitmap != null);

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
