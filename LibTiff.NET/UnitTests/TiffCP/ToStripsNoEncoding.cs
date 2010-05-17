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

        private static string[] m_args = new string[] { "-c", "none", "-s" };
        private const string m_suffix = "_converted_strips";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_tiger_minisblack_strip_05()
        {
            performTest("tiger-minisblack-strip-05.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_strip_07()
        {
            performTest("tiger-palette-strip-07.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_strip_contig_03()
        {
            performTest("tiger-rgb-strip-contig-03.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_strip_planar_03()
        {
            performTest("tiger-rgb-strip-planar-03.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_03()
        {
            performTest("tiger-rgb-tile-contig-03.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_planar_24()
        {
            performTest("tiger-rgb-tile-planar-24.tif", m_args, m_suffix);
        }

        [Test]
        public void test_penguin_jpeg()
        {
            performTest("penguin_jpeg.tif", m_args, m_suffix);
        }
    }
}
