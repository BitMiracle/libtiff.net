using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BitMiracle.Docotic.PDFLib
{
    class PDFPage : IDictionaryOwner, IObjectOwner
    {
        private const string contentsKey = "Contents";

        protected PDFDictionary m_dictionary;
        private DictionaryStream m_pageContents;
        
        public PDFDictionary GetDictionary()
        {
            return m_dictionary;
        }

        public DictionaryStream PageContents
        {
            get { return m_pageContents; }
            set { m_pageContents = value; }
        }

        public virtual void OnBeforeWrite()
        {
        }

        public PDFPage(IObjectRegistrator owner) 
        {
            if (owner == null)
		        throw new PdfException(PdfExceptionType.InvalidParameter);

	        m_dictionary = new PDFDictionary();
	        owner.Register(m_dictionary);
	        m_dictionary.SetOwner(this);
	        PDFObject.IncRef(m_dictionary);

            initialize(owner);
            initializeDictionary();
        }

        private void initialize(IObjectRegistrator owner)
        {
            m_pageContents = null;
            if (m_dictionary.GetItem(contentsKey) == null)
                createPageContents(owner);
        }

        private void initializeDictionary()
        {
            /* add required elements */
            GetDictionary().AddName("Type", "Page");
        }

        private void createPageContents(IObjectRegistrator owner)
        {
            m_pageContents = new DictionaryStream();

            if (m_dictionary.IsIndirect())
                owner.Register(m_pageContents);
            else
                throw new PdfException(PdfExceptionType.WrongDictionary);

            m_dictionary.Add(contentsKey, m_pageContents);
        }
    }
}
