Our priority was to introduce as little changes into LibTiff's API as possible. But, as you can imagine, some changes had to be made because of differences between C and C# languages and underlying class libraries. 

Also, we were unable to resist the temptation to rename some methods and types. 

LibTiff prefers unsigned integer types for parameters and return values. LibTiff.Net, by contrast, prefers signed integer types for parameters and return values. You can pass unsigned values or convert return values to unsigned types if you wish, but keep in mind, that it will affect performance (not seriously, though). 

General notes
-------------

**TIFF** struct changed it's name to <xref:BitMiracle.LibTiff.Classic.Tiff> and became full-featured class. 

In LibTiff most functions were accepting pointer to a TIFF struct as first parameter. LibTiff.NET does not need it because these functions are now methods of Tiff class. 

**TIFFCodec** struct changed it's name to <xref:BitMiracle.LibTiff.Classic.TiffCodec> and became full-featured class with virtual methods suitable for overloading. 

**TIFFTagMethods** struct changed it's name to <xref:BitMiracle.LibTiff.Classic.TiffTagMethods> and became full-featured class with virtual methods suitable for overloading. 

**TIFFRegisterCODEC** function changed it's name and signature. It is now known as <xref:BitMiracle.LibTiff.Classic.Tiff.RegisterCodec(BitMiracle.LibTiff.Classic.TiffCodec)> and gets an instance of class derived from TiffCodec. 

Custom error handling strategy changed. Now you should provide implementation of a class derived from <xref:BitMiracle.LibTiff.Classic.TiffErrorHandler> and pass it as parameter to <xref:BitMiracle.LibTiff.Classic.Tiff.SetErrorHandler(BitMiracle.LibTiff.Classic.TiffErrorHandler)> method. 

Custom file/stream handling strategy changed. Now you should provide implementation of a class derived from <xref:BitMiracle.LibTiff.Classic.TiffStream> and pass it as parameter to <xref:BitMiracle.LibTiff.Classic.Tiff.ClientOpen(System.String,System.String,System.Object,BitMiracle.LibTiff.Classic.TiffStream)> method. 

Renamings of functions
----------------------

