using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests
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

        public void Run(string[] args, string sourceFile, string targetFile)
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

                BitMiracle.Tiff2Pdf.Program.Main(completeArgs.ToArray());

                string sampleFile = targetFile.Replace(@"\pdf\", @"\sample_pdf\");
                Assert.IsTrue(File.Exists(targetFile));
                Assert.IsTrue(FilesAreEqual(sampleFile, targetFile));
            }
        }

        private static bool FilesAreEqual(string left, string right)
        {
            byte[] leftBytes = File.ReadAllBytes(left);
            byte[] rightBytes = File.ReadAllBytes(right);

            if (leftBytes == null || rightBytes == null)
                return false;

            if (leftBytes.Length != rightBytes.Length)
                return false;

            for (int i = 0; i < leftBytes.Length; i++)
            {
                if (leftBytes[i] != rightBytes[i])
                    return false;
            }

            return true;
        }
    }
}
