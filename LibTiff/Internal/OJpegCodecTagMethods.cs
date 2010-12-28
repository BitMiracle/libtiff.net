/* Copyright (C) 2008-2010, Bit Miracle
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

namespace BitMiracle.LibTiff.Classic.Internal
{
    class OJpegCodecTagMethods : TiffTagMethods
    {
        //    tif.tif_tagmethods.vgetfield=OJPEGVGetField;
        //    tif.tif_tagmethods.vsetfield=OJPEGVSetField;
        //    tif.tif_tagmethods.printdir=OJPEGPrintDir;

        //static int
        //OJPEGVGetField(TIFF* tif, ttag_t tag, va_list ap)
        //{
        //    OJPEGState* sp=(OJPEGState*)tif.tif_data;
        //    switch(tag)
        //    {
        //        case TIFFTAG_JPEGIFOFFSET:
        //            *va_arg(ap,uint32*)=(uint32)sp.jpeg_interchange_format;
        //            break;
        //        case TIFFTAG_JPEGIFBYTECOUNT:
        //            *va_arg(ap,uint32*)=(uint32)sp.jpeg_interchange_format_length;
        //            break;
        //        case TIFFTAG_YCBCRSUBSAMPLING:
        //            if (sp.subsamplingcorrect_done==0)
        //                OJPEGSubsamplingCorrect(tif);
        //            *va_arg(ap,uint16*)=(uint16)sp.subsampling_hor;
        //            *va_arg(ap,uint16*)=(uint16)sp.subsampling_ver;
        //            break;
        //        case TIFFTAG_JPEGQTABLES:
        //            *va_arg(ap,uint32*)=(uint32)sp.qtable_offset_count;
        //            *va_arg(ap,void**)=(void*)sp.qtable_offset;
        //            break;
        //        case TIFFTAG_JPEGDCTABLES:
        //            *va_arg(ap,uint32*)=(uint32)sp.dctable_offset_count;
        //            *va_arg(ap,void**)=(void*)sp.dctable_offset;
        //            break;
        //        case TIFFTAG_JPEGACTABLES:
        //            *va_arg(ap,uint32*)=(uint32)sp.actable_offset_count;
        //            *va_arg(ap,void**)=(void*)sp.actable_offset;
        //            break;
        //        case TIFFTAG_JPEGPROC:
        //            *va_arg(ap,uint16*)=(uint16)sp.jpeg_proc;
        //            break;
        //        case TIFFTAG_JPEGRESTARTINTERVAL:
        //            *va_arg(ap,uint16*)=sp.restart_interval;
        //            break;
        //        default:
        //            return (*sp.vgetparent)(tif,tag,ap);
        //    }
        //    return (1);
        //}

        //static int
        //OJPEGVSetField(TIFF* tif, ttag_t tag, va_list ap)
        //{
        //    static const char module[]="OJPEGVSetField";
        //    OJPEGState* sp=(OJPEGState*)tif.tif_data;
        //    uint32 ma;
        //    uint32* mb;
        //    uint32 n;
        //    switch(tag)
        //    {
        //        case TIFFTAG_JPEGIFOFFSET:
        //            sp.jpeg_interchange_format=(toff_t)va_arg(ap,uint32);  
        //            break;
        //        case TIFFTAG_JPEGIFBYTECOUNT:
        //            sp.jpeg_interchange_format_length=(toff_t)va_arg(ap,uint32);  
        //            break;
        //        case TIFFTAG_YCBCRSUBSAMPLING:
        //            sp.subsampling_tag=1;
        //            sp.subsampling_hor=(byte)va_arg(ap,int);
        //            sp.subsampling_ver=(byte)va_arg(ap,int);
        //            tif.tif_dir.td_ycbcrsubsampling[0]=sp.subsampling_hor;
        //            tif.tif_dir.td_ycbcrsubsampling[1]=sp.subsampling_ver;
        //            break;
        //        case TIFFTAG_JPEGQTABLES:
        //            ma=va_arg(ap,uint32);
        //            if (ma!=0)
        //            {
        //                if (ma>3)
        //                {
        //                    TIFFErrorExt(tif.tif_clientdata,module,"JpegQTables tag has incorrect count");
        //                    return(0);
        //                }
        //                sp.qtable_offset_count=(byte)ma;
        //                mb=va_arg(ap,uint32*);
        //                for (n=0; n<ma; n++)
        //                    sp.qtable_offset[n]=(toff_t)mb[n];
        //            }
        //            break;
        //        case TIFFTAG_JPEGDCTABLES:
        //            ma=va_arg(ap,uint32);
        //            if (ma!=0)
        //            {
        //                if (ma>3)
        //                {
        //                    TIFFErrorExt(tif.tif_clientdata,module,"JpegDcTables tag has incorrect count");
        //                    return(0);
        //                }
        //                sp.dctable_offset_count=(byte)ma;
        //                mb=va_arg(ap,uint32*);
        //                for (n=0; n<ma; n++)
        //                    sp.dctable_offset[n]=(toff_t)mb[n];
        //            }
        //            break;
        //        case TIFFTAG_JPEGACTABLES:
        //            ma=va_arg(ap,uint32);
        //            if (ma!=0)
        //            {
        //                if (ma>3)
        //                {
        //                    TIFFErrorExt(tif.tif_clientdata,module,"JpegAcTables tag has incorrect count");
        //                    return(0);
        //                }
        //                sp.actable_offset_count=(byte)ma;
        //                mb=va_arg(ap,uint32*);
        //                for (n=0; n<ma; n++)
        //                    sp.actable_offset[n]=(toff_t)mb[n];
        //            }
        //            break;
        //        case TIFFTAG_JPEGPROC:
        //            sp.jpeg_proc=(byte)va_arg(ap,uint32);
        //            break;
        //        case TIFFTAG_JPEGRESTARTINTERVAL:
        //            sp.restart_interval=(uint16)va_arg(ap,uint32);
        //            break;
        //        default:
        //            return (*sp.vsetparent)(tif,tag,ap);
        //    }
        //    TIFFSetFieldBit(tif,_TIFFFieldWithTag(tif,tag).field_bit);
        //    tif.tif_flags|=TIFF_DIRTYDIRECT;
        //    return(1);
        //}

        //static void
        //OJPEGPrintDir(TIFF* tif, FILE* fd, long flags)
        //{
        //    OJPEGState* sp=(OJPEGState*)tif.tif_data;
        //    byte m;
        //    (void)flags;
        //    assert(sp!=NULL);
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGINTERCHANGEFORMAT))
        //        fprintf(fd,"  JpegInterchangeFormat: %lu\n",(unsigned long)sp.jpeg_interchange_format);
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGINTERCHANGEFORMATLENGTH))
        //        fprintf(fd,"  JpegInterchangeFormatLength: %lu\n",(unsigned long)sp.jpeg_interchange_format_length);
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGQTABLES))
        //    {
        //        fprintf(fd,"  JpegQTables:");
        //        for (m=0; m<sp.qtable_offset_count; m++)
        //            fprintf(fd," %lu",(unsigned long)sp.qtable_offset[m]);
        //        fprintf(fd,"\n");
        //    }
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGDCTABLES))
        //    {
        //        fprintf(fd,"  JpegDcTables:");
        //        for (m=0; m<sp.dctable_offset_count; m++)
        //            fprintf(fd," %lu",(unsigned long)sp.dctable_offset[m]);
        //        fprintf(fd,"\n");
        //    }
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGACTABLES))
        //    {
        //        fprintf(fd,"  JpegAcTables:");
        //        for (m=0; m<sp.actable_offset_count; m++)
        //            fprintf(fd," %lu",(unsigned long)sp.actable_offset[m]);
        //        fprintf(fd,"\n");
        //    }
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGPROC))
        //        fprintf(fd,"  JpegProc: %u\n",(unsigned int)sp.jpeg_proc);
        //    if (TIFFFieldSet(tif,FIELD_OJPEG_JPEGRESTARTINTERVAL))
        //        fprintf(fd,"  JpegRestartInterval: %u\n",(unsigned int)sp.restart_interval);
        //}
    }
}
