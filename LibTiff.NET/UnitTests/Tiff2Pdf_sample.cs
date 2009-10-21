using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Tiff2Pdf_sample
    {
        private const string m_dataFolder = @"tiff2pdf_data\";
        private Tester m_tester = new Tester(m_dataFolder);

        [Test]
        public void TestAll()
        {
            string fullPath = Path.Combine(m_tester.TestCaseFolder, m_dataFolder);
            string[] files = Directory.GetFiles(fullPath, "*.tif?");
            foreach (string file in files)
            {
                string outputFile = fullPath + @"pdf\" + Path.GetFileName(file) + ".pdf";
                m_tester.Run(new string[] { "-o"}, file, outputFile);
            }
        }
    }
}
