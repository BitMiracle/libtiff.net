/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

/*
 * Builtin Compression Scheme Configuration Support.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        /*
         * Compression schemes statically built into the library.
         */
        private void setupBuiltInCodecs()
        {
            m_builtInCodecs = new TiffCodec*[19];

            int i = 0;
            m_builtInCodecs[i++] = new TiffCodec(this, -1, "Not configured");
            m_builtInCodecs[i++] = new DumpModeCodec(this, COMPRESSION.COMPRESSION_NONE, "None");
            m_builtInCodecs[i++] = new LZWCodec(this, COMPRESSION.COMPRESSION_LZW, "LZW");
            m_builtInCodecs[i++] = new PackBitsCodec(this, COMPRESSION.COMPRESSION_PACKBITS, "PackBits");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_THUNDERSCAN, "ThunderScan");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_NEXT, "NeXT");
            m_builtInCodecs[i++] = new JpegCodec(this, COMPRESSION.COMPRESSION_JPEG, "JPEG");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_OJPEG, "Old-style JPEG");
            m_builtInCodecs[i++] = new CCITTCodec(this, COMPRESSION.COMPRESSION_CCITTRLE, "CCITT RLE");
            m_builtInCodecs[i++] = new CCITTCodec(this, COMPRESSION.COMPRESSION_CCITTRLEW, "CCITT RLE/W");
            m_builtInCodecs[i++] = new CCITTCodec(this, COMPRESSION.COMPRESSION_CCITTFAX3, "CCITT Group 3");
            m_builtInCodecs[i++] = new CCITTCodec(this, COMPRESSION.COMPRESSION_CCITTFAX4, "CCITT Group 4");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_JBIG, "ISO JBIG");
            m_builtInCodecs[i++] = new DeflateCodec(this, COMPRESSION.COMPRESSION_DEFLATE, "Deflate");
            m_builtInCodecs[i++] = new DeflateCodec(this, COMPRESSION.COMPRESSION_ADOBE_DEFLATE, "AdobeDeflate");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_PIXARLOG, "PixarLog");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_SGILOG, "SGILog");
            m_builtInCodecs[i++] = new TiffCodec(this, COMPRESSION.COMPRESSION_SGILOG24, "SGILog24");
            m_builtInCodecs[i++] = null;
        }

        private void freeCodecs()
        {
            if (m_builtInCodecs != null)
            {
                int i = 0;
                while (m_builtInCodecs[i] != null)
                {
                    delete m_builtInCodecs[i];
                    i++;
                }

                delete[] m_builtInCodecs;
            }

            while (m_registeredCodecs != null)
            {
                codecList* c = m_registeredCodecs;
                m_registeredCodecs = m_registeredCodecs.next;
                delete c.codec;
                delete c;
            }
        }
    }
}
