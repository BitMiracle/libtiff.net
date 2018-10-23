using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class DetermineCorruptPage
    {
        public static void Main()
        {
            using (Tiff image = Tiff.Open(@"Sample Data\127.tif", "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not load incoming image");
                    return;
                }

                int numberOfDirectories = image.NumberOfDirectories();
                for (int i = 0; i < numberOfDirectories; ++i)
                {
                    image.SetDirectory((short)i);

                    int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                    int imageSize = height * width;
                    int[] raster = new int[imageSize];

                    if (!image.ReadRGBAImage(width, height, raster, true))
                    {
                        MessageBox.Show("Page " + i + " is corrupted");
                        return;
                    }
                }
            }
        }
    }
}