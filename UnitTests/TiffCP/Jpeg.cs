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
        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-08.tif",
                    "tiger-minisblack-tile-08.tif",
                    "tiger-rgb-tile-contig-08.tif",
                    "tiger-separated-strip-planar-08.tif",
                };
            }
        }

        [Test, TestCaseSource("Files")]
        public void Test(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "jpeg" }, "_converted_jpeg");
        }

        [Test, TestCaseSource("Files")]
        public void TestRGB(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "jpeg:r" }, "_converted_jpeg_rgb");
        }

        [Test, TestCaseSource("Files")]
        public void TestRGB_50(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "jpeg:r:50" }, "_converted_jpeg_rgb_50");
        }
    }
}
