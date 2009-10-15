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

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private static readonly int[] typemask = 
        {
            0,           /* TIFF_NOTYPE */
            0x000000ff,  /* TIFF_BYTE */
            -1,  /* TIFF_ASCII */
            0x0000ffff,  /* TIFF_SHORT */
            -1,  /* TIFF_LONG */
            -1,  /* TIFF_RATIONAL */
            0x000000ff,  /* TIFF_SBYTE */
            0x000000ff,  /* TIFF_UNDEFINED */
            0x0000ffff,  /* TIFF_SSHORT */
            -1,  /* TIFF_SLONG */
            -1,  /* TIFF_SRATIONAL */
            -1,  /* TIFF_FLOAT */
            -1,  /* TIFF_DOUBLE */
        };

        private static readonly int[] bigTypeshift = 
        {
            0,  /* TIFF_NOTYPE */
            24,  /* TIFF_BYTE */
            0,  /* TIFF_ASCII */
            16,  /* TIFF_SHORT */
            0,  /* TIFF_LONG */
            0,  /* TIFF_RATIONAL */
            24,  /* TIFF_SBYTE */
            24,  /* TIFF_UNDEFINED */
            16,  /* TIFF_SSHORT */
            0,  /* TIFF_SLONG */
            0,  /* TIFF_SRATIONAL */
            0,  /* TIFF_FLOAT */
            0,  /* TIFF_DOUBLE */
        };

        private static readonly int[] litTypeshift = 
        {
            0,  /* TIFF_NOTYPE */
            0,  /* TIFF_BYTE */
            0,  /* TIFF_ASCII */
            0,  /* TIFF_SHORT */
            0,  /* TIFF_LONG */
            0,  /* TIFF_RATIONAL */
            0,  /* TIFF_SBYTE */
            0,  /* TIFF_UNDEFINED */
            0,  /* TIFF_SSHORT */
            0,  /* TIFF_SLONG */
            0,  /* TIFF_SRATIONAL */
            0,  /* TIFF_FLOAT */
            0,  /* TIFF_DOUBLE */
        };

        /*
        * Initialize the shift & mask tables, and the
        * byte swapping state according to the file
        * contents and the machine architecture.
        */
        private void initOrder(int magic)
        {
            m_typemask = typemask;
            if (magic == TIFF_BIGENDIAN)
            {
                m_typeshift = bigTypeshift;
                m_flags |= Tiff.TIFF_SWAB;
            }
            else
            {
                m_typeshift = litTypeshift;
            }
        }

        private static int getMode(string mode, string module)
        {
            int m = -1;

            switch (mode[0])
            {
                case 'r':
                    m = O_RDONLY;
                    if (mode[1] == '+')
                        m = O_RDWR;
                    break;
                case 'w':
                case 'a':
                    m = O_RDWR | O_CREAT;
                    if (mode[0] == 'w')
                        m |= O_TRUNC;
                    break;
                default:
                    ErrorExt(null, 0, module, "\"%s\": Bad mode", mode);
                    break;
            }

            return m;
        }

        private Tiff safeOpenFailed()
        {
            m_mode = O_RDONLY; /* XXX avoid flush */
            return null;
        }
    }
}
