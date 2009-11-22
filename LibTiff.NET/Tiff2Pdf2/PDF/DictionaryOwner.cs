using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class DictionaryOwner : IObjectOwner, IDictionaryOwner//, IDisposable
    {
        protected PDFDictionary m_dictionary;

        protected DictionaryOwner()
        {
            m_dictionary = new PDFDictionary();
	        m_dictionary.SetOwner(this);
	        PDFObject.IncRef(m_dictionary);
        }

        protected DictionaryOwner(IObjectRegistrator registrator)
        {
            if (registrator == null)
                throw new PdfException(PdfExceptionType.InvalidParameter);

	        m_dictionary = new PDFDictionary();
	        registrator.Register(m_dictionary);
	        m_dictionary.SetOwner(this);
	        PDFObject.IncRef(m_dictionary);
        }

        protected DictionaryOwner(PDFDictionary dict)
        {
            m_dictionary = dict;
	        m_dictionary.SetOwner(this);
	        PDFObject.IncRef(m_dictionary);
        }

        public PDFDictionary GetDictionary()
        {
            return m_dictionary;
        }

        public virtual void OnBeforeWrite()
        {
        }
    }
}
