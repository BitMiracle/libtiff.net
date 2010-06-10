using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class ToStripsNoEncoding
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "None";

        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-05.tif",
                    "tiger-palette-strip-07.tif",
                    "tiger-rgb-strip-contig-03.tif",
                    "tiger-rgb-strip-planar-03.tif",
                    "tiger-rgb-tile-contig-03.tif",
                    "tiger-rgb-tile-planar-24.tif",
                    "penguin_jpeg.tif",
                };
            }
        }

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test, TestCaseSource("Files")]
        public void Test(string file)
        {
            performTest(file, new string[] { "-c", "none", "-s" }, "_converted_strips");
        }

        [Test]
        public void test_penguin_separate_jpeg()
        {
            performTest("penguin_jpeg.tif", 
                new string[] { "-c", "none", "-p", "separate" },
                "_converted_strips_separate");
        }
    }
}
