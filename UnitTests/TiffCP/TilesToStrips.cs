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

        private static string[] m_args = new string[] { "-c", "none", "-s" };
        private const string m_suffix = "_converted_strips_none";

        public void performTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_tiger_minisblack_tile_03()
        {
            performTest("tiger-minisblack-tile-03.tif", m_args, m_suffix);
        }
        
        [Test]
        public void test_tiger_minisblack_tile_04()
        {
            performTest("tiger-minisblack-tile-04.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_minisblack_tile_32()
        {
            performTest("tiger-minisblack-tile-32.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_minisblack_tile_64()
        {
            performTest("tiger-minisblack-tile-64.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_tile_03()
        {
            performTest("tiger-palette-tile-03.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_tile_04()
        {
            performTest("tiger-palette-tile-04.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_tile_15()
        {
            performTest("tiger-palette-tile-15.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_palette_tile_16()
        {
            performTest("tiger-palette-tile-16.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_03()
        {
            performTest("tiger-rgb-tile-contig-03.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_04()
        {
            performTest("tiger-rgb-tile-contig-04.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_32()
        {
            performTest("tiger-rgb-tile-contig-32.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_contig_64()
        {
            performTest("tiger-rgb-tile-contig-64.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_planar_24()
        {
            performTest("tiger-rgb-tile-planar-24.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_planar_32()
        {
            performTest("tiger-rgb-tile-planar-32.tif", m_args, m_suffix);
        }

        [Test]
        public void test_tiger_rgb_tile_planar_64()
        {
            performTest("tiger-rgb-tile-planar-64.tif", m_args, m_suffix);
        }
    }
}
