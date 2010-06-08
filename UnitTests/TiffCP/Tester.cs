using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;

namespace UnitTests.TiffCP
{
    class Tester
    {
        private const string m_testcase = @"..\..\..\TestCase\";

        private static object locked = new object();

        private string m_dataFolder;

        public Tester(string dataFolder)
        {
            m_dataFolder = dataFolder;
        }

        public static string TestCaseFolder
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

                completeArgs.Add(sourceFile);
                completeArgs.Add(targetFile);

                File.Delete(targetFile);

                BitMiracle.TiffCP.Program.Main(completeArgs.ToArray());

                string sampleFile = targetFile.Replace(@"\_converted\", @"\_sample\");
                Assert.IsTrue(File.Exists(targetFile));
                Assert.IsTrue(Utils.FilesAreEqual(sampleFile, targetFile));
            }
        }
    }
}
