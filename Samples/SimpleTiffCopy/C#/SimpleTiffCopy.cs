using System.Diagnostics;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class SimpleTiffCopy
    {
        public static void Main()
        {
            using (Tiff input = Tiff.Open(@"Sample Data\flag_t24.tif", "r"))
            {
                int width = input.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = input.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int samplesPerPixel = input.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
                int bitsPerSample = input.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
                int photo = input.GetField(TiffTag.PHOTOMETRIC)[0].ToInt();

                int scanlineSize = input.ScanlineSize();
                byte[][] buffer = new byte[height][];
                for (int i = 0; i < height; ++i)
                {
                    buffer[i] = new byte[scanlineSize];
                    input.ReadScanline(buffer[i], i);
                }

                using (Tiff output = Tiff.Open("SimpleTiffCopy.tif", "w"))
                {
                    output.SetField(TiffTag.IMAGEWIDTH, width);
                    output.SetField(TiffTag.IMAGELENGTH, height);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                    output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
                    output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0));
                    output.SetField(TiffTag.PHOTOMETRIC, photo);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                    // change orientation of the image
                    output.SetField(TiffTag.ORIENTATION, Orientation.RIGHTBOT);

                    for (int i = 0; i < height; ++i)
                        output.WriteScanline(buffer[i], i);
                }
            }

            Process.Start("SimpleTiffCopy.tif");
        }
    }
}
