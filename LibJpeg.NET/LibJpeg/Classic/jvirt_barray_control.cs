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
        private jvirt_array<JBLOCK> m_implementation;

        // Request a virtual 2-D coefficient-block array
        public jvirt_barray_control(jpeg_common_struct cinfo, int blocksperrow, int numrows)
        {
            m_implementation = new jvirt_array<JBLOCK>(blocksperrow, numrows, jpeg_common_struct.AllocJpegBlocks);
            m_implementation.ErrorProcessor = cinfo;
        }

        /// <summary>
        /// Access the part of a virtual block array starting at start_row
        /// and extending for num_rows rows.
        /// </summary>
        public JBLOCK[][] access_virt_barray(int start_row, int num_rows)
        {
            return m_implementation.access_virt_sarray(start_row, num_rows);
        }
    }
}
