using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class PdfException : Exception
    {
        public const string InvalidParameter = "Parameter is invalid";
        public const string InvalidObject = "Object is invalid";
        public const string NotAttached = "Object is not attached to a xref";

        public PdfException(string message) : base(message)
        {
        }
    }
}
