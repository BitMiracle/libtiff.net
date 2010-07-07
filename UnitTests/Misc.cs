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
            Tiff tiff = Tiff.Open(fn, "r");

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
}
