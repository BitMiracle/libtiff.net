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

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        /*
         * Compression schemes statically built into the library.
         */
        private void setupBuiltInCodecs()
        {
            m_builtInCodecs = new TiffCodec[19];

            int i = 0;
            m_builtInCodecs[i++] = new TiffCodec(this, (Compression)(-1), "Not configured");
            m_builtInCodecs[i++] = new DumpModeCodec(this, Compression.NONE, "None");
            m_builtInCodecs[i++] = new LZWCodec(this, Compression.LZW, "LZW");
            m_builtInCodecs[i++] = new PackBitsCodec(this, Compression.PACKBITS, "PackBits");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.THUNDERSCAN, "ThunderScan");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.NEXT, "NeXT");
            m_builtInCodecs[i++] = new JpegCodec(this, Compression.JPEG, "JPEG");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.OJPEG, "Old-style JPEG");
            m_builtInCodecs[i++] = new CCITTCodec(this, Compression.CCITTRLE, "CCITT RLE");
            m_builtInCodecs[i++] = new CCITTCodec(this, Compression.CCITTRLEW, "CCITT RLE/W");
            m_builtInCodecs[i++] = new CCITTCodec(this, Compression.CCITTFAX3, "CCITT Group 3");
            m_builtInCodecs[i++] = new CCITTCodec(this, Compression.CCITTFAX4, "CCITT Group 4");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.JBIG, "ISO JBIG");
            m_builtInCodecs[i++] = new DeflateCodec(this, Compression.DEFLATE, "Deflate");
            m_builtInCodecs[i++] = new DeflateCodec(this, Compression.ADOBE_DEFLATE, "AdobeDeflate");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.PIXARLOG, "PixarLog");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.SGILOG, "SGILog");
            m_builtInCodecs[i++] = new TiffCodec(this, Compression.SGILOG24, "SGILog24");
            m_builtInCodecs[i++] = null;
        }

        private void freeCodecs()
        {
            m_builtInCodecs = null;
            m_registeredCodecs = null;
        }
    }
}
