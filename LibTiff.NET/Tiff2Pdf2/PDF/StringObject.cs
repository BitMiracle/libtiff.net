using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class StringObject : PDFObject
    {
        private string m_value = null;

        public StringObject(string value)
        {
            m_value = value.Clone() as string;
        }

        public override string ToString()
        {
            return m_value;
        }

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFString;
        }

        public override void Write(PDFStream stream)
        {
            if (!IsANSI(m_value))
            {
                writeUnicodeString(stream);
                return;
            }

            if (!IsASCII(m_value))
            {
                stream.WriteChar('<');
                byte[] bytes = truncateToBytes(m_value);
                stream.WriteBinary(bytes, bytes.Length);
                stream.WriteChar('>');
            }
            else
                stream.WriteEscapeText(m_value);
        }

        protected override void clearImpl()
        {
            m_value = null;
        }

        private void writeUnicodeString(PDFStream stream)
        {
            stream.WriteChar('<');

            byte[] UNICODE_HEADER = { 0xFE, 0xFF };
            stream.WriteBinary(UNICODE_HEADER, 2);

            byte[] bytes = Encoding.Unicode.GetBytes(m_value);
            for (int i = 0; i < bytes.Length; i += 2)
            {
                byte temp = bytes[i + 1];
                bytes[i + 1] = bytes[i];
                bytes[i] = temp;
            }
            stream.WriteBinary(bytes, bytes.Length);

            stream.WriteChar('>');
        }

        private static bool IsANSI(string text)
        {
            return allCodesLessEqualThan(255, text);
        }

        private static bool IsASCII(string text)
        {
            return allCodesLessEqualThan(127, text);
        }

        private static byte[] truncateToBytes(string text)
        {
            byte[] bytes = new byte[text.Length];

            for (int i = 0; i < text.Length; ++i)
                bytes[i] = (byte)((int)text[i] & 0x00FF);

            return bytes;
        }

        private static bool allCodesLessEqualThan(int code, string text)
        {
            foreach (char c in text)
            {
                if ((int)c > code)
                    return false;
            }

            return true;
        }       
    }
}
