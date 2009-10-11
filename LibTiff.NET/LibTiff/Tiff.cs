using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff.Internal;

using thandle_t = System.Object;

namespace BitMiracle.LibTiff
{
    public class Tiff
    {
        //public ~Tiff();

    public static string GetVersion();

    /*
    * Macros for extracting components from the
    * packed ABGR form returned by ReadRGBAImage.
    */
    public static uint GetR(uint abgr);
    public static uint GetG(uint abgr);
    public static uint GetB(uint abgr);
    public static uint GetA(uint abgr);

    /*
    * Other compression schemes may be registered.  Registered
    * schemes can also override the builtin versions provided
    * by this library.
    */
    public TiffCodec FindCodec(UInt16 scheme);
    public bool RegisterCodec(TiffCodec codec);
    public void UnRegisterCodec(TiffCodec c);

    /**
    * Check whether we have working codec for the specific coding scheme.
    * @return returns true if the codec is configured and working. Otherwise
    * false will be returned.
    */
    public bool IsCodecConfigured(UInt16 scheme);

    /**
    * Get array of configured codecs, both built-in and registered by user.
    * Caller is responsible to free this array (but not codecs).
    * @return returns array of TiffCodec records (the last record should be NULL)
    * or NULL if function failed.
    */
    public TiffCodec[] GetConfiguredCodecs();

    /*
     * Auxiliary functions.
     */

    /**
    * Re-allocates array and copies data from old to new array. 
    * Size is in elements, not bytes!
    * Also frees old array. Returns new allocated array.
    */
    public static byte[] Realloc(byte[] oldBuffer, int elementCount, int newElementCount);
    public static uint[] Realloc(uint[] oldBuffer, int elementCount, int newElementCount);      

    public static int Compare(UInt16[] p1, UInt16[] p2, int elementCount);

    /*
    * Open a TIFF file for read/writing.
    */
    public static Tiff Open(string name, string mode);

    /*
    * Open a TIFF file descriptor for read/writing.
    */
    public static Tiff FdOpen(int ifd, string name, string mode);

    public static Tiff ClientOpen(string name, string mode, thandle_t clientdata, TiffStream stream);

    /*
     ** Stuff, related to tag handling and creating custom tags.
     */
    public int GetTagListCount();
    public uint GetTagListEntry(int tag_index);

    public void MergeFieldInfo(TiffFieldInfo[] info, int n);
    public TiffFieldInfo FindFieldInfo(uint tag, TiffDataType dt);
    public TiffFieldInfo FindFieldInfoByName(string field_name, TiffDataType dt);
    public TiffFieldInfo FieldWithTag(uint tag);
    public TiffFieldInfo FieldWithName(string field_name);

    public TiffTagMethods GetTagMethods();
    public TiffTagMethods SetTagMethods(TiffTagMethods tagMethods);

    public object GetClientInfo(string name);
    public void SetClientInfo(object data, string name);

    public bool Flush();
    
    /*
    * Flush buffered data to the file.
    *
    * Frank Warmerdam'2000: I modified this to return false if TIFF_BEENWRITING
    * is not set, so that TIFFFlush() will proceed to write out the directory.
    * The documentation says returning false is an error indicator, but not having
    * been writing isn't exactly a an error.  Hopefully this doesn't cause
    * problems for other people. 
    */
    public bool FlushData();
    
    /*
    * Return the value of a field in the
    * internal directory structure.
    */
    //public bool GetField(uint tag, ...);
    
    /*
    * Like GetField, but taking a varargs
    * parameter list.  This routine is useful
    * for building higher-level interfaces on
    * top of the library.
    */
    //public bool VGetField(uint tag, va_list ap);
    
    /*
    * Like GetField, but return any default
    * value if the tag is not present in the directory.
    */
    //public bool GetFieldDefaulted(uint tag, ...);
    
    /*
    * Like GetField, but return any default
    * value if the tag is not present in the directory.
    *
    * NB:  We use the value in the directory, rather than
    *  explicit values so that defaults exist only one
    *  place in the library -- in setupDefaultDirectory.
    */
    //public bool VGetFieldDefaulted(uint tag, va_list ap);

    /*
    * Read the next TIFF directory from a file
    * and convert it to the internal format.
    * We read directories sequentially.
    */
    public bool ReadDirectory();

    /* 
    * Read custom directory from the arbitrary offset.
    * The code is very similar to ReadDirectory().
    */
    public bool ReadCustomDirectory(uint diroff, TiffFieldInfo[] info, uint n);
    
    public bool WriteCustomDirectory(out uint pdiroff);

    /*
    * EXIF is important special case of custom IFD, so we have a special
    * function to read it.
    */
    public bool ReadEXIFDirectory(uint diroff);

    /*
    * Return the number of bytes to read/write in a call to
    * one of the scanline-oriented i/o routines.  Note that
    * this number may be 1/samples-per-pixel if data is
    * stored as separate planes.
    */
    public int ScanlineSize();

    /*
    * Return the number of bytes required to store a complete
    * decoded and packed raster scanline (as opposed to the
    * I/O size returned by ScanlineSize which may be less
    * if data is store as separate planes).
    */
    public int RasterScanlineSize();
    
    /*
    * Compute the # bytes in a (row-aligned) strip.
    *
    * Note that if RowsPerStrip is larger than the
    * recorded ImageLength, then the strip size is
    * truncated to reflect the actual space required
    * to hold the strip.
    */
    public int StripSize();
    
    /*
    * Compute the # bytes in a raw strip.
    */
    public int RawStripSize(uint strip);
    
    /*
    * Compute the # bytes in a variable height, row-aligned strip.
    */
    public int VStripSize(uint nrows);

    /*
    * Compute the # bytes in each row of a tile.
    */
    public int TileRowSize();

    /*
    * Compute the # bytes in a row-aligned tile.
    */
    public int TileSize();
    
    /*
    * Compute the # bytes in a variable length, row-aligned tile.
    */
    public int VTileSize(uint nrows);

