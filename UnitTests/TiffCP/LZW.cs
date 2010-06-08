using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class LZW
    {
        private const string m_dataFolder = @"tiffcp_data\";

        private const string m_hp_data_subfolder = "Predictor_Horizontal";
        private const string m_fp_data_subfolder = "Predictor_float";

        private static string[] m_lzw_fp_args = new string[] { "-c", "lzw" };
        private static string m_lzw_fp_suffix = "_converted_lzw";

        private static string[] m_lzw_fp_3_args = new string[] { "-c", "lzw:3" };
        private static string m_lzw_fp_3_suffix = "_converted_lzw_3";

        private static string[] m_lzw_hp_args = new string[] { "-c", "lzw:2" };
        private static string m_lzw_hp_suffix = "_converted_lzw_2";

        public void performTest(string file, string dataSubFolder, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_strip_16()
        {
            performTest("tiger-minisblack-float-strip-16.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_strip_24()
        {
            performTest("tiger-minisblack-float-strip-24.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_strip_32()
        {
            performTest("tiger-minisblack-float-strip-32.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_strip_64()
        {
            performTest("tiger-minisblack-float-strip-64.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_tile_16()
        {
            performTest("tiger-minisblack-float-tile-16.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_tile_24()
        {
            performTest("tiger-minisblack-float-tile-24.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_tile_32()
        {
            performTest("tiger-minisblack-float-tile-32.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_minisblack_float_tile_64()
        {
            performTest("tiger-minisblack-float-tile-64.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_contig_16()
        {
            performTest("tiger-rgb-float-strip-contig-16.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_contig_24()
        {
            performTest("tiger-rgb-float-strip-contig-24.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_contig_32()
        {
            performTest("tiger-rgb-float-strip-contig-32.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_contig_64()
        {
            performTest("tiger-rgb-float-strip-contig-64.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_planar_16()
        {
            performTest("tiger-rgb-float-strip-planar-16.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_planar_24()
        {
            performTest("tiger-rgb-float-strip-planar-24.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_planar_32()
        {
            performTest("tiger-rgb-float-strip-planar-32.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_strip_planar_64()
        {
            performTest("tiger-rgb-float-strip-planar-64.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_contig_16()
        {
            performTest("tiger-rgb-float-tile-contig-16.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_contig_24()
        {
            performTest("tiger-rgb-float-tile-contig-24.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_contig_32()
        {
            performTest("tiger-rgb-float-tile-contig-32.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_contig_64()
        {
            performTest("tiger-rgb-float-tile-contig-64.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_planar_16()
        {
            performTest("tiger-rgb-float-tile-planar-16.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_planar_24()
        {
            performTest("tiger-rgb-float-tile-planar-24.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_planar_32()
        {
            performTest("tiger-rgb-float-tile-planar-32.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_tiger_rgb_float_tile_planar_64()
        {
            performTest("tiger-rgb-float-tile-planar-64.tif", m_fp_data_subfolder, m_lzw_fp_args, m_lzw_fp_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_strip_16()
        {
            performTest("tiger-minisblack-float-strip-16.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_strip_24()
        {
            performTest("tiger-minisblack-float-strip-24.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_strip_32()
        {
            performTest("tiger-minisblack-float-strip-32.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_strip_64()
        {
            performTest("tiger-minisblack-float-strip-64.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_tile_16()
        {
            performTest("tiger-minisblack-float-tile-16.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_tile_24()
        {
            performTest("tiger-minisblack-float-tile-24.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_tile_32()
        {
            performTest("tiger-minisblack-float-tile-32.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_minisblack_float_tile_64()
        {
            performTest("tiger-minisblack-float-tile-64.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_contig_16()
        {
            performTest("tiger-rgb-float-strip-contig-16.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_contig_24()
        {
            performTest("tiger-rgb-float-strip-contig-24.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_contig_32()
        {
            performTest("tiger-rgb-float-strip-contig-32.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_contig_64()
        {
            performTest("tiger-rgb-float-strip-contig-64.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_planar_16()
        {
            performTest("tiger-rgb-float-strip-planar-16.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_planar_24()
        {
            performTest("tiger-rgb-float-strip-planar-24.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_planar_32()
        {
            performTest("tiger-rgb-float-strip-planar-32.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_strip_planar_64()
        {
            performTest("tiger-rgb-float-strip-planar-64.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_contig_16()
        {
            performTest("tiger-rgb-float-tile-contig-16.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_contig_24()
        {
            performTest("tiger-rgb-float-tile-contig-24.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_contig_32()
        {
            performTest("tiger-rgb-float-tile-contig-32.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_contig_64()
        {
            performTest("tiger-rgb-float-tile-contig-64.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_planar_16()
        {
            performTest("tiger-rgb-float-tile-planar-16.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_planar_24()
        {
            performTest("tiger-rgb-float-tile-planar-24.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_planar_32()
        {
            performTest("tiger-rgb-float-tile-planar-32.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_fp_3_tiger_rgb_float_tile_planar_64()
        {
            performTest("tiger-rgb-float-tile-planar-64.tif", m_fp_data_subfolder, m_lzw_fp_3_args, m_lzw_fp_3_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_minisblack_strip_08()
        {
            performTest("tiger-minisblack-strip-08.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_minisblack_strip_16()
        {
            performTest("tiger-minisblack-strip-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_minisblack_tile_08()
        {
            performTest("tiger-minisblack-tile-08.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_minisblack_tile_16()
        {
            performTest("tiger-minisblack-tile-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_palette_strip_16()
        {
            performTest("tiger-palette-strip-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_palette_tile_16()
        {
            performTest("tiger-palette-tile-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_rgb_strip_contig_16()
        {
            performTest("tiger-rgb-strip-contig-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_rgb_strip_planar_08()
        {
            performTest("tiger-rgb-strip-planar-08.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_rgb_strip_planar_16()
        {
            performTest("tiger-rgb-strip-planar-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_rgb_tile_contig_08()
        {
            performTest("tiger-rgb-tile-contig-08.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_rgb_tile_contig_16()
        {
            performTest("tiger-rgb-tile-contig-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_rgb_tile_planar_16()
        {
            performTest("tiger-rgb-tile-planar-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_separated_strip_contig_16()
        {
            performTest("tiger-separated-strip-contig-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_separated_strip_planar_08()
        {
            performTest("tiger-separated-strip-planar-08.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }

        [Test]
        public void test_lzw_hp_tiger_separated_strip_planar_16()
        {
            performTest("tiger-separated-strip-planar-16.tif", m_hp_data_subfolder, m_lzw_hp_args, m_lzw_hp_suffix);
        }
    }
}
