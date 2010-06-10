using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class TilesToStrips
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "2Strips";

        private static string[] TiledFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-tile-03.tif",
                    "tiger-minisblack-tile-04.tif",
                    "tiger-minisblack-tile-32.tif",
                    "tiger-minisblack-tile-64.tif",
                    "tiger-palette-tile-03.tif",
                    "tiger-palette-tile-04.tif",
                    "tiger-palette-tile-15.tif",
                    "tiger-palette-tile-16.tif",
                    "tiger-rgb-tile-contig-03.tif",
                    "tiger-rgb-tile-contig-04.tif",
                    "tiger-rgb-tile-contig-32.tif",
                    "tiger-rgb-tile-contig-64.tif",
                    "tiger-rgb-tile-planar-24.tif",
                    "tiger-rgb-tile-planar-32.tif",
                    "tiger-rgb-tile-planar-64.tif",
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

        [Test, TestCaseSource("TiledFiles")]
        public void Test(string file)
        {
            performTest(file, new string[] { "-c", "none", "-s" }, "_converted_strips_none");
        }
    }
}
