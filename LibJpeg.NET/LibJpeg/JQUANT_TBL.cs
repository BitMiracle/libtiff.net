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

namespace LibJpeg.Classic
{
    /// <summary>
    /// DCT coefficient quantization tables.
    /// </summary>
    class JQUANT_TBL
    {
        public JQUANT_TBL()
        {
            sent_table = false;
        }

        /* This array gives the coefficient quantizers in natural array order
         * (not the zigzag order in which they are stored in a JPEG DQT marker).
         * CAUTION: IJG versions prior to v6a kept this array in zigzag order.
         */
        public ushort[] quantval = new ushort[JpegConstants.DCTSIZE2];  /* quantization step for each coefficient */

        /* This field is used only during compression.  It's initialized false when
         * the table is created, and set true when it's been output to the file.
         * You could suppress output of a table by setting this to true.
         * (See jpeg_suppress_tables for an example.)
         */
        public bool sent_table;        /* true when table has been output */
    }
}
