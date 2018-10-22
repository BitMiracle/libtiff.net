#if !NETSTANDARD
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class EncodeBitmap
    {
        private static string[] CCITT_Files
        {
            get
            {
                return new string[]
                {
                    "bitonal.tif",
                    "CCITT_1.tif"
                };
            }
        }

        private static string[] RGB_Files
        {
            get
            {
                return new string[]
                {
                    "FLAG_T24.TIF",
                };
            }
        }

        private static byte[] getImageRasterBytes(Bitmap bmp, PixelFormat format)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            byte[] bits = null;

            try
            {
                // Lock the managed memory
                BitmapData bmpdata = bmp.LockBits(rect, ImageLockMode.ReadWrite, format);

                // Declare an array to hold the bytes of the bitmap.
                bits = new byte[bmpdata.Stride * bmpdata.Height];

                // Copy the values into the array.
                Marshal.Copy(bmpdata.Scan0, bits, 0, bits.Length);

                // Release managed memory
                bmp.UnlockBits(bmpdata);
            }
            catch
            {
                return null;
            }

            return bits;
        }

        private static byte[] getImageRasterBytes(Bitmap bmp)
        {
            return getImageRasterBytes(bmp, bmp.PixelFormat);
        }

        private string OutputFolder
        {
            get
            {
                return Path.Combine(TestCase.Folder, "Output.Tiff");
            }
        }

        private string ExpectedFolder
        {
            get
            {
                return Path.Combine(TestCase.Folder, "Expected.Tiff");
            }
        }

        [Test, TestCaseSource("CCITT_Files")]
        public void EncodeCCITTByScanlines(string file)
        {
            string fullPath = Path.Combine(TestCase.Folder, file);
            string outputPath = Path.Combine(OutputFolder, file);
            string expectedPath = Path.Combine(ExpectedFolder, file);
            
            string suffix = "_ccitt_scanlines.tif";
            string fullOutputPath = outputPath + suffix;
            string fullExpectedPath = expectedPath + suffix;

            using (Bitmap bmp = new Bitmap(fullPath))
            {
                byte[] raster = getImageRasterBytes(bmp);
                using (Tiff tif = Tiff.Open(fullOutputPath, "w"))
                {
                    Assert.IsNotNull(tif);

                    tif.SetField(TiffTag.IMAGEWIDTH, bmp.Width);
                    tif.SetField(TiffTag.IMAGELENGTH, bmp.Height);
                    tif.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4);
                    tif.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);

                    tif.SetField(TiffTag.ROWSPERSTRIP, bmp.Height);

                    tif.SetField(TiffTag.XRESOLUTION, bmp.HorizontalResolution);
                    tif.SetField(TiffTag.YRESOLUTION, bmp.VerticalResolution);

                    tif.SetField(TiffTag.SUBFILETYPE, 0);
                    tif.SetField(TiffTag.BITSPERSAMPLE, 1);
                    tif.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                    tif.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);

                    tif.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    tif.SetField(TiffTag.T6OPTIONS, 0);
                    tif.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                    tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                    int tiffStride = tif.ScanlineSize();
                    int stride = raster.Length / bmp.Height;

                    // raster stride MAY be bigger than TIFF stride (due to padding in raster bits)
                    Assert.False(tiffStride > stride);

                    for (int i = 0, offset = 0; i < bmp.Height; i++)
                    {
                        bool res = tif.WriteScanline(raster, offset, i, 0);
                        Assert.IsTrue(res);

                        offset += stride;
                    }
                }
            }

            FileAssert.AreEqual(fullExpectedPath, fullOutputPath);
        }

        [Test, TestCaseSource("CCITT_Files")]
        public void EncodeCCITT(string file)
        {
            string fullPath = Path.Combine(TestCase.Folder, file);
            string outputPath = Path.Combine(OutputFolder, file);
            string expectedPath = Path.Combine(ExpectedFolder, file);

            string suffix = "_ccitt.tif";
            string fullOutputPath = outputPath + suffix;
            string fullExpectedPath = expectedPath + suffix;

            using (Bitmap bmp = new Bitmap(fullPath))
            {
                byte[] raster = getImageRasterBytes(bmp);
                using (Tiff tif = Tiff.Open(fullOutputPath, "w"))
                {
                    Assert.IsNotNull(tif);

                    tif.SetField(TiffTag.IMAGEWIDTH, bmp.Width);
                    tif.SetField(TiffTag.IMAGELENGTH, bmp.Height);
                    tif.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4);
                    tif.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);

                    tif.SetField(TiffTag.ROWSPERSTRIP, bmp.Height);

                    tif.SetField(TiffTag.XRESOLUTION, bmp.HorizontalResolution);
                    tif.SetField(TiffTag.YRESOLUTION, bmp.VerticalResolution);

                    tif.SetField(TiffTag.SUBFILETYPE, 0);
                    tif.SetField(TiffTag.BITSPERSAMPLE, 1);
                    tif.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                    tif.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);

                    tif.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    tif.SetField(TiffTag.T6OPTIONS, 0);
                    tif.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

                    tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                    int tiffStride = tif.ScanlineSize();
                    int rasterStride = raster.Length / bmp.Height;

                    Assert.False(tiffStride > rasterStride);

                    if (tiffStride < rasterStride)
                    {
                        // raster stride is bigger than TIFF stride
                        // this is due to padding in raster bits
                        // we need to create correct TIFF strip and write it into TIFF

                        byte[] stripBits = new byte[tiffStride * bmp.Height];
                        for (int i = 0, rasterPos = 0, stripPos = 0; i < bmp.Height; i++)
                        {
                            System.Buffer.BlockCopy(raster, rasterPos, stripBits, stripPos, tiffStride);
                            rasterPos += rasterStride;
                            stripPos += tiffStride;
                        }

                        // Write the information to the file
                        int n = tif.WriteEncodedStrip(0, stripBits, stripBits.Length);
                        Assert.AreNotEqual(0, n);
                    }
                    else
                    {
                        // Write the information to the file
                        int n = tif.WriteEncodedStrip(0, raster, raster.Length);
                        Assert.AreNotEqual(0, n);
                    }
                }
            }

            FileAssert.AreEqual(fullExpectedPath, fullOutputPath);
        }

        [Test, TestCaseSource("RGB_Files")]
        public void EncodeRGBByScanlines(string file)
        {
            string fullPath = Path.Combine(TestCase.Folder, file);
            string outputPath = Path.Combine(OutputFolder, file);
            string expectedPath = Path.Combine(ExpectedFolder, file);

            string suffix = "_rgb_scanlines.tif";
            string fullOutputPath = outputPath + suffix;
            string fullExpectedPath = expectedPath + suffix;

            using (Bitmap bmp = new Bitmap(fullPath))
            {
                using (Tiff tif = Tiff.Open(fullOutputPath, "w"))
                {
                    Assert.IsNotNull(tif);
                    convertToTiff(bmp, tif, PixelFormat.Format24bppRgb);
                }
            }

            FileAssert.AreEqual(fullExpectedPath, fullOutputPath);
        }

        [Test, TestCaseSource("RGB_Files")]
        public void EncodeRGBAByScanlines(string file)
        {
            string fullPath = Path.Combine(TestCase.Folder, file);
            string outputPath = Path.Combine(OutputFolder, file);
            string expectedPath = Path.Combine(ExpectedFolder, file);

            string suffix = "_rgba_scanlines.tif";
            string fullOutputPath = outputPath + suffix;
            string fullExpectedPath = expectedPath + suffix;

            using (Bitmap bmp = new Bitmap(fullPath))
            {
                using (Tiff tif = Tiff.Open(fullOutputPath, "w"))
                {
                    Assert.IsNotNull(tif);
                    convertToTiff(bmp, tif, PixelFormat.Format32bppArgb);
                }
            }

            FileAssert.AreEqual(fullExpectedPath, fullOutputPath);
        }

        private static void convertToTiff(Bitmap bmp, Tiff tif, PixelFormat outputFormat)
        {
            if (outputFormat != PixelFormat.Format24bppRgb && outputFormat != PixelFormat.Format32bppArgb)
                throw new System.ArgumentOutOfRangeException();

            byte[] raster = getImageRasterBytes(bmp, outputFormat);
            tif.SetField(TiffTag.IMAGEWIDTH, bmp.Width);
            tif.SetField(TiffTag.IMAGELENGTH, bmp.Height);
            tif.SetField(TiffTag.COMPRESSION, Compression.LZW);
            tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);

            tif.SetField(TiffTag.ROWSPERSTRIP, bmp.Height);

            tif.SetField(TiffTag.XRESOLUTION, bmp.HorizontalResolution);
            tif.SetField(TiffTag.YRESOLUTION, bmp.VerticalResolution);

            tif.SetField(TiffTag.BITSPERSAMPLE, 8);
            if (outputFormat == PixelFormat.Format32bppArgb)
                tif.SetField(TiffTag.SAMPLESPERPIXEL, 4);
            else
                tif.SetField(TiffTag.SAMPLESPERPIXEL, 3);

            tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

            int stride = raster.Length / bmp.Height;
            convertRGBSamples(raster, bmp.Width, bmp.Height, outputFormat);

            for (int i = 0, offset = 0; i < bmp.Height; i++)
            {
                bool res = tif.WriteScanline(raster, offset, i, 0);
                Assert.IsTrue(res);

                offset += stride;
            }
        }

        /// <summary>
        /// Converts BGRA or BGR samples into RGBA or RGB samples
        /// </summary>
        private static void convertRGBSamples(byte[] data, int width, int height, PixelFormat format)
        {
            if (format != PixelFormat.Format24bppRgb && format != PixelFormat.Format32bppArgb)
                throw new InvalidDataException();

            int stride = data.Length / height;

            int samplesPerPixel = 4;
            if (format == PixelFormat.Format24bppRgb)
                samplesPerPixel = 3;

            for (int y = 0; y < height; y++)
            {
                int offset = stride * y;
                int strideEnd = offset + width * samplesPerPixel;

                for (int i = offset; i < strideEnd; i += samplesPerPixel)
                {
                    byte temp = data[i + 2];
                    data[i + 2] = data[i];
                    data[i] = temp;
                }
            }
        }
    }
}
#endif