    /*
    * Compute a default strip size based on the image
    * characteristics and a requested value.  If the
    * request is <1 then we choose a strip size according
    * to certain heuristics.
    */
    public uint DefaultStripSize(uint request);

    /*
    * Compute a default tile size based on the image
    * characteristics and a requested value.  If a
    * request is <1 then we choose a size according
    * to certain heuristics.
    */
    public void DefaultTileSize(ref uint tw, ref uint th);
    
    /*
    * Return open file's clientdata.
    */
    public thandle_t Clientdata();

    /*
    * Set open file's clientdata, and return previous value.
    */
    public thandle_t SetClientdata(thandle_t newvalue); // should become object reference

    /*
    * Return read/write mode.
    */
    public int GetMode();

    /*
    * Return read/write mode.
    */
    public int SetMode(int mode);

    /*
    * Return nonzero if file is organized in
    * tiles; zero if organized as strips.
    */
    public bool IsTiled();

    /*
    * Return nonzero if the file has byte-swapped data.
    */
    public bool IsByteSwapped();

    /*
    * Return nonzero if the data is returned up-sampled.
    */
    public bool IsUpSampled();

    /*
    * Return nonzero if the data is returned in MSB-to-LSB bit order.
    */
    public bool IsMSB2LSB();

    /*
    * Return nonzero if given file was written in big-endian order.
    */
    public bool IsBigEndian();

    public TiffStream GetStream();

    /*
    * Return current row being read/written.
    */
    public uint CurrentRow();

    /*
    * Return index of the current directory.
    */
    public UInt16 CurrentDirectory();

    /*
    * Count the number of directories in a file.
    */
    public UInt16 NumberOfDirectories();

    /*
    * Return file offset of the current directory.
    */
    public uint CurrentDirOffset();

    /*
    * Return current strip.
    */
    public uint CurrentStrip();

    /*
    * Return current tile.
    */
    public uint CurrentTile();

    /*
    * Setup the raw data buffer in preparation for
    * reading a strip of raw data.  If the buffer
    * is specified as zero, then a buffer of appropriate
    * size is allocated by the library.  Otherwise,
    * the client must guarantee that the buffer is
    * large enough to hold any individual strip of
    * raw data.
    */
    public bool ReadBufferSetup(byte[] bp, int size);

    /*
    * Setup the raw data buffer used for encoding.
    */
    public bool WriteBufferSetup(byte[] bp, int size);

    public bool SetupStrips();
    
    /*
    * Verify file is writable and that the directory
    * information is setup properly.  In doing the latter
    * we also "freeze" the state of the directory so
    * that important information is not changed.
    */
    public bool WriteCheck(int tiles, string module);
    
    /*
    * Release storage associated with a directory.
    */
    public void FreeDirectory();

    /*
    * Setup for a new directory.  Should we automatically call
    * WriteDirectory() if the current one is dirty?
    *
    * The newly created directory will not exist on the file till
    * WriteDirectory(), Flush() or Close() is called.
    */
    public void CreateDirectory();
    
    /*
    * Return an indication of whether or not we are
    * at the last directory in the file.
    */
    public bool LastDirectory();
    
    /*
    * Set the n-th directory as the current directory.
    * NB: Directories are numbered starting at 0.
    */
    public bool SetDirectory(UInt16 dirn);

    /*
    * Set the current directory to be the directory
    * located at the specified file offset.  This interface
    * is used mainly to access directories linked with
    * the SubIFD tag (e.g. thumbnail images).
    */
    public bool SetSubDirectory(uint diroff);

    /*
    * Unlink the specified directory from the directory chain.
    */
    public bool UnlinkDirectory(UInt16 dirn);
    
    /*
    * Record the value of a field in the
    * internal directory structure.  The
    * field will be written to the file
    * when/if the directory structure is
    * updated.
    */
    public bool SetField(uint tag, params object[] ap);

    /*
    * Like SetField, but taking a varargs
    * parameter list.  This routine is useful
    * for building higher-level interfaces on
    * top of the library.
    */
    //public bool VSetField(uint tag, va_list ap);

    public bool WriteDirectory();
    
    /*
    * Similar to WriteDirectory(), writes the directory out
    * but leaves all data structures in memory so that it can be
    * written again.  This will make a partially written TIFF file
    * readable before it is successfully completed/closed.
    */
    public bool CheckpointDirectory();

    /*
    * Similar to WriteDirectory(), but if the directory has already
    * been written once, it is relocated to the end of the file, in case it
    * has changed in size.  Note that this will result in the loss of the 
    * previously used directory space. 
    */
    public bool RewriteDirectory();
    
    /*
    * Print the contents of the current directory
    * to the specified stdio file stream.
    */
    public void PrintDirectory(Stream fd)
    {
        PrintDirectory(fd, TIFFPRINT_NONE);
    }

    public void PrintDirectory(Stream fd, TiffPrintDirectoryFlags flags);

    public bool ReadScanline(byte[] buf, uint row)
    {
        return ReadScanline(buf, row, 0);
    }

    public bool ReadScanline(byte[] buf, uint row, UInt16 sample);

    public bool WriteScanline(byte[] buf, uint row)
    {
        return WriteScanline(buf, row, 0);
    }

    public bool WriteScanline(byte[] buf, uint row, UInt16 sample);
    
    /*
    * Read the specified image into an ABGR-format raster. Use bottom left
    * origin for raster by default.
    */
    public bool ReadRGBAImage(uint rwidth, uint rheight, uint[] raster)
    {
        return ReadRGBAImage(rwidth, rheight, raster, false);
    }

    public bool ReadRGBAImage(uint rwidth, uint rheight, uint[] raster, bool stop);
    
    /*
    * Read the specified image into an ABGR-format raster taking in account
    * specified orientation.
    */
    public bool ReadRGBAImageOriented(uint rwidth, uint rheight, uint[] raster)
    {
        return ReadRGBAImageOriented(rwidth, rheight, raster, ORIENTATION_BOTLEFT, false);
    }

