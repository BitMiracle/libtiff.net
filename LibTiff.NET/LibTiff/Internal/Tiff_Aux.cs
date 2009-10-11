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

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private static bool defaultTransferFunction(TiffDirectory td)
        {
            UInt16** tf = td.td_transferfunction;
            tf[0] = null;
            tf[1] = null;
            tf[2] = null;

            if (td.td_bitspersample >= sizeof(int) * 8 - 2)
                return false;

            int n = 1 << td.td_bitspersample;
            int nbytes = n * sizeof(UInt16);
            tf[0] = new UInt16 [n];
            if (!tf[0])
                return false;

            tf[0][0] = 0;
            for (int i = 1; i < n; i++)
            {
                double t = (double)i / ((double)n - 1.);
                tf[0][i] = (UInt16)floor(65535. * pow(t, 2.2) + 0.5);
            }

            bool failed = false;
            if (td.td_samplesperpixel - td.td_extrasamples > 1)
            {
                tf[1] = new UInt16 [n];
                if (!tf[1])
                    failed = true;

                if (!failed)
                {
                    memcpy(tf[1], tf[0], nbytes);

                    tf[2] = new UInt16 [n];
                    if (!tf[2])
                        failed = true;

                    if (!failed)
                        memcpy(tf[2], tf[0], nbytes);
                }
            }

            if (failed)
            {
                delete tf[0];
                delete tf[1];
                delete tf[2];
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
            int* integers = new int[intCount];

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
            uint* integers = new uint[intCount];

            int byteStopPos = byteStartOffset + intCount * 4;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                uint value = b[i++] & 0xFF;
                value += (b[i++] & 0xFF) << 8;
                value += (b[i++] & 0xFF) << 16;
                value += b[i++] << 24;
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

        internal static Int16[] byteArrayToInt16(byte[] b, int byteStartOffset, int byteCount)
        {
            int intCount = byteCount / 2;
            Int16* integers = new Int16[intCount];

            int byteStopPos = byteStartOffset + intCount * 2;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                Int16 value = b[i++] & 0xFF;
                value += (b[i++] & 0xFF) << 8;
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
                Int16 value = integers[i];
                bytes[bytePos++] = (byte)value;
                bytes[bytePos++] = (byte)(value >> 8);
            }
        }

        internal static UInt16[] byteArrayToUInt16(byte[] b, int byteStartOffset, int byteCount)
        {
            int intCount = byteCount / 2;
            UInt16* integers = new UInt16[intCount];

            int byteStopPos = byteStartOffset + intCount * 2;
            int intPos = 0;
            for (int i = byteStartOffset; i < byteStopPos; )
            {
                UInt16 value = b[i++] & 0xFF;
                value += (b[i++] & 0xFF) << 8;
                integers[intPos++] = value;
            }

            return integers;
        }

        internal static void uint16ToByteArray(UInt16[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
        {
            int bytePos = byteStartOffset;
            int intStopPos = intStartOffset + intCount;
            for (int i = intStartOffset; i < intStopPos; i++)
            {
                UInt16 value = integers[i];
                bytes[bytePos++] = (byte)value;
                bytes[bytePos++] = (byte)(value >> 8);
            }
        }

        internal uint readUInt32(byte[] b, int byteStartOffset)
        {
            uint value = b[byteStartOffset++] & 0xFF;
            value += (b[byteStartOffset++] & 0xFF) << 8;
            value += (b[byteStartOffset++] & 0xFF) << 16;
            value += b[byteStartOffset++] << 24;
            return value;
        }

        internal void writeUInt32(uint value, byte[] b, int byteStartOffset)
        {
            b[byteStartOffset++] = (byte)value;
            b[byteStartOffset++] = (byte)(value >> 8);
            b[byteStartOffset++] = (byte)(value >> 16);
            b[byteStartOffset++] = (byte)(value >> 24);
        }

        internal UInt16 readUInt16(byte[] b, int byteStartOffset)
        {
            UInt16 value = b[byteStartOffset] & 0xFF;
            value += (b[byteStartOffset + 1] & 0xFF) << 8;
            return value;
        }
    }
}
