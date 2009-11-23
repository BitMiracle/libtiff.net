using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace BitMiracle.Docotic.PDFLib
{
    class PDFDocumentImpl : IObjectRegistrator
    {
	    private	XRefTable m_xref;
        private PageCollection m_rootPages;

        private int m_majorVersion = 1;
        private int m_minorVersion = 1;
        
        public PDFDocumentImpl()
        {
            m_xref = new XRefTable();

            // ensure Catalog and Info created and added to xref
            PDFDictionary c = m_xref.Catalog;
            PDFDictionary i = m_xref.Info;

            m_rootPages = new PageCollection(this);
            c.Add("Pages", m_rootPages.GetDictionary());
        }

        public int MajorVersion
        {
            get { return m_majorVersion; }
            set { m_majorVersion = value; }
        }
        
        public int MinorVersion
        {
            get { return m_minorVersion; }
            set { m_minorVersion = value; }
        }

        public PDFDictionary Catalog
        {
            get
            {
                return m_xref.Catalog;
            }
        }

        public PDFDictionary Info
        {
            get
            {
                return m_xref.Info;
            }
        }

        public PDFPage AddPage()
        {
            return m_rootPages.AddKid();
        }

        public void Save(Stream stream)
        {
            MemoryStream pdfStream = new MemoryStream();
            saveDocumentToStream(pdfStream);
            pdfStream.Write(stream);
        }

        public void SetTrailerID(byte[] pID)
        {
            m_xref.SetTrailerID(pID);
        }

        public virtual void Register(PDFObject obj)
        {
            m_xref.Register(obj);
        }

        public virtual void UnRegister(PDFObject obj)
        {
            m_xref.UnRegister(obj);
        }

        private void writeHeader(PDFStream stream)
        {
            string buffer = string.Format("%PDF-{0}.{1} ", m_majorVersion, m_minorVersion);
            stream.WriteStr(buffer);
            stream.WriteStr("\n%");

            byte[] octals = new byte [4];
            octals[0] = Convert.ToByte("342", 8);
            octals[1] = Convert.ToByte("343", 8);
            octals[2] = Convert.ToByte("317", 8);
            octals[3] = Convert.ToByte("323", 8);
            stream.Write(octals, octals.Length);
            stream.WriteStr("\n");
        }

        private void saveDocumentToStream(PDFStream stream)
        {
            writeHeader(stream);
            m_xref.Write(stream);
        }
    }
}
