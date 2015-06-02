using System.Collections.Generic;
using System.IO;

using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class RgbaImage
    {
        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "CCITT_1.TIF",
                    "CCITT_2.TIF",
                    "CCITT_3.TIF",
                    "CCITT_4.TIF",
                    "CCITT_5.TIF",
                    "CCITT_6.TIF",
                    "CCITT_7.TIF",
                    "CCITT_8.TIF",
                    "FLAG_T24.TIF",
                    "G31D.TIF",
                    "G31DS.TIF",
                    "G32D.TIF",
                    "G32DS.TIF",
                    "G4.TIF",
                    "G4S.TIF",
                    "GMARBLES.TIF",
                    "MARBIBM.TIF",
                    "MARBLES.TIF",
                    "XING_T24.TIF",
                    "cramps-tile.tif",
                    "cramps.tif",
                    "flower-minisblack-02.tif",
                    "flower-minisblack-04.tif",
                    "flower-minisblack-08.tif",
                    "flower-minisblack-16.tif",
                    "flower-palette-02.tif",
                    "flower-palette-04.tif",
                    "flower-palette-08.tif",
                    "flower-rgb-contig-08.tif",
                    "flower-rgb-contig-16.tif",
                    "flower-rgb-planar-08.tif",
                    "flower-rgb-planar-16.tif",
                    "flower-separated-contig-08.tif",
                    "dscf0013.tif",
                    "fax2d.tif",
                    "g3test.tif",
                    "jello.tif",
                    "jim___ah.tif",
                    "jim___cg.tif",
                    "jim___dg.tif",
                    "jim___gg.tif",
                    "ladoga.tif",
                    "oxford.tif",
                    "pc260001.tif",
                    "quad-jpeg.tif",
                    "quad-lzw.tif",
                    "quad-tile.tif",
                    "strike.tif",
                    "ycbcr-cat.tif",                    
                };
            }
        }

        private static string[] ToPlainRgbaFiles
        {
            get
            {
                return new string[]
                {
                    "lab8-lzw.tif",
                    "gray16-lzw-pc.tif",
                    "tiger-palette-strip-01.tif",
                    "chevron.tif",
                    "chevron-separate.tif",
                    "flower-rgb-contig-16-unassoc-alpha.tif",
                    "flower-rgb-contig-16-assoc-alpha.tif",
                    "flower-rgb-planar-16-unassoc-alpha.tif",
                    "flower-rgb-planar-16-assoc-alpha.tif",
                    "strike-separate.tif",
                    "flower-ycbcr-contig-08.tif",
                    "flower-ycbcr-separate-08.tif",
                    "flower-ycbcr12-contig-08.tif",
                    "flower-ycbcr41-contig-08.tif",
                    "flower-ycbcr42-contig-08.tif",
                    "flower-ycbcr44-contig-08.tif",
                    "palette1bpp.tif",
                    "cmyka-tile.tif",
                    "gray8a-tile.tif"
                };
            }
        }

        private static string[] ToRgbaFiles
        {
            get
            {
                return new string[]
                {
                    "gray16-lzw-pc.tif",
                    "smallliz.tif",
                    "zackthecat.tif",
                };
            }
        }

        private static string[] ToRgbaZipFiles
        {
            get
            {
                return new string[]
                {
                    "gray16-lzw-pc.tif",
                    "127.tif",
                };
            }
        }        

        private static bool tryReadRGBAImage(string file)
        {
            Tiff tiff = Tiff.Open(file, "r");
            int w = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            int[] raster = new int[w * h];
            return tiff.ReadRGBAImage(w, h, raster);
        }

        private static void testTiff2Rgba(string file, string[] args, string suffix)
        {
            string inputFile = Path.Combine(TestCase.Folder, Path.GetFileName(file));
            string outputFile = TestCase.Folder + @"Output.Tiff\" + Path.GetFileName(file) + suffix + ".tif";

            List<string> completeArgs = new List<string>(args.Length + 2);
            for (int i = 0; i < args.Length; ++i)
                completeArgs.Add(args[i]);

            completeArgs.Add(inputFile);
            completeArgs.Add(outputFile);

            File.Delete(outputFile);

            BitMiracle.Tiff2Rgba.Program.g_testFriendly = true;
            BitMiracle.Tiff2Rgba.Program.Main(completeArgs.ToArray());

            string sampleFile = outputFile.Replace(@"\Output.Tiff\", @"\Expected.Tiff\");
            Assert.IsTrue(File.Exists(outputFile));
            FileAssert.AreEqual(sampleFile, outputFile);
        }

        [Test, TestCaseSource("Files")]
        public void TestReadRGBAImage(string file)
        {
            string fullPath = Path.Combine(TestCase.Folder, file);
            bool ok = tryReadRGBAImage(fullPath);
            Assert.True(ok);
        }

        [Test, TestCaseSource("ToPlainRgbaFiles")]
        public void TestTiff2Rgba(string file)
        {
            testTiff2Rgba(file, new string[] {}, "_rgba");
        }

        [Test, TestCaseSource("ToRgbaFiles")]
        public void TestTiff2RgbaBlocks(string file)
        {
            testTiff2Rgba(file, new string[] { "-b"}, "_rgba_b");
        }

        [Test, TestCaseSource("ToRgbaFiles")]
        public void TestTiff2RgbaRows(string file)
        {
            testTiff2Rgba(file, new string[] { "-r", "3" }, "_rgba_r3");
        }

        [Test, TestCaseSource("ToRgbaFiles")]
        public void TestTiff2RgbaNoAlpha(string file)
        {
            testTiff2Rgba(file, new string[] { "-n" }, "_rgba_noalpha");
        }

        [Test, TestCaseSource("ToRgbaFiles")]
        public void TestTiff2RgbaJpeg(string file)
        {
            testTiff2Rgba(file, new string[] { "-c", "jpeg" }, "_rgba_jpeg");
        }

        [Test, TestCaseSource("ToRgbaZipFiles")]
        public void TestTiff2RgbaZip(string file)
        {
            testTiff2Rgba(file, new string[] { "-c", "zip" }, "_rgba_zip");
        }

        [Test, TestCaseSource("ToRgbaFiles")]
        public void TestTiff2RgbaLzw(string file)
        {
            testTiff2Rgba(file, new string[] { "-c", "lzw" }, "_rgba_lzw");
        }

        [Test, TestCaseSource("ToRgbaFiles")]
        public void TestTiff2RgbaNone(string file)
        {
            testTiff2Rgba(file, new string[] { "-c", "none" }, "_rgba_none");
        }
    }
}
