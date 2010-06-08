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

        private static string[] m_args = new string[] { "-p", "separate", "-c", "lzw" };
        private const string m_suffix = "_converted_separate_lzw";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_tiger_minisblack_strip_08()
        {
            performTest("tiger-minisblack-strip-08.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_minisblack_tile_08()
        {
            performTest("tiger-minisblack-tile-08.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_08()
        {
            performTest("tiger-rgb-tile-contig-08.tif", m_args, m_suffix);
        }
    }
}
