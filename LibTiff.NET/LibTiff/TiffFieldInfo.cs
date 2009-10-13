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

namespace BitMiracle.LibTiff
{
    public class TiffFieldInfo
    {
        public TIFFTAG field_tag; /* field's tag */
        public short field_readcount; /* read count/TIFF_VARIABLE/TIFF_SPP */
        public short field_writecount; /* write count/TIFF_VARIABLE */
        public TiffDataType field_type; /* type of associated data */
        public ushort field_bit; /* bit in fieldsset bit vector */
        public bool field_oktochange; /* if true, can change while writing */
        public bool field_passcount; /* if true, pass dir count on set */
        public string field_name; /* ASCII name */

        public TiffFieldInfo(TIFFTAG fieldTag, short fieldReadCount, short fieldWriteCount, TiffDataType fieldType,
            ushort fieldBit, bool fieldOkToChange, bool fieldPassCount, string fieldName)
        {
            field_tag = fieldTag;
            field_readcount = fieldReadCount;
            field_writecount = fieldWriteCount;
            field_type = fieldType;
            field_bit = fieldBit;
            field_oktochange = fieldOkToChange;
            field_passcount = fieldPassCount;
            field_name = fieldName;
        }
    }
}
