using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    struct TiffTagValue
    {
        public TiffFieldInfo info;
        public int count;
        public byte[] value;
    }
}
