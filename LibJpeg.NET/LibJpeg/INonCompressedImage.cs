using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibJpeg
{
    interface INonCompressedImage
    {
        int Width
        { get; }

        int Height
        { get; }

        Colorspace Colorspace
        { get; }

        int ComponentsPerPixel
        { get; }

        void Start();
        byte[] GetPixelRow();
        void Finish();
    }
}
