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
        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "tiger-minisblack-strip-02.tif",
                    "tiger-minisblack-tile-02.tif",
                    "tiger-palette-strip-02.tif",
                    "tiger-palette-tile-02.tif",
                    "tiger-rgb-strip-contig-02.tif",
                    "tiger-rgb-strip-planar-02.tif",
                    "tiger-rgb-tile-contig-02.tif",
                    "tiger-rgb-tile-planar-16.tif",
                };
            }
        }

        [Test, TestCaseSource("Files")]
        public void Test(string file)
        {
            Tester.PerformTest(file, new string[] { "-c", "packbits" }, "_converted_packbits");
        }
    }
}
