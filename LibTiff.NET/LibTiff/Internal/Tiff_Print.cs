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
 * Directory Printing Support
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private static readonly string[] photoNames = 
        {
            "min-is-white", /* PHOTOMETRIC_MINISWHITE */
            "min-is-black",  /* PHOTOMETRIC_MINISBLACK */
            "RGB color",  /* PHOTOMETRIC_RGB */
            "palette color (RGB from colormap)",  /* PHOTOMETRIC_PALETTE */
            "transparency mask",  /* PHOTOMETRIC_MASK */
            "separated",  /* PHOTOMETRIC_SEPARATED */
            "YCbCr",  /* PHOTOMETRIC_YCBCR */
            "7 (0x7)",
            "CIE L*a*b*",  /* PHOTOMETRIC_CIELAB */
        };

        private static readonly string[] orientNames = 
        {
            "0 (0x0)",
            "row 0 top, col 0 lhs", /* ORIENTATION_TOPLEFT */
            "row 0 top, col 0 rhs",  /* ORIENTATION_TOPRIGHT */
            "row 0 bottom, col 0 rhs",  /* ORIENTATION_BOTRIGHT */
            "row 0 bottom, col 0 lhs",  /* ORIENTATION_BOTLEFT */
            "row 0 lhs, col 0 top",  /* ORIENTATION_LEFTTOP */
            "row 0 rhs, col 0 top",  /* ORIENTATION_RIGHTTOP */
            "row 0 rhs, col 0 bottom",  /* ORIENTATION_RIGHTBOT */
            "row 0 lhs, col 0 bottom",  /* ORIENTATION_LEFTBOT */
        };

        private static void printField(Stream fd, TiffFieldInfo fip, uint value_count, object raw_data)
        {
            fprintf(fd, "  %s: ", fip.field_name);

            for (uint j = 0; j < value_count; j++)
            {
                if (fip.field_type == TiffDataType.TIFF_BYTE)
                    fprintf(fd, "%u", ((byte*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_UNDEFINED)
                    fprintf(fd, "0x%x", (uint)((byte*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_SBYTE)
                    fprintf(fd, "%d", ((sbyte*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_SHORT)
                    fprintf(fd, "%u", ((UInt16*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_SSHORT)
                    fprintf(fd, "%d", ((Int16*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_LONG)
                    fprintf(fd, "%lu", (uint)((uint*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_SLONG)
                    fprintf(fd, "%ld", (int)((int*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_RATIONAL || fip.field_type == TiffDataType.TIFF_SRATIONAL || fip.field_type == TiffDataType.TIFF_FLOAT)
                    fprintf(fd, "%f", ((float*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_IFD)
                    fprintf(fd, "0x%ulx", ((uint*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_ASCII)
                {
                    fprintf(fd, "%s", (char*)raw_data);
                    break;
                }
                else if (fip.field_type == TiffDataType.TIFF_DOUBLE)
                    fprintf(fd, "%f", ((double*)raw_data)[j]);
                else if (fip.field_type == TiffDataType.TIFF_FLOAT)
                    fprintf(fd, "%f", ((float*)raw_data)[j]);
                else
                {
                    fprintf(fd, "<unsupported data type in printField>");
                    break;
                }

                if (j < value_count - 1)
                    fprintf(fd, ",");
            }

            fprintf(fd, "\n");
        }

        private bool prettyPrintField(Stream fd, TIFFTAG tag, uint value_count, object raw_data)
        {
            switch (tag)
            {
                case TIFFTAG.TIFFTAG_INKSET:
                    fprintf(fd, "  Ink Set: ");
                    switch (*((UInt16*)raw_data))
                    {
                        case INKSET_CMYK:
                            fprintf(fd, "CMYK\n");
                            break;
                        default:
                            fprintf(fd, "%u (0x%x)\n", *((UInt16*)raw_data), *((UInt16*)raw_data));
                            break;
                    }
                    return true;
                case TIFFTAG.TIFFTAG_DOTRANGE:
                    fprintf(fd, "  Dot Range: %u-%u\n", ((UInt16*)raw_data)[0], ((UInt16*)raw_data)[1]);
                    return true;
                case TIFFTAG.TIFFTAG_WHITEPOINT:
                    fprintf(fd, "  White Point: %g-%g\n", ((float*)raw_data)[0], ((float*)raw_data)[1]);
                    return true;
                case TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE:
                    {
                        UInt16 i;

                        fprintf(fd, "  Reference Black/White:\n");
                        for (i = 0; i < m_dir.td_samplesperpixel; i++)
                            fprintf(fd, "    %2d: %5g %5g\n", i, ((float*)raw_data)[2 * i + 0], ((float*)raw_data)[2 * i + 1]);
                        return true;
                    }
                case TIFFTAG.TIFFTAG_XMLPACKET:
                    {
                        uint i;

                        fprintf(fd, "  XMLPacket (XMP Metadata):\n");
                        for (i = 0; i < value_count; i++)
                            fputc(((char*)raw_data)[i], fd);
                        fprintf(fd, "\n");
                        return true;
                    }
                case TIFFTAG.TIFFTAG_RICHTIFFIPTC:
                    /*
                     * XXX: for some weird reason RichTIFFIPTC tag
                     * defined as array of LONG values.
                     */
                    fprintf(fd, "  RichTIFFIPTC Data: <present>, %lu bytes\n", value_count * 4);
                    return true;
                case TIFFTAG.TIFFTAG_PHOTOSHOP:
                    fprintf(fd, "  Photoshop Data: <present>, %lu bytes\n", value_count);
                    return true;
                case TIFFTAG.TIFFTAG_ICCPROFILE:
                    fprintf(fd, "  ICC Profile: <present>, %lu bytes\n", value_count);
                    return true;
                case TIFFTAG.TIFFTAG_STONITS:
                    fprintf(fd, "  Sample to Nits conversion factor: %.4e\n", *((double*)raw_data));
                    return true;
            }

            return false;
        }

        private static void printAscii(Stream fd, string cp)
        {
            for (int cpPos = 0; cp[cpPos] != '\0'; cpPos++)
            {
                if (isprint((int)cp[cpPos]))
                {
                    fputc(cp[cpPos], fd);
                    continue;
                }

                const char* tp = "\tt\bb\rr\nn\vv";
                int tpPos = 0;
                for (; tp[tpPos] != 0; tpPos++)
                {
                    if (tp[tpPos++] == cp[cpPos])
                        break;
                }

                if (tp[tpPos] != 0)
                    fprintf(fd, "\\%c", tp[tpPos]);
                else
                    fprintf(fd, "\\%03o", cp[cpPos] & 0xff);
            }
        }

        private static void printAsciiTag(Stream fd, string name, string value)
        {
            fprintf(fd, "  %s: \"", name);
            printAscii(fd, value);
            fprintf(fd, "\"\n");
        }
    }
}
