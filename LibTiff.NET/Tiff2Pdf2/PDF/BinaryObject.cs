using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class BinaryObject : PDFObject
    {
        private byte[] m_value;

        public BinaryObject(byte[] value)
        {
            m_value = null;
            SetValue(value);
        }

        public BinaryObject(BinaryObject binObj)
        {
            m_value = null;
            SetValue(binObj.m_value);
        }

        public override PDFObject Clone()
        {
            return new BinaryObject(this);
        }

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFBinary;
        }

        public void SetValue(byte[] value)
        {
            if (value.Length > 65535)
                throw new PdfException(PdfExceptionType.BinaryIsTooLong);

            Clear(false);

            m_value = new byte [value.Length];
            Array.Copy(value, m_value, value.Length);
        }

        public byte[] GetValue()
        {
            return m_value;
        }

        public override void Write(PDFStream stream)
        {
            if (m_value.Length == 0)
            {
                stream.WriteStr("<>");
                return;
            }

            stream.WriteChar('<');
            stream.WriteBinary(m_value, m_value.Length);
            stream.WriteChar('>');
        }

        protected override void clearImpl()
        {
            m_value = null;
        }
    }
}
