using System.Diagnostics;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class CreateMultipageTiff
    {
        public static void Main()
        {
            const int numberOfPages = 4;

            const int width = 256;
            const int height = 256;
            const int samplesPerPixel = 1;
            const int bitsPerSample = 8;

            const string fileName = "CreateMultipageTiff.tif";

            byte[][] firstPageBuffer = new byte[height][];
            for (int j = 0; j < height; j++)
            {
                firstPageBuffer[j] = new byte[width];
                for (int i = 0; i < width; i++)
                    firstPageBuffer[j][i] = (byte)(j * i);
            }

            using (Tiff output = Tiff.Open(fileName, "w"))
            {
                for (int page = 0; page < numberOfPages; ++page)
                {
                    output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                    output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                    if (page % 2 == 0)
                        output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    else
                        output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISWHITE);

                    output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0));
                    output.SetField(TiffTag.XRESOLUTION, 100.0);
                    output.SetField(TiffTag.YRESOLUTION, 100.0);
                    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                    // specify that it's a page within the multipage file
                    output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
                    // specify the page number
                    output.SetField(TiffTag.PAGENUMBER, page, numberOfPages);

                    for (int j = 0; j < height; ++j)
                        output.WriteScanline(firstPageBuffer[j], j);

                    output.WriteDirectory();
                }
            }

            Process.Start(fileName);
        }
    }
}