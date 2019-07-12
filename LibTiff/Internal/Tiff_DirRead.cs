/*
 * Directory Read Support Routines.
 */

using System;
using System.Diagnostics;

using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        private long extractData(TiffDirEntry dir)
        {
            int type = (int)dir.tdir_type;
            if (m_header.tiff_magic == TIFF_BIGENDIAN)
                return (long)((dir.tdir_offset >> m_typeshift[type]) & m_typemask[type]);

            return (long)(dir.tdir_offset & m_typemask[type]);
        }

        private bool byteCountLooksBad(TiffDirectory td)
        {
            /* 
             * Assume we have wrong StripByteCount value (in case of single strip) in
             * following cases:
             *   - it is equal to zero along with StripOffset;
             *   - it is larger than file itself (in case of uncompressed image);
             *   - it is smaller than the size of the bytes per row multiplied on the
             *     number of rows.  The last case should not be checked in the case of
             *     writing new image, because we may do not know the exact strip size
             *     until the whole image will be written and directory dumped out.
             */
            return
            (
                (td.td_stripbytecount[0] == 0 && td.td_stripoffset[0] != 0) ||
                (td.td_compression == Compression.NONE && td.td_stripbytecount[0] > (ulong)getFileSize() - td.td_stripoffset[0]) ||
                (m_mode == O_RDONLY && td.td_compression == Compression.NONE && td.td_stripbytecount[0] < (ulong)(ScanlineSize() * td.td_imagelength))
            );
        }

        private static int howMany8(int x)
        {
            return ((x & 0x07) != 0 ? (x >> 3) + 1 : x >> 3);
        }

        private bool estimateStripByteCounts(TiffDirEntry[] dir, long dircount)
        {
            const string module = "estimateStripByteCounts";

            m_dir.td_stripbytecount = new ulong[m_dir.td_nstrips];

            if (m_dir.td_compression != Compression.NONE)
            {
                long filesize = getFileSize();
                long space = m_header.tiff_version == TIFF_BIGTIFF_VERSION
                  ? TiffHeader.SizeInBytes(true) + sizeof(long) +
                    (dircount * TiffDirEntry.SizeInBytes(true)) + sizeof(long)
                  : TiffHeader.SizeInBytes(false) + sizeof(short) +
                    (dircount * TiffDirEntry.SizeInBytes(false)) + sizeof(int);
                // calculate amount of space used by indirect values
                for (short n = 0; n < dircount; n++)
                {
                    int cc = DataWidth((TiffType)dir[n].tdir_type);
                    if (cc == 0)
                    {
                        ErrorExt(this, m_clientdata, module,
                            "{0}: Cannot determine size of unknown tag type {1}",
                            m_name, dir[n].tdir_type);
                        return false;
                    }

                    cc = cc * dir[n].tdir_count;
                    if (cc > sizeof(int))
                        space += cc;
                }

                space = filesize - space;
                if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                    space /= m_dir.td_samplesperpixel;

                int strip = 0;
                for (; strip < m_dir.td_nstrips; strip++)
                    m_dir.td_stripbytecount[strip] = (uint)space;

                // This gross hack handles the case were the offset to the last
                // strip is past the place where we think the strip should begin.
                // Since a strip of data must be contiguous, it's safe to assume
                // that we've overestimated the amount of data in the strip and
                // trim this number back accordingly.
                strip--;
                if ((m_dir.td_stripoffset[strip] + m_dir.td_stripbytecount[strip]) > (ulong)filesize)
                    m_dir.td_stripbytecount[strip] = ((ulong)filesize - m_dir.td_stripoffset[strip]);
            }
            else if (IsTiled())
            {
                int bytespertile = TileSize();
                for (int strip = 0; strip < m_dir.td_nstrips; strip++)
                    m_dir.td_stripbytecount[strip] = (uint)bytespertile;
            }
            else
            {
                int rowbytes = ScanlineSize();
                int rowsperstrip = m_dir.td_imagelength / m_dir.td_stripsperimage;
                for (int strip = 0; strip < m_dir.td_nstrips; strip++)
                    m_dir.td_stripbytecount[strip] = (uint)(rowbytes * rowsperstrip);
            }

            setFieldBit(FieldBit.StripByteCounts);
            if (!fieldSet(FieldBit.RowsPerStrip))
                m_dir.td_rowsperstrip = m_dir.td_imagelength;

            return true;
        }

        private void missingRequired(string tagname)
        {
            const string module = "missingRequired";
            ErrorExt(this, m_clientdata, module,
                "{0}: TIFF directory is missing required \"{1}\" field",
                m_name, tagname);
        }

        private int fetchFailed(TiffDirEntry dir)
        {
            string format = "Error fetching data for field \"{0}\"";

            TiffFieldInfo fi = FieldWithTag(dir.tdir_tag);
            if (fi.Bit == FieldBit.Custom)
            {
                // According to the TIFF standard, private (custom) tags can be safely
                // ignored when reading an image from a TIFF file.
                WarningExt(this, m_clientdata, m_name, format, fi.Name);
            }
            else
            {
                ErrorExt(this, m_clientdata, m_name, format, fi.Name);
            }

            return 0;
        }

        private static long readDirectoryFind(TiffDirEntry[] dir, ulong dircount, TiffTag tagid)
        {
            for (ulong n = 0; n < dircount; n++)
            {
                if (dir[n].tdir_tag == tagid)
                    return (long)n;
            }

            return -1;
        }

        /// <summary>
        /// Checks the directory offset against the list of already seen directory
        /// offsets.
        /// </summary>
        /// <remarks> This is a trick to prevent IFD looping. The one can
        /// create TIFF file with looped directory pointers. We will maintain a
        /// list of already seen directories and check every IFD offset against
        /// that list.</remarks>
        private bool checkDirOffset(ulong diroff)
        {
            if (diroff == 0)
            {
                // no more directories
                return false;
            }

            for (short n = 0; n < m_dirnumber && m_dirlist != null; n++)
            {
                if (m_dirlist[n] == diroff)
                    return false;
            }

            m_dirnumber++;

            if (m_dirnumber > m_dirlistsize)
            {
                // XXX: Reduce memory allocation granularity of the dirlist array.
                ulong[] new_dirlist = Realloc(m_dirlist, m_dirnumber - 1, 2 * m_dirnumber);
                m_dirlistsize = 2 * m_dirnumber;
                m_dirlist = new_dirlist;
            }

            m_dirlist[m_dirnumber - 1] = diroff;
            return true;
        }

        /// <summary>
        /// Reads IFD structure from the specified offset.
        /// </summary>
        /// <returns>The number of fields in the directory or 0 if failed.</returns>
        private ulong fetchDirectory(ulong diroff, out TiffDirEntry[] pdir, out ulong nextdiroff)
        {
            const string module = "fetchDirectory";

            m_diroff = diroff;
            nextdiroff = 0;

            ulong dircount;
            TiffDirEntry[] dir = null;
            pdir = null;

            if (!seekOK((long)m_diroff))
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Seek error accessing TIFF directory", m_name);
                return 0;
            }

            if (!readDirCountOK(out dircount, m_header.tiff_version == TIFF_BIGTIFF_VERSION))
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Can not read TIFF directory count", m_name);
                return 0;
            }

            if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                SwabBigTiffValue(ref dircount, m_header.tiff_version == TIFF_BIGTIFF_VERSION, true);

            dir = new TiffDirEntry[dircount];
            if (!readDirEntryOk(dir, dircount, (m_header.tiff_version == TIFF_BIGTIFF_VERSION)))
            {
                ErrorExt(this, m_clientdata, module, "{0}: Can not read TIFF directory", m_name);
                return 0;
            }
            ulong temp;
            // Read offset to next directory for sequential scans.
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION)
            {
                readUlongOK(out temp);
                nextdiroff = temp;
            }
            else
            {
                int tempInt = 0;
                readIntOK(out tempInt);
                nextdiroff = (ulong)tempInt;
            }

            if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
            {
                temp = nextdiroff;
                SwabBigTiffValue(ref temp, m_header.tiff_version == TIFF_BIGTIFF_VERSION, false);
                nextdiroff = (ulong)temp;
            }

            pdir = dir;
            return dircount;
        }

        /*
        * Fetch and set the SubjectDistance EXIF tag.
        */
        private bool fetchSubjectDistance(TiffDirEntry dir)
        {
            if (dir.tdir_count != 1 || dir.tdir_type != TiffType.RATIONAL)
            {
                Tiff.WarningExt(this, m_clientdata, m_name,
                    "incorrect count or type for SubjectDistance, tag ignored");

                return false;
            }

            bool ok = false;

            byte[] b = new byte[2 * sizeof(int)];
            int read = fetchData(dir, b);
            if (read != 0)
            {
                int[] l = new int[2];
                l[0] = readInt(b, 0);
                l[1] = readInt(b, sizeof(int));

                float v;
                if (cvtRational(dir, l[0], l[1], out v))
                {
                    /*
                    * XXX: Numerator -1 means that we have infinite
                    * distance. Indicate that with a negative floating point
                    * SubjectDistance value.
                    */
                    ok = SetField(dir.tdir_tag, (l[0] != -1) ? v : -v);
                }
            }

            return ok;
        }

        /*
        * Check the count field of a directory
        * entry against a known value.  The caller
        * is expected to skip/ignore the tag if
        * there is a mismatch.
        */
        private bool checkDirCount(TiffDirEntry dir, int count)
        {
            if (count > dir.tdir_count)
            {
                WarningExt(this, m_clientdata, m_name,
                    "incorrect count for field \"{0}\" ({1}, expecting {2}); tag ignored",
                    FieldWithTag(dir.tdir_tag).Name, dir.tdir_count, count);
                return false;
            }
            else if (count < dir.tdir_count)
            {
                WarningExt(this, m_clientdata, m_name,
                    "incorrect count for field \"{0}\" ({1}, expecting {2}); tag trimmed",
                    FieldWithTag(dir.tdir_tag).Name, dir.tdir_count, count);
                dir.tdir_count = count;
                return true;
            }

            return true;
        }

        /// <summary>
        /// Fetches a contiguous directory item.
        /// </summary>
        private int fetchData(TiffDirEntry dir, byte[] buffer)
        {
            int width = DataWidth(dir.tdir_type);
            int count = dir.tdir_count * width;

            // Check for overflow.
            if (dir.tdir_count == 0 || width == 0 || (count / width) != dir.tdir_count)
                fetchFailed(dir);

            if (!seekOK((long)dir.tdir_offset))
                fetchFailed(dir);

            if (!readOK(buffer, count))
                fetchFailed(dir);

            if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
            {
                switch (dir.tdir_type)
                {
                    case TiffType.SHORT:
                    case TiffType.SSHORT:
                        short[] s = ByteArrayToShorts(buffer, 0, count);
                        SwabArrayOfShort(s, dir.tdir_count);
                        ShortsToByteArray(s, 0, dir.tdir_count, buffer, 0);
                        break;

                    case TiffType.LONG:
                    case TiffType.SLONG:
                    case TiffType.FLOAT:
                    case TiffType.IFD:
                        int[] l = ByteArrayToInts(buffer, 0, count);
                        SwabArrayOfLong(l, dir.tdir_count);
                        IntsToByteArray(l, 0, dir.tdir_count, buffer, 0);
                        break;
                    case TiffType.LONG8:
                    case TiffType.SLONG8:
                    case TiffType.IFD8:
                        long[] m = ByteArrayToLong8(buffer, 0, count);
                        SwabArrayOfLong8(m, 2 * dir.tdir_count);
                        Long8ToByteArray(m, 0, 2 * dir.tdir_count, buffer, 0);
                        break;

                    case TiffType.RATIONAL:
                    case TiffType.SRATIONAL:
                        int[] r = ByteArrayToInts(buffer, 0, count);
                        SwabArrayOfLong(r, 2 * dir.tdir_count);
                        IntsToByteArray(r, 0, 2 * dir.tdir_count, buffer, 0);
                        break;

                    case TiffType.DOUBLE:
                        swab64BitData(buffer, 0, count);
                        break;
                }
            }

            return count;
        }

        /// <summary>
        /// Fetches an ASCII item from the file.
        /// </summary>
        private int fetchString(TiffDirEntry dir, out string cp)
        {
            byte[] bytes = null;

            if (m_header.tiff_version != TIFF_BIGTIFF_VERSION && dir.tdir_count <= 4)
            {
                int l = (int)dir.tdir_offset;
                if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                    SwabLong(ref l);

                bytes = new byte[sizeof(int)];
                writeInt(l, bytes, 0);
                cp = Latin1Encoding.GetString(bytes, 0, dir.tdir_count);
                return 1;
            }
            else if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count <= 8)
            {
                ulong l = dir.tdir_offset;
                if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                    SwabLong8(ref l);

                bytes = new byte[sizeof(ulong)];
                writeULong(l, bytes, 0);
                cp = Latin1Encoding.GetString(bytes, 0, dir.tdir_count);
                return 1;
            }

            bytes = new byte[dir.tdir_count];
            int res = fetchData(dir, bytes);
            cp = Latin1Encoding.GetString(bytes, 0, dir.tdir_count);
            return res;
        }

        // Convert integer numerator+denominator to float.
        private bool cvtRational(TiffDirEntry dir, int num, int denom, out float rv)
        {
            if (denom == 0)
            {
                ErrorExt(this, m_clientdata, m_name,
                    "{0}: Rational with zero denominator (num = {1})",
                    FieldWithTag(dir.tdir_tag).Name, num);
                rv = float.NaN;
                return false;
            }
            else
            {
                rv = ((float)num / (float)denom);
                return true;
            }
        }

        // Convert unsigned integer numerator+denominator to float.
        private bool cvtRational(TiffDirEntry dir, uint num, uint denom, out float rv)
        {
            if (denom == 0)
            {
                ErrorExt(this, m_clientdata, m_name,
                    "{0}: Rational with zero denominator (num = {1})",
                    FieldWithTag(dir.tdir_tag).Name, num);

                rv = float.NaN;
                return false;
            }
            else
            {
                rv = ((float)num / (float)denom);
                return true;
            }
        }

        /*
        * Fetch a rational item from the file
        * at offset off and return the value
        * as a floating point number.
        */
        private float fetchRational(TiffDirEntry dir)
        {
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION)
            {
                uint[] l = new uint[2];
                int count = dir.tdir_count;
                dir.tdir_count = 2;
                if (fetchULongArray(dir, l))
                {
                    dir.tdir_count = count;
                    float v;
                    bool res = cvtRational(dir, l[0], l[1], out v);
                    if (res)
                    {
                        return v;
                    }
                }
            }
            else
            {
                byte[] bytes = new byte[sizeof(int) * 2];
                int read = fetchData(dir, bytes);
                if (read != 0)
                {
                    if (dir.tdir_type == TiffType.SRATIONAL)
                    {
                        int[] l = new int[2];
                        l[0] = readInt(bytes, 0);
                        l[1] = readInt(bytes, sizeof(int));

                        float v;
                        bool res = cvtRational(dir, l[0], l[1], out v);
                        if (res)
                            return v;
                    }
                    else
                    {
                        uint[] l = new uint[2];
                        l[0] = BitConverter.ToUInt32(bytes, 0);
                        l[1] = BitConverter.ToUInt32(bytes, sizeof(uint));

                        float v;
                        bool res = cvtRational(dir, l[0], l[1], out v);
                        if (res)
                            return v;
                    }
                }
            }

            return 1.0f;
        }

        /// <summary>
        /// Fetch a single floating point value from the offset field and
        /// return it as a native float.
        /// </summary>
        private float fetchFloat(TiffDirEntry dir)
        {
            int l = (int)extractData(dir);
            return BitConverter.ToSingle(BitConverter.GetBytes(l), 0);
        }

        /// <summary>
        /// Fetches an array of BYTE or SBYTE values.
        /// </summary>
        private bool fetchByteArray(TiffDirEntry dir, byte[] v)
        {
            if (m_header.tiff_version != TIFF_BIGTIFF_VERSION && dir.tdir_count <= 4)
            {
                // Extract data from offset field.
                int count = dir.tdir_count;

                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 4)
                        v[3] = (byte)(dir.tdir_offset & 0xff);

                    if (count >= 3)
                        v[2] = (byte)((dir.tdir_offset >> 8) & 0xff);

                    if (count >= 2)
                        v[1] = (byte)((dir.tdir_offset >> 16) & 0xff);

                    if (count >= 1)
                        v[0] = (byte)(dir.tdir_offset >> 24);
                }
                else
                {
                    if (count == 4)
                        v[3] = (byte)(dir.tdir_offset >> 24);

                    if (count >= 3)
                        v[2] = (byte)((dir.tdir_offset >> 16) & 0xff);

                    if (count >= 2)
                        v[1] = (byte)((dir.tdir_offset >> 8) & 0xff);

                    if (count >= 1)
                        v[0] = (byte)(dir.tdir_offset & 0xff);
                }

                return true;
            }
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count <= 8)
            {
                // Extract data from offset field.
                int count = dir.tdir_count;

                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 8)
                        v[7] = (byte)(dir.tdir_offset & 0xff);

                    if (count >= 7)
                        v[6] = (byte)((dir.tdir_offset >> 8) & 0xff);

                    if (count >= 6)
                        v[5] = (byte)((dir.tdir_offset >> 16) & 0xff);

                    if (count >= 5)
                        v[4] = (byte)((dir.tdir_offset >> 24) & 0xff);

                    if (count >= 4)
                        v[3] = (byte)((dir.tdir_offset >> 32) & 0xff);

                    if (count >= 3)
                        v[2] = (byte)((dir.tdir_offset >> 40) & 0xff);

                    if (count >= 2)
                        v[1] = (byte)((dir.tdir_offset >> 48) & 0xff);

                    if (count >= 1)
                        v[0] = (byte)(dir.tdir_offset >> 56);
                }
                else
                {
                    if (count == 8)
                        v[7] = (byte)(dir.tdir_offset >> 56);

                    if (count >= 7)
                        v[6] = (byte)((dir.tdir_offset >> 48) & 0xff);

                    if (count >= 6)
                        v[5] = (byte)((dir.tdir_offset >> 40) & 0xff);

                    if (count >= 5)
                        v[4] = (byte)((dir.tdir_offset >> 32) & 0xff);

                    if (count >= 4)
                        v[3] = (byte)((dir.tdir_offset >> 24) & 0xff);

                    if (count >= 3)
                        v[2] = (byte)((dir.tdir_offset >> 16) & 0xff);

                    if (count >= 2)
                        v[1] = (byte)((dir.tdir_offset >> 8) & 0xff);

                    if (count >= 1)
                        v[0] = (byte)(dir.tdir_offset & 0xff);
                }

                return true;
            }

            return (fetchData(dir, v) != 0);
        }

        /// <summary>
        /// Fetch an array of SHORT or SSHORT values.
        /// </summary>
        private bool fetchShortArray(TiffDirEntry dir, short[] v)
        {
            if (m_header.tiff_version != TIFF_BIGTIFF_VERSION && dir.tdir_count <= 2)
            {
                int count = dir.tdir_count;

                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 2)
                        v[1] = (short)(dir.tdir_offset & 0xffff);

                    if (count >= 1)
                        v[0] = (short)(dir.tdir_offset >> 16);
                }
                else
                {
                    if (count == 2)
                        v[1] = (short)(dir.tdir_offset >> 16);

                    if (count >= 1)
                        v[0] = (short)(dir.tdir_offset & 0xffff);
                }

                return true;
            }
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count <= 4)
            {
                int count = dir.tdir_count;

                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 4)
                        v[3] = (short)(dir.tdir_offset & 0xffff);

                    if (count >= 3)
                        v[2] = (short)(dir.tdir_offset >> 48);

                    if (count >= 2)
                        v[1] = (short)(dir.tdir_offset >> 32);

                    if (count >= 1)
                        v[0] = (short)(dir.tdir_offset >> 16);
                }
                else
                {
                    if (count == 4)
                        v[3] = (short)(dir.tdir_offset >> 48);

                    if (count >= 3)
                        v[2] = (short)(dir.tdir_offset >> 32);

                    if (count >= 2)
                        v[1] = (short)(dir.tdir_offset >> 16);

                    if (count >= 1)
                        v[0] = (short)(dir.tdir_offset & 0xffff);
                }

                return true;
            }

            int cc = dir.tdir_count * sizeof(short);
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
                Buffer.BlockCopy(b, 0, v, 0, b.Length);

            return (read != 0);
        }

        /*
        * Fetch a pair of SHORT or BYTE values. Some tags may have either BYTE
        * or SHORT type and this function works with both ones.
        */
        private bool fetchShortPair(TiffDirEntry dir)
        {
            /*
            * Prevent overflowing arrays below by performing a sanity
            * check on tdir_count, this should never be greater than two.
            */
            if (dir.tdir_count > 2)
            {
                WarningExt(this, m_clientdata, m_name,
                    "unexpected count for field \"{0}\", {1}, expected 2; ignored",
                    FieldWithTag(dir.tdir_tag).Name, dir.tdir_count);
                return false;
            }

            switch (dir.tdir_type)
            {
                case TiffType.BYTE:
                case TiffType.SBYTE:
                    byte[] bytes = new byte[4];
                    return fetchByteArray(dir, bytes) && SetField(dir.tdir_tag, bytes[0], bytes[1]);

                case TiffType.SHORT:
                case TiffType.SSHORT:
                    short[] shorts = new short[2];
                    return fetchShortArray(dir, shorts) && SetField(dir.tdir_tag, shorts[0], shorts[1]);
            }

            return false;
        }

        /// <summary>
        /// Fetches an array of ULONG values.
        /// </summary>
        private bool fetchULongArray(TiffDirEntry dir, uint[] v)
        {
            if (m_header.tiff_version != TIFF_BIGTIFF_VERSION && dir.tdir_count == 1)
            {
                v[0] = (uint)dir.tdir_offset;
                return true;
            }

            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count <= 2)
            {
                int count = dir.tdir_count;
                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 2)
                        v[1] = (uint)(dir.tdir_offset & 0xffffffff);

                    if (count >= 1)
                        v[0] = (uint)(dir.tdir_offset >> 32);
                }
                else
                {
                    if (count == 2)
                        v[1] = (uint)(dir.tdir_offset >> 32);

                    if (count >= 1)
                        v[0] = (uint)(dir.tdir_offset & 0xffffffff);
                }
                return true;
            }

            int cc = dir.tdir_count * sizeof(int);
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
                Buffer.BlockCopy(b, 0, v, 0, b.Length);

            return (read != 0);
        }

        /// <summary>
        /// Fetches an array of LONG or SLONG values.
        /// </summary>
        private bool fetchLongArray(TiffDirEntry dir, int[] v)
        {
            if (m_header.tiff_version != TIFF_BIGTIFF_VERSION && dir.tdir_count == 1)
            {
                v[0] = (int)dir.tdir_offset;
                return true;
            }
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count <= 2)
            {
                int count = dir.tdir_count;
                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 2)
                        v[1] = (int)(dir.tdir_offset & 0xffffffff);

                    if (count >= 1)
                        v[0] = (int)(dir.tdir_offset >> 32);
                }
                else
                {
                    if (count == 2)
                        v[1] = (int)(dir.tdir_offset >> 32);

                    if (count >= 1)
                        v[0] = (int)(dir.tdir_offset & 0xffffffff);
                }
                return true;
            }

            int cc = dir.tdir_count * sizeof(int);
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
                Buffer.BlockCopy(b, 0, v, 0, b.Length);

            return (read != 0);
        }

        /// <summary>
        /// Fetches an array of LONG or SLONG values.
        /// </summary>
        private bool fetchLong8Array(TiffDirEntry dir, long[] v)
        {
            if (dir.tdir_count == 1)
            {
                v[0] = (long)dir.tdir_offset;
                return true;
            }

            int cc = dir.tdir_count * sizeof(long);
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
                Buffer.BlockCopy(b, 0, v, 0, b.Length);

            return (read != 0);
        }

        /// <summary>
        /// Fetch an array of RATIONAL or SRATIONAL values.
        /// </summary>
        private bool fetchRationalArray(TiffDirEntry dir, float[] v)
        {
            Debug.Assert(sizeof(float) == sizeof(int));

            bool ok = false;
            byte[] l = new byte[dir.tdir_count * DataWidth(dir.tdir_type)];
            if (fetchData(dir, l) != 0)
            {
                int offset = 0;
                int[] pair = new int[2];
                for (int i = 0; i < dir.tdir_count; i++)
                {
                    pair[0] = readInt(l, offset);
                    offset += sizeof(int);
                    pair[1] = readInt(l, offset);
                    offset += sizeof(int);

                    if (dir.tdir_type == TiffType.SRATIONAL)
                        ok = cvtRational(dir, pair[0], pair[1], out v[i]);
                    else
                        ok = cvtRational(dir, (uint)pair[0], (uint)pair[1], out v[i]);

                    if (!ok)
                        break;
                }
            }

            return ok;
        }

        /// <summary>
        /// Fetches an array of FLOAT values.
        /// </summary>
        private bool fetchFloatArray(TiffDirEntry dir, float[] v)
        {
            if (m_header.tiff_version != TIFF_BIGTIFF_VERSION && dir.tdir_count == 1)
            {
                v[0] = BitConverter.ToSingle(BitConverter.GetBytes(dir.tdir_offset), 0);
                return true;
            }
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count <= 2)
            {
                int count = dir.tdir_count;
                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 2)
                        v[1] = BitConverter.ToSingle(BitConverter.GetBytes(dir.tdir_offset), 4);
                    if (count >= 1)
                        v[0] = BitConverter.ToSingle(BitConverter.GetBytes(dir.tdir_offset), 0);
                }
                else
                {
                    if (count == 2)
                        v[1] = BitConverter.ToSingle(BitConverter.GetBytes(dir.tdir_offset), 0);
                    if (count >= 1)
                        v[0] = BitConverter.ToSingle(BitConverter.GetBytes(dir.tdir_offset), 4);
                }
                return true;
            }


            int w = DataWidth(dir.tdir_type);
            int cc = dir.tdir_count * w;
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
            {
                int byteOffset = 0;
                for (int i = 0; i < read / 4; i++)
                {
                    v[i] = BitConverter.ToSingle(b, byteOffset);
                    byteOffset += 4;
                }
            }

            return (read != 0);
        }

        /// <summary>
        /// Fetches an array of DOUBLE values.
        /// </summary>
        private bool fetchDoubleArray(TiffDirEntry dir, double[] v)
        {
            if (m_header.tiff_version == TIFF_BIGTIFF_VERSION && dir.tdir_count == 1)
            {
                v[0] = dir.tdir_offset;
                return true;
            }
            int w = DataWidth(dir.tdir_type);
            int cc = dir.tdir_count * w;
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
            {
                int byteOffset = 0;
                for (int i = 0; i < read / 8; i++)
                {
                    v[i] = BitConverter.ToDouble(b, byteOffset);
                    byteOffset += 8;
                }
            }

            return (read != 0);
        }

        /// <summary>
        /// Fetches an array of ANY values.
        /// </summary>
        /// <remarks>The actual values are returned as doubles which should be
        /// able hold all the types. Note in particular that we assume that the
        /// double return value vector is large enough to read in any
        /// fundamental type. We use that vector as a buffer to read in the base
        /// type vector and then convert it in place to double (from end to
        /// front of course).</remarks>
        private bool fetchAnyArray(TiffDirEntry dir, double[] v)
        {
            int i = 0;
            bool res = false;
            switch (dir.tdir_type)
            {
                case TiffType.BYTE:
                case TiffType.SBYTE:
                    byte[] b = new byte[dir.tdir_count];
                    res = fetchByteArray(dir, b);
                    if (res)
                    {
                        for (i = dir.tdir_count - 1; i >= 0; i--)
                            v[i] = b[i];
                    }

                    if (!res)
                        return false;

                    break;
                case TiffType.SHORT:
                case TiffType.SSHORT:
                    short[] u = new short[dir.tdir_count];
                    res = fetchShortArray(dir, u);
                    if (res)
                    {
                        for (i = dir.tdir_count - 1; i >= 0; i--)
                            v[i] = u[i];
                    }

                    if (!res)
                        return false;

                    break;
                case TiffType.LONG:
                case TiffType.SLONG:
                    int[] l = new int[dir.tdir_count];
                    res = fetchLongArray(dir, l);
                    if (res)
                    {
                        for (i = dir.tdir_count - 1; i >= 0; i--)
                            v[i] = l[i];
                    }

                    if (!res)
                        return false;

                    break;
                case TiffType.RATIONAL:
                case TiffType.SRATIONAL:
                    float[] r = new float[dir.tdir_count];
                    res = fetchRationalArray(dir, r);
                    if (res)
                    {
                        for (i = dir.tdir_count - 1; i >= 0; i--)
                            v[i] = r[i];
                    }

                    if (!res)
                        return false;

                    break;
                case TiffType.FLOAT:
                    float[] f = new float[dir.tdir_count];
                    res = fetchFloatArray(dir, f);
                    if (res)
                    {
                        for (i = dir.tdir_count - 1; i >= 0; i--)
                            v[i] = f[i];
                    }

                    if (!res)
                        return false;

                    break;
                case TiffType.DOUBLE:
                    return fetchDoubleArray(dir, v);
                default:
                    // NOTYPE
                    // ASCII
                    // UNDEFINED
                    ErrorExt(this, m_clientdata, m_name,
                        "cannot read TIFF_ANY type {0} for field \"{1}\"",
                        dir.tdir_type, FieldWithTag(dir.tdir_tag).Name);
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Fetches a tag that is not handled by special case code.
        /// </summary>
        private bool fetchNormalTag(TiffDirEntry dir)
        {
            bool ok = false;
            TiffFieldInfo fip = FieldWithTag(dir.tdir_tag);

            if (dir.tdir_count > 1)
            {
                switch (dir.tdir_type)
                {
                    case TiffType.BYTE:
                    case TiffType.SBYTE:
                        byte[] bytes = new byte[dir.tdir_count];
                        ok = fetchByteArray(dir, bytes);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, bytes);
                            else
                                ok = SetField(dir.tdir_tag, bytes);
                        }
                        break;

                    case TiffType.SHORT:
                    case TiffType.SSHORT:
                        short[] shorts = new short[dir.tdir_count];
                        ok = fetchShortArray(dir, shorts);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, shorts);
                            else
                                ok = SetField(dir.tdir_tag, shorts);
                        }
                        break;

                    case TiffType.LONG:
                    case TiffType.SLONG:
                        int[] ints = new int[dir.tdir_count];
                        ok = fetchLongArray(dir, ints);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, ints);
                            else
                                ok = SetField(dir.tdir_tag, ints);
                        }
                        break;

                    case TiffType.LONG8:
                    case TiffType.SLONG8:
                    case TiffType.IFD8:
                        long[] longs = new long[dir.tdir_count];
                        ok = fetchLong8Array(dir, longs);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, longs);
                            else
                                ok = SetField(dir.tdir_tag, longs);
                        }
                        break;

                    case TiffType.RATIONAL:
                    case TiffType.SRATIONAL:
                        float[] rs = new float[dir.tdir_count];
                        ok = fetchRationalArray(dir, rs);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, rs);
                            else
                                ok = SetField(dir.tdir_tag, rs);
                        }
                        break;

                    case TiffType.FLOAT:
                        float[] fs = new float[dir.tdir_count];
                        ok = fetchFloatArray(dir, fs);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, fs);
                            else
                                ok = SetField(dir.tdir_tag, fs);
                        }
                        break;

                    case TiffType.DOUBLE:
                        double[] ds = new double[dir.tdir_count];
                        ok = fetchDoubleArray(dir, ds);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, ds);
                            else
                                ok = SetField(dir.tdir_tag, ds);
                        }
                        break;

                    case TiffType.ASCII:
                    case TiffType.UNDEFINED:
                        // bit of a cheat...

                        // Some vendors write strings w/o the trailing null
                        // byte, so always append one just in case.
                        string cp;
                        ok = fetchString(dir, out cp) != 0;
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                            else
                                ok = SetField(dir.tdir_tag, cp);
                        }
                        break;
                }
            }
            else if (checkDirCount(dir, 1))
            {
                int v32 = 0;
                long v64 = 0;
                // singleton value
                switch (dir.tdir_type)
                {
                    case TiffType.BYTE:
                    case TiffType.SBYTE:
                    case TiffType.SHORT:
                    case TiffType.SSHORT:
                        // If the tag is also acceptable as a LONG or SLONG
                        // then SetField will expect an int parameter
                        // passed to it. 
                        //
                        // NB: We use FieldWithTag here knowing that
                        //     it returns us the first entry in the table
                        //     for the tag and that that entry is for the
                        //     widest potential data type the tag may have.
                        TiffType type = fip.Type;
                        if (type != TiffType.LONG && type != TiffType.SLONG)
                        {
                            short v = (short)extractData(dir);
                            if (fip.PassCount)
                            {
                                short[] a = new short[1];
                                a[0] = v;
                                ok = SetField(dir.tdir_tag, 1, a);
                            }
                            else
                                ok = SetField(dir.tdir_tag, v);

                            break;
                        }

                        v32 = (int)extractData(dir);
                        if (fip.PassCount)
                        {
                            int[] a = new int[1];
                            a[0] = (int)v32;
                            ok = SetField(dir.tdir_tag, 1, a);
                        }
                        else
                            ok = SetField(dir.tdir_tag, v32);

                        break;

                    case TiffType.LONG:
                    case TiffType.SLONG:
                    case TiffType.IFD:
                        v32 = (int)extractData(dir);
                        if (fip.PassCount)
                        {
                            int[] a = new int[1];
                            a[0] = (int)v32;
                            ok = SetField(dir.tdir_tag, 1, a);
                        }
                        else
                            ok = SetField(dir.tdir_tag, v32);
                        break;
                    case TiffType.LONG8:
                    case TiffType.SLONG8:
                    case TiffType.IFD8:
                        v64 = extractData(dir);
                        if (fip.PassCount)
                        {
                            long[] a = new long[1];
                            a[0] = v64;
                            ok = SetField(dir.tdir_tag, 1, a);
                        }
                        else
                            ok = SetField(dir.tdir_tag, v64);
                        break;

                    case TiffType.RATIONAL:
                    case TiffType.SRATIONAL:
                    case TiffType.FLOAT:
                        float f = (dir.tdir_type == TiffType.FLOAT ? fetchFloat(dir) : fetchRational(dir));
                        if (fip.PassCount)
                        {
                            float[] a = new float[1];
                            a[0] = f;
                            ok = SetField(dir.tdir_tag, 1, a);
                        }
                        else
                            ok = SetField(dir.tdir_tag, f);

                        break;

                    case TiffType.DOUBLE:
                        double[] ds = new double[1];
                        ok = fetchDoubleArray(dir, ds);
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, 1, ds);
                            else
                                ok = SetField(dir.tdir_tag, ds[0]);
                        }
                        break;

                    case TiffType.ASCII:
                    case TiffType.UNDEFINED:
                        // bit of a cheat...
                        string c;
                        ok = fetchString(dir, out c) != 0;
                        if (ok)
                        {
                            if (fip.PassCount)
                                ok = SetField(dir.tdir_tag, 1, c);
                            else
                                ok = SetField(dir.tdir_tag, c);
                        }
                        break;
                }
            }

            return ok;
        }

        /// <summary>
        /// Fetches samples/pixel short values for the specified tag and verify
        /// that all values are the same.
        /// </summary>
        private bool fetchPerSampleShorts(TiffDirEntry dir, out short pl)
        {
            pl = 0;
            short samples = m_dir.td_samplesperpixel;
            bool status = false;

            if (checkDirCount(dir, samples))
            {
                short[] v = new short[dir.tdir_count];
                if (fetchShortArray(dir, v))
                {
                    int check_count = dir.tdir_count;
                    if (samples < check_count)
                        check_count = samples;

                    bool failed = false;
                    for (ushort i = 1; i < check_count; i++)
                    {
                        if (v[i] != v[0])
                        {
                            ErrorExt(this, m_clientdata, m_name,
                                "Cannot handle different per-sample values for field \"{0}\"",
                                FieldWithTag(dir.tdir_tag).Name);
                            failed = true;
                            break;
                        }
                    }

                    if (!failed)
                    {
                        pl = v[0];
                        status = true;
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Fetches samples/pixel long values for the specified tag and verify
        /// that all values are the same.
        /// </summary>
        private bool fetchPerSampleLongs(TiffDirEntry dir, out int pl)
        {
            pl = 0;
            short samples = m_dir.td_samplesperpixel;
            bool status = false;

            if (checkDirCount(dir, samples))
            {
                int[] v = new int[dir.tdir_count];
                if (fetchLongArray(dir, v))
                {
                    int check_count = dir.tdir_count;
                    if (samples < check_count)
                        check_count = samples;

                    bool failed = false;
                    for (ushort i = 1; i < check_count; i++)
                    {
                        if (v[i] != v[0])
                        {
                            ErrorExt(this, m_clientdata, m_name,
                                "Cannot handle different per-sample values for field \"{0}\"",
                                FieldWithTag(dir.tdir_tag).Name);
                            failed = true;
                            break;
                        }
                    }

                    if (!failed)
                    {
                        pl = (int)v[0];
                        status = true;
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Fetches samples/pixel ANY values for the specified tag and verify
        /// that all values are the same.
        /// </summary>
        private bool fetchPerSampleAnys(TiffDirEntry dir, out double pl)
        {
            pl = 0;
            short samples = m_dir.td_samplesperpixel;
            bool status = false;

            if (checkDirCount(dir, samples))
            {
                double[] v = new double[dir.tdir_count];
                if (fetchAnyArray(dir, v))
                {
                    int check_count = dir.tdir_count;
                    if (samples < check_count)
                        check_count = samples;

                    bool failed = false;
                    for (ushort i = 1; i < check_count; i++)
                    {
                        if (v[i] != v[0])
                        {
                            ErrorExt(this, m_clientdata, m_name,
                                "Cannot handle different per-sample values for field \"{0}\"",
                                FieldWithTag(dir.tdir_tag).Name);
                            failed = true;
                            break;
                        }
                    }

                    if (!failed)
                    {
                        pl = v[0];
                        status = true;
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Fetches a set of offsets or lengths.
        /// </summary>
        /// <remarks>While this routine says "strips", in fact it's also used
        /// for tiles.</remarks>
        private bool fetchStripThing(TiffDirEntry dir, int nstrips, ref long[] lpp)
        {
            checkDirCount(dir, nstrips);

            // Allocate space for strip information.
            if (lpp == null)
                lpp = new long[nstrips];
            else
                Array.Clear(lpp, 0, lpp.Length);

            bool status = false;
            if (dir.tdir_type == TiffType.SHORT)
            {
                // Handle short -> int expansion.
                short[] dp = new short[dir.tdir_count];
                status = fetchShortArray(dir, dp);
                if (status)
                {
                    for (int i = 0; i < nstrips && i < dir.tdir_count; i++)
                        lpp[i] = (ushort)dp[i];
                }
            }
            else if (nstrips != dir.tdir_count)
            {
                // Special case to correct length
                int[] dp = new int[dir.tdir_count];
                status = fetchLongArray(dir, dp);
                if (status)
                {
                    for (int i = 0; i < nstrips && i < dir.tdir_count; i++)
                        lpp[i] = (uint)dp[i];
                }
            }
            else if (dir.tdir_type == TiffType.LONG8)
            {
                status = fetchLong8Array(dir, lpp);
            }
            else
            {
                int[] temp = new int[lpp.Length];
                status = fetchLongArray(dir, temp);
                lpp = IntToLong(temp);
            }

            return status;
        }

        private bool fetchStripThing(TiffDirEntry dir, int nstrips, ref ulong[] lpp)
        {
            long[] temp = null;
            if (lpp != null)
                temp = new long[lpp.Length];

            bool res = fetchStripThing(dir, nstrips, ref temp);
            if (res)
            {
                if (lpp == null)
                    lpp = new ulong[temp.Length];

                Buffer.BlockCopy(temp, 0, lpp, 0, temp.Length * sizeof(ulong));
            }

            return res;
        }

        /// <summary>
        /// Fetches and sets the RefBlackWhite tag.
        /// </summary>
        private bool fetchRefBlackWhite(TiffDirEntry dir)
        {
            // some OJPEG images specify Reference BlackWhite as array of longs
            //
            // so, we'll try to read value as float array and check them
            // if read was successfull and there is at least one value greater
            // then 1.0 then ok, return value just read.
            //
            // if read failed or all values are less then or equal to 1.0 then
            // try once again but read as long array this time.

            if (dir.tdir_type == TiffType.RATIONAL)
            {
                bool res = fetchNormalTag(dir);
                if (res)
                {
                    for (int i = 0; i < m_dir.td_refblackwhite.Length; i++)
                    {
                        if (m_dir.td_refblackwhite[i] > 1)
                            return true;
                    }
                }
            }

            // Handle LONG's for backward compatibility.
            dir.tdir_type = TiffType.LONG;
            int[] cp = new int[dir.tdir_count];
            bool ok = fetchLongArray(dir, cp);
            dir.tdir_type = TiffType.RATIONAL;

            if (ok)
            {
                float[] fp = new float[dir.tdir_count];
                for (int i = 0; i < dir.tdir_count; i++)
                    fp[i] = (float)cp[i];

                ok = SetField(dir.tdir_tag, fp);
            }

            return ok;
        }

        /// <summary>
        /// Replace a single strip (tile) of uncompressed data with multiple
        /// strips (tiles), each approximately 8Kbytes.
        /// </summary>
        /// <remarks>This is useful for dealing with large images or for
        /// dealing with machines with a limited amount of memory.</remarks>
        private void chopUpSingleUncompressedStrip()
        {
            ulong bytecount = m_dir.td_stripbytecount[0];
            ulong offset = m_dir.td_stripoffset[0];

            // Make the rows hold at least one scanline, but fill specified
            // amount of data if possible.
            int rowbytes = VTileSize(1);
            ulong stripbytes;
            int rowsperstrip;
            if (rowbytes > STRIP_SIZE_DEFAULT)
            {
                stripbytes = (ulong)rowbytes;
                rowsperstrip = 1;
            }
            else if (rowbytes > 0)
            {
                rowsperstrip = STRIP_SIZE_DEFAULT / rowbytes;
                stripbytes = (ulong)(rowbytes * rowsperstrip);
            }
            else
            {
                return;
            }

            // never increase the number of strips in an image
            if (rowsperstrip >= m_dir.td_rowsperstrip)
                return;

            ulong nstrips = howMany(bytecount, stripbytes);
            if (nstrips == 0)
            {
                // something is wonky, do nothing.
                return;
            }

            ulong[] newcounts = new ulong[nstrips];
            ulong[] newoffsets = new ulong[nstrips];

            // Fill the strip information arrays with new bytecounts and offsets
            // that reflect the broken-up format.
            for (ulong strip = 0; strip < nstrips; strip++)
            {
                if (stripbytes > bytecount)
                    stripbytes = bytecount;

                newcounts[strip] = stripbytes;
                newoffsets[strip] = offset;
                offset += stripbytes;
                bytecount -= stripbytes;
            }

            // Replace old single strip info with multi-strip info.
            m_dir.td_nstrips = (int)nstrips;
            m_dir.td_stripsperimage = (int)nstrips;
            SetField(TiffTag.ROWSPERSTRIP, rowsperstrip);

            m_dir.td_stripbytecount = newcounts;
            m_dir.td_stripoffset = newoffsets;
            m_dir.td_stripbytecountsorted = true;
        }

        internal static int roundUp(int x, int y)
        {
            return (howMany(x, y) * y);
        }

        internal static int howMany(int x, int y)
        {
            long res = (((long)x + ((long)y - 1)) / (long)y);
            if (res > int.MaxValue)
                return 0;

            return (int)res;
        }

        internal static ulong howMany(ulong x, ulong y)
        {
            ulong res = ((x + (y - 1)) / y);
            return res;
        }
    }
}
