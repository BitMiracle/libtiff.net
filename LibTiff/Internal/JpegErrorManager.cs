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

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Classic.Internal
{
    /// <summary>
    /// LibJpeg.Net interface layer.
    /// 
    /// We handle fatal errors when they are encountered within the JPEG
    /// library.  We also direct LibJpeg.Net error and warning
    /// messages through the appropriate LibTiff.Net handlers.
    /// </summary>
    class JpegErrorManager : jpeg_error_mgr
    {
        private JpegCodec m_sp;

        public JpegErrorManager(JpegCodec sp)
            : base()
        {
            m_sp = sp;
        }

        /*
        * Error handling routines (these replace corresponding
        * IJG routines).  These are used for both
        * compression and decompression.
        */
        public override void error_exit()
        {
            string buffer = format_message();
            Tiff.ErrorExt(m_sp.GetTiff(), m_sp.GetTiff().m_clientdata, "JPEGLib", "{0}", buffer); /* display the error message */
            
            // clean up LibJpeg.Net state
            m_sp.m_common.jpeg_abort();

            throw new Exception(buffer);
        }

        /*
        * This routine is invoked only for warning messages,
        * since error_exit does its own thing and trace_level
        * is never set > 0.
        */
        public override void output_message()
        {
            string buffer = format_message();
            Tiff.WarningExt(m_sp.GetTiff(), m_sp.GetTiff().m_clientdata, "JPEGLib", "{0}", buffer);
        }
    }
}
