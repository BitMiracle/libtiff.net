using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    interface IObjectRegistrator
    {
	    void Register(PDFObject obj);
        void UnRegister(PDFObject obj);
    }
}