|Libtiff name|LibTiff.Net name / notes|
|---|---|
|TIFFGetA|<xref:BitMiracle.LibTiff.Classic.Tiff.GetA(System.Int32)>|
|TIFFGetB|<xref:BitMiracle.LibTiff.Classic.Tiff.GetB(System.Int32)>|
|TIFFGetG|<xref:BitMiracle.LibTiff.Classic.Tiff.GetG(System.Int32)>|
|TIFFGetR|<xref:BitMiracle.LibTiff.Classic.Tiff.GetR(System.Int32)>|
|TIFF.tif_cleanup|<xref:BitMiracle.LibTiff.Classic.TiffCodec.Cleanup>|
|TIFF.tif_close|<xref:BitMiracle.LibTiff.Classic.Tiff.Close>|
|TIFF.tif_decoderow|<xref:BitMiracle.LibTiff.Classic.TiffCodec.DecodeRow(System.Byte[],System.Int32,System.Int32,System.Int16)>|
|TIFF.tif_decodestrip|<xref:BitMiracle.LibTiff.Classic.TiffCodec.DecodeStrip(System.Byte[],System.Int32,System.Int32,System.Int16)>|
|TIFF.tif_decodetile|<xref:BitMiracle.LibTiff.Classic.TiffCodec.DecodeTile(System.Byte[],System.Int32,System.Int32,System.Int16)>|
|TIFF.tif_defstripsize|<xref:BitMiracle.LibTiff.Classic.TiffCodec.DefStripSize(System.Int32)>|
|TIFF.tif_deftilesize|<xref:BitMiracle.LibTiff.Classic.TiffCodec.DefTileSize(System.Int32@,System.Int32@)>|
|TIFF.tif_encoderow|<xref:BitMiracle.LibTiff.Classic.TiffCodec.EncodeRow*>|
|TIFF.tif_encodestrip|<xref:BitMiracle.LibTiff.Classic.TiffCodec.EncodeStrip*>|
|TIFF.tif_encodetile|<xref:BitMiracle.LibTiff.Classic.TiffCodec.EncodeTile*>|
|TIFF.tif_postencode|<xref:BitMiracle.LibTiff.Classic.TiffCodec.PostEncode>|
|TIFF.tif_predecode|<xref:BitMiracle.LibTiff.Classic.TiffCodec.PreDecode(System.Int16)>|
|TIFF.tif_preencode|<xref:BitMiracle.LibTiff.Classic.TiffCodec.PreEncode(System.Int16)>|
|TIFF.tif_seek|<xref:BitMiracle.LibTiff.Classic.TiffCodec.Seek(System.Int32)>|
|TIFF.tif_setupdecode|<xref:BitMiracle.LibTiff.Classic.TiffCodec.SetupDecode>|
|TIFF.tif_setupencode|<xref:BitMiracle.LibTiff.Classic.TiffCodec.SetupEncode>|
|TIFFTagMethods.printdir|<xref:BitMiracle.LibTiff.Classic.Tiff.PrintDirectory(System.IO.Stream,BitMiracle.LibTiff.Classic.TiffPrintFlags)>|
|TIFFTagMethods.vgetfield|<xref:BitMiracle.LibTiff.Classic.Tiff.GetField*>|
|TIFFTagMethods.vsetfield|<xref:BitMiracle.LibTiff.Classic.Tiff.SetField*>|
|_TIFFmemcmp|<xref:BitMiracle.LibTiff.Classic.Tiff.Compare(System.Int16[],System.Int16[],System.Int32)> (for some types, write us if you need another overload.)|
|_TIFFmemcpy|no longer exists|
|_TIFFmemset|no longer exists|
|_TIFFrealloc|<xref:BitMiracle.LibTiff.Classic.Tiff.Realloc(System.Byte[],System.Int32)> (for some types, write us if you need another overload.)|
|TIFFAccessTagMethods|<xref:BitMiracle.LibTiff.Classic.Tiff.GetTagMethods> (Also added <xref:BitMiracle.LibTiff.Classic.Tiff.SetTagMethods(BitMiracle.LibTiff.Classic.TiffTagMethods)> in order to retain original abilities TIFFAccessTagMethods)|
|TIFFCheckTile|<xref:BitMiracle.LibTiff.Classic.Tiff.CheckTile(System.Int32,System.Int32,System.Int32,System.Int16)>|
|TIFFCheckpointDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.CheckpointDirectory>|
|TIFFCleanup|no longer exists|
|TIFFClientOpen|<xref:BitMiracle.LibTiff.Classic.Tiff.ClientOpen*>|
|TIFFClientdata|<xref:BitMiracle.LibTiff.Classic.Tiff.Clientdata>|
|TIFFClose|<xref:BitMiracle.LibTiff.Classic.Tiff.Close>|
|TIFFComputeStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.ComputeStrip*>|
|TIFFComputeTile|<xref:BitMiracle.LibTiff.Classic.Tiff.ComputeTile*>|
|TIFFCreateDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.CreateDirectory>|
|TIFFCurrentDirOffset|<xref:BitMiracle.LibTiff.Classic.Tiff.CurrentDirOffset>|
|TIFFCurrentDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.CurrentDirectory>|
|TIFFCurrentRow|<xref:BitMiracle.LibTiff.Classic.Tiff.CurrentRow>|
|TIFFCurrentStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.CurrentStrip>|
|TIFFCurrentTile|<xref:BitMiracle.LibTiff.Classic.Tiff.CurrentTile>|
|TIFFDataWidth|<xref:BitMiracle.LibTiff.Classic.Tiff.DataWidth*>|
|TIFFDefaultStripSize|<xref:BitMiracle.LibTiff.Classic.Tiff.DefaultStripSize(System.Int32)>|
|TIFFDefaultTileSize|<xref:BitMiracle.LibTiff.Classic.Tiff.DefaultTileSize(System.Int32@,System.Int32@)>|
|TIFFError|<xref:BitMiracle.LibTiff.Classic.Tiff.Error(System.String,System.String,System.Object[])> (also added overloaded method <xref:BitMiracle.LibTiff.Classic.Tiff.Error(BitMiracle.LibTiff.Classic.Tiff,System.String,System.String,System.Object[])> that accepts Tiff class instance as first parameter. Please use that overloaded method)|
|TIFFErrorExt|<xref:BitMiracle.LibTiff.Classic.Tiff.ErrorExt(System.Object,System.String,System.String,System.Object[])> (also added overloaded method <xref:BitMiracle.LibTiff.Classic.Tiff.ErrorExt(BitMiracle.LibTiff.Classic.Tiff,System.Object,System.String,System.String,System.Object[])> that accepts Tiff class instance as first parameter. Please use that overloaded method)|
|TIFFExtendProc|<xref:BitMiracle.LibTiff.Classic.Tiff.TiffExtendProc>|
|TIFFFdOpen|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.Open(System.String,System.String)>)|
|TIFFFieldWithName|<xref:BitMiracle.LibTiff.Classic.Tiff.FieldWithName(System.String)>|
|TIFFFieldWithTag|<xref:BitMiracle.LibTiff.Classic.Tiff.FieldWithTag*>|
|TIFFFileName|<xref:BitMiracle.LibTiff.Classic.Tiff.FileName>|
|TIFFFileno|no longer exists|
|TIFFFindCODEC|<xref:BitMiracle.LibTiff.Classic.Tiff.FindCodec(BitMiracle.LibTiff.Classic.Compression)>|
|TIFFFindFieldInfo|<xref:BitMiracle.LibTiff.Classic.Tiff.FindFieldInfo(BitMiracle.LibTiff.Classic.TiffTag,BitMiracle.LibTiff.Classic.TiffType)>|
|TIFFFindFieldInfoByName|<xref:BitMiracle.LibTiff.Classic.Tiff.FindFieldInfoByName(System.String,BitMiracle.LibTiff.Classic.TiffType)>|
|TIFFFlush|<xref:BitMiracle.LibTiff.Classic.Tiff.Flush>|
|TIFFFlushData|<xref:BitMiracle.LibTiff.Classic.Tiff.FlushData>|
|TIFFFreeDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.FreeDirectory>|
|TIFFGetBitRevTable|<xref:BitMiracle.LibTiff.Classic.Tiff.GetBitRevTable(System.Boolean)>|
|TIFFGetClientInfo|<xref:BitMiracle.LibTiff.Classic.Tiff.GetClientInfo(System.String)>|
|TIFFGetCloseProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFGetConfiguredCODECs|<xref:BitMiracle.LibTiff.Classic.Tiff.GetConfiguredCodecs>|
|TIFFGetField|<xref:BitMiracle.LibTiff.Classic.Tiff.GetField(BitMiracle.LibTiff.Classic.TiffTag)>|
|TIFFGetFieldDefaulted|<xref:BitMiracle.LibTiff.Classic.Tiff.GetFieldDefaulted(BitMiracle.LibTiff.Classic.TiffTag)>|
|TIFFGetMapFileProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFGetMode|<xref:BitMiracle.LibTiff.Classic.Tiff.GetMode>|
|TIFFGetReadProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFGetSeekProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFGetSizeProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFGetTagListCount|<xref:BitMiracle.LibTiff.Classic.Tiff.GetTagListCount>|
|TIFFGetTagListEntry|<xref:BitMiracle.LibTiff.Classic.Tiff.GetTagListEntry(System.Int32)>|
|TIFFGetUnmapFileProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFGetVersion|<xref:BitMiracle.LibTiff.Classic.Tiff.GetVersion>|
|TIFFGetWriteProc|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetStream>)|
|TIFFIsBigEndian|<xref:BitMiracle.LibTiff.Classic.Tiff.IsBigEndian>|
|TIFFIsByteSwapped|<xref:BitMiracle.LibTiff.Classic.Tiff.IsByteSwapped>|
|TIFFIsCODECConfigured|<xref:BitMiracle.LibTiff.Classic.Tiff.IsCodecConfigured*>|
|TIFFIsMSB2LSB|<xref:BitMiracle.LibTiff.Classic.Tiff.IsMSB2LSB>|
|TIFFIsTiled|<xref:BitMiracle.LibTiff.Classic.Tiff.IsTiled>|
|TIFFIsUpSampled|<xref:BitMiracle.LibTiff.Classic.Tiff.IsUpSampled>|
|TIFFLastDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.LastDirectory>|
|TIFFMergeFieldInfo|<xref:BitMiracle.LibTiff.Classic.Tiff.MergeFieldInfo*>|
|TIFFNumberOfDirectories|<xref:BitMiracle.LibTiff.Classic.Tiff.NumberOfDirectories>|
|TIFFNumberOfStrips|<xref:BitMiracle.LibTiff.Classic.Tiff.NumberOfStrips>|
|TIFFNumberOfTiles|<xref:BitMiracle.LibTiff.Classic.Tiff.NumberOfTiles>|
|TIFFOpen|<xref:BitMiracle.LibTiff.Classic.Tiff.Open(System.String,System.String)>|
|TIFFOpenW|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.Open(System.String,System.String)>)|
|TIFFPrintDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.PrintDirectory(System.IO.Stream)>|
|TIFFRGBAImageOK|<xref:BitMiracle.LibTiff.Classic.Tiff.RGBAImageOK(System.String@)>|
|TIFFRasterScanlineSize|<xref:BitMiracle.LibTiff.Classic.Tiff.RasterScanlineSize>|
|TIFFRawStripSize|<xref:BitMiracle.LibTiff.Classic.Tiff.RawStripSize(System.Int32)>|
|TIFFReadBufferSetup|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadBufferSetup(System.Byte[],System.Int32)>|
|TIFFReadCustomDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadCustomDirectory(System.Int64,BitMiracle.LibTiff.Classic.TiffFieldInfo[],System.Int32)>|
|TIFFReadDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadDirectory>|
|TIFFReadEXIFDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadEXIFDirectory(System.Int64)>|
|TIFFReadEncodedStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadEncodedStrip(System.Int32,System.Byte[],System.Int32,System.Int32)>|
|TIFFReadEncodedTile|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadEncodedTile(System.Int32,System.Byte[],System.Int32,System.Int32)>|
|TIFFReadRGBAImage|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage*>|
|TIFFReadRGBAImageOriented|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImageOriented*>|
|TIFFReadRGBAStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAStrip*>|
|TIFFReadRGBATile|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadRGBATile*>|
|TIFFReadRawStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadRawStrip*>|
|TIFFReadRawTile|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadRawTile(System.Int32,System.Byte[],System.Int32,System.Int32)>|
|TIFFReadScanline|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadScanline*>|
|TIFFReadTile|<xref:BitMiracle.LibTiff.Classic.Tiff.ReadTile(System.Byte[],System.Int32,System.Int32,System.Int32,System.Int32,System.Int16)>|
|TIFFReassignTagToIgnore|no longer exists|
|TIFFRegisterCODEC|<xref:BitMiracle.LibTiff.Classic.Tiff.RegisterCodec(BitMiracle.LibTiff.Classic.TiffCodec)>|
|TIFFReverseBits|<xref:BitMiracle.LibTiff.Classic.Tiff.ReverseBits(System.Byte[],System.Int32)>|
|TIFFRewriteDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.RewriteDirectory>|
|TIFFScanlineSize|<xref:BitMiracle.LibTiff.Classic.Tiff.ScanlineSize>|
|TIFFSetClientInfo|<xref:BitMiracle.LibTiff.Classic.Tiff.SetClientInfo(System.Object,System.String)>|
|TIFFSetClientdata|<xref:BitMiracle.LibTiff.Classic.Tiff.SetClientdata(System.Object)>|
|TIFFSetDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.SetDirectory(System.Int16)>|
|TIFFSetErrorHandler|no longer exists (use <xref:BitMiracle.LibTiff.Classic.Tiff.SetErrorHandler(BitMiracle.LibTiff.Classic.TiffErrorHandler)> with instance of class derived from <xref:BitMiracle.LibTiff.Classic.TiffErrorHandler>)|
|TIFFSetErrorHandlerExt|no longer exists (use <xref:BitMiracle.LibTiff.Classic.Tiff.SetErrorHandler(BitMiracle.LibTiff.Classic.TiffErrorHandler)> with instance of class derived from <xref:BitMiracle.LibTiff.Classic.TiffErrorHandler>)|
|TIFFSetField|<xref:BitMiracle.LibTiff.Classic.Tiff.SetField(BitMiracle.LibTiff.Classic.TiffTag,System.Object[])>|
|TIFFSetFileName|<xref:BitMiracle.LibTiff.Classic.Tiff.SetFileName(System.String)>|
|TIFFSetFileno|no longer exists|
|TIFFSetMode|<xref:BitMiracle.LibTiff.Classic.Tiff.SetMode(System.Int32)>|
|TIFFSetSubDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.SetSubDirectory(System.Int64)>|
|TIFFSetTagExtender|<xref:BitMiracle.LibTiff.Classic.Tiff.SetTagExtender(BitMiracle.LibTiff.Classic.Tiff.TiffExtendProc)>|
|TIFFSetWarningHandler|no longer exists (use <xref:BitMiracle.LibTiff.Classic.Tiff.SetErrorHandler(BitMiracle.LibTiff.Classic.TiffErrorHandler)> with instance of class derived from <xref:BitMiracle.LibTiff.Classic.TiffErrorHandler>)|
|TIFFSetWarningHandlerExt|no longer exists (use <xref:BitMiracle.LibTiff.Classic.Tiff.SetErrorHandler(BitMiracle.LibTiff.Classic.TiffErrorHandler)> with instance of class derived from <xref:BitMiracle.LibTiff.Classic.TiffErrorHandler>)|
|TIFFSetWriteOffset|<xref:BitMiracle.LibTiff.Classic.Tiff.SetWriteOffset(System.Int64)>|
|TIFFSetupStrips|<xref:BitMiracle.LibTiff.Classic.Tiff.SetupStrips>|
|TIFFStripSize|<xref:BitMiracle.LibTiff.Classic.Tiff.StripSize>|
|TIFFSwabArrayOfDouble|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabArrayOfDouble(System.Double[],System.Int32)>|
|TIFFSwabArrayOfLong|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabArrayOfLong(System.Int32[],System.Int32)>|
|TIFFSwabArrayOfShort|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabArrayOfShort(System.Int16[],System.Int32)>|
|TIFFSwabArrayOfTriples|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabArrayOfTriples*>|
|TIFFSwabDouble|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabDouble(System.Double@)>|
|TIFFSwabLong|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabLong(System.Int32@)>|
|TIFFSwabShort|<xref:BitMiracle.LibTiff.Classic.Tiff.SwabShort(System.Int16@)>|
|TIFFTileRowSize|<xref:BitMiracle.LibTiff.Classic.Tiff.TileRowSize>|
|TIFFTileSize|<xref:BitMiracle.LibTiff.Classic.Tiff.TileSize>|
|TIFFUnRegisterCODEC|<xref:BitMiracle.LibTiff.Classic.Tiff.UnRegisterCodec(BitMiracle.LibTiff.Classic.TiffCodec)>|
|TIFFUnlinkDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.UnlinkDirectory(System.Int16)>|
|TIFFVGetField|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetField(BitMiracle.LibTiff.Classic.TiffTag)>)|
|TIFFVGetFieldDefaulted|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.GetFieldDefaulted(BitMiracle.LibTiff.Classic.TiffTag)>)|
|TIFFVSetField|no longer exists (please use <xref:BitMiracle.LibTiff.Classic.Tiff.SetField(BitMiracle.LibTiff.Classic.TiffTag,System.Object[])>)|
|TIFFVStripSize|<xref:BitMiracle.LibTiff.Classic.Tiff.VStripSize(System.Int32)>|
|TIFFVTileSize|<xref:BitMiracle.LibTiff.Classic.Tiff.VTileSize(System.Int32)>|
|TIFFWarning|<xref:BitMiracle.LibTiff.Classic.Tiff.Warning(System.String,System.String,System.Object[])> (also added overloaded method <xref:BitMiracle.LibTiff.Classic.Tiff.Warning(BitMiracle.LibTiff.Classic.Tiff,System.String,System.String,System.Object[])> that accepts Tiff class instance as first parameter. Please use that overloaded method)|
|TIFFWarningExt|<xref:BitMiracle.LibTiff.Classic.Tiff.WarningExt(System.Object,System.String,System.String,System.Object[])> (also added overloaded method <xref:BitMiracle.LibTiff.Classic.Tiff.WarningExt(BitMiracle.LibTiff.Classic.Tiff,System.Object,System.String,System.String,System.Object[])> that accepts Tiff class instance as first parameter. Please use that overloaded method)|
|TIFFWriteBufferSetup|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteBufferSetup(System.Byte[],System.Int32)>|
|TIFFWriteCheck|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteCheck(System.Boolean,System.String)>|
|TIFFWriteDirectory|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteDirectory>|
|TIFFWriteEncodedStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteEncodedStrip*>|
|TIFFWriteEncodedTile|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteEncodedTile*>|
|TIFFWriteRawStrip|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteRawStrip*>|
|TIFFWriteRawTile|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteRawTile*>|
|TIFFWriteScanline|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteScanline*>|
|TIFFWriteTile|<xref:BitMiracle.LibTiff.Classic.Tiff.WriteTile*>|

