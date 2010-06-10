using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class CCITT
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "CCITT";

        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-01.tif",
                    "tiger-minisblack-tile-01.tif",
                    "tiger-palette-strip-01.tif",
                    "tiger-palette-tile-01.tif",
                    "tiger-rgb-strip-contig-01.tif",
                    "tiger-rgb-strip-planar-01.tif",
                    "tiger-rgb-tile-contig-01.tif",
                };
            }
        }

        private void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test, TestCaseSource("Files")]
        public void Test_G3_1D(string file)
        {
            performTest(file, new string[] { "-c", "g3:1d" }, "_converted_g3_1d");
        }

        [Test, TestCaseSource("Files")]
        public void Test_G3_1D_Fill(string file)
        {
            performTest(file, new string[] { "-c", "g3:1d:fill" }, "_converted_g3_1d_fill");
        }

        [Test, TestCaseSource("Files")]
        public void Test_G3_2D(string file)
        {
            performTest(file, new string[] { "-c", "g3:2d" }, "_converted_g3_2d");
        }

        [Test, TestCaseSource("Files")]
        public void Test_G3_2D_Fill(string file)
        {
            performTest(file, new string[] { "-c", "g3:2d:fill" }, "_converted_g3_2d_fill");
        }

        [Test, TestCaseSource("Files")]
        public void Test_G4(string file)
        {
            performTest(file, new string[] { "-c", "g4" }, "_converted_g4");
        }        
    }
}
