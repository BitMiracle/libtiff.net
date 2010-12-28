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
    class OJPEGState
    {
        public struct SosEnd
        {
            public byte log;
            public OJPEGStateInBufferSource in_buffer_source;
            public uint in_buffer_next_strile;
            public uint in_buffer_file_pos;
            public uint in_buffer_file_togo;
        }

        public Tiff tif;
        //#ifndef LIBJPEG_ENCAP_EXTERNAL
        //        JMP_BUF exit_jmpbuf;
        //#endif
        //TIFFVGetMethod vgetparent;
        //TIFFVSetMethod vsetparent;
        //uint file_size;
        //uint32 image_width;
        //uint32 image_length;
        //uint32 strile_width;
        //uint32 strile_length;
        //uint32 strile_length_total;
        //byte samples_per_pixel;
        public byte plane_sample_offset;
        //byte samples_per_pixel_per_plane;
        //uint jpeg_interchange_format;
        //uint jpeg_interchange_format_length;
        public byte jpeg_proc;
        //byte subsamplingcorrect;
        public byte subsamplingcorrect_done;
        //byte subsampling_tag;
        public byte subsampling_hor;
        public byte subsampling_ver;
        //byte subsampling_force_desubsampling_inside_decompression;
        //byte qtable_offset_count;
        //byte dctable_offset_count;
        //byte actable_offset_count;
        //uint qtable_offset[3];
        //uint dctable_offset[3];
        //uint actable_offset[3];
        //byte* qtable[4];
        //byte* dctable[4];
        //byte* actable[4];
        //ushort restart_interval;
        //byte restart_index;
        //byte sof_log;
        //byte sof_marker_id;
        //uint32 sof_x;
        //uint32 sof_y;
        //byte sof_c[3];
        //byte sof_hv[3];
        //byte sof_tq[3];
        //byte sos_cs[3];
        //byte sos_tda[3];
        public SosEnd[] sos_end = new SosEnd[3];
        public byte readheader_done;
        public byte writeheader_done;
        public short write_cursample;
        public uint write_curstrile;
        public byte libjpeg_session_active;
        public byte libjpeg_jpeg_query_style;
        //jpeg_error_mgr libjpeg_jpeg_error_mgr;
        //jpeg_decompress_struct libjpeg_jpeg_decompress_struct;
        //jpeg_source_mgr libjpeg_jpeg_source_mgr;
        //byte subsampling_convert_log;
        //uint32 subsampling_convert_ylinelen;
        //uint32 subsampling_convert_ylines;
        //uint32 subsampling_convert_clinelen;
        //uint32 subsampling_convert_clines;
        //uint32 subsampling_convert_ybuflen;
        //uint32 subsampling_convert_cbuflen;
        //uint32 subsampling_convert_ycbcrbuflen;
        //byte* subsampling_convert_ycbcrbuf;
        //byte* subsampling_convert_ybuf;
        //byte* subsampling_convert_cbbuf;
        //byte* subsampling_convert_crbuf;
        //uint32 subsampling_convert_ycbcrimagelen;
        //byte** subsampling_convert_ycbcrimage;
        //uint32 subsampling_convert_clinelenout;
        //uint32 subsampling_convert_state;
        //uint32 bytes_per_line;   /* if the codec outputs subsampled data, a 'line' in bytes_per_line */
        //uint32 lines_per_strile; /* and lines_per_strile means subsampling_ver desubsampled rows     */
        public OJPEGStateInBufferSource in_buffer_source;
        public uint in_buffer_next_strile;
        //uint in_buffer_strile_count;
        public uint in_buffer_file_pos;
        public byte in_buffer_file_pos_log;
        public uint in_buffer_file_togo;
        public ushort in_buffer_togo;
        public byte[] in_buffer_cur;
        //byte in_buffer[OJPEG_BUFFER];
        //OJPEGStateOutState out_state;
        //byte out_buffer[OJPEG_BUFFER];
        //byte* skip_buffer;
    }
}
