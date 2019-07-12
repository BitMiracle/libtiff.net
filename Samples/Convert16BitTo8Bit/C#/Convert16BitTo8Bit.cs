using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class Convert16BitTo8Bit
    {
        public static void Main()
        {
            using (Bitmap tiff8bit = getBitmap8Bit(@"Sample Data\16bit.tif"))
            {
                if (tiff8bit == null)
                {
                    Console.WriteLine("Failed to convert image. Maybe input image does not exist or is not 16 bit.");
                    return;
                }

                tiff8bit.Save("Convert16BitTo8Bit.bmp");
                Process.Start("Convert16BitTo8Bit.bmp");
            }
            
        }

        private static Bitmap getBitmap8Bit(string inputName)
        {
            Bitmap result;

            using (Tiff tif = Tiff.Open(inputName, "r"))
            {
                FieldValue[] res = tif.GetField(TiffTag.IMAGELENGTH);
                int height = res[0].ToInt();

                res = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = res[0].ToInt();

                res = tif.GetField(TiffTag.BITSPERSAMPLE);
                short bpp = res[0].ToShort();
                if (bpp != 16)
                    return null;

                res = tif.GetField(TiffTag.SAMPLESPERPIXEL);
                short spp = res[0].ToShort();
                if (spp != 1)
                    return null;

                res = tif.GetField(TiffTag.PHOTOMETRIC);
                Photometric photo = (Photometric)res[0].ToInt();
                if (photo != Photometric.MINISBLACK && photo != Photometric.MINISWHITE)
                    return null;

                int stride = tif.ScanlineSize();
                byte[] buffer = new byte[stride];

                result = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                byte[] buffer8Bit = null;

                for (int i = 0; i < height; i++)
                {
                    Rectangle imgRect = new Rectangle(0, i, width, 1);
                    BitmapData imgData = result.LockBits(imgRect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                    if (buffer8Bit == null)
                        buffer8Bit = new byte[imgData.Stride];
                    else
                        Array.Clear(buffer8Bit, 0, buffer8Bit.Length);

                    tif.ReadScanline(buffer, i);
                    convertBuffer(buffer, buffer8Bit);

                    Marshal.Copy(buffer8Bit, 0, imgData.Scan0, buffer8Bit.Length);
                    result.UnlockBits(imgData);
                }
            }

            return result;
        }

        private static void convertBuffer(byte[] buffer, byte[] buffer8Bit)
        {
            for (int src = 0, dst = 0; src < buffer.Length; dst++)
            {
                int value16 = buffer[src++];
                value16 = value16 + (buffer[src++] << 8);
                buffer8Bit[dst] = (byte)(value16 / 257.0 + 0.5);
            }
        }
    }
}
