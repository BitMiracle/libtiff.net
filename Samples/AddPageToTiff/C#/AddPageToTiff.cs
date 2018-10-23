using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class AddPageToTiff
    {
        public static void Main()
        {
            File.Copy(@"Sample Data\16bit.tif", @"Sample Data\ToBeAppended.tif", true);

            using (Tiff image = Tiff.Open(@"Sample Data\ToBeAppended.tif", "a"))
            {
                int newPageNumber = image.NumberOfDirectories() + 1;
                const int width = 100;
                const int height = 100;

                image.SetField(TiffTag.IMAGEWIDTH, width);
                image.SetField(TiffTag.IMAGELENGTH, height);
                image.SetField(TiffTag.BITSPERSAMPLE, 8);
                image.SetField(TiffTag.SAMPLESPERPIXEL, 3);
                image.SetField(TiffTag.ROWSPERSTRIP, height);

                image.SetField(TiffTag.COMPRESSION, Compression.LZW);
                image.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                byte[] buffer = null;
                using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.FillRectangle(Brushes.White, g.VisibleClipBounds);
                        string s = newPageNumber.ToString();
                        Font f = SystemFonts.DefaultFont;

                        SizeF size = g.MeasureString(s, f);
                        PointF loc = new PointF(Math.Max((bmp.Width - size.Width) / 2, 0), Math.Max((bmp.Height - size.Height) / 2, 0));
                        g.DrawString(s, f, Brushes.Black, loc);

                        buffer = getImageRasterBytes(bmp, PixelFormat.Format24bppRgb);
                    }
                }

                int stride = buffer.Length / height;
                convertRGBSamples(buffer, width, height);

                for (int i = 0, offset = 0; i < height; i++)
                {
                    image.WriteScanline(buffer, offset, i, 0);
                    offset += stride;
                }
            }

            Process.Start(@"Sample Data\ToBeAppended.tif");
        }

        private static byte[] getImageRasterBytes(Bitmap bmp, PixelFormat format)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            byte[] bits = null;

            try
            {
                // Lock the managed memory
                BitmapData bmpdata = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);

                // Declare an array to hold the bytes of the bitmap.
                bits = new byte[bmpdata.Stride * bmpdata.Height];

                // Copy the values into the array.
                System.Runtime.InteropServices.Marshal.Copy(bmpdata.Scan0, bits, 0, bits.Length);

                // Release managed memory
                bmp.UnlockBits(bmpdata);
            }
            catch
            {
                return null;
            }

            return bits;
        }

        /// <summary>
        /// Converts BGR samples into RGB samples
        /// </summary>
        private static void convertRGBSamples(byte[] data, int width, int height)
        {
            int stride = data.Length / height;
            const int samplesPerPixel = 3;

            for (int y = 0; y < height; y++)
            {
                int offset = stride * y;
                int strideEnd = offset + width * samplesPerPixel;

                for (int i = offset; i < strideEnd; i += samplesPerPixel)
                {
                    byte temp = data[i + 2];
                    data[i + 2] = data[i];
                    data[i] = temp;
                }
            }
        }
    }
}
