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

        private static string[] m_args = new string[] { "-c", "packbits" };
        private const string m_suffix = "_converted_packbits";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_tiger_minisblack_strip_02()
        {
            performTest("tiger-minisblack-strip-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_minisblack_tile_02()
        {
            performTest("tiger-minisblack-tile-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_strip_02()
        {
            performTest("tiger-palette-strip-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_tile_02()
        {
            performTest("tiger-palette-tile-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_strip_contig_02()
        {
            performTest("tiger-rgb-strip-contig-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_strip_planar_02()
        {
            performTest("tiger-rgb-strip-planar-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_02()
        {
            performTest("tiger-rgb-tile-contig-02.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_planar_16()
        {
            performTest("tiger-rgb-tile-planar-16.tif", m_args, m_suffix);
        }
    }
}
