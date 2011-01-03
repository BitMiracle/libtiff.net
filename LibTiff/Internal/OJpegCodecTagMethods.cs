/* Copyright (C) 2008-2011, Bit Miracle
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
using System.Diagnostics;

namespace BitMiracle.LibTiff.Classic.Internal
{
    class OJpegCodecTagMethods : TiffTagMethods
    {
        public override bool SetField(Tiff tif, TiffTag tag, FieldValue[] ap)
        {
            const string module = "OJPEGVSetField";
            OJpegCodec codec = tif.m_currentCodec as OJpegCodec;
            Debug.Assert(codec != null);
            OJPEGState sp = codec.sp;

            uint ma;
            uint[] mb;
            uint n;
            switch (tag)
            {
                case TiffTag.JPEGIFOFFSET:
                    sp.jpeg_interchange_format = ap[0].ToUInt();
                    break;
                case TiffTag.JPEGIFBYTECOUNT:
                    sp.jpeg_interchange_format_length = ap[0].ToUInt();
                    break;
                case TiffTag.YCBCRSUBSAMPLING:
                    sp.subsampling_tag = 1;
                    sp.subsampling_hor = ap[0].ToByte();
                    sp.subsampling_ver = ap[1].ToByte();
                    tif.m_dir.td_ycbcrsubsampling[0] = sp.subsampling_hor;
                    tif.m_dir.td_ycbcrsubsampling[1] = sp.subsampling_ver;
                    break;
                case TiffTag.JPEGQTABLES:
                    ma = ap[0].ToUInt();
                    if (ma != 0)
                    {
                        if (ma > 3)
                        {
                            Tiff.ErrorExt(tif, tif.m_clientdata, module, "JpegQTables tag has incorrect count");
                            return false;
                        }
                        sp.qtable_offset_count = (byte)ma;
                        mb = ap[1].ToUIntArray();
                        for (n = 0; n < ma; n++)
                            sp.qtable_offset[n] = mb[n];
                    }
                    break;
                case TiffTag.JPEGDCTABLES:
                    ma = ap[0].ToUInt();
                    if (ma != 0)
                    {
                        if (ma > 3)
                        {
                            Tiff.ErrorExt(tif, tif.m_clientdata, module, "JpegDcTables tag has incorrect count");
                            return false;
                        }
                        sp.dctable_offset_count = (byte)ma;
                        mb = ap[1].ToUIntArray();
                        for (n = 0; n < ma; n++)
                            sp.dctable_offset[n] = mb[n];
                    }
                    break;
                case TiffTag.JPEGACTABLES:
                    ma = ap[0].ToUInt();
                    if (ma != 0)
                    {
                        if (ma > 3)
                        {
                            Tiff.ErrorExt(tif, tif.m_clientdata, module, "JpegAcTables tag has incorrect count");
                            return false;
                        }
                        sp.actable_offset_count = (byte)ma;
                        mb = ap[1].ToUIntArray();
                        for (n = 0; n < ma; n++)
                            sp.actable_offset[n] = mb[n];
                    }
                    break;
                case TiffTag.JPEGPROC:
                    sp.jpeg_proc = ap[0].ToByte();
                    break;
                case TiffTag.JPEGRESTARTINTERVAL:
                    sp.restart_interval = ap[0].ToUShort();
                    break;
                default:
                    return base.SetField(tif, tag, ap);
            }

            TiffFieldInfo fip = tif.FieldWithTag(tag);
            if (fip != null)
                tif.setFieldBit(fip.Bit);
            else
                return false;

            tif.m_flags |= TiffFlags.DIRTYDIRECT;
            return true;
        }

        public override FieldValue[] GetField(Tiff tif, TiffTag tag)
        {
            OJpegCodec codec = tif.m_currentCodec as OJpegCodec;
            Debug.Assert(codec != null);
            OJPEGState sp = codec.sp;

            FieldValue[] result = null;

            switch (tag)
            {
                case TiffTag.JPEGIFOFFSET:
                    result = new FieldValue[1];
                    result[0].Set(sp.jpeg_interchange_format);
                    break;
                case TiffTag.JPEGIFBYTECOUNT:
                    result = new FieldValue[1];
                    result[0].Set(sp.jpeg_interchange_format_length);
                    break;
                case TiffTag.YCBCRSUBSAMPLING:
                    if (sp.subsamplingcorrect_done == 0)
                        codec.OJPEGSubsamplingCorrect();

                    result = new FieldValue[2];
                    result[0].Set(sp.subsampling_hor);
                    result[1].Set(sp.subsampling_ver);
                    break;
                case TiffTag.JPEGQTABLES:
                    result = new FieldValue[2];
                    result[0].Set(sp.qtable_offset_count);
                    result[1].Set(sp.qtable_offset);
                    break;
                case TiffTag.JPEGDCTABLES:
                    result = new FieldValue[2];
                    result[0].Set(sp.dctable_offset_count);
                    result[1].Set(sp.dctable_offset);
                    break;
                case TiffTag.JPEGACTABLES:
                    result = new FieldValue[2];
                    result[0].Set(sp.actable_offset_count);
                    result[1].Set(sp.actable_offset);
                    break;
                case TiffTag.JPEGPROC:
                    result = new FieldValue[1];
                    result[0].Set(sp.jpeg_proc);
                    break;
                case TiffTag.JPEGRESTARTINTERVAL:
                    result = new FieldValue[1];
                    result[0].Set(sp.restart_interval);
                    break;
                default:
                    return base.GetField(tif, tag);
            }

            return result;
        }

        public override void PrintDir(Tiff tif, Stream fd, TiffPrintFlags flags)
        {
            OJpegCodec codec = tif.m_currentCodec as OJpegCodec;
            Debug.Assert(codec != null);
            OJPEGState sp = codec.sp;

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGINTERCHANGEFORMAT))
                Tiff.fprintf(fd, "  JpegInterchangeFormat: {0}\n", sp.jpeg_interchange_format);

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGINTERCHANGEFORMATLENGTH))
                Tiff.fprintf(fd, "  JpegInterchangeFormatLength: {0}\n", sp.jpeg_interchange_format_length);

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGQTABLES))
            {
                Tiff.fprintf(fd, "  JpegQTables:");
                for (byte m = 0; m < sp.qtable_offset_count; m++)
                    Tiff.fprintf(fd, " {0}", sp.qtable_offset[m]);
                Tiff.fprintf(fd, "\n");
            }

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGDCTABLES))
            {
                Tiff.fprintf(fd, "  JpegDcTables:");
                for (byte m = 0; m < sp.dctable_offset_count; m++)
                    Tiff.fprintf(fd, " {0}", sp.dctable_offset[m]);
                Tiff.fprintf(fd, "\n");
            }

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGACTABLES))
            {
                Tiff.fprintf(fd, "  JpegAcTables:");
                for (byte m = 0; m < sp.actable_offset_count; m++)
                    Tiff.fprintf(fd, " {0}", sp.actable_offset[m]);
                Tiff.fprintf(fd, "\n");
            }

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGPROC))
                Tiff.fprintf(fd, "  JpegProc: {0}\n", sp.jpeg_proc);

            if (tif.fieldSet(OJpegCodec.FIELD_OJPEG_JPEGRESTARTINTERVAL))
                Tiff.fprintf(fd, "  JpegRestartInterval: {0}\n", sp.restart_interval);
        }
    }
}
