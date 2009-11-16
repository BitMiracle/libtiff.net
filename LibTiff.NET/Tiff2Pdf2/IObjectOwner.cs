using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    interface IObjectOwner
    {
        void OnBeforeWrite();
    }
}
