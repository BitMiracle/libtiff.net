using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
    class SampleRow
    {
        private byte[] m_bytes;
        private Sample[] m_samples;

        public SampleRow(byte[] row, int sampleCount, byte bitsPerComponent, byte componentsPerSample)
        {
            if (row == null)
                throw new ArgumentNullException("row");

            if (row.Length == 0)
                throw new ArgumentException("row is empty");

            if (sampleCount <= 0)
                throw new ArgumentOutOfRangeException("sampleCount");

            if (bitsPerComponent <= 0 || bitsPerComponent > 16)
                throw new ArgumentOutOfRangeException("bitsPerComponent");

            if (componentsPerSample <= 0 || componentsPerSample > 5)
                throw new ArgumentOutOfRangeException("componentsPerSample");

            m_bytes = row;

            BitStream bitStream = new BitStream(row);
            m_samples = new Sample[sampleCount];
            for (int i = 0; i < sampleCount; ++i)
                m_samples[i] = new Sample(bitStream, bitsPerComponent, componentsPerSample);
        }

        public SampleRow(short[] row, byte bitsPerComponent, byte componentsPerSample)
        {
            if (row == null)
                throw new ArgumentNullException("row");

            if (row.Length == 0)
                throw new ArgumentException("row is empty");

            if (bitsPerComponent <= 0 || bitsPerComponent > 16)
                throw new ArgumentOutOfRangeException("bitsPerComponent");

            if (componentsPerSample <= 0 || componentsPerSample > 5)
                throw new ArgumentOutOfRangeException("componentsPerSample");

            int sampleCount = row.Length / componentsPerSample;
            m_samples = new Sample[sampleCount];
            for (int i = 0; i < sampleCount; ++i)
            {
                short[] components = new short[componentsPerSample];
                Array.Copy(row, i * componentsPerSample, components, 0, componentsPerSample);
                m_samples[i] = new Sample(components, bitsPerComponent);
            }

            BitStream bits = new BitStream();
            for (int i = 0; i < sampleCount; ++i)
                bits.Write(row[i], bitsPerComponent);

            m_bytes = new byte[bits.UnderlyingStream.Length];
            bits.UnderlyingStream.Read(m_bytes, 0, (int)bits.UnderlyingStream.Length);
        }

        public int Length
        {
            get
            {
                return m_samples.Length;
            }
        }

        public Sample GetAt(int sampleNumber)
        {
            return m_samples[sampleNumber];
        }

        public Sample this[int sampleNumber]
        {
            get
            {
                return GetAt(sampleNumber);
            }
        }

        public byte[] ToBytes()
        {
            return m_bytes;
        }
    }
}
