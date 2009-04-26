/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

/*
 * This file contains application interface routines that are used for both
 * compression and decompression.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace LibJpeg.NET
{
    /// <summary>
    ///  Common fields between JPEG compression and decompression master structs.
    ///  Routines that are to be used by both halves of the library are declared
    ///  to receive an instance of this structure. There are no actual instances of 
    ///  jpeg_common_struct, only of jpeg_compress_struct and jpeg_decompress_struct.
    /// </summary>
    public class jpeg_common_struct
    {
        // Error handler module
        public jpeg_error_mgr m_err;

        // Progress monitor, or null if none
        public jpeg_progress_mgr m_progress;

        public bool m_is_decompressor;   /* So common code can tell which is which */

        //protected:
        public enum JpegState
        {
            DESTROYED = 0,
            CSTATE_START = 100,
            /* after create_compress */
            CSTATE_SCANNING = 101,
            /* start_compress done, write_scanlines OK */
            CSTATE_RAW_OK = 102,
            /* start_compress done, write_raw_data OK */
            CSTATE_WRCOEFS = 103,
            /* jpeg_write_coefficients done */
            DSTATE_START = 200,
            /* after create_decompress */
            DSTATE_INHEADER = 201,
            /* reading header markers, no SOS yet */
            DSTATE_READY = 202,
            /* found SOS, ready for start_decompress */
            DSTATE_PRELOAD = 203,
            /* reading multiscan file in start_decompress*/
            DSTATE_PRESCAN = 204,
            /* performing dummy pass for 2-pass quant */
            DSTATE_SCANNING = 205,
            /* start_decompress done, read_scanlines OK */
            DSTATE_RAW_OK = 206,
            /* start_decompress done, read_raw_data OK */
            DSTATE_BUFIMAGE = 207,
            /* expecting jpeg_start_output */
            DSTATE_BUFPOST = 208,
            /* looking for SOS/EOI in jpeg_finish_output */
            DSTATE_RDCOEFS = 209,
            /* reading file in jpeg_read_coefficients */
            DSTATE_STOPPING = 210 /* looking for EOI in jpeg_finish_decompress */
        }

        public JpegState m_global_state;     /* For checking call sequence validity */

        // Creation of 2-D sample arrays.
        public static byte[][] AllocJpegSamples(uint samplesPerRow, uint numberOfRows)
        {
            byte[][] result = new byte[numberOfRows][];
            for (int i = 0; i < (int)numberOfRows; i++)
                result[i] = new byte[samplesPerRow];

            return result;
        }

        public static string Version
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                string versionString = version.Major.ToString() + "." + version.Minor.ToString();

                versionString += "." + version.MajorRevision.ToString();
                versionString += "." + version.MinorRevision.ToString();

                return versionString;
            }
        }

        public static string Copyright
        {
            get
            {
                return "Copyright (C) 2008-2009, Bit Miracle";
            }
        }

        // Generic versions of jpeg_abort and jpeg_destroy that work on either
        // flavor of JPEG object.  These may be more convenient in some places.

        /// <summary>
        /// Abort processing of a JPEG compression or decompression operation,
        /// but don't destroy the object itself.
        /// 
        /// Closing a data source or destination, if necessary, is the 
        /// application's responsibility.
        /// </summary>
        public void jpeg_abort()
        {
            /* Reset overall state for possible reuse of object */
            if (m_is_decompressor)
            {
                m_global_state = JpegState.DSTATE_START;
                /* Try to keep application from accessing now-deleted marker list.
                 * A bit kludgy to do it here, but this is the most central place.
                 */
                //((jpeg_decompress_struct)this).m_marker_list = null;
            }
            else
            {
                m_global_state = JpegState.CSTATE_START;
            }
        }

        /// <summary>
        /// Destruction of a JPEG object. 
        /// 
        /// Closing a data source or destination, if necessary, is the 
        /// application's responsibility.
        /// </summary>
        public void jpeg_destroy()
        {
            // mark it destroyed
            m_global_state = JpegState.DESTROYED;
        }

        // Fatal errors (print message and exit)

        public void ERREXIT(int code)
        {
            m_err.m_msg_code = code;
            m_err.error_exit();
        }

        public void ERREXIT1(int code, int p1)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.error_exit();
        }

        public void ERREXIT2(int code, int p1, int p2)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.m_msg_parm.i[1] = p2;
            m_err.error_exit();
        }

        public void ERREXIT3(int code, int p1, int p2, int p3)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.m_msg_parm.i[1] = p2;
            m_err.m_msg_parm.i[2] = p3;
            m_err.error_exit();
        }

        public void ERREXIT4(int code, int p1, int p2, int p3, int p4)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.m_msg_parm.i[1] = p2;
            m_err.m_msg_parm.i[2] = p3;
            m_err.m_msg_parm.i[3] = p4;
            m_err.error_exit();
        }

        public void ERREXITS(int code, string str)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.s = str;
            m_err.error_exit();
        }

        // Nonfatal errors (we can keep going, but the data is probably corrupt)

        public void WARNMS(int code)
        {
            m_err.m_msg_code = code;
            m_err.emit_message(-1);
        }

        public void WARNMS1(int code, int p1)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.emit_message(-1);
        }

        public void WARNMS2(int code, int p1, int p2)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.m_msg_parm.i[1] = p2;
            m_err.emit_message(-1);
        }

        // Informational/debugging messages

        public void TRACEMS(int lvl, int code)
        {
            m_err.m_msg_code = code;
            m_err.emit_message(lvl);
        }

        public void TRACEMS1(int lvl, int code, int p1)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.emit_message(lvl);
        }

        public void TRACEMS2(int lvl, int code, int p1, int p2)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.i[0] = p1;
            m_err.m_msg_parm.i[1] = p2;
            m_err.emit_message(lvl);
        }

        public void TRACEMS3(int lvl, int code, int p1, int p2, int p3)
        {
            int[] _mp = m_err.m_msg_parm.i;
            _mp[0] = p1;
            _mp[1] = p2;
            _mp[2] = p3;

            m_err.m_msg_code = code;
            m_err.emit_message(lvl);
        }

        public void TRACEMS4(int lvl, int code, int p1, int p2, int p3, int p4)
        {
            int[] _mp = m_err.m_msg_parm.i;
            _mp[0] = p1;
            _mp[1] = p2;
            _mp[2] = p3;
            _mp[3] = p4;

            m_err.m_msg_code = code;
            m_err.emit_message(lvl);
        }

        public void TRACEMS5(int lvl, int code, int p1, int p2, int p3, int p4, int p5)
        {
            int[] _mp = m_err.m_msg_parm.i;
            _mp[0] = p1;
            _mp[1] = p2;
            _mp[2] = p3;
            _mp[3] = p4;
            _mp[4] = p5;

            m_err.m_msg_code = code;
            m_err.emit_message(lvl);
        }

        public void TRACEMS8(int lvl, int code, int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8)
        {
            int[] _mp = m_err.m_msg_parm.i;
            _mp[0] = p1;
            _mp[1] = p2;
            _mp[2] = p3;
            _mp[3] = p4;
            _mp[4] = p5;
            _mp[5] = p6;
            _mp[6] = p7;
            _mp[7] = p8;

            m_err.m_msg_code = code;
            m_err.emit_message(lvl);
        }

        public void TRACEMSS(int lvl, int code, string str)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm.s = str;
            m_err.emit_message(lvl);
        }
    }
}