Renamings of constants and types
--------------------------------

|Libtiff name|LibTiff.Net name / notes|
|---|---|
|CLEANFAXDATA_*|<xref:BitMiracle.LibTiff.Classic.CleanFaxData>.*|
|COLORRESPONSEUNIT_*|<xref:BitMiracle.LibTiff.Classic.ColorResponseUnit>.CRU*|
|COMPRESSION_*|<xref:BitMiracle.LibTiff.Classic.Compression>.*|
|EXIFTAG_*|<xref:BitMiracle.LibTiff.Classic.TiffTag>.EXIF_*|
|EXTRASAMPLE_*|<xref:BitMiracle.LibTiff.Classic.ExtraSample>.*|
|FAXMODE_*|<xref:BitMiracle.LibTiff.Classic.FaxMode>.*|
|FILETYPE_*|<xref:BitMiracle.LibTiff.Classic.FileType>.*|
|FILLORDER_*|<xref:BitMiracle.LibTiff.Classic.FillOrder>.*|
|GRAYRESPONSEUNIT_*|<xref:BitMiracle.LibTiff.Classic.GrayResponseUnit>.GRU*|
|GROUP3OPT_*|<xref:BitMiracle.LibTiff.Classic.Group3Opt>.* (note, that GROUP3OPT_2DENCODING renamed to ENCODING2D)|
|INKSET_*|<xref:BitMiracle.LibTiff.Classic.InkSet>.*|
|JPEGCOLORMODE_*|<xref:BitMiracle.LibTiff.Classic.JpegColorMode>.*|
|JPEGPROC_*|<xref:BitMiracle.LibTiff.Classic.JpegProc>.*|
|JPEGTABLESMODE_*|<xref:BitMiracle.LibTiff.Classic.JpegTablesMode>.*|
|OFILETYPE_*|<xref:BitMiracle.LibTiff.Classic.OFileType>.*|
|ORIENTATION_*|<xref:BitMiracle.LibTiff.Classic.Orientation>.*|
|PHOTOMETRIC_*|<xref:BitMiracle.LibTiff.Classic.Photometric>.*|
|PLANARCONFIG_*|<xref:BitMiracle.LibTiff.Classic.PlanarConfig>.*|
|PREDICTOR_*|<xref:BitMiracle.LibTiff.Classic.Predictor>.*|
|RESUNIT_*|<xref:BitMiracle.LibTiff.Classic.ResUnit>.*|
|SAMPLEFORMAT_*|<xref:BitMiracle.LibTiff.Classic.SampleFormat>.*|
|THRESHHOLD_*|<xref:BitMiracle.LibTiff.Classic.Threshold>.*|
|TIFFPRINT_*|<xref:BitMiracle.LibTiff.Classic.TiffPrintFlags>.*|
|TIFFTAG_*|<xref:BitMiracle.LibTiff.Classic.TiffTag>.*|
|YCBCRPOSITION_*|<xref:BitMiracle.LibTiff.Classic.YCbCrPosition>.*|
|TIFFDataType.TIFF_*|<xref:BitMiracle.LibTiff.Classic.TiffType>.*|
|TIFFFieldInfo|<xref:BitMiracle.LibTiff.Classic.TiffFieldInfo>
|TIFFRGBAImage|<xref:BitMiracle.LibTiff.Classic.TiffRgbaImage>
