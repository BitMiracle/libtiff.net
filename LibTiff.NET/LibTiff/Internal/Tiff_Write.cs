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
        private bool writeCheckStrips(string module)
        {
            return ((m_flags & TIFF_BEENWRITING) != 0 || WriteCheck(0, module));
        }

        private bool writeCheckTiles(string module)
        {
            return ((m_flags & TIFF_BEENWRITING) != 0 || WriteCheck(1, module));
        }

        private void bufferCheck()
        {
            if (!((m_flags & TIFF_BUFFERSETUP) != 0 && m_rawdata != null))
                WriteBufferSetup(null, -1);
        }

        private void writeFile(byte[] buf, int size)
        {
            m_stream.Write(m_clientdata, buf, size);
        }

        private bool writeOK(byte[] buf, int size)
        {
            try
            {
                writeFile(buf, size);
            }
            catch (Exception)
            {
                Tiff.Warning(this, "writeOK", "Failed to write {0} bytes", size);
                return false;
            }

            return true;
        }

        private bool writeHeaderOK(TiffHeader header)
        {
            bool res = writeShortOK(header.tiff_magic);

            if (res)
                res = writeShortOK(header.tiff_version);

            if (res)
                res = writeIntOK(header.tiff_diroff);

            return res;
        }

        private bool writeDirEntryOK(TiffDirEntry[] entries, int count)
        {
            bool res = true;
            for (int i = 0; i < count; i++)
            {
                res = writeShortOK((short)entries[i].tdir_tag);

                if (res)
                    res = writeShortOK((short)entries[i].tdir_type);

                if (res)
                    res = writeIntOK(entries[i].tdir_count);

                if (res)
                    res = writeIntOK(entries[i].tdir_offset);

                if (!res)
                    break;
            }

            return res;
        }

        private bool writeShortOK(short value)
        {
            byte[] cp = new byte[2];
            cp[0] = (byte)value;
            cp[1] = (byte)(value >> 8);

            return writeOK(cp, 2);
        }

        private bool writeIntOK(int value)
        {
            byte[] cp = new byte[4];
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
            Debug.Assert(m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG);
            int[] new_stripoffset = Realloc(m_dir.td_stripoffset, m_dir.td_nstrips, m_dir.td_nstrips + delta);
            int[] new_stripbytecount = Realloc(m_dir.td_stripbytecount, m_dir.td_nstrips, m_dir.td_nstrips + delta);
            m_dir.td_stripoffset = new_stripoffset;
            m_dir.td_stripbytecount = new_stripbytecount;
            Array.Clear(m_dir.td_stripoffset, m_dir.td_nstrips, delta);
            Array.Clear(m_dir.td_stripbytecount, m_dir.td_nstrips, delta);
            m_dir.td_nstrips += delta;
            return true;
        }

        /*
        * Append the data to the specified strip.
        */
        private bool appendToStrip(int strip, byte[] data, int cc)
        {
            const string module = "appendToStrip";

            if (m_dir.td_stripoffset[strip] == 0 || m_curoff == 0)
            {
                Debug.Assert(m_dir.td_nstrips > 0);
                if (m_dir.td_stripbytecount[strip] != 0 && m_dir.td_stripoffset[strip] != 0 && m_dir.td_stripbytecount[strip] >= cc)
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
                        ErrorExt(this, m_clientdata, module, "Seek error at scanline {0}", m_row);
                        return false;
                    }
                }
                else
                {
                    /* 
                    * Seek to end of file, and set that as our location to 
                    * write this strip.
                    */
                    m_dir.td_stripoffset[strip] = seekFile(0, SeekOrigin.End);
                }

                m_curoff = m_dir.td_stripoffset[strip];

                /*
                * We are starting a fresh strip/tile, so set the size to zero.
                */
                m_dir.td_stripbytecount[strip] = 0;
            }

            if (!writeOK(data, cc))
            {
                ErrorExt(this, m_clientdata, module, "Write error at scanline {0}", m_row);
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
