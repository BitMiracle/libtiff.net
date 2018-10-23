using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class UsingSystemIOStream
    {
        public static void Main()
        {
            // read bytes of an image
            byte[] buffer = File.ReadAllBytes(@"Sample Data\pc260001.tif");

            // create a memory stream out of them
            MemoryStream ms = new MemoryStream(buffer);

            // open a Tiff stored in the memory stream
            using (Tiff image = Tiff.ClientOpen("in-memory", "r", ms, new TiffStream()))
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
