using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Tiff2Pdf
    {
        private const string m_sampleDataFolder = @"tiff2pdf_data\";
        private const string m_chaoticDataFolder = @"tiff2pdf_data_chaotic\";

        [Test]
        public void TestSampleCollection()
        {
            Tester tester = new Tester(m_sampleDataFolder);
            string fullPath = Path.Combine(tester.TestCaseFolder, m_sampleDataFolder);
            string[] files = Directory.GetFiles(fullPath, "*.tif?");
            foreach (string file in files)
            {
                string outputFile = fullPath + @"pdf\" + Path.GetFileName(file) + ".pdf";
                tester.Run(new string[] { "-o"}, file, outputFile);
            }
        }

        [Test]
        public void TestChaoticCollection()
        {
            Tester tester = new Tester(m_chaoticDataFolder);
            string fullPath = Path.Combine(tester.TestCaseFolder, m_chaoticDataFolder);
            string[] files = Directory.GetFiles(fullPath, "*.tif?");
            foreach (string file in files)
            {
                string outputFile = fullPath + @"pdf\" + Path.GetFileName(file) + ".pdf";
                tester.Run(new string[] { "-o" }, file, outputFile);
            }
        }
    }
}
