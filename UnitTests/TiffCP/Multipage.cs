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
        private void performTest(string[] args, string file, string pageSpecifier, string suffix)
        {
            Tester tester = new Tester();

            string inputFile = Path.Combine(TestCase.Folder, Path.GetFileName(file));
            inputFile += pageSpecifier;
            
            string outputFile = TestCase.Folder + @"Output.Tiff\" + Path.GetFileName(file) + suffix + ".tif";
            tester.Run(args, inputFile, outputFile);
        }

        [Test]
        public void test_page0()
        {
            performTest(new string[] { }, "1.tif", ",0", "_page0_converted");
        }

        [Test]
        public void test_page1()
        {
            performTest(new string[] { }, "1.tif", ",1", "_page1_converted");
        }

        [Test]
        public void test_page2()
        {
            performTest(new string[] { }, "1.tif", ",2", "_page2_converted");
        }

        [Test]
        public void test_page0and2()
        {
            performTest(new string[] { }, "1.tif", ",0,2", "_page0and2_converted");
        }

        [Test]
        public void test_afterPage1()
        {
            performTest(new string[] { }, "1.tif", ",1,", "_afterPage1_converted");
        }
    }
}
