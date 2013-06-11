using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class CustomTags
    {
        private const TiffTag TIFFTAG_ASCIITAG = (TiffTag)666;
        private const TiffTag TIFFTAG_SHORTTAG = (TiffTag)667;
        private const TiffTag TIFFTAG_LONGTAG = (TiffTag)668;
        private const TiffTag TIFFTAG_RATIONALTAG = (TiffTag)669;
        private const TiffTag TIFFTAG_FLOATTAG = (TiffTag)670;
        private const TiffTag TIFFTAG_DOUBLETAG = (TiffTag)671;
        private const TiffTag TIFFTAG_BYTETAG = (TiffTag)672;
        private const TiffTag TIFFTAG_IFDTAG = (TiffTag)673;

        private Tiff.TiffExtendProc m_parentExtender;

        public void TagExtender(Tiff tif)
        {
            TiffFieldInfo[] tiffFieldInfo = 
            {
                new TiffFieldInfo(TIFFTAG_ASCIITAG, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, "MyTag"),
                new TiffFieldInfo(TIFFTAG_SHORTTAG, 2, 2, TiffType.SHORT, FieldBit.Custom, false, true, "ShortTag"),
                new TiffFieldInfo(TIFFTAG_LONGTAG, 2, 2, TiffType.LONG, FieldBit.Custom, false, true, "LongTag"),
                new TiffFieldInfo(TIFFTAG_RATIONALTAG, 2, 2, TiffType.RATIONAL, FieldBit.Custom, false, true, "RationalTag"),
                new TiffFieldInfo(TIFFTAG_FLOATTAG, 2, 2, TiffType.FLOAT, FieldBit.Custom, false, true, "FloatTag"),
                new TiffFieldInfo(TIFFTAG_DOUBLETAG, 2, 2, TiffType.DOUBLE, FieldBit.Custom, false, true, "DoubleTag"),
                new TiffFieldInfo(TIFFTAG_BYTETAG, 2, 2, TiffType.BYTE, FieldBit.Custom, false, true, "ByteTag"),
                new TiffFieldInfo(TIFFTAG_IFDTAG, 1, 1, TiffType.IFD, FieldBit.Custom, false, false, "IfdTag"),
            };

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);

            if (m_parentExtender != null)
                m_parentExtender(tif);
        }

        [Test]
        public void ReadWriteCustomTags()
        {
            // Define an image
            byte[] buffer = new byte[25 * 144];

            // Register the custom tag handler
            Tiff.TiffExtendProc extender = TagExtender;
            m_parentExtender = Tiff.SetTagExtender(extender);

            string outputFileName = "output.tif";
            Tiff image = Tiff.Open(outputFileName, "w");

            // We need to set some values for basic tags before we can add any data
            image.SetField(TiffTag.IMAGEWIDTH, 25 * 8);
            image.SetField(TiffTag.IMAGELENGTH, 144);
            image.SetField(TiffTag.BITSPERSAMPLE, 1);
            image.SetField(TiffTag.SAMPLESPERPIXEL, 1);
            image.SetField(TiffTag.ROWSPERSTRIP, 144);

            image.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4);
            image.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISWHITE);
            image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
            image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            
            image.SetField(TiffTag.XRESOLUTION, 150.0);
            image.SetField(TiffTag.YRESOLUTION, 150.0);
            image.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);

            // set custom tags
            
            string value = "Tag contents";
            image.SetField(TIFFTAG_ASCIITAG, value);

            short[] shorts = { 263, 264 };
            image.SetField(TIFFTAG_SHORTTAG, 2, shorts);
            
            int[] longs = { 117, 118 };
            image.SetField(TIFFTAG_LONGTAG, 2, longs);
            
            float[] rationals = { 0.333333f, 0.444444f };
            image.SetField(TIFFTAG_RATIONALTAG, 2, rationals);

            float[] floats = { 0.666666f, 0.777777f };
            image.SetField(TIFFTAG_FLOATTAG, 2, floats);
            
            double[] doubles = { 0.1234567, 0.7654321 };
            image.SetField(TIFFTAG_DOUBLETAG, 2, doubles);
            
            byte[] bytes = { 89, 90 };
            image.SetField(TIFFTAG_BYTETAG, 2, bytes);

            int ifd_offset = 1234567890;
            image.SetField(TIFFTAG_IFDTAG, ifd_offset);

            // Write the information to the file
            image.WriteEncodedStrip(0, buffer, 25 * 144);

            // Close the file
            image.Dispose();

            // Now open that TIFF back and read new tags
            image = Tiff.Open(outputFileName, "r");
            FieldValue[] res = image.GetField(TIFFTAG_ASCIITAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(1, res.Length);
            Assert.AreEqual(value, res[0].ToString());

            res = image.GetField(TIFFTAG_SHORTTAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(shorts, res[1].ToShortArray());

            res = image.GetField(TIFFTAG_LONGTAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(longs, res[1].ToIntArray());

            res = image.GetField(TIFFTAG_RATIONALTAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(rationals, res[1].ToFloatArray());

            res = image.GetField(TIFFTAG_FLOATTAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(floats, res[1].ToFloatArray());

            res = image.GetField(TIFFTAG_DOUBLETAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(doubles, res[1].ToDoubleArray());

            res = image.GetField(TIFFTAG_BYTETAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(bytes, res[1].ToByteArray());

            res = image.GetField(TIFFTAG_IFDTAG);
            Assert.IsNotNull(res);
            Assert.AreEqual(1, res.Length);
            Assert.AreEqual(ifd_offset, res[0].ToInt());

            image.Dispose();

            Tiff.SetTagExtender(m_parentExtender);
        }

        [Test]
        public void ReadExifTags()
        {
            string path = System.IO.Path.Combine(TestCase.Folder, "dscf0013.tif");
            using (Tiff image = Tiff.Open(path, "r"))
            {
                FieldValue[] exifIfd = image.GetField(TiffTag.EXIFIFD);
                Assert.IsNotNull(exifIfd);
                Assert.That(exifIfd.Length, Is.EqualTo(1));

                int exifIFDOffset = exifIfd[0].ToInt();
                Assert.That(exifIFDOffset, Is.EqualTo(640));

                bool readSuccessful = image.ReadEXIFDirectory(exifIFDOffset);
                Assert.That(readSuccessful);

                FieldValue[] fnumber = image.GetField(TiffTag.EXIF_FNUMBER);
                Assert.IsNotNull(fnumber);
                Assert.That(fnumber.Length, Is.EqualTo(1));
                Assert.AreEqual(3.4, fnumber[0].ToDouble(), 0.001);

                FieldValue[] exposureProgram = image.GetField(TiffTag.EXIF_EXPOSUREPROGRAM);
                Assert.IsNotNull(exposureProgram);
                Assert.That(exposureProgram.Length, Is.EqualTo(1));
                Assert.That(exposureProgram[0].ToString(), Is.EqualTo("2"));

                FieldValue[] dateTimeOriginal = image.GetField(TiffTag.EXIF_DATETIMEORIGINAL);
                Assert.IsNotNull(dateTimeOriginal);
                Assert.That(dateTimeOriginal.Length, Is.EqualTo(1));
                //The format is "YYYY:MM:DD HH:MM:SS", see DateTimeOriginal tag description
                Assert.That(dateTimeOriginal[0].ToString(), Is.EqualTo("2004:11:10 00:00:31"));

                FieldValue[] exifVersion = image.GetField(TiffTag.EXIF_EXIFVERSION);
                Assert.IsNotNull(exifVersion);
                Assert.That(exifVersion.Length, Is.EqualTo(1));
                Assert.That(exifVersion[0].ToString(), Is.EqualTo("0210"));

                FieldValue[] fileSource = image.GetField(TiffTag.EXIF_FILESOURCE);
                Assert.IsNotNull(fileSource);
                Assert.That(fileSource.Length, Is.EqualTo(1));
                Assert.That(fileSource[0].ToByte(), Is.EqualTo(3));

                FieldValue[] sceneType = image.GetField(TiffTag.EXIF_SCENETYPE);
                Assert.IsNotNull(sceneType);
                Assert.That(sceneType.Length, Is.EqualTo(1));
                Assert.That(sceneType[0].ToByte(), Is.EqualTo(1));
            }
        }
    }
}
