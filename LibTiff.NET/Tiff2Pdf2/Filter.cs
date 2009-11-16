using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    [Flags]
    enum Filter
    {
        None = 0x0000,
        ASCIIHex = 0x0001,
        ASCII85Decode = 0x0002,
        FlateDecode = 0x0004,
        DCTDecode = 0x0008,
        CCITTFaxDecode = 0x0010,
        LZWDecode = 0x0020
    }
}
