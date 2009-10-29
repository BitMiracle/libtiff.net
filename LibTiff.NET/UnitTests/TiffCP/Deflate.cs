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

        public void performTest(string dataSubFolder, string[] args, string file, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_deflate_horizontal_predictor()
        {
            string dataSubFolder = "Predictor_Horizontal";
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(dataSubFolder, new string[] { "-c", "zip" }, file, "_converted_deflate");
            }
        }

        [Test]
        public void test_deflate_2_horizontal_predictor()
        {
            string dataSubFolder = "Predictor_Horizontal";
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(dataSubFolder, new string[] { "-c", "zip:2" }, file, "_converted_deflate_2");
            }
        }

        [Test]
        public void test_deflate_float_predictor()
        {
            string dataSubFolder = "Predictor_float";
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(dataSubFolder, new string[] { "-c", "zip:3" }, file, "_converted_deflate_3");
            }
        }
    }
}
