using System.IO;

using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class Tiff2Pdf
    {
        private static object locked = new object();

        private static string[] SampleFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-01.tif",
                    "tiger-minisblack-strip-02.tif",
                    "tiger-minisblack-strip-04.tif",
                    "tiger-minisblack-strip-08.tif",
                    "tiger-minisblack-tile-01.tif",
                    "tiger-minisblack-tile-02.tif",
                    "tiger-minisblack-tile-04.tif",
                    "tiger-minisblack-tile-08.tif",
                    "tiger-palette-strip-01.tif",
                    "tiger-palette-strip-02.tif",
                    "tiger-palette-strip-03.tif",
                    "tiger-palette-strip-04.tif",
                    "tiger-palette-strip-05.tif",
                    "tiger-palette-strip-06.tif",
                    "tiger-palette-strip-07.tif",
                    "tiger-palette-strip-08.tif",
                    "tiger-palette-tile-01.tif",
                    "tiger-palette-tile-02.tif",
                    "tiger-palette-tile-03.tif",
                    "tiger-palette-tile-04.tif",
                    "tiger-palette-tile-05.tif",
                    "tiger-palette-tile-06.tif",
                    "tiger-palette-tile-07.tif",
                    "tiger-palette-tile-08.tif",
                    "tiger-rgb-strip-contig-01.tif",
                    "tiger-rgb-strip-contig-02.tif",
                    "tiger-rgb-strip-contig-04.tif",
                    "tiger-rgb-strip-contig-08.tif",
                    "tiger-rgb-strip-planar-08.tif",
                    "tiger-rgb-tile-contig-01.tif",
                    "tiger-rgb-tile-contig-02.tif",
                    "tiger-rgb-tile-contig-04.tif",
                    "tiger-rgb-tile-contig-08.tif",
                    "tiger-rgb-tile-planar-08.tif",
                    "tiger-separated-strip-contig-08.tif",
                    "tiger-separated-strip-planar-08.tif",
                };
            }
        }

        private static string[] ChaoticFiles
        {
            get
            {
                return new string[]
                {
                    "bitmap-zip-pc.tif",
                    "cas.tif",
                    "CCITT_1.TIF",
                    "CCITT_2.TIF",
                    "CCITT_3.TIF",
                    "CCITT_4.TIF",
                    "CCITT_5.TIF",
                    "CCITT_6.TIF",
                    "CCITT_7.TIF",
                    "CCITT_8.TIF",
                    "cmyk8-lzw.tif",
                    "color64-lzw-mac.tif",
                    "color64-lzw-pc.tif",
                    "cramps-tile.tif",
                    "cramps.tif",
                    "dscf0013.tif",
                    "fax2d.tif",
                    "FLAG_T24.TIF",
                    "G31D.TIF",
                    "G31DS.TIF",
                    "G32D.TIF",
                    "G32DS.TIF",
                    "g3test.tif",
                    "G4.TIF",
                    "G4S.TIF",
                    "GMARBLES.TIF",
                    "gray8-lzw-mac.tif",
                    "gray8-packbits-be.tif",
                    "gray8-packbits-le.tif",
                    "gray8-zip-pc.tif",
                    "jello.tif",
                    "jim___ah.tif",
                    "jim___cg.tif",
                    "jim___dg.tif",
                    "jim___gg.tif",
                    "lab8-lzw.tif",
                    "MARBIBM.TIF",
                    "MARBLES.TIF",
                    "oxford.tif",
                    "pc260001.tif",
                    "quad-jpeg.tif",
                    "quad-lzw.tif",
                    "quad-tile.tif",
                    "rgb8-jpeg-RGB.tif",
                    "rgb8-jpeg-YCrCb.tif",
                    "rgb8-lsb2msb.tif",
                    "rgb8-lzw-mac.tif",
                    "rgb8-lzw-pc.tif",
                    "rgb8-msb2lsb.tif",
                    "rgb8-separate.tif",
                    "rgb8-zip-mac.tif",
                    "rgb8-zip-pc.tif",
                    "strike.tif",
                    "XING_T24.TIF",
                    "ycbcr-cat.tif",
                    "multipage.tif",
                    "chevron.tif",                    
                };
            }
        }

        private void performTest(string file)
        {
            // console programs' Main are static, so lock concurrent access to 
            // a test code. we use a private field to lock upon 

            lock (locked)
            {
                string inputFile = Path.Combine(TestCase.Folder, file);
                string outputFile = TestCase.Folder + @"Output.Pdf\" + Path.GetFileName(file) + ".pdf";
                
                File.Delete(outputFile);

                BitMiracle.Tiff2Pdf.Program.g_testFriendly = true;
                BitMiracle.Tiff2Pdf.Program.Main(new string[] { "-o", outputFile, inputFile });

                string sampleFile = outputFile.Replace(@"\Output.Pdf\", @"\Expected.Pdf\");

                Assert.IsTrue(File.Exists(outputFile));
                FileAssert.AreEqual(sampleFile, outputFile);
            }
        }

        [Test, TestCaseSource("ChaoticFiles")]
        public void TestChaotic(string file)
        {
            performTest(file);
        }

        [Test, TestCaseSource("SampleFiles")]
        public void TestSample(string file)
        {
            performTest(file);
        }
        
        [Test]
        public void TestArgs()
        {
            string inputFile = Path.Combine(TestCase.Folder, "CCITT_1.TIF");
            string outputFile = "TestArgs.pdf";

            File.Delete(outputFile);

            string[] arguments = new string[] { "-x", "100", "-y", "100", "-o", outputFile, inputFile };
            BitMiracle.Tiff2Pdf.Program.Main(arguments);

            Assert.IsTrue(File.Exists(outputFile));
        }

        [Test]
        public void TestArgs2()
        {
            string inputFile = Path.Combine(TestCase.Folder, "CCITT_1.TIF");
            string outputFile = "TestArgs2.pdf";

            File.Delete(outputFile);

            string[] arguments = new string[] { "-o", outputFile, "-x", "100", "-y", "100", inputFile };
            BitMiracle.Tiff2Pdf.Program.Main(arguments);

            Assert.IsTrue(File.Exists(outputFile));
        }
    }
}
