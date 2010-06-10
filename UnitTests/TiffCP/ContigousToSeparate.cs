using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class ContigousToSeparate
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "2Separate";

        private static string[] ContigousFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-08.tif",
                    "tiger-minisblack-tile-08.tif",
                    "tiger-rgb-tile-contig-08.tif",
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

        [Test, TestCaseSource("ContigousFiles")]
        public void Test(string file)
        {
            performTest(file, new string[] { "-p", "separate", "-c", "lzw" }, "_converted_separate_lzw");
        }
    }
}
