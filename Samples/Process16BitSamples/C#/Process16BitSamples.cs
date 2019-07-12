using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class Process16BitSamples
    {
        public static void Main()
        {
            using (Tiff tiff = Tiff.Open(@"Sample Data\16bit-lzw.tif", "r"))
            {
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                double dpiX = tiff.GetField(TiffTag.XRESOLUTION)[0].ToDouble();
                double dpiY = tiff.GetField(TiffTag.YRESOLUTION)[0].ToDouble();

                byte[] scanline = new byte[tiff.ScanlineSize()];
                ushort[] scanline16Bit = new ushort[tiff.ScanlineSize() / 2];

                using (Tiff output = Tiff.Open("processed.tif", "w"))
                {
                    if (output == null)
                        return;

                    output.SetField(TiffTag.IMAGEWIDTH, width);
                    output.SetField(TiffTag.IMAGELENGTH, height);
                    output.SetField(TiffTag.BITSPERSAMPLE, 16);
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                    output.SetField(TiffTag.ROWSPERSTRIP, 1);
                    output.SetField(TiffTag.COMPRESSION, Compression.LZW);

                    for (int i = 0; i < height; i++)
                    {
                        tiff.ReadScanline(scanline, i);
                        MultiplyScanLineAs16BitSamples(scanline, scanline16Bit, 16);
                        output.WriteScanline(scanline, i);
                    }
                }

                Process.Start("processed.tif");
            }
        }

        private static void MultiplyScanLineAs16BitSamples(byte[] scanline, ushort[] temp, ushort factor)
        {
            if (scanline.Length % 2 != 0)
            {
                // each two bytes define one sample so there should be even number of bytes
                throw new ArgumentException();
            }

            // pack all bytes to ushorts
            Buffer.BlockCopy(scanline, 0, temp, 0, scanline.Length);

            for (int i = 0; i < temp.Length; i++)
                temp[i] *= factor;

            // unpack all ushorts to bytes
            Buffer.BlockCopy(temp, 0, scanline, 0, scanline.Length);
        }
    }
}
