using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class StripsToTiles
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "2Tiles";

        private static string[] StrippedFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-rgb-strip-contig-03.tif",
                    "tiger-rgb-strip-contig-04.tif",
                    "tiger-separated-strip-contig-16.tif",
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

        [Test, TestCaseSource("StrippedFiles")]
        public void Test(string file)
        {
            performTest(file, new string[] { "-c", "none", "-t" }, "_converted_tiles_none");
        }
    }
}
