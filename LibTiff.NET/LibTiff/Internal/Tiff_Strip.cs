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
 * Strip-organized Image Support Routines.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private uint summarize(uint summand1, uint summand2, string where)
        {
            /*
             * XXX: We are using casting to uint here, bacause sizeof(uint)
             * may be larger than sizeof(uint) on 64-bit architectures.
             */
            uint bytes = summand1 + summand2;

            if (bytes - summand1 != summand2)
            {
                ErrorExt(this, m_clientdata, m_name, "Integer overflow in %s", where);
                bytes = 0;
            }

            return bytes;
        }

        private int multiply(int nmemb, int elem_size, string where)
        {
            int bytes = nmemb * elem_size;

            if (elem_size != 0 && bytes / elem_size != nmemb)
            {
                ErrorExt(this, m_clientdata, m_name, "Integer overflow in %s", where);
                bytes = 0;
            }

            return bytes;
        }

        /*
        * Return the number of bytes to read/write in a call to
        * one of the scanline-oriented i/o routines.  Note that
        * this number may be 1/samples-per-pixel if data is
        * stored as separate planes.
        * The ScanlineSize in case of YCbCrSubsampling is defined as the
        * strip size divided by the strip height, i.e. the size of a pack of vertical
        * subsampling lines divided by vertical subsampling. It should thus make
        * sense when multiplied by a multiple of vertical subsampling.
        * Some stuff depends on this newer version of TIFFScanlineSize
        * TODO: resolve this
        */
        internal int newScanlineSize()
        {
            int scanline;
            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG)
            {
                if (m_dir.td_photometric == PHOTOMETRIC_YCBCR && !IsUpSampled())
                {
                    UInt16[] ycbcrsubsampling = new ushort[2];
                    GetField(TIFFTAG_YCBCRSUBSAMPLING, &ycbcrsubsampling[0], &ycbcrsubsampling[1]);

                    if (ycbcrsubsampling[0] * ycbcrsubsampling[1] == 0)
                    {
                        ErrorExt(this, m_clientdata, m_name, "Invalid YCbCr subsampling");
                        return 0;
                    }

                    return ((((m_dir.td_imagewidth + ycbcrsubsampling[0] - 1) / ycbcrsubsampling[0]) * (ycbcrsubsampling[0] * ycbcrsubsampling[1] + 2) * m_dir.td_bitspersample + 7) / 8) / ycbcrsubsampling[1];
                }
                else
                {
                    scanline = multiply(m_dir.td_imagewidth, m_dir.td_samplesperpixel, "TIFFScanlineSize");
                }
            }
            else
                scanline = m_dir.td_imagewidth;

            return howMany8(multiply(scanline, m_dir.td_bitspersample, "TIFFScanlineSize"));
        }

        /*
        * Some stuff depends on this older version of TIFFScanlineSize
        * TODO: resolve this
        */
        internal int oldScanlineSize()
        {
            int scanline = multiply(m_dir.td_bitspersample, m_dir.td_imagewidth, "TIFFScanlineSize");
            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG)
                scanline = multiply(scanline, m_dir.td_samplesperpixel, "TIFFScanlineSize");

            return howMany8(scanline);
        }
    }
}
