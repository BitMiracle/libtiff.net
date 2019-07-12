using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class WriteTagsBeforeData
    {
        public static void Main()
        {
            string fileName = "random.tif";
            int totalPages = 3;
            int width = 100;
            int height = 150;

            using (Tiff output = Tiff.Open(fileName, "w"))
            {
                for (short page = 0; page <= totalPages - 1; page++)
                {
                    if (page != 0)
                    {
                        // save previous directory data
                        output.WriteDirectory();

                        // create new directory and make it current
                        output.CreateDirectory();
                        output.SetDirectory(page);
                    }

                    // setup image properties
                    output.SetField(TiffTag.IMAGEWIDTH, width);
                    output.SetField(TiffTag.IMAGELENGTH, height);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    output.SetField(TiffTag.BITSPERSAMPLE, 16);
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                    output.SetField(TiffTag.ROWSPERSTRIP, height);
                    output.SetField(TiffTag.XRESOLUTION, 88.0);
                    output.SetField(TiffTag.YRESOLUTION, 88.0);
                    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.CENTIMETER);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                    // cause tags data to be put in an image
                    output.CheckpointDirectory();

                    // create image data
                    Random random = new Random();
                    for (int i = 0; i <= height - 1; i++)
                    {
                        short[] samples = new short[width];
                        for (int j = 0; j <= width - 1; j++)
                        {
                            samples[j] = Convert.ToInt16(random.Next(0, short.MaxValue));
                        }

                        byte[] buf = new byte[samples.Length * 2];
                        Buffer.BlockCopy(samples, 0, buf, 0, buf.Length);
                        output.WriteScanline(buf, i);
                    }
                }
            }

            System.Diagnostics.Process.Start(fileName);
        }
    }
}
