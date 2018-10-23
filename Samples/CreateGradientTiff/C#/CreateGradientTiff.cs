using System;
using System.Diagnostics;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class CreateGradientTiff
    {
        public static void Main()
        {
            using (Tiff tif = Tiff.Open("CreateGradientTiff.tif", "w"))
            {
                if (tif == null)
                    return;

                tif.SetField(TiffTag.IMAGEWIDTH, 256);
                tif.SetField(TiffTag.IMAGELENGTH, 256);
                tif.SetField(TiffTag.BITSPERSAMPLE, 8);
                tif.SetField(TiffTag.SAMPLESPERPIXEL, 3);
                tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tif.SetField(TiffTag.ROWSPERSTRIP, 1);

                byte[] color_ptr = new byte[256 * 3];
                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < 256; j++)
                    {
                        color_ptr[j * 3 + 0] = (byte)i;
                        color_ptr[j * 3 + 1] = (byte)i;
                        color_ptr[j * 3 + 2] = (byte)i;
                    }
                    tif.WriteScanline(color_ptr, i);
                }

                tif.FlushData();
                tif.Close();
            }

            Process.Start("CreateGradientTiff.tif");
        }
    }
}
