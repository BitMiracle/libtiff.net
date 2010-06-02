using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibJpeg
{
    /// <summary>
    /// Represents a row of image - collection of samples.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class SampleRow
    {
        private byte[] m_bytes;
        private Sample[] m_samples;

        /// <summary>
        /// Creates row from raw samples data.
        /// </summary>
        /// <param name="row">Raw description of samples.
        /// You can pass collection with more than sampleCount samples - when sampleCount samples will be parsed all remaining bytes
        /// will be ignored.</param>
        /// <param name="sampleCount">The number of samples in row.</param>
        /// <param name="bitsPerComponent">The number of bits per component.</param>
        /// <param name="componentsPerSample">The number of components per sample.</param>
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

            using (BitStream bitStream = new BitStream(row))
            {
                m_samples = new Sample[sampleCount];
                for (int i = 0; i < sampleCount; ++i)
                    m_samples[i] = new Sample(bitStream, bitsPerComponent, componentsPerSample);
            }
        }

        /// <summary>
        /// Creates row from an array of components.
        /// </summary>
        /// <param name="sampleComponents">Array of color components.</param>
        /// <param name="bitsPerComponent">The number of bits per component.</param>
        /// <param name="componentsPerSample">The number of components per sample.</param>
        /// <remarks>The difference between this constructor and another one -
        /// this constructor accept an array of prepared color components whereas
        /// another constructor accept raw bytes and parse them.
        /// </remarks>
        internal SampleRow(short[] sampleComponents, byte bitsPerComponent, byte componentsPerSample)
        {
            if (sampleComponents == null)
                throw new ArgumentNullException("sampleComponents");

            if (sampleComponents.Length == 0)
                throw new ArgumentException("row is empty");

            if (bitsPerComponent <= 0 || bitsPerComponent > 16)
                throw new ArgumentOutOfRangeException("bitsPerComponent");

            if (componentsPerSample <= 0 || componentsPerSample > 5)
                throw new ArgumentOutOfRangeException("componentsPerSample");

            int sampleCount = sampleComponents.Length / componentsPerSample;
            m_samples = new Sample[sampleCount];
            for (int i = 0; i < sampleCount; ++i)
            {
                short[] components = new short[componentsPerSample];
                Array.Copy(sampleComponents, i * componentsPerSample, components, 0, componentsPerSample);
                m_samples[i] = new Sample(components, bitsPerComponent);
            }

            using (BitStream bits = new BitStream())
            {
                for (int i = 0; i < sampleCount; ++i)
                {
                    for (int j = 0; j < componentsPerSample; ++j)
                        bits.Write(sampleComponents[i * componentsPerSample + j], bitsPerComponent);
                }

                m_bytes = new byte[bits.UnderlyingStream.Length];
                bits.UnderlyingStream.Seek(0, System.IO.SeekOrigin.Begin);
                bits.UnderlyingStream.Read(m_bytes, 0, (int)bits.UnderlyingStream.Length);
            }
        }

        /// <summary>The number of samples in row.</summary>
        public int Length
        {
            get
            {
                return m_samples.Length;
            }
        }

        /// <summary>Indexer, gets the sample at the specified index.</summary>
        /// <param name="sampleNumber">The number of sample.</param>
        public Sample this[int sampleNumber]
        {
            get
            {
                return GetAt(sampleNumber);
            }
        }

        /// <summary>Retrieves the required sample.</summary>
        /// <param name="sampleNumber">The number of sample.</param>
        public Sample GetAt(int sampleNumber)
        {
            return m_samples[sampleNumber];
        }

        /// <summary>Serializes row to raw bytes.</summary>
        public byte[] ToBytes()
        {
            return m_bytes;
        }
    }
}
