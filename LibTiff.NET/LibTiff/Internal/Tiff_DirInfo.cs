/* Copyright (C) 2008-2009, Bit Miracle
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
 * Core Directory Tag Support.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        /*
         * NB: NB: THIS ARRAY IS ASSUMED TO BE SORTED BY TAG.
         *       If a tag can have both LONG and SHORT types then the LONG must be
         *       placed before the SHORT for writing to work properly.
         *
         * NOTE: The second field (field_readcount) and third field (field_writecount)
         *       sometimes use the values TIFF_VARIABLE (-1), TIFF_VARIABLE2 (-3)
         *       and TIFFTAG.TIFFTAG_SPP (-2). The macros should be used but would throw off 
         *       the formatting of the code, so please interpret the -1, -2 and -3 
         *       values accordingly.
         */
        static TiffFieldInfo[] tiffFieldInfo = 
        {
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SUBFILETYPE, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_SUBFILETYPE, true, false, "SubfileType"), 
            /* XXX SHORT for compatibility w/ old versions of the library */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SUBFILETYPE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_SUBFILETYPE, true, false, "SubfileType"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_OSUBFILETYPE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_SUBFILETYPE, true, false, "OldSubfileType"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGEWIDTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_IMAGEDIMENSIONS, false, false, "ImageWidth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGEWIDTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IMAGEDIMENSIONS, false, false, "ImageWidth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGELENGTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_IMAGEDIMENSIONS, true, false, "ImageLength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGELENGTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IMAGEDIMENSIONS, true, false, "ImageLength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BITSPERSAMPLE, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_BITSPERSAMPLE, false, false, "BitsPerSample"), 
            /* XXX LONG for compatibility with some broken TIFF writers */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BITSPERSAMPLE, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_BITSPERSAMPLE, false, false, "BitsPerSample"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COMPRESSION, -1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_COMPRESSION, false, false, "Compression"), 
            /* XXX LONG for compatibility with some broken TIFF writers */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COMPRESSION, -1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_COMPRESSION, false, false, "Compression"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PHOTOMETRIC, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_PHOTOMETRIC, false, false, "PhotometricInterpretation"), 
            /* XXX LONG for compatibility with some broken TIFF writers */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PHOTOMETRIC, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_PHOTOMETRIC, false, false, "PhotometricInterpretation"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_THRESHHOLDING, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_THRESHHOLDING, true, false, "Threshholding"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CELLWIDTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IGNORE, true, false, "CellWidth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CELLLENGTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IGNORE, true, false, "CellLength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FILLORDER, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_FILLORDER, false, false, "FillOrder"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DOCUMENTNAME, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "DocumentName"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGEDESCRIPTION, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "ImageDescription"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MAKE, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "Make"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MODEL, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "Model"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_STRIPOFFSETS, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_STRIPOFFSETS, false, false, "StripOffsets"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_STRIPOFFSETS, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_STRIPOFFSETS, false, false, "StripOffsets"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ORIENTATION, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_ORIENTATION, false, false, "Orientation"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_SAMPLESPERPIXEL, false, false, "SamplesPerPixel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ROWSPERSTRIP, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_ROWSPERSTRIP, false, false, "RowsPerStrip"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ROWSPERSTRIP, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_ROWSPERSTRIP, false, false, "RowsPerStrip"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_STRIPBYTECOUNTS, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "StripByteCounts"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_STRIPBYTECOUNTS, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "StripByteCounts"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MINSAMPLEVALUE, -2, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_MINSAMPLEVALUE, true, false, "MinSampleValue"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MAXSAMPLEVALUE, -2, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_MAXSAMPLEVALUE, true, false, "MaxSampleValue"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_XRESOLUTION, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_RESOLUTION, true, false, "XResolution"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YRESOLUTION, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_RESOLUTION, true, false, "YResolution"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PLANARCONFIG, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_PLANARCONFIG, false, false, "PlanarConfiguration"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PAGENAME, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "PageName"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_XPOSITION, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_POSITION, true, false, "XPosition"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YPOSITION, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_POSITION, true, false, "YPosition"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FREEOFFSETS, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_IGNORE, false, false, "FreeOffsets"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_FREEBYTECOUNTS, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_IGNORE, false, false, "FreeByteCounts"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_GRAYRESPONSEUNIT, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IGNORE, true, false, "GrayResponseUnit"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_GRAYRESPONSECURVE, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IGNORE, true, false, "GrayResponseCurve"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_RESOLUTIONUNIT, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_RESOLUTIONUNIT, true, false, "ResolutionUnit"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PAGENUMBER, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_PAGENUMBER, true, false, "PageNumber"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COLORRESPONSEUNIT, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IGNORE, true, false, "ColorResponseUnit"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TRANSFERFUNCTION, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_TRANSFERFUNCTION, true, false, "TransferFunction"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SOFTWARE, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "Software"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DATETIME, 20, 20, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "DateTime"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ARTIST, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "Artist"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_HOSTCOMPUTER, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "HostComputer"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_WHITEPOINT, 2, 2, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "WhitePoint"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PRIMARYCHROMATICITIES, 6, 6, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "PrimaryChromaticities"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COLORMAP, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_COLORMAP, true, false, "ColorMap"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_HALFTONEHINTS, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_HALFTONEHINTS, true, false, "HalftoneHints"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEWIDTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileWidth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEWIDTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileWidth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILELENGTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileLength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILELENGTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileLength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEOFFSETS, -1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_STRIPOFFSETS, false, false, "TileOffsets"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEBYTECOUNTS, -1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "TileByteCounts"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEBYTECOUNTS, -1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "TileByteCounts"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SUBIFD, -1, -1, TiffDataType.TIFF_IFD, FIELD.FIELD_SUBIFD, true, true, "SubIFD"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SUBIFD, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_SUBIFD, true, true, "SubIFD"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_INKSET, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "InkSet"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_INKNAMES, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_INKNAMES, true, true, "InkNames"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_NUMBEROFINKS, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "NumberOfInks"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DOTRANGE, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "DotRange"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DOTRANGE, 2, 2, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, false, "DotRange"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TARGETPRINTER, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "TargetPrinter"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_EXTRASAMPLES, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_EXTRASAMPLES, false, true, "ExtraSamples"), 
            /* XXX for bogus Adobe Photoshop v2.5 files */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_EXTRASAMPLES, -1, -1, TiffDataType.TIFF_BYTE, FIELD.FIELD_EXTRASAMPLES, false, true, "ExtraSamples"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SAMPLEFORMAT, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_SAMPLEFORMAT, false, false, "SampleFormat"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SMINSAMPLEVALUE, -2, -1, TiffDataType.TIFF_ANY, FIELD.FIELD_SMINSAMPLEVALUE, true, false, "SMinSampleValue"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SMAXSAMPLEVALUE, -2, -1, TiffDataType.TIFF_ANY, FIELD.FIELD_SMAXSAMPLEVALUE, true, false, "SMaxSampleValue"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CLIPPATH, -1, -3, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, true, "ClipPath"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_XCLIPPATHUNITS, 1, 1, TiffDataType.TIFF_SLONG, FIELD.FIELD_CUSTOM, false, false, "XClipPathUnits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_XCLIPPATHUNITS, 1, 1, TiffDataType.TIFF_SSHORT, FIELD.FIELD_CUSTOM, false, false, "XClipPathUnits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_XCLIPPATHUNITS, 1, 1, TiffDataType.TIFF_SBYTE, FIELD.FIELD_CUSTOM, false, false, "XClipPathUnits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YCLIPPATHUNITS, 1, 1, TiffDataType.TIFF_SLONG, FIELD.FIELD_CUSTOM, false, false, "YClipPathUnits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YCLIPPATHUNITS, 1, 1, TiffDataType.TIFF_SSHORT, FIELD.FIELD_CUSTOM, false, false, "YClipPathUnits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YCLIPPATHUNITS, 1, 1, TiffDataType.TIFF_SBYTE, FIELD.FIELD_CUSTOM, false, false, "YClipPathUnits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YCBCRCOEFFICIENTS, 3, 3, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "YCbCrCoefficients"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_YCBCRSUBSAMPLING, false, false, "YCbCrSubsampling"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_YCBCRPOSITIONING, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_YCBCRPOSITIONING, false, false, "YCbCrPositioning"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE, 6, 6, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ReferenceBlackWhite"), 
            /* XXX temporarily accept LONG for backwards compatibility */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE, 6, 6, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, true, false, "ReferenceBlackWhite"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_XMLPACKET, -3, -3, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, true, "XMLPacket"), 
            /* begin SGI tags */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MATTEING, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_EXTRASAMPLES, false, false, "Matteing"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DATATYPE, -2, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_SAMPLEFORMAT, false, false, "DataType"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGEDEPTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_IMAGEDEPTH, false, false, "ImageDepth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_IMAGEDEPTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_IMAGEDEPTH, false, false, "ImageDepth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEDEPTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_TILEDEPTH, false, false, "TileDepth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_TILEDEPTH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_TILEDEPTH, false, false, "TileDepth"), 
            /* end SGI tags */
            /* begin Pixar tags */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_IMAGEFULLWIDTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, true, false, "ImageFullWidth"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_IMAGEFULLLENGTH, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, true, false, "ImageFullLength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_TEXTUREFORMAT, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "TextureFormat"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_WRAPMODES, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "TextureWrapModes"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_FOVCOT, 1, 1, TiffDataType.TIFF_FLOAT, FIELD.FIELD_CUSTOM, true, false, "FieldOfViewCotangent"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_MATRIX_WORLDTOSCREEN, 16, 16, TiffDataType.TIFF_FLOAT, FIELD.FIELD_CUSTOM, true, false, "MatrixWorldToScreen"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PIXAR_MATRIX_WORLDTOCAMERA, 16, 16, TiffDataType.TIFF_FLOAT, FIELD.FIELD_CUSTOM, true, false, "MatrixWorldToCamera"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COPYRIGHT, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "Copyright"), 
            /* end Pixar tags */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_RICHTIFFIPTC, -3, -3, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, true, "RichTIFFIPTC"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PHOTOSHOP, -3, -3, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, true, "Photoshop"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_EXIFIFD, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "EXIFIFDOffset"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ICCPROFILE, -3, -3, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "ICC Profile"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_GPSIFD, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "GPSIFDOffset"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_STONITS, 1, 1, TiffDataType.TIFF_DOUBLE, FIELD.FIELD_CUSTOM, false, false, "StoNits"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_INTEROPERABILITYIFD, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "InteroperabilityIFDOffset"), 
            /* begin DNG tags */
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DNGVERSION, 4, 4, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, false, "DNGVersion"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DNGBACKWARDVERSION, 4, 4, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, false, "DNGBackwardVersion"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_UNIQUECAMERAMODEL, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "UniqueCameraModel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_LOCALIZEDCAMERAMODEL, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "LocalizedCameraModel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_LOCALIZEDCAMERAMODEL, -1, -1, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, true, true, "LocalizedCameraModel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CFAPLANECOLOR, -1, -1, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, true, "CFAPlaneColor"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CFALAYOUT, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "CFALayout"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_LINEARIZATIONTABLE, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, true, "LinearizationTable"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BLACKLEVELREPEATDIM, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "BlackLevelRepeatDim"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BLACKLEVEL, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, true, "BlackLevel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BLACKLEVEL, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, true, "BlackLevel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BLACKLEVEL, -1, -1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, true, "BlackLevel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BLACKLEVELDELTAH, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "BlackLevelDeltaH"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BLACKLEVELDELTAV, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "BlackLevelDeltaV"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_WHITELEVEL, -2, -2, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "WhiteLevel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_WHITELEVEL, -2, -2, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "WhiteLevel"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTSCALE, 2, 2, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "DefaultScale"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BESTQUALITYSCALE, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "BestQualityScale"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTCROPORIGIN, 2, 2, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "DefaultCropOrigin"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTCROPORIGIN, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "DefaultCropOrigin"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTCROPORIGIN, 2, 2, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "DefaultCropOrigin"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTCROPSIZE, 2, 2, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "DefaultCropSize"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTCROPSIZE, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "DefaultCropSize"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DEFAULTCROPSIZE, 2, 2, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "DefaultCropSize"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COLORMATRIX1, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ColorMatrix1"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_COLORMATRIX2, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ColorMatrix2"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CAMERACALIBRATION1, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "CameraCalibration1"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CAMERACALIBRATION2, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "CameraCalibration2"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_REDUCTIONMATRIX1, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ReductionMatrix1"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_REDUCTIONMATRIX2, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ReductionMatrix2"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ANALOGBALANCE, -1, -1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, true, "AnalogBalance"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ASSHOTNEUTRAL, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, true, "AsShotNeutral"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ASSHOTNEUTRAL, -1, -1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, true, "AsShotNeutral"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ASSHOTWHITEXY, 2, 2, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "AsShotWhiteXY"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BASELINEEXPOSURE, 1, 1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, false, "BaselineExposure"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BASELINENOISE, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "BaselineNoise"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BASELINESHARPNESS, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "BaselineSharpness"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_BAYERGREENSPLIT, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "BayerGreenSplit"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_LINEARRESPONSELIMIT, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "LinearResponseLimit"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CAMERASERIALNUMBER, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "CameraSerialNumber"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_LENSINFO, 4, 4, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "LensInfo"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CHROMABLURRADIUS, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "ChromaBlurRadius"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ANTIALIASSTRENGTH, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "AntiAliasStrength"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_SHADOWSCALE, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, false, false, "ShadowScale"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_DNGPRIVATEDATA, -1, -1, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, true, "DNGPrivateData"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MAKERNOTESAFETY, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "MakerNoteSafety"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CALIBRATIONILLUMINANT1, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "CalibrationIlluminant1"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CALIBRATIONILLUMINANT2, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "CalibrationIlluminant2"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_RAWDATAUNIQUEID, 16, 16, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, false, false, "RawDataUniqueID"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ORIGINALRAWFILENAME, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "OriginalRawFileName"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ORIGINALRAWFILENAME, -1, -1, TiffDataType.TIFF_BYTE, FIELD.FIELD_CUSTOM, true, true, "OriginalRawFileName"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ORIGINALRAWFILEDATA, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "OriginalRawFileData"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ACTIVEAREA, 4, 4, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, false, "ActiveArea"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ACTIVEAREA, 4, 4, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, false, false, "ActiveArea"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_MASKEDAREAS, -1, -1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, false, true, "MaskedAreas"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ASSHOTICCPROFILE, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "AsShotICCProfile"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_ASSHOTPREPROFILEMATRIX, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "AsShotPreProfileMatrix"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CURRENTICCPROFILE, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "CurrentICCProfile"), 
            new TiffFieldInfo(TIFFTAG.TIFFTAG_CURRENTPREPROFILEMATRIX, -1, -1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "CurrentPreProfileMatrix"),
            /* end DNG tags */
        };

        static TiffFieldInfo[] exifFieldInfo = 
        {
            new TiffFieldInfo(TIFFTAG.EXIFTAG_EXPOSURETIME, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ExposureTime"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FNUMBER, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FNumber"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_EXPOSUREPROGRAM, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "ExposureProgram"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SPECTRALSENSITIVITY, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "SpectralSensitivity"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_ISOSPEEDRATINGS, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, true, "ISOSpeedRatings"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_OECF, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "OptoelectricConversionFactor"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_EXIFVERSION, 4, 4, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "ExifVersion"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_DATETIMEORIGINAL, 20, 20, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "DateTimeOriginal"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_DATETIMEDIGITIZED, 20, 20, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "DateTimeDigitized"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_COMPONENTSCONFIGURATION, 4, 4, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "ComponentsConfiguration"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_COMPRESSEDBITSPERPIXEL, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "CompressedBitsPerPixel"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SHUTTERSPEEDVALUE, 1, 1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, true, false, "ShutterSpeedValue"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_APERTUREVALUE, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ApertureValue"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_BRIGHTNESSVALUE, 1, 1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, true, false, "BrightnessValue"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_EXPOSUREBIASVALUE, 1, 1, TiffDataType.TIFF_SRATIONAL, FIELD.FIELD_CUSTOM, true, false, "ExposureBiasValue"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_MAXAPERTUREVALUE, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "MaxApertureValue"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBJECTDISTANCE, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "SubjectDistance"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_METERINGMODE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "MeteringMode"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_LIGHTSOURCE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "LightSource"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FLASH, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "Flash"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FOCALLENGTH, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FocalLength"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBJECTAREA, -1, -1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, true, "SubjectArea"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_MAKERNOTE, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "MakerNote"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_USERCOMMENT, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "UserComment"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBSECTIME, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "SubSecTime"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBSECTIMEORIGINAL, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "SubSecTimeOriginal"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBSECTIMEDIGITIZED, -1, -1, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "SubSecTimeDigitized"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FLASHPIXVERSION, 4, 4, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "FlashpixVersion"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_COLORSPACE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "ColorSpace"),
            new TiffFieldInfo(TIFFTAG.EXIFTAG_PIXELXDIMENSION, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, true, false, "PixelXDimension"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_PIXELXDIMENSION, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "PixelXDimension"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_PIXELYDIMENSION, 1, 1, TiffDataType.TIFF_LONG, FIELD.FIELD_CUSTOM, true, false, "PixelYDimension"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_PIXELYDIMENSION, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "PixelYDimension"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_RELATEDSOUNDFILE, 13, 13, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "RelatedSoundFile"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FLASHENERGY, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FlashEnergy"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SPATIALFREQUENCYRESPONSE, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "SpatialFrequencyResponse"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FOCALPLANEXRESOLUTION, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FocalPlaneXResolution"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FOCALPLANEYRESOLUTION, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FocalPlaneYResolution"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FOCALPLANERESOLUTIONUNIT, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "FocalPlaneResolutionUnit"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBJECTLOCATION, 2, 2, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "SubjectLocation"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_EXPOSUREINDEX, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ExposureIndex"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SENSINGMETHOD, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "SensingMethod"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FILESOURCE, 1, 1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "FileSource"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SCENETYPE, 1, 1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "SceneType"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_CFAPATTERN, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "CFAPattern"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_CUSTOMRENDERED, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "CustomRendered"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_EXPOSUREMODE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "ExposureMode"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_WHITEBALANCE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "WhiteBalance"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_DIGITALZOOMRATIO, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "DigitalZoomRatio"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_FOCALLENGTHIN35MMFILM, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "FocalLengthIn35mmFilm"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SCENECAPTURETYPE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "SceneCaptureType"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_GAINCONTROL, 1, 1, TiffDataType.TIFF_RATIONAL, FIELD.FIELD_CUSTOM, true, false, "GainControl"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_CONTRAST, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "Contrast"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SATURATION, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "Saturation"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SHARPNESS, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "Sharpness"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_DEVICESETTINGDESCRIPTION, -1, -1, TiffDataType.TIFF_UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "DeviceSettingDescription"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_SUBJECTDISTANCERANGE, 1, 1, TiffDataType.TIFF_SHORT, FIELD.FIELD_CUSTOM, true, false, "SubjectDistanceRange"), 
            new TiffFieldInfo(TIFFTAG.EXIFTAG_IMAGEUNIQUEID, 33, 33, TiffDataType.TIFF_ASCII, FIELD.FIELD_CUSTOM, true, false, "ImageUniqueID")
        };

        private TiffFieldInfo[] getFieldInfo(out int size)
        {
            size = tiffFieldInfo.Length;
            return tiffFieldInfo;
        }

        private TiffFieldInfo[] getExifFieldInfo(out int size)
        {
            size = exifFieldInfo.Length;
            return exifFieldInfo;
        }

        private void setupFieldInfo(TiffFieldInfo[] info, int n)
        {
            m_nfields = 0;
            MergeFieldInfo(info, n);
        }

        //private static bool tagCompare(TiffFieldInfo info)
        //{
        //    if (info != null && info.field_tag == tag && (dt == TiffDataType.TIFF_ANY || dt == info.field_type))
        //        return true;

        //    return false;
        //}

        //private static int tagNameCompare(const void* a, const void* b);

        private void printFieldInfo(Stream fd)
        {
            uint i;

            fprintf(fd, "%s: \n", m_name);
            for (i = 0; i < m_nfields; i++)
            {
                TiffFieldInfo fip = m_fieldinfo[i];
                fprintf(fd, "field[%2d] %5lu, %2d, %2d, %d, %2d, %5s, %5s, %s\n", i, fip.field_tag, fip.field_readcount, fip.field_writecount, fip.field_type, fip.field_bit, fip.field_oktochange ? "TRUE" : "FALSE", fip.field_passcount ? "TRUE" : "FALSE", fip.field_name);
            }
        }

        /*
        * Return nearest TiffDataType to the sample type of an image.
        */
        private TiffDataType sampleToTagType()
        {
            int bps = howMany8(m_dir.td_bitspersample);

            switch (m_dir.td_sampleformat)
            {
                case SAMPLEFORMAT.SAMPLEFORMAT_IEEEFP:
                    return (bps == 4 ? TiffDataType.TIFF_FLOAT : TiffDataType.TIFF_DOUBLE);
                case SAMPLEFORMAT.SAMPLEFORMAT_INT:
                    return (bps <= 1 ? TiffDataType.TIFF_SBYTE : bps <= 2 ? TiffDataType.TIFF_SSHORT : TiffDataType.TIFF_SLONG);
                case SAMPLEFORMAT.SAMPLEFORMAT_UINT:
                    return (bps <= 1 ? TiffDataType.TIFF_BYTE : bps <= 2 ? TiffDataType.TIFF_SHORT : TiffDataType.TIFF_LONG);
                case SAMPLEFORMAT.SAMPLEFORMAT_VOID:
                    return TiffDataType.TIFF_UNDEFINED;
            }
            
            return TiffDataType.TIFF_UNDEFINED;
        }

        private TiffFieldInfo findOrRegisterFieldInfo(TIFFTAG tag, TiffDataType dt)
        {
            TiffFieldInfo fld = FindFieldInfo(tag, dt);
            if (fld == null)
            {
                fld = createAnonFieldInfo(tag, dt);
                TiffFieldInfo[] array = { fld };
                MergeFieldInfo(array, 1);
            }

            return fld;
        }

        private TiffFieldInfo createAnonFieldInfo(TIFFTAG tag, TiffDataType field_type)
        {
            TiffFieldInfo fld = new TiffFieldInfo(tag, TIFF_VARIABLE2, TIFF_VARIABLE2, field_type, FIELD.FIELD_CUSTOM, true, true, null);

            /* note that this name is a special sign to Close() and
             * setupFieldInfo() to free the field
             */
            fld.field_name = string.Format("Tag {0}", tag);
            return fld;
        }
        
        /*
        * Return size of TiffDataType in bytes.
        *
        * XXX: We need a separate function to determine the space needed
        * to store the value. For TiffDataType.TIFF_RATIONAL values DataWidth()
        * returns 8, but we use 4-byte float to represent rationals.
        */
        internal static int dataSize(TiffDataType type)
        {
            switch (type)
            {
                case TiffDataType.TIFF_BYTE:
                case TiffDataType.TIFF_SBYTE:
                case TiffDataType.TIFF_ASCII:
                case TiffDataType.TIFF_UNDEFINED:
                    return 1;
                case TiffDataType.TIFF_SHORT:
                case TiffDataType.TIFF_SSHORT:
                    return 2;
                case TiffDataType.TIFF_LONG:
                case TiffDataType.TIFF_SLONG:
                case TiffDataType.TIFF_FLOAT:
                case TiffDataType.TIFF_IFD:
                case TiffDataType.TIFF_RATIONAL:
                case TiffDataType.TIFF_SRATIONAL:
                    return 4;
                case TiffDataType.TIFF_DOUBLE:
                    return 8;
                default:
                    return 0;
            }
        }
    }
}
