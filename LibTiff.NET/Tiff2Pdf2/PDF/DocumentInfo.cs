using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class DocumentInfo : DictionaryOwner
    {
        private DateTime m_creationDate;

        public DocumentInfo(IObjectRegistrator registrator) 
            : base(registrator)
        {
        }

        public string GetAuthor()
        {
            return getField("Author");
        }

        public void SetAuthor(string author)
        {
            setField("Author", author);
        }

        public string GetCreator()
        {
            return getField("Creator");
        }

        public void SetCreator(string creator)
        {
            setField("Creator", creator);
        }

        public string GetKeywords()
        {
            return getField("Keywords");
        }

        public void SetKeywords(string keywords)
        {
            setField("Keywords", keywords);
        }

        public string GetProducer()
        {
            return getField("Producer");
        }

        public void SetProducer(string producer)
        {
            setField("Producer", producer);
        }

        public string GetSubject()
        {
            return getField("Subject");
        }

        public void SetSubject(string subject)
        {
            setField("Subject", subject);
        }

        public string GetTitle()
        {
            return getField("Title");
        }

        public void SetTitle(string title)
        {
            setField("Title", title);
        }

        public string GetCreationDateAsString()
        {
            return getField("CreationDate");
        }
        
        public DateTime GetCreationDate()
        {
            return m_creationDate;
        }

        public void SetCreationDate(DateTime date)
        {
            m_creationDate = date;
            string creationDate = encodeTime(m_creationDate);
            m_dictionary.Add("CreationDate", new StringObject(creationDate));
        }

        public override void OnBeforeWrite()
        {
        }

        private string getField(string field)
        {
            PDFObject objField = m_dictionary.GetItem(field);
	        StringObject f = objField as StringObject;
	        if (f == null)
		        return "";

	        return f.GetValue();
        }

        private void setField(string field, string value)
        {
            m_dictionary.Add(field.ToString(), new StringObject(value));
        }

        private static string encodeTime(DateTime oleTime)
        {
            TimeSpan ts = TimeZone.CurrentTimeZone.GetUtcOffset(oleTime);

            string sign = "";
            if (ts.Hours > 0)
                sign = "+";
            
            return string.Format("{0:yyyyMMddHHmmss}{1}{2}'{3}'", oleTime, sign, ts.Hours.ToString("00"), ts.Minutes.ToString("00"));
        }
    }
}
