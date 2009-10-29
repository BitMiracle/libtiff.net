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
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "Jpeg";

        public void performTest(string[] args, string file, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_jpeg()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "jpeg" }, file, "_converted_jpeg");
            }
        }

        [Test]
        public void test_jpeg_rgb()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "jpeg:r" }, file, "_converted_jpeg_rgb");
            }
        }

        [Test]
        public void test_jpeg_rgb_50()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "jpeg:r:50" }, file, "_converted_jpeg_rgb_50");
            }
        }
    }
}
