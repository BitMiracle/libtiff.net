using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class DictionaryStream : PDFDictionary
    {
        protected MemoryStream m_stream;
        private bool m_alreadyEncoded;

        public DictionaryStream()
        {
            m_alreadyEncoded = false;
	        Add("Length", new NumberObject(0));

	        m_stream = new MemoryStream();
	        if (m_stream == null)
		        throw new PdfException(PdfExceptionType.MemoryAllocationFailed);
        }

	    public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFDictionaryStream;
        }

	    public PDFStream GetStream()
        {
            return m_stream;
        }

        public bool AlreadyEncoded
        {
            get
            {
                return m_alreadyEncoded;
            }
            set
            {
                m_alreadyEncoded = value;
            }
        }

	    public override void Write(PDFStream stream)
        {
	        PDFStream tmpStream = new MemoryStream();
	        int streamLength = writeStream(tmpStream);

	        NumberObject length = GetItem("Length") as NumberObject;
	        if (length == null)
		        throw new PdfException(PdfExceptionType.WrongDictionary);
	        length.SetValue((int)streamLength);

	        base.Write(stream);
	        writeStream(stream);
        }

        protected override void clearImpl()
        {
            base.clearImpl();
		    m_stream = null;
        }

	    private int writeStream(PDFStream stream)
        {
	        stream.WriteStr("\nstream\r\n");

	        int streamSizeBefore = stream.Size();
	        m_stream.WriteToStream(stream);

	        int result = stream.Size() - streamSizeBefore;
	        stream.WriteStr("\nendstream");

	        return result;
        }
    }
}
