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
using System.IO;

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
    public class TiffTagMethods
    {
        /*
         * These are used in the backwards compatibility code...
         */
        private const ushort DATATYPE_VOID = 0;       /* !untyped data */
        private const ushort DATATYPE_INT = 1;       /* !signed integer data */
        private const ushort DATATYPE_UINT = 2;       /* !unsigned integer data */
        private const ushort DATATYPE_IEEEFP = 3;       /* !IEEE floating point data */

        /* tag set routine */
        public virtual bool vsetfield(Tiff tif, TIFFTAG tag, params object[] ap)
        {
            const string module = "_TIFFVSetField";

            TiffDirectory td = tif.m_dir;
            bool status = true;
            uint v32 = 0;
            int v = 0;

            bool end = false;
            bool badvalue = false;
            bool badvalue32 = false;

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_SUBFILETYPE:
                    td.td_subfiletype = (FILETYPE)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_IMAGEWIDTH:
                    td.td_imagewidth = (int)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_IMAGELENGTH:
                    td.td_imagelength = (int)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_BITSPERSAMPLE:
                    td.td_bitspersample = (ushort)ap[0];
                    /*
                    * If the data require post-decoding processing to byte-swap
                    * samples, set it up here.  Note that since tags are required
                    * to be ordered, compression code can override this behaviour
                    * in the setup method if it wants to roll the post decoding
                    * work in with its normal work.
                    */
                    if ((tif.m_flags & Tiff.TIFF_SWAB) != 0)
                    {
                        if (td.td_bitspersample == 16)
                            tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab16Bit;
                        else if (td.td_bitspersample == 24)
                            tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab24Bit;
                        else if (td.td_bitspersample == 32)
                            tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab32Bit;
                        else if (td.td_bitspersample == 64)
                            tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab64Bit;
                        else if (td.td_bitspersample == 128)
                        {
                            /* two 64's */
                            tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab64Bit;
                        }
                    }
                    break;
                case TIFFTAG.TIFFTAG_COMPRESSION:
                    v = (int)ap[0] & 0xffff;
                    COMPRESSION comp = (COMPRESSION)v;
                    /*
                    * If we're changing the compression scheme, the notify the
                    * previous module so that it can cleanup any state it's
                    * setup.
                    */
                    if (tif.fieldSet(FIELD.FIELD_COMPRESSION))
                    {
                        if (td.td_compression == comp)
                            break;

                        tif.m_currentCodec.tif_cleanup();
                        tif.m_flags &= ~Tiff.TIFF_CODERSETUP;
                    }
                    /*
                    * Setup new compression routine state.
                    */
                    status = tif.setCompressionScheme(comp);
                    if (status)
                        td.td_compression = comp;
                    else
                        status = false;
                    break;

                case TIFFTAG.TIFFTAG_PHOTOMETRIC:
                    td.td_photometric = (PHOTOMETRIC)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_THRESHHOLDING:
                    td.td_threshholding = (THRESHHOLD)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_FILLORDER:
                    v = (int)ap[0];
                    FILLORDER fo = (FILLORDER)v;
                    if (fo != FILLORDER.FILLORDER_LSB2MSB && fo != FILLORDER.FILLORDER_MSB2LSB)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_fillorder = fo;
                    break;
                case TIFFTAG.TIFFTAG_ORIENTATION:
                    v = (int)ap[0];
                    ORIENTATION or = (ORIENTATION)v;
                    if (or < ORIENTATION.ORIENTATION_TOPLEFT || ORIENTATION.ORIENTATION_LEFTBOT < or)
                    {
                        badvalue = true;
                        break;
                    }
                    else
                        td.td_orientation = or;
                    break;
                case TIFFTAG.TIFFTAG_SAMPLESPERPIXEL:
                    /* XXX should cross check -- e.g. if pallette, then 1 */
                    v = (int)ap[0];
                    if (v == 0)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_samplesperpixel = (ushort)v;
                    break;
                case TIFFTAG.TIFFTAG_ROWSPERSTRIP:
                    v32 = (uint)ap[0];
                    if (v32 == 0)
                    {
                        badvalue32 = true;
                        break;
                    }

                    td.td_rowsperstrip = (int)v32;
                    if (!tif.fieldSet(FIELD.FIELD_TILEDIMENSIONS))
                    {
                        td.td_tilelength = (int)v32;
                        td.td_tilewidth = td.td_imagewidth;
                    }
                    break;
                case TIFFTAG.TIFFTAG_MINSAMPLEVALUE:
                    td.td_minsamplevalue = (ushort)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_MAXSAMPLEVALUE:
                    td.td_maxsamplevalue = (ushort)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_SMINSAMPLEVALUE:
                    td.td_sminsamplevalue = (double)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_SMAXSAMPLEVALUE:
                    td.td_smaxsamplevalue = (double)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_XRESOLUTION:
                    td.td_xresolution = (float)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_YRESOLUTION:
                    td.td_yresolution = (float)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_PLANARCONFIG:
                    v = (int)ap[0];
                    PLANARCONFIG pc = (PLANARCONFIG)v;
                    if (pc != PLANARCONFIG.PLANARCONFIG_CONTIG && pc != PLANARCONFIG.PLANARCONFIG_SEPARATE)
                    {
                        badvalue = true;
                        break;
                    }
                    td.td_planarconfig = pc;
                    break;
                case TIFFTAG.TIFFTAG_XPOSITION:
                    td.td_xposition = (float)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_YPOSITION:
                    td.td_yposition = (float)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_RESOLUTIONUNIT:
                    v = (int)ap[0];
                    RESUNIT ru = (RESUNIT)v;
                    if (ru < RESUNIT.RESUNIT_NONE || RESUNIT.RESUNIT_CENTIMETER < ru)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_resolutionunit = ru;
                    break;
                case TIFFTAG.TIFFTAG_PAGENUMBER:
                    td.td_pagenumber[0] = (ushort)ap[0];
                    td.td_pagenumber[1] = (ushort)ap[1];
                    break;
                case TIFFTAG.TIFFTAG_HALFTONEHINTS:
                    td.td_halftonehints[0] = (ushort)ap[0];
                    td.td_halftonehints[1] = (ushort)ap[1];
                    break;
                case TIFFTAG.TIFFTAG_COLORMAP:
                    v32 = (uint)(1L << td.td_bitspersample);
                    Tiff.setShortArray(out td.td_colormap[0], ap[0] as ushort[], (int)v32);
                    Tiff.setShortArray(out td.td_colormap[1], ap[1] as ushort[], (int)v32);
                    Tiff.setShortArray(out td.td_colormap[2], ap[2] as ushort[], (int)v32);
                    break;
                case TIFFTAG.TIFFTAG_EXTRASAMPLES:
                    if (!setExtraSamples(td, ref v, ap))
                    {
                        badvalue = true;
                        break;
                    }

                    break;
                case TIFFTAG.TIFFTAG_MATTEING:
                    if ((ushort)ap[0] != 0)
                        td.td_extrasamples = 1;
                    else
                        td.td_extrasamples = 0;

                    if (td.td_extrasamples != 0)
                    {
                        td.td_sampleinfo = new EXTRASAMPLE[1];
                        td.td_sampleinfo[0] = EXTRASAMPLE.EXTRASAMPLE_ASSOCALPHA;
                    }
                    break;
                case TIFFTAG.TIFFTAG_TILEWIDTH:
                    v32 = (uint)ap[0];
                    if ((v32 % 16) != 0)
                    {
                        if (tif.m_mode != Tiff.O_RDONLY)
                        {
                            badvalue32 = true;
                            break;
                        }

                        Tiff.WarningExt(tif, tif.m_clientdata, tif.m_name, "Nonstandard tile width %d, convert file", v32);
                    }
                    td.td_tilewidth = (int)v32;
                    tif.m_flags |= Tiff.TIFF_ISTILED;
                    break;
                case TIFFTAG.TIFFTAG_TILELENGTH:
                    v32 = (uint)ap[0];
                    if ((v32 % 16) != 0)
                    {
                        if (tif.m_mode != Tiff.O_RDONLY)
                        {
                            badvalue32 = true;
                            break;
                        }

                        Tiff.WarningExt(tif, tif.m_clientdata, tif.m_name, "Nonstandard tile length %d, convert file", v32);
                    }
                    td.td_tilelength = (int)v32;
                    tif.m_flags |= Tiff.TIFF_ISTILED;
                    break;
                case TIFFTAG.TIFFTAG_TILEDEPTH:
                    v32 = (uint)ap[0];
                    if (v32 == 0)
                    {
                        badvalue32 = true;
                        break;
                    }

                    td.td_tiledepth = (int)v32;
                    break;
                case TIFFTAG.TIFFTAG_DATATYPE:
                    v = (int)ap[0];
                    SAMPLEFORMAT sf = SAMPLEFORMAT.SAMPLEFORMAT_VOID;
                    switch (v)
                    {
                        case DATATYPE_VOID:
                            sf = SAMPLEFORMAT.SAMPLEFORMAT_VOID;
                            break;
                        case DATATYPE_INT:
                            sf = SAMPLEFORMAT.SAMPLEFORMAT_INT;
                            break;
                        case DATATYPE_UINT:
                            sf = SAMPLEFORMAT.SAMPLEFORMAT_UINT;
                            break;
                        case DATATYPE_IEEEFP:
                            sf = SAMPLEFORMAT.SAMPLEFORMAT_IEEEFP;
                            break;
                        default:
                            badvalue = true;
                            break;
                    }

                    if (!badvalue)
                        td.td_sampleformat = sf;

                    break;
                case TIFFTAG.TIFFTAG_SAMPLEFORMAT:
                    v = (int)ap[0];
                    sf = (SAMPLEFORMAT)v;
                    if (sf < SAMPLEFORMAT.SAMPLEFORMAT_UINT || SAMPLEFORMAT.SAMPLEFORMAT_COMPLEXIEEEFP < sf)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_sampleformat = sf;

                    /*  Try to fix up the SWAB function for complex data. */
                    if (td.td_sampleformat == SAMPLEFORMAT.SAMPLEFORMAT_COMPLEXINT &&
                        td.td_bitspersample == 32 && tif.m_postDecodeMethod == Tiff.PostDecodeMethodType.pdmSwab32Bit)
                    {
                        tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab16Bit;
                    }
                    else if ((td.td_sampleformat == SAMPLEFORMAT.SAMPLEFORMAT_COMPLEXINT ||
                        td.td_sampleformat == SAMPLEFORMAT.SAMPLEFORMAT_COMPLEXIEEEFP) &&
                        td.td_bitspersample == 64 && tif.m_postDecodeMethod == Tiff.PostDecodeMethodType.pdmSwab64Bit)
                    {
                        tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab32Bit;
                    }
                    break;
                case TIFFTAG.TIFFTAG_IMAGEDEPTH:
                    td.td_imagedepth = (int)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_SUBIFD:
                    if ((tif.m_flags & Tiff.TIFF_INSUBIFD) == 0)
                    {
                        td.td_nsubifd = (ushort)ap[0];
                        Tiff.setLongArray(out td.td_subifd, ap[1] as int[], td.td_nsubifd);
                    }
                    else
                    {
                        Tiff.ErrorExt(tif, tif.m_clientdata, module, "%s: Sorry, cannot nest SubIFDs", tif.m_name);
                        status = false;
                    }
                    break;
                case TIFFTAG.TIFFTAG_YCBCRPOSITIONING:
                    td.td_ycbcrpositioning = (YCBCRPOSITION)ap[0];
                    break;
                case TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING:
                    td.td_ycbcrsubsampling[0] = (ushort)ap[0];
                    td.td_ycbcrsubsampling[1] = (ushort)ap[1];
                    break;
                case TIFFTAG.TIFFTAG_TRANSFERFUNCTION:
                    v = ((td.td_samplesperpixel - td.td_extrasamples) > 1 ? 3 : 1);
                    for (uint i = 0; i < v; i++)
                    {
                        Tiff.setShortArray(out td.td_transferfunction[i], ap[0] as ushort[], 1 << td.td_bitspersample);
                    }
                    break;
                case TIFFTAG.TIFFTAG_INKNAMES:
                    v = (int)ap[0];
                    string s = ap[1] as string;
                    v = checkInkNamesString(tif, (int)v, s);
                    status = v > 0;
                    if (v > 0)
                    {
                        setNString(out td.td_inknames, s, (int)v);
                        td.td_inknameslen = (int)v;
                    }
                    break;
                default:
                    {
                        /*
                        * This can happen if multiple images are open with different
                        * codecs which have private tags.  The global tag information
                        * table may then have tags that are valid for one file but not
                        * the other. If the client tries to set a tag that is not valid
                        * for the image's codec then we'll arrive here.  This
                        * happens, for example, when tiffcp is used to convert between
                        * compression schemes and codec-specific tags are blindly copied.
                        */
                        TiffFieldInfo fip = tif.FindFieldInfo(tag, TiffDataType.TIFF_ANY);
                        if (fip == null || fip.field_bit != FIELD.FIELD_CUSTOM)
                        {
                            Tiff.ErrorExt(tif, tif.m_clientdata, module, "%s: Invalid %stag \"%s\" (not supported by codec)", tif.m_name, Tiff.isPseudoTag(tag) ? "pseudo-" : "", fip != null ? fip.field_name : "Unknown");
                            status = false;
                            break;
                        }

                        /*
                        * Find the existing entry for this custom value.
                        */
                        int tvIndex = -1;
                        for (int iCustom = 0; iCustom < td.td_customValueCount; iCustom++)
                        {
                            if (td.td_customValues[iCustom].info.field_tag == tag)
                            {
                                td.td_customValues[iCustom].value = null;
                                break;
                            }
                        }

                        /*
                        * Grow the custom list if the entry was not found.
                        */
                        if (tvIndex == -1)
                        {
                            td.td_customValueCount++;
                            TiffTagValue[] new_customValues = Tiff.Realloc(td.td_customValues, td.td_customValueCount - 1, td.td_customValueCount);
                            if (new_customValues == null)
                            {
                                Tiff.ErrorExt(tif, tif.m_clientdata, module, "%s: Failed to allocate space for list of custom values", tif.m_name);
                                status = false;
                                end = true;
                                break;
                            }

                            td.td_customValues = new_customValues;

                            tvIndex = td.td_customValueCount - 1;
                            td.td_customValues[tvIndex].info = fip;
                            td.td_customValues[tvIndex].value = null;
                            td.td_customValues[tvIndex].count = 0;
                        }

                        /*
                        * Set custom value ... save a copy of the custom tag value.
                        */
                        int tv_size = Tiff.dataSize(fip.field_type);
                        if (tv_size == 0)
                        {
                            status = false;
                            Tiff.ErrorExt(tif, tif.m_clientdata, module, "%s: Bad field type %d for \"%s\"", tif.m_name, fip.field_type, fip.field_name);
                            end = true;
                            break;
                        }

                        if (fip.field_passcount)
                        {
                            if (fip.field_writecount == Tiff.TIFF_VARIABLE2)
                                td.td_customValues[tvIndex].count = (int)ap[0];
                            else
                                td.td_customValues[tvIndex].count = (int)ap[0];
                        }
                        else if (fip.field_writecount == Tiff.TIFF_VARIABLE || fip.field_writecount == Tiff.TIFF_VARIABLE2)
                            td.td_customValues[tvIndex].count = 1;
                        else if (fip.field_writecount == Tiff.TIFF_SPP)
                            td.td_customValues[tvIndex].count = td.td_samplesperpixel;
                        else
                            td.td_customValues[tvIndex].count = fip.field_writecount;

                        if (fip.field_type == TiffDataType.TIFF_ASCII)
                        {
                            string ascii;
                            Tiff.setString(out ascii, ap[1] as string);
                            td.td_customValues[tvIndex].value = Encoding.ASCII.GetBytes(ascii);
                        }
                        else
                        {
                            td.td_customValues[tvIndex].value = new byte[tv_size * td.td_customValues[tvIndex].count];
                            if (td.td_customValues[tvIndex].value == null)
                            {
                                Tiff.ErrorExt(tif, tif.m_clientdata, tif.m_name, "No space Tag Value");
                                status = false;
                                end = true;
                                break;
                            }

                            if ((fip.field_passcount || fip.field_writecount == Tiff.TIFF_VARIABLE ||
                                fip.field_writecount == Tiff.TIFF_VARIABLE2 ||
                                fip.field_writecount == Tiff.TIFF_SPP || td.td_customValues[tvIndex].count > 1) &&
                                fip.field_tag != TIFFTAG.TIFFTAG_PAGENUMBER &&
                                fip.field_tag != TIFFTAG.TIFFTAG_HALFTONEHINTS &&
                                fip.field_tag != TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING &&
                                fip.field_tag != TIFFTAG.TIFFTAG_DOTRANGE)
                            {
                                byte[] apBytes = ap[1] as byte[];
                                Array.Copy(apBytes, td.td_customValues[tvIndex].value, apBytes.Length);
                            }
                            else
                            {
                                /*
                                * XXX: The following loop required to handle
                                * TIFFTAG_PAGENUMBER, TIFFTAG_HALFTONEHINTS,
                                * TIFFTAG_YCBCRSUBSAMPLING and TIFFTAG_DOTRANGE tags.
                                * These tags are actually arrays and should be passed as
                                * array pointers to TIFFSetField() function, but actually
                                * passed as a list of separate values. This behavior
                                * must be changed in the future!
                                */
                                byte[] val = td.td_customValues[tvIndex].value;
                                int valPos = 0;
                                for (int i = 0; i < td.td_customValues[tvIndex].count; i++, valPos += tv_size)
                                {
                                    switch (fip.field_type)
                                    {
                                        case TiffDataType.TIFF_BYTE:
                                        case TiffDataType.TIFF_UNDEFINED:
                                            val[valPos] = (byte)ap[i + 1];
                                            break;
                                        case TiffDataType.TIFF_SBYTE:
                                            val[valPos] = (byte)ap[i + 1];
                                            break;
                                        case TiffDataType.TIFF_SHORT:
                                            Array.Copy(BitConverter.GetBytes((ushort)ap[i + 1]), 0, val, valPos, tv_size);
                                            break;
                                        case TiffDataType.TIFF_SSHORT:
                                            Array.Copy(BitConverter.GetBytes((short)ap[i + 1]), 0, val, valPos, tv_size);
                                            break;
                                        case TiffDataType.TIFF_LONG:
                                        case TiffDataType.TIFF_IFD:
                                            Array.Copy(BitConverter.GetBytes((uint)ap[i + 1]), 0, val, valPos, tv_size);
                                            break;
                                        case TiffDataType.TIFF_SLONG:
                                            Array.Copy(BitConverter.GetBytes((int)ap[i + 1]), 0, val, valPos, tv_size);
                                            break;
                                        case TiffDataType.TIFF_RATIONAL:
                                        case TiffDataType.TIFF_SRATIONAL:
                                        case TiffDataType.TIFF_FLOAT:
                                            Array.Copy(BitConverter.GetBytes((float)ap[i + 1]), 0, val, valPos, tv_size);
                                            break;
                                        case TiffDataType.TIFF_DOUBLE:
                                            Array.Copy(BitConverter.GetBytes((double)ap[i + 1]), 0, val, valPos, tv_size);
                                            break;
                                        default:
                                            Array.Clear(val, valPos, tv_size);
                                            status = false;
                                            break;
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            if (!end && !badvalue && !badvalue32)
            {
                if (status)
                {
                    tif.setFieldBit(tif.FieldWithTag(tag).field_bit);
                    tif.m_flags |= Tiff.TIFF_DIRTYDIRECT;
                }
            }

            if (badvalue)
            {
                Tiff.ErrorExt(tif, tif.m_clientdata, module, "%s: Bad value %d for \"%s\" tag", tif.m_name, v, tif.FieldWithTag(tag).field_name);
                return false;
            }

            if (badvalue32)
            {
                Tiff.ErrorExt(tif, tif.m_clientdata, module, "%s: Bad value %ld for \"%s\" tag", tif.m_name, v32, tif.FieldWithTag(tag).field_name);
                return false;
            }

            return status;
        }

        /* tag get routine */
        public virtual object[] vgetfield(Tiff tif, TIFFTAG tag)
        {
            TiffDirectory td = tif.m_dir;
            object[] result = null;

            switch (tag)
            {
            case TIFFTAG.TIFFTAG_SUBFILETYPE:
                    result = new object[1];
                    result[0] = td.td_subfiletype;
                break;
            case TIFFTAG.TIFFTAG_IMAGEWIDTH:
                    result = new object[1];
                    result[0] = td.td_imagewidth;
                break;
            case TIFFTAG.TIFFTAG_IMAGELENGTH:
                    result = new object[1];
                    result[0] = td.td_imagelength;
                break;
            case TIFFTAG.TIFFTAG_BITSPERSAMPLE:
                    result = new object[1];
                    result[0] = td.td_bitspersample;
                break;
            case TIFFTAG.TIFFTAG_COMPRESSION:
                    result = new object[1];
                    result[0] = td.td_compression;
                break;
            case TIFFTAG.TIFFTAG_PHOTOMETRIC:
                    result = new object[1];
                    result[0] = td.td_photometric;
                break;
            case TIFFTAG.TIFFTAG_THRESHHOLDING:
                    result = new object[1];
                    result[0] = td.td_threshholding;
                break;
            case TIFFTAG.TIFFTAG_FILLORDER:
                    result = new object[1];
                    result[0] = td.td_fillorder;
                break;
            case TIFFTAG.TIFFTAG_ORIENTATION:
                    result = new object[1];
                    result[0] = td.td_orientation;
                break;
            case TIFFTAG.TIFFTAG_SAMPLESPERPIXEL:
                    result = new object[1];
                    result[0] = td.td_samplesperpixel;
                break;
            case TIFFTAG.TIFFTAG_ROWSPERSTRIP:
                    result = new object[1];
                    result[0] = td.td_rowsperstrip;
                break;
            case TIFFTAG.TIFFTAG_MINSAMPLEVALUE:
                    result = new object[1];
                    result[0] = td.td_minsamplevalue;
                break;
            case TIFFTAG.TIFFTAG_MAXSAMPLEVALUE:
                    result = new object[1];
                    result[0] = td.td_maxsamplevalue;
                break;
            case TIFFTAG.TIFFTAG_SMINSAMPLEVALUE:
                    result = new object[1];
                    result[0] = td.td_sminsamplevalue;
                break;
            case TIFFTAG.TIFFTAG_SMAXSAMPLEVALUE:
                    result = new object[1];
                    result[0] = td.td_smaxsamplevalue;
                break;
            case TIFFTAG.TIFFTAG_XRESOLUTION:
                    result = new object[1];
                    result[0] = td.td_xresolution;
                break;
            case TIFFTAG.TIFFTAG_YRESOLUTION:
                    result = new object[1];
                    result[0] = td.td_yresolution;
                break;
            case TIFFTAG.TIFFTAG_PLANARCONFIG:
                    result = new object[1];
                    result[0] = td.td_planarconfig;
                break;
            case TIFFTAG.TIFFTAG_XPOSITION:
                    result = new object[1];
                    result[0] = td.td_xposition;
                break;
            case TIFFTAG.TIFFTAG_YPOSITION:
                    result = new object[1];
                    result[0] = td.td_yposition;
                break;
            case TIFFTAG.TIFFTAG_RESOLUTIONUNIT:
                    result = new object[1];
                    result[0] = td.td_resolutionunit;
                break;
            case TIFFTAG.TIFFTAG_PAGENUMBER:
                    result = new object[2];
                    result[0] = td.td_pagenumber[0];
                    result[1] = td.td_pagenumber[1];
                break;
            case TIFFTAG.TIFFTAG_HALFTONEHINTS:
                    result = new object[2];
                    result[0] = td.td_halftonehints[0];
                    result[1] = td.td_halftonehints[1];
                break;
            case TIFFTAG.TIFFTAG_COLORMAP:
                    result = new object[3];
                    result[0] = td.td_colormap[0];
                    result[1] = td.td_colormap[1];
                    result[2] = td.td_colormap[2];
                break;
            case TIFFTAG.TIFFTAG_STRIPOFFSETS:
            case TIFFTAG.TIFFTAG_TILEOFFSETS:
                    result = new object[1];
                    result[0] = td.td_stripoffset;
                break;
            case TIFFTAG.TIFFTAG_STRIPBYTECOUNTS:
            case TIFFTAG.TIFFTAG_TILEBYTECOUNTS:
                    result = new object[1];
                    result[0] = td.td_stripbytecount;
                break;
            case TIFFTAG.TIFFTAG_MATTEING:
                    result = new object[1];
                    result[0] = (td.td_extrasamples == 1 && td.td_sampleinfo[0] == EXTRASAMPLE.EXTRASAMPLE_ASSOCALPHA);
                break;
            case TIFFTAG.TIFFTAG_EXTRASAMPLES:
                    result = new object[2];
                    result[0] = td.td_extrasamples;
                    result[1] = td.td_sampleinfo;
                break;
            case TIFFTAG.TIFFTAG_TILEWIDTH:
                    result = new object[1];
                    result[0] = td.td_tilewidth;
                break;
            case TIFFTAG.TIFFTAG_TILELENGTH:
                    result = new object[1];
                    result[0] = td.td_tilelength;
                break;
            case TIFFTAG.TIFFTAG_TILEDEPTH:
                    result = new object[1];
                    result[0] = td.td_tiledepth;
                break;
            case TIFFTAG.TIFFTAG_DATATYPE:
                switch (td.td_sampleformat)
                {
                    case SAMPLEFORMAT.SAMPLEFORMAT_UINT:
                        result = new object[1];
                        result[0] = DATATYPE_UINT;
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_INT:
                        result = new object[1];
                        result[0] = DATATYPE_INT;
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_IEEEFP:
                        result = new object[1];
                        result[0] = DATATYPE_IEEEFP;
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_VOID:
                        result = new object[1];
                        result[0] = DATATYPE_VOID;
                        break;
                }
                break;
            case TIFFTAG.TIFFTAG_SAMPLEFORMAT:
                    result = new object[1];
                    result[0] = td.td_sampleformat;
                break;
            case TIFFTAG.TIFFTAG_IMAGEDEPTH:
                    result = new object[1];
                    result[0] = td.td_imagedepth;
                break;
            case TIFFTAG.TIFFTAG_SUBIFD:
                    result = new object[2];
                    result[0] = td.td_nsubifd;
                    result[1] = td.td_subifd;
                break;
            case TIFFTAG.TIFFTAG_YCBCRPOSITIONING:
                    result = new object[1];
                    result[0] = td.td_ycbcrpositioning;
                break;
            case TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING:
                    result = new object[2];
                    result[0] = td.td_ycbcrsubsampling[0];
                    result[1] = td.td_ycbcrsubsampling[1];
                break;
            case TIFFTAG.TIFFTAG_TRANSFERFUNCTION:
                    result = new object[3];
                    result[0] = td.td_transferfunction[0];
                    if (td.td_samplesperpixel - td.td_extrasamples > 1)
                    {
                        result[1] = td.td_transferfunction[1];
                        result[2] = td.td_transferfunction[2];
                    }
                    break;
            case TIFFTAG.TIFFTAG_INKNAMES:
                    result = new object[1];
                    result[0] = td.td_inknames;
                break;
            default:
                {
                    /*
                    * This can happen if multiple images are open with
                    * different codecs which have private tags.  The
                    * global tag information table may then have tags
                    * that are valid for one file but not the other. 
                    * If the client tries to get a tag that is not valid
                    * for the image's codec then we'll arrive here.
                    */
                    TiffFieldInfo fip = tif.FindFieldInfo(tag, TiffDataType.TIFF_ANY);
                    if (fip == null || fip.field_bit != FIELD.FIELD_CUSTOM)
                    {
                        Tiff.ErrorExt(tif, tif.m_clientdata, "_TIFFVGetField", "%s: Invalid %stag \"%s\" (not supported by codec)", tif.m_name, Tiff.isPseudoTag(tag) ? "pseudo-": "", fip != null ? fip.field_name : "Unknown");
                        result = null;
                        break;
                    }

                    /*
                    * Do we have a custom value?
                    */
                    result = null;
                    for (int i = 0; i < td.td_customValueCount; i++)
                    {
                        TiffTagValue tv = td.td_customValues[i];
                        if (tv.info.field_tag != tag)
                            continue;

                        if (fip.field_passcount)
                        {
                            result = new object[2];

                            if (fip.field_readcount == Tiff.TIFF_VARIABLE2)
                                result[0] = tv.count;
                            else
                            {
                                /* Assume TIFF_VARIABLE */
                                result[0] = tv.count;
                            }
                            
                            result[1] = tv.value;
                        }
                        else
                        {
                            if ((fip.field_type == TiffDataType.TIFF_ASCII || fip.field_readcount == Tiff.TIFF_VARIABLE ||
                                fip.field_readcount == Tiff.TIFF_VARIABLE2 || fip.field_readcount == Tiff.TIFF_SPP || 
                                tv.count > 1) && fip.field_tag != TIFFTAG.TIFFTAG_PAGENUMBER && 
                                fip.field_tag != TIFFTAG.TIFFTAG_HALFTONEHINTS && 
                                fip.field_tag != TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING && 
                                fip.field_tag != TIFFTAG.TIFFTAG_DOTRANGE)
                            {
                                result = new object[1];
                                result[0] = tv.value;
                            }
                            else
                            {
                                result = new object[tv.count];
                                byte[] val = tv.value;
                                int valPos = 0;
                                for (int j = 0; j < tv.count; j++, valPos += Tiff.dataSize(tv.info.field_type))
                                {
                                    switch (fip.field_type)
                                    {
                                        case TiffDataType.TIFF_BYTE:
                                        case TiffDataType.TIFF_UNDEFINED:
                                            result[j + 1] = val[valPos];
                                            break;
                                        case TiffDataType.TIFF_SBYTE:
                                            result[j + 1] = val[valPos];
                                            break;
                                        case TiffDataType.TIFF_SHORT:
                                            result[j + 1] = BitConverter.ToUInt16(val, valPos);
                                            break;
                                        case TiffDataType.TIFF_SSHORT:
                                            result[j + 1] = BitConverter.ToInt16(val, valPos);
                                            break;
                                        case TiffDataType.TIFF_LONG:
                                        case TiffDataType.TIFF_IFD:
                                            result[j + 1] = BitConverter.ToUInt32(val, valPos);
                                            break;
                                        case TiffDataType.TIFF_SLONG:
                                            result[j + 1] = BitConverter.ToInt32(val, valPos);
                                            break;
                                        case TiffDataType.TIFF_RATIONAL:
                                        case TiffDataType.TIFF_SRATIONAL:
                                        case TiffDataType.TIFF_FLOAT:
                                            result[j + 1] = BitConverter.ToSingle(val, valPos);
                                            break;
                                        case TiffDataType.TIFF_DOUBLE:
                                            result[j + 1] = BitConverter.ToDouble(val, valPos);
                                            break;
                                        default:
                                            result = null;
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                break;
            }

            return result;
        }

        /* directory print routine */
        public virtual void printdir(Tiff tif, Stream fd, TiffPrintDirectoryFlags flags)
        {
        }

        /*
        * Install extra samples information.
        */
        private static bool setExtraSamples(TiffDirectory td, ref int v, params object[] ap)
        {
            /* XXX: Unassociated alpha data == 999 is a known Corel Draw bug, see below */
            const ushort EXTRASAMPLE_COREL_UNASSALPHA = 999;

            v = (int)ap[0];
            if (v > td.td_samplesperpixel)
                return false;

            EXTRASAMPLE[] va = ap[1] as EXTRASAMPLE[];
            if (v > 0 && va == null)
            {
                /* typically missing param */
                return false;
            }

            for (uint i = 0; i < v; i++)
            {
                if (va[i] > EXTRASAMPLE.EXTRASAMPLE_UNASSALPHA)
                {
                    /*
                    * XXX: Corel Draw is known to produce incorrect
                    * ExtraSamples tags which must be patched here if we
                    * want to be able to open some of the damaged TIFF
                    * files: 
                    */
                    if ((ushort)va[i] == EXTRASAMPLE_COREL_UNASSALPHA)
                        va[i] = EXTRASAMPLE.EXTRASAMPLE_UNASSALPHA;
                    else
                        return false;
                }
            }

            td.td_extrasamples = (ushort)v;
            td.td_sampleinfo = new EXTRASAMPLE[td.td_extrasamples];
            for (int i = 0; i < td.td_extrasamples; i++)
                td.td_sampleinfo[i] = va[i];

            return true;
        }

        private static int checkInkNamesString(Tiff tif, int slen, string s)
        {
            bool failed = false;
            ushort i = tif.m_dir.td_samplesperpixel;

            if (slen > 0)
            {
                int endPos = slen;
                int pos = 0;

                for (; i > 0; i--)
                {
                    for (; s[pos] != '\0'; pos++)
                    {
                        if (pos >= endPos)
                        {
                            failed = true;
                            break;
                        }
                    }

                    if (failed)
                        break;

                    pos++; /* skip \0 */
                }

                if (!failed)
                    return pos;
            }

            Tiff.ErrorExt(tif, tif.m_clientdata, "TIFFSetField", "%s: Invalid InkNames value; expecting %d names, found %d", tif.m_name, tif.m_dir.td_samplesperpixel, tif.m_dir.td_samplesperpixel - i);
            return 0;
        }

        private static void setNString(out string cpp, string cp, int n)
        {
            cpp = cp.Substring(0, n);
        }
    }
}
