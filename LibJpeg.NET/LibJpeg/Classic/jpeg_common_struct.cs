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

namespace LibJpeg.Classic
{
    /// <summary>
    ///  Common fields between JPEG compression and decompression master structs.
    ///  Routines that are to be used by both halves of the library are declared
    ///  to receive an instance of this structure. There are no actual instances of 
    ///  jpeg_common_struct, only of jpeg_compress_struct and jpeg_decompress_struct.
    /// </summary>
    public class jpeg_common_struct
    {
        internal enum JpegState
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

        // Error handler module
        internal jpeg_error_mgr m_err;
        
        // Progress monitor, or null if none
        internal jpeg_progress_mgr m_progress;
        internal bool m_is_decompressor;   /* So common code can tell which is which */
        
        internal JpegState m_global_state;     /* For checking call sequence validity */

        public jpeg_progress_mgr Progress
        {
            get { return m_progress; }
            set { m_progress = value; }
        }

        public LibJpeg.Classic.jpeg_error_mgr Err
        {
            get { return m_err; }
            set { m_err = value; }
        }

        // Creation of 2-D sample arrays.
        public static byte[][] AllocJpegSamples(int samplesPerRow, int numberOfRows)
        {
            byte[][] result = new byte[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++)
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
                ((jpeg_decompress_struct)this).m_marker_list = null;
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

        public void ERREXIT(J_MESSAGE_CODE code)
        {
            ERREXIT((int)code);
        }

        public void ERREXIT(int code)
        {
            m_err.m_msg_code = code;
            m_err.error_exit();
        }

        public void ERREXIT(J_MESSAGE_CODE code, params object[] args)
        {
            ERREXIT((int)code, args);
        }

        public void ERREXIT(int code, params object[] args)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm = args;
            m_err.error_exit();
        }

        // Nonfatal errors (we can keep going, but the data is probably corrupt)

        public void WARNMS(J_MESSAGE_CODE code)
        {
            WARNMS((int)code);
        }

        public void WARNMS(int code)
        {
            m_err.m_msg_code = code;
            m_err.emit_message(-1);
        }

        public void WARNMS(J_MESSAGE_CODE code, params object[] args)
        {
            WARNMS((int)code, args);
        }

        public void WARNMS(int code, params object[] args)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm = args;
            m_err.emit_message(-1);
        }

        // Informational/debugging messages

        public void TRACEMS(int lvl, J_MESSAGE_CODE code)
        {
            TRACEMS(lvl, (int)code);
        }

        public void TRACEMS(int lvl, int code)
        {
            m_err.m_msg_code = code;
            m_err.emit_message(lvl);
        }

        public void TRACEMS(int lvl, J_MESSAGE_CODE code, params object[] args)
        {
            TRACEMS(lvl, (int)code, args);
        }

        public void TRACEMS(int lvl, int code, params object[] args)
        {
            m_err.m_msg_code = code;
            m_err.m_msg_parm = args;
            m_err.emit_message(lvl);
        }
    }
}
