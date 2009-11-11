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
 * Scanline-oriented Read Support
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        private const int NOSTRIP = -1;         /* undefined state */
        private const int NOTILE = -1;          /* undefined state */

        internal const int O_RDONLY = 0;
        internal const int O_WRONLY = 0x0001;
        internal const int O_CREAT = 0x0100;
        internal const int O_TRUNC = 0x0200;
        internal const int O_RDWR = 0x0002;

        /*
        * Default Read/Seek/Write definitions.
        */
        private int readFile(byte[] buf, int offset, int size)
        {
            return m_stream.Read(m_clientdata, buf, offset, size);
        }

        private int seekFile(int off, SeekOrigin whence)
        {
            return (int)m_stream.Seek(m_clientdata, off, whence);
        }

        private void closeFile()
        {
            m_stream.Close(m_clientdata);
        }

        private int getFileSize()
        {
            return (int)m_stream.Size(m_clientdata);
        }

        private bool readOK(byte[] buf, int size)
        {
            return (readFile(buf, 0, size) == size);
        }

        private bool readShortOK(out short value)
        {
            byte[] bytes = new byte[2];
            bool res = readOK(bytes, 2);
            value = 0;
            if (res)
            {
                value = (short)(bytes[0] & 0xFF);
                value += (short)((bytes[1] & 0xFF) << 8);
            }

            return res;
        }

        private bool readIntOK(out int value)
        {
            byte[] cp = new byte[4];
            bool res = readOK(cp, 4);
            value = 0;
            if (res)
            {
                value = (cp[0] & 0xFF);
                value += (int)((cp[1] & 0xFF) << 8);
                value += (int)((cp[2] & 0xFF) << 16);
                value += (int)(cp[3] << 24);
            }

            return res;
        }

        private bool readDirEntryOk(TiffDirEntry[] dir, short dircount)
        {
            int entrySize = sizeof(ushort) * 2 + sizeof(uint) * 2;
            int totalSize = entrySize * dircount;
            byte[] bytes = new byte[totalSize];
            bool res = readOK(bytes, totalSize);
            if (res)
                readDirEntry(dir, dircount, bytes, 0);

            return res;
        }

        private void readDirEntry(TiffDirEntry[] dir, short dircount, byte[] bytes, uint offset)
        {
            int pos = (int)offset;
            for (int i = 0; i < dircount; i++)
            {
                TiffDirEntry entry = new TiffDirEntry();
                entry.tdir_tag = (TIFFTAG)readUInt16(bytes, pos);
                pos += sizeof(ushort);
                entry.tdir_type = (TiffDataType)readUInt16(bytes, pos);
                pos += sizeof(ushort);
                entry.tdir_count = readInt(bytes, pos);
                pos += sizeof(uint);
                entry.tdir_offset = readInt(bytes, pos);
                pos += sizeof(uint);
                dir[i] = entry;
            }
        }

        private bool readHeaderOk(ref TiffHeader header)
        {
            bool res = readShortOK(out header.tiff_magic);

            if (res)
                res = readShortOK(out header.tiff_version);

            if (res)
                res = readIntOK(out header.tiff_diroff);

            return res;
        }

        private bool seekOK(int off)
        {
            return (seekFile(off, SeekOrigin.Begin) == off);
        }

        /*
        * Seek to a random row+sample in a file.
        */
        private bool seek(int row, short sample)
        {
            if (row >= m_dir.td_imagelength)
            {
                /* out of range */
                ErrorExt(this, m_clientdata, m_name,
                    "{0}: Row out of range, max {1}", row, m_dir.td_imagelength);
                return false;
            }

            int strip;
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name,
                        "{0}: Sample out of range, max {1}", sample, m_dir.td_samplesperpixel);
                    return false;
                }

                strip = (int)(sample * m_dir.td_stripsperimage + row / m_dir.td_rowsperstrip);
            }
            else
                strip = (int)(row / m_dir.td_rowsperstrip);
            
            if (strip != m_curstrip)
            {
                /* different strip, refill */
                if (!fillStrip(strip))
                    return false;
            }
            else if (row < m_row)
            {
                /*
                 * Moving backwards within the same strip: backup
                 * to the start and then decode forward (below).
                 *
                 * NB: If you're planning on lots of random access within a
                 * strip, it's better to just read and decode the entire
                 * strip, and then access the decoded data in a random fashion.
                 */
                if (!startStrip(strip))
                    return false;
            }

            if (row != m_row)
            {
                /*
                 * Seek forward to the desired row.
                 */
                if (!m_currentCodec.tif_seek(row - m_row))
                    return false;

                m_row = row;
            }

            return true;
        }

        private int readRawStrip1(int strip, byte[] buf, int offset, int size, string module)
        {
            Debug.Assert((m_flags & TIFF_NOREADRAW) == 0);

            if (!seekOK(m_dir.td_stripoffset[strip]))
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Seek error at scanline {1}, strip {2}", m_name, m_row, strip);
                return -1;
            }

            int cc = readFile(buf, offset, size);
            if (cc != size)
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Read error at scanline {1}; got {2} bytes, expected {3}",
                    m_name, m_row, cc, size);
                return -1;
            }

            return size;
        }

        private int readRawTile1(int tile, byte[] buf, int offset, int size, string module)
        {
            Debug.Assert((m_flags & TIFF_NOREADRAW) == 0);

            if (!seekOK(m_dir.td_stripoffset[tile]))
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Seek error at row {1}, col {2}, tile {3}", m_name, m_row, m_col, tile);
                return -1;
            }

            int cc = readFile(buf, offset, size);
            if (cc != size)
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Read error at row {1}, col {2}; got {3} bytes, expected {4}",
                    m_name, m_row, m_col, cc, size);
                return -1;
            }

            return size;
        }

        /*
        * Set state to appear as if a
        * strip has just been read in.
        */
        private bool startStrip(int strip)
        {
            if ((m_flags & TIFF_CODERSETUP) == 0)
            {
                if (!m_currentCodec.tif_setupdecode())
                    return false;

                m_flags |= TIFF_CODERSETUP;
            }

            m_curstrip = strip;
            m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
            m_rawcp = 0;

            if ((m_flags & TIFF_NOREADRAW) != 0)
                m_rawcc = 0;
            else
                m_rawcc = (int)m_dir.td_stripbytecount[strip];

            return m_currentCodec.tif_predecode((short)(strip / m_dir.td_stripsperimage));
        }

        /*
        * Set state to appear as if a
        * tile has just been read in.
        */
        private bool startTile(int tile)
        {
            if ((m_flags & TIFF_CODERSETUP) == 0)
            {
                if (!m_currentCodec.tif_setupdecode())
                    return false;

                m_flags |= TIFF_CODERSETUP;
            }

            m_curtile = tile;
            m_row = (tile % howMany(m_dir.td_imagewidth, m_dir.td_tilewidth)) * m_dir.td_tilelength;
            m_col = (tile % howMany(m_dir.td_imagelength, m_dir.td_tilelength)) * m_dir.td_tilewidth;
            m_rawcp = 0;
            if ((m_flags & TIFF_NOREADRAW) != 0)
                m_rawcc = 0;
            else
                m_rawcc = (int)m_dir.td_stripbytecount[tile];

            return m_currentCodec.tif_predecode((short)(tile / m_dir.td_stripsperimage));
        }

        private bool checkRead(int tiles)
        {
            if (m_mode == O_WRONLY)
            {
                ErrorExt(this, m_clientdata, m_name, "File not open for reading");
                return false;
            }

            int temp = 0;
            if (IsTiled())
                temp = 1;

            if ((tiles ^ temp) != 0)
            {
                ErrorExt(this, m_clientdata, m_name, tiles != 0 ? "Can not read tiles from a stripped image": "Can not read scanlines from a tiled image");
                return false;
            }

            return true;
        }

        private static void swab16BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc & 1) == 0);
            ushort[] swabee = ByteArrayToUInt16(buf, 0, cc);
            SwabArrayOfShort(swabee, cc / 2);
            UInt16ToByteArray(swabee, 0, cc / 2, buf, 0);
        }

        private static void swab24BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc % 3) == 0);
            SwabArrayOfTriples(buf, cc / 3);
        }

        private static void swab32BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc & 3) == 0);
            int[] swabee = ByteArrayToInt(buf, 0, cc);
            SwabArrayOfLong(swabee, cc / 4);
            IntToByteArray(swabee, 0, cc / 4, buf, 0);
        }

        private static void swab64BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc & 7) == 0);

            int doubleCount = cc / 8;
            double[] doubles = new double[doubleCount];
            int byteOffset = 0;
            for (int i = 0; i < doubleCount; i++)
            {
                doubles[i] = BitConverter.ToDouble(buf, byteOffset);
                byteOffset += 8;
            }

            SwabArrayOfDouble(doubles, doubleCount);

            byteOffset = 0;
            for (int i = 0; i < doubleCount; i++)
            {
                byte[] bytes = BitConverter.GetBytes(doubles[i]);
                Array.Copy(bytes, 0, buf, byteOffset, bytes.Length);
                byteOffset += bytes.Length;
            }
        }

        /*
        * Read the specified strip and setup for decoding. 
        * The data buffer is expanded, as necessary, to
        * hold the strip's data.
        */
        internal bool fillStrip(int strip)
        {
            const string module = "fillStrip";
    
            if ((m_flags & TIFF_NOREADRAW) == 0)
            {
                /*
                * FIXME: bytecount should have int type, but for now
                * libtiff defines int as a signed 32-bit integer and we
                * are losing ability to read arrays larger than 2^31 bytes.
                * So we are using uint instead of int here.
                */

                int bytecount = m_dir.td_stripbytecount[strip];
                if (bytecount <= 0)
                {
                    ErrorExt(this, m_clientdata, m_name,
                        "{0}: Invalid strip byte count, strip {1}", bytecount, strip);
                    return false;
                }

                /*
                 * Expand raw data buffer, if needed, to
                 * hold data strip coming from file
                 * (perhaps should set upper bound on
                 *  the size of a buffer we'll use?).
                 */
                if (bytecount > (uint)m_rawdatasize)
                {
                    m_curstrip = NOSTRIP;
                    if ((m_flags & TIFF_MYBUFFER) == 0)
                    {
                        ErrorExt(this, m_clientdata, module,
                            "{0}: Data buffer too small to hold strip {1}", m_name, strip);
                        return false;
                    }

                    ReadBufferSetup(null, roundUp(bytecount, 1024));
                }
                
                if (readRawStrip1(strip, m_rawdata, 0, bytecount, module) != bytecount)
                    return false;
                
                if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TIFF_NOBITREV) == 0)
                    ReverseBits(m_rawdata, bytecount);
            }

            return startStrip(strip);
        }

        /*
        * Read the specified tile and setup for decoding. 
        * The data buffer is expanded, as necessary, to
        * hold the tile's data.
        */
        internal bool fillTile(int tile)
        {
            const string module = "fillTile";

            if ((m_flags & TIFF_NOREADRAW) == 0)
            {
                /*
                * FIXME: butecount should have int type, but for now
                * libtiff defines int as a signed 32-bit integer and we
                * are losing ability to read arrays larger than 2^31 bytes.
                * So we are using uint instead of int here.
                */

                int bytecount = m_dir.td_stripbytecount[tile];
                if (bytecount <= 0)
                {
                    ErrorExt(this, m_clientdata, m_name,
                        "{0}: Invalid tile byte count, tile {1}", bytecount, tile);
                    return false;
                }

                /*
                 * Expand raw data buffer, if needed, to
                 * hold data tile coming from file
                 * (perhaps should set upper bound on
                 *  the size of a buffer we'll use?).
                 */
                if (bytecount > (uint)m_rawdatasize)
                {
                    m_curtile = NOTILE;
                    if ((m_flags & TIFF_MYBUFFER) == 0)
                    {
                        ErrorExt(this, m_clientdata, module,
                            "{0}: Data buffer too small to hold tile {1}", m_name, tile);
                        return false;
                    }

                    ReadBufferSetup(null, roundUp(bytecount, 1024));
                }

                if ((uint)readRawTile1(tile, m_rawdata, 0, bytecount, module) != bytecount)
                    return false;

                if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TIFF_NOBITREV) == 0)
                    ReverseBits(m_rawdata, bytecount);
            }

            return startTile(tile);
        }
    }
}