    public bool ReadRGBAImageOriented(uint rwidth, uint rheight, uint[] raster, int orientation)
    {
        return ReadRGBAImageOriented(rwidth, rheight, raster, orientation, false);
    }

    public bool ReadRGBAImageOriented(uint rwidth, uint rheight, uint[] raster, int orientation, bool stop);

    /*
    * Read a whole strip off data from the file, and convert to RGBA form.
    * If this is the last strip, then it will only contain the portion of
    * the strip that is actually within the image space.  The result is
    * organized in bottom to top form.
    */
    public bool ReadRGBAStrip(uint row, uint[] raster);

    /*
    * Read a whole tile off data from the file, and convert to RGBA form.
    * The returned RGBA data is organized from bottom to top of tile,
    * and may include zeroed areas if the tile extends off the image.
    */
    public bool ReadRGBATile(uint col, uint row, uint[] raster);
    
    /*
    * Check the image to see if ReadRGBAImage can deal with it.
    * true/false is returned according to whether or not the image can
    * be handled.  If false is returned, emsg contains the reason
    * why it is being rejected.
    */
    public bool RGBAImageOK(out string emsg);

    /*
    * Return open file's name.
    */
    public string FileName();

    /*
    * Set the file name.
    */
    public string SetFileName(string name);

    // "tif" parameter can be NULL
    public static void Error(Tiff tif, string module, string fmt, params object[] ap);
    public static void ErrorExt(Tiff tif, thandle_t fd, string module, string fmt, params object[] ap);
    public static void Warning(Tiff tif, string module, string fmt, params object[] ap);
    public static void WarningExt(Tiff tif, thandle_t fd, string module, string fmt, params object[] ap);
    
    public static void Error(string module, string fmt, params object[] ap);
    public static void ErrorExt(thandle_t fd, string module, string fmt, params object[] ap);
    public static void Warning(string module, string fmt, params object[] ap);
    public static void WarningExt(thandle_t fd, string module, string fmt, params object[] ap);

    public static TiffErrorHandler SetErrorHandler(TiffErrorHandler errorHandler);

    //public static TiffExtendProc SetTagExtender(TiffExtendProc);

    /*
    * Compute which tile an (x,y,z,s) value is in.
    */
    public uint ComputeTile(uint x, uint y, uint z, UInt16 s);

    /*
    * Check an (x,y,z,s) coordinate
    * against the image bounds.
    */
    public bool CheckTile(uint x, uint y, uint z, UInt16 s);

    /*
    * Compute how many tiles are in an image.
    */
    public uint NumberOfTiles();
    
    /*
    * Tile-oriented Read Support
    * Contributed by Nancy Cam (Silicon Graphics).
    */

    /*
    * Read and decompress a tile of data.  The
    * tile is selected by the (x,y,z,s) coordinates.
    */
    public int ReadTile(byte[] buf, int offset, uint x, uint y, uint z, UInt16 s);

    /*
    * Read a tile of data and decompress the specified
    * amount into the user-supplied buffer.
    */
    public int ReadEncodedTile(uint tile, byte[] buf, int offset, int size);

    /*
    * Read a tile of data from the file.
    */
    public int ReadRawTile(uint tile, byte[] buf, int offset, int size);

    /*
    * Write and compress a tile of data.  The
    * tile is selected by the (x,y,z,s) coordinates.
    */
    public int WriteTile(byte[] buf, uint x, uint y, uint z, UInt16 s);
    
    /*
    * Compute which strip a (row,sample) value is in.
    */
    public uint ComputeStrip(uint row, UInt16 sample);

    /*
    * Compute how many strips are in an image.
    */
    public uint NumberOfStrips();
    
    /*
    * Read a strip of data and decompress the specified
    * amount into the user-supplied buffer.
    */
    public int ReadEncodedStrip(uint strip, byte[] buf, int offset, int size);

    /*
    * Read a strip of data from the file.
    */
    public int ReadRawStrip(uint strip, byte[] buf, int offset, int size);

    /*
    * Encode the supplied data and write it to the
    * specified strip.
    *
    * NB: Image length must be setup before writing.
    */
    public int WriteEncodedStrip(uint strip, byte[] data, int cc);

    /*
    * Write the supplied data to the specified strip.
    *
    * NB: Image length must be setup before writing.
    */
    public int WriteRawStrip(uint strip, byte[] data, int cc);

    /*
    * Encode the supplied data and write it to the
    * specified tile.  There must be space for the
    * data.  The function clamps individual writes
    * to a tile to the tile size, but does not (and
    * can not) check that multiple writes to the same
    * tile do not write more than tile size data.
    *
    * NB: Image length must be setup before writing; this
    *     interface does not support automatically growing
    *     the image on each write (as WriteScanline does).
    */
    public int WriteEncodedTile(uint tile, byte[] data, int cc);

    /*
    * Write the supplied data to the specified strip.
    * There must be space for the data; we don't check
    * if strips overlap!
    *
    * NB: Image length must be setup before writing; this
    *     interface does not support automatically growing
    *     the image on each write (as WriteScanline does).
    */
    public int WriteRawTile(uint tile, byte[] data, int cc);

    /*
    * Set the current write offset.  This should only be
    * used to set the offset to a known previous location
    * (very carefully), or to 0 so that the next write gets
    * appended to the end of the file.
    */
    public void SetWriteOffset(uint off);

    /*
    * Return size of TiffDataType in bytes
    */
    public static int DataWidth(TiffDataType type);

    /*
    * TIFF Library Bit & Byte Swapping Support.
    *
    * XXX We assume short = 16-bits and long = 32-bits XXX
    */
    public static void SwabShort(ref UInt16 wp);
    public static void SwabLong(ref uint lp);
    public static void SwabDouble(ref double dp);
    public static void SwabArrayOfShort(UInt16[] wp, int n);
    public static void SwabArrayOfTriples(byte[] tp, int n);
    public static void SwabArrayOfLong(uint[] lp, int n);
    public static void SwabArrayOfDouble(double[] dp, int n);
    
