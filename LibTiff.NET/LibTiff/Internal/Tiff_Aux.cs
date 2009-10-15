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

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private static bool defaultTransferFunction(TiffDirectory td)
        {
            ushort[][] tf = td.td_transferfunction;
            tf[0] = null;
            tf[1] = null;
            tf[2] = null;

            if (td.td_bitspersample >= sizeof(int) * 8 - 2)
                return false;

            int n = 1 << td.td_bitspersample;
            int nbytes = n * sizeof(ushort);
            tf[0] = new ushort [n];
            if (tf[0] == null)
                return false;

            tf[0][0] = 0;
            for (int i = 1; i < n; i++)
            {
                double t = (double)i / ((double)n - 1.0);
                tf[0][i] = (ushort)Math.Floor(65535.0 * Math.Pow(t, 2.2) + 0.5);
            }

            bool failed = false;
            if (td.td_samplesperpixel - td.td_extrasamples > 1)
            {
                tf[1] = new ushort [n];
                if (tf[1] == null)
                    failed = true;

                if (!failed)
                {
                    Array.Copy(tf[0], tf[1], nbytes);

                    tf[2] = new ushort [n];
                    if (tf[2] == null)
                        failed = true;

                    if (!failed)
                        Array.Copy(tf[0], tf[2], nbytes);
                }
            }

            if (failed)
            {
                tf[0] = null;
                tf[1] = null;
                tf[2] = null;
                return false;
            }

            return true;
        }

        internal static int[] byteArrayToInt(byte[] b, int byteStartOffset, int byteCount)
        {
            int intCount = byteCount / 4;
            int[] integers = new int[intCount];

            int byteStopPos = byteStartOffset + intCount * 4;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                int value = b[i++] & 0xFF;
                value += (b[i++] & 0xFF) << 8;
                value += (b[i++] & 0xFF) << 16;
                value += b[i++] << 24;
                integers[intPos++] = value;
            }

            return integers;
        }

        internal static void intToByteArray(int[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
        {
            int bytePos = byteStartOffset;
            int intStopPos = intStartOffset + intCount;
            for (int i = intStartOffset; i < intStopPos; i++)
            {
                int value = integers[i];
                bytes[bytePos++] = (byte)value;
                bytes[bytePos++] = (byte)(value >> 8);
                bytes[bytePos++] = (byte)(value >> 16);
                bytes[bytePos++] = (byte)(value >> 24);
            }
        }

        internal static uint[] byteArrayToUInt(byte[] b, int byteStartOffset, int byteCount)
        {
            int intCount = byteCount / 4;
            uint[] integers = new uint[intCount];

            int byteStopPos = byteStartOffset + intCount * 4;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                uint value = (uint)(b[i++] & 0xFF);
                value += (uint)((b[i++] & 0xFF) << 8);
                value += (uint)((b[i++] & 0xFF) << 16);
                value += (uint)(b[i++] << 24);
                integers[intPos++] = value;
            }

            return integers;
        }

        internal static void uintToByteArray(uint[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
        {
            int bytePos = byteStartOffset;
            int intStopPos = intStartOffset + intCount;
            for (int i = intStartOffset; i < intStopPos; i++)
            {
                uint value = integers[i];
                bytes[bytePos++] = (byte)value;
                bytes[bytePos++] = (byte)(value >> 8);
                bytes[bytePos++] = (byte)(value >> 16);
                bytes[bytePos++] = (byte)(value >> 24);
            }
        }

        internal static short[] byteArrayToInt16(byte[] b, int byteStartOffset, int byteCount)
        {
            int intCount = byteCount / 2;
            short[] integers = new short[intCount];

            int byteStopPos = byteStartOffset + intCount * 2;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                short value = (short)(b[i++] & 0xFF);
                value += (short)((b[i++] & 0xFF) << 8);
                integers[intPos++] = value;
            }

            return integers;
        }

        internal static void int16ToByteArray(Int16[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
        {
            int bytePos = byteStartOffset;
            int intStopPos = intStartOffset + intCount;
            for (int i = intStartOffset; i < intStopPos; i++)
            {
                short value = integers[i];
                bytes[bytePos++] = (byte)value;
                bytes[bytePos++] = (byte)(value >> 8);
            }
        }

        internal static ushort[] byteArrayToUInt16(byte[] b, int byteStartOffset, int byteCount)
        {
            int intCount = byteCount / 2;
            ushort[] integers = new ushort[intCount];

            int byteStopPos = byteStartOffset + intCount * 2;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                ushort value = (ushort)(b[i++] & 0xFF);
                value += (ushort)((b[i++] & 0xFF) << 8);
                integers[intPos++] = value;
            }

            return integers;
        }

        internal static void uint16ToByteArray(ushort[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
        {
            int bytePos = byteStartOffset;
            int intStopPos = intStartOffset + intCount;
            for (int i = intStartOffset; i < intStopPos; i++)
            {
                ushort value = integers[i];
                bytes[bytePos++] = (byte)value;
                bytes[bytePos++] = (byte)(value >> 8);
            }
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
            string s = string.Format(format, list);
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            fd.Write(bytes, 0, bytes.Length);
        }
    }
}
