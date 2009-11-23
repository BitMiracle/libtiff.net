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
		        throw new PdfException(PdfException.InvalidParameter);

	        m_dictionary = new PDFDictionary();
	        owner.Register(m_dictionary);
	        m_dictionary.SetOwner(this);
	        PDFObject.IncRef(m_dictionary);

            m_pageContents = null;
            if (m_dictionary.GetItem(contentsKey) == null)
            {
                m_pageContents = new DictionaryStream();

                if (m_dictionary.IsIndirect())
                    owner.Register(m_pageContents);
                else
                    throw new PdfException(PdfException.InvalidObject);

                m_dictionary.Add(contentsKey, m_pageContents);
            }

            GetDictionary().AddName("Type", "Page");
        }
    }
}
