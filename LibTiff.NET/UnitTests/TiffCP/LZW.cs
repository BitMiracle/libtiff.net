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

        public void performTest(string dataSubFolder, string[] args, string file, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_lzw_float_predictor()
        {
            string dataSubFolder = "Predictor_float";
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(dataSubFolder, new string[] { "-c", "lzw" }, file, "_converted_lzw");
            }
        }

        [Test]
        public void test_lzw_3_float_predictor()
        {
            string dataSubFolder = "Predictor_float";
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(dataSubFolder, new string[] { "-c", "lzw:3" }, file, "_converted_lzw_3");
            }
        }

        [Test]
        public void test_lzw_horizontal_predictor()
        {
            string dataSubFolder = "Predictor_Horizontal";
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(dataSubFolder, new string[] { "-c", "lzw:2" }, file, "_converted_lzw_2");
            }
        }
    }
}
