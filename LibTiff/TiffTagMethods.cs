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

using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// 
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffTagMethods
    {
        //
        // These are used in the backwards compatibility code...
        // 

        /// <summary>
        /// !untyped data
        /// </summary>
        private const short DATATYPE_VOID = 0;

        /// <summary>
        /// !signed integer data
        /// </summary>
        private const short DATATYPE_INT = 1;

        /// <summary>
        /// !unsigned integer data
        /// </summary>
        private const short DATATYPE_UINT = 2;

        /// <summary>
        /// !IEEE floating point data
        /// </summary>
        private const short DATATYPE_IEEEFP = 3;

        /// <summary>
        /// Sets the tag field.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="ap">The field value.</param>
        /// <returns><c>true</c> if set successfully; otherwise, <c>false</c></returns>
        public virtual bool SetField(Tiff tif, TiffTag tag, FieldValue[] ap)
        {
            const string module = "vsetfield";

            TiffDirectory td = tif.m_dir;
            bool status = true;
            int v32 = 0;
            int v = 0;

            bool end = false;
            bool badvalue = false;
            bool badvalue32 = false;

            switch (tag)
            {
                case TiffTag.SUBFILETYPE:
                    td.td_subfiletype = (FileType)ap[0].ToByte();
                    break;
                case TiffTag.IMAGEWIDTH:
                    td.td_imagewidth = ap[0].ToInt();
                    break;
                case TiffTag.IMAGELENGTH:
                    td.td_imagelength = ap[0].ToInt();
                    break;
                case TiffTag.BITSPERSAMPLE:
                    td.td_bitspersample = ap[0].ToShort();
                    /*
                    * If the data require post-decoding processing to byte-swap
                    * samples, set it up here.  Note that since tags are required
                    * to be ordered, compression code can override this behavior
                    * in the setup method if it wants to roll the post decoding
                    * work in with its normal work.
                    */
                    if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
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
                case TiffTag.COMPRESSION:
                    v = ap[0].ToInt() & 0xffff;
                    Compression comp = (Compression)v;
                    /*
                    * If we're changing the compression scheme, the notify the
                    * previous module so that it can cleanup any state it's
                    * setup.
                    */
                    if (tif.fieldSet(FieldBit.FIELD_COMPRESSION))
                    {
                        if (td.td_compression == comp)
                            break;

                        tif.m_currentCodec.Cleanup();
                        tif.m_flags &= ~TiffFlags.CODERSETUP;
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

                case TiffTag.PHOTOMETRIC:
                    td.td_photometric = (Photometric)ap[0].ToInt();
                    break;
                case TiffTag.THRESHHOLDING:
                    td.td_threshholding = (Threshold)ap[0].ToByte();
                    break;
                case TiffTag.FILLORDER:
                    v = ap[0].ToInt();
                    FillOrder fo = (FillOrder)v;
                    if (fo != FillOrder.LSB2MSB && fo != FillOrder.MSB2LSB)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_fillorder = fo;
                    break;
                case TiffTag.ORIENTATION:
                    v = ap[0].ToInt();
                    Orientation or = (Orientation)v;
                    if (or < Orientation.TOPLEFT || Orientation.LEFTBOT < or)
                    {
                        badvalue = true;
                        break;
                    }
                    else
                        td.td_orientation = or;
                    break;
                case TiffTag.SAMPLESPERPIXEL:
                    /* XXX should cross check -- e.g. if pallette, then 1 */
                    v = ap[0].ToInt();
                    if (v == 0)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_samplesperpixel = (short)v;
                    break;
                case TiffTag.ROWSPERSTRIP:
                    v32 = ap[0].ToInt();
                    if (v32 == 0)
                    {
                        badvalue32 = true;
                        break;
                    }

                    td.td_rowsperstrip = v32;
                    if (!tif.fieldSet(FieldBit.FIELD_TILEDIMENSIONS))
                    {
                        td.td_tilelength = v32;
                        td.td_tilewidth = td.td_imagewidth;
                    }
                    break;
                case TiffTag.MINSAMPLEVALUE:
                    td.td_minsamplevalue = ap[0].ToShort();
                    break;
                case TiffTag.MAXSAMPLEVALUE:
                    td.td_maxsamplevalue = ap[0].ToShort();
                    break;
                case TiffTag.SMINSAMPLEVALUE:
                    td.td_sminsamplevalue = ap[0].ToDouble();
                    break;
                case TiffTag.SMAXSAMPLEVALUE:
                    td.td_smaxsamplevalue = ap[0].ToDouble();
                    break;
                case TiffTag.XRESOLUTION:
                    td.td_xresolution = ap[0].ToFloat();
                    break;
                case TiffTag.YRESOLUTION:
                    td.td_yresolution = ap[0].ToFloat();
                    break;
                case TiffTag.PLANARCONFIG:
                    v = ap[0].ToInt();
                    PlanarConfig pc = (PlanarConfig)v;
                    if (pc != PlanarConfig.CONTIG && pc != PlanarConfig.SEPARATE)
                    {
                        badvalue = true;
                        break;
                    }
                    td.td_planarconfig = pc;
                    break;
                case TiffTag.XPOSITION:
                    td.td_xposition = ap[0].ToFloat();
                    break;
                case TiffTag.YPOSITION:
                    td.td_yposition = ap[0].ToFloat();
                    break;
                case TiffTag.RESOLUTIONUNIT:
                    v = ap[0].ToInt();
                    ResUnit ru = (ResUnit)v;
                    if (ru < ResUnit.NONE || ResUnit.CENTIMETER < ru)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_resolutionunit = ru;
                    break;
                case TiffTag.PAGENUMBER:
                    td.td_pagenumber[0] = ap[0].ToShort();
                    td.td_pagenumber[1] = ap[1].ToShort();
                    break;
                case TiffTag.HALFTONEHINTS:
                    td.td_halftonehints[0] = ap[0].ToShort();
                    td.td_halftonehints[1] = ap[1].ToShort();
                    break;
                case TiffTag.COLORMAP:
                    v32 = 1 << td.td_bitspersample;
                    Tiff.setShortArray(out td.td_colormap[0], ap[0].ToShortArray(), v32);
                    Tiff.setShortArray(out td.td_colormap[1], ap[1].ToShortArray(), v32);
                    Tiff.setShortArray(out td.td_colormap[2], ap[2].ToShortArray(), v32);
                    break;
                case TiffTag.EXTRASAMPLES:
                    if (!setExtraSamples(td, ref v, ap))
                    {
                        badvalue = true;
                        break;
                    }

                    break;
                case TiffTag.MATTEING:
                    if (ap[0].ToShort() != 0)
                        td.td_extrasamples = 1;
                    else
                        td.td_extrasamples = 0;

                    if (td.td_extrasamples != 0)
                    {
                        td.td_sampleinfo = new ExtraSample[1];
                        td.td_sampleinfo[0] = ExtraSample.ASSOCALPHA;
                    }
                    break;
                case TiffTag.TILEWIDTH:
                    v32 = ap[0].ToInt();
                    if ((v32 % 16) != 0)
                    {
                        if (tif.m_mode != Tiff.O_RDONLY)
                        {
                            badvalue32 = true;
                            break;
                        }

                        Tiff.WarningExt(tif, tif.m_clientdata, tif.m_name,
                            "Nonstandard tile width {0}, convert file", v32);
                    }
                    td.td_tilewidth = v32;
                    tif.m_flags |= TiffFlags.ISTILED;
                    break;
                case TiffTag.TILELENGTH:
                    v32 = ap[0].ToInt();
                    if ((v32 % 16) != 0)
                    {
                        if (tif.m_mode != Tiff.O_RDONLY)
                        {
                            badvalue32 = true;
                            break;
                        }

                        Tiff.WarningExt(tif, tif.m_clientdata, tif.m_name,
                            "Nonstandard tile length {0}, convert file", v32);
                    }
                    td.td_tilelength = v32;
                    tif.m_flags |= TiffFlags.ISTILED;
                    break;
                case TiffTag.TILEDEPTH:
                    v32 = ap[0].ToInt();
                    if (v32 == 0)
                    {
                        badvalue32 = true;
                        break;
                    }

                    td.td_tiledepth = v32;
                    break;
                case TiffTag.DATATYPE:
                    v = ap[0].ToInt();
                    SampleFormat sf = SampleFormat.VOID;
                    switch (v)
                    {
                        case DATATYPE_VOID:
                            sf = SampleFormat.VOID;
                            break;
                        case DATATYPE_INT:
                            sf = SampleFormat.INT;
                            break;
                        case DATATYPE_UINT:
                            sf = SampleFormat.UINT;
                            break;
                        case DATATYPE_IEEEFP:
                            sf = SampleFormat.IEEEFP;
                            break;
                        default:
                            badvalue = true;
                            break;
                    }

                    if (!badvalue)
                        td.td_sampleformat = sf;

                    break;
                case TiffTag.SAMPLEFORMAT:
                    v = ap[0].ToInt();
                    sf = (SampleFormat)v;
                    if (sf < SampleFormat.UINT || SampleFormat.COMPLEXIEEEFP < sf)
                    {
                        badvalue = true;
                        break;
                    }

                    td.td_sampleformat = sf;

                    /*  Try to fix up the SWAB function for complex data. */
                    if (td.td_sampleformat == SampleFormat.COMPLEXINT &&
                        td.td_bitspersample == 32 && tif.m_postDecodeMethod == Tiff.PostDecodeMethodType.pdmSwab32Bit)
                    {
                        tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab16Bit;
                    }
                    else if ((td.td_sampleformat == SampleFormat.COMPLEXINT ||
                        td.td_sampleformat == SampleFormat.COMPLEXIEEEFP) &&
                        td.td_bitspersample == 64 && tif.m_postDecodeMethod == Tiff.PostDecodeMethodType.pdmSwab64Bit)
                    {
                        tif.m_postDecodeMethod = Tiff.PostDecodeMethodType.pdmSwab32Bit;
                    }
                    break;
                case TiffTag.IMAGEDEPTH:
                    td.td_imagedepth = ap[0].ToInt();
                    break;
                case TiffTag.SUBIFD:
                    if ((tif.m_flags & TiffFlags.INSUBIFD) != TiffFlags.INSUBIFD)
                    {
                        td.td_nsubifd = ap[0].ToShort();
                        Tiff.setLongArray(out td.td_subifd, ap[1].ToIntArray(), td.td_nsubifd);
                    }
                    else
                    {
                        Tiff.ErrorExt(tif, tif.m_clientdata, module,
                            "{0}: Sorry, cannot nest SubIFDs", tif.m_name);
                        status = false;
                    }
                    break;
                case TiffTag.YCBCRPOSITIONING:
                    td.td_ycbcrpositioning = (YCbCrPosition)ap[0].ToByte();
                    break;
                case TiffTag.YCBCRSUBSAMPLING:
                    td.td_ycbcrsubsampling[0] = ap[0].ToShort();
                    td.td_ycbcrsubsampling[1] = ap[1].ToShort();
                    break;
                case TiffTag.TRANSFERFUNCTION:
                    v = ((td.td_samplesperpixel - td.td_extrasamples) > 1 ? 3 : 1);
                    for (int i = 0; i < v; i++)
                    {
                        Tiff.setShortArray(out td.td_transferfunction[i], ap[0].ToShortArray(), 1 << td.td_bitspersample);
                    }
                    break;
                case TiffTag.INKNAMES:
                    v = ap[0].ToInt();
                    string s = ap[1].ToString();
                    v = checkInkNamesString(tif, v, s);
                    status = v > 0;
                    if (v > 0)
                    {
                        setNString(out td.td_inknames, s, v);
                        td.td_inknameslen = v;
                    }
                    break;
                default:
                    // This can happen if multiple images are open with
                    // different codecs which have private tags. The global tag
                    // information table may then have tags that are valid for
                    // one file but not the other. If the client tries to set a
                    // tag that is not valid for the image's codec then we'll
                    // arrive here. This happens, for example, when tiffcp is
                    // used to convert between compression schemes and
                    // codec-specific tags are blindly copied.
                    TiffFieldInfo fip = tif.FindFieldInfo(tag, TiffType.ANY);
                    if (fip == null || fip.Bit != FieldBit.Custom)
                    {
                        Tiff.ErrorExt(tif, tif.m_clientdata, module,
                            "{0}: Invalid {1}tag \"{2}\" (not supported by codec)",
                            tif.m_name, Tiff.isPseudoTag(tag) ? "pseudo-" : "",
                            fip != null ? fip.Name : "Unknown");
                        status = false;
                        break;
                    }

                    // Find the existing entry for this custom value.
                    int tvIndex = -1;
                    for (int iCustom = 0; iCustom < td.td_customValueCount; iCustom++)
                    {
                        if (td.td_customValues[iCustom].info.Tag == tag)
                        {
                            td.td_customValues[iCustom].value = null;
                            break;
                        }
                    }

                    // Grow the custom list if the entry was not found.
                    if (tvIndex == -1)
                    {
                        td.td_customValueCount++;
                        TiffTagValue[] new_customValues = Tiff.Realloc(td.td_customValues, td.td_customValueCount - 1, td.td_customValueCount);
                        td.td_customValues = new_customValues;

                        tvIndex = td.td_customValueCount - 1;
                        td.td_customValues[tvIndex].info = fip;
                        td.td_customValues[tvIndex].value = null;
                        td.td_customValues[tvIndex].count = 0;
                    }

                    // Set custom value ... save a copy of the custom tag value.
                    int tv_size = Tiff.dataSize(fip.Type);
                    if (tv_size == 0)
                    {
                        status = false;
                        Tiff.ErrorExt(tif, tif.m_clientdata, module,
                            "{0}: Bad field type {1} for \"{2}\"",
                            tif.m_name, fip.Type, fip.Name);
                        end = true;
                        break;
                    }

                    int paramIndex = 0;
                    if (fip.PassCount)
                    {
                        if (fip.WriteCount == TiffFieldInfo.Variable2)
                            td.td_customValues[tvIndex].count = ap[paramIndex++].ToInt();
                        else
                            td.td_customValues[tvIndex].count = ap[paramIndex++].ToInt();
                    }
                    else if (fip.WriteCount == TiffFieldInfo.Variable ||
                        fip.WriteCount == TiffFieldInfo.Variable2)
                    {
                        td.td_customValues[tvIndex].count = 1;
                    }
                    else if (fip.WriteCount == TiffFieldInfo.Spp)
                    {
                        td.td_customValues[tvIndex].count = td.td_samplesperpixel;
                    }
                    else
                    {
                        td.td_customValues[tvIndex].count = fip.WriteCount;
                    }

                    if (fip.Type == TiffType.ASCII)
                    {
                        string ascii;
                        Tiff.setString(out ascii, ap[paramIndex++].ToString());
                        td.td_customValues[tvIndex].value = Tiff.Latin1Encoding.GetBytes(ascii);
                    }
                    else
                    {
                        td.td_customValues[tvIndex].value = new byte[tv_size * td.td_customValues[tvIndex].count];
                        if ((fip.PassCount ||
                            fip.WriteCount == TiffFieldInfo.Variable ||
                            fip.WriteCount == TiffFieldInfo.Variable2 ||
                            fip.WriteCount == TiffFieldInfo.Spp ||
                            td.td_customValues[tvIndex].count > 1) &&
                            fip.Tag != TiffTag.PAGENUMBER &&
                            fip.Tag != TiffTag.HALFTONEHINTS &&
                            fip.Tag != TiffTag.YCBCRSUBSAMPLING &&
                            fip.Tag != TiffTag.DOTRANGE)
                        {
                            byte[] apBytes = ap[paramIndex++].GetBytes();
                            Array.Copy(apBytes, td.td_customValues[tvIndex].value, apBytes.Length);
                        }
                        else
                        {
                            // XXX: The following loop required to handle
                            // PAGENUMBER, HALFTONEHINTS,
                            // YCBCRSUBSAMPLING and DOTRANGE tags.
                            // These tags are actually arrays and should be
                            // passed as arrays to SetField() function, but
                            // actually passed as a list of separate values.
                            // This behavior must be changed in the future!
                            byte[] val = td.td_customValues[tvIndex].value;
                            int valPos = 0;
                            for (int i = 0; i < td.td_customValues[tvIndex].count; i++, valPos += tv_size)
                            {
                                switch (fip.Type)
                                {
                                    case TiffType.BYTE:
                                    case TiffType.UNDEFINED:
                                        val[valPos] = ap[paramIndex + i].ToByte();
                                        break;
                                    case TiffType.SBYTE:
                                        val[valPos] = ap[paramIndex + i].ToByte();
                                        break;
                                    case TiffType.SHORT:
                                        Array.Copy(BitConverter.GetBytes(ap[paramIndex + i].ToShort()), 0, val, valPos, tv_size);
                                        break;
                                    case TiffType.SSHORT:
                                        Array.Copy(BitConverter.GetBytes(ap[paramIndex + i].ToShort()), 0, val, valPos, tv_size);
                                        break;
                                    case TiffType.LONG:
                                    case TiffType.IFD:
                                        Array.Copy(BitConverter.GetBytes(ap[paramIndex + i].ToInt()), 0, val, valPos, tv_size);
                                        break;
                                    case TiffType.SLONG:
                                        Array.Copy(BitConverter.GetBytes(ap[paramIndex + i].ToInt()), 0, val, valPos, tv_size);
                                        break;
                                    case TiffType.RATIONAL:
                                    case TiffType.SRATIONAL:
                                    case TiffType.FLOAT:
                                        Array.Copy(BitConverter.GetBytes(ap[paramIndex + i].ToFloat()), 0, val, valPos, tv_size);
                                        break;
                                    case TiffType.DOUBLE:
                                        Array.Copy(BitConverter.GetBytes(ap[paramIndex + i].ToDouble()), 0, val, valPos, tv_size);
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

            if (!end && !badvalue && !badvalue32)
            {
                if (status)
                {
                    tif.setFieldBit(tif.FieldWithTag(tag).Bit);
                    tif.m_flags |= TiffFlags.DIRTYDIRECT;
                }
            }

            if (badvalue)
            {
                Tiff.ErrorExt(tif, tif.m_clientdata, module,
                    "{0}: Bad value {1} for \"{2}\" tag",
                    tif.m_name, v, tif.FieldWithTag(tag).Name);
                return false;
            }

            if (badvalue32)
            {
                Tiff.ErrorExt(tif, tif.m_clientdata, module,
                    "{0}: Bad value {1} for \"{2}\" tag",
                    tif.m_name, v32, tif.FieldWithTag(tag).Name);
                return false;
            }

            return status;
        }

        /// <summary>
        /// Gets the field value by specified tag.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>The field value.</returns>
        public virtual FieldValue[] GetField(Tiff tif, TiffTag tag)
        {
            TiffDirectory td = tif.m_dir;
            FieldValue[] result = null;

            switch (tag)
            {
                case TiffTag.SUBFILETYPE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_subfiletype);
                    break;
                case TiffTag.IMAGEWIDTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_imagewidth);
                    break;
                case TiffTag.IMAGELENGTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_imagelength);
                    break;
                case TiffTag.BITSPERSAMPLE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_bitspersample);
                    break;
                case TiffTag.COMPRESSION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_compression);
                    break;
                case TiffTag.PHOTOMETRIC:
                    result = new FieldValue[1];
                    result[0].Set(td.td_photometric);
                    break;
                case TiffTag.THRESHHOLDING:
                    result = new FieldValue[1];
                    result[0].Set(td.td_threshholding);
                    break;
                case TiffTag.FILLORDER:
                    result = new FieldValue[1];
                    result[0].Set(td.td_fillorder);
                    break;
                case TiffTag.ORIENTATION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_orientation);
                    break;
                case TiffTag.SAMPLESPERPIXEL:
                    result = new FieldValue[1];
                    result[0].Set(td.td_samplesperpixel);
                    break;
                case TiffTag.ROWSPERSTRIP:
                    result = new FieldValue[1];
                    result[0].Set(td.td_rowsperstrip);
                    break;
                case TiffTag.MINSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_minsamplevalue);
                    break;
                case TiffTag.MAXSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_maxsamplevalue);
                    break;
                case TiffTag.SMINSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_sminsamplevalue);
                    break;
                case TiffTag.SMAXSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_smaxsamplevalue);
                    break;
                case TiffTag.XRESOLUTION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_xresolution);
                    break;
                case TiffTag.YRESOLUTION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_yresolution);
                    break;
                case TiffTag.PLANARCONFIG:
                    result = new FieldValue[1];
                    result[0].Set(td.td_planarconfig);
                    break;
                case TiffTag.XPOSITION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_xposition);
                    break;
                case TiffTag.YPOSITION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_yposition);
                    break;
                case TiffTag.RESOLUTIONUNIT:
                    result = new FieldValue[1];
                    result[0].Set(td.td_resolutionunit);
                    break;
                case TiffTag.PAGENUMBER:
                    result = new FieldValue[2];
                    result[0].Set(td.td_pagenumber[0]);
                    result[1].Set(td.td_pagenumber[1]);
                    break;
                case TiffTag.HALFTONEHINTS:
                    result = new FieldValue[2];
                    result[0].Set(td.td_halftonehints[0]);
                    result[1].Set(td.td_halftonehints[1]);
                    break;
                case TiffTag.COLORMAP:
                    result = new FieldValue[3];
                    result[0].Set(td.td_colormap[0]);
                    result[1].Set(td.td_colormap[1]);
                    result[2].Set(td.td_colormap[2]);
                    break;
                case TiffTag.STRIPOFFSETS:
                case TiffTag.TILEOFFSETS:
                    result = new FieldValue[1];
                    result[0].Set(td.td_stripoffset);
                    break;
                case TiffTag.STRIPBYTECOUNTS:
                case TiffTag.TILEBYTECOUNTS:
                    result = new FieldValue[1];
                    result[0].Set(td.td_stripbytecount);
                    break;
                case TiffTag.MATTEING:
                    result = new FieldValue[1];
                    result[0].Set((td.td_extrasamples == 1 && td.td_sampleinfo[0] == ExtraSample.ASSOCALPHA));
                    break;
                case TiffTag.EXTRASAMPLES:
                    result = new FieldValue[2];
                    result[0].Set(td.td_extrasamples);
                    result[1].Set(td.td_sampleinfo);
                    break;
                case TiffTag.TILEWIDTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_tilewidth);
                    break;
                case TiffTag.TILELENGTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_tilelength);
                    break;
                case TiffTag.TILEDEPTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_tiledepth);
                    break;
                case TiffTag.DATATYPE:
                    switch (td.td_sampleformat)
                    {
                        case SampleFormat.UINT:
                            result = new FieldValue[1];
                            result[0].Set(DATATYPE_UINT);
                            break;
                        case SampleFormat.INT:
                            result = new FieldValue[1];
                            result[0].Set(DATATYPE_INT);
                            break;
                        case SampleFormat.IEEEFP:
                            result = new FieldValue[1];
                            result[0].Set(DATATYPE_IEEEFP);
                            break;
                        case SampleFormat.VOID:
                            result = new FieldValue[1];
                            result[0].Set(DATATYPE_VOID);
                            break;
                    }
                    break;
                case TiffTag.SAMPLEFORMAT:
                    result = new FieldValue[1];
                    result[0].Set(td.td_sampleformat);
                    break;
                case TiffTag.IMAGEDEPTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_imagedepth);
                    break;
                case TiffTag.SUBIFD:
                    result = new FieldValue[2];
                    result[0].Set(td.td_nsubifd);
                    result[1].Set(td.td_subifd);
                    break;
                case TiffTag.YCBCRPOSITIONING:
                    result = new FieldValue[1];
                    result[0].Set(td.td_ycbcrpositioning);
                    break;
                case TiffTag.YCBCRSUBSAMPLING:
                    result = new FieldValue[2];
                    result[0].Set(td.td_ycbcrsubsampling[0]);
                    result[1].Set(td.td_ycbcrsubsampling[1]);
                    break;
                case TiffTag.TRANSFERFUNCTION:
                    result = new FieldValue[3];
                    result[0].Set(td.td_transferfunction[0]);
                    if (td.td_samplesperpixel - td.td_extrasamples > 1)
                    {
                        result[1].Set(td.td_transferfunction[1]);
                        result[2].Set(td.td_transferfunction[2]);
                    }
                    break;
                case TiffTag.INKNAMES:
                    result = new FieldValue[1];
                    result[0].Set(td.td_inknames);
                    break;
                default:
                    // This can happen if multiple images are open with 
                    // different codecs which have private tags. The global tag
                    // information table may then have tags that are valid for
                    // one file but not the other. If the client tries to get a
                    // tag that is not valid for the image's codec then we'll
                    // arrive here.
                    TiffFieldInfo fip = tif.FindFieldInfo(tag, TiffType.ANY);
                    if (fip == null || fip.Bit != FieldBit.Custom)
                    {
                        Tiff.ErrorExt(tif, tif.m_clientdata, "_TIFFVGetField",
                            "{0}: Invalid {1}tag \"{2}\" (not supported by codec)",
                            tif.m_name, Tiff.isPseudoTag(tag) ? "pseudo-" : "",
                            fip != null ? fip.Name : "Unknown");
                        result = null;
                        break;
                    }

                    // Do we have a custom value?
                    result = null;
                    for (int i = 0; i < td.td_customValueCount; i++)
                    {
                        TiffTagValue tv = td.td_customValues[i];
                        if (tv.info.Tag != tag)
                            continue;

                        if (fip.PassCount)
                        {
                            result = new FieldValue[2];

                            if (fip.ReadCount == TiffFieldInfo.Variable2)
                            {
                                result[0].Set(tv.count);
                            }
                            else
                            {
                                // Assume TiffFieldInfo.Variable
                                result[0].Set(tv.count);
                            }
                            
                            result[1].Set(tv.value);
                        }
                        else
                        {
                            if ((fip.Type == TiffType.ASCII ||
                                fip.ReadCount == TiffFieldInfo.Variable ||
                                fip.ReadCount == TiffFieldInfo.Variable2 ||
                                fip.ReadCount == TiffFieldInfo.Spp || 
                                tv.count > 1) && fip.Tag != TiffTag.PAGENUMBER && 
                                fip.Tag != TiffTag.HALFTONEHINTS && 
                                fip.Tag != TiffTag.YCBCRSUBSAMPLING && 
                                fip.Tag != TiffTag.DOTRANGE)
                            {
                                result = new FieldValue[1];
                                byte[] value = tv.value;

                                if (fip.Type == TiffType.ASCII &&
                                    tv.value.Length > 0 &&
                                    tv.value[tv.value.Length - 1] == 0)
                                {
                                    // cut unwanted zero at the end
                                    value = new byte[Math.Max(tv.value.Length - 1, 0)];
                                    Array.Copy(tv.value, value, value.Length);
                                }

                                result[0].Set(value);
                            }
                            else
                            {
                                result = new FieldValue[tv.count];
                                byte[] val = tv.value;
                                int valPos = 0;
                                for (int j = 0; j < tv.count; j++, valPos += Tiff.dataSize(tv.info.Type))
                                {
                                    switch (fip.Type)
                                    {
                                        case TiffType.BYTE:
                                        case TiffType.UNDEFINED:
                                        case TiffType.SBYTE:
                                            result[j].Set(val[valPos]);
                                            break;
                                        case TiffType.SHORT:
                                        case TiffType.SSHORT:
                                            result[j].Set(BitConverter.ToInt16(val, valPos));
                                            break;
                                        case TiffType.LONG:
                                        case TiffType.IFD:
                                        case TiffType.SLONG:
                                            result[j].Set(BitConverter.ToInt32(val, valPos));
                                            break;
                                        case TiffType.RATIONAL:
                                        case TiffType.SRATIONAL:
                                        case TiffType.FLOAT:
                                            result[j].Set(BitConverter.ToSingle(val, valPos));
                                            break;
                                        case TiffType.DOUBLE:
                                            result[j].Set(BitConverter.ToDouble(val, valPos));
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
                    break;
            }

            return result;
        }

        /// <summary>
        /// Prints the directory.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="fd">The fd.</param>
        /// <param name="flags">The flags.</param>
        public virtual void PrintDir(Tiff tif, Stream fd, TiffPrintFlags flags)
        {
        }

        /*
        * Install extra samples information.
        */
        private static bool setExtraSamples(TiffDirectory td, ref int v, FieldValue[] ap)
        {
            // XXX: Unassociated alpha data == 999 is a known Corel Draw bug, see below
            const short EXTRASAMPLE_COREL_UNASSALPHA = 999;

            v = ap[0].ToInt();
            if (v > td.td_samplesperpixel)
                return false;

            byte[] va = ap[1].ToByteArray();
            if (v > 0 && va == null)
            {
                // typically missing param
                return false;
            }

            for (int i = 0; i < v; i++)
            {
                if ((ExtraSample)va[i] > ExtraSample.UNASSALPHA)
                {
                    // XXX: Corel Draw is known to produce incorrect 
                    // ExtraSamples tags which must be patched here if we
                    // want to be able to open some of the damaged TIFF files: 
                    if (i < v - 1)
                    {
                        short s = BitConverter.ToInt16(va, i);
                        if (s == EXTRASAMPLE_COREL_UNASSALPHA)
                            va[i] = (byte)ExtraSample.UNASSALPHA;
                    }
                    else
                        return false;
                }
            }

            td.td_extrasamples = (short)v;
            td.td_sampleinfo = new ExtraSample[td.td_extrasamples];
            for (int i = 0; i < td.td_extrasamples; i++)
                td.td_sampleinfo[i] = (ExtraSample)va[i];

            return true;
        }

        private static int checkInkNamesString(Tiff tif, int slen, string s)
        {
            bool failed = false;
            short i = tif.m_dir.td_samplesperpixel;

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

                    pos++; // skip \0
                }

                if (!failed)
                    return pos;
            }

            Tiff.ErrorExt(tif, tif.m_clientdata, "TIFFSetField",
                "{0}: Invalid InkNames value; expecting {1} names, found {2}",
                tif.m_name, tif.m_dir.td_samplesperpixel, tif.m_dir.td_samplesperpixel - i);
            return 0;
        }

        private static void setNString(out string cpp, string cp, int n)
        {
            cpp = cp.Substring(0, n);
        }
    }
}
