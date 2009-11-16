using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class DictionaryStream : PDFDictionary
    {
        protected MemoryStream m_stream;

        private bool m_alreadyEncoded;
	    private Filter m_filter;

	    private const string keyFilter = "Filter";

        public DictionaryStream()
        {
            m_alreadyEncoded = false;
	        m_filter = Filter.None;

	        Add("Length", new NumberObject(0));

	        m_stream = new MemoryStream();
	        if (m_stream == null)
		        throw new PdfException(PdfExceptionType.MemoryAllocationFailed);
        }

	    //public virtual ~DictionaryStream();

	    public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFDictionaryStream;
        }

	    public PDFStream GetStream()
        {
            return m_stream;
        }

        public void SetStream(byte[] streamStart, int streamLength)
        {
            SetStream(streamStart, 0, streamLength);
        }

        public void SetStream(byte[] streamStart, int offset, int streamLength)
        {
            byte[] cropped = new byte[streamLength];
            Array.Copy(streamStart, offset, cropped, 0, streamLength);

            m_stream = new MemoryStream(cropped, streamLength);
            m_alreadyEncoded = true;
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

	    public Filter GetFilter()
        {
            return m_filter;
        }

        public void SetFilter(Filter filter)
        {
            m_filter = filter;
        }

        public void AddFilter(Filter filter)
        {
            m_filter |= filter;
        }

	    public override void Write(PDFStream stream)
        {
            if (!m_alreadyEncoded)
		        setFilter();

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

        private void setFilter()
        {
            if (m_stream == null)
		        return;

	        RemoveItem(keyFilter);
	        if (m_filter == Filter.None)
		        return;

	        if ((m_filter & Filter.CCITTFaxDecode) != Filter.None)
		        setCCITTFaxDecodeFilter();
	        else
		        setCompositeFilter();
        }

	    private void setCCITTFaxDecodeFilter()
        {
            AddName(keyFilter, "CCITTFaxDecode");
        }

        private void setCompositeFilter()
        {
            PDFArray arr = new PDFArray();
	        Add(keyFilter, arr);

	        if ((m_filter & Filter.FlateDecode) != Filter.None)
		        arr.AddName("FlateDecode");

	        if ((m_filter & Filter.DCTDecode) != Filter.None)
		        arr.AddName("DCTDecode");
        }

	    private int writeStream(PDFStream stream)
        {
	        stream.WriteStr("\nstream\r\n");

	        int streamSizeBefore = stream.Size();
	        Filter filter = m_alreadyEncoded ? Filter.None : m_filter;
	        m_stream.WriteToStream(stream, filter);

	        int result = stream.Size() - streamSizeBefore;
	        stream.WriteStr("\nendstream");

	        return result;
        }

	    private void getFilters(List<Filter> filters)
        {
            PDFObject filterObject = GetItem(keyFilter);
	        if (filterObject == null)
		        return;

	        if (filterObject.GetPDFType() == PDFObject.Type.PDFName)
	        {
		        NameObject name = (NameObject) filterObject;
		        Filter filter = createFilter(name.GetValue());
		        filters.Add(filter);
	        }
	        else if (filterObject.GetPDFType() == PDFObject.Type.PDFArray)
	        {
		        PDFArray array = (PDFArray)filterObject;
		        int filtersCount = array.GetItemCount();
		        for (int i = 0; i < filtersCount; ++i)
		        {
			        PDFObject filterArrayItem = array.GetItem(i);
			        if (filterArrayItem.GetPDFType() != PDFObject.Type.PDFName)
				        throw new PdfException(PdfExceptionType.WrongDictionary);

			        NameObject name = (NameObject) filterArrayItem;
			        Filter filter = createFilter(name.GetValue());
			        filters.Add(filter);
		        }
	        }
	        else
		        throw new PdfException(PdfExceptionType.InvalidObject);
        }

	    private Filter createFilter(string strFilter)
        {
            if (strFilter == "FlateDecode")
		        return Filter.FlateDecode;

	        if (strFilter == "LZWDecode")
		        return Filter.LZWDecode;

            if (strFilter == "ASCII85Decode")
                return Filter.ASCII85Decode;

	        return Filter.None;
        }
    }
}
