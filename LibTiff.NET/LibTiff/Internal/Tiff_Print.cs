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
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
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

        private static void printField(Stream fd, TiffFieldInfo fip, int value_count, object raw_data)
        {
            fprintf(fd, "  {0}: ", fip.field_name);

            for (int j = 0; j < value_count; j++)
            {
                if (fip.field_type == TiffDataType.TIFF_BYTE || 
                    fip.field_type == TiffDataType.TIFF_SBYTE)
                {
                    byte[] bytes = raw_data as byte[];
                    sbyte[] sbytes = raw_data as sbyte[];
                    if (bytes != null)
                        fprintf(fd, "{0}", bytes[j]);
                    else if (sbytes != null)
                        fprintf(fd, "{0}", sbytes[j]);
                }
                else if (fip.field_type == TiffDataType.TIFF_UNDEFINED)
                {
                    byte[] bytes = raw_data as byte[];
                    if (bytes != null)
                        fprintf(fd, "0x{0:x}", bytes[j]);
                }
                else if (fip.field_type == TiffDataType.TIFF_SHORT || 
                    fip.field_type == TiffDataType.TIFF_SSHORT)
                {
                    short[] shorts = raw_data as short[];
                    ushort[] ushorts = raw_data as ushort[];
                    if (shorts != null)
                        fprintf(fd, "{0}", shorts[j]);
                    else if (ushorts != null)
                        fprintf(fd, "{0}", ushorts[j]);
                }
                else if (fip.field_type == TiffDataType.TIFF_LONG || 
                    fip.field_type == TiffDataType.TIFF_SLONG)
                {
                    int[] ints = raw_data as int[];
                    uint[] uints = raw_data as uint[];
                    if (ints != null)
                        fprintf(fd, "{0}", ints[j]);
                    else if (uints != null)
                        fprintf(fd, "{0}", uints[j]);
                }
                else if (fip.field_type == TiffDataType.TIFF_RATIONAL ||
                    fip.field_type == TiffDataType.TIFF_SRATIONAL ||
                    fip.field_type == TiffDataType.TIFF_FLOAT)
                {
                    float[] floats = raw_data as float[];
                    if (floats != null)
                        fprintf(fd, "{0}", floats[j]);
                }
                else if (fip.field_type == TiffDataType.TIFF_IFD)
                {
                    int[] ints = raw_data as int[];
                    uint[] uints = raw_data as uint[];
                    if (ints != null)
                        fprintf(fd, "0x{0:x}", ints[j]);
                    else if (uints != null)
                        fprintf(fd, "0x{0:x}", uints[j]);
                }
                else if (fip.field_type == TiffDataType.TIFF_ASCII)
                {
                    string s = raw_data as string;
                    if (s != null)
                        fprintf(fd, "{0}", s);

                    break;
                }
                else if (fip.field_type == TiffDataType.TIFF_DOUBLE ||
                    fip.field_type == TiffDataType.TIFF_FLOAT)
                {
                    float[] floats = raw_data as float[];
                    double[] doubles = raw_data as double[];
                    if (floats != null)
                        fprintf(fd, "{0}", floats[j]);
                    else if (doubles != null)
                        fprintf(fd, "{0}", doubles[j]);
                }
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

        private bool prettyPrintField(Stream fd, TIFFTAG tag, int value_count, object raw_data)
        {
            FieldValue value = new FieldValue(raw_data);
            short[] sdata = value.ToShortArray();
            float[] fdata = value.ToFloatArray();
            double[] ddata = value.ToDoubleArray();

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_INKSET:
                    if (sdata != null)
                    {
                        fprintf(fd, "  Ink Set: ");
                        switch ((INKSET)sdata[0])
                        {
                            case INKSET.INKSET_CMYK:
                                fprintf(fd, "CMYK\n");
                                break;

                            default:
                                fprintf(fd, "{0} (0x{1:x})\n", sdata[0], sdata[0]);
                                break;
                        }
                        return true;
                    }
                    return false;

                case TIFFTAG.TIFFTAG_DOTRANGE:
                    if (sdata != null)
                    {
                        fprintf(fd, "  Dot Range: {0}-{1}\n", sdata[0], sdata[1]);
                        return true;
                    }
                    return false;

                case TIFFTAG.TIFFTAG_WHITEPOINT:
                    if (fdata != null)
                    {
                        fprintf(fd, "  White Point: {0:G}-{1:G}\n", fdata[0], fdata[1]);
                        return true;
                    }
                    return false;

                case TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE:
                    if (fdata != null)
                    {
                        fprintf(fd, "  Reference Black/White:\n");
                        for (short i = 0; i < m_dir.td_samplesperpixel; i++)
                            fprintf(fd, "    {0,2:D}: {1,5:G} {2,5:G}\n", i, fdata[2 * i + 0], fdata[2 * i + 1]);
                        return true;
                    }
                    return false;

                case TIFFTAG.TIFFTAG_XMLPACKET:
                    string s = raw_data as string;
                    if (s != null)
                    {
                        fprintf(fd, "  XMLPacket (XMP Metadata):\n");
                        fprintf(fd, s.Substring(0, value_count));
                        fprintf(fd, "\n");
                        return true;
                    }
                    return false;

                case TIFFTAG.TIFFTAG_RICHTIFFIPTC:
                    /*
                     * XXX: for some weird reason RichTIFFIPTC tag
                     * defined as array of LONG values.
                     */
                    fprintf(fd, "  RichTIFFIPTC Data: <present>, {0} bytes\n", value_count * 4);
                    return true;

                case TIFFTAG.TIFFTAG_PHOTOSHOP:
                    fprintf(fd, "  Photoshop Data: <present>, {0} bytes\n", value_count);
                    return true;

                case TIFFTAG.TIFFTAG_ICCPROFILE:
                    fprintf(fd, "  ICC Profile: <present>, {0} bytes\n", value_count);
                    return true;

                case TIFFTAG.TIFFTAG_STONITS:
                    if (ddata != null)
                    {
                        fprintf(fd, "  Sample to Nits conversion factor: {0:e4}\n", ddata[0]);
                        return true;
                    }
                    return false;
            }

            return false;
        }

        private static void printAscii(Stream fd, string cp)
        {
            for (int cpPos = 0; cp[cpPos] != '\0'; cpPos++)
            {
                if (!char.IsControl(cp[cpPos]))
                {
                    fprintf(fd, "{0}", cp[cpPos]);
                    continue;
                }

                string tp = "\tt\bb\rr\nn\vv";
                int tpPos = 0;
                for (; tp[tpPos] != 0; tpPos++)
                {
                    if (tp[tpPos++] == cp[cpPos])
                        break;
                }

                if (tp[tpPos] != 0)
                    fprintf(fd, "\\{0}", tp[tpPos]);
                else
                    fprintf(fd, "\\{0}", encodeOctalString((byte)(cp[cpPos] & 0xff)));
            }
        }

        private static void printAsciiTag(Stream fd, string name, string value)
        {
            fprintf(fd, "  {0}: \"", name);
            printAscii(fd, value);
            fprintf(fd, "\"\n");
        }
    }
}
