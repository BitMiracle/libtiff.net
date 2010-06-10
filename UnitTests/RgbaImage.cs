using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;
using BitMiracle.LibTiff.Classic;

namespace UnitTests
{
    [TestFixture]
    class RgbaImage
    {
        private static string[] Files
        {
            get
            {
                return new string[]
                {
                    "CCITT_1.TIF",
                    "CCITT_2.TIF",
                    "CCITT_3.TIF",
                    "CCITT_4.TIF",
                    "CCITT_5.TIF",
                    "CCITT_6.TIF",
                    "CCITT_7.TIF",
                    "CCITT_8.TIF",
                    "FLAG_T24.TIF",
                    "G31D.TIF",
                    "G31DS.TIF",
                    "G32D.TIF",
                    "G32DS.TIF",
                    "G4.TIF",
                    "G4S.TIF",
                    "GMARBLES.TIF",
                    "MARBIBM.TIF",
                    "MARBLES.TIF",
                    "XING_T24.TIF",
                    "cramps-tile.tif",
                    "cramps.tif",
                    "flower-minisblack-02.tif",
                    "flower-minisblack-04.tif",
                    "flower-minisblack-08.tif",
                    "flower-minisblack-16.tif",
                    "flower-palette-02.tif",
                    "flower-palette-04.tif",
                    "flower-palette-08.tif",
                    "flower-rgb-contig-08.tif",
                    "flower-rgb-contig-16.tif",
                    "flower-rgb-planar-08.tif",
                    "flower-rgb-planar-16.tif",
                    "flower-separated-contig-08.tif",
                    "dscf0013.tif",
                    "fax2d.tif",
                    "g3test.tif",
                    "jello.tif",
                    "jim___ah.tif",
                    "jim___cg.tif",
                    "jim___dg.tif",
                    "jim___gg.tif",
                    "ladoga.tif",
                    "oxford.tif",
                    "pc260001.tif",
                    "quad-jpeg.tif",
                    "quad-lzw.tif",
                    "quad-tile.tif",
                    "strike.tif",
                    "ycbcr-cat.tif",
                };
            }
        }

        private static bool tryReadRGBAImage(string file)
        {
            Tiff tiff = Tiff.Open(file, "r");
            int w = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int h = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            int[] raster = new int[w * h];
            return tiff.ReadRGBAImage(w, h, raster);
        }

        [Test, TestCaseSource("Files")]
        public void TestReadRGBAImage(string file)
        {
            string fullPath = Path.Combine(TestCase.Folder, file);
            bool ok = tryReadRGBAImage(fullPath);
            Assert.True(ok);
        }
    }
}
