using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class PageCollection : DictionaryOwner
    {
        private IObjectRegistrator m_owner;
        private List<PDFPage> m_Pages = new List<PDFPage>();

        public PageCollection(IObjectRegistrator owner)
            : base(owner)
        {
            m_owner = owner;

            m_dictionary.AddName("Type", "Pages");
            m_dictionary.Add("Kids", new PDFArray());
            m_dictionary.Add("Count", new NumberObject(0));
        }

        public void AddKid(PDFPage kid)
        {
            AddKid(kid, -1);
        }

        public void AddKid(PDFPage kid, int insertPosition)
        {
            if (insertPosition >= GetCount() && insertPosition != (int)-1)
		        throw new PdfException(PdfExceptionType.InvalidIndex);

            if (kid.GetDictionary().GetItem("Parent") != null)
                throw new PdfException(PdfExceptionType.CannotSetParent);

            kid.GetDictionary().Add("Parent", m_dictionary);

            PDFArray kids = getKids();
            if (kids == null)
                throw new PdfException(PdfExceptionType.InvalidItem);

	        if (insertPosition != -1)
	        {
		        kids.Insert(kid.GetDictionary(), insertPosition);
                m_Pages.Insert(insertPosition, kid);
	        }
	        else
	        {
		        kids.Add(kid.GetDictionary());
		        m_Pages.Add(kid);
	        }
        }

        public PDFPage AddKid()
        {
            PDFPage page = new PDFPage(m_owner);
            if (page == null)
                throw new PdfException(PdfExceptionType.MemoryAllocationFailed);

            AddKid(page);
            return page;
        }

        public int GetCount()
        {
            PDFArray kids = getKids();
            if (kids == null)
                return 0;

            return kids.GetItemCount();
        }

        public PDFPage GetPage(int pageIndex)
        {
            if (!correctIndex(pageIndex))
                return null;

            return m_Pages[pageIndex];
        }

        public override void OnBeforeWrite()
        {
            PDFArray kids = getKids();
            if (kids == null)
                throw new PdfException(PdfExceptionType.InvalidItem);

            NumberObject count = getCount();
            if (count != null)
                count.SetValue((int)GetCount());
            else
            {
                count = new NumberObject((int)GetCount());
                if (count == null)
                    throw new PdfException(PdfExceptionType.MemoryAllocationFailed);

                m_dictionary.Add("Count", count);
            }
        }

        protected NumberObject getCount()
        {
            return getCount(m_dictionary);
        }

        protected static NumberObject getCount(PDFDictionary dict)
        {
            PDFObject item = dict.GetItem("Count");
            NumberObject count = item as NumberObject;
            if (count == null)
                throw new PdfException(PdfExceptionType.WrongPages);

            return count;
        }

        protected PDFArray getKids()
        {
            return getKids(m_dictionary);
        }

        protected static PDFArray getKids(PDFDictionary dict)
        {
            PDFObject item = dict.GetItem("Kids");
            PDFArray kids = item as PDFArray;
            if (kids == null)
                throw new PdfException(PdfExceptionType.WrongPages);

            return kids;
        }

        private bool correctIndex(int pageIndex)
        {
            return (pageIndex >= 0 && pageIndex < m_Pages.Count);
        }
    }
}
