using System.Drawing;
using System.Drawing.Imaging;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class BitmapTo24BitColorTiff
    {
        public static void Main()
        {
            using (Bitmap bmp = new Bitmap(@"Sample data\rgb.jpg"))
            {
                using (Tiff tif = Tiff.Open("BitmapTo24BitColorTiff.tif", "w"))
                {
                    byte[] raster = getImageRasterBytes(bmp, PixelFormat.Format24bppRgb);
                    tif.SetField(TiffTag.IMAGEWIDTH, bmp.Width);
                    tif.SetField(TiffTag.IMAGELENGTH, bmp.Height);
                    tif.SetField(TiffTag.COMPRESSION, Compression.LZW);
                    tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);

                    tif.SetField(TiffTag.ROWSPERSTRIP, bmp.Height);

                    tif.SetField(TiffTag.XRESOLUTION, bmp.HorizontalResolution);
                    tif.SetField(TiffTag.YRESOLUTION, bmp.VerticalResolution);

                    tif.SetField(TiffTag.BITSPERSAMPLE, 8);
                    tif.SetField(TiffTag.SAMPLESPERPIXEL, 3);

                    tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                    int stride = raster.Length / bmp.Height;
                    convertSamples(raster, bmp.Width, bmp.Height);

                    for (int i = 0, offset = 0; i < bmp.Height; i++)
                    {
                        tif.WriteScanline(raster, offset, i, 0);
                        offset += stride;
                    }
                }

                System.Diagnostics.Process.Start("BitmapTo24BitColorTiff.tif");
            }
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
        private static void convertSamples(byte[] data, int width, int height)
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
