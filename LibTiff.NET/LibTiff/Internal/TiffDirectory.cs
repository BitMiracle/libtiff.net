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

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// Internal format of a TIFF directory entry.
    /// </summary>
    class TiffDirectory
    {
        public const int FIELD_SETLONGS = 4;

        /* bit vector of fields that are set */
        public uint[] td_fieldsset = new uint[FIELD_SETLONGS];

        public uint td_imagewidth;
        public uint td_imagelength;
        public uint td_imagedepth;
        public uint td_tilewidth;
        public uint td_tilelength;
        public uint td_tiledepth;
        public uint td_subfiletype;
        public UInt16 td_bitspersample;
        public UInt16 td_sampleformat;
        public UInt16 td_compression;
        public UInt16 td_photometric;
        public UInt16 td_threshholding;
        public UInt16 td_fillorder;
        public UInt16 td_orientation;
        public UInt16 td_samplesperpixel;
        public uint td_rowsperstrip;
        public UInt16 td_minsamplevalue;
        public UInt16 td_maxsamplevalue;
        public double td_sminsamplevalue;
        public double td_smaxsamplevalue;
        public float td_xresolution;
        public float td_yresolution;
        public UInt16 td_resolutionunit;
        public UInt16 td_planarconfig;
        public float td_xposition;
        public float td_yposition;
        public UInt16[] td_pagenumber = new ushort[2];
        public UInt16[][] td_colormap = { null, null, null };
        public UInt16[] td_halftonehints = new ushort[2];
        public UInt16 td_extrasamples;
        public UInt16[] td_sampleinfo;
        public uint td_stripsperimage;
        public uint td_nstrips; /* size of offset & bytecount arrays */
        public uint[] td_stripoffset;
        public uint[] td_stripbytecount;
        public int td_stripbytecountsorted; /* is the bytecount array sorted ascending? */
        public UInt16 td_nsubifd;
        public uint[] td_subifd;
        /* YCbCr parameters */
        public UInt16[] td_ycbcrsubsampling = new ushort[2];
        public UInt16 td_ycbcrpositioning;
        /* Colorimetry parameters */
        public UInt16[][] td_transferfunction = { null, null, null };
        /* CMYK parameters */
        public int td_inknameslen;
        public string td_inknames;

        public int td_customValueCount;
        public TiffTagValue[] td_customValues;

        public TiffDirectory()
        {
            memset(td_fieldsset, 0, sizeof(unsigned int) * FIELD_SETLONGS);

            td_imagewidth = 0;
            td_imagelength = 0;
            td_subfiletype = 0;
            td_compression = 0;
            td_photometric = 0;
            td_minsamplevalue = 0;
            td_maxsamplevalue = 0;
            td_sminsamplevalue = 0;
            td_smaxsamplevalue = 0;
            td_xresolution = 0;
            td_yresolution = 0;
            td_planarconfig = 0;
            td_xposition = 0;
            td_yposition = 0;

            memset(td_pagenumber, 0, sizeof(UInt16) * 2);
            memset(td_colormap, 0, sizeof(UInt16*) * 3);
            memset(td_halftonehints, 0, sizeof(UInt16) * 2);

            td_extrasamples = 0;
            td_sampleinfo = NULL;
            td_stripsperimage = 0;
            td_nstrips = 0;
            td_stripoffset = NULL;
            td_stripbytecount = NULL;
            td_nsubifd = 0;
            td_subifd = NULL;

            memset(td_transferfunction, 0, sizeof(UInt16*) * 3);
            td_inknameslen = 0;
            td_inknames = NULL;

            td_customValueCount = 0;
            td_customValues = NULL;

            td_fillorder = FILLORDER_MSB2LSB;
            td_bitspersample = 1;
            td_threshholding = THRESHHOLD_BILEVEL;
            td_orientation = ORIENTATION_TOPLEFT;
            td_samplesperpixel = 1;
            td_rowsperstrip = (uint)-1;
            td_tilewidth = 0;
            td_tilelength = 0;
            td_tiledepth = 1;
            td_stripbytecountsorted = 1; /* Our own arrays always sorted. */
            td_resolutionunit = RESUNIT_INCH;
            td_sampleformat = SAMPLEFORMAT_UINT;
            td_imagedepth = 1;
            td_ycbcrsubsampling[0] = 2;
            td_ycbcrsubsampling[1] = 2;
            td_ycbcrpositioning = YCBCRPOSITION_CENTERED;
        }
    }
}
