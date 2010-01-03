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
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        /*
         * NB: NB: THIS ARRAY IS ASSUMED TO BE SORTED BY TAG.
         *       If a tag can have both LONG and SHORT types then the LONG must be
         *       placed before the SHORT for writing to work properly.
         *
         * NOTE: The second field (field_readcount) and third field (field_writecount)
         *       sometimes use the values TIFF_VARIABLE (-1), TIFF_VARIABLE2 (-3)
         *       and TIFFTAG_SPP (-2). The macros should be used but would throw off 
         *       the formatting of the code, so please interpret the -1, -2 and -3 
         *       values accordingly.
         */
        static TiffFieldInfo[] tiffFieldInfo = 
        {
            new TiffFieldInfo(TiffTag.SUBFILETYPE, 1, 1, TiffType.LONG, FIELD.FIELD_SUBFILETYPE, true, false, "SubfileType"), 
            /* XXX SHORT for compatibility w/ old versions of the library */
            new TiffFieldInfo(TiffTag.SUBFILETYPE, 1, 1, TiffType.SHORT, FIELD.FIELD_SUBFILETYPE, true, false, "SubfileType"), 
            new TiffFieldInfo(TiffTag.OSUBFILETYPE, 1, 1, TiffType.SHORT, FIELD.FIELD_SUBFILETYPE, true, false, "OldSubfileType"), 
            new TiffFieldInfo(TiffTag.IMAGEWIDTH, 1, 1, TiffType.LONG, FIELD.FIELD_IMAGEDIMENSIONS, false, false, "ImageWidth"), 
            new TiffFieldInfo(TiffTag.IMAGEWIDTH, 1, 1, TiffType.SHORT, FIELD.FIELD_IMAGEDIMENSIONS, false, false, "ImageWidth"), 
            new TiffFieldInfo(TiffTag.IMAGELENGTH, 1, 1, TiffType.LONG, FIELD.FIELD_IMAGEDIMENSIONS, true, false, "ImageLength"), 
            new TiffFieldInfo(TiffTag.IMAGELENGTH, 1, 1, TiffType.SHORT, FIELD.FIELD_IMAGEDIMENSIONS, true, false, "ImageLength"), 
            new TiffFieldInfo(TiffTag.BITSPERSAMPLE, -1, -1, TiffType.SHORT, FIELD.FIELD_BITSPERSAMPLE, false, false, "BitsPerSample"), 
            /* XXX LONG for compatibility with some broken TIFF writers */
            new TiffFieldInfo(TiffTag.BITSPERSAMPLE, -1, -1, TiffType.LONG, FIELD.FIELD_BITSPERSAMPLE, false, false, "BitsPerSample"), 
            new TiffFieldInfo(TiffTag.COMPRESSION, -1, 1, TiffType.SHORT, FIELD.FIELD_COMPRESSION, false, false, "Compression"), 
            /* XXX LONG for compatibility with some broken TIFF writers */
            new TiffFieldInfo(TiffTag.COMPRESSION, -1, 1, TiffType.LONG, FIELD.FIELD_COMPRESSION, false, false, "Compression"), 
            new TiffFieldInfo(TiffTag.PHOTOMETRIC, 1, 1, TiffType.SHORT, FIELD.FIELD_PHOTOMETRIC, false, false, "PhotometricInterpretation"), 
            /* XXX LONG for compatibility with some broken TIFF writers */
            new TiffFieldInfo(TiffTag.PHOTOMETRIC, 1, 1, TiffType.LONG, FIELD.FIELD_PHOTOMETRIC, false, false, "PhotometricInterpretation"), 
            new TiffFieldInfo(TiffTag.THRESHHOLDING, 1, 1, TiffType.SHORT, FIELD.FIELD_THRESHHOLDING, true, false, "Threshholding"), 
            new TiffFieldInfo(TiffTag.CELLWIDTH, 1, 1, TiffType.SHORT, FIELD.FIELD_IGNORE, true, false, "CellWidth"), 
            new TiffFieldInfo(TiffTag.CELLLENGTH, 1, 1, TiffType.SHORT, FIELD.FIELD_IGNORE, true, false, "CellLength"), 
            new TiffFieldInfo(TiffTag.FILLORDER, 1, 1, TiffType.SHORT, FIELD.FIELD_FILLORDER, false, false, "FillOrder"), 
            new TiffFieldInfo(TiffTag.DOCUMENTNAME, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "DocumentName"), 
            new TiffFieldInfo(TiffTag.IMAGEDESCRIPTION, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "ImageDescription"), 
            new TiffFieldInfo(TiffTag.MAKE, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "Make"), 
            new TiffFieldInfo(TiffTag.MODEL, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "Model"), 
            new TiffFieldInfo(TiffTag.STRIPOFFSETS, -1, -1, TiffType.LONG, FIELD.FIELD_STRIPOFFSETS, false, false, "StripOffsets"), 
            new TiffFieldInfo(TiffTag.STRIPOFFSETS, -1, -1, TiffType.SHORT, FIELD.FIELD_STRIPOFFSETS, false, false, "StripOffsets"), 
            new TiffFieldInfo(TiffTag.ORIENTATION, 1, 1, TiffType.SHORT, FIELD.FIELD_ORIENTATION, false, false, "Orientation"), 
            new TiffFieldInfo(TiffTag.SAMPLESPERPIXEL, 1, 1, TiffType.SHORT, FIELD.FIELD_SAMPLESPERPIXEL, false, false, "SamplesPerPixel"), 
            new TiffFieldInfo(TiffTag.ROWSPERSTRIP, 1, 1, TiffType.LONG, FIELD.FIELD_ROWSPERSTRIP, false, false, "RowsPerStrip"), 
            new TiffFieldInfo(TiffTag.ROWSPERSTRIP, 1, 1, TiffType.SHORT, FIELD.FIELD_ROWSPERSTRIP, false, false, "RowsPerStrip"), 
            new TiffFieldInfo(TiffTag.STRIPBYTECOUNTS, -1, -1, TiffType.LONG, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "StripByteCounts"), 
            new TiffFieldInfo(TiffTag.STRIPBYTECOUNTS, -1, -1, TiffType.SHORT, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "StripByteCounts"), 
            new TiffFieldInfo(TiffTag.MINSAMPLEVALUE, -2, -1, TiffType.SHORT, FIELD.FIELD_MINSAMPLEVALUE, true, false, "MinSampleValue"), 
            new TiffFieldInfo(TiffTag.MAXSAMPLEVALUE, -2, -1, TiffType.SHORT, FIELD.FIELD_MAXSAMPLEVALUE, true, false, "MaxSampleValue"), 
            new TiffFieldInfo(TiffTag.XRESOLUTION, 1, 1, TiffType.RATIONAL, FIELD.FIELD_RESOLUTION, true, false, "XResolution"), 
            new TiffFieldInfo(TiffTag.YRESOLUTION, 1, 1, TiffType.RATIONAL, FIELD.FIELD_RESOLUTION, true, false, "YResolution"), 
            new TiffFieldInfo(TiffTag.PLANARCONFIG, 1, 1, TiffType.SHORT, FIELD.FIELD_PLANARCONFIG, false, false, "PlanarConfiguration"), 
            new TiffFieldInfo(TiffTag.PAGENAME, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "PageName"), 
            new TiffFieldInfo(TiffTag.XPOSITION, 1, 1, TiffType.RATIONAL, FIELD.FIELD_POSITION, true, false, "XPosition"), 
            new TiffFieldInfo(TiffTag.YPOSITION, 1, 1, TiffType.RATIONAL, FIELD.FIELD_POSITION, true, false, "YPosition"), 
            new TiffFieldInfo(TiffTag.FREEOFFSETS, -1, -1, TiffType.LONG, FIELD.FIELD_IGNORE, false, false, "FreeOffsets"), 
            new TiffFieldInfo(TiffTag.FREEBYTECOUNTS, -1, -1, TiffType.LONG, FIELD.FIELD_IGNORE, false, false, "FreeByteCounts"), 
            new TiffFieldInfo(TiffTag.GRAYRESPONSEUNIT, 1, 1, TiffType.SHORT, FIELD.FIELD_IGNORE, true, false, "GrayResponseUnit"), 
            new TiffFieldInfo(TiffTag.GRAYRESPONSECURVE, -1, -1, TiffType.SHORT, FIELD.FIELD_IGNORE, true, false, "GrayResponseCurve"), 
            new TiffFieldInfo(TiffTag.RESOLUTIONUNIT, 1, 1, TiffType.SHORT, FIELD.FIELD_RESOLUTIONUNIT, true, false, "ResolutionUnit"), 
            new TiffFieldInfo(TiffTag.PAGENUMBER, 2, 2, TiffType.SHORT, FIELD.FIELD_PAGENUMBER, true, false, "PageNumber"), 
            new TiffFieldInfo(TiffTag.COLORRESPONSEUNIT, 1, 1, TiffType.SHORT, FIELD.FIELD_IGNORE, true, false, "ColorResponseUnit"), 
            new TiffFieldInfo(TiffTag.TRANSFERFUNCTION, -1, -1, TiffType.SHORT, FIELD.FIELD_TRANSFERFUNCTION, true, false, "TransferFunction"), 
            new TiffFieldInfo(TiffTag.SOFTWARE, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "Software"), 
            new TiffFieldInfo(TiffTag.DATETIME, 20, 20, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "DateTime"), 
            new TiffFieldInfo(TiffTag.ARTIST, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "Artist"), 
            new TiffFieldInfo(TiffTag.HOSTCOMPUTER, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "HostComputer"), 
            new TiffFieldInfo(TiffTag.WHITEPOINT, 2, 2, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "WhitePoint"), 
            new TiffFieldInfo(TiffTag.PRIMARYCHROMATICITIES, 6, 6, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "PrimaryChromaticities"), 
            new TiffFieldInfo(TiffTag.COLORMAP, -1, -1, TiffType.SHORT, FIELD.FIELD_COLORMAP, true, false, "ColorMap"), 
            new TiffFieldInfo(TiffTag.HALFTONEHINTS, 2, 2, TiffType.SHORT, FIELD.FIELD_HALFTONEHINTS, true, false, "HalftoneHints"), 
            new TiffFieldInfo(TiffTag.TILEWIDTH, 1, 1, TiffType.LONG, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileWidth"), 
            new TiffFieldInfo(TiffTag.TILEWIDTH, 1, 1, TiffType.SHORT, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileWidth"), 
            new TiffFieldInfo(TiffTag.TILELENGTH, 1, 1, TiffType.LONG, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileLength"), 
            new TiffFieldInfo(TiffTag.TILELENGTH, 1, 1, TiffType.SHORT, FIELD.FIELD_TILEDIMENSIONS, false, false, "TileLength"), 
            new TiffFieldInfo(TiffTag.TILEOFFSETS, -1, 1, TiffType.LONG, FIELD.FIELD_STRIPOFFSETS, false, false, "TileOffsets"), 
            new TiffFieldInfo(TiffTag.TILEBYTECOUNTS, -1, 1, TiffType.LONG, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "TileByteCounts"), 
            new TiffFieldInfo(TiffTag.TILEBYTECOUNTS, -1, 1, TiffType.SHORT, FIELD.FIELD_STRIPBYTECOUNTS, false, false, "TileByteCounts"), 
            new TiffFieldInfo(TiffTag.SUBIFD, -1, -1, TiffType.IFD, FIELD.FIELD_SUBIFD, true, true, "SubIFD"), 
            new TiffFieldInfo(TiffTag.SUBIFD, -1, -1, TiffType.LONG, FIELD.FIELD_SUBIFD, true, true, "SubIFD"), 
            new TiffFieldInfo(TiffTag.INKSET, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "InkSet"), 
            new TiffFieldInfo(TiffTag.INKNAMES, -1, -1, TiffType.ASCII, FIELD.FIELD_INKNAMES, true, true, "InkNames"), 
            new TiffFieldInfo(TiffTag.NUMBEROFINKS, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "NumberOfInks"), 
            new TiffFieldInfo(TiffTag.DOTRANGE, 2, 2, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "DotRange"), 
            new TiffFieldInfo(TiffTag.DOTRANGE, 2, 2, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, false, "DotRange"), 
            new TiffFieldInfo(TiffTag.TARGETPRINTER, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "TargetPrinter"), 
            new TiffFieldInfo(TiffTag.EXTRASAMPLES, -1, -1, TiffType.SHORT, FIELD.FIELD_EXTRASAMPLES, false, true, "ExtraSamples"), 
            /* XXX for bogus Adobe Photoshop v2.5 files */
            new TiffFieldInfo(TiffTag.EXTRASAMPLES, -1, -1, TiffType.BYTE, FIELD.FIELD_EXTRASAMPLES, false, true, "ExtraSamples"), 
            new TiffFieldInfo(TiffTag.SAMPLEFORMAT, -1, -1, TiffType.SHORT, FIELD.FIELD_SAMPLEFORMAT, false, false, "SampleFormat"), 
            new TiffFieldInfo(TiffTag.SMINSAMPLEVALUE, -2, -1, TiffType.ANY, FIELD.FIELD_SMINSAMPLEVALUE, true, false, "SMinSampleValue"), 
            new TiffFieldInfo(TiffTag.SMAXSAMPLEVALUE, -2, -1, TiffType.ANY, FIELD.FIELD_SMAXSAMPLEVALUE, true, false, "SMaxSampleValue"), 
            new TiffFieldInfo(TiffTag.CLIPPATH, -1, -3, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, true, "ClipPath"), 
            new TiffFieldInfo(TiffTag.XCLIPPATHUNITS, 1, 1, TiffType.SLONG, FIELD.FIELD_CUSTOM, false, false, "XClipPathUnits"), 
            new TiffFieldInfo(TiffTag.XCLIPPATHUNITS, 1, 1, TiffType.SSHORT, FIELD.FIELD_CUSTOM, false, false, "XClipPathUnits"), 
            new TiffFieldInfo(TiffTag.XCLIPPATHUNITS, 1, 1, TiffType.SBYTE, FIELD.FIELD_CUSTOM, false, false, "XClipPathUnits"), 
            new TiffFieldInfo(TiffTag.YCLIPPATHUNITS, 1, 1, TiffType.SLONG, FIELD.FIELD_CUSTOM, false, false, "YClipPathUnits"), 
            new TiffFieldInfo(TiffTag.YCLIPPATHUNITS, 1, 1, TiffType.SSHORT, FIELD.FIELD_CUSTOM, false, false, "YClipPathUnits"), 
            new TiffFieldInfo(TiffTag.YCLIPPATHUNITS, 1, 1, TiffType.SBYTE, FIELD.FIELD_CUSTOM, false, false, "YClipPathUnits"), 
            new TiffFieldInfo(TiffTag.YCBCRCOEFFICIENTS, 3, 3, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "YCbCrCoefficients"), 
            new TiffFieldInfo(TiffTag.YCBCRSUBSAMPLING, 2, 2, TiffType.SHORT, FIELD.FIELD_YCBCRSUBSAMPLING, false, false, "YCbCrSubsampling"), 
            new TiffFieldInfo(TiffTag.YCBCRPOSITIONING, 1, 1, TiffType.SHORT, FIELD.FIELD_YCBCRPOSITIONING, false, false, "YCbCrPositioning"), 
            new TiffFieldInfo(TiffTag.REFERENCEBLACKWHITE, 6, 6, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ReferenceBlackWhite"), 
            /* XXX temporarily accept LONG for backwards compatibility */
            new TiffFieldInfo(TiffTag.REFERENCEBLACKWHITE, 6, 6, TiffType.LONG, FIELD.FIELD_CUSTOM, true, false, "ReferenceBlackWhite"), 
            new TiffFieldInfo(TiffTag.XMLPACKET, -3, -3, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, true, "XMLPacket"), 
            /* begin SGI tags */
            new TiffFieldInfo(TiffTag.MATTEING, 1, 1, TiffType.SHORT, FIELD.FIELD_EXTRASAMPLES, false, false, "Matteing"), 
            new TiffFieldInfo(TiffTag.DATATYPE, -2, -1, TiffType.SHORT, FIELD.FIELD_SAMPLEFORMAT, false, false, "DataType"), 
            new TiffFieldInfo(TiffTag.IMAGEDEPTH, 1, 1, TiffType.LONG, FIELD.FIELD_IMAGEDEPTH, false, false, "ImageDepth"), 
            new TiffFieldInfo(TiffTag.IMAGEDEPTH, 1, 1, TiffType.SHORT, FIELD.FIELD_IMAGEDEPTH, false, false, "ImageDepth"), 
            new TiffFieldInfo(TiffTag.TILEDEPTH, 1, 1, TiffType.LONG, FIELD.FIELD_TILEDEPTH, false, false, "TileDepth"), 
            new TiffFieldInfo(TiffTag.TILEDEPTH, 1, 1, TiffType.SHORT, FIELD.FIELD_TILEDEPTH, false, false, "TileDepth"), 
            /* end SGI tags */
            /* begin Pixar tags */
            new TiffFieldInfo(TiffTag.PIXAR_IMAGEFULLWIDTH, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, true, false, "ImageFullWidth"), 
            new TiffFieldInfo(TiffTag.PIXAR_IMAGEFULLLENGTH, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, true, false, "ImageFullLength"), 
            new TiffFieldInfo(TiffTag.PIXAR_TEXTUREFORMAT, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "TextureFormat"), 
            new TiffFieldInfo(TiffTag.PIXAR_WRAPMODES, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "TextureWrapModes"), 
            new TiffFieldInfo(TiffTag.PIXAR_FOVCOT, 1, 1, TiffType.FLOAT, FIELD.FIELD_CUSTOM, true, false, "FieldOfViewCotangent"), 
            new TiffFieldInfo(TiffTag.PIXAR_MATRIX_WORLDTOSCREEN, 16, 16, TiffType.FLOAT, FIELD.FIELD_CUSTOM, true, false, "MatrixWorldToScreen"), 
            new TiffFieldInfo(TiffTag.PIXAR_MATRIX_WORLDTOCAMERA, 16, 16, TiffType.FLOAT, FIELD.FIELD_CUSTOM, true, false, "MatrixWorldToCamera"), 
            new TiffFieldInfo(TiffTag.COPYRIGHT, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "Copyright"), 
            /* end Pixar tags */
            new TiffFieldInfo(TiffTag.RICHTIFFIPTC, -3, -3, TiffType.LONG, FIELD.FIELD_CUSTOM, false, true, "RichTIFFIPTC"), 
            new TiffFieldInfo(TiffTag.PHOTOSHOP, -3, -3, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, true, "Photoshop"), 
            new TiffFieldInfo(TiffTag.EXIFIFD, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "EXIFIFDOffset"), 
            new TiffFieldInfo(TiffTag.ICCPROFILE, -3, -3, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "ICC Profile"), 
            new TiffFieldInfo(TiffTag.GPSIFD, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "GPSIFDOffset"), 
            new TiffFieldInfo(TiffTag.STONITS, 1, 1, TiffType.DOUBLE, FIELD.FIELD_CUSTOM, false, false, "StoNits"), 
            new TiffFieldInfo(TiffTag.INTEROPERABILITYIFD, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "InteroperabilityIFDOffset"), 
            /* begin DNG tags */
            new TiffFieldInfo(TiffTag.DNGVERSION, 4, 4, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, false, "DNGVersion"), 
            new TiffFieldInfo(TiffTag.DNGBACKWARDVERSION, 4, 4, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, false, "DNGBackwardVersion"), 
            new TiffFieldInfo(TiffTag.UNIQUECAMERAMODEL, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "UniqueCameraModel"), 
            new TiffFieldInfo(TiffTag.LOCALIZEDCAMERAMODEL, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "LocalizedCameraModel"), 
            new TiffFieldInfo(TiffTag.LOCALIZEDCAMERAMODEL, -1, -1, TiffType.BYTE, FIELD.FIELD_CUSTOM, true, true, "LocalizedCameraModel"), 
            new TiffFieldInfo(TiffTag.CFAPLANECOLOR, -1, -1, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, true, "CFAPlaneColor"), 
            new TiffFieldInfo(TiffTag.CFALAYOUT, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "CFALayout"), 
            new TiffFieldInfo(TiffTag.LINEARIZATIONTABLE, -1, -1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, true, "LinearizationTable"), 
            new TiffFieldInfo(TiffTag.BLACKLEVELREPEATDIM, 2, 2, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "BlackLevelRepeatDim"), 
            new TiffFieldInfo(TiffTag.BLACKLEVEL, -1, -1, TiffType.LONG, FIELD.FIELD_CUSTOM, false, true, "BlackLevel"), 
            new TiffFieldInfo(TiffTag.BLACKLEVEL, -1, -1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, true, "BlackLevel"), 
            new TiffFieldInfo(TiffTag.BLACKLEVEL, -1, -1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, true, "BlackLevel"), 
            new TiffFieldInfo(TiffTag.BLACKLEVELDELTAH, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "BlackLevelDeltaH"), 
            new TiffFieldInfo(TiffTag.BLACKLEVELDELTAV, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "BlackLevelDeltaV"), 
            new TiffFieldInfo(TiffTag.WHITELEVEL, -2, -2, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "WhiteLevel"), 
            new TiffFieldInfo(TiffTag.WHITELEVEL, -2, -2, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "WhiteLevel"), 
            new TiffFieldInfo(TiffTag.DEFAULTSCALE, 2, 2, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "DefaultScale"), 
            new TiffFieldInfo(TiffTag.BESTQUALITYSCALE, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "BestQualityScale"), 
            new TiffFieldInfo(TiffTag.DEFAULTCROPORIGIN, 2, 2, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "DefaultCropOrigin"), 
            new TiffFieldInfo(TiffTag.DEFAULTCROPORIGIN, 2, 2, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "DefaultCropOrigin"), 
            new TiffFieldInfo(TiffTag.DEFAULTCROPORIGIN, 2, 2, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "DefaultCropOrigin"), 
            new TiffFieldInfo(TiffTag.DEFAULTCROPSIZE, 2, 2, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "DefaultCropSize"), 
            new TiffFieldInfo(TiffTag.DEFAULTCROPSIZE, 2, 2, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "DefaultCropSize"), 
            new TiffFieldInfo(TiffTag.DEFAULTCROPSIZE, 2, 2, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "DefaultCropSize"), 
            new TiffFieldInfo(TiffTag.COLORMATRIX1, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ColorMatrix1"), 
            new TiffFieldInfo(TiffTag.COLORMATRIX2, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ColorMatrix2"), 
            new TiffFieldInfo(TiffTag.CAMERACALIBRATION1, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "CameraCalibration1"), 
            new TiffFieldInfo(TiffTag.CAMERACALIBRATION2, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "CameraCalibration2"), 
            new TiffFieldInfo(TiffTag.REDUCTIONMATRIX1, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ReductionMatrix1"), 
            new TiffFieldInfo(TiffTag.REDUCTIONMATRIX2, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "ReductionMatrix2"), 
            new TiffFieldInfo(TiffTag.ANALOGBALANCE, -1, -1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, true, "AnalogBalance"), 
            new TiffFieldInfo(TiffTag.ASSHOTNEUTRAL, -1, -1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, true, "AsShotNeutral"), 
            new TiffFieldInfo(TiffTag.ASSHOTNEUTRAL, -1, -1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, true, "AsShotNeutral"), 
            new TiffFieldInfo(TiffTag.ASSHOTWHITEXY, 2, 2, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "AsShotWhiteXY"), 
            new TiffFieldInfo(TiffTag.BASELINEEXPOSURE, 1, 1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, false, "BaselineExposure"), 
            new TiffFieldInfo(TiffTag.BASELINENOISE, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "BaselineNoise"), 
            new TiffFieldInfo(TiffTag.BASELINESHARPNESS, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "BaselineSharpness"), 
            new TiffFieldInfo(TiffTag.BAYERGREENSPLIT, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "BayerGreenSplit"), 
            new TiffFieldInfo(TiffTag.LINEARRESPONSELIMIT, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "LinearResponseLimit"), 
            new TiffFieldInfo(TiffTag.CAMERASERIALNUMBER, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "CameraSerialNumber"), 
            new TiffFieldInfo(TiffTag.LENSINFO, 4, 4, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "LensInfo"), 
            new TiffFieldInfo(TiffTag.CHROMABLURRADIUS, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "ChromaBlurRadius"), 
            new TiffFieldInfo(TiffTag.ANTIALIASSTRENGTH, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "AntiAliasStrength"), 
            new TiffFieldInfo(TiffTag.SHADOWSCALE, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, false, false, "ShadowScale"), 
            new TiffFieldInfo(TiffTag.DNGPRIVATEDATA, -1, -1, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, true, "DNGPrivateData"), 
            new TiffFieldInfo(TiffTag.MAKERNOTESAFETY, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "MakerNoteSafety"), 
            new TiffFieldInfo(TiffTag.CALIBRATIONILLUMINANT1, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "CalibrationIlluminant1"), 
            new TiffFieldInfo(TiffTag.CALIBRATIONILLUMINANT2, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "CalibrationIlluminant2"), 
            new TiffFieldInfo(TiffTag.RAWDATAUNIQUEID, 16, 16, TiffType.BYTE, FIELD.FIELD_CUSTOM, false, false, "RawDataUniqueID"), 
            new TiffFieldInfo(TiffTag.ORIGINALRAWFILENAME, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "OriginalRawFileName"), 
            new TiffFieldInfo(TiffTag.ORIGINALRAWFILENAME, -1, -1, TiffType.BYTE, FIELD.FIELD_CUSTOM, true, true, "OriginalRawFileName"), 
            new TiffFieldInfo(TiffTag.ORIGINALRAWFILEDATA, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "OriginalRawFileData"), 
            new TiffFieldInfo(TiffTag.ACTIVEAREA, 4, 4, TiffType.LONG, FIELD.FIELD_CUSTOM, false, false, "ActiveArea"), 
            new TiffFieldInfo(TiffTag.ACTIVEAREA, 4, 4, TiffType.SHORT, FIELD.FIELD_CUSTOM, false, false, "ActiveArea"), 
            new TiffFieldInfo(TiffTag.MASKEDAREAS, -1, -1, TiffType.LONG, FIELD.FIELD_CUSTOM, false, true, "MaskedAreas"), 
            new TiffFieldInfo(TiffTag.ASSHOTICCPROFILE, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "AsShotICCProfile"), 
            new TiffFieldInfo(TiffTag.ASSHOTPREPROFILEMATRIX, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "AsShotPreProfileMatrix"), 
            new TiffFieldInfo(TiffTag.CURRENTICCPROFILE, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, false, true, "CurrentICCProfile"), 
            new TiffFieldInfo(TiffTag.CURRENTPREPROFILEMATRIX, -1, -1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, false, true, "CurrentPreProfileMatrix"),
            /* end DNG tags */
        };

        static TiffFieldInfo[] exifFieldInfo = 
        {
            new TiffFieldInfo(TiffTag.EXIF_EXPOSURETIME, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ExposureTime"), 
            new TiffFieldInfo(TiffTag.EXIF_FNUMBER, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FNumber"), 
            new TiffFieldInfo(TiffTag.EXIF_EXPOSUREPROGRAM, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "ExposureProgram"), 
            new TiffFieldInfo(TiffTag.EXIF_SPECTRALSENSITIVITY, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "SpectralSensitivity"), 
            new TiffFieldInfo(TiffTag.EXIF_ISOSPEEDRATINGS, -1, -1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, true, "ISOSpeedRatings"), 
            new TiffFieldInfo(TiffTag.EXIF_OECF, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "OptoelectricConversionFactor"), 
            new TiffFieldInfo(TiffTag.EXIF_EXIFVERSION, 4, 4, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "ExifVersion"), 
            new TiffFieldInfo(TiffTag.EXIF_DATETIMEORIGINAL, 20, 20, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "DateTimeOriginal"), 
            new TiffFieldInfo(TiffTag.EXIF_DATETIMEDIGITIZED, 20, 20, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "DateTimeDigitized"), 
            new TiffFieldInfo(TiffTag.EXIF_COMPONENTSCONFIGURATION, 4, 4, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "ComponentsConfiguration"), 
            new TiffFieldInfo(TiffTag.EXIF_COMPRESSEDBITSPERPIXEL, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "CompressedBitsPerPixel"), 
            new TiffFieldInfo(TiffTag.EXIF_SHUTTERSPEEDVALUE, 1, 1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, true, false, "ShutterSpeedValue"), 
            new TiffFieldInfo(TiffTag.EXIF_APERTUREVALUE, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ApertureValue"), 
            new TiffFieldInfo(TiffTag.EXIF_BRIGHTNESSVALUE, 1, 1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, true, false, "BrightnessValue"), 
            new TiffFieldInfo(TiffTag.EXIF_EXPOSUREBIASVALUE, 1, 1, TiffType.SRATIONAL, FIELD.FIELD_CUSTOM, true, false, "ExposureBiasValue"), 
            new TiffFieldInfo(TiffTag.EXIF_MAXAPERTUREVALUE, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "MaxApertureValue"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBJECTDISTANCE, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "SubjectDistance"), 
            new TiffFieldInfo(TiffTag.EXIF_METERINGMODE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "MeteringMode"), 
            new TiffFieldInfo(TiffTag.EXIF_LIGHTSOURCE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "LightSource"), 
            new TiffFieldInfo(TiffTag.EXIF_FLASH, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "Flash"), 
            new TiffFieldInfo(TiffTag.EXIF_FOCALLENGTH, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FocalLength"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBJECTAREA, -1, -1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, true, "SubjectArea"), 
            new TiffFieldInfo(TiffTag.EXIF_MAKERNOTE, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "MakerNote"), 
            new TiffFieldInfo(TiffTag.EXIF_USERCOMMENT, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "UserComment"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBSECTIME, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "SubSecTime"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBSECTIMEORIGINAL, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "SubSecTimeOriginal"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBSECTIMEDIGITIZED, -1, -1, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "SubSecTimeDigitized"), 
            new TiffFieldInfo(TiffTag.EXIF_FLASHPIXVERSION, 4, 4, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "FlashpixVersion"), 
            new TiffFieldInfo(TiffTag.EXIF_COLORSPACE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "ColorSpace"),
            new TiffFieldInfo(TiffTag.EXIF_PIXELXDIMENSION, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, true, false, "PixelXDimension"), 
            new TiffFieldInfo(TiffTag.EXIF_PIXELXDIMENSION, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "PixelXDimension"), 
            new TiffFieldInfo(TiffTag.EXIF_PIXELYDIMENSION, 1, 1, TiffType.LONG, FIELD.FIELD_CUSTOM, true, false, "PixelYDimension"), 
            new TiffFieldInfo(TiffTag.EXIF_PIXELYDIMENSION, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "PixelYDimension"), 
            new TiffFieldInfo(TiffTag.EXIF_RELATEDSOUNDFILE, 13, 13, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "RelatedSoundFile"), 
            new TiffFieldInfo(TiffTag.EXIF_FLASHENERGY, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FlashEnergy"), 
            new TiffFieldInfo(TiffTag.EXIF_SPATIALFREQUENCYRESPONSE, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "SpatialFrequencyResponse"), 
            new TiffFieldInfo(TiffTag.EXIF_FOCALPLANEXRESOLUTION, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FocalPlaneXResolution"), 
            new TiffFieldInfo(TiffTag.EXIF_FOCALPLANEYRESOLUTION, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "FocalPlaneYResolution"), 
            new TiffFieldInfo(TiffTag.EXIF_FOCALPLANERESOLUTIONUNIT, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "FocalPlaneResolutionUnit"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBJECTLOCATION, 2, 2, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "SubjectLocation"), 
            new TiffFieldInfo(TiffTag.EXIF_EXPOSUREINDEX, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "ExposureIndex"), 
            new TiffFieldInfo(TiffTag.EXIF_SENSINGMETHOD, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "SensingMethod"), 
            new TiffFieldInfo(TiffTag.EXIF_FILESOURCE, 1, 1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "FileSource"), 
            new TiffFieldInfo(TiffTag.EXIF_SCENETYPE, 1, 1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, false, "SceneType"), 
            new TiffFieldInfo(TiffTag.EXIF_CFAPATTERN, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "CFAPattern"), 
            new TiffFieldInfo(TiffTag.EXIF_CUSTOMRENDERED, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "CustomRendered"), 
            new TiffFieldInfo(TiffTag.EXIF_EXPOSUREMODE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "ExposureMode"), 
            new TiffFieldInfo(TiffTag.EXIF_WHITEBALANCE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "WhiteBalance"), 
            new TiffFieldInfo(TiffTag.EXIF_DIGITALZOOMRATIO, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "DigitalZoomRatio"), 
            new TiffFieldInfo(TiffTag.EXIF_FOCALLENGTHIN35MMFILM, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "FocalLengthIn35mmFilm"), 
            new TiffFieldInfo(TiffTag.EXIF_SCENECAPTURETYPE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "SceneCaptureType"), 
            new TiffFieldInfo(TiffTag.EXIF_GAINCONTROL, 1, 1, TiffType.RATIONAL, FIELD.FIELD_CUSTOM, true, false, "GainControl"), 
            new TiffFieldInfo(TiffTag.EXIF_CONTRAST, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "Contrast"), 
            new TiffFieldInfo(TiffTag.EXIF_SATURATION, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "Saturation"), 
            new TiffFieldInfo(TiffTag.EXIF_SHARPNESS, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "Sharpness"), 
            new TiffFieldInfo(TiffTag.EXIF_DEVICESETTINGDESCRIPTION, -1, -1, TiffType.UNDEFINED, FIELD.FIELD_CUSTOM, true, true, "DeviceSettingDescription"), 
            new TiffFieldInfo(TiffTag.EXIF_SUBJECTDISTANCERANGE, 1, 1, TiffType.SHORT, FIELD.FIELD_CUSTOM, true, false, "SubjectDistanceRange"), 
            new TiffFieldInfo(TiffTag.EXIF_IMAGEUNIQUEID, 33, 33, TiffType.ASCII, FIELD.FIELD_CUSTOM, true, false, "ImageUniqueID")
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

        private void printFieldInfo(Stream fd)
        {
            fprintf(fd, "{0}: \n", m_name);
            for (int i = 0; i < m_nfields; i++)
            {
                TiffFieldInfo fip = m_fieldinfo[i];
                fprintf(fd, "field[{0,2:D}] {1,5:D}, {2,2:D}, {3,2:D}, {4}, {5,2:D}, {6,5}, {7,5}, {8}\n",
                    i, fip.Field_tag, fip.Field_read_count, fip.Field_write_count,
                    fip.Field_type, fip.Field_bit, fip.Field_okto_change ? "TRUE" : "FALSE",
                    fip.Field_pass_count ? "TRUE" : "FALSE", fip.Field_name);
            }
        }

        /*
        * Return nearest TiffDataType to the sample type of an image.
        */
        private TiffType sampleToTagType()
        {
            int bps = howMany8(m_dir.td_bitspersample);

            switch (m_dir.td_sampleformat)
            {
                case SampleFormat.IEEEFP:
                    return (bps == 4 ? TiffType.FLOAT : TiffType.DOUBLE);
                case SampleFormat.INT:
                    return (bps <= 1 ? TiffType.SBYTE : bps <= 2 ? TiffType.SSHORT : TiffType.SLONG);
                case SampleFormat.UINT:
                    return (bps <= 1 ? TiffType.BYTE : bps <= 2 ? TiffType.SHORT : TiffType.LONG);
                case SampleFormat.VOID:
                    return TiffType.UNDEFINED;
            }
            
            return TiffType.UNDEFINED;
        }

        private TiffFieldInfo findOrRegisterFieldInfo(TiffTag tag, TiffType dt)
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

        private TiffFieldInfo createAnonFieldInfo(TiffTag tag, TiffType field_type)
        {
            TiffFieldInfo fld = new TiffFieldInfo(tag, TIFF_VARIABLE2, TIFF_VARIABLE2, field_type, FIELD.FIELD_CUSTOM, true, true, null);

            /* note that this name is a special sign to Close() and
             * setupFieldInfo() to free the field
             */
            fld.Field_name = string.Format("Tag {0}", tag);
            return fld;
        }
        
        /*
        * Return size of TiffDataType in bytes.
        *
        * XXX: We need a separate function to determine the space needed
        * to store the value. For TiffType.RATIONAL values DataWidth()
        * returns 8, but we use 4-byte float to represent rationals.
        */
        internal static int dataSize(TiffType type)
        {
            switch (type)
            {
                case TiffType.BYTE:
                case TiffType.SBYTE:
                case TiffType.ASCII:
                case TiffType.UNDEFINED:
                    return 1;

                case TiffType.SHORT:
                case TiffType.SSHORT:
                    return 2;

                case TiffType.LONG:
                case TiffType.SLONG:
                case TiffType.FLOAT:
                case TiffType.IFD:
                case TiffType.RATIONAL:
                case TiffType.SRATIONAL:
                    return 4;

                case TiffType.DOUBLE:
                    return 8;

                default:
                    return 0;
            }
        }
    }
}
