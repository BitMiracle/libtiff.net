using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitMiracle.LibJpeg
{
    class DecompressorToJpegImage : IDecompressDestination
    {
        private JpegImage m_jpegImage;

        internal DecompressorToJpegImage(JpegImage jpegImage)
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
            m_jpegImage.ComponentsPerSample = (byte)parameters.ComponentsPerSample;
            m_jpegImage.Colorspace = parameters.Colorspace;
        }

        public void Start()
        {
        }

        public void ProcessPixelsRow(byte[] row)
        {
            SampleRow samplesRow = new SampleRow(row, m_jpegImage.Width, m_jpegImage.BitsPerComponent, m_jpegImage.ComponentsPerSample);
            m_jpegImage.addRowOfSamples(samplesRow);
        }

        public void Finish()
        {
        }
    }
}
