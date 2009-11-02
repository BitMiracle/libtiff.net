using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests.Tiff2Pdf
{
    [TestFixture]
    public class Sample
    {
        private const string m_dataFolder = @"tiff2pdf_data\";
        
        public void performTest(string file)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"pdf\" + Path.GetFileName(file) + ".pdf";
            tester.Run(new string[] { "-o"}, Path.Combine(fullPath, file), outputFile);
        }

        [Test]
        public void test_tiger_minisblack_strip_01()
        {
            performTest("tiger-minisblack-strip-01.tif");
        }

        [Test]
        public void test_tiger_minisblack_strip_02()
        {
            performTest("tiger-minisblack-strip-02.tif");
        }

        [Test]
        public void test_tiger_minisblack_strip_04()
        {
            performTest("tiger-minisblack-strip-04.tif");
        }

        [Test]
        public void test_tiger_minisblack_strip_08()
        {
            performTest("tiger-minisblack-strip-08.tif");
        }

        [Test]
        public void test_tiger_minisblack_tile_01()
        {
            performTest("tiger-minisblack-tile-01.tif");
        }

        [Test]
        public void test_tiger_minisblack_tile_02()
        {
            performTest("tiger-minisblack-tile-02.tif");
        }

        [Test]
        public void test_tiger_minisblack_tile_04()
        {
            performTest("tiger-minisblack-tile-04.tif");
        }

        [Test]
        public void test_tiger_minisblack_tile_08()
        {
            performTest("tiger-minisblack-tile-08.tif");
        }

        [Test]
        public void test_tiger_palette_strip_01()
        {
            performTest("tiger-palette-strip-01.tif");
        }

        [Test]
        public void test_tiger_palette_strip_02()
        {
            performTest("tiger-palette-strip-02.tif");
        }

        [Test]
        public void test_tiger_palette_strip_03()
        {
            performTest("tiger-palette-strip-03.tif");
        }

        [Test]
        public void test_tiger_palette_strip_04()
        {
            performTest("tiger-palette-strip-04.tif");
        }

        [Test]
        public void test_tiger_palette_strip_05()
        {
            performTest("tiger-palette-strip-05.tif");
        }

        [Test]
        public void test_tiger_palette_strip_06()
        {
            performTest("tiger-palette-strip-06.tif");
        }

        [Test]
        public void test_tiger_palette_strip_07()
        {
            performTest("tiger-palette-strip-07.tif");
        }

        [Test]
        public void test_tiger_palette_strip_08()
        {
            performTest("tiger-palette-strip-08.tif");
        }

        [Test]
        public void test_tiger_palette_tile_01()
        {
            performTest("tiger-palette-tile-01.tif");
        }

        [Test]
        public void test_tiger_palette_tile_02()
        {
            performTest("tiger-palette-tile-02.tif");
        }

        [Test]
        public void test_tiger_palette_tile_03()
        {
            performTest("tiger-palette-tile-03.tif");
        }

        [Test]
        public void test_tiger_palette_tile_04()
        {
            performTest("tiger-palette-tile-04.tif");
        }

        [Test]
        public void test_tiger_palette_tile_05()
        {
            performTest("tiger-palette-tile-05.tif");
        }

        [Test]
        public void test_tiger_palette_tile_06()
        {
            performTest("tiger-palette-tile-06.tif");
        }

        [Test]
        public void test_tiger_palette_tile_07()
        {
            performTest("tiger-palette-tile-07.tif");
        }

        [Test]
        public void test_tiger_palette_tile_08()
        {
            performTest("tiger-palette-tile-08.tif");
        }

        [Test]
        public void test_tiger_rgb_strip_contig_01()
        {
            performTest("tiger-rgb-strip-contig-01.tif");
        }

        [Test]
        public void test_tiger_rgb_strip_contig_02()
        {
            performTest("tiger-rgb-strip-contig-02.tif");
        }

        [Test]
        public void test_tiger_rgb_strip_contig_04()
        {
            performTest("tiger-rgb-strip-contig-04.tif");
        }

        [Test]
        public void test_tiger_rgb_strip_contig_08()
        {
            performTest("tiger-rgb-strip-contig-08.tif");
        }

        [Test]
        public void test_tiger_rgb_strip_planar_08()
        {
            performTest("tiger-rgb-strip-planar-08.tif");
        }

        [Test]
        public void test_tiger_rgb_tile_contig_01()
        {
            performTest("tiger-rgb-tile-contig-01.tif");
        }

        [Test]
        public void test_tiger_rgb_tile_contig_02()
        {
            performTest("tiger-rgb-tile-contig-02.tif");
        }

        [Test]
        public void test_tiger_rgb_tile_contig_04()
        {
            performTest("tiger-rgb-tile-contig-04.tif");
        }

        [Test]
        public void test_tiger_rgb_tile_contig_08()
        {
            performTest("tiger-rgb-tile-contig-08.tif");
        }

        [Test]
        public void test_tiger_rgb_tile_planar_08()
        {
            performTest("tiger-rgb-tile-planar-08.tif");
        }

        [Test]
        public void test_tiger_separated_strip_contig_08()
        {
            performTest("tiger-separated-strip-contig-08.tif");
        }

        [Test]
        public void test_tiger_separated_strip_planar_08()
        {
            performTest("tiger-separated-strip-planar-08.tif");
        }
    }
}
