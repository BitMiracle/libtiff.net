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
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffFieldInfo
    {
        private TiffTag field_tag;
        private short field_readcount;
        private short field_writecount;
        private TiffType field_type;
        private short field_bit;
        private bool field_oktochange;
        private bool field_passcount;
        private string field_name;

        public TiffFieldInfo(TiffTag fieldTag, short fieldReadCount, short fieldWriteCount, TiffType fieldType,
            short fieldBit, bool fieldOkToChange, bool fieldPassCount, string fieldName)
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

        public override string ToString()
        {
            return field_tag.ToString();
        }

        /// <summary>
        /// Field's tag.
        /// </summary>
        public TiffTag Field_tag
        {
            get { return field_tag; }
        }

        /// <summary>
        /// Read count/TIFF_VARIABLE/TIFF_SPP
        /// </summary>
        public short Field_read_count
        {
            get { return field_readcount; }
        }

        /// <summary>
        /// Write count/TIFF_VARIABLE
        /// </summary>
        public short Field_write_count
        {
            get { return field_writecount; }
        }

        /// <summary>
        /// Type of associated data.
        /// </summary>
        public TiffType Field_type
        {
            get { return field_type; }
        }


        /// <summary>
        /// Bit in fields set bit vector
        /// </summary>
        public short Field_bit
        {
            get { return field_bit; }
        }


        /// <summary>
        /// If true, can change while writing
        /// </summary>
        public bool Field_okto_change
        {
            get { return field_oktochange; }
        }


        /// <summary>
        /// If true, pass dir count on set
        /// </summary>
        public bool Field_pass_count
        {
            get { return field_passcount; }
        }

        /// <summary>
        /// ASCII name
        /// </summary>
        public string Field_name
        {
            get { return field_name; }
            set { field_name = value; }
        }
    }
}
