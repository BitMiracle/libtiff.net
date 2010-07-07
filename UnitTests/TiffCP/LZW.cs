using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class LZW
    {
        private static string[] FP_Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-float-strip-16.tif",
                    "tiger-minisblack-float-strip-24.tif",
                    "tiger-minisblack-float-strip-32.tif",
                    "tiger-minisblack-float-strip-64.tif",
                    "tiger-minisblack-float-tile-16.tif",
                    "tiger-minisblack-float-tile-24.tif",
                    "tiger-minisblack-float-tile-32.tif",
                    "tiger-minisblack-float-tile-64.tif",
                    "tiger-rgb-float-strip-contig-16.tif",
                    "tiger-rgb-float-strip-contig-24.tif",
                    "tiger-rgb-float-strip-contig-32.tif",
                    "tiger-rgb-float-strip-contig-64.tif",
                    "tiger-rgb-float-strip-planar-16.tif",
                    "tiger-rgb-float-strip-planar-24.tif",
                    "tiger-rgb-float-strip-planar-32.tif",
                    "tiger-rgb-float-strip-planar-64.tif",
                    "tiger-rgb-float-tile-contig-16.tif",
                    "tiger-rgb-float-tile-contig-24.tif",
                    "tiger-rgb-float-tile-contig-32.tif",
                    "tiger-rgb-float-tile-contig-64.tif",
                    "tiger-rgb-float-tile-planar-16.tif",
                    "tiger-rgb-float-tile-planar-24.tif",
                    "tiger-rgb-float-tile-planar-32.tif",
                    "tiger-rgb-float-tile-planar-64.tif",
                };
            }
        }

        private static string[] HP_Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-08.tif",
                    "tiger-minisblack-strip-16.tif",
                    "tiger-minisblack-tile-08.tif",
                    "tiger-minisblack-tile-16.tif",
                    "tiger-palette-strip-16.tif",
                    "tiger-palette-tile-16.tif",
                    "tiger-rgb-strip-contig-16.tif",
                    "tiger-rgb-strip-planar-08.tif",
                    "tiger-rgb-strip-planar-16.tif",
                    "tiger-rgb-tile-contig-08.tif",
                    "tiger-rgb-tile-contig-16.tif",
                    "tiger-rgb-tile-planar-16.tif",
                    "tiger-separated-strip-contig-16.tif",
                    "tiger-separated-strip-planar-08.tif",
                    "tiger-separated-strip-planar-16.tif",
                };
            }
        }

        [Test, TestCaseSource("FP_Files")]
        public void Test_FP(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "lzw" }, "_converted_lzw");
        }

        [Test, TestCaseSource("FP_Files")]
        public void Test_FP_3(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "lzw:3" }, "_converted_lzw_3");
        }

        [Test, TestCaseSource("HP_Files")]
        public void Test_HP(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "lzw:2" }, "_converted_lzw_2");
        }
    }
}
