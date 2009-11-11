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
 * Auxiliary Support Routines.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff.Internal;
using System.Globalization;

namespace BitMiracle.LibTiff
{
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        private static bool defaultTransferFunction(TiffDirectory td)
        {
            short[][] tf = td.td_transferfunction;
            tf[0] = null;
            tf[1] = null;
            tf[2] = null;

            if (td.td_bitspersample >= sizeof(int) * 8 - 2)
                return false;

            int n = 1 << td.td_bitspersample;
            tf[0] = new short [n];
            tf[0][0] = 0;
            for (int i = 1; i < n; i++)
            {
                double t = (double)i / ((double)n - 1.0);
                tf[0][i] = (short)Math.Floor(65535.0 * Math.Pow(t, 2.2) + 0.5);
            }

            if (td.td_samplesperpixel - td.td_extrasamples > 1)
            {
                tf[1] = new short [n];
                Array.Copy(tf[0], tf[1], tf[0].Length);

                tf[2] = new short [n];
                Array.Copy(tf[0], tf[2], tf[0].Length);
            }

            return true;
        }

        internal int readInt(byte[] b, int byteStartOffset)
        {
            int value = b[byteStartOffset++] & 0xFF;
            value += (b[byteStartOffset++] & 0xFF) << 8;
            value += (b[byteStartOffset++] & 0xFF) << 16;
            value += b[byteStartOffset++] << 24;
            return value;
        }

        internal void writeInt(int value, byte[] b, int byteStartOffset)
        {
            b[byteStartOffset++] = (byte)value;
            b[byteStartOffset++] = (byte)(value >> 8);
            b[byteStartOffset++] = (byte)(value >> 16);
            b[byteStartOffset++] = (byte)(value >> 24);
        }

        internal ushort readUInt16(byte[] b, int byteStartOffset)
        {
            ushort value = (ushort)(b[byteStartOffset] & 0xFF);
            value += (ushort)((b[byteStartOffset + 1] & 0xFF) << 8);
            return value;
        }

        internal static void fprintf(Stream fd, string format, params object[] list)
        {
            string s = string.Format(CultureInfo.InvariantCulture, format, list);
            byte[] bytes = Latin1Encoding.GetBytes(s);
            fd.Write(bytes, 0, bytes.Length);
        }

        private static string encodeOctalString(byte value)
        {
            //convert to int, for cleaner syntax below. 
            int x = (int)value;

            //return octal encoding \ddd of the character value. 
            return string.Format(@"\{0}{1}{2}", (x >> 6) & 7, (x >> 3) & 7, x & 7);
        }
    }
}
