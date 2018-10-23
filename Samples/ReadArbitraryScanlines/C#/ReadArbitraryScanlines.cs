using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ReadArbitraryScanlines
    {
        public static void Main()
        {
            int startScanline = 10;
            int stopScanline = 20;

            using (Tiff image = Tiff.Open(@"Sample Data\f-lzw.tif", "r"))
            {
                int stride = image.ScanlineSize();
                byte[] scanline = new byte[stride];

                Compression compression = (Compression)image.GetField(TiffTag.COMPRESSION)[0].ToInt();
                if (compression == Compression.LZW || compression == Compression.PACKBITS)
                {
                    // LZW and PackBits compression schemes do not allow
                    // scanlines to be read in a random fashion.
                    // So, we need to read all scanlines from start of the image.

                    for (int i = 0; i < startScanline; i++)
                    {
                        // of course, the data won't be used.
                        image.ReadScanline(scanline, i);
                    }
                }

                for (int i = startScanline; i < stopScanline; i++)
                {
                    image.ReadScanline(scanline, i);

                    // do what ever you need with the data
                }
            }
        }
    }
}
