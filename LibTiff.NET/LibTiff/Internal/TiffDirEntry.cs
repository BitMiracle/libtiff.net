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

namespace BitMiracle.LibTiff.Classic.Internal
{
    /// <summary>
    /// TIFF Image File Directories are comprised of a table of field
    /// descriptors of the form shown below.  The table is sorted in
    /// ascending order by tag.  The values associated with each entry are
    /// disjoint and may appear anywhere in the file (so long as they are
    /// placed on a word boundary).
    /// 
    /// If the value is 4 bytes or less, then it is placed in the offset
    /// field to save space.  If the value is less than 4 bytes, it is
    /// left-justified in the offset field.
    /// </summary>
    class TiffDirEntry
    {
        public const int SizeInBytes = 12;
        public TiffTag tdir_tag; /* see below */
        public TiffType tdir_type; /* data type; see below */
        public int tdir_count; /* number of items; length in spec */
        public int tdir_offset; /* byte offset to field data */

        public new string ToString()
        {
            return tdir_tag.ToString() + ", " + tdir_type.ToString() + " " + tdir_offset.ToString();
        }
    }
}
