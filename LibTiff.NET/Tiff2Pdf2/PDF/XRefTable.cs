using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class XRefTable : IObjectRegistrator
    {
        internal class Entry
        {
            public PDFObject m_object;       //!< object corresponding to entry
            public int m_offset;        //!< offset (in bytes) from the beginning of PDF file to the beginning of object pointed to by this cross-reference entry
            public int m_generation;    //!< "generation number" of the entry
            public bool m_inUse;           //!< true if this entry is in use; false if entry marked as obsolete

            public Entry(int offset, int generation, bool inUse)
            {
                m_object = null;
                m_offset = offset;
                m_generation = generation;
                m_inUse = inUse;
            }
        };

        private const int HPDF_MAX_GENERATION_NUM = 65535;
        private const string entryWriteFormat = "{0:0000000000} {1:00000} {2}\r\n";

        private IObjectRegistrator m_owner;

        private Dictionary<int, Entry> m_entries = new Dictionary<int,Entry>();

        private PDFDictionary m_trailer;
        private DocumentCatalog m_catalog;
        private DocumentInfo m_info;

        public XRefTable(IObjectRegistrator owner)
        {
            if (owner == null)
                throw new PdfException(PdfExceptionType.InvalidParameter);

            m_trailer = null;
            m_catalog = null;
            m_info = null;
            m_owner = owner;

            // add first entry which is free entry and whose generation number is 0
            Entry new_entry = new Entry(0, HPDF_MAX_GENERATION_NUM, false);
            m_entries[0] = new_entry;

            m_trailer = new PDFDictionary();
        }

        public virtual void Register(PDFObject obj)
        {
            if (obj == null)
                throw new PdfException(PdfExceptionType.InvalidParameter);

            if (m_entries.Count >= Limit.LIMIT_MAX_XREF_ELEMENT)
                throw new PdfException(PdfExceptionType.XrefLengthExceeded);

            Entry entry = new Entry(0, 0, true);
            entry.m_object = obj;

            int no = addEntry(entry);
            markRegistered(entry.m_object, no);
        }

        public virtual void UnRegister(PDFObject obj)
        {
            if (obj == null)
                throw new PdfException(PdfExceptionType.InvalidParameter);

            Entry entry = getEntryAt(obj.GetID());
            if (entry == null)
            {
                if (obj.IsIndirect())
                    throw new PdfException(PdfExceptionType.InvalidObject);

                throw new PdfException(PdfExceptionType.NotAttached);
            }

            entry.m_object.MakeDirect();
	        PDFObject.DecRef(ref entry.m_object);
            entry.m_object = null;

            entry.m_generation = HPDF_MAX_GENERATION_NUM;
            entry.m_inUse = false;
            entry.m_offset = 0;
        }

        public int GetEntryCount()
        {
            return m_entries.Count;
        }

        /** Adds unique file identifier to a document trailer
         *  @param pID pointer to unique file identifier
         */
        public void SetTrailerID(byte[] pID)
        {
            setTrailerIDs(pID, pID);
        }

        public void SetTrailerIDs(BinaryObject original, BinaryObject updated)
        {
            PDFArray idArray = new PDFArray();
            idArray.Add(original);
            idArray.Add(updated);

            m_trailer.Add("ID", idArray);
        }

        public DocumentCatalog Catalog
        {
            get
            {
                if (m_catalog == null)
                {
                    m_catalog = new DocumentCatalog(m_owner);
                    m_trailer.Add("Root", m_catalog.GetDictionary());
                }

                return m_catalog;
            }
        }

        public DocumentInfo Info
        {
            get
            {
                if (m_info == null)
                {
                    m_info = new DocumentInfo(this);
                    m_trailer.Add("Info", m_info.GetDictionary());
                }

                return m_info;
            }
        }

        public void Write(PDFStream stream)
        {
            beforeWrite();

            deleteUnusedObjects();
            deleteUnusedObjects();//deletes objects which has been 1 time referenced from already deleted objects
            removeUnusedEntries();

            writeObjects(stream);
            int offset = writeReference(stream);
            writeTrailer(stream, offset);
        }

        private void clearEntries()
        {
            foreach (KeyValuePair<int, Entry> kvp in m_entries)
            {
                Entry entry = kvp.Value;
                if (entry == null)
                    throw new PdfException(PdfExceptionType.WrongXref);

                PDFObject obj = (PDFObject)entry.m_object;
                if (obj != null)
                    obj.Clear();
            }
        }

        private void deleteEntries()
        {
            m_entries.Clear();
        }

        private int addEntry(Entry entry)
        {
            int previousIndex = 0;
            foreach (KeyValuePair<int, Entry> kvp in m_entries)
            {
                if (kvp.Key > (previousIndex + 1))
                    break;

                previousIndex = kvp.Key;
            }

            int newIndex = previousIndex + 1;
            m_entries[newIndex] = entry;

            return newIndex;
        }

        internal Entry getEntryAt(int index)
        {
            Entry entry = null;
            m_entries.TryGetValue(index, out entry);
            return entry;
        }

        private void deleteUnusedObjects()
        {
            foreach (KeyValuePair<int, Entry> kvp in m_entries)
            {
                Entry entry = kvp.Value;
                PDFObject obj = entry.m_object;
                if (obj != null && obj.RefCount() == 1)
                    UnRegister(obj);
            }
        }

        private void removeUnusedEntries()
        {
            Dictionary<int, Entry> usedEntries = new Dictionary<int, Entry>();
            Dictionary<int, Entry>.Enumerator enumerator = m_entries.GetEnumerator();

            foreach (KeyValuePair<int, Entry> kvp in m_entries)
            {
                Entry entry = kvp.Value;
                if (entry == null)
                    continue;

                if (entry.m_inUse || kvp.Key == enumerator.Current.Key)
                {
                    int newIndex = usedEntries.Count;
                    
                    if (entry.m_object != null)
                        entry.m_object.m_id = newIndex;

                    usedEntries[newIndex] = entry;
                }
            }

            m_entries.Clear();
            m_entries = usedEntries;
        }

        private void beforeWrite()
        {
            List<Entry> entries = getAllUsedEntries();
            prepareEntriesForWrite(entries);

            while (true)
            {
                List<Entry> entriesAfterPreparation = getAllUsedEntries();
                removeLeftFromRight(entries, entriesAfterPreparation);

                if (entriesAfterPreparation.Count == 0)
                    break;

                prepareEntriesForWrite(entriesAfterPreparation);

                foreach (Entry e in entriesAfterPreparation)
                    entries.Add(e);
            }
        }

        private void prepareEntriesForWrite(List<Entry> entries)
        {
            foreach (Entry entry in entries)
                entry.m_object.beforeWrite();
        }

        private List<Entry> getAllUsedEntries()
        {
            List<Entry> usedEntries = new List<Entry>();

            foreach (KeyValuePair<int, Entry> kvp in m_entries)
            {
                Entry entry = kvp.Value;
                if (entry == null)
                    throw new PdfException(PdfExceptionType.WrongXref);

                if (entry.m_inUse == false)
                    continue;

                usedEntries.Add(entry);
            }

            return usedEntries;
        }

        private void removeLeftFromRight(List<Entry> left, List<Entry> right)
        {
            foreach (Entry entry in left)
            {
                int index = right.IndexOf(entry);
                if (index != -1)
                    right.RemoveAt(index);
            }
        }

        private void writeObjects(PDFStream stream)
        {
            for (int i = 0; i < GetEntryCount(); i++)
            {
                Entry entry = getEntryAt(i);
                if (entry == null)
                    throw new PdfException(PdfExceptionType.WrongXref);

                if (entry.m_inUse == false)
                    continue;

                entry.m_offset = stream.Size();

                stream.Write(i, (int)entry.m_generation, "obj\n");

                PDFObject pObj = (PDFObject)entry.m_object;
                pObj.Write(stream);
                
                stream.WriteStr("\nendobj\n");
            }
        }

        private int writeReference(PDFStream stream)
        {
            int startXrefOffset = stream.Size();

            stream.WriteStr("xref\n");
            stream.Write(0, (int)GetEntryCount(), "\n");
            
            for (int i = 0; i < GetEntryCount(); i++)
            {
                Entry entry = getEntryAt(i);

                string str;
                if (!entry.m_inUse)
                    str = string.Format(entryWriteFormat, 0, HPDF_MAX_GENERATION_NUM, 'f');
                else
                    str = string.Format(entryWriteFormat, entry.m_offset, entry.m_generation, 'n');

                stream.WriteStr(str);
            }

            return startXrefOffset;
        }

        private void writeTrailer(PDFStream stream, int startXrefOffset)
        {
            m_trailer.AddNumber("Size", GetEntryCount());

            stream.WriteStr("trailer\n");
            m_trailer.Write(stream);

            stream.WriteStr("\nstartxref\n");
            stream.WriteUInt(startXrefOffset);

            stream.WriteStr("\n%%EOF\n");
        }

        private void setTrailerIDs(byte[] pOriginalID, byte[] pUpdatedID)
        {
            byte[] bytes = new byte[pOriginalID.Length];
            Array.Copy(pOriginalID, bytes, bytes.Length);
            BinaryObject strOriginal = new BinaryObject(bytes);

            Array.Copy(pUpdatedID, bytes, bytes.Length);
            BinaryObject strUpdated = new BinaryObject(bytes);

            SetTrailerIDs(strOriginal, strUpdated);
        }

        private byte[] getTrailerIDString(int no)
        {
            PDFArray idArray = (PDFArray)m_trailer.GetItem("ID");
            if (idArray == null)
                return null;

            BinaryObject str = (BinaryObject)idArray.GetItem(no);
            if (str != null)
                return str.GetValue();

            return null;
        }
        
        private void markRegistered(PDFObject obj, int no)
        {
            if (obj == null)
		        return;

	        obj.MakeIndirect();
	        obj.m_id = no;
	        PDFObject.IncRef(obj);
        }

        private Entry getEntryBy(PDFObject obj)
        {
            foreach (KeyValuePair<int, Entry> kvp in m_entries)
            {
                Entry entry = kvp.Value;
                if (entry.m_object == obj)
                    return entry;
            }

            return null;
        }
    }
}
