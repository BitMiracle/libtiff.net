using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class NumberObject : PDFObject
    {
        private int m_value;

        protected NumberObject()
        {
        }

        public NumberObject(int value)
        {
            SetValue(value);
        }

        public NumberObject(NumberObject number)
        {
            SetValue(number.m_value);
        }

        //public NumberObject& operator=(const NumberObject& number);

        public override PDFObject Clone()
        {
            return new NumberObject(this);
        }

        public override string ToString()
        {
            return m_value.ToString();
        }

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFNumber;
        }

        public int GetValue()
        {
            return m_value;
        }

        public void SetValue(int value)
        {
            m_value = value;
        }

        public override void Write(PDFStream stream)
        {
            stream.WriteInt(m_value);
        }

        protected override void clearImpl()
        {
        }
    }
}
