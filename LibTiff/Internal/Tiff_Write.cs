/*
 * Scanline-oriented Write Support
 */

using System;
using System.Diagnostics;
using System.IO;

using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
#if EXPOSE_LIBTIFF
    public
#endif
 partial class Tiff
    {
        private bool writeCheckStrips(string module)
        {
            return ((m_flags & TiffFlags.BEENWRITING) == TiffFlags.BEENWRITING || WriteCheck(false, module));
        }

        private bool writeCheckTiles(string module)
        {
            return ((m_flags & TiffFlags.BEENWRITING) == TiffFlags.BEENWRITING || WriteCheck(true, module));
        }

        private void bufferCheck()
        {
            if (!((m_flags & TiffFlags.BUFFERSETUP) == TiffFlags.BUFFERSETUP && m_rawdata != null))
                WriteBufferSetup(null, -1);
        }

        private bool writeOK(byte[] buffer, int offset, int count)
        {
            try
            {
                m_stream.Write(m_clientdata, buffer, offset, count);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Tiff.Warning(this, "writeOK", "Failed to write {0} bytes", count);
                return false;
            }

            return true;
        }

        private bool writeHeaderOK(TiffHeader header)
        {
            // if we are here the cached image directory shortcut jump is invalid
            resetPenultimateDirectoryOffset();

            bool res = writeShortOK(header.tiff_magic);
            if (res)
                res = writeShortOK(header.tiff_version);
            if (header.tiff_version == TIFF_BIGTIFF_VERSION)
            {
                if (res)
                    res = writeShortOK(header.tiff_offsize);
                if (res)
                    res = writeShortOK(header.tiff_fill);
                if (res)
                    res = writelongOK((long)header.tiff_diroff);
            }
            else
            {
                if (res)
                    res = writeIntOK((int)header.tiff_diroff);
              if (res)
                res = writelongOK(0);
            }
            return res;
        }

        private bool writeDirEntryOK(TiffDirEntry[] entries, long count, bool isBigTiff)
        {
            bool res = true;

            for (long i = 0; i < count; i++)
            {
                res = writeShortOK((short)entries[i].tdir_tag);
                if (res)
                    res = writeShortOK((short)entries[i].tdir_type);
                if (isBigTiff)
                {
                    if (res)
                        res = writelongOK(entries[i].tdir_count);

                    if (res)
                        res = writelongOK((long)entries[i].tdir_offset);
                }
                else
                {
                    if (res)
                        res = writeIntOK(entries[i].tdir_count);

                    if (res)
                        res = writeIntOK((int)entries[i].tdir_offset);
                }
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

            return writeOK(cp, 0, 2);
        }

        private bool writeDirCountOK(long value, bool isBigTiff)
        {
            if (isBigTiff)
            {
                return writelongOK(value);
            }
            else
            {
                return writeShortOK((short)value);
            }
        }

        private bool writeDirOffOK(long value, bool isBigTiff)
        {
            if (isBigTiff)
            {
                return writelongOK(value);
            }
            else
            {
                return writeIntOK((int)value);
            }
        }

        private bool writeIntOK(int value)
        {
            byte[] cp = new byte[4];
            cp[0] = (byte)value;
            cp[1] = (byte)(value >> 8);
            cp[2] = (byte)(value >> 16);
            cp[3] = (byte)(value >> 24);

            return writeOK(cp, 0, 4);
        }

        private bool writelongOK(long value)
        {
            byte[] cp = new byte[8];
            cp[0] = (byte)value;
            cp[1] = (byte)(value >> 8);
            cp[2] = (byte)(value >> 16);
            cp[3] = (byte)(value >> 24);
            cp[4] = (byte)(value >> 32);
            cp[5] = (byte)(value >> 40);
            cp[6] = (byte)(value >> 48);
            cp[7] = (byte)(value >> 56);

            return writeOK(cp, 0, 8);
        }

        private bool isUnspecified(int f)
        {
            return (fieldSet(f) && m_dir.td_imagelength == 0);
        }

        /*
        * Grow the strip data structures by delta strips.
        */
        private bool growStrips(int delta)
        {
            Debug.Assert(m_dir.td_planarconfig == PlanarConfig.CONTIG);
            ulong[] new_stripoffset = Realloc(m_dir.td_stripoffset, m_dir.td_nstrips, m_dir.td_nstrips + delta);
            ulong[] new_stripbytecount = Realloc(m_dir.td_stripbytecount, m_dir.td_nstrips, m_dir.td_nstrips + delta);
            m_dir.td_stripoffset = new_stripoffset;
            m_dir.td_stripbytecount = new_stripbytecount;
            Array.Clear(m_dir.td_stripoffset, m_dir.td_nstrips, delta);
            Array.Clear(m_dir.td_stripbytecount, m_dir.td_nstrips, delta);
            m_dir.td_nstrips += delta;
            return true;
        }

        /// <summary>
        /// Appends the data to the specified strip.
        /// </summary>
        private bool appendToStrip(int strip, byte[] buffer, int offset, long count)
        {
            const string module = "appendToStrip";

            if (m_dir.td_stripoffset[strip] == 0 || m_curoff == 0)
            {
                Debug.Assert(m_dir.td_nstrips > 0);

                if (m_dir.td_stripbytecount[strip] != 0 &&
                    m_dir.td_stripoffset[strip] != 0 &&
                    m_dir.td_stripbytecount[strip] >= (ulong)count)
                {
                    // There is already tile data on disk, and the new tile 
                    // data we have to will fit in the same space. The only
                    // aspect of this that is risky is that there could be
                    // more data to append to this strip before we are done
                    // depending on how we are getting called.
                    if (!seekOK((long)m_dir.td_stripoffset[strip]))
                    {
                        ErrorExt(this, m_clientdata, module, "Seek error at scanline {0}", m_row);
                        return false;
                    }
                }
                else
                {
                    // Seek to end of file, and set that as our location
                    // to write this strip.
                    m_dir.td_stripoffset[strip] = (ulong)seekFile(0, SeekOrigin.End);
                }

                m_curoff = m_dir.td_stripoffset[strip];

                // We are starting a fresh strip/tile, so set the size to zero.
                m_dir.td_stripbytecount[strip] = 0;
            }

            if (!writeOK(buffer, offset, (int)count))
            {
                ErrorExt(this, m_clientdata, module, "Write error at scanline {0}", m_row);
                return false;
            }

            m_curoff += (ulong)count;
            m_dir.td_stripbytecount[strip] += (ulong)count;
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
                if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TiffFlags.NOBITREV) != TiffFlags.NOBITREV)
                    ReverseBits(m_rawdata, m_rawcc);

                if (!appendToStrip(IsTiled() ? m_curtile : m_curstrip, m_rawdata, 0, m_rawcc))
                    return false;

                m_rawcc = 0;
                m_rawcp = 0;
            }

            return true;
        }
    }
}
