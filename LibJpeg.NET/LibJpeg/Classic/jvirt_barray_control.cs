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

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// JPEG system-independent memory management routines for binary arrays. 
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class jvirt_barray_control
    {
        private jpeg_common_struct m_cinfo;
        private JBLOCK[][] m_mem_buffer;    /* => the in-memory buffer */
        private int m_rows_in_array;   /* total virtual array height */
        private int m_blocksperrow;    /* width of array (and of memory buffer) */

        // Request a virtual 2-D coefficient-block array
        public jvirt_barray_control(jpeg_common_struct cinfo, bool pre_zero, int blocksperrow, int numrows)
        {
            m_cinfo = cinfo;
            m_mem_buffer = null;  /* marks array not yet realized */
            m_rows_in_array = numrows;
            m_blocksperrow = blocksperrow;

            m_mem_buffer = new JBLOCK[m_rows_in_array][];
            for (int i = 0; i < m_rows_in_array; i++)
            {
                m_mem_buffer[i] = new JBLOCK[m_blocksperrow];
                for (int j = 0; j < m_blocksperrow; j++)
                    m_mem_buffer[i][j] = new JBLOCK();
            }
        }

        /// <summary>
        /// Access the part of a virtual block array starting at start_row
        /// and extending for num_rows rows.
        /// </summary>
        public JBLOCK[][] access_virt_barray(int start_row, int num_rows)
        {
            int end_row = start_row + num_rows;

            /* debugging check */
            if (end_row > m_rows_in_array || m_mem_buffer == null)
                m_cinfo.ERREXIT(J_MESSAGE_CODE.JERR_BAD_VIRTUAL_ACCESS);

            /* Return proper part of the buffer */
            JBLOCK[][] ret = new JBLOCK[num_rows][];
            for (int i = 0; i < num_rows; i++)
                ret[i] = m_mem_buffer[start_row + i];

            return ret;
        }
    }
}