    public static void ReverseBits(byte[] cp, int n);
    
    public static byte[] GetBitRevTable(bool reversed);

    internal const int STRIP_SIZE_DEFAULT = 8192;

    internal static TiffFieldInfo[] Realloc(TiffFieldInfo[] oldBuffer, int elementCount, int newElementCount);
    internal static TiffTagValue[] Realloc(TiffTagValue[] oldBuffer, int elementCount, int newElementCount);

    /*
    * Read the specified strip and setup for decoding. 
    * The data buffer is expanded, as necessary, to
    * hold the strip's data.
    */
    internal bool fillStrip(uint strip);

    /*
    * Read the specified tile and setup for decoding. 
    * The data buffer is expanded, as necessary, to
    * hold the tile's data.
    */
    internal bool fillTile(uint tile);

    internal static uint roundUp(uint x, uint y);
    internal static uint howMany(uint x, uint y);

    internal static void setString(out string cpp, string cp);
    internal static void setShortArray(out UInt16[] wpp, UInt16[] wp, uint n);
    internal static void setLongArray(out uint[] lpp, uint[] lp, uint n);

    /*
    * Internal version of FlushData that can be
    * called by ``encodestrip routines'' w/o concern
    * for infinite recursion.
    */
    internal bool flushData1();

    /*
    * Return size of TiffDataType in bytes.
    *
    * XXX: We need a separate function to determine the space needed
    * to store the value. For TIFF_RATIONAL values DataWidth() returns 8,
    * but we use 4-byte float to represent rationals.
    */
    internal static int dataSize(TiffDataType type);
    
    internal bool setCompressionScheme(int scheme);

    internal bool fieldSet(int field);
    internal void setFieldBit(int field);
    internal void clearFieldBit(int field);

    /*
    * Return the number of bytes to read/write in a call to
    * one of the scanline-oriented i/o routines.  Note that
    * this number may be 1/samples-per-pixel if data is
    * stored as separate planes.
    * The ScanlineSize in case of YCbCrSubsampling is defined as the
    * strip size divided by the strip height, i.e. the size of a pack of vertical
    * subsampling lines divided by vertical subsampling. It should thus make
    * sense when multiplied by a multiple of vertical subsampling.
    * Some stuff depends on this newer version of TIFFScanlineSize
    * TODO: resolve this
    */
    internal int newScanlineSize();

    /*
    * Some stuff depends on this older version of TIFFScanlineSize
    * TODO: resolve this
    */
    internal int oldScanlineSize();

