using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    /// <summary>
    /// Limitations of object implementation (PDF1.4)
    /// </summary>
    class Limit
    {
        public const int LIMIT_MAX_PDFINT = 2147483647;
        public const int LIMIT_MIN_PDFINT = -2147483647;

        public const int LIMIT_MAX_PDFREAL = 32767;
        public const int LIMIT_MIN_PDFREAL = -32767;

        public const int LIMIT_MAX_PDFSTRING_LEN = 65535;
        public const int LIMIT_MAX_PDFNAME_LEN = 127;

        public const int LIMIT_MAX_PDFARRAY = 81910;
        public const int LIMIT_MAX_PDFDICT_ELEMENT = 65535;
        public const int LIMIT_MAX_XREF_ELEMENT = 8388607;
        public const int LIMIT_MAX_GSTATE = 128;
    }
}
