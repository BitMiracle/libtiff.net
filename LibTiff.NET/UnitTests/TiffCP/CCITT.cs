using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class CCITT
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "CCITT";

        public void performTest(string[] args, string file, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_g3_1d()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "g3:1d" }, file, "_converted_g3_1d");
            }
        }

        [Test]
        public void test_g3_1d_fill()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "g3:1d:fill" }, file, "_converted_g3_1d_fill");
            }
        }

        [Test]
        public void test_g3_2d()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "g3:2d" }, file, "_converted_g3_2d");
            }
        }

        [Test]
        public void test_g3_2d_fill()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "g3:2d:fill" }, file, "_converted_g3_2d_fill");
            }
        }

        [Test]
        public void test_g4()
        {
            string fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);
            string[] files = Directory.GetFiles(fullPath);
            foreach (string file in files)
            {
                performTest(new string[] { "-c", "g4" }, file, "_converted_g4");
            }
        }
    }
}
