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
    /// Huffman coding tables.
    /// </summary>
    public class JHUFF_TBL
    {
        /* These two fields directly represent the contents of a JPEG DHT marker */
        private readonly byte[] m_bits = new byte[17];     /* bits[k] = # of symbols with codes of */
        
        /* length k bits; bits[0] is unused */
        private readonly byte[] m_huffval = new byte[256];     /* The symbols, in order of incr code length */
        
        private bool sent_table;        /* true when table has been output */


        public JHUFF_TBL()
        {
            sent_table = false;
        }

        public byte[] Bits
        {
            get { return m_bits; }
        }

        public byte[] Huffval
        {
            get { return m_huffval; }
        }

        /* This property is used only during compression.  It's initialized false when
         * the table is created, and set true when it's been output to the file.
         * You could suppress output of a table by setting this to true.
         * (See jpeg_suppress_tables for an example.)
         */
        public bool Sent_table
        {
            get { return sent_table; }
            set { sent_table = value; }
        }
    }
}
