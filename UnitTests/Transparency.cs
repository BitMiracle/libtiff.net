using System.IO;
using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Transparency
    {
        private static readonly string OutputFolder = Path.Combine(TestCase.Folder, "Output.Tiff");
        private static readonly string ExpectedFolder = Path.Combine(TestCase.Folder, "Expected.Tiff");

        [Test]
        public void UnassociatedAlpha()
        {
            string outputPath = Path.Combine(OutputFolder, "UnassociatedAlpha.tif");

            using (Tiff tif = Tiff.Open(outputPath, "w"))
            {
                Assert.IsNotNull(tif);

                const int samplesPerPixel = 4;
                tif.SetField(TiffTag.IMAGEWIDTH, 256);
                tif.SetField(TiffTag.IMAGELENGTH, 256);
                tif.SetField(TiffTag.BITSPERSAMPLE, 8);
                tif.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                tif.SetField(TiffTag.EXTRASAMPLES, 1, new byte[] { (byte)ExtraSample.UNASSALPHA });
                tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tif.SetField(TiffTag.ROWSPERSTRIP, 1);

                // create gradient where right-bottom 64x64 pixels are transparent
                byte[] color_ptr = new byte[256 * samplesPerPixel];
                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < 256; j++)
                    {
                        byte alpha = 255; // opaque
                        if (i > 192 && j > 192)
                            alpha = 0;

                        color_ptr[j * samplesPerPixel + 0] = (byte)i;
                        color_ptr[j * samplesPerPixel + 1] = (byte)i;
                        color_ptr[j * samplesPerPixel + 2] = (byte)i;
                        color_ptr[j * samplesPerPixel + 3] = alpha;
                    }
                    tif.WriteScanline(color_ptr, i);
                }

                tif.FlushData();
                tif.Close();
            }

            string expectedPath = Path.Combine(ExpectedFolder, "UnassociatedAlpha.tif");
            FileAssert.AreEqual(expectedPath, outputPath);
        }

        [Test]
        public void AssociatedAlpha()
        {
            string outputPath = Path.Combine(OutputFolder, "AssociatedAlpha.tif");

            using (Tiff tif = Tiff.Open(outputPath, "w"))
            {
                Assert.IsNotNull(tif);

                const int samplesPerPixel = 4;
                tif.SetField(TiffTag.IMAGEWIDTH, 256);
                tif.SetField(TiffTag.IMAGELENGTH, 256);
                tif.SetField(TiffTag.BITSPERSAMPLE, 8);
                tif.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                tif.SetField(TiffTag.EXTRASAMPLES, 1, new byte[] { (byte)ExtraSample.ASSOCALPHA });
                tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tif.SetField(TiffTag.ROWSPERSTRIP, 1);

                // emit "transparent" gradient for image with fixed (100, 150, 200) pixels
                byte[] color_ptr = new byte[256 * samplesPerPixel];
                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < 256; j++)
                    {
                        byte[] pixelValue = { 100, 150, 200 };
                        byte alpha = (byte)i;
                        double mappedAlpha = alpha / 255.0;
                        color_ptr[j * samplesPerPixel + 0] = (byte)(pixelValue[0] * mappedAlpha);
                        color_ptr[j * samplesPerPixel + 1] = (byte)(pixelValue[1] * mappedAlpha);
                        color_ptr[j * samplesPerPixel + 2] = (byte)(pixelValue[2] * mappedAlpha);
                        color_ptr[j * samplesPerPixel + 3] = alpha;
                    }
                    tif.WriteScanline(color_ptr, i);
                }

                tif.FlushData();
                tif.Close();
            }
        }
    }
}
