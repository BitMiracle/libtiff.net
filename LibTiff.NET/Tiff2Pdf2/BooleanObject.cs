using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class BooleanObject : PDFObject
    {
        private bool m_value;

        public BooleanObject(bool value)
        {
            m_value = value;
        }

        public BooleanObject(BooleanObject boolObj)
        {
            m_value = boolObj.m_value;
        }

        //public BooleanObject& operator=(const BooleanObject& boolObj);

        public override PDFObject Clone()
        {
            return new BooleanObject(this);
        }

        public override PDFObject.Type GetPDFType()
        {
            return PDFObject.Type.PDFBoolean;
        }

        public bool GetValue()
        {
            return m_value;
        }

        public override void Write(PDFStream stream)
        {
            if (m_value)
                stream.WriteStr("true");
            else
                stream.WriteStr("false");
        }

        protected override void clearImpl()
        {
        }
    }
}
