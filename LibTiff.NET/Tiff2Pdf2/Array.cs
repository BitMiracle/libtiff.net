using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class PDFArray : PDFObject
    {
        private bool m_willHoldFontWidths;
        protected List<PDFObject> m_items = new List<PDFObject>();

        public PDFArray() : this(false)
        {
        }

        public PDFArray(bool willHoldFontWidths)
        {
            m_willHoldFontWidths = willHoldFontWidths;
        }

        public PDFArray(PDFRect box)
        {
            m_willHoldFontWidths = false;

            AddReal(box.left);
            AddReal(box.bottom);
            AddReal(box.right);
            AddReal(box.top);
        }

        public PDFArray(PDFArray arr) : base(arr)
        {
            set(arr);
        }

        public PDFArray Assign(PDFArray arr)
        {
            set(arr);
            return this;
        }

        public override PDFObject Clone()
        {
            return new PDFArray(this);
        }
        
        //public virtual ~PDFArray();

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFArray;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ ");

            foreach (PDFObject item in m_items)
            {
                sb.Append(item.ToString());
                sb.Append(" ");
            }

            sb.Append("]");
            return sb.ToString();
        }

        public PDFArray Add(PDFObject obj)
        {
            if (this == obj)
		        return this;

            if (obj == null)
                throw new PdfException(PdfExceptionType.InvalidObject);

            if (m_items.Count >= Limit.LIMIT_MAX_PDFARRAY)
                throw new PdfException(PdfExceptionType.ArrayLengthExceeded);

            m_items.Add(obj);
	        PDFObject.IncRef(obj);

            return this;
        }

        public void Insert(PDFObject obj, int position)
        {
            if (this == obj)
		        return;

	        if (obj == null)
		        throw new PdfException(PdfExceptionType.InvalidObject);

	        if (position >= GetItemCount() && position != 0)
		        throw new PdfException(PdfExceptionType.InvalidParameter);

            if (m_items.Count >= Limit.LIMIT_MAX_PDFARRAY)
                throw new PdfException(PdfExceptionType.ArrayLengthExceeded);

            m_items.Insert(position, obj);
	        PDFObject.IncRef(obj);
        }

        public PDFArray AddNumber(int value)
        {
            return Add(new NumberObject(value));
        }

        public PDFArray AddReal(float value)
        {
            return Add(new RealObject(value));
        }

        public PDFArray AddName(string value)
        {
            return Add(new NameObject(value));
        }

        public bool Contains(PDFObject obj)
        {
            foreach (PDFObject item in m_items)
            {
                if (item == obj)
                    return true;
            }

            return false;
        }

        public PDFObject GetItem(int index)
        {
            if (index < 0 || index >= m_items.Count)
                throw new PdfException(PdfExceptionType.InvalidIndex);

            return m_items[index];
        }

        public int GetNumber(int index)
        {
            PDFObject item = GetItem(index);
            if (item.GetPDFType() == PDFObject.Type.PDFNumber)
                return ((NumberObject)item).GetValue();
            else if (item.GetPDFType() == PDFObject.Type.PDFReal)
                return (int)((RealObject)item).GetValue();
                
            throw new PdfException(PdfExceptionType.InvalidObject);
        }

        public float GetFloat(int index)
        {
            PDFObject item = GetItem(index);
            if (item.GetPDFType() == PDFObject.Type.PDFNumber)
                return (float)((NumberObject)item).GetValue();
            else if (item.GetPDFType() == PDFObject.Type.PDFReal)
                return ((RealObject)item).GetValue();

            throw new PdfException(PdfExceptionType.InvalidObject);
        }

        public int GetItemCount()
        {
            return m_items.Count;
        }

        public PDFRect ToRect()
        {
            if (m_items.Count != 4)
                throw new PdfException(PdfExceptionType.WrongArray);

            // Typically, the array takes the form [ llx lly urx ury ]
            // specifying the lower-left x, lower-left y, upper-right x, and 
            // upper-right y coordinates of the rectangle, in that order.
            // i.e [left, bottom, right, top]

            float left = GetFloat(0);
            float top = GetFloat(3);
            float right = GetFloat(2);
            float bottom = GetFloat(1);

            return new PDFRect(left, top, right, bottom);
        }

        public void RemoveAt(int index)
        {
            if (index >= m_items.Count) 
		        return;

            PDFObject obj = m_items[index];
            m_items.RemoveAt(index);
	        PDFObject.DecRef(ref obj);
        }

        public override void Write(PDFStream stream)
        {
            stream.WriteStr("[ ");

            for (int i = 0; i < m_items.Count; i++)
            {
                if (m_willHoldFontWidths && (i % 16 == 0))
                    stream.WriteStr("\n");

                PDFObject obj = m_items[i];
                if (obj.IsIndirect())
                    stream.Write(obj.GetID(), obj.GetGeneration(), "R");
                else
                    obj.Write(stream);

                stream.WriteChar(' ');
            }

            if (m_willHoldFontWidths)
                stream.WriteStr("\n");

            stream.WriteChar(']');
        }

        protected override void clearImpl()
        {
            for (int i = 0; i < m_items.Count; i++)
            {
                PDFObject obj = m_items[i];
		        m_items[i] = null;
		        PDFObject.DecRef(ref obj);
            }

            m_items.Clear();
        }

        private void set(PDFArray arr)
        {
            if (this != arr)
            {
                Clear(false);
                base.Assign(arr);
                m_willHoldFontWidths = arr.m_willHoldFontWidths;
                int count = arr.m_items.Count;
                for (int i = 0; i < count; ++i)
                {
                    PDFObject obj = arr.m_items[i];
                    if (obj.IsIndirect())
                        Add(obj);
                    else
                        Add(obj.Clone());
                }
            }
        }
    }
}
