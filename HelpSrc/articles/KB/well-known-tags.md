The tags understood by LibTiff.Net, the number of parameter values, and the types for the values returned by <xref:BitMiracle.LibTiff.Classic.Tiff.GetField(BitMiracle.LibTiff.Classic.TiffTag)> and <xref:BitMiracle.LibTiff.Classic.Tiff.GetFieldDefaulted(BitMiracle.LibTiff.Classic.TiffTag)> are shown below. 

The data types correspond to the types used to specify tag values to <xref:BitMiracle.LibTiff.Classic.Tiff.SetField(BitMiracle.LibTiff.Classic.TiffTag,System.Object[])>. Consult the TIFF specification (or relevant industry specification) for information on the meaning of each tag and their possible values. 

|Tag|Value count|Type(s)|Notes|
|---|---|---|---|
|TiffTag.ARTIST|1|System.Byte[]|
|TiffTag.BADFAXLINES|1|System.Int32|
|TiffTag.BITSPERSAMPLE|1|System.Int16|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.CLEANFAXDATA|1|<xref:BitMiracle.LibTiff.Classic.CleanFaxData>|
|TiffTag.COLORMAP|3|System.Int16[]|Each arrays contains (1 << BitsPerSample) elements|
|TiffTag.COMPRESSION|1|<xref:BitMiracle.LibTiff.Classic.Compression>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.CONSECUTIVEBADFAXLINES|1|System.Int32|
|TiffTag.COPYRIGHT|1|System.Byte[]|
|TiffTag.DATATYPE|1|System.Int16|
|TiffTag.DATETIME|1|System.Byte[]|
|TiffTag.DOCUMENTNAME|1|System.Byte[]|
|TiffTag.DOTRANGE|2|System.Int16|
|TiffTag.EXTRASAMPLES 2|System.Int16<br><xref:BitMiracle.LibTiff.Classic.ExtraSample>[]|count & types array<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.FAXFILLFUNC|1|<xref:BitMiracle.LibTiff.Classic.Tiff.FaxFillFunc>|G3/G4 compression pseudo-tag|
|TiffTag.FAXMODE|1|<xref:BitMiracle.LibTiff.Classic.FaxMode>|G3/G4 compression pseudo-tag<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.FILLORDER|1|<xref:BitMiracle.LibTiff.Classic.FillOrder>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.GROUP3OPTIONS|1|<xref:BitMiracle.LibTiff.Classic.Group3Opt>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.GROUP4OPTIONS|1|<xref:BitMiracle.LibTiff.Classic.Group3Opt>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.HALFTONEHINTS|2|System.Int16||
|TiffTag.HOSTCOMPUTER|1|System.Byte[]||
|TiffTag.ICCPROFILE|2|System.Int32<br>System.Byte[]|count, profile data.<br>The contents of this field is quite complex. See The ICC Profile Format Specification, Annex B.3 "Embedding ICC Profiles in TIFF Files" (available at http://www.color.org) for an explanation.|
|TiffTag.IMAGEDEPTH|1|System.Int32|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.IMAGEDESCRIPTION|1|System.Byte[]||
|TiffTag.IMAGELENGTH|1|System.Int32||
|TiffTag.IMAGEWIDTH|1|System.Int32|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.INKNAMES|1|System.Byte[]||
|TiffTag.INKSET|1|<xref:BitMiracle.LibTiff.Classic.InkSet>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.JPEGCOLORMODE|1|<xref:BitMiracle.LibTiff.Classic.JpegColorMode>|JPEG pseudo-tag<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.JPEGQUALITY|1|System.Int32|JPEG pseudo-tag|
|TiffTag.JPEGTABLES|2|System.Int32<br>System.Byte[]|count & tables<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.JPEGTABLESMODE|1|<xref:BitMiracle.LibTiff.Classic.JpegTablesMode>|JPEG pseudo-tag<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.MAKE|1|System.Byte[]||
|TiffTag.MATTEING|1|System.Int16|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.MAXSAMPLEVALUE|1|System.Int16||
|TiffTag.MINSAMPLEVALUE|1|System.Int16||
|TiffTag.MODEL|1|System.Byte[]||
|TiffTag.ORIENTATION|1|<xref:BitMiracle.LibTiff.Classic.Orientation>||
|TiffTag.PAGENAME|1|System.Byte[]||
|TiffTag.PAGENUMBER|2|System.Int16||
|TiffTag.PHOTOMETRIC|1|<xref:BitMiracle.LibTiff.Classic.Photometric>||
|TiffTag.PHOTOSHOP|2|System.Int32<br>System.Byte[]|count, data|
|TiffTag.PLANARCONFIG|1|<xref:BitMiracle.LibTiff.Classic.PlanarConfig>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.PREDICTOR|1|<xref:BitMiracle.LibTiff.Classic.Predictor>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.PRIMARYCHROMATICITIES|1|System.Single[]|The array contains 6 elements|
|TiffTag.REFERENCEBLACKWHITE|1|System.Single[]|The array contains (2 * SamplesPerPixel) elements<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.RESOLUTIONUNIT|1|<xref:BitMiracle.LibTiff.Classic.ResUnit>||
|TiffTag.RICHTIFFIPTC|2|System.Int32<br>System.Byte[]|count, data|
|TiffTag.ROWSPERSTRIP|1|System.Int32|must be > 0<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.SAMPLEFORMAT|1|<xref:BitMiracle.LibTiff.Classic.SampleFormat>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.SAMPLESPERPIXEL|1|System.Int16|must be <= 4<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.SMAXSAMPLEVALUE|1|System.Double||
|TiffTag.SMINSAMPLEVALUE|1|System.Double||
|TiffTag.SOFTWARE|1|System.Byte[]||
|TiffTag.STONITS|1|System.Double[]|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.STRIPBYTECOUNTS|1|System.UInt32[]||
|TiffTag.STRIPOFFSETS|1|System.UInt32[]||
|TiffTag.SUBFILETYPE|1|<xref:BitMiracle.LibTiff.Classic.FileType>||
|TiffTag.SUBIFD|2|System.Int16<br>System.Int32[]|count & offsets array|
|TiffTag.TARGETPRINTER|1|System.Byte[]||
|TiffTag.THRESHHOLDING|1|<xref:BitMiracle.LibTiff.Classic.Threshold>||
|TiffTag.TILEBYTECOUNTS|1|System.UInt32[]||
|TiffTag.TILEDEPTH|1|System.Int32|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.TILELENGTH|1|System.Int32|must be a multiple of 8<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.TILEOFFSETS|1|System.UInt32[]||
|TiffTag.TILEWIDTH|1|System.Int32|must be a multiple of 8<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.TRANSFERFUNCTION|1 or 3|System.Int16[]|Each array contains (1 << BitsPerSample) elements<br>If SamplesPerPixel is one, then a single array is used; otherwise three arrays are used.<br>GetField returns three arrays (last 2 arrays can be null).|
|TiffTag.WHITEPOINT|1|System.Single[]|The array contains 2 elements|
|TiffTag.XMLPACKET|2|System.Int32<br>System.Byte[]|count, data|
|TiffTag.XPOSITION|1|System.Single||
|TiffTag.XRESOLUTION|1|System.Single||
|TiffTag.YCBCRCOEFFICIENTS|1|System.Single[]|The array contains 3 elements<br>Tag may not have its values changed once data is written to file/stream.|
|TiffTag.YCBCRPOSITIONING|1|<xref:BitMiracle.LibTiff.Classic.YCbCrPosition>|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.YCBCRSUBSAMPLING|2|System.Int16|Tag may not have its values changed once data is written to file/stream.|
|TiffTag.YPOSITION|1|System.Single||
|TiffTag.YRESOLUTION|1|System.Single||

Auto-registered tags
--------------------

If you can’t find the tag in the table above that means this is unsupported tag. But you still be able to read it's value. You will need to know the data type of that tag to correctly interpret returned value(s), though. 

For example, if you want to read and print value(s) from the tag 50341 you can use the following code: 

```cs
using BitMiracle.LibTiff.Classic;

namespace ReadAndPrintAutoRegisteredTag
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Tiff image = Tiff.Open(args[0], "r"))
            {
                if (image == null)
                    return;

                // read auto-registered tag 50341
                FieldValue[] value = image.GetField((TiffTag)50341);
                System.Console.Out.WriteLine("Tag value(s) are as follows:");
                for (int i = 0; i < value.Length; i++)
                    System.Console.Out.WriteLine("{0} : {1}", i, value[i].ToString());
            }
        }
    }
}
```