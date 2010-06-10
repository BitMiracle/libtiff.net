using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class Deflate
    {
        private const string m_dataFolder = @"tiffcp_data\";

        private const string m_hp_data_subfolder = "Predictor_Horizontal";
        private const string m_fp_data_subfolder = "Predictor_float";

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

        public void performTest(string file, string dataSubFolder, string[] args, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test, TestCaseSource("HP_Files")]
        public void Test_HP(string file)
        {
            performTest(file, m_hp_data_subfolder, new string[] { "-c", "zip" }, "_converted_deflate");
        }

        [Test, TestCaseSource("HP_Files")]
        public void Test_HP_2(string file)
        {
            performTest(file, m_hp_data_subfolder, new string[] { "-c", "zip:2" }, "_converted_deflate_2");
        }

        [Test, TestCaseSource("FP_Files")]
        public void Test_FP(string file)
        {
            performTest(file, m_fp_data_subfolder, new string[] { "-c", "zip:3" }, "_converted_deflate_3");
        }
    }
}
