/* Copyright (C) 2008-2010, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

/*
 * TIFF Tag Definitions.
 */

/*
 * NB: In the comments below,
 *  - items marked with a + are obsoleted by revision 5.0,
 *  - items marked with a ! are introduced in revision 6.0.
 *  - items marked with a % are introduced post revision 6.0.
 *  - items marked with a $ are obsoleted by revision 6.0.
 *  - items marked with a & are introduced by Adobe DNG specification.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// FileType
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum FileType
    {
        /// <summary>
        /// Reduced resolution version
        /// </summary>
        REDUCEDIMAGE = 0x1,
        /// <summary>
        /// One page of many
        /// </summary>
        PAGE = 0x2,
        /// <summary>
        /// Transparency mask.
        /// </summary>
        MASK = 0x4
    };

    /// <summary>
    /// OFileType
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum OFileType
    {
        /// <summary>
        /// Full resolution image data
        /// </summary>
        IMAGE = 1,
        /// <summary>
        /// Reduced size image data
        /// </summary>
        REDUCEDIMAGE = 2,
        /// <summary>
        /// One page of many
        /// </summary>
        PAGE = 3
    };

    /// <summary>
    /// Compression
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Compression
    {
        /// <summary>
        /// Dump mode
        /// </summary>
        NONE = 1,
        /// <summary>
        /// CCITT modified Huffman RLE
        /// </summary>
        CCITTRLE = 2,
        /// <summary>
        /// CCITT Group 3 fax encoding
        /// </summary>
        CCITTFAX3 = 3,
        /// <summary>
        /// CCITT T.4 (TIFF 6 name)
        /// </summary>
        CCITT_T4 = 3,
        /// <summary>
        /// CCITT Group 4 fax encoding
        /// </summary>
        CCITTFAX4 = 4,
        /// <summary>
        /// CCITT T.6 (TIFF 6 name)
        /// </summary>
        CCITT_T6 = 4,
        /// <summary>
        /// Lempel-Ziv &amp; Welch
        /// </summary>
        LZW = 5,
        /// <summary>
        /// Old-style JPEG (6.0)
        /// </summary>
        OJPEG = 6, //!
        /// <summary>
        /// JPEG DCT compression
        /// </summary>
        JPEG = 7, //%
        /// <summary>
        /// NeXT 2-bit RLE
        /// </summary>
        NEXT = 32766,
        /// <summary>
        /// CCITT RLE
        /// </summary>
        CCITTRLEW = 32771,
        /// <summary>
        /// Macintosh RLE
        /// </summary>
        PACKBITS = 32773,
        /// <summary>
        /// ThunderScan RLE
        /// </summary>
        THUNDERSCAN = 32809,
        /* codes 32895-32898 are reserved for ANSI IT8 TIFF/IT */
        /// <summary>
        /// IT8 CT w/padding
        /// </summary>
        IT8CTPAD = 32895,
        /// <summary>
        /// IT8 Linework RLE
        /// </summary>
        IT8LW = 32896,
        /// <summary>
        /// IT8 Monochrome picture
        /// </summary>
        IT8MP = 32897,
        /// <summary>
        /// IT8 Binary line art
        /// </summary>
        IT8BL = 32898,
        /* compression codes 32908-32911 are reserved for Pixar */
        /// <summary>
        /// Pixar companded 10bit LZW
        /// </summary>
        PIXARFILM = 32908,
        /// <summary>
        /// Pixar companded 11bit ZIP
        /// </summary>
        PIXARLOG = 32909,
        /// <summary>
        /// Deflate compression
        /// </summary>
        DEFLATE = 32946,
        /// <summary>
        /// Deflate compression, as recognized by Adobe
        /// </summary>
        ADOBE_DEFLATE = 8,
        /* compression code 32947 is reserved for Oceana Matrix <dev@oceana.com> */
        /// <summary>
        /// Kodak DCS encoding
        /// </summary>
        DCS = 32947,
        /// <summary>
        /// ISO JBIG
        /// </summary>
        JBIG = 34661,
        /// <summary>
        /// SGI Log Luminance RLE
        /// </summary>
        SGILOG = 34676,
        /// <summary>
        /// SGI Log 24-bit packed
        /// </summary>
        SGILOG24 = 34677,
        /// <summary>
        /// Leadtools JPEG2000
        /// </summary>
        JP2000 = 34712,
    };

    /// <summary>
    /// Photometric
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Photometric
    {
        /// <summary>
        /// Min value is white
        /// </summary>
        MINISWHITE = 0,
        /// <summary>
        /// Min value is black
        /// </summary>
        MINISBLACK = 1,
        /// <summary>
        /// RGB color model
        /// </summary>
        RGB = 2,
        /// <summary>
        /// Color map indexed
        /// </summary>
        PALETTE = 3,
        /// <summary>
        /// Holdout mask
        /// </summary>
        MASK = 4, //$
        /// <summary>
        /// Color separations
        /// </summary>
        SEPARATED = 5, //!
        /// <summary>
        /// CCIR 601
        /// </summary>
        YCBCR = 6, //!
        /// <summary>
        /// 1976 CIE L*a*b*
        /// </summary>
        CIELAB = 8, //!
        /// <summary>
        /// ICC L*a*b* [Adobe TIFF Technote 4]
        /// </summary>
        ICCLAB = 9,
        /// <summary>
        /// ITU L*a*b*
        /// </summary>
        ITULAB = 10,
        /// <summary>
        /// CIE Log2(L)
        /// </summary>
        LOGL = 32844,
        /// <summary>
        /// CIE Log2(L) (u',v')
        /// </summary>
        LOGLUV = 32845,
    };

    /// <summary>
    /// Threshold
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Threshold
    {
        /// <summary>
        /// B&amp;W art scan
        /// </summary>
        BILEVEL = 1,
        /// <summary>
        /// Dithered scan
        /// </summary>
        HALFTONE = 2,
        /// <summary>
        /// Usually Floyd-Steinberg
        /// </summary>
        ERRORDIFFUSE = 3,
    };

    /// <summary>
    /// FillOrder
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum FillOrder
    {
        /// <summary>
        /// Most significant -> least
        /// </summary>
        MSB2LSB = 1,
        /// <summary>
        /// Least significant -> most
        /// </summary>
        LSB2MSB = 2,
    };

    /// <summary>
    /// Orientation
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Orientation
    {
        /// <summary>
        /// Row 0 top, Column 0 lhs
        /// </summary>
        TOPLEFT = 1,
        /// <summary>
        /// Row 0 top, Column 0 rhs
        /// </summary>
        TOPRIGHT = 2,
        /// <summary>
        /// Row 0 bottom, Column 0 rhs
        /// </summary>
        BOTRIGHT = 3,
        /// <summary>
        /// Row 0 bottom, Column 0 lhs
        /// </summary>
        BOTLEFT = 4,
        /// <summary>
        /// Row 0 lhs, Column 0 top
        /// </summary>
        LEFTTOP = 5,
        /// <summary>
        /// Row 0 rhs, Column 0 top
        /// </summary>
        RIGHTTOP = 6,
        /// <summary>
        /// Row 0 rhs, Column 0 bottom
        /// </summary>
        RIGHTBOT = 7,
        /// <summary>
        /// Row 0 lhs, Column 0 bottom
        /// </summary>
        LEFTBOT = 8,
    };

    /// <summary>
    /// PlanarConfig
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum PlanarConfig
    {
        /// <summary>
        /// Unknown (uninitialized)
        /// </summary>
        UNKNOWN = 0,
        /// <summary>
        /// Single image plane
        /// </summary>
        CONTIG = 1,
        /// <summary>
        /// Separate planes of data
        /// </summary>
        SEPARATE = 2
    };

    /// <summary>
    /// GrayResponseUnit
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum GrayResponseUnit
    {
        /// <summary>
        /// Tenths of a unit
        /// </summary>
        GRU10S = 1,
        /// <summary>
        /// Hundredths of a unit
        /// </summary>
        GRU100S = 2,
        /// <summary>
        /// Thousandths of a unit
        /// </summary>
        GRU1000S = 3,
        /// <summary>
        /// Ten-thousandths of a unit
        /// </summary>
        GRU10000S = 4,
        /// <summary>
        /// Hundred-thousandths
        /// </summary>
        GRU100000S = 5,
    };

    /// <summary>
    /// Group3Opt
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Group3Opt
    {
        /// <summary>
        /// Unknown (uninitialized)
        /// </summary>
        UNKNOWN = -1,
        /// <summary>
        /// 2-dimensional coding
        /// </summary>
        ENCODING2D = 0x1,
        /// <summary>
        /// Data not compressed
        /// </summary>
        UNCOMPRESSED = 0x2,
        /// <summary>
        /// Fill to byte boundary
        /// </summary>
        FILLBITS = 0x4,
    };

    /// <summary>
    /// ResUnit
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum ResUnit
    {
        /// <summary>
        /// No meaningful units
        /// </summary>
        NONE = 1,
        /// <summary>
        /// English
        /// </summary>
        INCH = 2,
        /// <summary>
        /// Metric
        /// </summary>
        CENTIMETER = 3,
    };

    /// <summary>
    /// ColorResponseUnit
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum ColorResponseUnit
    {
        /// <summary>
        /// Tenths of a unit
        /// </summary>
        CRU10S = 1,
        /// <summary>
        /// Hundredths of a unit
        /// </summary>
        CRU100S = 2,
        /// <summary>
        /// Thousandths of a unit
        /// </summary>
        CRU1000S = 3,
        /// <summary>
        /// Ten-thousandths of a unit
        /// </summary>
        CRU10000S = 4,
        /// <summary>
        /// Hundred-thousandths
        /// </summary>
        CRU100000S = 5,
    };

    /// <summary>
    /// Predictor
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum Predictor
    {
        /// <summary>
        /// No prediction scheme used
        /// </summary>
        NONE = 1,
        /// <summary>
        /// Horizontal differencing
        /// </summary>
        HORIZONTAL = 2,
        /// <summary>
        /// Floating point predictor
        /// </summary>
        FLOATINGPOINT = 3,
    };

    /// <summary>
    /// CleanFaxData
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum CleanFaxData
    {
        /// <summary>
        /// No errors detected
        /// </summary>
        CLEAN = 0,
        /// <summary>
        /// Receiver regenerated lines
        /// </summary>
        REGENERATED = 1,
        /// <summary>
        /// Uncorrected errors exist
        /// </summary>
        UNCLEAN = 2,
    };

    /// <summary>
    /// InkSet
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum InkSet
    {
        /// <summary>
        /// Cyan-magenta-yellow-black color
        /// </summary>
        CMYK = 1,// !
        /// <summary>
        /// Multi-ink or hi-fi color
        /// </summary>
        MULTIINK = 2,// !
    };

    /// <summary>
    /// ExtraSample
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum ExtraSample
    {
        /// <summary>
        /// Unspecified data
        /// </summary>
        UNSPECIFIED = 0,// !
        /// <summary>
        /// Associated alpha data
        /// </summary>
        ASSOCALPHA = 1,// !
        /// <summary>
        /// Unassociated alpha data
        /// </summary>
        UNASSALPHA = 2,// !
    };

    /// <summary>
    /// SampleFormat
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum SampleFormat
    {
        /// <summary>
        /// Unsigned integer data
        /// </summary>
        UINT = 1,// !
        /// <summary>
        /// Signed integer data
        /// </summary>
        INT = 2,// !
        /// <summary>
        /// IEEE floating point data
        /// </summary>
        IEEEFP = 3,// !
        /// <summary>
        /// Untyped data
        /// </summary>
        VOID = 4,// !
        /// <summary>
        /// Complex signed int
        /// </summary>
        COMPLEXINT = 5,// !
        /// <summary>
        /// Complex ieee floating
        /// </summary>
        COMPLEXIEEEFP = 6,// !
    };

    /// <summary>
    /// JpegProc
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum JpegProc
    {
        /// <summary>
        /// Baseline sequential
        /// </summary>
        BASELINE = 1,// !
        /// <summary>
        /// Huffman coded lossless
        /// </summary>
        LOSSLESS = 14,// !
    };

    /// <summary>
    /// YCbCrPosition
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum YCbCrPosition
    {
        /// <summary>
        /// As in PostScript Level 2
        /// </summary>
        CENTERED = 1,// !
        /// <summary>
        /// As in CCIR 601-1
        /// </summary>
        COSITED = 2,// !
    };

    /// <summary>
    /// FaxMode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum FaxMode
    {
        /// <summary>
        /// Default, include RTC
        /// </summary>
        CLASSIC = 0x0000,
        /// <summary>
        /// No RTC at end of data
        /// </summary>
        NORTC = 0x0001,
        /// <summary>
        /// No EOL code at end of row
        /// </summary>
        NOEOL = 0x0002,
        /// <summary>
        /// Byte align row
        /// </summary>
        BYTEALIGN = 0x0004,
        /// <summary>
        /// Word align row
        /// </summary>
        WORDALIGN = 0x0008,
        /// <summary>
        /// TIFF Class F
        /// </summary>
        CLASSF = NORTC,
    };

    /// <summary>
    /// JpegColorMode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum JpegColorMode
    {
        /// <summary>
        /// No conversion (default)
        /// </summary>
        RAW = 0x0000,
        /// <summary>
        /// Do auto conversion
        /// </summary>
        RGB = 0x0001,
    };

    /// <summary>
    /// JpegTablesMode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum JpegTablesMode
    {
        /// <summary>
        /// None
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Include quantization tables
        /// </summary>
        QUANT = 0x0001,
        /// <summary>
        /// Include Huffman tables
        /// </summary>
        HUFF = 0x0002,
    };

    /// <summary>
    /// PixarLogDataFmt
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum PixarLogDataFmt
    {
        /// <summary>
        /// Regular u_char samples
        /// </summary>
        FMT8BIT = 0,
        /// <summary>
        /// ABGR-order u_chars
        /// </summary>
        FMT8BITABGR = 1,
        /// <summary>
        /// 11-bit log-encoded (raw)
        /// </summary>
        FMT11BITLOG = 2,
        /// <summary>
        /// As per PICIO (1.0==2048)
        /// </summary>
        FMT12BITPICIO = 3,
        /// <summary>
        /// Signed short samples
        /// </summary>
        FMT16BIT = 4,
        /// <summary>
        /// IEEE float samples
        /// </summary>
        FMTFLOAT = 5,
    };

    /// <summary>
    /// DCSImagerModel
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum DCSImagerModel
    {
        /// <summary>
        /// M3 chip (1280 x 1024)
        /// </summary>
        M3 = 0,
        /// <summary>
        /// M5 chip (1536 x 1024)
        /// </summary>
        M5 = 1,
        /// <summary>
        /// M6 chip (3072 x 2048)
        /// </summary>
        M6 = 2,
    };

    /// <summary>
    /// DCSImagerFilter
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum DCSImagerFilter
    {
        /// <summary>
        /// Infrared filter
        /// </summary>
        IR = 0,
        /// <summary>
        /// Monochrome filter
        /// </summary>
        MONO = 1,
        /// <summary>
        /// Color filter array
        /// </summary>
        CFA = 2,
        /// <summary>
        /// Other filter
        /// </summary>
        OTHER = 3,
    };

    /// <summary>
    /// DCSInterpMode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum DCSInterpMode
    {
        /// <summary>
        /// Whole image, default
        /// </summary>
        NORMAL = 0x0,
        /// <summary>
        /// Preview of image (384x256)
        /// </summary>
        PREVIEW = 0x1,
    };

    /// <summary>
    /// SGILogDataFmt
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum SGILogDataFmt
    {
        /// <summary>
        /// IEEE float samples
        /// </summary>
        FMTFLOAT = 0,
        /// <summary>
        /// 16-bit samples
        /// </summary>
        FMT16BIT = 1,
        /// <summary>
        /// Uninterpreted data
        /// </summary>
        FMTRAW = 2,
        /// <summary>
        /// 8-bit RGB monitor values
        /// </summary>
        FMT8BIT = 3,
    };

    /// <summary>
    /// SGILogEncode
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum SGILogEncode
    {
        /// <summary>
        /// Do not dither encoded values
        /// </summary>
        NODITHER = 0,
        /// <summary>
        /// Randomly dither encd values
        /// </summary>
        RANDITHER = 1,
    };

    /// <summary>
    /// TiffTag
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    enum TiffTag
    {
        /// <summary>
        /// Tag placeholder
        /// </summary>
        IGNORE = 0,
        /// <summary>
        /// Subfile data descriptor.
        /// </summary>
        SUBFILETYPE = 254,
        /// <summary>
        /// Kind of data in subfile.
        /// </summary>
        OSUBFILETYPE = 255,//+
        /// <summary>
        /// Image width in pixels.
        /// </summary>
        IMAGEWIDTH = 256,
        /// <summary>
        /// Image height in pixels.
        /// </summary>
        IMAGELENGTH = 257,
        /// <summary>
        /// Bits per channel (sample).
        /// </summary>
        BITSPERSAMPLE = 258,
        /// <summary>
        /// Data compression technique. See <see cref="Compression"/>
        /// </summary>
        COMPRESSION = 259,
        /// <summary>
        /// Photometric interpretation. See <see cref="Photometric"/>
        /// </summary>
        PHOTOMETRIC = 262,
        /// <summary>
        /// Thresholding used on data. See <see cref="Threshold"/>
        /// </summary>
        THRESHHOLDING = 263,//+
        /// <summary>
        /// Dithering matrix width
        /// </summary>
        CELLWIDTH = 264,//+
        /// <summary>
        /// Dithering matrix height
        /// </summary>
        CELLLENGTH = 265,//+
        /// <summary>
        /// Data order within a byte. See <see cref="FillOrder"/>
        /// </summary>
        FILLORDER = 266,
        /// <summary>
        /// Name of document which holds for image.
        /// </summary>
        DOCUMENTNAME = 269,
        /// <summary>
        /// Information about image.
        /// </summary>
        IMAGEDESCRIPTION = 270,
        /// <summary>
        /// Scanner manufacturer name.
        /// </summary>
        MAKE = 271,
        /// <summary>
        /// Scanner model name/number
        /// </summary>
        MODEL = 272,
        /// <summary>
        /// Offsets to data strips
        /// </summary>
        STRIPOFFSETS = 273,
        /// <summary>
        /// image orientation. See <see cref="Orientation"/>
        /// </summary>
        ORIENTATION = 274,//+
        /// <summary>
        /// Samples per pixel.
        /// </summary>
        SAMPLESPERPIXEL = 277,
        /// <summary>
        /// Rows per strip of data.
        /// </summary>
        ROWSPERSTRIP = 278,
        /// <summary>
        /// Bytes counts for strips.
        /// </summary>
        STRIPBYTECOUNTS = 279,
        /// <summary>
        /// Minimum sample value.
        /// </summary>
        MINSAMPLEVALUE = 280,//+
        /// <summary>
        /// Maximum sample value.
        /// </summary>
        MAXSAMPLEVALUE = 281,//+
        /// <summary>
        /// Pixels/resolution in x
        /// </summary>
        XRESOLUTION = 282,
        /// <summary>
        /// Pixels/resolution in y
        /// </summary>
        YRESOLUTION = 283,
        /// <summary>
        /// Storage organization. See <see cref="PlanarConfig"/>
        /// </summary>
        PLANARCONFIG = 284,
        /// <summary>
        /// Page name image is from.
        /// </summary>
        PAGENAME = 285,
        /// <summary>
        /// X page offset of image lhs.
        /// </summary>
        XPOSITION = 286,
        /// <summary>
        /// Y page offset of image lhs.
        /// </summary>
        YPOSITION = 287,
        /// <summary>
        /// Byte offset to free block.
        /// </summary>
        FREEOFFSETS = 288,//+
        /// <summary>
        /// Sizes of free blocks.
        /// </summary>
        FREEBYTECOUNTS = 289,//+
        /// <summary>
        /// Gray scale curve accuracy. See <see cref="GrayResponseUnit"/>
        /// </summary>
        GRAYRESPONSEUNIT = 290,//$
        /// <summary>
        /// Gray scale response curve.
        /// </summary>
        GRAYRESPONSECURVE = 291,//$
        /// <summary>
        /// 32 flag bits.
        /// </summary>
        GROUP3OPTIONS = 292,
        /// <summary>
        /// TIFF 6.0 proper name alias.
        /// </summary>
        T4OPTIONS = 292,
        /// <summary>
        /// 32 flag bits.
        /// </summary>
        GROUP4OPTIONS = 293,
        /// <summary>
        /// TIFF 6.0 proper name.
        /// </summary>
        T6OPTIONS = 293,
        /// <summary>
        /// Units of resolutions. See <see cref="ResUnit"/>
        /// </summary>
        RESOLUTIONUNIT = 296,
        /// <summary>
        /// Page numbers of multi-page.
        /// </summary>
        PAGENUMBER = 297,
        /// <summary>
        /// Color curve accuracy. See <see cref="ColorResponseUnit"/>
        /// </summary>
        COLORRESPONSEUNIT = 300,//$
        /// <summary>
        /// Colorimetry info.
        /// </summary>
        TRANSFERFUNCTION = 301,//!
        /// <summary>
        /// Name &amp; release
        /// </summary>
        SOFTWARE = 305,
        /// <summary>
        /// Creation date and time
        /// </summary>
        DATETIME = 306,
        /// <summary>
        /// Creator of image
        /// </summary>
        ARTIST = 315,
        /// <summary>
        /// Machine where created.
        /// </summary>
        HOSTCOMPUTER = 316,
        /// <summary>
        /// Prediction scheme w/ LZW. See <see cref="Predictor"/>
        /// </summary>
        PREDICTOR = 317,
        /// <summary>
        /// Image white point.
        /// </summary>
        WHITEPOINT = 318,
        /// <summary>
        /// Primary chromaticities.
        /// </summary>
        PRIMARYCHROMATICITIES = 319,//!
        /// <summary>
        /// RGB map for pallette image
        /// </summary>
        COLORMAP = 320,
        /// <summary>
        /// Highlight+shadow info
        /// </summary>
        HALFTONEHINTS = 321,//!
        /// <summary>
        /// Tile width in pixels.
        /// </summary>
        TILEWIDTH = 322,//!
        /// <summary>
        /// Tile height in pixels.
        /// </summary>
        TILELENGTH = 323,//!
        /// <summary>
        /// Offsets to data tiles.
        /// </summary>
        TILEOFFSETS = 324,//!
        /// <summary>
        /// Byte counts for tiles.
        /// </summary>
        TILEBYTECOUNTS = 325,//!
        /// <summary>
        /// Lines with wrong pixel count.
        /// </summary>
        BADFAXLINES = 326,
        /// <summary>
        /// Regenerated line info. See <see cref="CleanFaxData"/>
        /// </summary>
        CLEANFAXDATA = 327,
        /// <summary>
        /// Max consecutive bad lines.
        /// </summary>
        CONSECUTIVEBADFAXLINES = 328,
        /// <summary>
        /// Subimage descriptors.
        /// </summary>
        SUBIFD = 330,
        /// <summary>
        /// Inks in separated image. See <see cref="InkSet"/>
        /// </summary>
        INKSET = 332,//!
        /// <summary>
        /// ASCII names of inks.
        /// </summary>
        INKNAMES = 333,//!
        /// <summary>
        /// Number of inks.
        /// </summary>
        NUMBEROFINKS = 334,//!
        /// <summary>
        /// 0% and 100% dot codes.
        /// </summary>
        DOTRANGE = 336,//!
        /// <summary>
        /// Separation target.
        /// </summary>
        TARGETPRINTER = 337,//!
        /// <summary>
        /// Information about extra samples. See <see cref="ExtraSample"/>
        /// </summary>
        EXTRASAMPLES = 338,//!
        /// <summary>
        /// Data sample format. See <see cref="SampleFormat"/>
        /// </summary>
        SAMPLEFORMAT = 339,//!
        /// <summary>
        /// Variable MinSampleValue.
        /// </summary>
        SMINSAMPLEVALUE = 340,//!
        /// <summary>
        /// Variable MaxSampleValue.
        /// </summary>
        SMAXSAMPLEVALUE = 341,//!
        /// <summary>
        /// ClipPath [Adobe TIFF technote 2]
        /// </summary>
        CLIPPATH = 343,//%
        /// <summary>
        /// XClipPathUnits [Adobe TIFF technote 2]
        /// </summary>
        XCLIPPATHUNITS = 344,//%
        /// <summary>
        /// YClipPathUnits [Adobe TIFF technote 2]
        /// </summary>
        YCLIPPATHUNITS = 345,//%
        /// <summary>
        /// Indexed [Adobe TIFF Technote 3]
        /// </summary>
        INDEXED = 346,//%
        /// <summary>
        /// JPEG table stream
        /// </summary>
        JPEGTABLES = 347,//%
        /// <summary>
        /// OPI Proxy [Adobe TIFF technote]
        /// </summary>
        OPIPROXY = 351,//%
        /*
         * Tags 512-521 are obsoleted by Technical Note #2 which specifies a
         * revised JPEG-in-TIFF scheme.
         */
        /// <summary>
        /// JPEG processing algorithm. See <see cref="JpegProc"/>
        /// </summary>
        JPEGPROC = 512,// !
        /// <summary>
        /// Pointer to SOI marker.
        /// </summary>
        JPEGIFOFFSET = 513,// !
        /// <summary>
        /// JFIF stream length
        /// </summary>
        JPEGIFBYTECOUNT = 514,// !
        /// <summary>
        /// Restart interval length.
        /// </summary>
        JPEGRESTARTINTERVAL = 515,// !
        /// <summary>
        /// Lossless proc predictor.
        /// </summary>
        JPEGLOSSLESSPREDICTORS = 517,// !
        /// <summary>
        /// Lossless point transform.
        /// </summary>
        JPEGPOINTTRANSFORM = 518,// !
        /// <summary>
        /// Q matrice offsets.
        /// </summary>
        JPEGQTABLES = 519,// !
        /// <summary>
        /// DCT table offsets
        /// </summary>
        JPEGDCTABLES = 520,// !
        /// <summary>
        /// AC coefficient offsets
        /// </summary>
        JPEGACTABLES = 521,// !
        /// <summary>
        /// RGB -> YCbCr transform
        /// </summary>
        YCBCRCOEFFICIENTS = 529,// !
        /// <summary>
        /// YCbCr subsampling factors
        /// </summary>
        YCBCRSUBSAMPLING = 530,// !
        /// <summary>
        /// Subsample positioning. See <see cref="YCbCrPosition"/>
        /// </summary>
        YCBCRPOSITIONING = 531,// !
        /// <summary>
        /// Colorimetry info.
        /// </summary>
        REFERENCEBLACKWHITE = 532,// !
        /// <summary>
        /// XML packet [Adobe XMP Specification, January 2004]
        /// </summary>
        XMLPACKET = 700,//%
        /// <summary>
        /// OPI ImageID [Adobe TIFF technote]
        /// </summary>
        OPIIMAGEID = 32781,//%
        /* tags 32952-32956 are private tags registered to Island Graphics */
        /// <summary>
        /// Image reference points.
        /// </summary>
        REFPTS = 32953,
        /// <summary>
        /// Region-xform tack point.
        /// </summary>
        REGIONTACKPOINT = 32954,
        /// <summary>
        /// Warp quadrilateral.
        /// </summary>
        REGIONWARPCORNERS = 32955,
        /// <summary>
        /// Affine transformation matrix.
        /// </summary>
        REGIONAFFINE = 32956,
        /* tags 32995-32999 are private tags registered to SGI */
        /// <summary>
        /// Use <see cref="ExtraSample"/>
        /// </summary>
        MATTEING = 32995,//$
        /// <summary>
        /// Use <see cref="SampleFormat"/>
        /// </summary>
        DATATYPE = 32996,//$
        /// <summary>
        /// Z depth of image.
        /// </summary>
        IMAGEDEPTH = 32997,
        /// <summary>
        /// Z depth/data tile.
        /// </summary>
        TILEDEPTH = 32998,
        /* tags 33300-33309 are private tags registered to Pixar */
        /*
         * PIXAR_IMAGEFULLWIDTH and PIXAR_IMAGEFULLLENGTH
         * are set when an image has been cropped out of a larger image.  
         * They reflect the size of the original uncropped image.
         * The XPOSITION and YPOSITION can be used
         * to determine the position of the smaller image in the larger one.
         */
        /// <summary>
        /// Full image size in X.
        /// </summary>
        PIXAR_IMAGEFULLWIDTH = 33300,
        /// <summary>
        /// Full image size in Y.
        /// </summary>
        PIXAR_IMAGEFULLLENGTH = 33301,
        /* Tags 33302-33306 are used to identify special image modes and data
         * used by Pixar's texture formats.
         */
        /// <summary>
        /// Texture map format.
        /// </summary>
        PIXAR_TEXTUREFORMAT = 33302, /* t */
        /// <summary>
        /// S&amp;T wrap modes.
        /// </summary>
        PIXAR_WRAPMODES = 33303,
        /// <summary>
        /// Cotan(fov) for env. maps.
        /// </summary>
        PIXAR_FOVCOT = 33304,
        /// <summary>
        /// 
        /// </summary>
        PIXAR_MATRIX_WORLDTOSCREEN = 33305,
        /// <summary>
        /// 
        /// </summary>
        PIXAR_MATRIX_WORLDTOCAMERA = 33306,
        /* tag 33405 is a private tag registered to Eastman Kodak */
        /// <summary>
        /// Device serial number
        /// </summary>
        WRITERSERIALNUMBER = 33405,
        /* tag 33432 is listed in the 6.0 spec w/ unknown ownership */
        /// <summary>
        /// Copyright string.
        /// </summary>
        COPYRIGHT = 33432,
        /// <summary>
        /// IPTC TAG from RichTIFF specifications.
        /// </summary>
        RICHTIFFIPTC = 33723,
        /* 34016-34029 are reserved for ANSI IT8 TIFF/IT */
        /// <summary>
        /// Site name
        /// </summary>
        IT8SITE = 34016,
        /// <summary>
        /// Solor seq. [RGB,CMYK,etc]
        /// </summary>
        IT8COLORSEQUENCE = 34017,
        /// <summary>
        /// DDES Header
        /// </summary>
        IT8HEADER = 34018,
        /// <summary>
        /// Raster scanline padding
        /// </summary>
        IT8RASTERPADDING = 34019,
        /// <summary>
        /// The number of bits in short run.
        /// </summary>
        IT8BITSPERRUNLENGTH = 34020,
        /// <summary>
        /// The number of bits in long run.
        /// </summary>
        IT8BITSPEREXTENDEDRUNLENGTH = 34021,
        /// <summary>
        /// LW colortable.
        /// </summary>
        IT8COLORTABLE = 34022,
        /// <summary>
        /// BP/BL image color switch.
        /// </summary>
        IT8IMAGECOLORINDICATOR = 34023,
        /// <summary>
        /// BP/BL bg color switch.
        /// </summary>
        IT8BKGCOLORINDICATOR = 34024,
        /// <summary>
        /// BP/BL image color value.
        /// </summary>
        IT8IMAGECOLORVALUE = 34025,
        /// <summary>
        /// BP/BL bg color value.
        /// </summary>
        IT8BKGCOLORVALUE = 34026,
        /// <summary>
        /// MP pixel intensity value
        /// </summary>
        IT8PIXELINTENSITYRANGE = 34027,
        /// <summary>
        /// HC transparency switch.
        /// </summary>
        IT8TRANSPARENCYINDICATOR = 34028,
        /// <summary>
        /// Color characterization table.
        /// </summary>
        IT8COLORCHARACTERIZATION = 34029,
        /// <summary>
        /// HC usage indicator
        /// </summary>
        IT8HCUSAGE = 34030,
        /// <summary>
        /// Trapping indicator (untrapped=0, trapped=1)
        /// </summary>
        IT8TRAPINDICATOR = 34031,
        /// <summary>
        /// CMYK color equivalents.
        /// </summary>
        IT8CMYKEQUIVALENT = 34032,
        /* tags 34232-34236 are private tags registered to Texas Instruments */
        /// <summary>
        /// Sequence Frame Count.
        /// </summary>
        FRAMECOUNT = 34232,
        /// <summary>
        /// Tag 34377 is private tag registered to Adobe for PhotoShop
        /// </summary>
        PHOTOSHOP = 34377,
        /* tags 34665, 34853 and 40965 are documented in EXIF specification */
        /// <summary>
        /// Pointer to EXIF private directory.
        /// </summary>
        EXIFIFD = 34665,
        /* tag 34675 is a private tag registered to Adobe? */
        /// <summary>
        /// ICC profile data.
        /// </summary>
        ICCPROFILE = 34675,
        /* tag 34750 is a private tag registered to Pixel Magic */
        /// <summary>
        /// JBIG options.
        /// </summary>
        JBIGOPTIONS = 34750,
        /// <summary>
        /// Pointer to GPS private directory.
        /// </summary>
        GPSIFD = 34853,
        /* tags 34908-34914 are private tags registered to SGI */
        /// <summary>
        /// Encoded Class 2 ses. params
        /// </summary>
        FAXRECVPARAMS = 34908,
        /// <summary>
        /// Received SubAddr string.
        /// </summary>
        FAXSUBADDRESS = 34909,
        /// <summary>
        /// Receive time (secs).
        /// </summary>
        FAXRECVTIME = 34910,
        /// <summary>
        /// Encoded fax ses. params, Table 2/T.30
        /// </summary>
        FAXDCS = 34911,
        /* tags 37439-37443 are registered to SGI */
        /// <summary>
        /// Sample value to Nits
        /// </summary>
        STONITS = 37439,
        /// <summary>
        /// Private tag registered to FedEx.
        /// </summary>
        FEDEX_EDR = 34929,
        /// <summary>
        /// Pointer to Interoperability private directory.
        /// </summary>
        INTEROPERABILITYIFD = 40965,
        /* Adobe Digital Negative (DNG) format tags */
        /// <summary>
        /// DNG version number.
        /// </summary>
        DNGVERSION = 50706,//&
        /// <summary>
        /// DNG compatibility version.
        /// </summary>
        DNGBACKWARDVERSION = 50707,//&
        /// <summary>
        /// Name for the camera model.
        /// </summary>
        UNIQUECAMERAMODEL = 50708,//&
        /// <summary>
        /// Localized camera model name.
        /// </summary>
        LOCALIZEDCAMERAMODEL = 50709,//&
        /// <summary>
        /// CFAPattern->LinearRaw space mapping.
        /// </summary>
        CFAPLANECOLOR = 50710,//&
        /// <summary>
        /// Spatial layout of the CFA.
        /// </summary>
        CFALAYOUT = 50711,//&
        /// <summary>
        /// Lookup table description.
        /// </summary>
        LINEARIZATIONTABLE = 50712,//&
        /// <summary>
        /// Repeat pattern size for the BlackLevel tag.
        /// </summary>
        BLACKLEVELREPEATDIM = 50713,//&
        /// <summary>
        /// Zero light encoding level
        /// </summary>
        BLACKLEVEL = 50714,//&
        /// <summary>
        /// Zero light encoding level differences (columns)
        /// </summary>
        BLACKLEVELDELTAH = 50715,//&
        /// <summary>
        /// Zero light encoding level differences (rows).
        /// </summary>
        BLACKLEVELDELTAV = 50716,//&
        /// <summary>
        /// Fully saturated encoding level.
        /// </summary>
        WHITELEVEL = 50717,//&
        /// <summary>
        /// Default scale factors.
        /// </summary>
        DEFAULTSCALE = 50718,//&
        /// <summary>
        /// Origin of the final image area.
        /// </summary>
        DEFAULTCROPORIGIN = 50719,//&
        /// <summary>
        /// Size of the final image area.
        /// </summary>
        DEFAULTCROPSIZE = 50720,//&
        /// <summary>
        /// XYZ->reference color space transformation matrix 1
        /// </summary>
        COLORMATRIX1 = 50721,//&
        /// <summary>
        /// XYZ->reference color space transformation matrix 2
        /// </summary>
        COLORMATRIX2 = 50722,//&
        /// <summary>
        /// Calibration matrix 1
        /// </summary>
        CAMERACALIBRATION1 = 50723,//&
        /// <summary>
        /// Calibration matrix 2
        /// </summary>
        CAMERACALIBRATION2 = 50724,//&
        /// <summary>
        /// Dimensionality reduction matrix 1
        /// </summary>
        REDUCTIONMATRIX1 = 50725,//&
        /// <summary>
        /// Dimensionality reduction matrix 2
        /// </summary>
        REDUCTIONMATRIX2 = 50726,//&
        /// <summary>
        /// Gain applied the stored raw values
        /// </summary>
        ANALOGBALANCE = 50727,//&
        /// <summary>
        /// Selected white balance in linear reference space.
        /// </summary>
        ASSHOTNEUTRAL = 50728,//&
        /// <summary>
        /// Selected white balance in x-y chromaticity coordinates.
        /// </summary>
        ASSHOTWHITEXY = 50729,//&
        /// <summary>
        /// How much to move the zero point.
        /// </summary>
        BASELINEEXPOSURE = 50730,//&
        /// <summary>
        /// Relative noise level.
        /// </summary>
        BASELINENOISE = 50731,//&
        /// <summary>
        /// Relative amount of sharpening.
        /// </summary>
        BASELINESHARPNESS = 50732,//&
        /// <summary>
        /// How closely the values of the green pixels in the blue/green rows 
        /// track the values of the green pixels in the red/green rows.
        /// </summary>
        BAYERGREENSPLIT = 50733,//&
        /// <summary>
        /// Non-linear encoding range.
        /// </summary>
        LINEARRESPONSELIMIT = 50734,//&
        /// <summary>
        /// Camera's serial number.
        /// </summary>
        CAMERASERIALNUMBER = 50735,//&
        /// <summary>
        /// Information about the lens.
        /// </summary>
        LENSINFO = 50736,
        /// <summary>
        /// Chroma blur radius.
        /// </summary>
        CHROMABLURRADIUS = 50737,//&
        /// <summary>
        /// Relative strength of the camera's anti-alias filter.
        /// </summary>
        ANTIALIASSTRENGTH = 50738,//&
        /// <summary>
        /// Used by Adobe Camera Raw.
        /// </summary>
        SHADOWSCALE = 50739,//&
        /// <summary>
        /// Manufacturer's private data.
        /// </summary>
        DNGPRIVATEDATA = 50740,//&
        /// <summary>
        /// Whether the EXIF MakerNote tag is safe to preserve 
        /// along with the rest of the EXIF data.
        /// </summary>
        MAKERNOTESAFETY = 50741,//&
        /// <summary>
        /// Illuminant 1.
        /// </summary>
        CALIBRATIONILLUMINANT1 = 50778,//&
        /// <summary>
        /// Illuminant 2
        /// </summary>
        CALIBRATIONILLUMINANT2 = 50779,//&
        /// <summary>
        /// Best quality multiplier
        /// </summary>
        BESTQUALITYSCALE = 50780,//&
        /// <summary>
        /// Unique identifier for the raw image data.
        /// </summary>
        RAWDATAUNIQUEID = 50781,//&
        /// <summary>
        /// File name of the original raw file.
        /// </summary>
        ORIGINALRAWFILENAME = 50827,//&
        /// <summary>
        /// Contents of the original raw file.
        /// </summary>
        ORIGINALRAWFILEDATA = 50828,//&
        /// <summary>
        /// Active (non-masked) pixels of the sensor.
        /// </summary>
        ACTIVEAREA = 50829,//&
        /// <summary>
        /// List of coordinates of fully masked pixels.
        /// </summary>
        MASKEDAREAS = 50830,//&
        /// <summary>
        /// These two tags used to.
        /// </summary>
        ASSHOTICCPROFILE = 50831,//&
        /// <summary>
        /// Map cameras's color space into ICC profile space.
        /// </summary>
        ASSHOTPREPROFILEMATRIX = 50832,
        /// <summary>
        /// 
        /// </summary>
        CURRENTICCPROFILE = 50833,//&
        /// <summary>
        /// 
        /// </summary>
        CURRENTPREPROFILEMATRIX = 50834,//&
        /// <summary>
        /// Undefined tag used by Eastman Kodak, hue shift correction data.
        /// </summary>
        DCSHUESHIFTVALUES = 65535,

        /*
         * The following are ``pseudo tags'' that can be used to control
         * codec-specific functionality.  These tags are not written to file.
         * Note that these values start at 0xffff+1 so that they'll never
         * collide with Aldus-assigned tags.
         *
         * If you want your private pseudo tags ``registered'' (i.e. added to
         * this file), please post a bug report via the tracking system at
         * http://www.remotesensing.org/libtiff/bugs.html with the appropriate
         * C definitions to add.
         */
        /// <summary>
        /// Group 3/4 format control. See <see cref="FaxMode"/>.
        /// </summary>
        FAXMODE = 65536,
        /// <summary>
        /// Compression quality level
        /// </summary>
        /// <remarks>Quality level is on the IJG 0-100 scale.  Default value is 75.</remarks>
        JPEGQUALITY = 65537,
        /// <summary>
        /// Auto RGB&lt;=&gt;YCbCr convert. See <see cref="JpegColorMode"/>.
        /// </summary>
        JPEGCOLORMODE = 65538,
        /// <summary>
        /// What to put in <see cref="JpegTablesMode"/>. See <see cref="JpegTablesMode"/>
        /// </summary>
        /// <remarks>Default is <see cref="JpegTablesMode.QUANT"/> | <see cref="JpegTablesMode.HUFF"/></remarks>
        JPEGTABLESMODE = 65539,
        /// <summary>
        /// G3/G4 fill function.
        /// </summary>
        FAXFILLFUNC = 65540,
        /// <summary>
        /// PixarLogCodec I/O data sz. See <see cref="PixarLogDataFmt"/>
        /// </summary>
        PIXARLOGDATAFMT = 65549,
        /* 65550-65556 are allocated to Oceana Matrix <dev@oceana.com> */
        /// <summary>
        /// Imager mode &amp; filter. See <see cref="DCSImagerFilter"/>
        /// </summary>
        DCSIMAGERTYPE = 65550,
        /// <summary>
        /// Interpolation mode. See <see cref="DCSInterpMode"/>.
        /// </summary>
        DCSINTERPMODE = 65551,
        /// <summary>
        /// Color balance values.
        /// </summary>
        DCSBALANCEARRAY = 65552,
        /// <summary>
        /// Color correction values.
        /// </summary>
        DCSCORRECTMATRIX = 65553,
        /// <summary>
        /// Gamma value.
        /// </summary>
        DCSGAMMA = 65554,
        /// <summary>
        /// Toe &amp; shoulder points.
        /// </summary>
        DCSTOESHOULDERPTS = 65555,
        /// <summary>
        /// Calibration file description.
        /// </summary>
        DCSCALIBRATIONFD = 65556,
        /// <summary>
        /// Compression quality level.
        /// </summary>
        /// <remarks>Quality level is on the ZLIB 1-9 scale. Default value is -1.</remarks>
        ZIPQUALITY = 65557,
        /// <summary>
        /// PixarLog uses same scale.
        /// </summary>
        PIXARLOGQUALITY = 65558,
        /* 65559 is allocated to Oceana Matrix <dev@oceana.com> */
        /// <summary>
        /// Area of image to acquire.
        /// </summary>
        DCSCLIPRECTANGLE = 65559,
        /// <summary>
        /// SGILog user data format. See <see cref="SGILogDataFmt"/>
        /// </summary>
        SGILOGDATAFMT = 65560,
        /// <summary>
        /// SGILog data encoding control. See <see cref="SGILogEncode"/>
        /// </summary>
        SGILOGENCODE = 65561,

        /*
         * EXIF tags
         */
        /// <summary>
        /// Exposure time.
        /// </summary>
        EXIF_EXPOSURETIME = 33434,
        /// <summary>
        /// F number.
        /// </summary>
        EXIF_FNUMBER = 33437,
        /// <summary>
        /// Exposure program.
        /// </summary>
        EXIF_EXPOSUREPROGRAM = 34850,
        /// <summary>
        /// Spectral sensitivity.
        /// </summary>
        EXIF_SPECTRALSENSITIVITY = 34852,
        /// <summary>
        /// ISO speed rating.
        /// </summary>
        EXIF_ISOSPEEDRATINGS = 34855,
        /// <summary>
        /// Optoelectric conversion factor.
        /// </summary>
        EXIF_OECF = 34856,
        /// <summary>
        /// Exif version.
        /// </summary>
        EXIF_EXIFVERSION = 36864,
        /// <summary>
        /// Date and time of original data generation.
        /// </summary>
        EXIF_DATETIMEORIGINAL = 36867,
        /// <summary>
        /// Date and time of digital data generation.
        /// </summary>
        EXIF_DATETIMEDIGITIZED = 36868,
        /// <summary>
        /// Meaning of each component.
        /// </summary>
        EXIF_COMPONENTSCONFIGURATION = 37121,
        /// <summary>
        /// Image compression mode.
        /// </summary>
        EXIF_COMPRESSEDBITSPERPIXEL = 37122,
        /// <summary>
        /// Shutter speed.
        /// </summary>
        EXIF_SHUTTERSPEEDVALUE = 37377,
        /// <summary>
        /// Aperture.
        /// </summary>
        EXIF_APERTUREVALUE = 37378,
        /// <summary>
        /// Brightness.
        /// </summary>
        EXIF_BRIGHTNESSVALUE = 37379,
        /// <summary>
        /// Exposure bias.
        /// </summary>
        EXIF_EXPOSUREBIASVALUE = 37380,
        /// <summary>
        /// Maximum lens aperture.
        /// </summary>
        EXIF_MAXAPERTUREVALUE = 37381,
        /// <summary>
        /// Subject distance.
        /// </summary>
        EXIF_SUBJECTDISTANCE = 37382,
        /// <summary>
        /// Metering mode.
        /// </summary>
        EXIF_METERINGMODE = 37383,
        /// <summary>
        /// Light source.
        /// </summary>
        EXIF_LIGHTSOURCE = 37384,
        /// <summary>
        /// Flash.
        /// </summary>
        EXIF_FLASH = 37385,
        /// <summary>
        /// Lens focal length.
        /// </summary>
        EXIF_FOCALLENGTH = 37386,
        /// <summary>
        /// Subject area.
        /// </summary>
        EXIF_SUBJECTAREA = 37396,
        /// <summary>
        /// Manufacturer notes.
        /// </summary>
        EXIF_MAKERNOTE = 37500,
        /// <summary>
        /// User comments.
        /// </summary>
        EXIF_USERCOMMENT = 37510,
        /// <summary>
        /// DateTime subseconds.
        /// </summary>
        EXIF_SUBSECTIME = 37520,
        /// <summary>
        /// DateTimeOriginal subseconds.
        /// </summary>
        EXIF_SUBSECTIMEORIGINAL = 37521,
        /// <summary>
        /// DateTimeDigitized subseconds.
        /// </summary>
        EXIF_SUBSECTIMEDIGITIZED = 37522,
        /// <summary>
        /// Supported Flashpix version.
        /// </summary>
        EXIF_FLASHPIXVERSION = 40960,
        /// <summary>
        /// Color space information.
        /// </summary>
        EXIF_COLORSPACE = 40961,
        /// <summary>
        /// Valid image width.
        /// </summary>
        EXIF_PIXELXDIMENSION = 40962,
        /// <summary>
        /// Valid image height.
        /// </summary>
        EXIF_PIXELYDIMENSION = 40963,
        /// <summary>
        /// Related audio file.
        /// </summary>
        EXIF_RELATEDSOUNDFILE = 40964,
        /// <summary>
        /// Flash energy.
        /// </summary>
        EXIF_FLASHENERGY = 41483,
        /// <summary>
        /// Spatial frequency response.
        /// </summary>
        EXIF_SPATIALFREQUENCYRESPONSE = 41484,
        /// <summary>
        /// Focal plane X resolution.
        /// </summary>
        EXIF_FOCALPLANEXRESOLUTION = 41486,
        /// <summary>
        /// Focal plane Y resolution.
        /// </summary>
        EXIF_FOCALPLANEYRESOLUTION = 41487,
        /// <summary>
        /// Focal plane resolution unit.
        /// </summary>
        EXIF_FOCALPLANERESOLUTIONUNIT = 41488,
        /// <summary>
        /// Subject location.
        /// </summary>
        EXIF_SUBJECTLOCATION = 41492,
        /// <summary>
        /// Exposure index.
        /// </summary>
        EXIF_EXPOSUREINDEX = 41493,
        /// <summary>
        /// Sensing method.
        /// </summary>
        EXIF_SENSINGMETHOD = 41495,
        /// <summary>
        /// File source.
        /// </summary>
        EXIF_FILESOURCE = 41728,
        /// <summary>
        /// Scene type.
        /// </summary>
        EXIF_SCENETYPE = 41729,
        /// <summary>
        /// CFA pattern.
        /// </summary>
        EXIF_CFAPATTERN = 41730,
        /// <summary>
        /// Custom image processing.
        /// </summary>
        EXIF_CUSTOMRENDERED = 41985,
        /// <summary>
        /// Exposure mode.
        /// </summary>
        EXIF_EXPOSUREMODE = 41986,
        /// <summary>
        /// White balance
        /// </summary>
        EXIF_WHITEBALANCE = 41987,
        /// <summary>
        /// Digital zoom ratio
        /// </summary>
        EXIF_DIGITALZOOMRATIO = 41988,
        /// <summary>
        /// Focal length in 35 mm film
        /// </summary>
        EXIF_FOCALLENGTHIN35MMFILM = 41989,
        /// <summary>
        /// Scene capture type
        /// </summary>
        EXIF_SCENECAPTURETYPE = 41990,
        /// <summary>
        /// Gain control
        /// </summary>
        EXIF_GAINCONTROL = 41991,
        /// <summary>
        /// Contrast
        /// </summary>
        EXIF_CONTRAST = 41992,
        /// <summary>
        /// Saturation
        /// </summary>
        EXIF_SATURATION = 41993,
        /// <summary>
        /// Sharpness
        /// </summary>
        EXIF_SHARPNESS = 41994,
        /// <summary>
        /// Device settings description
        /// </summary>
        EXIF_DEVICESETTINGDESCRIPTION = 41995,
        /// <summary>
        /// Subject distance range
        /// </summary>
        EXIF_SUBJECTDISTANCERANGE = 41996,
        /// <summary>
        /// Unique image ID
        /// </summary>
        EXIF_IMAGEUNIQUEID = 42016,
    };
}
