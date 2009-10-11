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
 * Scanline-oriented Write Support
 */

using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        private bool writeCheckStrips(string module)
        {
            return ((m_flags & TIFF_BEENWRITING) != 0 || WriteCheck(0, module));
        }

        private bool writeCheckTiles(string module)
        {
            return ((m_flags & TIFF_BEENWRITING) != 0 || WriteCheck(1, module));
        }

        private bool bufferCheck()
        {
            return (((m_flags & TIFF_BUFFERSETUP) != 0 && m_rawdata != null) || WriteBufferSetup(null, (int)-1));
        }

        private int writeFile(byte[] buf, int size)
        {
            return m_stream.Write(m_clientdata, buf, size);
        }

        private bool writeOK(byte[] buf, int size)
        {
            return (writeFile(buf, size) == size);
        }

        private bool writeHeaderOK(TiffHeader header)
        {
            bool res = writeUInt16OK(header.tiff_magic);

            if (res)
                res = writeUInt16OK(header.tiff_version);

            if (res)
                res = writeUInt32OK(header.tiff_diroff);

            return res;
        }

        private bool writeDirEntryOK(TiffDirEntry[] entries, int count)
        {
            bool res = true;
            for (int i = 0; i < count; i++)
            {
                res = writeUInt16OK(entries[i].tdir_tag);

                if (res)
                    res = writeUInt16OK(entries[i].tdir_type);

                if (res)
                    res = writeUInt32OK(entries[i].tdir_count);

                if (res)
                    res = writeUInt32OK(entries[i].tdir_offset);

                if (!res)
                    break;
            }

            return res;
        }

        private bool writeUInt16OK(UInt16 value)
        {
            byte cp[2];
            cp[0] = (byte)value;
            cp[1] = (byte)(value >> 8);

            return writeOK(cp, 2);
        }

        private bool writeUInt32OK(uint value)
        {
            byte cp[4];
            cp[0] = (byte)value;
            cp[1] = (byte)(value >> 8);
            cp[2] = (byte)(value >> 16);
            cp[3] = (byte)(value >> 24);

            return writeOK(cp, 4);
        }

        private bool isUnspecified(int f)
        {
            return (fieldSet(f) && m_dir.td_imagelength == 0);
        }

        /*
        * Grow the strip data structures by delta strips.
        */
        private bool growStrips(int delta, string module)
        {
            assert(m_dir.td_planarconfig == PLANARCONFIG_CONTIG);
            uint* new_stripoffset = Tiff::Realloc(m_dir.td_stripoffset, m_dir.td_nstrips, m_dir.td_nstrips + delta);
            uint* new_stripbytecount = Tiff::Realloc(m_dir.td_stripbytecount, m_dir.td_nstrips, m_dir.td_nstrips + delta);
            if (new_stripoffset == null || new_stripbytecount == null)
            {
                delete new_stripoffset;
                delete new_stripbytecount;

                m_dir.td_nstrips = 0;
                Tiff::ErrorExt(this, m_clientdata, module, "%s: No space to expand strip arrays", m_name);
                return false;
            }

            m_dir.td_stripoffset = new_stripoffset;
            m_dir.td_stripbytecount = new_stripbytecount;
            memset(m_dir.td_stripoffset + m_dir.td_nstrips, 0, delta * sizeof(uint));
            memset(m_dir.td_stripbytecount + m_dir.td_nstrips, 0, delta * sizeof(uint));
            m_dir.td_nstrips += delta;
            return true;
        }

        /*
        * Append the data to the specified strip.
        */
        private bool appendToStrip(uint strip, byte[] data, int cc)
        {
            static const char module[] = "appendToStrip";

            if (m_dir.td_stripoffset[strip] == 0 || m_curoff == 0)
            {
                assert(m_dir.td_nstrips > 0);
                if (m_dir.td_stripbytecount[strip] != 0 && m_dir.td_stripoffset[strip] != 0 && (int)m_dir.td_stripbytecount[strip] >= cc)
                {
                    /* 
                    * There is already tile data on disk, and the new tile
                    * data we have to will fit in the same space.  The only 
                    * aspect of this that is risky is that there could be
                    * more data to append to this strip before we are done
                    * depending on how we are getting called.
                    */
                    if (!seekOK(m_dir.td_stripoffset[strip]))
                    {
                        Tiff::ErrorExt(this, m_clientdata, module, "Seek error at scanline %lu", m_row);
                        return false;
                    }
                }
                else
                {
                    /* 
                    * Seek to end of file, and set that as our location to 
                    * write this strip.
                    */
                    m_dir.td_stripoffset[strip] = seekFile(0, SEEK_END);
                }

                m_curoff = m_dir.td_stripoffset[strip];

                /*
                * We are starting a fresh strip/tile, so set the size to zero.
                */
                m_dir.td_stripbytecount[strip] = 0;
            }

            if (!writeOK(data, cc))
            {
                Tiff::ErrorExt(this, m_clientdata, module, "Write error at scanline %lu", m_row);
                return false;
            }

            m_curoff += cc;
            m_dir.td_stripbytecount[strip] += cc;
            return true;
        }

        /*
        * Internal version of FlushData that can be
        * called by ``encodestrip routines'' w/o concern
        * for infinite recursion.
        */
        internal bool flushData1()
        {
            if (m_rawcc > 0)
            {
                if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TIFF_NOBITREV) == 0)
                    ReverseBits(m_rawdata, m_rawcc);

                if (!appendToStrip(IsTiled() ? m_curtile : m_curstrip, m_rawdata, m_rawcc))
                    return false;

                m_rawcc = 0;
                m_rawcp = 0;
            }

            return true;
        }
    }
}
