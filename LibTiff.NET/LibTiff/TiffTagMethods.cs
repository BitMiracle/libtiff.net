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
        private const UInt16 DATATYPE_VOID = 0;       /* !untyped data */
        private const UInt16 DATATYPE_INT = 1;       /* !signed integer data */
        private const UInt16 DATATYPE_UINT = 2;       /* !unsigned integer data */
        private const UInt16 DATATYPE_IEEEFP = 3;       /* !IEEE floating point data */

        /* tag set routine */
        public virtual bool vsetfield(Tiff tif, TIFFTAG tag, params object[] ap)
        {
            static const char module[] = "_TIFFVSetField";

            TiffDirectory* td = tif->m_dir;
            bool status = true;
            uint v32 = 0;
            uint v = 0;

            bool end = false;
            bool badvalue = false;
            bool badvalue32 = false;

            switch (tag)
            {
            case TIFFTAG_SUBFILETYPE:
                td->td_subfiletype = va_arg(ap, uint);
                break;
            case TIFFTAG_IMAGEWIDTH:
                td->td_imagewidth = va_arg(ap, uint);
                break;
            case TIFFTAG_IMAGELENGTH:
                td->td_imagelength = va_arg(ap, uint);
                break;
            case TIFFTAG_BITSPERSAMPLE:
                td->td_bitspersample = (UInt16)va_arg(ap, int);
                /*
                * If the data require post-decoding processing to byte-swap
                * samples, set it up here.  Note that since tags are required
                * to be ordered, compression code can override this behaviour
                * in the setup method if it wants to roll the post decoding
                * work in with its normal work.
                */
                if ((tif->m_flags & Tiff::TIFF_SWAB) != 0)
                {
                    if (td->td_bitspersample == 16)
                        tif->m_postDecodeMethod = Tiff::pdmSwab16Bit;
                    else if (td->td_bitspersample == 24)
                        tif->m_postDecodeMethod = Tiff::pdmSwab24Bit;
                    else if (td->td_bitspersample == 32)
                        tif->m_postDecodeMethod = Tiff::pdmSwab32Bit;
                    else if (td->td_bitspersample == 64)
                        tif->m_postDecodeMethod = Tiff::pdmSwab64Bit;
                    else if (td->td_bitspersample == 128)
                    {
                        /* two 64's */
                        tif->m_postDecodeMethod = Tiff::pdmSwab64Bit;
                    }
                }
                break;
            case TIFFTAG_COMPRESSION:
                v = va_arg(ap, uint) & 0xffff;
                /*
                * If we're changing the compression scheme, the notify the
                * previous module so that it can cleanup any state it's
                * setup.
                */
                if (tif->fieldSet(FIELD_COMPRESSION))
                {
                    if (td->td_compression == v)
                        break;

                    tif->m_currentCodec->tif_cleanup();
                    tif->m_flags &= ~Tiff::TIFF_CODERSETUP;
                }
                /*
                * Setup new compression routine state.
                */
                status = tif->setCompressionScheme(v);
                if (status)
                    td->td_compression = (UInt16)v;
                else
                    status = false;
                break;
            case TIFFTAG_PHOTOMETRIC:
                td->td_photometric = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_THRESHHOLDING:
                td->td_threshholding = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_FILLORDER:
                v = va_arg(ap, uint);
                if (v != FILLORDER_LSB2MSB && v != FILLORDER_MSB2LSB)
                {
                    badvalue = true;
                    break;
                }

                td->td_fillorder = (UInt16)v;
                break;
            case TIFFTAG_ORIENTATION:
                v = va_arg(ap, uint);
                if (v < ORIENTATION.ORIENTATION_TOPLEFT || ORIENTATION.ORIENTATION_LEFTBOT < v)
                {
                    badvalue = true;
                    break;
                }
                else
                    td->td_orientation = (UInt16)v;
                break;
            case TIFFTAG_SAMPLESPERPIXEL:
                /* XXX should cross check -- e.g. if pallette, then 1 */
                v = va_arg(ap, uint);
                if (v == 0)
                {
                    badvalue = true;
                    break;
                }

                td->td_samplesperpixel = (UInt16)v;
                break;
            case TIFFTAG_ROWSPERSTRIP:
                v32 = va_arg(ap, uint);
                if (v32 == 0)
                {
                    badvalue32 = true;
                    break;
                }

                td->td_rowsperstrip = v32;
                if (!tif->fieldSet(FIELD_TILEDIMENSIONS))
                {
                    td->td_tilelength = v32;
                    td->td_tilewidth = td->td_imagewidth;
                }
                break;
            case TIFFTAG_MINSAMPLEVALUE:
                td->td_minsamplevalue = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_MAXSAMPLEVALUE:
                td->td_maxsamplevalue = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_SMINSAMPLEVALUE:
                td->td_sminsamplevalue = va_arg(ap, double);
                break;
            case TIFFTAG_SMAXSAMPLEVALUE:
                td->td_smaxsamplevalue = va_arg(ap, double);
                break;
            case TIFFTAG_XRESOLUTION:
                td->td_xresolution = (float)va_arg(ap, double);
                break;
            case TIFFTAG_YRESOLUTION:
                td->td_yresolution = (float)va_arg(ap, double);
                break;
            case TIFFTAG_PLANARCONFIG:
                v = va_arg(ap, uint);
                if (v != PLANARCONFIG_CONTIG && v != PLANARCONFIG_SEPARATE)
                {
                    badvalue = true;
                    break;
                }
                td->td_planarconfig = (UInt16)v;
                break;
            case TIFFTAG_XPOSITION:
                td->td_xposition = (float)va_arg(ap, double);
                break;
            case TIFFTAG_YPOSITION:
                td->td_yposition = (float)va_arg(ap, double);
                break;
            case TIFFTAG_RESOLUTIONUNIT:
                v = va_arg(ap, uint);
                if (v < RESUNIT.RESUNIT_NONE || RESUNIT.RESUNIT_CENTIMETER < v)
                {
                    badvalue = true;
                    break;
                }

                td->td_resolutionunit = (UInt16)v;
                break;
            case TIFFTAG_PAGENUMBER:
                td->td_pagenumber[0] = (UInt16)va_arg(ap, int);
                td->td_pagenumber[1] = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_HALFTONEHINTS:
                td->td_halftonehints[0] = (UInt16)va_arg(ap, int);
                td->td_halftonehints[1] = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_COLORMAP:
                v32 = (uint)(1L << td->td_bitspersample);
                Tiff::setShortArray(td->td_colormap[0], va_arg(ap, UInt16*), v32);
                Tiff::setShortArray(td->td_colormap[1], va_arg(ap, UInt16*), v32);
                Tiff::setShortArray(td->td_colormap[2], va_arg(ap, UInt16*), v32);
                break;
            case TIFFTAG_EXTRASAMPLES:
                if (!setExtraSamples(td, ap, v))
                {
                    badvalue = true;
                    break;
                }

                break;
            case TIFFTAG_MATTEING:
                td->td_extrasamples = (UInt16)(va_arg(ap, int) != 0);
                if (td->td_extrasamples)
                {
                    UInt16 sv[1];
                    sv[0] = EXTRASAMPLE.EXTRASAMPLE_ASSOCALPHA;
                    Tiff::setShortArray(td->td_sampleinfo, sv, 1);
                }
                break;
            case TIFFTAG_TILEWIDTH:
                v32 = va_arg(ap, uint);
                if ((v32 % 16) != 0)
                {
                    if (tif->m_mode != O_RDONLY)
                    {
                        badvalue32 = true;
                        break;
                    }

                    Tiff::WarningExt(tif, tif->m_clientdata, tif->m_name, "Nonstandard tile width %d, convert file", v32);
                }
                td->td_tilewidth = v32;
                tif->m_flags |= Tiff::TIFF_ISTILED;
                break;
            case TIFFTAG_TILELENGTH:
                v32 = va_arg(ap, uint);
                if ((v32 % 16) != 0)
                {
                    if (tif->m_mode != O_RDONLY)
                    {
                        badvalue32 = true;
                        break;
                    }

                    Tiff::WarningExt(tif, tif->m_clientdata, tif->m_name, "Nonstandard tile length %d, convert file", v32);
                }
                td->td_tilelength = v32;
                tif->m_flags |= Tiff::TIFF_ISTILED;
                break;
            case TIFFTAG_TILEDEPTH:
                v32 = va_arg(ap, uint);
                if (v32 == 0)
                {
                    badvalue32 = true;
                    break;
                }

                td->td_tiledepth = v32;
                break;
            case TIFFTAG_DATATYPE:
                v = va_arg(ap, uint);
                switch (v)
                {
                case DATATYPE_VOID:
                    v = SAMPLEFORMAT_VOID;
                    break;
                case DATATYPE_INT:
                    v = SAMPLEFORMAT_INT;
                    break;
                case DATATYPE_UINT:
                    v = SAMPLEFORMAT_UINT;
                    break;
                case DATATYPE_IEEEFP:
                    v = SAMPLEFORMAT_IEEEFP;
                    break;
                default:
                    badvalue = true;
                    break;
                }

                if (!badvalue)
                    td->td_sampleformat = (UInt16)v;

                break;
            case TIFFTAG_SAMPLEFORMAT:
                v = va_arg(ap, uint);
                if (v < SAMPLEFORMAT_UINT || SAMPLEFORMAT_COMPLEXIEEEFP < v)
                {
                    badvalue = true;
                    break;
                }

                td->td_sampleformat = (UInt16)v;

                /*  Try to fix up the SWAB function for complex data. */
                if (td->td_sampleformat == SAMPLEFORMAT_COMPLEXINT && td->td_bitspersample == 32 && tif->m_postDecodeMethod == Tiff::pdmSwab32Bit)
                    tif->m_postDecodeMethod = Tiff::pdmSwab16Bit;
                else if ((td->td_sampleformat == SAMPLEFORMAT_COMPLEXINT || td->td_sampleformat == SAMPLEFORMAT_COMPLEXIEEEFP) && td->td_bitspersample == 64 && tif->m_postDecodeMethod == Tiff::pdmSwab64Bit)
                    tif->m_postDecodeMethod = Tiff::pdmSwab32Bit;
                break;
            case TIFFTAG_IMAGEDEPTH:
                td->td_imagedepth = va_arg(ap, uint);
                break;
            case TIFFTAG_SUBIFD:
                if ((tif->m_flags & Tiff::TIFF_INSUBIFD) == 0)
                {
                    td->td_nsubifd = (UInt16)va_arg(ap, int);
                    Tiff::setLongArray(td->td_subifd, va_arg(ap, uint*), td->td_nsubifd);
                }
                else
                {
                    Tiff::ErrorExt(tif, tif->m_clientdata, module, "%s: Sorry, cannot nest SubIFDs", tif->m_name);
                    status = false;
                }
                break;
            case TIFFTAG_YCBCRPOSITIONING:
                td->td_ycbcrpositioning = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_YCBCRSUBSAMPLING:
                td->td_ycbcrsubsampling[0] = (UInt16)va_arg(ap, int);
                td->td_ycbcrsubsampling[1] = (UInt16)va_arg(ap, int);
                break;
            case TIFFTAG_TRANSFERFUNCTION:
                v = (td->td_samplesperpixel - td->td_extrasamples) > 1 ? 3 : 1;
                for (uint i = 0; i < v; i++)
                {
                    Tiff::setShortArray(td->td_transferfunction[i], va_arg(ap, UInt16*), 1L << td->td_bitspersample);
                }
                break;
            case TIFFTAG_INKNAMES:
                {
                    v = va_arg(ap, uint);
                    char* s = va_arg(ap, char*);
                    v = checkInkNamesString(tif, v, s);
                    status = v > 0;
                    if (v > 0)
                    {
                        setNString(td->td_inknames, s, v);
                        td->td_inknameslen = v;
                    }
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
                    const TiffFieldInfo* fip = tif->FindFieldInfo(tag, TIFF_ANY);
                    if (fip == NULL || fip->field_bit != FIELD_CUSTOM)
                    {
                        Tiff::ErrorExt(tif, tif->m_clientdata, module, "%s: Invalid %stag \"%s\" (not supported by codec)", tif->m_name, isPseudoTag(tag) ? "pseudo-": "", fip ? fip->field_name : "Unknown");
                        status = false;
                        break;
                    }

                    /*
                    * Find the existing entry for this custom value.
                    */
                    int tvIndex = -1;
                    for (int iCustom = 0; iCustom < td->td_customValueCount; iCustom++)
                    {
                        if (td->td_customValues[iCustom].info->field_tag == tag)
                        {
                            delete td->td_customValues[iCustom].value;
                            td->td_customValues[iCustom].value = NULL;
                            break;
                        }
                    }

                    /*
                    * Grow the custom list if the entry was not found.
                    */
                    if (tvIndex == -1)
                    {
                        td->td_customValueCount++;
                        TiffTagValue* new_customValues = Tiff::Realloc(td->td_customValues, td->td_customValueCount - 1, td->td_customValueCount);
                        if (new_customValues == NULL)
                        {
                            Tiff::ErrorExt(tif, tif->m_clientdata, module, "%s: Failed to allocate space for list of custom values", tif->m_name);
                            status = false;
                            end = true;
                            break;
                        }

                        delete td->td_customValues;
                        td->td_customValues = new_customValues;

                        tvIndex = td->td_customValueCount - 1;
                        td->td_customValues[tvIndex].info = fip;
                        td->td_customValues[tvIndex].value = NULL;
                        td->td_customValues[tvIndex].count = 0;
                    }

                    /*
                    * Set custom value ... save a copy of the custom tag value.
                    */
                    int tv_size = Tiff::dataSize(fip->field_type);
                    if (tv_size == 0)
                    {
                        status = false;
                        Tiff::ErrorExt(tif, tif->m_clientdata, module, "%s: Bad field type %d for \"%s\"", tif->m_name, fip->field_type, fip->field_name);
                        end = true;
                        break;
                    }

                    if (fip->field_passcount)
                    {
                        if (fip->field_writecount == TIFF_VARIABLE2)
                            td->td_customValues[tvIndex].count = (uint)va_arg(ap, uint);
                        else
                            td->td_customValues[tvIndex].count = (int)va_arg(ap, int);
                    }
                    else if (fip->field_writecount == TIFF_VARIABLE || fip->field_writecount == TIFF_VARIABLE2)
                        td->td_customValues[tvIndex].count = 1;
                    else if (fip->field_writecount == TIFF_SPP)
                        td->td_customValues[tvIndex].count = td->td_samplesperpixel;
                    else
                        td->td_customValues[tvIndex].count = fip->field_writecount;

                    if (fip->field_type == TIFF_ASCII)
                        Tiff::setString((char*&)td->td_customValues[tvIndex].value, va_arg(ap, char*));
                    else
                    {
                        td->td_customValues[tvIndex].value = new byte[tv_size * td->td_customValues[tvIndex].count];
                        if (td->td_customValues[tvIndex].value == NULL)
                        {
                            Tiff::ErrorExt(tif, tif->m_clientdata, tif->m_name, "No space Tag Value");
                            status = false;
                            end = true;
                            break;
                        }

                        if ((fip->field_passcount || fip->field_writecount == TIFF_VARIABLE || fip->field_writecount == TIFF_VARIABLE2 || fip->field_writecount == TIFF_SPP || td->td_customValues[tvIndex].count > 1) && fip->field_tag != TIFFTAG_PAGENUMBER && fip->field_tag != TIFFTAG_HALFTONEHINTS && fip->field_tag != TIFFTAG_YCBCRSUBSAMPLING && fip->field_tag != TIFFTAG_DOTRANGE)
                        {
                            memcpy(td->td_customValues[tvIndex].value, va_arg(ap, void*), td->td_customValues[tvIndex].count * tv_size);
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
                            byte[] val = td->td_customValues[tvIndex].value;
                            int valPos = 0;
                            for (int i = 0; i < td->td_customValues[tvIndex].count; i++, valPos += tv_size)
                            {
                                switch (fip->field_type)
                                {
                                case TIFF_BYTE:
                                case TIFF_UNDEFINED:
                                    {
                                        byte v = (byte)va_arg(ap, int);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_SBYTE:
                                    {
                                        sbyte v = (sbyte)va_arg(ap, int);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_SHORT:
                                    {
                                        UInt16 v = (UInt16)va_arg(ap, int);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_SSHORT:
                                    {
                                        Int16 v = (Int16)va_arg(ap, int);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_LONG:
                                case TIFF_IFD:
                                    {
                                        uint v = va_arg(ap, uint);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_SLONG:
                                    {
                                        int v = va_arg(ap, int);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_RATIONAL:
                                case TIFF_SRATIONAL:
                                case TIFF_FLOAT:
                                    {
                                        float v = (float)va_arg(ap, double);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                case TIFF_DOUBLE:
                                    {
                                        double v = va_arg(ap, double);
                                        memcpy(&val[valPos], &v, tv_size);
                                    }
                                    break;
                                default:
                                    memset(&val[valPos], 0, tv_size);
                                    status = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (!end && !badvalue && !badvalue32)
            {
                if (status)
                {
                    tif->setFieldBit(tif->FieldWithTag(tag)->field_bit);
                    tif->m_flags |= Tiff::TIFF_DIRTYDIRECT;
                }
            }

            va_end(ap);

            if (badvalue)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, module, "%s: Bad value %d for \"%s\" tag", tif->m_name, v, tif->FieldWithTag(tag)->field_name);
                return false;
            }

            if (badvalue32)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, module, "%s: Bad value %ld for \"%s\" tag", tif->m_name, v32, tif->FieldWithTag(tag)->field_name);
                return false;
            }

            return status;
        }
        
        /* tag get routine */
        public virtual object[] vgetfield(Tiff tif, TIFFTAG tag)
        {
            TiffDirectory* td = tif->m_dir;
            bool ret_val = true;

            switch (tag)
            {
            case TIFFTAG_SUBFILETYPE:
                *va_arg(ap, uint*) = td->td_subfiletype;
                break;
            case TIFFTAG_IMAGEWIDTH:
                *va_arg(ap, uint*) = td->td_imagewidth;
                break;
            case TIFFTAG_IMAGELENGTH:
                *va_arg(ap, uint*) = td->td_imagelength;
                break;
            case TIFFTAG_BITSPERSAMPLE:
                *va_arg(ap, UInt16*) = td->td_bitspersample;
                break;
            case TIFFTAG_COMPRESSION:
                *va_arg(ap, UInt16*) = td->td_compression;
                break;
            case TIFFTAG_PHOTOMETRIC:
                *va_arg(ap, UInt16*) = td->td_photometric;
                break;
            case TIFFTAG_THRESHHOLDING:
                *va_arg(ap, UInt16*) = td->td_threshholding;
                break;
            case TIFFTAG_FILLORDER:
                *va_arg(ap, UInt16*) = td->td_fillorder;
                break;
            case TIFFTAG_ORIENTATION:
                *va_arg(ap, UInt16*) = td->td_orientation;
                break;
            case TIFFTAG_SAMPLESPERPIXEL:
                *va_arg(ap, UInt16*) = td->td_samplesperpixel;
                break;
            case TIFFTAG_ROWSPERSTRIP:
                *va_arg(ap, uint*) = td->td_rowsperstrip;
                break;
            case TIFFTAG_MINSAMPLEVALUE:
                *va_arg(ap, UInt16*) = td->td_minsamplevalue;
                break;
            case TIFFTAG_MAXSAMPLEVALUE:
                *va_arg(ap, UInt16*) = td->td_maxsamplevalue;
                break;
            case TIFFTAG_SMINSAMPLEVALUE:
                *va_arg(ap, double*) = td->td_sminsamplevalue;
                break;
            case TIFFTAG_SMAXSAMPLEVALUE:
                *va_arg(ap, double*) = td->td_smaxsamplevalue;
                break;
            case TIFFTAG_XRESOLUTION:
                *va_arg(ap, float*) = td->td_xresolution;
                break;
            case TIFFTAG_YRESOLUTION:
                *va_arg(ap, float*) = td->td_yresolution;
                break;
            case TIFFTAG_PLANARCONFIG:
                *va_arg(ap, UInt16*) = td->td_planarconfig;
                break;
            case TIFFTAG_XPOSITION:
                *va_arg(ap, float*) = td->td_xposition;
                break;
            case TIFFTAG_YPOSITION:
                *va_arg(ap, float*) = td->td_yposition;
                break;
            case TIFFTAG_RESOLUTIONUNIT:
                *va_arg(ap, UInt16*) = td->td_resolutionunit;
                break;
            case TIFFTAG_PAGENUMBER:
                *va_arg(ap, UInt16*) = td->td_pagenumber[0];
                *va_arg(ap, UInt16*) = td->td_pagenumber[1];
                break;
            case TIFFTAG_HALFTONEHINTS:
                *va_arg(ap, UInt16*) = td->td_halftonehints[0];
                *va_arg(ap, UInt16*) = td->td_halftonehints[1];
                break;
            case TIFFTAG_COLORMAP:
                *va_arg(ap, UInt16**) = td->td_colormap[0];
                *va_arg(ap, UInt16**) = td->td_colormap[1];
                *va_arg(ap, UInt16**) = td->td_colormap[2];
                break;
            case TIFFTAG_STRIPOFFSETS:
            case TIFFTAG_TILEOFFSETS:
                *va_arg(ap, uint**) = td->td_stripoffset;
                break;
            case TIFFTAG_STRIPBYTECOUNTS:
            case TIFFTAG_TILEBYTECOUNTS:
                *va_arg(ap, uint**) = td->td_stripbytecount;
                break;
            case TIFFTAG_MATTEING:
                *va_arg(ap, UInt16*) = (td->td_extrasamples == 1 && td->td_sampleinfo[0] == EXTRASAMPLE.EXTRASAMPLE_ASSOCALPHA);
                break;
            case TIFFTAG_EXTRASAMPLES:
                *va_arg(ap, UInt16*) = td->td_extrasamples;
                *va_arg(ap, UInt16**) = td->td_sampleinfo;
                break;
            case TIFFTAG_TILEWIDTH:
                *va_arg(ap, uint*) = td->td_tilewidth;
                break;
            case TIFFTAG_TILELENGTH:
                *va_arg(ap, uint*) = td->td_tilelength;
                break;
            case TIFFTAG_TILEDEPTH:
                *va_arg(ap, uint*) = td->td_tiledepth;
                break;
            case TIFFTAG_DATATYPE:
                switch (td->td_sampleformat)
                {
                case SAMPLEFORMAT_UINT:
                    *va_arg(ap, UInt16*) = DATATYPE_UINT;
                    break;
                case SAMPLEFORMAT_INT:
                    *va_arg(ap, UInt16*) = DATATYPE_INT;
                    break;
                case SAMPLEFORMAT_IEEEFP:
                    *va_arg(ap, UInt16*) = DATATYPE_IEEEFP;
                    break;
                case SAMPLEFORMAT_VOID:
                    *va_arg(ap, UInt16*) = DATATYPE_VOID;
                    break;
                }
                break;
            case TIFFTAG_SAMPLEFORMAT:
                *va_arg(ap, UInt16*) = td->td_sampleformat;
                break;
            case TIFFTAG_IMAGEDEPTH:
                *va_arg(ap, uint*) = td->td_imagedepth;
                break;
            case TIFFTAG_SUBIFD:
                *va_arg(ap, UInt16*) = td->td_nsubifd;
                *va_arg(ap, uint**) = td->td_subifd;
                break;
            case TIFFTAG_YCBCRPOSITIONING:
                *va_arg(ap, UInt16*) = td->td_ycbcrpositioning;
                break;
            case TIFFTAG_YCBCRSUBSAMPLING:
                *va_arg(ap, UInt16*) = td->td_ycbcrsubsampling[0];
                *va_arg(ap, UInt16*) = td->td_ycbcrsubsampling[1];
                break;
            case TIFFTAG_TRANSFERFUNCTION:
                *va_arg(ap, UInt16**) = td->td_transferfunction[0];
                if (td->td_samplesperpixel - td->td_extrasamples > 1)
                {
                    *va_arg(ap, UInt16**) = td->td_transferfunction[1];
                    *va_arg(ap, UInt16**) = td->td_transferfunction[2];
                }
                break;
            case TIFFTAG_INKNAMES:
                *va_arg(ap, char**) = td->td_inknames;
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
                    const TiffFieldInfo* fip = tif->FindFieldInfo(tag, TIFF_ANY);
                    if (fip == NULL || fip->field_bit != FIELD_CUSTOM)
                    {
                        Tiff::ErrorExt(tif, tif->m_clientdata, "_TIFFVGetField", "%s: Invalid %stag \"%s\" (not supported by codec)", tif->m_name, isPseudoTag(tag) ? "pseudo-": "", fip ? fip->field_name : "Unknown");
                        ret_val = false;
                        break;
                    }

                    /*
                    * Do we have a custom value?
                    */
                    ret_val = false;
                    for (int i = 0; i < td->td_customValueCount; i++)
                    {
                        TiffTagValue* tv = &td->td_customValues[i];
                        if (tv->info->field_tag != tag)
                            continue;

                        if (fip->field_passcount)
                        {
                            if (fip->field_readcount == TIFF_VARIABLE2)
                                *va_arg(ap, uint*) = (uint)tv->count;
                            else
                            {
                                /* Assume TIFF_VARIABLE */
                                *va_arg(ap, UInt16*) = (UInt16)tv->count;
                            }
                            
                            *va_arg(ap, void**) = tv->value;
                            ret_val = true;
                        }
                        else
                        {
                            if ((fip->field_type == TIFF_ASCII || fip->field_readcount == TIFF_VARIABLE || fip->field_readcount == TIFF_VARIABLE2 || fip->field_readcount == TIFF_SPP || tv->count > 1) && fip->field_tag != TIFFTAG_PAGENUMBER && fip->field_tag != TIFFTAG_HALFTONEHINTS && fip->field_tag != TIFFTAG_YCBCRSUBSAMPLING && fip->field_tag != TIFFTAG_DOTRANGE)
                            {
                                *va_arg(ap, void**) = tv->value;
                                ret_val = true;
                            }
                            else
                            {
                                byte[] val = (byte*)tv->value;
                                int valPos = 0;
                                for (int j = 0; j < tv->count; j++, valPos += Tiff::dataSize(tv->info->field_type))
                                {
                                    switch (fip->field_type)
                                    {
                                    case TIFF_BYTE:
                                    case TIFF_UNDEFINED:
                                        *va_arg(ap, byte*) = *(byte*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_SBYTE:
                                        *va_arg(ap, sbyte*) = *(sbyte*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_SHORT:
                                        *va_arg(ap, UInt16*) = *(UInt16*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_SSHORT:
                                        *va_arg(ap, Int16*) = *(Int16*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_LONG:
                                    case TIFF_IFD:
                                        *va_arg(ap, uint*) = *(uint*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_SLONG:
                                        *va_arg(ap, int*) = *(int*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_RATIONAL:
                                    case TIFF_SRATIONAL:
                                    case TIFF_FLOAT:
                                        *va_arg(ap, float*) = *(float*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    case TIFF_DOUBLE:
                                        *va_arg(ap, double*) = *(double*)&val[valPos];
                                        ret_val = true;
                                        break;
                                    default:
                                        ret_val = false;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return ret_val;
        }

        /* directory print routine */
        public virtual void printdir(Tiff tif, Stream fd, TiffPrintDirectoryFlags flags)
        {
        }

        /* is tag value normal or pseudo */
        private static bool isPseudoTag(uint t)
        {
            return (t > 0xffff);
        }

        /*
        * Install extra samples information.
        */
        private static bool setExtraSamples(TiffDirectory td, ref uint v, params object[] ap)
        {
            /* XXX: Unassociated alpha data == 999 is a known Corel Draw bug, see below */
            static const UInt16 EXTRASAMPLE_COREL_UNASSALPHA = 999;

            v = va_arg(ap, uint);
            if (v > td->td_samplesperpixel)
                return 0;

            UInt16[] va = va_arg(ap, UInt16*);
            if (v > 0 && va == NULL)
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
                    if (va[i] == EXTRASAMPLE_COREL_UNASSALPHA)
                        va[i] = EXTRASAMPLE.EXTRASAMPLE_UNASSALPHA;
                    else
                        return false;
                }
            }

            td->td_extrasamples = (UInt16)v;
            Tiff::setShortArray(td->td_sampleinfo, va, td->td_extrasamples);
            return true;
        }

        private static uint checkInkNamesString(Tiff tif, uint slen, string s)
        {
            bool failed = false;
            UInt16 i = tif->m_dir->td_samplesperpixel;

            if (slen > 0)
            {
                int endPos = slen;
                int pos = 0;

                /*const char* ep = s + slen;
                const char* cp = s;*/
                for ( ; i > 0; i--)
                {
                    for ( ; s[pos] != '\0'; pos++)
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

            Tiff::ErrorExt(tif, tif->m_clientdata, "TIFFSetField", "%s: Invalid InkNames value; expecting %d names, found %d", tif->m_name, tif->m_dir->td_samplesperpixel, tif->m_dir->td_samplesperpixel - i);
            return 0;
        }

        private static void setNString(out string cpp, string cp, uint n)
        {
            delete cpp;

            size_t sz = n + 1;
            cpp = new char[sz];

            strncpy(cpp, cp, n);
            cpp[sz - 1] = 0;
        }
    }
}
