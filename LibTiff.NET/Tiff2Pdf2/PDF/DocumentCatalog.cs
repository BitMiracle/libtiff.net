using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class DocumentCatalog : DictionaryOwner
    {
        private PageCollection m_rootPages;

        public DocumentCatalog(IObjectRegistrator owner)
            : base(owner)
        {
            m_dictionary.AddName("Type", "Catalog");
        }

        public void AddPages(IObjectRegistrator owner)
        {
            m_rootPages = new PageCollection(owner);
            m_dictionary.Add("Pages", m_rootPages.GetDictionary());
        }

        public void AddPage(PDFPage page)
        {
            if (page == null)
                throw new PdfException(PdfExceptionType.InvalidParameter);

            m_rootPages.AddKid(page);
        }

        public PDFPage AddPage()
        {
            return m_rootPages.AddKid();
        }

        public int GetPageCount()
        {
            return m_rootPages.GetCount();
        }

        public PDFPage GetPage(int pageIndex)
        {
            return m_rootPages.GetPage(pageIndex);
        }
    }
}
