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

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Classic.Internal
{
    class OJPEGState
    {
        public const int OJPEG_BUFFER = 2048;

        public struct SosEnd
        {
            public byte m_log;
            public OJPEGStateInBufferSource m_in_buffer_source;
            public uint m_in_buffer_next_strile;
            public uint m_in_buffer_file_pos;
            public uint m_in_buffer_file_togo;
        }

        public Tiff m_tif;
        public uint m_file_size;
        public uint m_image_width;
        public uint m_image_length;
        public uint m_strile_width;
        public uint m_strile_length;
        public uint m_strile_length_total;
        public byte m_samples_per_pixel;
        public byte m_plane_sample_offset;
        public byte m_samples_per_pixel_per_plane;
        public uint m_jpeg_interchange_format;
        public uint m_jpeg_interchange_format_length;
        public byte m_jpeg_proc;
        public byte m_subsamplingcorrect;
        public byte m_subsamplingcorrect_done;
        public byte m_subsampling_tag;
        public byte m_subsampling_hor;
        public byte m_subsampling_ver;
        public byte m_subsampling_force_desubsampling_inside_decompression;
        public byte m_qtable_offset_count;
        public byte m_dctable_offset_count;
        public byte m_actable_offset_count;
        public uint[] m_qtable_offset = new uint[3];
        public uint[] m_dctable_offset = new uint[3];
        public uint[] m_actable_offset = new uint[3];
        public byte[][] m_qtable = new byte[4][];
        public byte[][] m_dctable = new byte[4][];
        public byte[][] m_actable = new byte[4][];
        public ushort m_restart_interval;
        public byte m_restart_index;
        public byte m_sof_log;
        public byte m_sof_marker_id;
        public uint m_sof_x;
        public uint m_sof_y;
        public byte[] m_sof_c = new byte[3];
        public byte[] m_sof_hv = new byte[3];
        public byte[] m_sof_tq = new byte[3];
        public byte[] m_sos_cs = new byte[3];
        public byte[] m_sos_tda = new byte[3];
        public SosEnd[] m_sos_end = new SosEnd[3];
        public byte m_readheader_done;
        public byte m_writeheader_done;
        public short m_write_cursample;
        public uint m_write_curstrile;
        public byte m_libjpeg_session_active;
        public byte m_libjpeg_jpeg_query_style;
        public jpeg_error_mgr m_libjpeg_jpeg_error_mgr;
        public jpeg_decompress_struct m_libjpeg_jpeg_decompress_struct;
        public jpeg_source_mgr m_libjpeg_jpeg_source_mgr;
        public byte m_subsampling_convert_log;
        public uint m_subsampling_convert_ylinelen;
        public uint m_subsampling_convert_ylines;
        public uint m_subsampling_convert_clinelen;
        public uint m_subsampling_convert_clines;
        public byte[][] m_subsampling_convert_ybuf;
        public byte[][] m_subsampling_convert_cbbuf;
        public byte[][] m_subsampling_convert_crbuf;
        public byte[][][] m_subsampling_convert_ycbcrimage;
        public uint m_subsampling_convert_clinelenout;
        public uint m_subsampling_convert_state;
        public uint m_bytes_per_line;   /* if the codec outputs subsampled data, a 'line' in bytes_per_line */
        public uint m_lines_per_strile; /* and lines_per_strile means subsampling_ver desubsampled rows     */
        public OJPEGStateInBufferSource m_in_buffer_source;
        public uint m_in_buffer_next_strile;
        public uint m_in_buffer_strile_count;
        public uint m_in_buffer_file_pos;
        public byte m_in_buffer_file_pos_log;
        public uint m_in_buffer_file_togo;
        public ushort m_in_buffer_togo;
        public int m_in_buffer_cur; // index into m_in_buffer
        public byte[] m_in_buffer = new byte[OJPEG_BUFFER];
        public OJPEGStateOutState m_out_state;
        public byte[] m_out_buffer = new byte[OJPEG_BUFFER];
        public byte[] m_skip_buffer;
    }
}
