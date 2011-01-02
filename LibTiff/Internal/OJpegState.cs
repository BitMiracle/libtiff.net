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

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Classic.Internal
{
    class OJPEGState
    {
        public const int OJPEG_BUFFER = 2048;

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
        public uint file_size;
        public uint image_width;
        public uint image_length;
        public uint strile_width;
        public uint strile_length;
        public uint strile_length_total;
        public byte samples_per_pixel;
        public byte plane_sample_offset;
        public byte samples_per_pixel_per_plane;
        public uint jpeg_interchange_format;
        public uint jpeg_interchange_format_length;
        public byte jpeg_proc;
        public byte subsamplingcorrect;
        public byte subsamplingcorrect_done;
        public byte subsampling_tag;
        public byte subsampling_hor;
        public byte subsampling_ver;
        public byte subsampling_force_desubsampling_inside_decompression;
        //byte qtable_offset_count;
        //byte dctable_offset_count;
        //byte actable_offset_count;
        public uint[] qtable_offset = new uint[3];
        public uint[] dctable_offset = new uint[3];
        public uint[] actable_offset = new uint[3];
        public byte[][] qtable = new byte[4][];
        public byte[][] dctable = new byte[4][];
        public byte[][] actable = new byte[4][];
        public ushort restart_interval;
        public byte restart_index;
        public byte sof_log;
        public byte sof_marker_id;
        public uint sof_x;
        public uint sof_y;
        public byte[] sof_c = new byte[3];
        public byte[] sof_hv = new byte[3];
        public byte[] sof_tq = new byte[3];
        public byte[] sos_cs = new byte[3];
        public byte[] sos_tda = new byte[3];
        public SosEnd[] sos_end = new SosEnd[3];
        public byte readheader_done;
        public byte writeheader_done;
        public short write_cursample;
        public uint write_curstrile;
        public byte libjpeg_session_active;
        public byte libjpeg_jpeg_query_style;
        public jpeg_error_mgr libjpeg_jpeg_error_mgr;
        public jpeg_decompress_struct libjpeg_jpeg_decompress_struct;
        public jpeg_source_mgr libjpeg_jpeg_source_mgr;
        public byte subsampling_convert_log;
        public uint subsampling_convert_ylinelen;
        public uint subsampling_convert_ylines;
        public uint subsampling_convert_clinelen;
        public uint subsampling_convert_clines;
        //public uint subsampling_convert_ybuflen;
        //public uint subsampling_convert_cbuflen;
        //public uint subsampling_convert_ycbcrbuflen;
        //public byte[] subsampling_convert_ycbcrbuf;
        public byte[][] subsampling_convert_ybuf; // was byte[]
        public byte[][] subsampling_convert_cbbuf; // was byte[]
        public byte[][] subsampling_convert_crbuf; // was byte[]
        public uint subsampling_convert_ycbcrimagelen;
        public byte[][] subsampling_convert_ycbcrimage;
        public uint subsampling_convert_clinelenout;
        public uint subsampling_convert_state;
        public uint bytes_per_line;   /* if the codec outputs subsampled data, a 'line' in bytes_per_line */
        public uint lines_per_strile; /* and lines_per_strile means subsampling_ver desubsampled rows     */
        public OJPEGStateInBufferSource in_buffer_source;
        public uint in_buffer_next_strile;
        public uint in_buffer_strile_count;
        public uint in_buffer_file_pos;
        public byte in_buffer_file_pos_log;
        public uint in_buffer_file_togo;
        public ushort in_buffer_togo;
        public int in_buffer_cur; // was byte[], index into in_buffer
        public byte[] in_buffer = new byte[OJPEG_BUFFER];
        public OJPEGStateOutState out_state;
        public byte[] out_buffer = new byte[OJPEG_BUFFER];
        public byte[] skip_buffer;
    }
}
