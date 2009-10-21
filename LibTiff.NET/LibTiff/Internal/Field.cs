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
    class FIELD
    {
        public const int FIELD_SETLONGS = 4;

        /*
         * Field flags used to indicate fields that have
         * been set in a directory, and to reference fields
         * when manipulating a directory.
         */

        /*
         * FIELD_IGNORE is used to signify tags that are to
         * be processed but otherwise ignored.  This permits
         * antiquated tags to be quietly read and discarded.
         * Note that a bit *is* allocated for ignored tags;
         * this is understood by the directory reading logic
         * which uses this fact to avoid special-case handling
         */
        internal const short FIELD_IGNORE = 0;

        /*
         * Pseudo-tags don't normally need field bits since they
         * are not written to an output file (by definition).
         * The library also has express logic to always query a
         * codec for a pseudo-tag so allocating a field bit for
         * one is a waste.   If codec wants to promote the notion
         * of a pseudo-tag being ``set'' or ``unset'' then it can
         * do using internal state flags without polluting the
         * field bit space defined for real tags.
         */
        internal const short FIELD_PSEUDO = 0;

        /* multi-item fields */
        internal const short FIELD_IMAGEDIMENSIONS = 1;
        internal const short FIELD_TILEDIMENSIONS = 2;
        internal const short FIELD_RESOLUTION = 3;
        internal const short FIELD_POSITION = 4;

        /* single-item fields */
        internal const short FIELD_SUBFILETYPE = 5;
        internal const short FIELD_BITSPERSAMPLE = 6;
        internal const short FIELD_COMPRESSION = 7;
        internal const short FIELD_PHOTOMETRIC = 8;
        internal const short FIELD_THRESHHOLDING = 9;
        internal const short FIELD_FILLORDER = 10;
        internal const short FIELD_ORIENTATION = 15;
        internal const short FIELD_SAMPLESPERPIXEL = 16;
        internal const short FIELD_ROWSPERSTRIP = 17;
        internal const short FIELD_MINSAMPLEVALUE = 18;
        internal const short FIELD_MAXSAMPLEVALUE = 19;
        internal const short FIELD_PLANARCONFIG = 20;
        internal const short FIELD_RESOLUTIONUNIT = 22;
        internal const short FIELD_PAGENUMBER = 23;
        internal const short FIELD_STRIPBYTECOUNTS = 24;
        internal const short FIELD_STRIPOFFSETS = 25;
        internal const short FIELD_COLORMAP = 26;
        internal const short FIELD_EXTRASAMPLES = 31;
        internal const short FIELD_SAMPLEFORMAT = 32;
        internal const short FIELD_SMINSAMPLEVALUE = 33;
        internal const short FIELD_SMAXSAMPLEVALUE = 34;
        internal const short FIELD_IMAGEDEPTH = 35;
        internal const short FIELD_TILEDEPTH = 36;
        internal const short FIELD_HALFTONEHINTS = 37;
        internal const short FIELD_YCBCRSUBSAMPLING = 39;
        internal const short FIELD_YCBCRPOSITIONING = 40;
        internal const short FIELD_TRANSFERFUNCTION = 44;
        internal const short FIELD_INKNAMES = 46;
        internal const short FIELD_SUBIFD = 49;
        internal const short FIELD_CUSTOM = 65;
        /* end of support for well-known tags; codec-private tags follow */

        internal const short FIELD_CODEC = 66;  /* base of codec-private tags */
        internal const short FIELD_LAST = (32 * FIELD_SETLONGS - 1);
    }
}
