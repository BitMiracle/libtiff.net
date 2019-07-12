using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class RotateTiff
    {
        public static void Main()
        {
            int[] rotateAngles = new int[] { 90, 180, 270 };

            for (int angleIndex = 0; angleIndex < rotateAngles.Length; angleIndex++)
            {
                string outputFileName = string.Format("Rotated-{0}-degrees.tif", rotateAngles[angleIndex]);

                using (Tiff input = Tiff.Open(@"Sample Data\flag_t24.tif", "r"))
                {
                    using (Tiff output = Tiff.Open(outputFileName, "w"))
                    {
                        for (short page = 0; page < input.NumberOfDirectories(); page++)
                        {
                            input.SetDirectory(page);
                            output.SetDirectory(page);

                            int width = input.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                            int height = input.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                            int samplesPerPixel = input.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
                            int bitsPerSample = input.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
                            int photo = input.GetField(TiffTag.PHOTOMETRIC)[0].ToInt();

                            int[] raster = new int[width * height];
                            input.ReadRGBAImageOriented(width, height, raster, Orientation.TOPLEFT);

                            raster = rotate(raster, rotateAngles[angleIndex], ref width, ref height);

                            output.SetField(TiffTag.IMAGEWIDTH, width);
                            output.SetField(TiffTag.IMAGELENGTH, height);
                            output.SetField(TiffTag.SAMPLESPERPIXEL, 3);
                            output.SetField(TiffTag.BITSPERSAMPLE, 8);
                            output.SetField(TiffTag.ROWSPERSTRIP, height);
                            output.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                            output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                            output.SetField(TiffTag.COMPRESSION, Compression.DEFLATE);

                            byte[] strip = rasterToRgbBuffer(raster);
                            output.WriteEncodedStrip(0, strip, strip.Length);

                            output.WriteDirectory();
                        }
                    }
                }

                Process.Start(outputFileName);
            }
        }

        private static byte[] rasterToRgbBuffer(int[] raster)
        {
            byte[] buffer = new byte[raster.Length * 3];
            for (int i = 0; i < raster.Length; i++)
                Buffer.BlockCopy(raster, i * 4, buffer, i * 3, 3);

            return buffer;
        }

        private static int[] rotate(int[] buffer, int angle, ref int width, ref int height)
        {
            int rotatedWidth = width;
            int rotatedHeight = height;
            int numberOf90s = angle / 90;
            if (numberOf90s % 2 != 0)
            {
                int tmp = rotatedWidth;
                rotatedWidth = rotatedHeight;
                rotatedHeight = tmp;
            }

            int[] rotated = new int[rotatedWidth * rotatedHeight];

            for (int h = 0; h < height; ++h)
            {
                for (int w = 0; w < width; ++w)
                {
                    int item = buffer[h * width + w];
                    int x = 0;
                    int y = 0;
                    switch (numberOf90s % 4)
                    {
                        case 0:
                            x = w;
                            y = h;
                            break;

                        case 1:
                            x = (height - h - 1);
                            y = (rotatedHeight - 1) - (width - w - 1);
                            break;

                        case 2:
                            x = (width - w - 1);
                            y = (height - h - 1);

                            break;

                        case 3:
                            x = (rotatedWidth - 1) - (height - h - 1);
                            y = (width - w - 1);
                            break;
                    }

                    rotated[y * rotatedWidth + x] = item;
                }
            }

            width = rotatedWidth;
            height = rotatedHeight;
            return rotated;
        }
    }
}
