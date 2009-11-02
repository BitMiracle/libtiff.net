using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class Jpeg
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "Jpeg";

        private static string[] m_jpeg_args = new string[] { "-c", "jpeg" };
        private const string m_jpeg_suffix = "_converted_jpeg";

        private static string[] m_jpeg_rgb_args = new string[] { "-c", "jpeg:r" };
        private const string m_jpeg_rgb_suffix = "_converted_jpeg_rgb";

        private static string[] m_jpeg_rgb_50_args = new string[] { "-c", "jpeg:r:50" };
        private const string m_jpeg_rgb_50_suffix = "_converted_jpeg_rgb_50";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_jpeg_tiger_minisblack_strip_08()
        {
            performTest("tiger-minisblack-strip-08.tif", m_jpeg_args, m_jpeg_suffix);
        }

        [Test]
        public void test_jpeg_tiger_minisblack_tile_08()
        {
            performTest("tiger-minisblack-tile-08.tif", m_jpeg_args, m_jpeg_suffix);
        }

        [Test]
        public void test_jpeg_tiger_rgb_tile_contig_08()
        {
            performTest("tiger-rgb-tile-contig-08.tif", m_jpeg_args, m_jpeg_suffix);
        }

        [Test]
        public void test_jpeg_tiger_separated_strip_planar_08()
        {
            performTest("tiger-separated-strip-planar-08.tif", m_jpeg_args, m_jpeg_suffix);
        }

        [Test]
        public void test_jpeg_rgb_tiger_minisblack_strip_08()
        {
            performTest("tiger-minisblack-strip-08.tif", m_jpeg_rgb_args, m_jpeg_rgb_suffix);
        }

        [Test]
        public void test_jpeg_rgb_tiger_minisblack_tile_08()
        {
            performTest("tiger-minisblack-tile-08.tif", m_jpeg_rgb_args, m_jpeg_rgb_suffix);
        }

        [Test]
        public void test_jpeg_rgb_tiger_rgb_tile_contig_08()
        {
            performTest("tiger-rgb-tile-contig-08.tif", m_jpeg_rgb_args, m_jpeg_rgb_suffix);
        }

        [Test]
        public void test_jpeg_rgb_tiger_separated_strip_planar_08()
        {
            performTest("tiger-separated-strip-planar-08.tif", m_jpeg_rgb_args, m_jpeg_rgb_suffix);
        }

        [Test]
        public void test_jpeg_rgb_50_tiger_minisblack_strip_08()
        {
            performTest("tiger-minisblack-strip-08.tif", m_jpeg_rgb_50_args, m_jpeg_rgb_50_suffix);
        }

        [Test]
        public void test_jpeg_rgb_50_tiger_minisblack_tile_08()
        {
            performTest("tiger-minisblack-tile-08.tif", m_jpeg_rgb_50_args, m_jpeg_rgb_50_suffix);
        }

        [Test]
        public void test_jpeg_rgb_50_tiger_rgb_tile_contig_08()
        {
            performTest("tiger-rgb-tile-contig-08.tif", m_jpeg_rgb_50_args, m_jpeg_rgb_50_suffix);
        }

        [Test]
        public void test_jpeg_rgb_50_tiger_separated_strip_planar_08()
        {
            performTest("tiger-separated-strip-planar-08.tif", m_jpeg_rgb_50_args, m_jpeg_rgb_50_suffix);
        }
    }
}
