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
        /* bit vector of fields that are set */
        public int[] td_fieldsset = new int[FIELD.FIELD_SETLONGS];

        public int td_imagewidth;
        public int td_imagelength;
        public int td_imagedepth;
        public int td_tilewidth;
        public int td_tilelength;
        public int td_tiledepth;
        public FILETYPE td_subfiletype;
        public short td_bitspersample;
        public SAMPLEFORMAT td_sampleformat;
        public COMPRESSION td_compression;
        public PHOTOMETRIC td_photometric;
        public THRESHHOLD td_threshholding;
        public FILLORDER td_fillorder;
        public ORIENTATION td_orientation;
        public short td_samplesperpixel;
        public int td_rowsperstrip;
        public short td_minsamplevalue;
        public short td_maxsamplevalue;
        public double td_sminsamplevalue;
        public double td_smaxsamplevalue;
        public float td_xresolution;
        public float td_yresolution;
        public RESUNIT td_resolutionunit;
        public PLANARCONFIG td_planarconfig;
        public float td_xposition;
        public float td_yposition;
        public short[] td_pagenumber = new short[2];
        public short[][] td_colormap = { null, null, null };
        public short[] td_halftonehints = new short[2];
        public short td_extrasamples;
        public EXTRASAMPLE[] td_sampleinfo;
        public int td_stripsperimage;
        public int td_nstrips; /* size of offset & bytecount arrays */
        public int[] td_stripoffset;
        public int[] td_stripbytecount;
        public int td_stripbytecountsorted; /* is the bytecount array sorted ascending? */
        public short td_nsubifd;
        public int[] td_subifd;
        /* YCbCr parameters */
        public short[] td_ycbcrsubsampling = new short[2];
        public YCBCRPOSITION td_ycbcrpositioning;
        /* Colorimetry parameters */
        public short[][] td_transferfunction = { null, null, null };
        /* CMYK parameters */
        public int td_inknameslen;
        public string td_inknames;

        public int td_customValueCount;
        public TiffTagValue[] td_customValues;

        public TiffDirectory()
        {
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

            td_extrasamples = 0;
            td_sampleinfo = null;
            td_stripsperimage = 0;
            td_nstrips = 0;
            td_stripoffset = null;
            td_stripbytecount = null;
            td_nsubifd = 0;
            td_subifd = null;

            td_inknameslen = 0;
            td_inknames = null;

            td_customValueCount = 0;
            td_customValues = null;

            td_fillorder = FILLORDER.FILLORDER_MSB2LSB;
            td_bitspersample = 1;
            td_threshholding = THRESHHOLD.THRESHHOLD_BILEVEL;
            td_orientation = ORIENTATION.ORIENTATION_TOPLEFT;
            td_samplesperpixel = 1;
            td_rowsperstrip = -1;
            td_tilewidth = 0;
            td_tilelength = 0;
            td_tiledepth = 1;
            td_stripbytecountsorted = 1; /* Our own arrays always sorted. */
            td_resolutionunit = RESUNIT.RESUNIT_INCH;
            td_sampleformat = SAMPLEFORMAT.SAMPLEFORMAT_UINT;
            td_imagedepth = 1;
            td_ycbcrsubsampling[0] = 2;
            td_ycbcrsubsampling[1] = 2;
            td_ycbcrpositioning = YCBCRPOSITION.YCBCRPOSITION_CENTERED;
        }
    }
}
