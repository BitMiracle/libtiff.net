using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests.Tiff2Pdf
{
    class Tester
    {
        private const string m_testcase = @"..\..\..\..\TestCase\";
	
        private static object locked = new object();

        private string m_dataFolder;

        public Tester(string dataFolder)
        {
            m_dataFolder = dataFolder;
        }

        public string TestCaseFolder
	    {
		    get { return m_testcase; }
	    }

        public void Run(bool useV2, string[] args, string sourceFile, string targetFile)
        {
            // console programs' Main are static, so lock concurrent access to 
            // a test code. we use a private field to lock upon 

            lock (locked)
            {
                string dataFolder = m_testcase + m_dataFolder;
                List<string> completeArgs = new List<string>(args.Length + 2);

                for (int i = 0; i < args.Length; ++i)
                    completeArgs.Add(args[i]);

                completeArgs.Add(targetFile);
                completeArgs.Add(sourceFile);

                File.Delete(targetFile);

                if (useV2)
                {
                    BitMiracle.Tiff2Pdf2.Program.g_testFriendly = true;
                    BitMiracle.Tiff2Pdf2.Program.Main(completeArgs.ToArray());
                }
                else
                {
                    BitMiracle.Tiff2Pdf.Program.g_testFriendly = true;
                    BitMiracle.Tiff2Pdf.Program.Main(completeArgs.ToArray());
                }

                string sampleFile = targetFile.Replace(@"\pdf\", @"\sample_pdf\");
                if (useV2)
                    sampleFile = targetFile.Replace(@"\pdf2\", @"\sample_pdf2\");

                Assert.IsTrue(File.Exists(targetFile));
                Assert.IsTrue(Utils.FilesAreEqual(sampleFile, targetFile));
            }
        }
    }
}
