using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class PDFDictionary : PDFObject//, IDisposable
    {
        private Dictionary<string, PDFObject> m_items = new Dictionary<string,PDFObject>();
        private bool m_alwaysWriteWithoutEncrypt;

        public PDFDictionary()
        {
            m_alwaysWriteWithoutEncrypt = false;
        }

        public PDFDictionary(PDFDictionary dict) : base(dict)
        {
            set(dict);
        }

        //public PDFDictionary operator=(PDFDictionary dict);

        public override PDFObject Clone()
        {
            return new PDFDictionary(this);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<< ");

            foreach (KeyValuePair<string, PDFObject> kvp in m_items)
            {
                sb.Append("/" + kvp.Key);
                sb.Append(" ");
                sb.Append(kvp.Value.ToString());
                sb.Append(" ");
            }

            sb.Append(">>");
            return sb.ToString();
        }

        //public virtual ~PDFDictionary();

        public override Type GetPDFType()
        {
            return PDFObject.Type.PDFDictionary;
        }

        public void AlwaysWriteWithoutEncrypt()
        {
            m_alwaysWriteWithoutEncrypt = true;
        }

        public void Add(string key, PDFObject obj)
        {
            if (obj == null)
                throw new PdfException(PdfExceptionType.InvalidParameter);

            if (key == null || key.Length == 0)
            {
                obj = null;
                throw new PdfException(PdfExceptionType.InvalidParameter);
            }

            if (m_items.Count >= Limit.LIMIT_MAX_PDFDICT_ELEMENT)
            {
                obj = null;
                throw new PdfException(PdfExceptionType.DictionaryLengthExceeded);
            }

	        if (GetItem(key) != obj)
	        {
		        /* check whether there is an object which has same name */
		        RemoveItem(key);
		        m_items[key] = obj;
		        PDFObject.IncRef(obj);
	        }
        }

        public void Add(PDFDictionary dict)
        {
            if (this == dict)
		        return;

            foreach (KeyValuePair<string, PDFObject> kvp in dict.m_items)
            {
                PDFObject obj = kvp.Value;
                if (obj == null)
                    continue;

                string key = kvp.Key;
                if (obj.IsIndirect())
                    Add(key, obj);
                else
                    Add(key, obj.Clone());
            }
        }

        public void AddBoolean(string key, bool value)
        {
            Add(key, new BooleanObject(value));
        }

        public void AddName(string key, string value)
        {
            Add(key, new NameObject(value));
        }

        public void AddString(string key, string value)
        {
            Add(key, new StringObject(value));
        }

        public void AddNumber(string key, uint value)
        {
            AddNumber(key, (int)value);
        }

        public void AddNumber(string key, int value)
        {
            Add(key, new NumberObject(value));
        }

        public void AddReal(string key, float value)
        {
            Add(key, new RealObject(value));
        }

        public string GetKeyForObj(PDFObject obj)
        {
            foreach (KeyValuePair<string, PDFObject> kvp in m_items)
            {
                if (kvp.Value == obj)
                    return kvp.Key;
            }

            return null;
        }

        public int GetItemCount()
        {
            return m_items.Count;
        }

        public PDFObject GetItem(string key)
        {
            PDFObject obj = null;
            m_items.TryGetValue(key, out obj);
            return obj;
        }

        public PDFObject GetInheritableItem(string key)
        {
            string[] entryNames = { "Resources", "MediaBox", "CropBox", "Rotate", null };

            /* check whether the specified key is valid */
            bool requestedKeyIsValid = false;
            int i = 0;
            while (entryNames[i] != null)
            {
                if (key == entryNames[i])
                {
                    requestedKeyIsValid = true;
                    break;
                }

                i++;
            }

            /* the key is not inheritable */
            if (requestedKeyIsValid != true)
                throw new PdfException(PdfExceptionType.InvalidParameter);

            // if resources of the object is 0, search resources of parent pages
            PDFObject obj = GetItem(key);
            if (obj == null)
            {
                PDFObject parent = GetItem("Parent");
                PDFDictionary parentDict = parent as PDFDictionary;
                if (parentDict != null)
                    obj = parentDict.GetInheritableItem(key);
            }

            return obj;
        }

        public PDFObject GetRequiredItem(string key)
        {
            PDFObject item = GetItem(key);
            if (item == null)
                throw new PdfException(PdfExceptionType.WrongDictionary);

            return item;
        }

        public string GetKeyAt(int pos)
        {
            if (pos >= m_items.Count)
                return "";

            Dictionary<string, PDFObject>.Enumerator enumerator = m_items.GetEnumerator();
            enumerator.MoveNext();

            for (int i = 0; i < pos; i++)
                enumerator.MoveNext();

            return enumerator.Current.Key;
        }

        public void RemoveItem(string key)
        {
            PDFObject obj = null;
            m_items.TryGetValue(key, out obj);

            if (obj != null)
            {
                m_items.Remove(key);
                PDFObject.DecRef(ref obj);
            }
        }

        public override void Write(PDFStream stream)
        {
            writeDictionary(stream);
        }

        protected override void clearImpl()
        {
            foreach (KeyValuePair<string, PDFObject> kvp in m_items)
            {
                PDFObject obj = kvp.Value;
                PDFObject.DecRef(ref obj);
            }

            m_items.Clear();
        }

        protected void writeDictionary(PDFStream stream)
        {
            stream.WriteStr("<<\n");

            foreach (KeyValuePair<string, PDFObject> kvp in m_items)
                writeEntry(stream, kvp.Key, kvp.Value);

            stream.WriteStr(">>");
        }

        protected void writeEntry(PDFStream stream, string key, PDFObject value)
        {
            if (value != null)
            {
                stream.WriteEscapeName(key);
                stream.WriteChar(' ');

                if (value.IsIndirect())
                    stream.Write((int)value.GetID(), value.GetGeneration(), "R");
                else
                    value.Write(stream);

                stream.WriteStr("\n");
            }
        }

        private void set(PDFDictionary dict)
        {
            Clear(false);
	        base.Assign(dict);
	        m_alwaysWriteWithoutEncrypt = dict.m_alwaysWriteWithoutEncrypt;
	        Add(dict);
        }
    }
}
