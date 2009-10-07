/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

/*
 * This file contains the JPEG system-independent memory management
 * routines. 
 */

/*
 * About virtual array management:
 *
 * Full-image-sized buffers
 * are handled as "virtual" arrays.  The array is still accessed a strip at a
 * time, but the memory manager must save the whole array for repeated
 * accesses.
 *
 * The access_virt_array routines are responsible for making a specific strip
 * area accessible.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// JPEG memory management routine for binary arrays.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class jvirt_array<T>
    {
        public delegate T[][] Allocator(int width, int height);

        private jpeg_common_struct m_cinfo;

        private T[][] m_buffer;   /* => the in-memory buffer */

        /// <summary>
        /// Request a virtual 2-D sample array
        /// </summary>
        /// <param name="width">Width of array</param>
        /// <param name="height">Total virtual array height</param>
        public jvirt_array(int width, int height, Allocator allocator)
        {
            m_cinfo = null;
            m_buffer = allocator(width, height);

            Debug.Assert(m_buffer != null);
        }

        public jpeg_common_struct ErrorProcessor
        {
            get { return m_cinfo; }
            set { m_cinfo = value; }
        }

        /// <summary>
        /// Access the part of a virtual sample array starting at start_row
        /// and extending for num_rows rows.
        /// </summary>
        public T[][] access_virt_sarray(int startRow, int numberOfRows)
        {
            /* debugging check */
            if (startRow + numberOfRows > m_buffer.Length)
            {
                if (m_cinfo != null)
                    m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_VIRTUAL_ACCESS);
                else
                    throw new InvalidOperationException("Bogus virtual array access");
            }

            /* Return proper part of the buffer */
            T[][] ret = new T[numberOfRows][];
            for (int i = 0; i < numberOfRows; i++)
                ret[i] = m_buffer[startRow + i];

            return ret;
        }
    }
}
