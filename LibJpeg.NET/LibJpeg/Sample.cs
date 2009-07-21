using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
    class Sample
    {
        private short[] m_components;
        private byte m_bitsPerComponent;

        internal Sample(BitStream bitStream, byte bitsPerComponent, byte componentCount)
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

        internal Sample(short[] components, byte bitsPerComponent)
        {
            if (components == null)
                throw new ArgumentNullException("components");

            if (components.Length == 0 || components.Length > 5)
                throw new ArgumentException("components must be not empty and contain less than 5 elements");

            if (bitsPerComponent <= 0 || bitsPerComponent > 16)
                throw new ArgumentOutOfRangeException("bitsPerComponent");

            m_bitsPerComponent = bitsPerComponent;

            m_components = new short[components.Length];
            Array.Copy(components, m_components, components.Length);
        }

        public byte BitsPerComponent
        {
            get
            {
                return m_bitsPerComponent;
            }
        }

        public byte ComponentCount
        {
            get
            {
                return (byte)m_components.Length;
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
