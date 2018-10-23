using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ReadWriteCustomTags
    {
        private const TiffTag TIFFTAG_ASCIITAG = (TiffTag)666;
        private const TiffTag TIFFTAG_LONGTAG = (TiffTag)667;
        private const TiffTag TIFFTAG_SHORTTAG = (TiffTag)668;
        private const TiffTag TIFFTAG_RATIONALTAG = (TiffTag)669;
        private const TiffTag TIFFTAG_FLOATTAG = (TiffTag)670;
        private const TiffTag TIFFTAG_DOUBLETAG = (TiffTag)671;
        private const TiffTag TIFFTAG_BYTETAG = (TiffTag)672;

        private static Tiff.TiffExtendProc m_parentExtender;

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
                new TiffFieldInfo(TIFFTAG_BYTETAG, 2, 2, TiffType.BYTE, FieldBit.Custom, false, true, "ByteTag"),
            };

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);

            if (m_parentExtender != null)
                m_parentExtender(tif);
        }

        public static void Main()
        {
            // Define an image
            byte[] buffer = new byte[25 * 144];

            // Register the extender callback
            // It's a good idea to keep track of the previous tag extender (if any) so that we can call it
            // from our extender allowing a chain of customizations to take effect.
            m_parentExtender = Tiff.SetTagExtender(TagExtender);

            string outputFileName = writeTiffWithCustomTags(buffer);
            readCustomTags(outputFileName);
            
            // restore previous tag extender
            Tiff.SetTagExtender(m_parentExtender);
        }

        private static string writeTiffWithCustomTags(byte[] buffer)
        {
            string outputFileName = "output.tif";
            using (Tiff image = Tiff.Open(outputFileName, "w"))
            {
                // set up some basic tags before adding data
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

                // Write the information to the file
                image.WriteEncodedStrip(0, buffer, 25 * 144);
            }
            return outputFileName;
        }

        private static void readCustomTags(string outputFileName)
        {
            const string messageFormat = "{0} is read ok: {1}\n";
            StringBuilder result = new StringBuilder();

            // Now open that TIFF back and read new tags
            using (Tiff image = Tiff.Open(outputFileName, "r"))
            {
                FieldValue[] res = image.GetField(TIFFTAG_ASCIITAG);
                bool tagOk = (res != null && res.Length == 1 && res[0].ToString() == "Tag contents");
                result.AppendFormat(messageFormat, "MyTag", tagOk);

                res = image.GetField(TIFFTAG_SHORTTAG);
                tagOk = (res != null && res.Length == 2 && res[0].ToInt() == 2 && res[1].ToShortArray() != null);
                result.AppendFormat(messageFormat, "ShortTag", tagOk);

                res = image.GetField(TIFFTAG_LONGTAG);
                tagOk = (res != null && res.Length == 2 && res[0].ToInt() == 2 && res[1].ToIntArray() != null);
                result.AppendFormat(messageFormat, "LongTag", tagOk);

                res = image.GetField(TIFFTAG_RATIONALTAG);
                tagOk = (res != null && res.Length == 2 && res[0].ToInt() == 2 && res[1].ToFloatArray() != null);
                result.AppendFormat(messageFormat, "RationalTag", tagOk);

                res = image.GetField(TIFFTAG_FLOATTAG);
                tagOk = (res != null && res.Length == 2 && res[0].ToInt() == 2 && res[1].ToFloatArray() != null);
                result.AppendFormat(messageFormat, "FloatTag", tagOk);

                res = image.GetField(TIFFTAG_DOUBLETAG);
                tagOk = (res != null && res.Length == 2 && res[0].ToInt() == 2 && res[1].ToFloatArray() != null);
                result.AppendFormat(messageFormat, "DoubleTag", tagOk);

                res = image.GetField(TIFFTAG_BYTETAG);
                tagOk = (res != null && res.Length == 2 && res[0].ToInt() == 2 && res[1].ToByteArray() != null);
                result.AppendFormat(messageFormat, "ByteTag", tagOk);
            }

            MessageBox.Show(result.ToString());
        }
    }
}
