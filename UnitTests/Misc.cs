using System;
using System.IO;

using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class Misc
    {
        [Test]
        public void TestReadUndefinedType()
        {
            string fn = Path.Combine(TestCase.Folder, "pc260001.tif");
            using (Tiff tiff = Tiff.Open(fn, "r"))
            {
                int customValueCount = tiff.GetTagListCount();
                Assert.AreEqual(customValueCount, 7);


                TiffTag tag = (TiffTag)tiff.GetTagListEntry(6);
                FieldValue[] fieldValues = tiff.GetField(tag);


                int actualLength = fieldValues[0].ToInt();
                byte[] actualData = fieldValues[1].GetBytes();
                string actualDataBase64 = Convert.ToBase64String(actualData);

                int expectedLength = 260;
                string expectedDataBase64 = "UHJpbnRJTQAwMjUwAAAUAAEAFAAUAAIAAQAAAAMAiAAAAAcAAAAAAAgAAAAAAAkAAAAAAAoAAAAAAAsA0AAAAAwAAAAAAA0AAAAAAA4A6AAAAAABAQAAAAEB/wAAAAIBgwAAAAMBgwAAAAQBgwAAAAUBgwAAAAYBgwAAAAcBgICAABABggAAAAkRAAAQJwAACw8AABAnAACXBQAAECcAALAIAAAQJwAAARwAABAnAABeAgAAECcAAIsAAAAQJwAAywMAABAnAADlGwAAECcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABwAmoIFAAEAAADiAwAAnYI=";

                Assert.AreEqual(expectedLength, actualLength);
                Assert.AreEqual(expectedDataBase64, actualDataBase64);
            }
        }

        [Test]
		[Ignore]
        public void TestWriteCustomDirectory()
        {
            using (Tiff image = Tiff.Open("TestWriteCustomDirectory.tif", "w"))
            {
                Assert.IsNotNull(image);

                image.SetField(TiffTag.IMAGEWIDTH, 256);
                image.SetField(TiffTag.IMAGELENGTH, 256);
                image.SetField(TiffTag.BITSPERSAMPLE, 8);
                image.SetField(TiffTag.SAMPLESPERPIXEL, 3);
                image.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                image.SetField(TiffTag.ROWSPERSTRIP, 1);

                long offset;
                bool written = image.WriteCustomDirectory(out offset);
                Assert.IsTrue(written);

                byte[] color_ptr = new byte[256 * 3];
                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < 256; j++)
                    {
                        color_ptr[j * 3 + 0] = (byte)i;
                        color_ptr[j * 3 + 1] = (byte)i;
                        color_ptr[j * 3 + 2] = (byte)i;
                    }
                    image.WriteScanline(color_ptr, i);
                }
            }
        }
    }
}
