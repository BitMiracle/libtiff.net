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

namespace BitMiracle.LibJpeg.Classic
{
    /// <summary>
    /// The control blocks for virtual arrays.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    class jvirt_sarray_control
    {
        private jvirt_array<byte> m_implementation;

        // Request a virtual 2-D sample array
        public jvirt_sarray_control(int samplesperrow, int numrows)
        {
            m_implementation = new jvirt_array<byte>(samplesperrow, numrows, jpeg_common_struct.AllocJpegSamples);
        }

        public jpeg_common_struct ErrorProcessor
        {
            get { return m_implementation.ErrorProcessor; }
            set { m_implementation.ErrorProcessor = value; }
        }

        /// <summary>
        /// Access the part of a virtual sample array starting at start_row
        /// and extending for num_rows rows.
        /// </summary>
        public byte[][] access_virt_sarray(int start_row, int num_rows)
        {
            return m_implementation.access_virt_sarray(start_row, num_rows);
        }
    }
}
