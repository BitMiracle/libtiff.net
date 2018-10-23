using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class BitonalTiffToBitmap
    {
        public static void Main()
        {
            using (Bitmap bitmap = tiffToBitmap(@"Sample Data\bitonal.tif"))
            {
                if (bitmap == null)
                {
                    MessageBox.Show("Failed to convert image. Maybe input image does not exist or is not 1 bit per pixel.");
                    return;
                }

                bitmap.Save("BitonalTiffToBitmap.bmp");
                Process.Start("BitonalTiffToBitmap.bmp");
            }
        }

        private static Bitmap tiffToBitmap(string fileName)
        {
            using (Tiff tif = Tiff.Open(fileName, "r"))
            {
                if (tif == null)
                    return null;

                FieldValue[] imageHeight = tif.GetField(TiffTag.IMAGELENGTH);
                int height = imageHeight[0].ToInt();

                FieldValue[] imageWidth = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = imageWidth[0].ToInt();

                FieldValue[] bitsPerSample = tif.GetField(TiffTag.BITSPERSAMPLE);
                short bpp = bitsPerSample[0].ToShort();
                if (bpp != 1)
                    return null;

                FieldValue[] samplesPerPixel = tif.GetField(TiffTag.SAMPLESPERPIXEL);
                short spp = samplesPerPixel[0].ToShort();
                if (spp != 1)
                    return null;

                FieldValue[] photoMetric = tif.GetField(TiffTag.PHOTOMETRIC);
                Photometric photo = (Photometric)photoMetric[0].ToInt();
                if (photo != Photometric.MINISBLACK && photo != Photometric.MINISWHITE)
                    return null;

                int stride = tif.ScanlineSize();
                Bitmap result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);

                // update bitmap palette according to Photometric value
                bool minIsWhite = (photo == Photometric.MINISWHITE);
                ColorPalette palette = result.Palette;
                palette.Entries[0] = (minIsWhite ? Color.White : Color.Black);
                palette.Entries[1] = (minIsWhite ? Color.Black : Color.White);
                result.Palette = palette;
                    
                for (int i = 0; i < height; i++)
                {
                    Rectangle imgRect = new Rectangle(0, i, width, 1);
                    BitmapData imgData = result.LockBits(imgRect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

                    byte[] buffer = new byte[stride];
                    tif.ReadScanline(buffer, i);

                    Marshal.Copy(buffer, 0, imgData.Scan0, buffer.Length);
                    result.UnlockBits(imgData);
                }

                return result;
            }
        }
    }
}
