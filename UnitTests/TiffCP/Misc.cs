using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    class Misc
    {
        private static string[] ContigousFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-08.tif",
                    "tiger-minisblack-tile-08.tif",
                    "tiger-rgb-tile-contig-08.tif",
                };
            }
        }

        private static string[] SeparateFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-08.tif",
                    "tiger-minisblack-tile-08.tif",
                    "tiger-rgb-strip-planar-08.tif",
                    "tiger-separated-strip-planar-08.tif",
                };
            }
        }

        private static string[] StrippedFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-rgb-strip-contig-03.tif",
                    "tiger-rgb-strip-contig-04.tif",
                    "tiger-separated-strip-contig-16.tif",
                };
            }
        }

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

        private static string[] ToNoEncodingFiles
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-05.tif",
                    "tiger-palette-strip-07.tif",
                    "tiger-rgb-strip-contig-03.tif",
                    "tiger-rgb-strip-planar-03.tif",
                    "tiger-rgb-tile-contig-03.tif",
                    "tiger-rgb-tile-planar-24.tif",
                    "penguin_jpeg.tif",
                };
            }
        }

        [Test, TestCaseSource("ContigousFiles")]
        public void TestContigousToSeparate(string file)
        {
            Tester.PerformTest(file, new string[] { "-p", "separate", "-c", "lzw" }, "_converted_separate_lzw");
        }

        [Test, TestCaseSource("SeparateFiles")]
        public void TestSeparateToContigous(string file)
        {
            Tester.PerformTest(file, new string[] { "-p", "contig", "-c", "lzw" }, "_converted_contig_lzw");
        }

        [Test, TestCaseSource("StrippedFiles")]
        public void TestStripsToTiles(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "none", "-t" }, "_converted_tiles_none");
        }

        [Test, TestCaseSource("TiledFiles")]
        public void TestTilesToStrips(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "none", "-s" }, "_converted_strips_none");
        }

        [Test, TestCaseSource("ToNoEncodingFiles")]
        public void TestToNoEncoding(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "none", "-s" }, "_converted_strips");
        }

        [Test]
        public void test_penguin_separate_jpeg()
        {
            Tester.PerformTest("penguin_jpeg.tif",
                new string[] { "-c", "none", "-p", "separate" },
                "_converted_strips_separate");
        }
    }
}
