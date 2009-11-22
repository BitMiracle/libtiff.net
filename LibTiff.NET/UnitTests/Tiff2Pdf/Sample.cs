using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests.Tiff2Pdf
{
    [TestFixture]
    public class Sample : SampleBase
    {
        public override void performTest(string file)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"pdf\" + Path.GetFileName(file) + ".pdf";
            tester.Run(false, new string[] { "-o" }, Path.Combine(fullPath, file), outputFile);
        }
    }

    [TestFixture]
    public class Sample2 : SampleBase
    {
        public override void performTest(string file)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"pdf2\" + Path.GetFileName(file) + ".pdf";
            tester.Run(true, new string[] { "-o" }, Path.Combine(fullPath, file), outputFile);
        }
    }
}
