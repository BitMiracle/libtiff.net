using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    [TestFixture]
    public class Multipage
    {
        private const string m_dataFolder = @"tiffcp_data\";
        private const string m_dataSubFolder = "Multipage";
        private string m_fullPath = Path.Combine(Path.Combine(Tester.TestCaseFolder, m_dataFolder), m_dataSubFolder);

        public void performTest(string[] args, string file, string pageSpecifier, string suffix)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(Tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"_converted\" + Path.GetFileName(file) + suffix + ".tif";
            string inputFile = Path.Combine(Path.Combine(fullPath, m_dataSubFolder), Path.GetFileName(file));
            inputFile += pageSpecifier;
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_page0()
        {
            string file = Path.Combine(m_fullPath, "1.tif");
            performTest(new string[] { }, file, ",0", "_page0_converted");
        }

        [Test]
        public void test_page1()
        {
            string file = Path.Combine(m_fullPath, "1.tif");
            performTest(new string[] { }, file, ",1", "_page1_converted");
        }

        [Test]
        public void test_page2()
        {
            string file = Path.Combine(m_fullPath, "1.tif");
            performTest(new string[] { }, file, ",2", "_page2_converted");
        }

        [Test]
        public void test_page0and2()
        {
            string file = Path.Combine(m_fullPath, "1.tif");
            performTest(new string[] { }, file, ",0,2", "_page0and2_converted");
        }

        [Test]
        public void test_afterPage1()
        {
            string file = Path.Combine(m_fullPath, "1.tif");
            performTest(new string[] { }, file, ",1,", "_afterPage1_converted");
        }
    }
}