    internal static int[] byteArrayToInt(byte[] b, int byteStartOffset, int byteCount);
    internal static void intToByteArray(int[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset);

    internal static uint[] byteArrayToUInt(byte[] b, int byteStartOffset, int byteCount);
    internal static void uintToByteArray(uint[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset);

    internal static Int16[] byteArrayToInt16(byte[] b, int byteStartOffset, int byteCount);
    internal static void int16ToByteArray(Int16[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset);

    internal static UInt16[] byteArrayToUInt16(byte[] b, int byteStartOffset, int byteCount);
    internal static void uint16ToByteArray(UInt16[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset);

    internal uint readUInt32(byte[] b, int byteStartOffset);
    internal void writeUInt32(uint value, byte[] b, int byteStartOffset);
    internal UInt16 readUInt16(byte[] b, int byteStartOffset);

//////////////////////////////////////////////////////////////////////////

    internal const uint TIFF_FILLORDER = 0x0003;  /* natural bit fill order for machine */
    internal const uint TIFF_DIRTYDIRECT = 0x0008;  /* current directory must be written */
    internal const uint TIFF_BUFFERSETUP = 0x0010;  /* data buffers setup */
    internal const uint TIFF_CODERSETUP = 0x0020;  /* encoder/decoder setup done */
    internal const uint TIFF_BEENWRITING = 0x0040;  /* written 1+ scanlines to file */
    internal const uint TIFF_SWAB = 0x0080;  /* byte swap file information */
    internal const uint TIFF_NOBITREV = 0x0100;  /* inhibit bit reversal logic */
    internal const uint TIFF_MYBUFFER = 0x0200;  /* my raw data buffer; free on close */
    internal const uint TIFF_ISTILED = 0x0400;  /* file is tile, not strip- based */
    internal const uint TIFF_POSTENCODE = 0x1000;  /* need call to postencode routine */
    internal const uint TIFF_INSUBIFD = 0x2000;  /* currently writing a subifd */
    internal const uint TIFF_UPSAMPLED = 0x4000;  /* library is doing data up-sampling */ 
    internal const uint TIFF_STRIPCHOP = 0x8000;  /* enable strip chopping support */
    internal const uint TIFF_HEADERONLY = 0x10000; /* read header only, do not process the first directory*/
    internal const uint TIFF_NOREADRAW = 0x20000; /* skip reading of raw uncompressed image data*/

    internal enum PostDecodeMethodType
    {
        pdmNone,
        pdmSwab16Bit,
        pdmSwab24Bit,
        pdmSwab32Bit,
        pdmSwab64Bit
    };
    
    internal string m_name; /* name of open file */
    internal int m_mode; /* open mode (O_*) */
    internal uint m_flags;

    /* the first directory */
    internal uint m_diroff; /* file offset of current directory */

    /* directories to prevent IFD looping */
    internal TiffDirectory m_dir; /* internal rep of current directory */
    internal uint m_row; /* current scanline */
    internal uint m_curstrip; /* current strip for read/write */

    /* tiling support */
    internal uint m_curtile; /* current tile for read/write */
    internal int m_tilesize; /* # of bytes in a tile */

    /* compression scheme hooks */
    internal TiffCodec m_currentCodec;

    /* input/output buffering */
    internal int m_scanlinesize; /* # of bytes in a scanline */
    internal byte[] m_rawdata; /* raw data buffer */
    internal int m_rawdatasize; /* # of bytes in raw data buffer */
    internal int m_rawcp; /* current spot in raw buffer */
    internal int m_rawcc; /* bytes unread from raw buffer */

    internal thandle_t m_clientdata; /* callback parameter */ // should become object reference

    /* post-decoding support */
    internal PostDecodeMethodType m_postDecodeMethod;  /* post decoding method type */

    /* tag support */
    internal TiffTagMethods m_tagmethods; /* tag get/set/print routines */

    private class codecList
    {
        public codecList next;
        public TiffCodec codec;
    };

    private class clientInfoLink
    {
        public clientInfoLink next;
        public object data;
        public string name;
    };

    /* the first directory */
    private uint m_nextdiroff; /* file offset of following directory */
    private uint[] m_dirlist; /* list of offsets to already seen directories to prevent IFD looping */
    private int	m_dirlistsize; /* number of entires in offset list */
    private UInt16 m_dirnumber; /* number of already seen directories */
    private TiffHeader m_header; /* file's header block */
    private int[] m_typeshift; /* data type shift counts */
    private int[] m_typemask; /* data type masks */
    private UInt16 m_curdir; /* current directory (index) */
    private uint m_curoff; /* current offset for read/write */
    private uint m_dataoff; /* current offset for writing dir */
    
    /* SubIFD support */
    private UInt16 m_nsubifd; /* remaining subifds to write */
    private uint m_subifdoff; /* offset for patching SubIFD link */
    
    /* tiling support */
    private uint m_col; /* current column (offset by row too) */
    
    /* compression scheme hooks */
    private bool m_decodestatus;
    
    /* tag support */
    private TiffFieldInfo[] m_fieldinfo; /* sorted table of registered tags */
    private uint m_nfields; /* # entries in registered tag table */
    private TiffFieldInfo m_foundfield; /* cached pointer to already found tag */
    
    private clientInfoLink m_clientinfo; /* extra client information. */

    private TiffCodec[] m_builtInCodecs;
    private codecList m_registeredCodecs;

    private TiffTagMethods m_defaultTagMethods;

    private static TiffErrorHandler m_errorHandler;
    private TiffErrorHandler m_defaultErrorHandler;

    /*
    * Client Tag extension support (from Niles Ritter).
    */
    //private static TiffExtendProc m_extender;

    private TiffStream m_stream; // stream used for read|write|etc.
    private bool m_userStream; // if true, then stream in use is provided by user.

    //private const string m_version = TIFFLIB_VERSION_STR;

    // tiff.cpp
    private Tiff();
    private void cleanUp();
    private void postDecode(byte[] buf, int cc); /* post decoding routine */

    // tif_aux.cpp 
    private static bool defaultTransferFunction(TiffDirectory td);   

    // tif_codec.cpp 
    private void setupBuiltInCodecs();
    private void freeCodecs();

    // tif_dir.cpp 
    
    /* is tag value normal or pseudo */
    private static bool isPseudoTag(uint t);
    private bool isFillOrder(UInt16 o);
    private static uint BITn(int n);

    /*
    * Return true / false according to whether or not
    * it is permissible to set the tag's value.
    * Note that we allow ImageLength to be changed
    * so that we can append and extend to images.
    * Any other tag may not be altered once writing
    * has commenced, unless its value has no effect
    * on the format of the data that is written.
    */
    private bool okToChangeTag(uint tag);
    
    /*
    * Setup a default directory structure.
    */
    private void setupDefaultDirectory();

    private bool advanceDirectory(ref uint nextdir, out uint off);

    // tif_dirinfo.cpp 
    private TiffFieldInfo getFieldInfo(out uint size);
    private TiffFieldInfo getExifFieldInfo(out uint size);
    private void setupFieldInfo(TiffFieldInfo[] info, uint n);
    
    //private static int tagCompare(const void* a, const void* b);
    //private static int tagNameCompare(const void* a, const void* b);

    private void printFieldInfo(Stream fd);

    /*
    * Return nearest TiffDataType to the sample type of an image.
    */
    private TiffDataType sampleToTagType();

    private TiffFieldInfo findOrRegisterFieldInfo(uint tag, TiffDataType dt);
    private TiffFieldInfo createAnonFieldInfo(uint tag, TiffDataType field_type);

    // tif_dirread.cpp 
    private const UInt16 TIFFTAG_IGNORE = 0;       /* tag placeholder used below */
    
    private uint extractData(TiffDirEntry dir);
    private bool byteCountLooksBad(TiffDirectory td);
    private static uint howMany8(uint x);
    private bool readDirectoryFailed(TiffDirEntry dir);
    private bool estimateStripByteCounts(TiffDirEntry dir, UInt16 dircount);
    private void missingRequired(string tagname);
    private int fetchFailed(TiffDirEntry dir);
    private static int readDirectoryFind(TiffDirEntry dir, UInt16 dircount, UInt16 tagid);
    
    /*
    * Check the directory offset against the list of already seen directory
    * offsets. This is a trick to prevent IFD looping. The one can create TIFF
    * file with looped directory pointers. We will maintain a list of already
    * seen directories and check every IFD offset against that list.
    */
    private bool checkDirOffset(uint diroff);

    /*
    * Read IFD structure from the specified offset. If the pointer to
    * nextdiroff variable has been specified, read it too. Function returns a
    * number of fields in the directory or 0 if failed.
    */
    private UInt16 fetchDirectory(uint diroff, out TiffDirEntry[] pdir, out uint nextdiroff);

    /*
    * Fetch and set the SubjectDistance EXIF tag.
    */
    private bool fetchSubjectDistance(TiffDirEntry dir);

    /*
    * Check the count field of a directory
    * entry against a known value.  The caller
    * is expected to skip/ignore the tag if
    * there is a mismatch.
    */
    private bool checkDirCount(TiffDirEntry dir, uint count);

    /*
    * Fetch a contiguous directory item.
    */
    private int fetchData(TiffDirEntry dir, byte[] cp);

    /*
    * Fetch an ASCII item from the file.
    */
    private int fetchString(TiffDirEntry dir, out string cp);

    /*
    * Convert numerator+denominator to float.
    */
    private bool cvtRational(TiffDirEntry dir, uint num, uint denom, out float rv);

    /*
    * Fetch a rational item from the file
    * at offset off and return the value
    * as a floating point number.
    */
    private float fetchRational(TiffDirEntry dir);

    /*
    * Fetch a single floating point value
    * from the offset field and return it
    * as a native float.
    */
    private float fetchFloat(TiffDirEntry dir);

    /*
    * Fetch an array of BYTE or SBYTE values.
    */
    private bool fetchByteArray(TiffDirEntry dir, byte[] v);

    /*
    * Fetch an array of SHORT or SSHORT values.
    */
    private bool fetchShortArray(TiffDirEntry dir, UInt16[] v);

    /*
    * Fetch a pair of SHORT or BYTE values. Some tags may have either BYTE
    * or SHORT type and this function works with both ones.
    */
    private bool fetchShortPair(TiffDirEntry dir);

    /*
    * Fetch an array of LONG or SLONG values.
    */
    private bool fetchLongArray(TiffDirEntry dir, uint[] v);

    /*
    * Fetch an array of RATIONAL or SRATIONAL values.
    */
    private bool fetchRationalArray(TiffDirEntry dir, float[] v);
    
    /*
    * Fetch an array of FLOAT values.
    */
    private bool fetchFloatArray(TiffDirEntry dir, float[] v);

    /*
    * Fetch an array of DOUBLE values.
    */
    private bool fetchDoubleArray(TiffDirEntry dir, double[] v);

    /*
    * Fetch an array of ANY values.  The actual values are
    * returned as doubles which should be able hold all the
    * types.  Yes, there really should be an tany_t to avoid
    * this potential non-portability ...  Note in particular
    * that we assume that the double return value vector is
    * large enough to read in any fundamental type.  We use
    * that vector as a buffer to read in the base type vector
    * and then convert it in place to double (from end
    * to front of course).
    */
    private bool fetchAnyArray(TiffDirEntry dir, double[] v);

    /*
    * Fetch a tag that is not handled by special case code.
    */
    private bool fetchNormalTag(TiffDirEntry dir);

    /*
    * Fetch samples/pixel short values for 
    * the specified tag and verify that
    * all values are the same.
    */
    private bool fetchPerSampleShorts(TiffDirEntry dir, out UInt16 pl);

    /*
    * Fetch samples/pixel long values for 
    * the specified tag and verify that
    * all values are the same.
    */
    private bool fetchPerSampleLongs(TiffDirEntry dir, out uint pl);

    /*
    * Fetch samples/pixel ANY values for the specified tag and verify that all
    * values are the same.
    */
    private bool fetchPerSampleAnys(TiffDirEntry dir, out double pl);

    /*
    * Fetch a set of offsets or lengths.
    * While this routine says "strips", in fact it's also used for tiles.
    */
    private bool fetchStripThing(TiffDirEntry dir, int nstrips, ref uint[] lpp);

    /*
    * Fetch and set the RefBlackWhite tag.
    */
    private bool fetchRefBlackWhite(TiffDirEntry dir);

    /*
    * Replace a single strip (tile) of uncompressed data by
    * multiple strips (tiles), each approximately 8Kbytes.
    * This is useful for dealing with large images or
    * for dealing with machines with a limited amount
    * memory.
    */
    private void chopUpSingleUncompressedStrip();

    // tif_dirwrite.cpp     

    private uint insertData(UInt16 type, uint v);
    private static void resetFieldBit(uint[] fields, ushort f);
    private static bool fieldSet(uint[] fields, ushort f);

    private bool writeRational(TiffDataType type, UInt16 tag, ref TiffDirEntry dir, float v);
    private bool writeRationalPair(TiffDirEntry[] entries, int dirOffset, TiffDataType type, UInt16 tag1, float v1, UInt16 tag2, float v2);

    /*
    * Write the contents of the current directory
    * to the specified file.  This routine doesn't
    * handle overwriting a directory with auxiliary
    * storage that's been changed.
    */
    private bool writeDirectory(bool done);

    /*
    * Process tags that are not special cased.
    */
    private bool writeNormalTag(ref TiffDirEntry dir, TiffFieldInfo fip);

    /*
    * Setup a directory entry with either a SHORT
    * or LONG type according to the value.
    */
    private void setupShortLong(uint tag, ref TiffDirEntry dir, uint v);

    /*
    * Setup a SHORT directory entry
    */
    private void setupShort(uint tag, ref TiffDirEntry dir, UInt16 v);

    /*
    * Setup a directory entry that references a
    * samples/pixel array of SHORT values and
    * (potentially) write the associated indirect
    * values.
    */
    private bool writePerSampleShorts(uint tag, ref TiffDirEntry dir);

    /*
    * Setup a directory entry that references a samples/pixel array of ``type''
    * values and (potentially) write the associated indirect values.  The source
    * data from GetField() for the specified tag must be returned as double.
    */
    private bool writePerSampleAnys(TiffDataType type, uint tag, ref TiffDirEntry dir);

    /*
    * Setup a pair of shorts that are returned by
    * value, rather than as a reference to an array.
    */
    private bool setupShortPair(uint tag, ref TiffDirEntry dir);

    /*
    * Setup a directory entry for an NxM table of shorts,
    * where M is known to be 2**bitspersample, and write
    * the associated indirect data.
    */
    private bool writeShortTable(uint tag, ref TiffDirEntry dir, uint n, UInt16[][] table);

    /*
    * Write/copy data associated with an ASCII or opaque tag value.
    */
    private bool writeByteArray(ref TiffDirEntry dir, byte[] cp);

    /*
    * Setup a directory entry of an array of SHORT
    * or SSHORT and write the associated indirect values.
    */
    private bool writeShortArray(ref TiffDirEntry dir, UInt16[] v);

    /*
    * Setup a directory entry of an array of LONG
    * or SLONG and write the associated indirect values.
    */
    private bool writeLongArray(ref TiffDirEntry dir, uint[] v);

    /*
    * Setup a directory entry of an array of RATIONAL
    * or SRATIONAL and write the associated indirect values.
    */
    private bool writeRationalArray(ref TiffDirEntry dir, float[] v);
    private bool writeFloatArray(ref TiffDirEntry dir, float[] v);
    private bool writeDoubleArray(ref TiffDirEntry dir, double[] v);

    /*
    * Write an array of ``type'' values for a specified tag (i.e. this is a tag
    * which is allowed to have different types, e.g. SMaxSampleType).
    * Internally the data values are represented as double since a double can
    * hold any of the TIFF tag types (yes, this should really be an abstract
    * type tany_t for portability).  The data is converted into the specified
    * type in a temporary buffer and then handed off to the appropriate array
    * writer.
    */
    private bool writeAnyArray(TiffDataType type, uint tag, ref TiffDirEntry dir, int n, double[] v);

    private bool writeTransferFunction(ref TiffDirEntry dir);
    private bool writeInkNames(ref TiffDirEntry dir);

    /*
    * Write a contiguous directory item.
    */
    private bool writeData(ref TiffDirEntry dir, byte[] cp, int cc);
    private bool writeData(ref TiffDirEntry dir, UInt16[] cp, uint cc);
    private bool writeData(ref TiffDirEntry dir, uint[] cp, uint cc);
    private bool writeData(ref TiffDirEntry dir, float[] cp, uint cc);
    private bool writeData(ref TiffDirEntry dir, double[] cp, uint cc);

    /*
    * Link the current directory into the
    * directory chain for the file.
    */
    private bool linkDirectory();


    // tif_open.cpp 

    private static readonly int[] typemask = 
    {
        0,           /* TIFF_NOTYPE */
        0x000000ff,  /* TIFF_BYTE */
        0xffffffff,  /* TIFF_ASCII */
        0x0000ffff,  /* TIFF_SHORT */
        0xffffffff,  /* TIFF_LONG */
        0xffffffff,  /* TIFF_RATIONAL */
        0x000000ff,  /* TIFF_SBYTE */
        0x000000ff,  /* TIFF_UNDEFINED */
        0x0000ffff,  /* TIFF_SSHORT */
        0xffffffff,  /* TIFF_SLONG */
        0xffffffff,  /* TIFF_SRATIONAL */
        0xffffffff,  /* TIFF_FLOAT */
        0xffffffff,  /* TIFF_DOUBLE */
    };

    private static readonly int[] bigTypeshift = 
    {
        0,  /* TIFF_NOTYPE */
        24,  /* TIFF_BYTE */
        0,  /* TIFF_ASCII */
        16,  /* TIFF_SHORT */
        0,  /* TIFF_LONG */
        0,  /* TIFF_RATIONAL */
        24,  /* TIFF_SBYTE */
        24,  /* TIFF_UNDEFINED */
        16,  /* TIFF_SSHORT */
        0,  /* TIFF_SLONG */
        0,  /* TIFF_SRATIONAL */
        0,  /* TIFF_FLOAT */
        0,  /* TIFF_DOUBLE */
    };

    private static readonly int[] litTypeshift = 
    {
        0,  /* TIFF_NOTYPE */
        0,  /* TIFF_BYTE */
        0,  /* TIFF_ASCII */
        0,  /* TIFF_SHORT */
        0,  /* TIFF_LONG */
        0,  /* TIFF_RATIONAL */
        0,  /* TIFF_SBYTE */
        0,  /* TIFF_UNDEFINED */
        0,  /* TIFF_SSHORT */
        0,  /* TIFF_SLONG */
        0,  /* TIFF_SRATIONAL */
        0,  /* TIFF_FLOAT */
        0,  /* TIFF_DOUBLE */
    };

    /*
    * Initialize the shift & mask tables, and the
    * byte swapping state according to the file
    * contents and the machine architecture.
    */
    private void initOrder(int magic);

    private static int getMode(string mode, string module);
    private Tiff safeOpenFailed();


    // tif_print.cpp 

    private static readonly string[] photoNames = 
    {
        "min-is-white", /* PHOTOMETRIC_MINISWHITE */
        "min-is-black",  /* PHOTOMETRIC_MINISBLACK */
        "RGB color",  /* PHOTOMETRIC_RGB */
        "palette color (RGB from colormap)",  /* PHOTOMETRIC_PALETTE */
        "transparency mask",  /* PHOTOMETRIC_MASK */
        "separated",  /* PHOTOMETRIC_SEPARATED */
        "YCbCr",  /* PHOTOMETRIC_YCBCR */
        "7 (0x7)",
        "CIE L*a*b*",  /* PHOTOMETRIC_CIELAB */
    };

    private static readonly string[] orientNames = 
    {
        "0 (0x0)",
        "row 0 top, col 0 lhs", /* ORIENTATION_TOPLEFT */
        "row 0 top, col 0 rhs",  /* ORIENTATION_TOPRIGHT */
        "row 0 bottom, col 0 rhs",  /* ORIENTATION_BOTRIGHT */
        "row 0 bottom, col 0 lhs",  /* ORIENTATION_BOTLEFT */
        "row 0 lhs, col 0 top",  /* ORIENTATION_LEFTTOP */
        "row 0 rhs, col 0 top",  /* ORIENTATION_RIGHTTOP */
        "row 0 rhs, col 0 bottom",  /* ORIENTATION_RIGHTBOT */
        "row 0 lhs, col 0 bottom",  /* ORIENTATION_LEFTBOT */
    };

    private static void printField(Stream fd, TiffFieldInfo fip, uint value_count, object raw_data);
    private bool prettyPrintField(Stream fd, uint tag, uint value_count, object raw_data);
    private static void printAscii(Stream fd, string cp);
    private static void printAsciiTag(Stream fd, string name, string value);


    // tif_read.cpp 

    //private const uint NOSTRIP = ((uint) -1);         /* undefined state */
    //private const uint NOTILE = ((uint) -1);          /* undefined state */

    /*
    * Default Read/Seek/Write definitions.
    */
    private int readFile(byte[] buf, int offset, int size);
    private uint seekFile(uint off, int whence);
    private bool closeFile();
    private uint getFileSize();
    
    private bool readOK(byte[] buf, int size);
    private bool readUInt16OK(out UInt16 value);
    private bool readUInt32OK(out uint value);
    private bool readDirEntryOk(TiffDirEntry[] dir, UInt16 dircount);
    private void readDirEntry(TiffDirEntry[] dir, UInt16 dircount, byte[] bytes, uint offset);
    private bool readHeaderOk(out TiffHeader header);

    private bool seekOK(uint off);

    /*
    * Seek to a random row+sample in a file.
    */
    private bool seek(uint row, UInt16 sample);

    private int readRawStrip1(uint strip, byte[] buf, int offset, int size, string module);
    private int readRawTile1(uint tile, byte[] buf, int offset, int size, string module);

    /*
    * Set state to appear as if a
    * strip has just been read in.
    */
    private bool startStrip(uint strip);

    /*
    * Set state to appear as if a
    * tile has just been read in.
    */
    private bool startTile(uint tile);

    private bool checkRead(int tiles);

    private static void swab16BitData(byte[] buf, int cc);
    private static void swab24BitData(byte[] buf, int cc);
    private static void swab32BitData(byte[] buf, int cc);
    private static void swab64BitData(byte[] buf, int cc);

    // tif_strip.cpp 

    private uint summarize(uint summand1, uint summand2, string where);
    private uint multiply(uint nmemb, uint elem_size, string where);
    
    // tif_swab.cpp 

    /*
    * Bit reversal tables.  TIFFBitRevTable[<byte>] gives
    * the bit reversed value of <byte>.  Used in various
    * places in the library when the FillOrder requires
    * bit reversal of byte values (e.g. CCITT Fax 3
    * encoding/decoding).  TIFFNoBitRevTable is provided
    * for algorithms that want an equivalent table that
    * do not reverse bit values.
    */
    private static readonly byte[] TIFFBitRevTable = 
    {
        0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0, 0x10, 0x90, 0x50, 0xd0, 
        0x30, 0xb0, 0x70, 0xf0, 0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8, 
        0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8, 0x04, 0x84, 0x44, 0xc4, 
        0x24, 0xa4, 0x64, 0xe4, 0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4, 
        0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec, 0x1c, 0x9c, 0x5c, 0xdc, 
        0x3c, 0xbc, 0x7c, 0xfc, 0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2, 
        0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2, 0x0a, 0x8a, 0x4a, 0xca, 
        0x2a, 0xaa, 0x6a, 0xea, 0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa, 
        0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6, 0x16, 0x96, 0x56, 0xd6, 
        0x36, 0xb6, 0x76, 0xf6, 0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee, 
        0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe, 0x01, 0x81, 0x41, 0xc1, 
        0x21, 0xa1, 0x61, 0xe1, 0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1, 
        0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9, 0x19, 0x99, 0x59, 0xd9, 
        0x39, 0xb9, 0x79, 0xf9, 0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5, 
        0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5, 0x0d, 0x8d, 0x4d, 0xcd, 
        0x2d, 0xad, 0x6d, 0xed, 0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd, 
        0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3, 0x13, 0x93, 0x53, 0xd3, 
        0x33, 0xb3, 0x73, 0xf3, 0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb, 
        0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb, 0x07, 0x87, 0x47, 0xc7, 
        0x27, 0xa7, 0x67, 0xe7, 0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7, 
        0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef, 0x1f, 0x9f, 0x5f, 0xdf, 
        0x3f, 0xbf, 0x7f, 0xff
    };

    private static readonly byte[] TIFFNoBitRevTable = 
    {
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 
        0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 
        0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 
        0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 
        0x3c, 0x3d, 0x3e, 0x3f, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 
        0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53, 
        0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f, 
        0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 
        0x6c, 0x6d, 0x6e, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 
        0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x82, 0x83, 
        0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x8b, 0x8c, 0x8d, 0x8e, 0x8f, 
        0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0x9b, 
        0x9c, 0x9d, 0x9e, 0x9f, 0xa0, 0xa1, 0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 
        0xa8, 0xa9, 0xaa, 0xab, 0xac, 0xad, 0xae, 0xaf, 0xb0, 0xb1, 0xb2, 0xb3, 
        0xb4, 0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xbb, 0xbc, 0xbd, 0xbe, 0xbf, 
        0xc0, 0xc1, 0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xcb, 
        0xcc, 0xcd, 0xce, 0xcf, 0xd0, 0xd1, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 
        0xd8, 0xd9, 0xda, 0xdb, 0xdc, 0xdd, 0xde, 0xdf, 0xe0, 0xe1, 0xe2, 0xe3, 
        0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xeb, 0xec, 0xed, 0xee, 0xef, 
        0xf0, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa, 0xfb, 
        0xfc, 0xfd, 0xfe, 0xff, 
    };

    // tif_write.cpp 

    private bool writeCheckStrips(string module);
    private bool writeCheckTiles(string module);
    private bool bufferCheck();

    private int writeFile(byte[] buf, int size);
    private bool writeOK(byte[] buf, int size);
    private bool writeHeaderOK(TiffHeader header);
    private bool writeDirEntryOK(TiffDirEntry[] entries, int count);
    private bool writeUInt16OK(UInt16 value);
    private bool writeUInt32OK(uint value);

    private bool isUnspecified(int f);

    /*
    * Grow the strip data structures by delta strips.
    */
    private bool growStrips(int delta, string module);

    /*
    * Append the data to the specified strip.
    */
    private bool appendToStrip(uint strip, byte[] data, int cc);

    }
}
