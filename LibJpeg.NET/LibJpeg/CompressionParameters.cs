using System;
using System.Collections.Generic;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
#if EXPOSE_LIBJPEG
    public
#endif
    class CompressionParameters
    {
        private DCTMethod m_dctMethod = (DCTMethod)JpegConstants.JDCT_DEFAULT;
        private int m_traceLevel = 0;
        private Colorspace m_colorspace = Colorspace.Unknown;
        private bool m_optimizeCoding = false;
        private int m_restartInterval = 0;
        private int m_restartInRows = 0;
        private int m_smoothingFactor = 0;
        private bool m_forceBaseline = true;
        private int m_quality = 75;
        private bool m_simpleProgressive = false;


        public DCTMethod DCTMethod
        {
            get { return m_dctMethod; }
            set { m_dctMethod = value; }
        }
        public int TraceLevel
        {
            get { return m_traceLevel; }
            set { m_traceLevel = value; }
        }

        public Colorspace Colorspace
        {
            get { return m_colorspace; }
            set { m_colorspace = value; }
        }

        public bool OptimizeCoding
        {
            get { return m_optimizeCoding; }
            set { m_optimizeCoding = value; }
        }

        public int RestartInterval
        {
            get { return m_restartInterval; }
            set { m_restartInterval = value; }
        }

        public int RestartInRows
        {
            get { return m_restartInRows; }
            set { m_restartInRows = value; }
        }

        public int SmoothingFactor
        {
            get { return m_smoothingFactor; }
            set { m_smoothingFactor = value; }
        }

        public bool ForceBaseline
        {
            get { return m_forceBaseline; }
            set { m_forceBaseline = value; }
        }

        public int Quality
        {
            get { return m_quality; }
            set { m_quality = value; }
        }

        public bool SimpleProgressive
        {
            get { return m_simpleProgressive; }
            set { m_simpleProgressive = value; }
        }
    }
}