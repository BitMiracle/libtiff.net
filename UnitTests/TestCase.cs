using System.IO;
using System.Text;

using NUnit.Framework;

namespace UnitTests
{
    static class TestCase
    {
        public static readonly string Folder;

        static TestCase()
        {
#if NETSTANDARD
            string currentDirectoryPath = Directory.GetCurrentDirectory();
#else
            string currentDirectoryPath = TestContext.CurrentContext.TestDirectory;
#endif
            StringBuilder pathToTestcase = new StringBuilder("TestCase\\");
            var dir = new DirectoryInfo(currentDirectoryPath);
            while (dir.Parent != null)
            {
                var dirs = dir.GetDirectories("TestCase", SearchOption.TopDirectoryOnly);
                var testcase = (dirs.Length > 0) ? dirs[0] : null;
                if (testcase != null)
                {
                    Folder = Path.Combine(currentDirectoryPath, pathToTestcase.ToString());
                    return;
                }

                dir = dir.Parent;
                pathToTestcase.Insert(0, "..\\");
            }

            Assert.Fail("Unable to find TestCase directory");
        }
    }
}
