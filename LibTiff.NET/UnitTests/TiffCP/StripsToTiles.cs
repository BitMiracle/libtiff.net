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

        private static string[] m_args = new string[] { "-c", "none", "-t" };
        private const string m_suffix = "_converted_tiles_none";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_tiger_rgb_strip_contig_03()
        {
            performTest("tiger-rgb-strip-contig-03.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_strip_contig_04()
        {
            performTest("tiger-rgb-strip-contig-04.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_separated_strip_contig_16()
        {
            performTest("tiger-separated-strip-contig-16.tif", m_args, m_suffix);
        }
    }
}
