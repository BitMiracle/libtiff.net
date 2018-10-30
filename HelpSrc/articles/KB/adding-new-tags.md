LibTiff.Net has built-in knowledge of all the standard TIFF tags, as well as extentions. The following describes how application specific tags can be used by applications without modifying LibTiff.Net. 

TiffFieldInfo
-------------

How LibTiff.Net manages specific tags is primarily controlled by the definition for that tag value stored internally as a TiffFieldInfo structure. Please consult documetation for <xref:BitMiracle.LibTiff.Classic.TiffFieldInfo> for more details about members of this structure. 

A TiffFieldInfo definition exists for each built-in tag in the Tiff_DirInfo.cs file. Some tags which support multiple data types have more than one definition, one per data type supported. 

Two methods exist for getting the internal TiffFieldInfo definitions: <xref:BitMiracle.LibTiff.Classic.Tiff.FindFieldInfo(BitMiracle.LibTiff.Classic.TiffTag,BitMiracle.LibTiff.Classic.TiffType)> and <xref:BitMiracle.LibTiff.Classic.Tiff.FindFieldInfoByName(System.String,BitMiracle.LibTiff.Classic.TiffType)>. 

Default Tag Auto-registration
-----------------------------

LibTiff.Net reads unrecognised tags automatically. When an unknown tags is encountered, it is automatically internally defined with a default name and a type derived from the tag value in the file. Applications only need to predefine application specific tags if they need to be able to set them in a file, or if particular calling conventions are desired for <xref:BitMiracle.LibTiff.Classic.Tiff.GetField(BitMiracle.LibTiff.Classic.TiffTag)> and <xref:BitMiracle.LibTiff.Classic.Tiff.SetField(BitMiracle.LibTiff.Classic.TiffTag,System.Object[])>. 

When tags are autodefined like this the <xref:BitMiracle.LibTiff.Classic.TiffFieldInfo.ReadCount> and <xref:BitMiracle.LibTiff.Classic.TiffFieldInfo.WriteCount> are always <xref:BitMiracle.LibTiff.Classic.TiffFieldInfo.Variable2>. The <xref:BitMiracle.LibTiff.Classic.TiffFieldInfo.PassCount> is always true, and the <xref:BitMiracle.LibTiff.Classic.TiffFieldInfo.Bit> is <xref:BitMiracle.LibTiff.Classic.FieldBit.Custom>. The field name will be "Tag {0}" where the {0} is the tag number. 

Defining Application Tags
-------------------------

For various reasons, it is common for applications to want to define their own tags to store information outside the core TIFF specification. This is done by calling <xref:BitMiracle.LibTiff.Classic.Tiff.MergeFieldInfo(BitMiracle.LibTiff.Classic.TiffFieldInfo[],System.Int32)> with one or more TiffFieldInfo objects. 

The tags need to be defined for each TIFF file opened - and when reading they should be defined before the tags of the file are read, yet a valid TIFF object is needed to merge the tags against. In order to get them registered at the appropriate part of the setup process, it is necessary to register our merge function as an extender callback with LibTiff.Net. This is done with <xref:BitMiracle.LibTiff.Classic.Tiff.SetTagExtender(BitMiracle.LibTiff.Classic.Tiff.TiffExtendProc)>. It's a good idea to keep track of the previous tag extender (if any) so that we can call it from our extender allowing a chain of customizations to take effect. 

The whole process is performed in following sample: 

```cs
using BitMiracle.LibTiff.Classic;
using System.Diagnostics;

namespace ReadWriteCustomTags
{
    class Program
    {
        private const TiffTag TIFFTAG_ASCIITAG = (TiffTag)666;
        private const TiffTag TIFFTAG_LONGTAG = (TiffTag)667;
        private const TiffTag TIFFTAG_SHORTTAG = (TiffTag)668;
        private const TiffTag TIFFTAG_RATIONALTAG = (TiffTag)669;
        private const TiffTag TIFFTAG_FLOATTAG = (TiffTag)670;
        private const TiffTag TIFFTAG_DOUBLETAG = (TiffTag)671;
        private const TiffTag TIFFTAG_BYTE = (TiffTag)672;

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
                new TiffFieldInfo(TIFFTAG_BYTE, 2, 2, TiffType.BYTE, FieldBit.Custom, false, true, "ByteTag"),
            };

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);

            if (m_parentExtender != null)
                m_parentExtender(tif);
        }

        static void Main(string[] args)
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
            image.SetField(TIFFTAG_BYTE, 2, bytes);

            // Write the information to the file
            image.WriteEncodedStrip(0, buffer, 25 * 144);

            // Close the file
            image.Dispose();

            // Now open that TIFF back and read new tags
            image = Tiff.Open(outputFileName, "r");
            FieldValue[] res = image.GetField(TIFFTAG_ASCIITAG);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 1);
            Debug.Assert(res[0].ToString() == value);

            res = image.GetField(TIFFTAG_SHORTTAG);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 2);
            Debug.Assert(res[0].ToInt() == 2);
            Debug.Assert(res[1].ToShortArray() != null);

            res = image.GetField(TIFFTAG_LONGTAG);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 2);
            Debug.Assert(res[0].ToInt() == 2);
            Debug.Assert(res[1].ToIntArray() != null);

            res = image.GetField(TIFFTAG_RATIONALTAG);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 2);
            Debug.Assert(res[0].ToInt() == 2);
            Debug.Assert(res[1].ToFloatArray() != null);

            res = image.GetField(TIFFTAG_FLOATTAG);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 2);
            Debug.Assert(res[0].ToInt() == 2);
            Debug.Assert(res[1].ToFloatArray() != null);

            res = image.GetField(TIFFTAG_DOUBLETAG);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 2);
            Debug.Assert(res[0].ToInt() == 2);
            Debug.Assert(res[1].ToDoubleArray() != null);

            res = image.GetField(TIFFTAG_BYTE);
            Debug.Assert(res != null);
            Debug.Assert(res.Length == 2);
            Debug.Assert(res[0].ToInt() == 2);
            Debug.Assert(res[1].ToByteArray() != null);

            image.Dispose();
        }
    }
}
```
