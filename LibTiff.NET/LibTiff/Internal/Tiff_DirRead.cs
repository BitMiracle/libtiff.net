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
 * Directory Read Support Routines.
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
        private int extractData(TiffDirEntry dir)
        {
            return (m_header.tiff_magic == TIFF_BIGENDIAN ?
                (dir.tdir_offset >> m_typeshift[(ushort)dir.tdir_type]) & m_typemask[(ushort)dir.tdir_type] :
                dir.tdir_offset & m_typemask[(ushort)dir.tdir_type]);
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
                (td.td_compression == COMPRESSION.COMPRESSION_NONE && td.td_stripbytecount[0] > getFileSize() - td.td_stripoffset[0]) ||
                (m_mode == O_RDONLY && td.td_compression == COMPRESSION.COMPRESSION_NONE && td.td_stripbytecount[0] < ScanlineSize() * td.td_imagelength)
            );
        }

        private static int howMany8(int x)
        {
            return ((x & 0x07) != 0 ? (x >> 3) + 1 : x >> 3);
        }

        private bool readDirectoryFailed(TiffDirEntry[] dir)
        {
            return false;
        }

        private bool estimateStripByteCounts(TiffDirEntry[] dir, ushort dircount)
        {
            const string module = "estimateStripByteCounts";

            m_dir.td_stripbytecount = new int [m_dir.td_nstrips];

            if (m_dir.td_compression != COMPRESSION.COMPRESSION_NONE)
            {
                int space = TiffHeader.SizeInBytes + sizeof(ushort) + (dircount * TiffDirEntry.SizeInBytes) + sizeof(uint);
                int filesize = getFileSize();

                /* calculate amount of space used by indirect values */
                for (ushort n = 0; n < dircount; n++)
                {
                    int cc = DataWidth((TiffDataType)dir[n].tdir_type);
                    if (cc == 0)
                    {
                        ErrorExt(this, m_clientdata, module,
                            "{0}: Cannot determine size of unknown tag type {1}",
                            m_name, dir[n].tdir_type);
                        return false;
                    }

                    cc = cc * dir[n].tdir_count;
                    if (cc > sizeof(uint))
                        space += cc;
                }

                space = filesize - space;
                if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                    space /= m_dir.td_samplesperpixel;
                
                uint strip = 0;
                for ( ; strip < m_dir.td_nstrips; strip++)
                    m_dir.td_stripbytecount[strip] = space;
                
                /*
                 * This gross hack handles the case were the offset to
                 * the last strip is past the place where we think the strip
                 * should begin.  Since a strip of data must be contiguous,
                 * it's safe to assume that we've overestimated the amount
                 * of data in the strip and trim this number back accordingly.
                 */
                strip--;
                if ((m_dir.td_stripoffset[strip] + m_dir.td_stripbytecount[strip]) > filesize)
                    m_dir.td_stripbytecount[strip] = filesize - m_dir.td_stripoffset[strip];
            }
            else if (IsTiled()) 
            {
                int bytespertile = TileSize();

                for (uint strip = 0; strip < m_dir.td_nstrips; strip++)
                    m_dir.td_stripbytecount[strip] = bytespertile;
            }
            else
            {
                int rowbytes = ScanlineSize();
                int rowsperstrip = m_dir.td_imagelength / m_dir.td_stripsperimage;
                for (uint strip = 0; strip < m_dir.td_nstrips; strip++)
                    m_dir.td_stripbytecount[strip] = rowbytes * rowsperstrip;
            }
            
            setFieldBit(FIELD.FIELD_STRIPBYTECOUNTS);
            if (!fieldSet(FIELD.FIELD_ROWSPERSTRIP))
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
            ErrorExt(this, m_clientdata, m_name,
                "Error fetching data for field \"{0}\"", FieldWithTag(dir.tdir_tag).field_name);
            return 0;
        }

        private static int readDirectoryFind(TiffDirEntry[] dir, ushort dircount, TIFFTAG tagid)
        {
            for (ushort n = 0; n < dircount; n++)
            {
                if (dir[n].tdir_tag == tagid)
                    return n;
            }

            return -1;
        }

        /*
        * Check the directory offset against the list of already seen directory
        * offsets. This is a trick to prevent IFD looping. The one can create TIFF
        * file with looped directory pointers. We will maintain a list of already
        * seen directories and check every IFD offset against that list.
        */
        private bool checkDirOffset(int diroff)
        {
            if (diroff == 0)
            {
                /* no more directories */
                return false;
            }

            for (ushort n = 0; n < m_dirnumber && m_dirlist != null; n++)
            {
                if (m_dirlist[n] == diroff)
                    return false;
            }

            m_dirnumber++;

            if (m_dirnumber > m_dirlistsize)
            {
                /*
                * XXX: Reduce memory allocation granularity of the dirlist array.
                */
                int[] new_dirlist = Realloc(m_dirlist, m_dirnumber - 1, 2 * m_dirnumber);
                m_dirlistsize = 2 * m_dirnumber;
                m_dirlist = new_dirlist;
            }

            m_dirlist[m_dirnumber - 1] = diroff;
            return true;
        }
        
        /*
        * Read IFD structure from the specified offset. If the pointer to
        * nextdiroff variable has been specified, read it too. Function returns a
        * number of fields in the directory or 0 if failed.
        */
        private ushort fetchDirectory(int diroff, out TiffDirEntry[] pdir, out int nextdiroff)
        {
            const string module = "fetchDirectory";

            m_diroff = diroff;
            nextdiroff = 0;

            ushort dircount;
            TiffDirEntry[] dir = null;
            pdir = null;

            if (!seekOK(m_diroff)) 
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Seek error accessing TIFF directory", m_name);
                return 0;
            }
            
            if (!readUInt16OK(out dircount)) 
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Can not read TIFF directory count", m_name);
                return 0;
            }
            
            if ((m_flags & Tiff.TIFF_SWAB) != 0)
                SwabShort(ref dircount);

            dir = new TiffDirEntry [dircount];
            if (!readDirEntryOk(dir, dircount))
            {
                ErrorExt(this, m_clientdata, module, "{0}: Can not read TIFF directory", m_name);
                return 0;
            }

            /*
            * Read offset to next directory for sequential scans.
            */
            readIntOK(out nextdiroff);

            if ((m_flags & Tiff.TIFF_SWAB) != 0)
                SwabLong(ref nextdiroff);

            pdir = dir;
            return dircount;
        }

        /*
        * Fetch and set the SubjectDistance EXIF tag.
        */
        private bool fetchSubjectDistance(TiffDirEntry dir)
        {
            bool ok = false;

            byte[] b = new byte[2 * sizeof(uint)];
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
                    FieldWithTag(dir.tdir_tag).field_name, dir.tdir_count, count);
                return false;
            }
            else if (count < dir.tdir_count)
            {
                WarningExt(this, m_clientdata, m_name,
                    "incorrect count for field \"{0}\" ({1}, expecting {2}); tag trimmed",
                    FieldWithTag(dir.tdir_tag).field_name, dir.tdir_count, count);
                return true;
            }

            return true;
        }

        /*
        * Fetch a contiguous directory item.
        */
        private int fetchData(TiffDirEntry dir, byte[] cp)
        {
            /* 
            * FIXME: bytecount should have int type, but for now libtiff
            * defines int as a signed 32-bit integer and we are losing
            * ability to read arrays larger than 2^31 bytes. So we are using
            * uint instead of int here.
            */

            int w = DataWidth(dir.tdir_type);
            int cc = dir.tdir_count * w;

            /* Check for overflow. */
            if (dir.tdir_count == 0 || w == 0 || (cc / w) != dir.tdir_count)
                fetchFailed(dir);

            if (!seekOK(dir.tdir_offset))
                fetchFailed(dir);

            if (!readOK(cp, cc))
                fetchFailed(dir);

            if ((m_flags & Tiff.TIFF_SWAB) != 0)
            {
                switch (dir.tdir_type)
                {
                    case TiffDataType.TIFF_SHORT:
                    case TiffDataType.TIFF_SSHORT:
                        {
                            ushort[] u = byteArrayToUInt16(cp, 0, cc);
                            SwabArrayOfShort(u, dir.tdir_count);
                            uint16ToByteArray(u, 0, dir.tdir_count, cp, 0);
                        }
                        break;
                    case TiffDataType.TIFF_LONG:
                    case TiffDataType.TIFF_SLONG:
                    case TiffDataType.TIFF_FLOAT:
                        {
                            int[] u = byteArrayToInt(cp, 0, cc);
                            SwabArrayOfLong(u, dir.tdir_count);
                            intToByteArray(u, 0, dir.tdir_count, cp, 0);
                        }
                        break;
                    case TiffDataType.TIFF_RATIONAL:
                    case TiffDataType.TIFF_SRATIONAL:
                        {
                            int[] u = byteArrayToInt(cp, 0, cc);
                            SwabArrayOfLong(u, 2 * dir.tdir_count);
                            intToByteArray(u, 0, 2 * dir.tdir_count, cp, 0);
                        }
                        break;
                    case TiffDataType.TIFF_DOUBLE:
                        swab64BitData(cp, cc);
                        break;
                }
            }

            return cc;
        }

        /*
        * Fetch an ASCII item from the file.
        */
        private int fetchString(TiffDirEntry dir, out string cp)
        {
            byte[] bytes = null;

            if (dir.tdir_count <= 4)
            {
                int l = dir.tdir_offset;
                if ((m_flags & Tiff.TIFF_SWAB) != 0)
                    SwabLong(ref l);

                bytes = new byte[sizeof(uint)];
                writeInt(l, bytes, 0);
                cp = Encoding.GetEncoding("Latin1").GetString(bytes, 0, dir.tdir_count);
                return 1;
            }

            bytes = new byte[dir.tdir_count];
            int res = fetchData(dir, bytes);
            cp = Encoding.GetEncoding("Latin1").GetString(bytes, 0, dir.tdir_count);
            return res;
        }

        /*
        * Convert numerator+denominator to float.
        */
        private bool cvtRational(TiffDirEntry dir, int num, int denom, out float rv)
        {
            if (denom == 0)
            {
                ErrorExt(this, m_clientdata, m_name,
                    "{0}: Rational with zero denominator (num = {1})",
                    FieldWithTag(dir.tdir_tag).field_name, num);
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
            byte[] bytes = new byte[sizeof(uint) * 2];
            int read = fetchData(dir, bytes);
            if (read != 0)
            {
                int[] l = new int[2];
                l[0] = readInt(bytes, 0);
                l[1] = readInt(bytes, sizeof(int));

                float v;
                bool res = cvtRational(dir, l[0], l[1], out v);
                if (res)
                    return v;
            }

            return 1.0f;
        }

        /*
        * Fetch a single floating point value
        * from the offset field and return it
        * as a native float.
        */
        private float fetchFloat(TiffDirEntry dir)
        {
            int l = extractData(dir);
            float v = BitConverter.ToSingle(BitConverter.GetBytes(l), 0);
            return v;
        }

        /*
        * Fetch an array of BYTE or SBYTE values.
        */
        private bool fetchByteArray(TiffDirEntry dir, byte[] v)
        {
            if (dir.tdir_count <= 4)
            {
                /*
                 * Extract data from offset field.
                 */
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

            return (fetchData(dir, v) != 0);
        }

        /*
        * Fetch an array of SHORT or SSHORT values.
        */
        private bool fetchShortArray(TiffDirEntry dir, ushort[] v)
        {
            if (dir.tdir_count <= 2)
            {
                int count = dir.tdir_count;

                if (m_header.tiff_magic == TIFF_BIGENDIAN)
                {
                    if (count == 2)
                        v[1] = (ushort)(dir.tdir_offset & 0xffff);

                    if (count >= 1)
                        v[0] = (ushort)(dir.tdir_offset >> 16);
                }
                else
                {
                    if (count == 2)
                        v[1] = (ushort)(dir.tdir_offset >> 16);

                    if (count >= 1)
                        v[0] = (ushort)(dir.tdir_offset & 0xffff);
                }

                return true;
            }

            int cc = dir.tdir_count * sizeof(ushort);
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
            {
                ushort[] u = byteArrayToUInt16(b, 0, read);
                Array.Copy(u, v, u.Length);
            }

            return (read != 0);
        }

        /*
        * Fetch a pair of SHORT or BYTE values. Some tags may have either BYTE
        * or SHORT type and this function works with both ones.
        */
        private bool fetchShortPair(TiffDirEntry dir)
        {
            /*
            * Prevent overflowing the v stack arrays below by performing a sanity
            * check on tdir_count, this should never be greater than two.
            */
            if (dir.tdir_count > 2) 
            {
                WarningExt(this, m_clientdata, m_name,
                    "unexpected count for field \"{0}\", {1}, expected 2; ignored",
                    FieldWithTag(dir.tdir_tag).field_name, dir.tdir_count);
                return false;
            }

            switch (dir.tdir_type)
            {
                case TiffDataType.TIFF_BYTE:
                case TiffDataType.TIFF_SBYTE:
                    {
                        byte[] v = new byte[4];
                        return fetchByteArray(dir, v) && SetField(dir.tdir_tag, v[0], v[1]);
                    }
                case TiffDataType.TIFF_SHORT:
                case TiffDataType.TIFF_SSHORT:
                    {
                        ushort[] v = new ushort[2];
                        return fetchShortArray(dir, v) && SetField(dir.tdir_tag, v[0], v[1]);
                    }
            }

            return false;
        }

        /*
        * Fetch an array of LONG or SLONG values.
        */
        private bool fetchLongArray(TiffDirEntry dir, int[] v)
        {
            if (dir.tdir_count == 1)
            {
                v[0] = dir.tdir_offset;
                return true;
            }

            int cc = dir.tdir_count * sizeof(int);
            byte[] b = new byte[cc];
            int read = fetchData(dir, b);
            if (read != 0)
            {
                int[] u = byteArrayToInt(b, 0, read);
                Array.Copy(u, v, u.Length);
            }

            return (read != 0);
        }

        /*
        * Fetch an array of RATIONAL or SRATIONAL values.
        */
        private bool fetchRationalArray(TiffDirEntry dir, float[] v)
        {
            Debug.Assert(sizeof(float) == sizeof(uint));

            bool ok = false;
            byte[] l = new byte [dir.tdir_count * DataWidth(dir.tdir_type)];
            if (fetchData(dir, l) != 0)
            {
                int offset = 0;
                int[] pair = new int[2];
                for (uint i = 0; i < dir.tdir_count; i++)
                {
                    pair[0] = readInt(l, offset);
                    offset += sizeof(int);
                    pair[1] = readInt(l, offset);
                    offset += sizeof(int);

                    ok = cvtRational(dir, pair[0], pair[1], out v[i]);
                    if (!ok)
                        break;
                }
            }

            return ok;
        }

        /*
        * Fetch an array of FLOAT values.
        */
        private bool fetchFloatArray(TiffDirEntry dir, float[] v)
        {
            if (dir.tdir_count == 1)
            {
                v[0] = BitConverter.ToSingle(BitConverter.GetBytes(dir.tdir_offset), 0);
                return true;
            }

            int w = DataWidth(dir.tdir_type);
            int cc = dir.tdir_count * w;
            byte[] b = new byte [cc];
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

        /*
        * Fetch an array of DOUBLE values.
        */
        private bool fetchDoubleArray(TiffDirEntry dir, double[] v)
        {
            int w = DataWidth(dir.tdir_type);
            int cc = dir.tdir_count * w;
            byte[] b = new byte [cc];
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

        /*
        * Fetch an array of ANY values.  The actual values are
        * returned as doubles which should be able hold all the
        * types.  Yes, there really should be an tany_t to avoid
        * this potential non-portability ...  Note in particular
        * that we assume that the double return value vector is
        * large enough to read in any fundamental type.  We use
        * that vector as a buffer to read in the base type vector
        * and then convert it in place to double (from end
        * to front of course).
        */
        private bool fetchAnyArray(TiffDirEntry dir, double[] v)
        {
            switch (dir.tdir_type)
            {
                case TiffDataType.TIFF_BYTE:
                case TiffDataType.TIFF_SBYTE:
                    {
                        byte[] b = new byte[dir.tdir_count];
                        bool res = fetchByteArray(dir, b);
                        if (res)
                        {
                            for (int i = dir.tdir_count - 1; i >= 0; i--)
                                v[i] = b[i];
                        }
                        
                        if (!res)
                            return false;
                    }
                    break;
                case TiffDataType.TIFF_SHORT:
                case TiffDataType.TIFF_SSHORT:
                    {
                        ushort[] u = new ushort[dir.tdir_count];
                        bool res = fetchShortArray(dir, u);
                        if (res)
                        {
                            for (int i = dir.tdir_count - 1; i >= 0; i--)
                                v[i] = u[i];
                        }

                        if (!res)
                            return false;
                    }
                    break;
                case TiffDataType.TIFF_LONG:
                case TiffDataType.TIFF_SLONG:
                    {
                        int[] l = new int[dir.tdir_count];
                        bool res = fetchLongArray(dir, l);
                        if (res)
                        {
                            for (int i = dir.tdir_count - 1; i >= 0; i--)
                                v[i] = l[i];
                        }

                        if (!res)
                            return false;
                    }
                    break;
                case TiffDataType.TIFF_RATIONAL:
                case TiffDataType.TIFF_SRATIONAL:
                    {
                        float[] f = new float[dir.tdir_count];
                        bool res = fetchRationalArray(dir, f);
                        if (res)
                        {
                            for (int i = dir.tdir_count - 1; i >= 0; i--)
                                v[i] = f[i];
                        }

                        if (!res)
                            return false;
                    }
                    break;
                case TiffDataType.TIFF_FLOAT:
                    {
                        float[] f = new float[dir.tdir_count];
                        bool res = fetchFloatArray(dir, f);
                        if (res)
                        {
                            for (int i = dir.tdir_count - 1; i >= 0; i--)
                                v[i] = f[i];
                        }

                        if (!res)
                            return false;
                    }
                    break;
                case TiffDataType.TIFF_DOUBLE:
                    return fetchDoubleArray(dir, v);
                default:
                    /* TIFF_NOTYPE */
                    /* TIFF_ASCII */
                    /* TIFF_UNDEFINED */
                    ErrorExt(this, m_clientdata, m_name,
                        "cannot read TIFF_ANY type {0} for field \"{1}\"",
                        dir.tdir_type, FieldWithTag(dir.tdir_tag).field_name);
                    return false;
            }

            return true;
        }

        /*
        * Fetch a tag that is not handled by special case code.
        */
        private bool fetchNormalTag(TiffDirEntry dir)
        {
            bool ok = false;
            TiffFieldInfo fip = FieldWithTag(dir.tdir_tag);

            if (dir.tdir_count > 1)
            {
                switch (dir.tdir_type)
                {
                    case TiffDataType.TIFF_BYTE:
                    case TiffDataType.TIFF_SBYTE:
                        {
                            byte[] cp = new byte [dir.tdir_count];
                            ok = fetchByteArray(dir, cp);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_SHORT:
                    case TiffDataType.TIFF_SSHORT:
                        {
                            ushort[] cp = new ushort [dir.tdir_count];
                            ok = fetchShortArray(dir, cp);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_LONG:
                    case TiffDataType.TIFF_SLONG:
                        {
                            int[] cp = new int [dir.tdir_count];
                            ok = fetchLongArray(dir, cp);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_RATIONAL:
                    case TiffDataType.TIFF_SRATIONAL:
                        {
                            float[] cp = new float [dir.tdir_count];
                            ok = fetchRationalArray(dir, cp);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_FLOAT:
                        {
                            float[] cp = new float [dir.tdir_count];
                            ok = fetchFloatArray(dir, cp);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_DOUBLE:
                        {
                            double[] cp = new double [dir.tdir_count];
                            ok = fetchDoubleArray(dir, cp);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_ASCII:
                    case TiffDataType.TIFF_UNDEFINED:
                        {
                            /* bit of a cheat... */
                            /*
                             * Some vendors write strings w/o the trailing
                             * null byte, so always append one just in case.
                             */
                            string cp;
                            ok = fetchString(dir, out cp) != 0;
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, dir.tdir_count, cp);
                                else
                                    ok = SetField(dir.tdir_tag, cp);
                            }
                        }
                        break;
                }        
            }
            else if (checkDirCount(dir, 1))
            {
                /* singleton value */
                switch (dir.tdir_type)
                {
                    case TiffDataType.TIFF_BYTE:
                    case TiffDataType.TIFF_SBYTE:
                    case TiffDataType.TIFF_SHORT:
                    case TiffDataType.TIFF_SSHORT:
                        /*
                         * If the tag is also acceptable as a LONG or SLONG
                         * then SetField will expect an uint parameter
                         * passed to it (through varargs).  Thus, for machines
                         * where sizeof (int) != sizeof (uint) we must do
                         * a careful check here.  It's hard to say if this
                         * is worth optimizing.
                         *
                         * NB: We use FieldWithTag here knowing that
                         *     it returns us the first entry in the table
                         *     for the tag and that that entry is for the
                         *     widest potential data type the tag may have.
                         */
                        {
                            TiffDataType type = fip.field_type;
                            if (type != TiffDataType.TIFF_LONG && type != TiffDataType.TIFF_SLONG)
                            {
                                ushort v = (ushort)extractData(dir);
                                if (fip.field_passcount)
                                {
                                    ushort[] a = new ushort[1];
                                    a[0] = v;
                                    ok = SetField(dir.tdir_tag, 1, a);
                                }
                                else
                                    ok = SetField(dir.tdir_tag, v);

                                break;
                            }

                            int v32 = extractData(dir);
                            if (fip.field_passcount)
                            {
                                int[] a = new int[1];
                                a[0] = v32;
                                ok = SetField(dir.tdir_tag, 1, a);
                            }
                            else
                                ok = SetField(dir.tdir_tag, v32);
                        }
                        break;

                    case TiffDataType.TIFF_LONG:
                    case TiffDataType.TIFF_SLONG:
                        {
                            int v32 = extractData(dir);
                            if (fip.field_passcount)
                            {
                                int[] a = new int[1];
                                a[0] = v32;
                                ok = SetField(dir.tdir_tag, 1, a);
                            }
                            else
                                ok = SetField(dir.tdir_tag, v32);
                        }
                        break;
                    case TiffDataType.TIFF_RATIONAL:
                    case TiffDataType.TIFF_SRATIONAL:
                    case TiffDataType.TIFF_FLOAT:
                        {
                            float v = (dir.tdir_type == TiffDataType.TIFF_FLOAT ? fetchFloat(dir): fetchRational(dir));
                            if (fip.field_passcount)
                            {
                                float[] a = new float[1];
                                a[0] = v;
                                ok = SetField(dir.tdir_tag, 1, a);
                            }
                            else
                                ok = SetField(dir.tdir_tag, v);
                        }
                        break;
                    case TiffDataType.TIFF_DOUBLE:
                        {
                            double[] v = new double[1];
                            ok = fetchDoubleArray(dir, v);
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, 1, v);
                                else
                                    ok = SetField(dir.tdir_tag, v[0]);
                            }
                        }
                        break;
                    case TiffDataType.TIFF_ASCII:
                    case TiffDataType.TIFF_UNDEFINED:
                         /* bit of a cheat... */
                        {
                            string c;
                            ok = fetchString(dir, out c) != 0;
                            if (ok)
                            {
                                if (fip.field_passcount)
                                    ok = SetField(dir.tdir_tag, 1, c);
                                else
                                    ok = SetField(dir.tdir_tag, c);
                            }
                        }
                        break;
                }
            }

            return ok;
        }

        /*
        * Fetch samples/pixel short values for 
        * the specified tag and verify that
        * all values are the same.
        */
        private bool fetchPerSampleShorts(TiffDirEntry dir, out ushort pl)
        {
            pl = 0;
            ushort samples = m_dir.td_samplesperpixel;
            bool status = false;

            if (checkDirCount(dir, samples))
            {
                ushort[] v = new ushort [dir.tdir_count];
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
                                FieldWithTag(dir.tdir_tag).field_name);
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

        /*
        * Fetch samples/pixel long values for 
        * the specified tag and verify that
        * all values are the same.
        */
        private bool fetchPerSampleLongs(TiffDirEntry dir, out int pl)
        {
            pl = 0;
            ushort samples = m_dir.td_samplesperpixel;
            bool status = false;

            if (checkDirCount(dir, samples))
            {
                int[] v = new int [dir.tdir_count];
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
                                FieldWithTag(dir.tdir_tag).field_name);
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

        /*
        * Fetch samples/pixel ANY values for the specified tag and verify that all
        * values are the same.
        */
        private bool fetchPerSampleAnys(TiffDirEntry dir, out double pl)
        {
            pl = 0;
            ushort samples = m_dir.td_samplesperpixel;
            bool status = false;

            if (checkDirCount(dir, samples))
            {
                double[] v = new double [dir.tdir_count];
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
                                FieldWithTag(dir.tdir_tag).field_name);
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

        /*
        * Fetch a set of offsets or lengths.
        * While this routine says "strips", in fact it's also used for tiles.
        */
        private bool fetchStripThing(TiffDirEntry dir, int nstrips, ref int[] lpp)
        {
            checkDirCount(dir, nstrips);

            /*
             * Allocate space for strip information.
             */
            if (lpp == null)
                lpp = new int [nstrips];
            else
                Array.Clear(lpp, 0, lpp.Length);

            bool status = false;
            if (dir.tdir_type == TiffDataType.TIFF_SHORT)
            {
                /*
                 * Handle uint16.uint expansion.
                 */
                ushort[] dp = new ushort[dir.tdir_count];
                status = fetchShortArray(dir, dp);
                if (status)
                {
                    for (int i = 0; i < nstrips && i < (int)dir.tdir_count; i++)
                        lpp[i] = dp[i];
                }
            }
            else if (nstrips != (int)dir.tdir_count)
            {
                /* Special case to correct length */

                int[] dp = new int[dir.tdir_count];
                status = fetchLongArray(dir, dp);
                if (status)
                {
                    for (int i = 0; i < nstrips && i < (int)dir.tdir_count; i++)
                        lpp[i] = dp[i];
                }
            }
            else
                status = fetchLongArray(dir, lpp);

            return status;
        }

        /*
        * Fetch and set the RefBlackWhite tag.
        */
        private bool fetchRefBlackWhite(TiffDirEntry dir)
        {
            if (dir.tdir_type == TiffDataType.TIFF_RATIONAL)
                return fetchNormalTag(dir);
            
            /*
             * Handle LONG's for backward compatibility.
             */
            int[] cp = new int [dir.tdir_count];
            bool ok = fetchLongArray(dir, cp);
            if (ok)
            {
                float[] fp = new float [dir.tdir_count];
                for (uint i = 0; i < dir.tdir_count; i++)
                    fp[i] = (float)cp[i];

                ok = SetField(dir.tdir_tag, fp);
            }

            return ok;
        }

        /*
        * Replace a single strip (tile) of uncompressed data by
        * multiple strips (tiles), each approximately 8Kbytes.
        * This is useful for dealing with large images or
        * for dealing with machines with a limited amount
        * memory.
        */
        private void chopUpSingleUncompressedStrip()
        {
            int bytecount = m_dir.td_stripbytecount[0];
            int offset = m_dir.td_stripoffset[0];

            /*
             * Make the rows hold at least one scanline, but fill specified amount
             * of data if possible.
             */
            int rowbytes = VTileSize(1);
            int stripbytes;
            int rowsperstrip;
            if (rowbytes > STRIP_SIZE_DEFAULT)
            {
                stripbytes = rowbytes;
                rowsperstrip = 1;
            }
            else if (rowbytes > 0)
            {
                rowsperstrip = STRIP_SIZE_DEFAULT / rowbytes;
                stripbytes = rowbytes * rowsperstrip;
            }
            else
                return ;

            /* 
             * never increase the number of strips in an image
             */
            if (rowsperstrip >= m_dir.td_rowsperstrip)
                return ;
            
            int nstrips = howMany(bytecount, stripbytes);
            if (nstrips == 0)
            {
                /* something is wonky, do nothing. */
                return ;
            }

            int[] newcounts = new int [nstrips];
            int[] newoffsets = new int [nstrips];

            /*
             * Fill the strip information arrays with new bytecounts and offsets
             * that reflect the broken-up format.
             */
            for (uint strip = 0; strip < nstrips; strip++)
            {
                if (stripbytes > bytecount)
                    stripbytes = bytecount;

                newcounts[strip] = stripbytes;
                newoffsets[strip] = offset;
                offset += stripbytes;
                bytecount -= stripbytes;
            }

            /*
             * Replace old single strip info with multi-strip info.
             */
            m_dir.td_nstrips = nstrips;
            m_dir.td_stripsperimage = nstrips;
            SetField(TIFFTAG.TIFFTAG_ROWSPERSTRIP, rowsperstrip);

            m_dir.td_stripbytecount = newcounts;
            m_dir.td_stripoffset = newoffsets;
            m_dir.td_stripbytecountsorted = 1;
        }

        internal static int roundUp(int x, int y)
        {
            return (howMany(x, y) * y);
        }

        internal static int howMany(int x, int y)
        {
            return ((x + (y - 1)) / y);
        }
    }
}
