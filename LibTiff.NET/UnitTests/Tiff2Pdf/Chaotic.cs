using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace UnitTests.Tiff2Pdf
{
    [TestFixture]
    public class Chaotic
    {
        private const string m_dataFolder = @"tiff2pdf_data_chaotic\";

        public void performTest(string file)
        {
            Tester tester = new Tester(m_dataFolder);
            string fullPath = Path.Combine(tester.TestCaseFolder, m_dataFolder);
            string outputFile = fullPath + @"pdf\" + Path.GetFileName(file) + ".pdf";
            tester.Run(new string[] { "-o" }, Path.Combine(fullPath, file), outputFile);
        }

        [Test]
        public void test_bitmap_zip_pc()
        {
            performTest("bitmap-zip-pc.tif");
        }

        [Test]
        public void test_cas()
        {
            performTest("cas.tif");
        }

        [Test]
        public void test_CCITT_1()
        {
            performTest("CCITT_1.TIF");
        }

        [Test]
        public void test_CCITT_2()
        {
            performTest("CCITT_2.TIF");
        }

        [Test]
        public void test_CCITT_3()
        {
            performTest("CCITT_3.TIF");
        }

        [Test]
        public void test_CCITT_4()
        {
            performTest("CCITT_4.TIF");
        }

        [Test]
        public void test_CCITT_5()
        {
            performTest("CCITT_5.TIF");
        }

        [Test]
        public void test_CCITT_6()
        {
            performTest("CCITT_6.TIF");
        }

        [Test]
        public void test_CCITT_7()
        {
            performTest("CCITT_7.TIF");
        }

        [Test]
        public void test_CCITT_8()
        {
            performTest("CCITT_8.TIF");
        }

        [Test]
        public void test_cmyk8_lzw()
        {
            performTest("cmyk8-lzw.tif");
        }

        [Test]
        public void test_color64_lzw_mac()
        {
            performTest("color64-lzw-mac.tif");
        }

        [Test]
        public void test_color64_lzw_pc()
        {
            performTest("color64-lzw-pc.tif");
        }

        [Test]
        public void test_cramps_tile()
        {
            performTest("cramps-tile.tif");
        }

        [Test]
        public void test_cramps()
        {
            performTest("cramps.tif");
        }

        [Test]
        public void test_dscf0013()
        {
            performTest("dscf0013.tif");
        }

        [Test]
        public void test_fax2d()
        {
            performTest("fax2d.tif");
        }

        [Test]
        public void test_FLAG_T24()
        {
            performTest("FLAG_T24.TIF");
        }

        [Test]
        public void test_G31D()
        {
            performTest("G31D.TIF");
        }

        [Test]
        public void test_G31DS()
        {
            performTest("G31DS.TIF");
        }

        [Test]
        public void test_G32D()
        {
            performTest("G32D.TIF");
        }

        [Test]
        public void test_G32DS()
        {
            performTest("G32DS.TIF");
        }

        [Test]
        public void test_g3test()
        {
            performTest("g3test.tif");
        }

        [Test]
        public void test_G4()
        {
            performTest("G4.TIF");
        }

        [Test]
        public void test_G4S()
        {
            performTest("G4S.TIF");
        }

        [Test]
        public void test_GMARBLES()
        {
            performTest("GMARBLES.TIF");
        }

        [Test]
        public void test_gray8_lzw_mac()
        {
            performTest("gray8-lzw-mac.tif");
        }

        [Test]
        public void test_gray8_packbits_be()
        {
            performTest("gray8-packbits-be.tif");
        }

        [Test]
        public void test_gray8_packbits_le()
        {
            performTest("gray8-packbits-le.tif");
        }

        [Test]
        public void test_gray8_zip_pc()
        {
            performTest("gray8-zip-pc.tif");
        }

        [Test]
        public void test_jello()
        {
            performTest("jello.tif");
        }

        [Test]
        public void test_jim___ah()
        {
            performTest("jim___ah.tif");
        }

        [Test]
        public void test_jim___cg()
        {
            performTest("jim___cg.tif");
        }

        [Test]
        public void test_jim___dg()
        {
            performTest("jim___dg.tif");
        }

        [Test]
        public void test_jim___gg()
        {
            performTest("jim___gg.tif");
        }

        [Test]
        public void test_lab8_lzw()
        {
            performTest("lab8-lzw.tif");
        }

        [Test]
        public void test_MARBIBM()
        {
            performTest("MARBIBM.TIF");
        }

        [Test]
        public void test_MARBLES()
        {
            performTest("MARBLES.TIF");
        }

        [Test]
        public void test_oxford()
        {
            performTest("oxford.tif");
        }

        [Test]
        public void test_pc260001()
        {
            performTest("pc260001.tif");
        }

        [Test]
        public void test_quad_jpeg()
        {
            performTest("quad-jpeg.tif");
        }

        [Test]
        public void test_quad_lzw()
        {
            performTest("quad-lzw.tif");
        }

        [Test]
        public void test_quad_tile()
        {
            performTest("quad-tile.tif");
        }

        [Test]
        public void test_rgb8_jpeg_RGB()
        {
            performTest("rgb8-jpeg-RGB.tif");
        }

        [Test]
        public void test_rgb8_jpeg_YCrCb()
        {
            performTest("rgb8-jpeg-YCrCb.tif");
        }

        [Test]
        public void test_rgb8_lsb2msb()
        {
            performTest("rgb8-lsb2msb.tif");
        }

        [Test]
        public void test_rgb8_lzw_mac()
        {
            performTest("rgb8-lzw-mac.tif");
        }

        [Test]
        public void test_rgb8_lzw_pc()
        {
            performTest("rgb8-lzw-pc.tif");
        }

        [Test]
        public void test_rgb8_msb2lsb()
        {
            performTest("rgb8-msb2lsb.tif");
        }

        [Test]
        public void test_rgb8_separate()
        {
            performTest("rgb8-separate.tif");
        }

        [Test]
        public void test_rgb8_zip_mac()
        {
            performTest("rgb8-zip-mac.tif");
        }

        [Test]
        public void test_rgb8_zip_pc()
        {
            performTest("rgb8-zip-pc.tif");
        }

        [Test]
        public void test_strike()
        {
            performTest("strike.tif");
        }

        [Test]
        public void test_XING_T24()
        {
            performTest("XING_T24.TIF");
        }

        [Test]
        public void test_ycbcr_cat()
        {
            performTest("ycbcr-cat.tif");
        }
    }
}
