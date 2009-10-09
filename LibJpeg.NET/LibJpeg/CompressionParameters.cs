using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
    class CompressionParameters
    {
        private int m_quality = 75;
        private int m_smoothingFactor = 0;
        private bool m_simpleProgressive = false;

        public CompressionParameters()
        {
        }

        internal CompressionParameters(CompressionParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            m_quality = parameters.m_quality;
            m_smoothingFactor = parameters.m_smoothingFactor;
            m_simpleProgressive = parameters.m_simpleProgressive;
        }

        public override bool Equals(object obj)
        {
            CompressionParameters parameters = obj as CompressionParameters;
            if (parameters == null)
                return false;

            return (m_quality == parameters.m_quality &&
                    m_smoothingFactor == parameters.m_smoothingFactor &&
                    m_simpleProgressive == parameters.m_simpleProgressive);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int Quality
        {
            get { return m_quality; }
            set { m_quality = value; }
        }

        public int SmoothingFactor
        {
            get { return m_smoothingFactor; }
            set { m_smoothingFactor = value; }
        }

        public bool SimpleProgressive
        {
            get { return m_simpleProgressive; }
            set { m_simpleProgressive = value; }
        }
    }
}