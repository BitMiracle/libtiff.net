using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class RawString : PDFObject
    {
        private string m_value;
        private int m_length;

        public RawString(string value)
        {
            m_value = null;
            m_length = 0;

            SetValue(value);
        }

        public RawString(RawString rawString)
        {
            m_value = null;
            m_length = 0;

            SetValue(rawString.m_value);
        }

        //public RawString& operator=(const RawString& rawString);

        public override PDFObject Clone()
        {
            return new RawString(this);
        }

        //public virtual ~RawString();

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFRawString;
        }

        public void SetValue(string value)
        {
            m_value = null;
            m_length = 0;

            int len = value.Length;
            if (len > Limit.LIMIT_MAX_PDFSTRING_LEN)
                throw new PdfException(PdfExceptionType.StringIsTooLong);

            m_value = value.Clone() as string;
            m_length = len;
        }

        public string GetValue()
        {
            return m_value;
        }

        public int GetLength()
        {
            return m_length;
        }

        public override void Write(PDFStream stream)
        {
            stream.WriteChar('(');
            stream.WriteStr(m_value);
            stream.WriteChar(')');
        }

        protected override void clearImpl()
        {
            m_value = null;
        }
    }
}
