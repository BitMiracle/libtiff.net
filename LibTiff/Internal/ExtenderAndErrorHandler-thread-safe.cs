using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Classic
{
    partial class Tiff
    {
        private static TiffErrorHandler m_errorHandler;

        /// <summary>
        /// Client Tag extension support (from Niles Ritter).
        /// </summary>
        private static TiffExtendProc m_extender;

        private static TiffErrorHandler setErrorHandlerImpl(TiffErrorHandler errorHandler)
        {
            TiffErrorHandler prev = m_errorHandler;
            m_errorHandler = errorHandler;
            return prev;
        }

        private static TiffExtendProc setTagExtenderImpl(TiffExtendProc extender)
        {
            TiffExtendProc prev = m_extender;
            m_extender = extender;
            return prev;
        }
    }
}
