using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ReadBasicProperties
    {
        public static void Main()
        {
            using (Tiff image = Tiff.Open(@"Sample Data\pc260001.tif", "r"))
            {
                FieldValue[] value = image.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();

                value = image.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                value = image.GetField(TiffTag.XRESOLUTION);
                float dpiX = value[0].ToFloat();

                value = image.GetField(TiffTag.YRESOLUTION);
                float dpiY = value[0].ToInt();

                MessageBox.Show(string.Format("Width = {0}, Height = {1}, DPI = {2}x{3}",
                    width, height, dpiX, dpiY), "TIFF properties");
            }
        }
    }
}
