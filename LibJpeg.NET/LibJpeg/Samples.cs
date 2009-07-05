using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
 class RowOfSamples
    {
        private byte[] m_bytes;
        private Sample[] m_samples;

        internal RowOfSamples(byte[] row, int sampleCount, short bitsPerComponent, short componentsPerSample)
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

        public int SampleCount
        {
            get
            {
                return m_samples.Length;
            }
        }

        public Sample GetSampleAt(int sampleNumber)
        {
            return m_samples[sampleNumber];
        }

        public Sample this[int sampleNumber]
        {
            get
            {
                return GetSampleAt(sampleNumber);
            }
        }

        public byte[] ToBytes()
        {
            return m_bytes;
        }
    }

#if EXPOSE_LIBJPEG
    public
#endif
 class Sample
    {
        private short[] m_components;
        private short m_bitsPerComponent;

        internal Sample(BitStream bitStream, short bitsPerComponent, short componentCount)
        {
            if (bitStream == null)
                throw new ArgumentNullException("bitStream");

            if (bitsPerComponent <= 0 || bitsPerComponent > 16)
                throw new ArgumentOutOfRangeException("bitsPerComponent");

            if (componentCount <= 0 || componentCount > 5)
                throw new ArgumentOutOfRangeException("componentCount");

            m_bitsPerComponent = bitsPerComponent;

            m_components = new short[componentCount];
            for (short i = 0; i < componentCount; ++i)
                m_components[i] = (short)bitStream.Read(bitsPerComponent);
        }

        public short BitsPerComponent
        {
            get
            {
                return m_bitsPerComponent;
            }
        }

        public short ComponentCount
        {
            get
            {
                return (short)m_components.Length;
            }
        }

        public short GetComponent(int componentNumber)
        {
            return m_components[componentNumber];
        }

        public short this[int componentNumber]
        {
            get
            {
                return GetComponent(componentNumber);
            }
        }
    }
}
