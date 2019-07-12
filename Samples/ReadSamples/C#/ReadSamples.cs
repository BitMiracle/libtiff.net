using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ReadSamples
    {
        public static void Main()
        {
            // Open the TIFF image
            using (Tiff image = Tiff.Open(@"Sample Data\marbles.tif", "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                // Find the width and height of the image
                FieldValue[] value = image.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();

                value = image.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                int imageSize = height * width;
                int[] raster = new int[imageSize];

                // Read the image into the memory buffer
                if (!image.ReadRGBAImage(width, height, raster))
                {
                    MessageBox.Show("Could not read image");
                    return;
                }

                using (Bitmap bmp = new Bitmap(200, 200))
                {
                    for (int i = 0; i < bmp.Width; ++i)
                        for (int j = 0; j < bmp.Height; ++j)
                            bmp.SetPixel(i, j, getSample(i + 330, j + 30, raster, width, height));

                    bmp.Save("ReadSamples.bmp");
                }
                
            }

            Process.Start("ReadSamples.bmp");
        }

        private static Color getSample(int x, int y, int[] raster, int width, int height)
        {
            int offset = (height - y - 1) * width + x;
            int red = Tiff.GetR(raster[offset]);
            int green = Tiff.GetG(raster[offset]);
            int blue = Tiff.GetB(raster[offset]);
            return Color.FromArgb(red, green, blue);
        }
    }
}
