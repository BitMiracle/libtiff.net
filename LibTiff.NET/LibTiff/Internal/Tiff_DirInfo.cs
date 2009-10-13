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

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private TiffFieldInfo[] getFieldInfo(out int size)
        {
            size = sizeof(tiffFieldInfo) / sizeof(tiffFieldInfo[0]);
            return tiffFieldInfo;
        }

        private TiffFieldInfo[] getExifFieldInfo(out int size)
        {
            size = sizeof(exifFieldInfo) / sizeof(exifFieldInfo[0]);
            return exifFieldInfo;
        }

        private void setupFieldInfo(TiffFieldInfo[] info, int n)
        {
            m_nfields = 0;
            MergeFieldInfo(info, n);
        }

        //private static int tagCompare(const void* a, const void* b);
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

        private TiffFieldInfo findOrRegisterFieldInfo(uint tag, TiffDataType dt)
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

        private TiffFieldInfo createAnonFieldInfo(uint tag, TiffDataType field_type)
        {
            TiffFieldInfo fld = new TiffFieldInfo(tag, TIFF_VARIABLE2, TIFF_VARIABLE2, field_type, FIELD_CUSTOM, true, true, null);

            if (fld == null)
                return null;

            /* note that this name is a special sign to Close() and
             * setupFieldInfo() to free the field
             */
            fld.field_name = string.Format("Tag %d", tag);
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
