using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class Packbits
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "PackBits";

        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-02.tif",
                    "tiger-minisblack-tile-02.tif",
                    "tiger-palette-strip-02.tif",
                    "tiger-palette-tile-02.tif",
                    "tiger-rgb-strip-contig-02.tif",
                    "tiger-rgb-strip-planar-02.tif",
                    "tiger-rgb-tile-contig-02.tif",
                    "tiger-rgb-tile-planar-16.tif",
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
            performTest(file, new string[] { "-c", "packbits" }, "_converted_packbits");
        }
    }
}
