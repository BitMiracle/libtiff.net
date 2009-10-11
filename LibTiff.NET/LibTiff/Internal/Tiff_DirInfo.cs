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
        private TiffFieldInfo getFieldInfo(out uint size)
        {
            size = sizeof(tiffFieldInfo) / sizeof(tiffFieldInfo[0]);
            return tiffFieldInfo;
        }

        private TiffFieldInfo getExifFieldInfo(out uint size)
        {
            size = sizeof(exifFieldInfo) / sizeof(exifFieldInfo[0]);
            return exifFieldInfo;
        }

        private void setupFieldInfo(TiffFieldInfo[] info, uint n)
        {
            if (m_fieldinfo != null)
            {
                for (size_t i = 0; i < m_nfields; i++)
                {
                    TiffFieldInfo* fld = m_fieldinfo[i];
                    if (fld.field_bit == FIELD_CUSTOM && strncmp("Tag ", fld.field_name, 4) == 0)
                    {
                        delete fld.field_name;
                        delete fld;
                    }
                }

                delete m_fieldinfo;
                m_nfields = 0;
            }

            MergeFieldInfo(info, n);
        }

        //private static int tagCompare(const void* a, const void* b);
        //private static int tagNameCompare(const void* a, const void* b);

        private void printFieldInfo(Stream fd)
        {
            size_t i;

            fprintf(fd, "%s: \n", m_name);
            for (i = 0; i < m_nfields; i++)
            {
                const TiffFieldInfo* fip = m_fieldinfo[i];
                fprintf(fd, "field[%2d] %5lu, %2d, %2d, %d, %2d, %5s, %5s, %s\n", i, fip.field_tag, fip.field_readcount, fip.field_writecount, fip.field_type, fip.field_bit, fip.field_oktochange ? "TRUE" : "FALSE", fip.field_passcount ? "TRUE" : "FALSE", fip.field_name);
            }
        }

        /*
        * Return nearest TiffDataType to the sample type of an image.
        */
        private TiffDataType sampleToTagType()
        {
            uint bps = Tiff::howMany8(m_dir.td_bitspersample);

            switch (m_dir.td_sampleformat)
            {
                case SAMPLEFORMAT_IEEEFP:
                    return (bps == 4 ? TIFF_FLOAT : TIFF_DOUBLE);
                case SAMPLEFORMAT_INT:
                    return (bps <= 1 ? TIFF_SBYTE : bps <= 2 ? TIFF_SSHORT : TIFF_SLONG);
                case SAMPLEFORMAT_UINT:
                    return (bps <= 1 ? TIFF_BYTE : bps <= 2 ? TIFF_SHORT : TIFF_LONG);
                case SAMPLEFORMAT_VOID:
                    return TIFF_UNDEFINED;
            }
            
            return TIFF_UNDEFINED;
        }

        private TiffFieldInfo findOrRegisterFieldInfo(uint tag, TiffDataType dt)
        {
            const TiffFieldInfo* fld = FindFieldInfo(tag, dt);
            if (fld == null)
            {
                fld = createAnonFieldInfo(tag, dt);
                MergeFieldInfo(fld, 1);
            }

            return fld;
        }

        private TiffFieldInfo createAnonFieldInfo(uint tag, TiffDataType field_type)
        {
            TiffFieldInfo* fld = new TiffFieldInfo(tag, TIFF_VARIABLE2, TIFF_VARIABLE2, field_type, FIELD_CUSTOM, true, true, null);

            if (fld == null)
                return null;

            fld.field_name = new char[32];
            if (fld.field_name == null)
            {
                delete fld;
                return null;
            }

            /* note that this name is a special sign to TIFFClose() and
             * setupFieldInfo() to free the field
             */
            sprintf(fld.field_name, "Tag %d", tag);
            return fld;
        }
        
        /*
        * Return size of TiffDataType in bytes.
        *
        * XXX: We need a separate function to determine the space needed
        * to store the value. For TIFF_RATIONAL values DataWidth() returns 8,
        * but we use 4-byte float to represent rationals.
        */
        internal static int dataSize(TiffDataType type)
        {
            switch (type)
            {
                case TIFF_BYTE:
                case TIFF_SBYTE:
                case TIFF_ASCII:
                case TIFF_UNDEFINED:
                    return 1;
                case TIFF_SHORT:
                case TIFF_SSHORT:
                    return 2;
                case TIFF_LONG:
                case TIFF_SLONG:
                case TIFF_FLOAT:
                case TIFF_IFD:
                case TIFF_RATIONAL:
                case TIFF_SRATIONAL:
                    return 4;
                case TIFF_DOUBLE:
                    return 8;
                default:
                    return 0;
            }
        }
    }
}
