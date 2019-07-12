using System;
using System.IO;

using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class Misc
    {
        private static int TagToWrite = 0;

        [TestCase("StripoffsetsAboveShortMaxValue.tif")]
        public void TestStripOffsetsArePositive(string name)
        {
            string fn = Path.Combine(TestCase.Folder, name);
            using (Tiff tiff = Tiff.Open(fn, "r"))
            {
                FieldValue[] fieldValues = tiff.GetField(TiffTag.STRIPOFFSETS);
                long[] offsets = fieldValues[0].TolongArray();
                
                foreach (long offset in offsets)
                    Assert.GreaterOrEqual(offset, 0);
            }
        }

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

        [TestCase("B00005-no-100.tif")]
        [TestCase("16-bit-4-Band.tiff")]
        public void TestReadMinMaxSampleValues(string name)
        {
            string fn = Path.Combine(TestCase.Folder, name);
            using (Tiff tiff = Tiff.Open(fn, "r"))
            {
                FieldValue[] fieldValues = tiff.GetField(TiffTag.MINSAMPLEVALUE);
                uint min = fieldValues[0].ToUInt();
                Assert.AreEqual(0, min);

                fieldValues = tiff.GetField(TiffTag.MAXSAMPLEVALUE);
                uint max = (uint)fieldValues[0].ToInt();
                Assert.AreEqual(65535, max);
            }
        }

        [Test]
        public void TestReadTileOJpeg()
        {
            string fn = Path.Combine(TestCase.Folder, "zackthecat.tif");
            using (Tiff tiff = Tiff.Open(fn, "r"))
            {
                long size = tiff.TileSize();
                byte[] buffer = new byte[size];
                int read = tiff.ReadTile(buffer, 0, 0, 0, 0, 0);
                Assert.AreNotEqual(-1, read);
            }
        }

        [Test]
        public void TestReadAndIgnoreTags()
        {
            string fn = Path.Combine(TestCase.Folder, "doc-1211625-8-1.tiff");
            using (Tiff tiff = Tiff.Open(fn, "r"))
            {
                Assert.IsNotNull(tiff);
            }
        }

        [Test]        
        public void TestReadLoopedTiff()
        {
            string fn = Path.Combine(TestCase.Folder, "loop.tif");
            using (Tiff tiff = Tiff.Open(fn, "r"))
            {
                Assert.That(() => tiff.NumberOfDirectories(),
                    Throws.Exception.TypeOf<InvalidDataException>());
            }
        }

        [Test]
        public void TestAppendedNulls()
        {
            // A test to see if nulls get appended to custom ascii tags 
            // when they don't have a valid FieldInfo object registered.
            // Contributed by Alex J. Kennedy

            int freetag = 42117;
            int freetag2 = 42118;
            string value = "abc";
            string value2 = "def";
            byte[] expectedReadValue = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'\0' };

            string filename = "testAppendedNulls.tif";

            // Clean
            if (File.Exists(filename))
                File.Delete(filename);

            // Create a basic tiff image
            CreateBasicTif(filename);

            // Register the new tag
            TagToWrite = freetag;
            Tiff.SetTagExtender(TagExtender);

            using (Tiff image = Tiff.Open(filename, "a"))
            {
                image.SetDirectory(0);

                // write "abc" to directory 0, tag 42117
                image.SetField((TiffTag)freetag, value);

                // save        
                image.CheckpointDirectory();
            }

            // Reset TagExtender
            Tiff.SetTagExtender(null);

            // Try to read it back in
            using (Tiff image = Tiff.Open(filename, "r"))
            {
                image.SetDirectory(0);

                FieldValue[] fv = image.GetField((TiffTag)freetag);
                Assert.AreEqual(2, fv.Length);

                // Ensure that a null was appended.
                byte[] readValue = (byte[])(fv[1].Value);
                Assert.AreEqual(4, (int)fv[0].Value);
                Assert.AreEqual(4, readValue.Length);
                Assert.AreEqual('a', readValue[0]);
                Assert.AreEqual('b', readValue[1]);
                Assert.AreEqual('c', readValue[2]);
                Assert.AreEqual('\0', readValue[3]);
            }

            // Now write a different tag to see if it appends a null char to 42117
            TagToWrite = freetag2;
            Tiff.SetTagExtender(TagExtender);

            using (Tiff image = Tiff.Open(filename, "a"))
            {
                image.SetDirectory(0);

                // write "def" to directory 0, tag 42118
                image.SetField((TiffTag)freetag2, value2);

                // save        
                image.CheckpointDirectory();
            }

            Tiff.SetTagExtender(null);

            // Read in tag 42117 to make sure it doesn't have an extra null appended
            using (Tiff image = Tiff.Open(filename, "r"))
            {
                image.SetDirectory(0);

                TiffFieldInfo tfi = image.FindFieldInfo((TiffTag)freetag, TiffType.ANY);
                TiffFieldInfo tfi2 = image.FindFieldInfo((TiffTag)freetag2, TiffType.ANY);

                FieldValue[] fv = image.GetField((TiffTag)freetag);
                Assert.AreEqual(2, fv.Length);

                // Ensure that only one null was appended.
                byte[] readValue = (byte[])(fv[1].Value);
                Assert.AreEqual(4, (int)fv[0].Value);
                Assert.AreEqual(4, readValue.Length);
                Assert.AreEqual('a', readValue[0]);
                Assert.AreEqual('b', readValue[1]);
                Assert.AreEqual('c', readValue[2]);
                Assert.AreEqual('\0', readValue[3]);
            }
        }

        // Helper methods
        private static void TagExtender(Tiff tif)
        {
            TiffFieldInfo[] tiffFieldInfo = new TiffFieldInfo[1];
            tiffFieldInfo[0] = new TiffFieldInfo((TiffTag)TagToWrite, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, "Tag." + TagToWrite);

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);
        }

        private static void CreateBasicTif(string filename)
        {
            // Open the file
            using (Tiff tiffImage = Tiff.Open(filename, "w"))
            {
                if (tiffImage == null)
                    throw new Exception("Could not create " + filename);

                int len = 128;

                // Basic params
                tiffImage.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP);
                tiffImage.SetField(TiffTag.IMAGEWIDTH, len);
                tiffImage.SetField(TiffTag.IMAGELENGTH, len);
                tiffImage.SetField(TiffTag.TILEWIDTH, len);
                tiffImage.SetField(TiffTag.TILELENGTH, len);
                tiffImage.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                tiffImage.SetField(TiffTag.BITSPERSAMPLE, 32);
                tiffImage.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tiffImage.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                tiffImage.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                tiffImage.SetField(TiffTag.COMPRESSION, Compression.ADOBE_DEFLATE);
                tiffImage.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                // write tile
                float[,] arr = new float[len, len];
                for (int i = 0; i < len; i++)
                    for (int j = 0; j < len; j++)
                        arr[i, j] = i + j;

                byte[] byteData = new byte[arr.Length * sizeof(float)];
                Buffer.BlockCopy(arr, 0, byteData, 0, byteData.Length);

                tiffImage.WriteEncodedTile(0, byteData, byteData.Length);
            }
        }
    }
}
