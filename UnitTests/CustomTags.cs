using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class CustomTags
    {
        private const TiffTag TIFFTAG_ASCIITAG = (TiffTag)666;
        private const TiffTag TIFFTAG_LONGTAG = (TiffTag)667;
        private const TiffTag TIFFTAG_SHORTTAG = (TiffTag)668;
        private const TiffTag TIFFTAG_RATIONALTAG = (TiffTag)669;
        private const TiffTag TIFFTAG_FLOATTAG = (TiffTag)670;
        private const TiffTag TIFFTAG_DOUBLETAG = (TiffTag)671;
        private const TiffTag TIFFTAG_BYTE = (TiffTag)672;

        public static void TagExtender(Tiff tif)
        {
            TiffFieldInfo[] tiffFieldInfo = 
            {
                new TiffFieldInfo(TIFFTAG_ASCIITAG, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, "MyTag"),
                new TiffFieldInfo(TIFFTAG_SHORTTAG, 2, 2, TiffType.SHORT, FieldBit.Custom, false, true, "ShortTag"),
                new TiffFieldInfo(TIFFTAG_LONGTAG, 2, 2, TiffType.LONG, FieldBit.Custom, false, true, "LongTag"),
                new TiffFieldInfo(TIFFTAG_RATIONALTAG, 2, 2, TiffType.RATIONAL, FieldBit.Custom, false, true, "RationalTag"),
                new TiffFieldInfo(TIFFTAG_FLOATTAG, 2, 2, TiffType.FLOAT, FieldBit.Custom, false, true, "FloatTag"),
                new TiffFieldInfo(TIFFTAG_DOUBLETAG, 2, 2, TiffType.DOUBLE, FieldBit.Custom, false, true, "DoubleTag"),
                new TiffFieldInfo(TIFFTAG_BYTE, 2, 2, TiffType.BYTE, FieldBit.Custom, false, true, "ByteTag"),
            };

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);
        }

        [Test]
        public void ReadWriteCustomTags()
        {
            // Define an image
            byte[] buffer = new byte[25 * 144];

            // Register the custom tag handler
            Tiff.TiffExtendProc extender = TagExtender;
            Tiff.SetTagExtender(extender);

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
            image.SetField(TIFFTAG_BYTE, 2, bytes);

            // Write the information to the file
            image.WriteEncodedStrip(0, buffer, 25 * 144);
            image.WriteDirectory();

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

            res = image.GetField(TIFFTAG_BYTE);
            Assert.IsNotNull(res);
            Assert.AreEqual(2, res.Length);
            Assert.AreEqual(2, res[0].ToInt());
            Assert.AreEqual(bytes, res[1].ToByteArray());

            image.Dispose();
        }
    }
}
