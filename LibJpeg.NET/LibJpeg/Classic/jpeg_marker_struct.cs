/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// The decompressor can save APPn and COM markers in a list of these:
    /// </summary>
    /// <remarks>The marker length word is not counted in Data.Length or OriginalLength</remarks>
#if EXPOSE_LIBJPEG
    public
#endif
    class jpeg_marker_struct
    {
        private byte m_marker;           /* marker code: JPEG_COM, or JPEG_APP0+n */
        private int m_originalLength;   /* # bytes of data in the file */
        private byte[] m_data;       /* the data contained in the marker */

        internal jpeg_marker_struct(byte marker, int originalDataLength, int lengthLimit)
        {
            m_marker = marker;
            m_originalLength = originalDataLength;
            m_data = new byte[lengthLimit];
        }

        public byte Marker
        {
            get
            {
                return m_marker;
            }
        }

        public int OriginalLength
        {
            get
            {
                return m_originalLength;
            }
        }

        public byte[] Data
        {
            get
            {
                return m_data;
            }
        }
    }
}
