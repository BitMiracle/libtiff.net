using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibJpeg
{
    interface IRawImage
    {
        int Width
        { get; }

        int Height
        { get; }

        Colorspace Colorspace
        { get; }

        int ComponentsPerPixel
        { get; }

        void BeginRead();
        byte[] GetPixelRow();
        void EndRead();
    }
}
