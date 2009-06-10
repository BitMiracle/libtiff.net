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
using System.Text;

namespace LibJpeg.Classic
{
    /// <summary>
    /// The control blocks for virtual arrays.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class jvirt_sarray_control
    {
        private jpeg_common_struct m_cinfo;
        private byte[][] m_mem_buffer;   /* => the in-memory buffer */
        private int m_rows_in_array;   /* total virtual array height */
        private int m_samplesperrow;   /* width of array (and of memory buffer) */

        // Request a virtual 2-D sample array
        public jvirt_sarray_control(jpeg_common_struct cinfo, bool pre_zero, int samplesperrow, int numrows)
        {
            m_cinfo = cinfo;
            m_rows_in_array = numrows;
            m_samplesperrow = samplesperrow;
            m_mem_buffer = jpeg_common_struct.AllocJpegSamples(m_samplesperrow, m_rows_in_array);
        }

        /// <summary>
        /// Access the part of a virtual sample array starting at start_row
        /// and extending for num_rows rows.
        /// </summary>
        public byte[][] access_virt_sarray(int start_row, int num_rows)
        {
            int end_row = start_row + num_rows;

            /* debugging check */
            if (end_row > m_rows_in_array || m_mem_buffer == null)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_VIRTUAL_ACCESS);

            /* Return proper part of the buffer */
            byte[][] ret = new byte[num_rows][];
            for (int i = 0; i < num_rows; i++)
                ret[i] = m_mem_buffer[start_row + i];

            return ret;
        }
    }
}
