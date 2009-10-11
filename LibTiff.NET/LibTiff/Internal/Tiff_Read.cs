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

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private const int NOSTRIP = -1;         /* undefined state */
        private const int NOTILE = -1;          /* undefined state */

        /*
        * Default Read/Seek/Write definitions.
        */
        private int readFile(byte[] buf, int offset, int size)
        {
            return m_stream.Read(m_clientdata, buf, offset, size);
        }

        private uint seekFile(uint off, int whence)
        {
            return m_stream.Seek(m_clientdata, off, whence);
        }

        private bool closeFile()
        {
            return m_stream.Close(m_clientdata);
        }

        private uint getFileSize()
        {
            return m_stream.Size(m_clientdata);
        }

        private bool readOK(byte[] buf, int size)
        {
            return (readFile(buf, 0, size) == size);
        }

        private bool readUInt16OK(out UInt16 value)
        {
            byte[] bytes = new byte[2];
            bool res = readOK(bytes, 2);
            value = 0;
            if (res)
            {
                value = (ushort)(bytes[0] & 0xFF);
                value += (ushort)((bytes[1] & 0xFF) << 8);
            }

            return res;
        }

        private bool readUInt32OK(out uint value)
        {
            byte[] cp = new byte[4];
            bool res = readOK(cp, 4);
            value = 0;
            if (res)
            {
                value = (uint)(cp[0] & 0xFF);
                value += (uint)((cp[1] & 0xFF) << 8);
                value += (uint)((cp[2] & 0xFF) << 16);
                value += (uint)(cp[3] << 24);
            }

            return res;
        }

        private bool readDirEntryOk(TiffDirEntry[] dir, UInt16 dircount)
        {
            int entrySize = sizeof(UInt16) * 2 + sizeof(uint) * 2;
            int totalSize = entrySize * dircount;
            byte[] bytes = new byte[totalSize];
            bool res = readOK(bytes, totalSize);
            if (res)
                readDirEntry(dir, dircount, bytes, 0);

            return res;
        }

        private void readDirEntry(TiffDirEntry[] dir, UInt16 dircount, byte[] bytes, uint offset)
        {
            int pos = (int)offset;
            for (int i = 0; i < dircount; i++)
            {
                TiffDirEntry entry = dir[i];
                entry.tdir_tag = readUInt16(bytes, pos);
                pos += sizeof(UInt16);
                entry.tdir_type = readUInt16(bytes, pos);
                pos += sizeof(UInt16);
                entry.tdir_count = readUInt32(bytes, pos);
                pos += sizeof(uint);
                entry.tdir_offset = readUInt32(bytes, pos);
                pos += sizeof(uint);
            }
        }

        private bool readHeaderOk(ref TiffHeader header)
        {
            bool res = readUInt16OK(out header.tiff_magic);

            if (res)
                res = readUInt16OK(out header.tiff_version);

            if (res)
                res = readUInt32OK(out header.tiff_diroff);

            return res;
        }

        private bool seekOK(uint off)
        {
            return (seekFile(off, SEEK_SET) == off);
        }

        /*
        * Seek to a random row+sample in a file.
        */
        private bool seek(uint row, UInt16 sample)
        {
            if (row >= m_dir.td_imagelength)
            {
                /* out of range */
                ErrorExt(this, m_clientdata, m_name, "%lu: Row out of range, max %lu", row, m_dir.td_imagelength);
                return false;
            }

            uint strip;
            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name, "%lu: Sample out of range, max %lu", sample, m_dir.td_samplesperpixel);
                    return false;
                }

                strip = sample * m_dir.td_stripsperimage + row / m_dir.td_rowsperstrip;
            }
            else
                strip = row / m_dir.td_rowsperstrip;
            
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

        private int readRawStrip1(uint strip, byte[] buf, int offset, int size, string module)
        {
            Debug.Assert((m_flags & TIFF_NOREADRAW) == 0);

            if (!seekOK(m_dir.td_stripoffset[strip]))
            {
                ErrorExt(this, m_clientdata, module, "%s: Seek error at scanline %lu, strip %lu", m_name, m_row, strip);
                return -1;
            }

            int cc = readFile(buf, offset, size);
            if (cc != size)
            {
                ErrorExt(this, m_clientdata, module, "%s: Read error at scanline %lu; got %lu bytes, expected %lu", m_name, m_row, cc, size);
                return -1;
            }

            return size;
        }

        private int readRawTile1(uint tile, byte[] buf, int offset, int size, string module)
        {
            Debug.Assert((m_flags & TIFF_NOREADRAW) == 0);

            if (!seekOK(m_dir.td_stripoffset[tile]))
            {
                ErrorExt(this, m_clientdata, module, "%s: Seek error at row %ld, col %ld, tile %ld", m_name, m_row, m_col, tile);
                return -1;
            }

            int cc = readFile(buf, offset, size);
            if (cc != size)
            {
                ErrorExt(this, m_clientdata, module, "%s: Read error at row %ld, col %ld; got %lu bytes, expected %lu", m_name, m_row, m_col, cc, size);
                return -1;
            }

            return size;
        }

        /*
        * Set state to appear as if a
        * strip has just been read in.
        */
        private bool startStrip(uint strip)
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

            return m_currentCodec.tif_predecode((UInt16)(strip / m_dir.td_stripsperimage));
        }

        /*
        * Set state to appear as if a
        * tile has just been read in.
        */
        private bool startTile(uint tile)
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

            return m_currentCodec.tif_predecode((UInt16)(tile / m_dir.td_stripsperimage));
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
            UInt16[] swabee = byteArrayToUInt16(buf, 0, cc);
            SwabArrayOfShort(swabee, cc / 2);
            uint16ToByteArray(swabee, 0, cc / 2, buf, 0);
        }

        private static void swab24BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc % 3) == 0);
            SwabArrayOfTriples(buf, cc / 3);
        }

        private static void swab32BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc & 3) == 0);
            uint[] swabee = byteArrayToUInt(buf, 0, cc);
            SwabArrayOfLong(swabee, cc / 4);
            uintToByteArray(swabee, 0, cc / 4, buf, 0);
        }

        private static void swab64BitData(byte[] buf, int cc)
        {
            Debug.Assert((cc & 7) == 0);
            SwabArrayOfDouble(buf, cc / 8);
        }

        /*
        * Read the specified strip and setup for decoding. 
        * The data buffer is expanded, as necessary, to
        * hold the strip's data.
        */
        internal bool fillStrip(uint strip)
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

                uint bytecount = m_dir.td_stripbytecount[strip];
                if (bytecount <= 0)
                {
                    ErrorExt(this, m_clientdata, m_name, "%lu: Invalid strip byte count, strip %lu", bytecount, strip);
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
                        ErrorExt(this, m_clientdata, module, "%s: Data buffer too small to hold strip %lu", m_name, strip);
                        return false;
                    }
                    
                    if (!ReadBufferSetup(null, roundUp(bytecount, 1024)))
                        return false;
                }
                
                if ((uint)readRawStrip1(strip, m_rawdata, 0, bytecount, module) != bytecount)
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
        internal bool fillTile(uint tile)
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

                uint bytecount = m_dir.td_stripbytecount[tile];
                if (bytecount <= 0)
                {
                    ErrorExt(this, m_clientdata, m_name, "%lu: Invalid tile byte count, tile %lu", bytecount, tile);
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
                        ErrorExt(this, m_clientdata, module, "%s: Data buffer too small to hold tile %ld", m_name, tile);
                        return false;
                    }

                    if (!ReadBufferSetup(null, roundUp(bytecount, 1024)))
                        return false;
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
