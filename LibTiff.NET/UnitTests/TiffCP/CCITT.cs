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

        private static string[] m_g3_1d_args = new string[] { "-c", "g3:1d" };
        private const string m_g3_1d_suffix = "_converted_g3_1d";

        private static string[] m_g3_1d_fill_args = new string[] { "-c", "g3:1d:fill" };
        private const string m_g3_1d_fill_suffix = "_converted_g3_1d_fill";

        private static string[] m_g3_2d_args = new string[] { "-c", "g3:2d" };
        private const string m_g3_2d_suffix = "_converted_g3_2d";

        private static string[] m_g3_2d_fill_args = new string[] { "-c", "g3:2d:fill" };
        private const string m_g3_2d_fill_suffix = "_converted_g3_2d_fill";

        private static string[] m_g4_args = new string[] { "-c", "g4" };
        private const string m_g4_suffix = "_converted_g4";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_g3_1d_tiger_minisblack_strip_01()
        {
            performTest("tiger-minisblack-strip-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_tiger_minisblack_tile_01()
        {
            performTest("tiger-minisblack-tile-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_tiger_palette_strip_01()
        {
            performTest("tiger-palette-strip-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_tiger_palette_tile_01()
        {
            performTest("tiger-palette-tile-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_tiger_rgb_strip_contig_01()
        {
            performTest("tiger-rgb-strip-contig-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_tiger_rgb_strip_planar_01()
        {
            performTest("tiger-rgb-strip-planar-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_tiger_rgb_tile_contig_01()
        {
            performTest("tiger-rgb-tile-contig-01.tif", m_g3_1d_args, m_g3_1d_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_minisblack_strip_01()
        {
            performTest("tiger-minisblack-strip-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_minisblack_tile_01()
        {
            performTest("tiger-minisblack-tile-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_palette_strip_01()
        {
            performTest("tiger-palette-strip-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_palette_tile_01()
        {
            performTest("tiger-palette-tile-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_rgb_strip_contig_01()
        {
            performTest("tiger-rgb-strip-contig-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_rgb_strip_planar_01()
        {
            performTest("tiger-rgb-strip-planar-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_1d_fill_tiger_rgb_tile_contig_01()
        {
            performTest("tiger-rgb-tile-contig-01.tif", m_g3_1d_fill_args, m_g3_1d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_minisblack_strip_01()
        {
            performTest("tiger-minisblack-strip-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_minisblack_tile_01()
        {
            performTest("tiger-minisblack-tile-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_palette_strip_01()
        {
            performTest("tiger-palette-strip-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_palette_tile_01()
        {
            performTest("tiger-palette-tile-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_rgb_strip_contig_01()
        {
            performTest("tiger-rgb-strip-contig-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_rgb_strip_planar_01()
        {
            performTest("tiger-rgb-strip-planar-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_tiger_rgb_tile_contig_01()
        {
            performTest("tiger-rgb-tile-contig-01.tif", m_g3_2d_args, m_g3_2d_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_minisblack_strip_01()
        {
            performTest("tiger-minisblack-strip-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_minisblack_tile_01()
        {
            performTest("tiger-minisblack-tile-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_palette_strip_01()
        {
            performTest("tiger-palette-strip-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_palette_tile_01()
        {
            performTest("tiger-palette-tile-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_rgb_strip_contig_01()
        {
            performTest("tiger-rgb-strip-contig-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_rgb_strip_planar_01()
        {
            performTest("tiger-rgb-strip-planar-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g3_2d_fill_tiger_rgb_tile_contig_01()
        {
            performTest("tiger-rgb-tile-contig-01.tif", m_g3_2d_fill_args, m_g3_2d_fill_suffix);
        }

        [Test]
        public void test_g4_tiger_minisblack_strip_01()
        {
            performTest("tiger-minisblack-strip-01.tif", m_g4_args, m_g4_suffix);
        }

        [Test]
        public void test_g4_tiger_minisblack_tile_01()
        {
            performTest("tiger-minisblack-tile-01.tif", m_g4_args, m_g4_suffix);
        }

        [Test]
        public void test_g4_tiger_palette_strip_01()
        {
            performTest("tiger-palette-strip-01.tif", m_g4_args, m_g4_suffix);
        }

        [Test]
        public void test_g4_tiger_palette_tile_01()
        {
            performTest("tiger-palette-tile-01.tif", m_g4_args, m_g4_suffix);
        }

        [Test]
        public void test_g4_tiger_rgb_strip_contig_01()
        {
            performTest("tiger-rgb-strip-contig-01.tif", m_g4_args, m_g4_suffix);
        }

        [Test]
        public void test_g4_tiger_rgb_strip_planar_01()
        {
            performTest("tiger-rgb-strip-planar-01.tif", m_g4_args, m_g4_suffix);
        }

        [Test]
        public void test_g4_tiger_rgb_tile_contig_01()
        {
            performTest("tiger-rgb-tile-contig-01.tif", m_g4_args, m_g4_suffix);
        }
    }
}
