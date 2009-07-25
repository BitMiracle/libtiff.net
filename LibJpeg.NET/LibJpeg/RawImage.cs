using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BitMiracle.LibJpeg
{
    class RawImage : INonCompressedImage
    {
        private List<SampleRow> m_samples;
        private Colorspace m_colorspace;

        private int m_currentRow = -1;

        internal RawImage(List<SampleRow> samples, Colorspace colorspace)
        {
            Debug.Assert(samples != null);
            Debug.Assert(samples.Count > 0);
            Debug.Assert(colorspace != Colorspace.Unknown);

            m_samples = samples;
            m_colorspace = colorspace;
        }

        public int Width
        {
            get
            {
                SampleRow firstRow = m_samples[0];
                return m_samples[0].Length;
            }
        }

        public int Height
        {
            get
            {
                return m_samples.Count;
            }
        }

        public Colorspace Colorspace
        {
            get
            {
                return m_colorspace;
            }
        }

        public int ComponentsPerPixel
        {
            get
            {
                return m_samples[0][0].ComponentCount;
            }
        }

        public void Start()
        {
            m_currentRow = 0;
        }

        public byte[] GetPixelRow()
        {
            SampleRow row = m_samples[m_currentRow];
            List<byte> result = new List<byte>();
            for (int i = 0; i < row.Length; ++i)
            {
                Sample sample = row[i];
                for (int j = 0; j < sample.ComponentCount; ++j)
                    result.Add((byte)sample[j]);
            }
            ++m_currentRow;
            return result.ToArray();
        }

        public void Finish()
        {
        }
    }
}
