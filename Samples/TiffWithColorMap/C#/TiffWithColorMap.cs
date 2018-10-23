using System.Diagnostics;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class TiffWithColorMap
    {
        public static void Main()
        {
            const int numberOfColors = 256;
            const int width = 32;
            const int height = 100;
            const int samplesPerPixel = 1;
            const int bitsPerSample = 8;
            const string fileName = "TiffWithColorMap.tif";

            using (Tiff output = Tiff.Open(fileName, "w"))
            {
                output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel);
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample);
                output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                output.SetField(TiffTag.PHOTOMETRIC, Photometric.PALETTE);
                output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0));

                // it is good idea to specify resolution too (but it is not necessary)
                output.SetField(TiffTag.XRESOLUTION, 100.0);
                output.SetField(TiffTag.YRESOLUTION, 100.0);
                output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                // compression is optional
                output.SetField(TiffTag.COMPRESSION, Compression.ADOBE_DEFLATE);

                // fill color tables
                ushort[] redTable = new ushort[1 << bitsPerSample];
                ushort[] greenTable = new ushort[1 << bitsPerSample];
                ushort[] blueTable = new ushort[1 << bitsPerSample];
                for (int i = 0; i < numberOfColors; ++i)
                {
                    redTable[i] = (ushort)(100 * i);
                    greenTable[i] = (ushort)(150 * i);
                    blueTable[i] = (ushort)(200 * i);
                }
                output.SetField(TiffTag.COLORMAP, redTable, greenTable, blueTable);

                // fill samples array
                byte[][] buffer = new byte[height][];
                for (int j = 0; j < height; j++)
                {
                    buffer[j] = new byte[width];
                    for (int i = 0; i < width; i++)
                        buffer[j][i] = (byte)(j * width + i);
                }
            
                for (int j = 0; j < height; ++j)
                    output.WriteScanline(buffer[j], j);
            }

            Process.Start(fileName);
        }
    }
}
