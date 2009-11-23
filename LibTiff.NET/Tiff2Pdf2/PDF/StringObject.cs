using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class StringObject : PDFObject
    {
        private string m_value = null;
	    private int m_length;

	    private string m_rawData;
	    private char m_openBracket;

        public StringObject(string value)
        {
            initialize();
            setValue(value, value.Length);
        }

        public StringObject(StringObject str)
        {
            initialize();
            set(str);
        }

        public StringObject(string rawData, char openBracket)
        {
            if (openBracket != '(' && openBracket != '<')
		        throw new PdfException(PdfException.InvalidParameter);

	        initialize();
	        m_rawData = rawData;
	        m_openBracket = openBracket;
        }

        public StringObject(byte[] bytes, int length)
        {
            initialize();

            m_value = Encoding.Default.GetString(bytes);
	        m_length = m_value.Length;
        }

        public static StringObject Create(string value)
        {
            return new StringObject(value);
        }

        public override PDFObject Clone()
        {
            return new StringObject(this);
        }

        public override string ToString()
        {
            return m_value;
        }

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFString;
        }

        public string GetValue()
        {
            return m_value;
        }

	    public byte[] GetBytes()
        {
            return Encoding.Default.GetBytes(m_value);
        }

        public override void Write(PDFStream stream)
        {
	        if (needWriteRawData())
		        writeRawData(stream);
	        else
		        writeValue(stream);
        }

        protected override void clearImpl()
        {
            m_value = null;
	        m_length = 0;

	        m_rawData = "";
	        m_openBracket = '\0';
        }

        private void initialize()
        {
            m_value = null;
	        m_length = 0;

	        m_rawData = "";
	        m_openBracket = '\0';
        }

	    private void set(StringObject obj)
        {
            setValue(obj.m_value, obj.m_length);
	        m_rawData = obj.m_rawData;
	        m_openBracket = obj.m_openBracket;
        }

        private void setValue(string value, int length)
        {
            m_length = length;
            m_value = value.Clone() as string;
        }

	    private bool needWriteRawData()
        {
            return m_openBracket != '\0';
        }

	    private void writeRawData(PDFStream stream)
        {
            char closeBracket;
	        if (m_openBracket == '<')
		        closeBracket = '>';
	        else
		        closeBracket = ')';

	        stream.WriteChar(m_openBracket);
	        stream.WriteStr(m_rawData);
	        stream.WriteChar(closeBracket);
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

        private void writeValue(PDFStream stream)
        {
            if (!IsANSI(GetValue()))
            {
                writeUnicodeString(stream);
                return;
            }

            if (!IsASCII(GetValue()))
            {
                stream.WriteChar('<');
                byte[] bytes = truncateToBytes(m_value);
                stream.WriteBinary(bytes, bytes.Length);
                stream.WriteChar('>');
            }
            else
                stream.WriteEscapeText(m_value);
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
