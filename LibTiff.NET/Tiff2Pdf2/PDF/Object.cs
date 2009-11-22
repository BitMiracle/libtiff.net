using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    abstract class PDFObject
    {
        internal int m_id;

        private IObjectOwner m_owner;
	    private bool m_indirect;
	    private bool m_alreadyCleared;
	    private int m_referenceCount;

        public enum Type
        {
            PDFNumber,
            PDFReal,
            PDFName,
            PDFBoolean,
            PDFString,
            PDFRawString,
            PDFBinary,
            PDFNull,
            PDFArray,
            PDFDictionary,
            PDFDictionaryStream,
        };

        public PDFObject()
        {
            initialize();
        }

        public PDFObject(PDFObject obj)
        {
            initialize();
            set(obj);
        }

        public PDFObject Assign(PDFObject obj)
        {
            if (this != obj)
                set(obj);

            return this;
        }

        public virtual PDFObject Clone()
        {
            return null;
        }

        //public virtual ~PDFObject();

        public void SetOwner(IObjectOwner listener)
        {
            m_owner = listener;
        }

        public void Clear()
        {
            Clear(true);
        }

        public void Clear(bool withChecks)
        {
            if (withChecks)
            {
                if (m_alreadyCleared)
                    return;

                m_alreadyCleared = true;
            }

            clearImpl();
        }

        public abstract Type GetPDFType();

        public abstract void Write(PDFStream stream);

        public static void IncRef(PDFObject obj)
        {
            if (obj != null)
                obj.m_referenceCount += 1;
        }

        public static void DecRef(ref PDFObject obj)
        {
            if (obj == null)
                return;

            if (obj.m_referenceCount == 0)
                return;

            obj.m_referenceCount -= 1;

            if (!obj.IsIndirect() && obj.RefCount() == 0)
            {
                obj.Clear();
                obj = null;
            }
        }

        public int RefCount()
        {
            return m_referenceCount;
        }

        public bool IsIndirect()
        {
            return m_indirect;
        }

        public void MakeIndirect()
        {
            m_indirect = true;
        }

        public void MakeDirect()
        {
            m_indirect = false;
        }

        public int GetID()
        {
            return m_id;
        }

        public int GetGeneration()
        {
            // currently generation number is always zero
            return 0;
        }

        //protected friend class XRefTable;

        protected abstract void clearImpl();

        internal virtual void beforeWrite()
        {
            if (m_owner != null)
                m_owner.OnBeforeWrite();
        }

        private void initialize()
        {
            m_owner = null;
            m_id = 0;
            m_indirect = false;
            m_alreadyCleared = false;
            m_referenceCount = 0;
        }

        private void set(PDFObject obj)
        {
            m_owner = obj.m_owner;
        }
    }
}
