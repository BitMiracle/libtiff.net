using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class RealObject : PDFObject
    {
        private float m_value;

        protected RealObject()
        {
        }

        public RealObject(float value)
        {
            SetValue(value);
        }

        public RealObject(RealObject real)
        {
            SetValue(real.m_value);
        }
        
        //public RealObject& operator=(const RealObject& real);
        
        public override PDFObject Clone()
        {
            return new RealObject(this);
        }

        public override string ToString()
        {
            return m_value.ToString();
        }

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFReal;
        }

        public void SetValue(float value)
        {
            if (value > 32767)
                throw new PdfException(PdfExceptionType.RealValueIsOutOfRange);

            if (value < -32767)
                throw new PdfException(PdfExceptionType.RealValueIsOutOfRange);

            m_value = value;
        }

        public float GetValue()
        {
            return m_value;
        }

        public override void Write(PDFStream stream)
        {
            stream.WriteReal(m_value);
        }

        protected override void clearImpl()
        {
        }
    }
}
