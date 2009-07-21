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
    /// Data destination object for compression
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    abstract class jpeg_destination_mgr
    {
        private byte[] m_next_output_byte;   /* => next byte to write in buffer */
        private int m_position;
        private int m_free_in_buffer;  /* # of byte spaces remaining in buffer */

        public abstract void init_destination();
        public abstract bool empty_output_buffer();
        public abstract void term_destination();

        /// <summary>
        /// Emit a byte
        /// </summary>
        public virtual bool emit_byte(int val)
        {
            m_next_output_byte[m_position] = (byte)val;
            m_position++;

            if (--m_free_in_buffer == 0)
            {
                if (!empty_output_buffer())
                    return false;
            }

            return true;
        }

        protected void initInternalBuffer(byte[] buffer, int size)
        {
            m_next_output_byte = buffer;
            m_free_in_buffer = size;
            m_position = 0;
        }

        protected int freeInBuffer()
        {
            return m_free_in_buffer;
        }
    }
}
