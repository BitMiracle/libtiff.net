using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace UnitTests.TiffCP
{
    class Tester
    {
        private static object locked = new object();

        public static void PerformTest(string file, string[] args, string suffix)
        {
            Tester tester = new Tester();
            string inputFile = Path.Combine(TestCase.Folder, Path.GetFileName(file));
            string outputFile = TestCase.Folder + @"Output.Tiff\" + Path.GetFileName(file) + suffix + ".tif";
            tester.Run(args, inputFile, outputFile);
        }

        public void Run(string[] args, string sourceFile, string targetFile)
        {
            // console programs' Main are static, so lock concurrent access to 
            // a test code. we use a private field to lock upon 

            lock (locked)
            {
                List<string> completeArgs = new List<string>(args.Length + 2);

                for (int i = 0; i < args.Length; ++i)
                    completeArgs.Add(args[i]);

                completeArgs.Add(sourceFile);
                completeArgs.Add(targetFile);

                File.Delete(targetFile);

                BitMiracle.TiffCP.Program.Main(completeArgs.ToArray());

                string sampleFile = targetFile.Replace(@"\Output.Tiff\", @"\Expected.Tiff\");
                Assert.IsTrue(File.Exists(targetFile));
                FileAssert.AreEqual(sampleFile, targetFile);
            }
        }
    }
}
