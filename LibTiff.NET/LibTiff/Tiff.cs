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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using BitMiracle.LibTiff.Internal;

using thandle_t = System.Object;

namespace BitMiracle.LibTiff
{
    /// <summary>
    /// Tag Image File Format (TIFF)
    /// 
    /// Based on Rev 6.0 from:
    ///     Developer's Desk
    ///     Aldus Corporation
    ///     411 First Ave. South
    ///     Suite 200
    ///     Seattle, WA  98104
    ///     206-622-5500
    ///
    /// (http://partners.adobe.com/asn/developer/PDFS/TN/TIFF6.pdf)
    /// 
    /// For Big TIFF design notes see the following link
    /// http://gdal.maptools.org/twiki/bin/view/libtiff/BigTIFFDesign
    /// </summary>
    public partial class Tiff
    {
        /// <summary>
        /// Support strip chopping (whether or not to convert single-strip 
        /// uncompressed images to mutiple strips of ~8Kb to reduce memory usage)
        /// </summary>
        internal const uint STRIPCHOP_DEFAULT = TIFF_STRIPCHOP;

        /// <summary>
        /// Treat extra sample as alpha (default enabled). The RGBA interface 
        /// will treat a fourth sample with no EXTRASAMPLE_ value as being 
        /// ASSOCALPHA. Many packages produce RGBA files but don't mark the 
        /// alpha properly.
        /// </summary>
        internal const bool DEFAULT_EXTRASAMPLE_AS_ALPHA = true;

        /// <summary>
        /// Pick up YCbCr subsampling info from the JPEG data stream to support 
        /// files lacking the tag (default enabled).
        /// </summary>
        internal const bool CHECK_JPEG_YCBCR_SUBSAMPLING = true;

        internal const string TIFFLIB_VERSION_STR = "LIBTIFF, Version 3.9.1\nCopyright (c) 1988-1996 Sam Leffler\nCopyright (c) 1991-1996 Silicon Graphics, Inc.";

        /*
         * These constants can be used in code that requires
         * compilation-related definitions specific to a
         * version or versions of the library.  Runtime
         * version checking should be done based on the
         * string returned by TIFFGetVersion.
         */

        public const int TIFF_VERSION = 42;
        public const int TIFF_BIGTIFF_VERSION = 43;

        public const ushort TIFF_BIGENDIAN = 0x4d4d;
        public const ushort TIFF_LITTLEENDIAN = 0x4949;
        public const ushort MDI_LITTLEENDIAN = 0x5045;

        //public ~Tiff();

        public static string GetVersion()
        {
            return m_version;
        }

        /*
        * Macros for extracting components from the
        * packed ABGR form returned by ReadRGBAImage.
        */
        public static uint GetR(uint abgr)
        {
            return (abgr & 0xff);
        }

        public static uint GetG(uint abgr)
        {
            return ((abgr >> 8) & 0xff);
        }

        public static uint GetB(uint abgr)
        {
            return ((abgr >> 16) & 0xff);
        }

        public static uint GetA(uint abgr)
        {
            return ((abgr >> 24) & 0xff);
        }

        /*
        * Other compression schemes may be registered.  Registered
        * schemes can also override the built in versions provided
        * by this library.
        */
        public TiffCodec FindCodec(UInt16 scheme)
        {
            for (codecList cd = m_registeredCodecs; cd != null; cd = cd.next)
            {
                if (cd.codec.m_scheme == scheme)
                    return cd.codec;
            }

            for (int i = 0; m_builtInCodecs[i] != null; i++)
            {
                TiffCodec c = m_builtInCodecs[i];
                if (c.m_scheme == scheme)
                    return c;
            }

            return null;
        }

        public bool RegisterCodec(TiffCodec codec)
        {
            if (codec == null)
                return false;

            codecList cd = new codecList();
            if (cd != null)
            {
                cd.codec = codec;
                cd.next = m_registeredCodecs;
                m_registeredCodecs = cd;
            }
            else
            {
                ErrorExt(this, 0, "RegisterCodec", "No space to register compression scheme %s", codec.m_name);
                return false;
            }

            return true;
        }

        public void UnRegisterCodec(TiffCodec c)
        {
            if (m_registeredCodecs == null)
                return;

            codecList temp;
            if (m_registeredCodecs.codec == c)
            {
                temp = m_registeredCodecs.next;
                m_registeredCodecs = temp;
                return;
            }

            for (codecList cd = m_registeredCodecs; cd != null; cd = cd.next)
            {
                if (cd.next != null)
                {
                    if (cd.next.codec == c)
                    {
                        temp = cd.next.next;
                        cd.next = temp;
                        return;
                    }
                }
            }

            ErrorExt(this, 0, "UnRegisterCodec", "Cannot remove compression scheme %s; not registered", c.m_name);
        }

        /**
        * Check whether we have working codec for the specific coding scheme.
        * @return returns true if the codec is configured and working. Otherwise
        * false will be returned.
        */
        public bool IsCodecConfigured(UInt16 scheme)
        {
            TiffCodec codec = FindCodec(scheme);

            if (codec == null)
                return false;

            if (codec.CanEncode() != false || codec.CanDecode() != false)
                return true;

            return false;
        }

        /**
        * Get array of configured codecs, both built-in and registered by user.
        * Caller is responsible to free this array (but not codecs).
        * @return returns array of TiffCodec records (the last record should be null)
        * or null if function failed.
        */
        public TiffCodec[] GetConfiguredCodecs()
        {
            int totalCodecs = 0;
            for (int i = 0; m_builtInCodecs[i] != null; i++)
            {
                if (m_builtInCodecs[i] != null && IsCodecConfigured((UInt16)m_builtInCodecs[i].m_scheme))
                    totalCodecs++;
            }

            for (codecList cd = m_registeredCodecs; cd != null; cd = cd.next)
                totalCodecs++;

            TiffCodec[] codecs = new TiffCodec [totalCodecs + 1];
            if (codecs == null)
                return null;

            int codecPos = 0;
            for (codecList cd = m_registeredCodecs; cd != null; cd = cd.next)
                codecs[codecPos++] = cd.codec;

            for (int i = 0; m_builtInCodecs[i] != null; i++)
            {
                if (m_builtInCodecs[i] != null && IsCodecConfigured((UInt16)m_builtInCodecs[i].m_scheme))
                    codecs[codecPos++] = m_builtInCodecs[i];
            }

            codecs[codecPos] = null;
            return codecs;
        }

        /*
         * Auxiliary functions.
         */

        /**
        * Re-allocates array and copies data from old to new array. 
        * Size is in elements, not bytes!
        * Also frees old array. Returns new allocated array.
        */
        public static byte[] Realloc(byte[] oldBuffer, int elementCount, int newElementCount)
        {
            byte[] newBuffer = new byte[newElementCount];
            //memset(newBuffer, 0, newElementCount * sizeof(byte));

            if (oldBuffer == null)
                return newBuffer;

            if (newBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
        }

        public static int[] Realloc(int[] oldBuffer, int elementCount, int newElementCount)
        {
            int[] newBuffer = new int[newElementCount];
            //memset(newBuffer, 0, newElementCount * sizeof(uint));

            if (oldBuffer == null)
                return newBuffer;

            if (newBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
        }

        public static int Compare(UInt16[] p1, UInt16[] p2, int elementCount)
        {
            for (int i = 0; i < elementCount; i++)
            {
                if (p1[i] != p2[i])
                    return p1[i] - p2[i];
            }

            return 0;
        }

        /*
        * Open a TIFF file for read/writing.
        */
        public static Tiff Open(string name, string mode)
        {
            //const string module = "Open";

            return null;
            //DWORD dwMode;
            //int m = getMode(mode, module);
            //switch (m)
            //{
            //    case O_RDONLY:
            //        dwMode = OPEN_EXISTING;
            //        break;
            //    case O_RDWR:
            //        dwMode = OPEN_ALWAYS;
            //        break;
            //    case O_RDWR | O_CREAT: 
            //        dwMode = OPEN_ALWAYS;
            //        break;
            //    case O_RDWR | O_TRUNC: 
            //        dwMode = CREATE_ALWAYS;
            //        break;
            //    case O_RDWR | O_CREAT | O_TRUNC: 
            //        dwMode = CREATE_ALWAYS;
            //        break;
            //    default:
            //        return null;
            //}

            //thandle_t fd = (thandle_t)CreateFileA(name, (m == O_RDONLY) ? GENERIC_READ: (GENERIC_READ | GENERIC_WRITE), FILE_SHARE_READ | FILE_SHARE_WRITE, null, dwMode, (m == O_RDONLY) ? FILE_ATTRIBUTE_READONLY: FILE_ATTRIBUTE_NORMAL, null);
            //if (fd == INVALID_HANDLE_VALUE)
            //{
            //    ErrorExt(null, 0, module, "%s: Cannot open", name);
            //    return null;
            //}

            //Tiff tif = FdOpen((int)fd, name, mode);
            //if (tif == null)
            //    CloseHandle(fd);

            //return tif;
        }

        /*
        * Open a TIFF file descriptor for read/writing.
        */
        public static Tiff FdOpen(int ifd, string name, string mode)
        {
            Tiff tif = ClientOpen(name, mode, (thandle_t)ifd, new TiffStream());
            if (tif != null)
                tif.m_userStream = false; // clear flag, so stream will be deleted

            return tif;
        }

        public static Tiff ClientOpen(string name, string mode, thandle_t clientdata, TiffStream stream)
        {
            const string module = "ClientOpen";
    
            int m = getMode(mode, module);
            if (m == -1)
                return null;

            Tiff tif = new Tiff();
            if (tif == null)
            {
                ErrorExt(tif, clientdata, module, "%s: Out of memory (TIFF structure)", name);
                return null;
            }

            tif.m_name = name.Clone() as string;

            tif.m_mode = m & ~(O_CREAT | O_TRUNC);
            tif.m_curdir = -1; /* non-existent directory */
            tif.m_curoff = 0;
            tif.m_curstrip = -1; /* invalid strip */
            tif.m_row = -1; /* read/write pre-increment */
            tif.m_clientdata = clientdata;

            if (stream == null)
            {
                ErrorExt(tif, clientdata, module, "TiffStream is null pointer.");
                return null;
            }

            tif.m_stream = stream;
            tif.m_userStream = true;

            /* setup default state */
            tif.m_currentCodec = tif.m_builtInCodecs[0];

            /*
             * Default is to return data MSB2LSB and enable the
             * use of memory-mapped files and strip chopping when
             * a file is opened read-only.
             */
            tif.m_flags = FILLORDER_MSB2LSB;

            if (m == O_RDONLY || m == O_RDWR)
                tif.m_flags |= STRIPCHOP_DEFAULT;

            /*
             * Process library-specific flags in the open mode string.
             * The following flags may be used to control intrinsic library
             * behaviour that may or may not be desirable (usually for
             * compatibility with some application that claims to support
             * TIFF but only supports some braindead idea of what the
             * vendor thinks TIFF is):
             *
             * 'l'      use little-endian byte order for creating a file
             * 'b'      use big-endian byte order for creating a file
             * 'L'      read/write information using LSB2MSB bit order
             * 'B'      read/write information using MSB2LSB bit order
             * 'H'      read/write information using host bit order
             * 'C'      enable strip chopping support when reading
             * 'c'      disable strip chopping support
             * 'h'      read TIFF header only, do not load the first IFD
             *
             * The use of the 'l' and 'b' flags is strongly discouraged.
             * These flags are provided solely because numerous vendors,
             * typically on the PC, do not correctly support TIFF; they
             * only support the Intel little-endian byte order.  This
             * support is not configured by default because it supports
             * the violation of the TIFF spec that says that readers *MUST*
             * support both byte orders.  It is strongly recommended that
             * you not use this feature except to deal with busted apps
             * that write invalid TIFF.  And even in those cases you should
             * bang on the vendors to fix their software.
             *
             * The 'L', 'B', and 'H' flags are intended for applications
             * that can optimize operations on data by using a particular
             * bit order.  By default the library returns data in MSB2LSB
             * bit order for compatibiltiy with older versions of this
             * library.  Returning data in the bit order of the native cpu
             * makes the most sense but also requires applications to check
             * the value of the FillOrder tag; something they probably do
             * not do right now.
             *
             * The 'C' and 'c' flags are provided because the library support
             * for chopping up large strips into multiple smaller strips is not
             * application-transparent and as such can cause problems.  The 'c'
             * option permits applications that only want to look at the tags,
             * for example, to get the unadulterated TIFF tag information.
             */
            int modelength = mode.Length;
            for (int i = 0; i < modelength; i++)
            {
                switch (mode[i])
                {
                    case 'b':
                        if ((m & O_CREAT) != 0)
                            tif.m_flags |= TIFF_SWAB;
                        break;
                    case 'l':
                        break;
                    case 'B':
                        tif.m_flags = (tif.m_flags & ~TIFF_FILLORDER) | FILLORDER_MSB2LSB;
                        break;
                    case 'L':
                        tif.m_flags = (tif.m_flags & ~TIFF_FILLORDER) | FILLORDER_LSB2MSB;
                        break;
                    case 'H':
                        tif.m_flags = (tif.m_flags & ~TIFF_FILLORDER) | FILLORDER_LSB2MSB;
                        break;
                    case 'C':
                        if (m == O_RDONLY)
                            tif.m_flags |= TIFF_STRIPCHOP;
                        break;
                    case 'c':
                        if (m == O_RDONLY)
                            tif.m_flags &= ~TIFF_STRIPCHOP;
                        break;
                    case 'h':
                        tif.m_flags |= TIFF_HEADERONLY;
                        break;
                }
            }

            /*
             * Read in TIFF header.
             */

            if ((tif.m_mode & O_TRUNC) != 0 || !tif.readHeaderOk(tif.m_header))
            {
                if (tif.m_mode == O_RDONLY)
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Cannot read TIFF header");
                    return tif.safeOpenFailed();
                }

                /*
                 * Setup header and write.
                 */
                tif.m_header.tiff_magic = (tif.m_flags & TIFF_SWAB) != 0 ? TIFF_BIGENDIAN : TIFF_LITTLEENDIAN;
                tif.m_header.tiff_version = TIFF_VERSION;
                if ((tif.m_flags & TIFF_SWAB) != 0)
                    SwabShort(ref tif.m_header.tiff_version);

                tif.m_header.tiff_diroff = 0; /* filled in later */

                /*
                 * The doc for "fopen" for some STD_C_LIBs says that if you 
                 * open a file for modify ("+"), then you must fseek (or 
                 * fflush?) between any freads and fwrites.  This is not
                 * necessary on most systems, but has been shown to be needed
                 * on Solaris. 
                 */
                tif.seekFile(0, SEEK_SET);

                if (!tif.writeHeaderOK(tif.m_header))
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Error writing TIFF header");
                    return tif.safeOpenFailed();
                }
                /*
                 * Setup the byte order handling.
                 */
                tif.initOrder(tif.m_header.tiff_magic);

                /*
                 * Setup default directory.
                 */
                tif.setupDefaultDirectory();
                tif.m_diroff = 0;
                tif.m_dirlist = null;
                tif.m_dirlistsize = 0;
                tif.m_dirnumber = 0;
                return tif;
            }

            /*
             * Setup the byte order handling.
             */
            if (tif.m_header.tiff_magic != TIFF_BIGENDIAN && tif.m_header.tiff_magic != TIFF_LITTLEENDIAN && tif.m_header.tiff_magic != MDI_LITTLEENDIAN)
            {
                ErrorExt(tif, tif.m_clientdata, name, "Not a TIFF or MDI file, bad magic number %d (0x%x)", tif.m_header.tiff_magic, tif.m_header.tiff_magic);
                return tif.safeOpenFailed();
            }

            tif.initOrder(tif.m_header.tiff_magic);

            /*
             * Swap header if required.
             */
            if ((tif.m_flags & TIFF_SWAB) != 0)
            {
                SwabShort(ref tif.m_header.tiff_version);
                SwabLong(ref tif.m_header.tiff_diroff);
            }
            /*
             * Now check version (if needed, it's been byte-swapped).
             * Note that this isn't actually a version number, it's a
             * magic number that doesn't change (stupid).
             */
            if (tif.m_header.tiff_version == TIFF_BIGTIFF_VERSION)
            {
                ErrorExt(tif, tif.m_clientdata, name, "This is a BigTIFF file.  This format not supported\nby this version of libtiff.");
                return tif.safeOpenFailed();
            }

            if (tif.m_header.tiff_version != TIFF_VERSION)
            {
                ErrorExt(tif, tif.m_clientdata, name, "Not a TIFF file, bad version number %d (0x%x)", tif.m_header.tiff_version, tif.m_header.tiff_version);
                return tif.safeOpenFailed();
            }

            tif.m_flags |= TIFF_MYBUFFER;
            tif.m_rawcp = 0;
            tif.m_rawdata = null;
            tif.m_rawdatasize = 0;

            /*
             * Sometimes we do not want to read the first directory (for example,
             * it may be broken) and want to proceed to other directories. I this
             * case we use the TIFF_HEADERONLY flag to open file and return
             * immediately after reading TIFF header.
             */
            if ((tif.m_flags & TIFF_HEADERONLY) != 0)
                return tif;

            /*
             * Setup initial directory.
             */
            switch (mode[0])
            {
                case 'r':
                    tif.m_nextdiroff = tif.m_header.tiff_diroff;
                    
                    if (tif.ReadDirectory())
                    {
                        tif.m_rawcc = -1;
                        tif.m_flags |= TIFF_BUFFERSETUP;
                        return tif;
                    }
                    break;
                case 'a':
                    /*
                     * New directories are automatically append
                     * to the end of the directory chain when they
                     * are written out (see TIFFWriteDirectory).
                     */
                    tif.setupDefaultDirectory();
                    return tif;
            }

            return tif.safeOpenFailed();
        }

        /*
         ** Stuff, related to tag handling and creating custom tags.
         */
        public int GetTagListCount()
        {
            return m_dir.td_customValueCount;
        }

        public uint GetTagListEntry(int tag_index)
        {
            if (tag_index < 0 || tag_index >= m_dir.td_customValueCount)
                return (uint)-1;
            else
                return m_dir.td_customValues[tag_index].info.field_tag;
        }

        public void MergeFieldInfo(TiffFieldInfo[] info, int n)
        {
            m_foundfield = null;

            if (m_nfields > 0)
                m_fieldinfo = Realloc(m_fieldinfo, m_nfields, m_nfields + n);
            else
                m_fieldinfo = new TiffFieldInfo [n];

            for (int i = 0; i < n; i++)
            {
                const TiffFieldInfo fip = FindFieldInfo(info[i].field_tag, info[i].field_type);

                /* only add definitions that aren't already present */
                if (fip == null)
                {
                    m_fieldinfo[m_nfields] = info[i];
                    m_nfields++;
                }
            }

            /* Sort the field info by tag number */
            qsort(m_fieldinfo, m_nfields, sizeof(TiffFieldInfo*), tagCompare);
        }

        public TiffFieldInfo FindFieldInfo(uint tag, TiffDataType dt)
        {
            if (m_foundfield && m_foundfield.field_tag == tag && (dt == TIFF_ANY || dt == m_foundfield.field_type))
                return m_foundfield;

            /* If we are invoked with no field information, then just return. */
            if (m_fieldinfo == null)
                return null;

            /* NB: use sorted search (e.g. binary search) */
            TiffFieldInfo key = new TiffFieldInfo(0, 0, 0, TIFF_NOTYPE, 0, false, false, null);
            key.field_tag = tag;
            key.field_type = dt;
            TiffFieldInfo* pkey = &key;

            const TiffFieldInfo** ret = (const TiffFieldInfo **) bsearch(&pkey, m_fieldinfo, m_nfields, sizeof(TiffFieldInfo*), tagCompare);
            return m_foundfield = (ret ? *ret : null);
        }

        public TiffFieldInfo FindFieldInfoByName(string field_name, TiffDataType dt)
        {
            if (m_foundfield && (strcmp(m_foundfield.field_name, field_name) == 0) && (dt == TIFF_ANY || dt == m_foundfield.field_type))
                return m_foundfield;

            /* If we are invoked with no field information, then just return. */
            if (m_fieldinfo == null)
                return null;

            /* NB: use sorted search (e.g. binary search) */
            TiffFieldInfo key(0, 0, 0, TIFF_NOTYPE, 0, false, false, null);
            key.field_name = (char*)field_name;
            key.field_type = dt;
            TiffFieldInfo* pkey = &key;

            const TiffFieldInfo** ret = (const TiffFieldInfo**)_lfind(&pkey, m_fieldinfo, &m_nfields, sizeof(TiffFieldInfo*), tagNameCompare);
            return m_foundfield = (ret ? *ret : null);
        }

        public TiffFieldInfo FieldWithTag(uint tag)
        {
            TiffFieldInfo fip = FindFieldInfo(tag, TIFF_ANY);
            if (fip == null)
            {
                ErrorExt(this, m_clientdata, "FieldWithTag", "Internal error, unknown tag 0x%x", tag);
                Debug.Assert(false);
                /*NOTREACHED*/
            }

            return fip;
        }

        public TiffFieldInfo FieldWithName(string field_name)
        {
            TiffFieldInfo fip = FindFieldInfoByName(field_name, TIFF_ANY);
            if (fip == null)
            {
                ErrorExt(this, m_clientdata, "FieldWithName", "Internal error, unknown tag %s", field_name);
                Debug.Assert(false);
                /*NOTREACHED*/
            }

            return fip;
        }

        public TiffTagMethods GetTagMethods()
        {
            return m_tagmethods;
        }

        public TiffTagMethods SetTagMethods(TiffTagMethods tagMethods)
        {
            TiffTagMethods oldTagMethods = m_tagmethods;

            if (tagMethods != null)
                m_tagmethods = tagMethods;

            return oldTagMethods;
        }

        public object GetClientInfo(string name)
        {
            // should get copy
            clientInfoLink link = m_clientinfo;

            while (link != null && strcmp(link.name, name) != 0)
                link = link.next;

            if (link != null)
                return link.data;

            return null;
        }

        public void SetClientInfo(object data, string name)
        {
            clientInfoLink link = m_clientinfo;

            /*
             ** Do we have an existing link with this name?  If so, just
             ** set it.
             */
            while (link != null && strcmp(link.name, name) != 0)
                link = link.next;

            if (link != null)
            {
                link.data = data;
                return;
            }

            /*
             ** Create a new link.
             */

            link = new clientInfoLink();
            Debug.Assert(link != null);
            link.next = m_clientinfo;
            link.name = new char[name.Length + 1];
            Debug.Assert(link.name != null);
            strcpy(link.name, name);
            link.data = data;

            m_clientinfo = link;
        }

        public bool Flush()
        {
            if (m_mode != O_RDONLY)
            {
                if (!FlushData())
                    return false;

                if ((m_flags & TIFF_DIRTYDIRECT) != 0 && !WriteDirectory())
                    return false;
            }

            return true;
        }
        
        /*
        * Flush buffered data to the file.
        *
        * Frank Warmerdam'2000: I modified this to return false if TIFF_BEENWRITING
        * is not set, so that TIFFFlush() will proceed to write out the directory.
        * The documentation says returning false is an error indicator, but not having
        * been writing isn't exactly a an error.  Hopefully this doesn't cause
        * problems for other people. 
        */
        public bool FlushData()
        {
            if ((m_flags & TIFF_BEENWRITING) == 0)
                return false;

            if ((m_flags & TIFF_POSTENCODE) != 0)
            {
                m_flags &= ~TIFF_POSTENCODE;
                if (!m_currentCodec.tif_postencode())
                    return false;
            }

            return flushData1();
        }
        
        /*
        * Return the value of a field in the
        * internal directory structure.
        */
        //public bool GetField(uint tag, ...);
        
        /*
        * Like GetField, but taking a varargs
        * parameter list.  This routine is useful
        * for building higher-level interfaces on
        * top of the library.
        */
        //public bool VGetField(uint tag, va_list ap);
        
        /*
        * Like GetField, but return any default
        * value if the tag is not present in the directory.
        */
        //public bool GetFieldDefaulted(uint tag, ...);
        
        /*
        * Like GetField, but return any default
        * value if the tag is not present in the directory.
        *
        * NB:  We use the value in the directory, rather than
        *  explicit values so that defaults exist only one
        *  place in the library -- in setupDefaultDirectory.
        */
        //public bool VGetFieldDefaulted(uint tag, va_list ap);

        /*
        * Read the next TIFF directory from a file
        * and convert it to the internal format.
        * We read directories sequentially.
        */
        public bool ReadDirectory()
        {
            const string module = "ReadDirectory";

            m_diroff = m_nextdiroff;
            if (m_diroff == 0)
            {
                /* no more directories */
                return false;
            }

            /*
            * Check whether we have the last offset or bad offset (IFD looping).
            */
            if (!checkDirOffset(m_nextdiroff))
                return false;

            /*
             * Cleanup any previous compression state.
             */
            m_currentCodec.tif_cleanup();
            m_curdir++;
            TiffDirEntry* dir = null;
            UInt16 dircount = fetchDirectory(m_nextdiroff, dir, m_nextdiroff);
            if (dircount == 0)
            {
                ErrorExt(this, m_clientdata, module, "%s: Failed to read directory at offset %u", m_name, m_nextdiroff);
                return false;
            }

            m_flags &= ~TIFF_BEENWRITING; /* reset before new dir */

            /*
             * Setup default value and then make a pass over
             * the fields to check type and tag information,
             * and to extract info required to size data
             * structures.  A second pass is made afterwards
             * to read in everthing not taken in the first pass.
             */
            
            /* free any old stuff and reinit */
            FreeDirectory();
            setupDefaultDirectory();

            /*
             * Electronic Arts writes gray-scale TIFF files
             * without a PlanarConfiguration directory entry.
             * Thus we setup a default value here, even though
             * the TIFF spec says there is no default value.
             */
            SetField(TIFFTAG_PLANARCONFIG, PLANARCONFIG_CONTIG);

            /*
             * Sigh, we must make a separate pass through the
             * directory for the following reason:
             *
             * We must process the Compression tag in the first pass
             * in order to merge in codec-private tag definitions (otherwise
             * we may get complaints about unknown tags).  However, the
             * Compression tag may be dependent on the SamplesPerPixel
             * tag value because older TIFF specs permited Compression
             * to be written as a SamplesPerPixel-count tag entry.
             * Thus if we don't first figure out the correct SamplesPerPixel
             * tag value then we may end up ignoring the Compression tag
             * value because it has an incorrect count value (if the
             * true value of SamplesPerPixel is not 1).
             *
             * It sure would have been nice if Aldus had really thought
             * this stuff through carefully.
             */
            for (int i = 0; i < dircount; i++)
            {
                TiffDirEntry* dp = &dir[i];
                if ((m_flags & TIFF_SWAB) != 0)
                {
                    SwabShort(ref dp.tdir_tag);
                    SwabShort(ref dp.tdir_type);
                    SwabLong(ref dp.tdir_count);
                    SwabLong(ref dp.tdir_offset);
                }
                
                if (dp.tdir_tag == TIFFTAG_SAMPLESPERPIXEL)
                {
                    if (!fetchNormalTag(dir[i]))
                        return readDirectoryFailed(dir);

                    dp.tdir_tag = TIFFTAG_IGNORE;
                }
            }

            /*
             * First real pass over the directory.
             */
            uint fix = 0;
            bool diroutoforderwarning = false;
            for (int i = 0; i < dircount; i++)
            {
                if (fix >= m_nfields || dir[i].tdir_tag == TIFFTAG_IGNORE)
                    continue;

                /*
                 * Silicon Beach (at least) writes unordered
                 * directory tags (violating the spec).  Handle
                 * it here, but be obnoxious (maybe they'll fix it?).
                 */
                if (dir[i].tdir_tag < m_fieldinfo[fix].field_tag)
                {
                    if (!diroutoforderwarning)
                    {
                        WarningExt(this, m_clientdata, module, "%s: invalid TIFF directory; tags are not sorted in ascending order", m_name);
                        diroutoforderwarning = true;
                    }

                    fix = 0; /* O(n^2) */
                }

                while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                    fix++;

                if (fix >= m_nfields || m_fieldinfo[fix].field_tag != dir[i].tdir_tag)
                {
                    WarningExt(this, m_clientdata, module, "%s: unknown field with tag %d (0x%x) encountered", m_name, dir[i].tdir_tag, dir[i].tdir_tag);

                    MergeFieldInfo(createAnonFieldInfo(dir[i].tdir_tag, (TiffDataType)dir[i].tdir_type), 1);
                    fix = 0;
                    while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                        fix++;
                }

                /*
                 * null out old tags that we ignore.
                 */
                if (m_fieldinfo[fix].field_bit == FIELD_IGNORE)
                {
                    dir[i].tdir_tag = TIFFTAG_IGNORE;
                    continue;
                }

                /*
                 * Check data type.
                 */
                TiffFieldInfo fip = m_fieldinfo[fix];
                while (dir[i].tdir_type != (unsigned short)fip.field_type && fix < m_nfields)
                {
                    if (fip.field_type == TIFF_ANY)
                    {
                        /* wildcard */
                        break;
                    }

                    fip = m_fieldinfo[++fix];
                    if (fix >= m_nfields || fip.field_tag != dir[i].tdir_tag)
                    {
                        WarningExt(this, m_clientdata, module, "%s: wrong data type %d for \"%s\"; tag ignored", m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].field_name);
                        dir[i].tdir_tag = TIFFTAG_IGNORE;
                        continue;
                    }
                }

                /*
                 * Check count if known in advance.
                 */
                if (fip.field_readcount != TIFF_VARIABLE && fip.field_readcount != TIFF_VARIABLE2)
                {
                    uint expected = (fip.field_readcount == TIFF_SPP) ? m_dir.td_samplesperpixel : fip.field_readcount;
                    if (!checkDirCount(dir[i], expected))
                    {
                        dir[i].tdir_tag = TIFFTAG_IGNORE;
                        continue;
                    }
                }

                switch (dir[i].tdir_tag)
                {
                    case TIFFTAG_COMPRESSION:
                        /*
                         * The 5.0 spec says the Compression tag has
                         * one value, while earlier specs say it has
                         * one value per sample.  Because of this, we
                         * accept the tag if one value is supplied.
                         */
                        if (dir[i].tdir_count == 1)
                        {
                            uint v = extractData(dir[i]);
                            if (!SetField(dir[i].tdir_tag, (UInt16)v))
                                return readDirectoryFailed(dir);
                            
                            break;
                            /* XXX: workaround for broken TIFFs */
                        }
                        else if (dir[i].tdir_type == TiffDataType.TIFF_LONG)
                        {
                            uint v;
                            if (!fetchPerSampleLongs(dir[i], v) || !SetField(dir[i].tdir_tag, (UInt16)v))
                                return readDirectoryFailed(dir);
                        }
                        else
                        {
                            UInt16 iv;
                            if (!fetchPerSampleShorts(dir[i], iv) || !SetField(dir[i].tdir_tag, iv))
                                return readDirectoryFailed(dir);
                        }
                        dir[i].tdir_tag = TIFFTAG_IGNORE;
                        break;
                    case TIFFTAG_STRIPOFFSETS:
                    case TIFFTAG_STRIPBYTECOUNTS:
                    case TIFFTAG_TILEOFFSETS:
                    case TIFFTAG_TILEBYTECOUNTS:
                        setFieldBit(fip.field_bit);
                        break;
                    case TIFFTAG_IMAGEWIDTH:
                    case TIFFTAG_IMAGELENGTH:
                    case TIFFTAG_IMAGEDEPTH:
                    case TIFFTAG_TILELENGTH:
                    case TIFFTAG_TILEWIDTH:
                    case TIFFTAG_TILEDEPTH:
                    case TIFFTAG_PLANARCONFIG:
                    case TIFFTAG_ROWSPERSTRIP:
                    case TIFFTAG_EXTRASAMPLES:
                        if (!fetchNormalTag(dir[i]))
                            return readDirectoryFailed(dir);
                        dir[i].tdir_tag = TIFFTAG_IGNORE;
                        break;
                }
            }

            /*
            * XXX: OJPEG hack.
            * If a) compression is OJPEG, b) planarconfig tag says it's separate,
            * c) strip offsets/bytecounts tag are both present and
            * d) both contain exactly one value, then we consistently find
            * that the buggy implementation of the buggy compression scheme
            * matches contig planarconfig best. So we 'fix-up' the tag here
            */
            if ((m_dir.td_compression == COMPRESSION_OJPEG) && (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)) 
            {
                int dpIndex = readDirectoryFind(dir, dircount, TIFFTAG_STRIPOFFSETS);
                if (dpIndex != -1 && dir[dpIndex].tdir_count == 1) 
                {
                    dpIndex = readDirectoryFind(dir, dircount, TIFFTAG_STRIPBYTECOUNTS);
                    if (dpIndex != -1 && dir[dpIndex].tdir_count == 1) 
                    {
                        m_dir.td_planarconfig = PLANARCONFIG_CONTIG;
                        WarningExt(this, m_clientdata, "ReadDirectory", "Planarconfig tag value assumed incorrect, assuming data is contig instead of chunky");
                    }
                }
            }

            /*
             * Allocate directory structure and setup defaults.
             */
            if (!fieldSet(FIELD_IMAGEDIMENSIONS))
            {
                missingRequired("ImageLength");
                return readDirectoryFailed(dir);
            }

            /* 
             * Setup appropriate structures (by strip or by tile)
             */
            if (!fieldSet(FIELD_TILEDIMENSIONS))
            {
                m_dir.td_nstrips = NumberOfStrips();
                m_dir.td_tilewidth = m_dir.td_imagewidth;
                m_dir.td_tilelength = m_dir.td_rowsperstrip;
                m_dir.td_tiledepth = m_dir.td_imagedepth;
                m_flags &= ~TIFF_ISTILED;
            }
            else
            {
                m_dir.td_nstrips = NumberOfTiles();
                m_flags |= TIFF_ISTILED;
            }

            if (m_dir.td_nstrips == 0)
            {
                ErrorExt(this, m_clientdata, module, "%s: cannot handle zero number of %s", m_name, IsTiled() ? "tiles" : "strips");
                return readDirectoryFailed(dir);
            }

            m_dir.td_stripsperimage = m_dir.td_nstrips;
            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                m_dir.td_stripsperimage /= m_dir.td_samplesperpixel;

            if (!fieldSet(FIELD_STRIPOFFSETS))
            {
                if ((m_dir.td_compression == COMPRESSION_OJPEG) && !IsTiled() && (m_dir.td_nstrips == 1)) 
                {
                    /*
                    * XXX: OJPEG hack.
                    * If a) compression is OJPEG, b) it's not a tiled TIFF,
                    * and c) the number of strips is 1,
                    * then we tolerate the absence of stripoffsets tag,
                    * because, presumably, all required data is in the
                    * JpegInterchangeFormat stream.
                    */
                    setFieldBit(FIELD_STRIPOFFSETS);
                } 
                else 
                {
                    missingRequired(IsTiled() ? "TileOffsets" : "StripOffsets");
                    return readDirectoryFailed(dir);
                }
            }

            /*
             * Second pass: extract other information.
             */
            for (int i = 0; i < dircount; i++)
            {
                if (dir[i].tdir_tag == TIFFTAG_IGNORE)
                    continue;
                
                switch (dir[i].tdir_tag)
                {
                    case TIFFTAG_MINSAMPLEVALUE:
                    case TIFFTAG_MAXSAMPLEVALUE:
                    case TIFFTAG_BITSPERSAMPLE:
                    case TIFFTAG_DATATYPE:
                    case TIFFTAG_SAMPLEFORMAT:
                        /*
                         * The 5.0 spec says the Compression tag has
                         * one value, while earlier specs say it has
                         * one value per sample.  Because of this, we
                         * accept the tag if one value is supplied.
                         *
                         * The MinSampleValue, MaxSampleValue, BitsPerSample
                         * DataType and SampleFormat tags are supposed to be
                         * written as one value/sample, but some vendors
                         * incorrectly write one value only -- so we accept
                         * that as well (yech). Other vendors write correct
                         * value for NumberOfSamples, but incorrect one for
                         * BitsPerSample and friends, and we will read this
                         * too.
                         */
                        if (dir[i].tdir_count == 1)
                        {
                            uint v = extractData(dir[i]);
                            if (!SetField(dir[i].tdir_tag, (UInt16)v))
                                return readDirectoryFailed(dir);
                            /* XXX: workaround for broken TIFFs */
                        }
                        else if (dir[i].tdir_tag == TIFFTAG_BITSPERSAMPLE && dir[i].tdir_type == TiffDataType.TIFF_LONG)
                        {
                            uint v;
                            if (!fetchPerSampleLongs(dir[i], v) || !SetField(dir[i].tdir_tag, (UInt16)v))
                                return readDirectoryFailed(dir);
                        }
                        else
                        {
                            UInt16 iv;
                            if (!fetchPerSampleShorts(dir[i], iv) || !SetField(dir[i].tdir_tag, iv))
                                return readDirectoryFailed(dir);
                        }
                        break;
                    case TIFFTAG_SMINSAMPLEVALUE:
                    case TIFFTAG_SMAXSAMPLEVALUE:
                        {
                            double dv = 0.0;
                            if (!fetchPerSampleAnys(dir[i], dv) || !SetField(dir[i].tdir_tag, dv))
                                return readDirectoryFailed(dir);
                        }
                        break;
                    case TIFFTAG_STRIPOFFSETS:
                    case TIFFTAG_TILEOFFSETS:
                        if (!fetchStripThing(dir[i], m_dir.td_nstrips, m_dir.td_stripoffset))
                            return readDirectoryFailed(dir);
                        break;
                    case TIFFTAG_STRIPBYTECOUNTS:
                    case TIFFTAG_TILEBYTECOUNTS:
                        if (!fetchStripThing(dir[i], m_dir.td_nstrips, m_dir.td_stripbytecount))
                            return readDirectoryFailed(dir);
                        break;
                    case TIFFTAG_COLORMAP:
                    case TIFFTAG_TRANSFERFUNCTION:
                        {
                            /*
                             * TransferFunction can have either 1x or 3x
                             * data values; Colormap can have only 3x
                             * items.
                             */
                            uint v = 1L << m_dir.td_bitspersample;
                            if (dir[i].tdir_tag == TIFFTAG_COLORMAP || dir[i].tdir_count != v)
                            {
                                if (!checkDirCount(dir[i], 3 * v))
                                    break;
                            }

                            byte[] cp = new byte [dir[i].tdir_count * sizeof(UInt16)];
                            if (cp == null)
                                ErrorExt(this, m_clientdata, m_name, "No space to read \"TransferFunction\" tag");

                            if (cp != null)
                            {
                                if (fetchData(dir[i], cp))
                                {
                                    uint c = 1L << m_dir.td_bitspersample;
                                    if (dir[i].tdir_count == c)
                                    {
                                        /*
                                        * This deals with there being
                                        * only one array to apply to
                                        * all samples.
                                        */
                                        UInt16[] u = byteArrayToUInt16(cp, 0, dir[i].tdir_count * sizeof(UInt16));
                                        SetField(dir[i].tdir_tag, u, u, u);
                                    }
                                    else
                                    {
                                        v *= sizeof(UInt16);
                                        UInt16[] u0 = byteArrayToUInt16(cp, 0, v);
                                        UInt16[] u1 = byteArrayToUInt16(cp, v, v);
                                        UInt16[] u2 = byteArrayToUInt16(cp, 2 * v, v);
                                        SetField(dir[i].tdir_tag, u0, u1, u2);
                                    }
                                }
                            }
                            break;
                        }
                    case TIFFTAG_PAGENUMBER:
                    case TIFFTAG_HALFTONEHINTS:
                    case TIFFTAG_YCBCRSUBSAMPLING:
                    case TIFFTAG_DOTRANGE:
                        fetchShortPair(dir[i]);
                        break;
                    case TIFFTAG_REFERENCEBLACKWHITE:
                        fetchRefBlackWhite(dir[i]);
                        break;
                        /* BEGIN REV 4.0 COMPATIBILITY */
                    case TIFFTAG_OSUBFILETYPE:
                        {
                            uint v = 0L;
                            switch (extractData(dir[i]))
                            {
                                case OFILETYPE_REDUCEDIMAGE:
                                    v = FILETYPE_REDUCEDIMAGE;
                                    break;
                                case OFILETYPE_PAGE:
                                    v = FILETYPE_PAGE;
                                    break;
                            }

                            if (v != 0)
                                SetField(TIFFTAG_SUBFILETYPE, v);
                        }
                        break;
                        /* END REV 4.0 COMPATIBILITY */
                    default:
                        fetchNormalTag(dir[i]);
                        break;
                }
            }

            /*
            * OJPEG hack:
            * - If a) compression is OJPEG, and b) photometric tag is missing,
            * then we consistently find that photometric should be YCbCr
            * - If a) compression is OJPEG, and b) photometric tag says it's RGB,
            * then we consistently find that the buggy implementation of the
            * buggy compression scheme matches photometric YCbCr instead.
            * - If a) compression is OJPEG, and b) bitspersample tag is missing,
            * then we consistently find bitspersample should be 8.
            * - If a) compression is OJPEG, b) samplesperpixel tag is missing,
            * and c) photometric is RGB or YCbCr, then we consistently find
            * samplesperpixel should be 3
            * - If a) compression is OJPEG, b) samplesperpixel tag is missing,
            * and c) photometric is MINISWHITE or MINISBLACK, then we consistently
            * find samplesperpixel should be 3
            */
            if (m_dir.td_compression == COMPRESSION_OJPEG)
            {
                if (!fieldSet(FIELD_PHOTOMETRIC))
                {
                    WarningExt(this, m_clientdata, "ReadDirectory", "Photometric tag is missing, assuming data is YCbCr");
                    if (!SetField(TIFFTAG_PHOTOMETRIC, PHOTOMETRIC_YCBCR))
                        return readDirectoryFailed(dir);
                }
                else if (m_dir.td_photometric == PHOTOMETRIC_RGB)
                {
                    m_dir.td_photometric = PHOTOMETRIC_YCBCR;
                    WarningExt(this, m_clientdata, "ReadDirectory", "Photometric tag value assumed incorrect, assuming data is YCbCr instead of RGB");
                }
                
                if (!fieldSet(FIELD_BITSPERSAMPLE))
                {
                    WarningExt(this, m_clientdata, "ReadDirectory", "BitsPerSample tag is missing, assuming 8 bits per sample");
                    if (!SetField(TIFFTAG_BITSPERSAMPLE, 8))
                        return readDirectoryFailed(dir);
                }

                if (!fieldSet(FIELD_SAMPLESPERPIXEL))
                {
                    if ((m_dir.td_photometric == PHOTOMETRIC_RGB) || (m_dir.td_photometric == PHOTOMETRIC_YCBCR))
                    {
                        WarningExt(this, m_clientdata, "ReadDirectory", "SamplesPerPixel tag is missing, assuming correct SamplesPerPixel value is 3");
                        if (!SetField(TIFFTAG_SAMPLESPERPIXEL, 3))
                            return readDirectoryFailed(dir);
                    }
                    else if ((m_dir.td_photometric == PHOTOMETRIC_MINISWHITE) || (m_dir.td_photometric == PHOTOMETRIC_MINISBLACK))
                    {
                        WarningExt(this, m_clientdata, "ReadDirectory", "SamplesPerPixel tag is missing, assuming correct SamplesPerPixel value is 1");
                        if (!SetField(TIFFTAG_SAMPLESPERPIXEL, 1))
                            return readDirectoryFailed(dir);
                    }
                }
            }

            /*
             * Verify Palette image has a Colormap.
             */
            if (m_dir.td_photometric == PHOTOMETRIC_PALETTE && !fieldSet(FIELD_COLORMAP))
            {
                missingRequired("Colormap");
                return readDirectoryFailed(dir);
            }

            /*
            * OJPEG hack:
            * We do no further messing with strip/tile offsets/bytecounts in OJPEG
            * TIFFs
            */
            if (m_dir.td_compression != COMPRESSION_OJPEG)
            {
                /*
                 * Attempt to deal with a missing StripByteCounts tag.
                 */
                if (!fieldSet(FIELD_STRIPBYTECOUNTS))
                {
                    /*
                     * Some manufacturers violate the spec by not giving
                     * the size of the strips.  In this case, assume there
                     * is one uncompressed strip of data.
                     */
                    if ((m_dir.td_planarconfig == PLANARCONFIG_CONTIG && m_dir.td_nstrips > 1) || 
                        (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE && m_dir.td_nstrips != m_dir.td_samplesperpixel))
                    {
                        missingRequired("StripByteCounts");
                        return readDirectoryFailed(dir);
                    }

                    WarningExt(this, m_clientdata, module, "%s: TIFF directory is missing required ""\"%s\" field, calculating from imagelength", m_name, FieldWithTag(TIFFTAG_STRIPBYTECOUNTS).field_name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return readDirectoryFailed(dir);
                }
                else if (m_dir.td_nstrips == 1 && m_dir.td_stripoffset[0] != 0 && byteCountLooksBad(m_dir))
                {
                    /*
                     * XXX: Plexus (and others) sometimes give a value of zero for
                     * a tag when they don't know what the correct value is!  Try
                     * and handle the simple case of estimating the size of a one
                     * strip image.
                     */
                    WarningExt(this, m_clientdata, module, "%s: Bogus \"%s\" field, ignoring and calculating from imagelength", m_name, FieldWithTag(TIFFTAG_STRIPBYTECOUNTS).field_name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return readDirectoryFailed(dir);
                }
                else if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG && m_dir.td_nstrips > 2 && m_dir.td_compression == COMPRESSION_NONE && m_dir.td_stripbytecount[0] != m_dir.td_stripbytecount[1])
                {
                    /*
                     * XXX: Some vendors fill StripByteCount array with absolutely
                     * wrong values (it can be equal to StripOffset array, for
                     * example). Catch this case here.
                     */
                    WarningExt(this, m_clientdata, module, "%s: Wrong \"%s\" field, ignoring and calculating from imagelength", m_name, FieldWithTag(TIFFTAG_STRIPBYTECOUNTS).field_name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return readDirectoryFailed(dir);
                }
            }

            dir = null;

            if (!fieldSet(FIELD_MAXSAMPLEVALUE))
                m_dir.td_maxsamplevalue = (UInt16)((1L << m_dir.td_bitspersample) - 1);

            /*
             * Setup default compression scheme.
             */

            /*
             * XXX: We can optimize checking for the strip bounds using the sorted
             * bytecounts array. See also comments for appendToStrip() function.
             */
            if (m_dir.td_nstrips > 1)
            {
                m_dir.td_stripbytecountsorted = 1;
                for (uint strip = 1; strip < m_dir.td_nstrips; strip++)
                {
                    if (m_dir.td_stripoffset[strip - 1] > m_dir.td_stripoffset[strip])
                    {
                        m_dir.td_stripbytecountsorted = 0;
                        break;
                    }
                }
            }

            if (!fieldSet(FIELD_COMPRESSION))
                SetField(TIFFTAG_COMPRESSION, COMPRESSION_NONE);

            /*
             * Some manufacturers make life difficult by writing
             * large amounts of uncompressed data as a single strip.
             * This is contrary to the recommendations of the spec.
             * The following makes an attempt at breaking such images
             * into strips closer to the recommended 8k bytes.  A
             * side effect, however, is that the RowsPerStrip tag
             * value may be changed.
             */
            if (m_dir.td_nstrips == 1 && m_dir.td_compression == COMPRESSION_NONE && (m_flags & (TIFF_STRIPCHOP | TIFF_ISTILED)) == TIFF_STRIPCHOP)
                chopUpSingleUncompressedStrip();

            /*
             * Reinitialize i/o since we are starting on a new directory.
             */
            m_row = (uint)-1;
            m_curstrip = (uint)-1;
            m_col = (uint)-1;
            m_curtile = (uint)-1;
            m_tilesize = (int)-1;

            m_scanlinesize = ScanlineSize();
            if (m_scanlinesize == 0)
            {
                ErrorExt(this, m_clientdata, module, "%s: cannot handle zero scanline size", m_name);
                return false;
            }

            if (IsTiled())
            {
                m_tilesize = TileSize();
                if (m_tilesize == 0)
                {
                    ErrorExt(this, m_clientdata, module, "%s: cannot handle zero tile size", m_name);
                    return false;
                }
            }
            else
            {
                if (StripSize() == 0)
                {
                    ErrorExt(this, m_clientdata, module, "%s: cannot handle zero strip size", m_name);
                    return false;
                }
            }

            return true;
        }
        
        /* 
        * Read custom directory from the arbitrary offset.
        * The code is very similar to ReadDirectory().
        */
        public bool ReadCustomDirectory(uint diroff, TiffFieldInfo[] info, uint n)
        {
            const string module = "ReadCustomDirectory";

            setupFieldInfo(info, n);

            uint dummyNextDirOff;
            TiffDirEntry* dir = null;
            UInt16 dircount = fetchDirectory(diroff, dir, dummyNextDirOff);
            if (dircount == 0)
            {
                ErrorExt(this, m_clientdata, module, "%s: Failed to read custom directory at offset %u", m_name, diroff);
                return false;
            }

            FreeDirectory();
            m_dir = new TiffDirectory();

            uint fix = 0;
            for (UInt16 i = 0; i < dircount; i++)
            {
                if ((m_flags & TIFF_SWAB) != 0)
                {
                    SwabShort(ref dir[i].tdir_tag);
                    SwabShort(ref dir[i].tdir_type);
                    SwabLong(ref dir[i].tdir_count);
                    SwabLong(ref dir[i].tdir_offset);
                }

                if (fix >= m_nfields || dir[i].tdir_tag == TIFFTAG_IGNORE)
                    continue;

                while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                    fix++;

                if (fix >= m_nfields || m_fieldinfo[fix].field_tag != dir[i].tdir_tag)
                {
                    WarningExt(this, m_clientdata, module, "%s: unknown field with tag %d (0x%x) encountered", m_name, dir[i].tdir_tag, dir[i].tdir_tag);

                    MergeFieldInfo(createAnonFieldInfo(dir[i].tdir_tag, (TiffDataType)dir[i].tdir_type), 1);

                    fix = 0;
                    while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                        fix++;
                }

                /*
                 * null out old tags that we ignore.
                 */
                if (m_fieldinfo[fix].field_bit == FIELD_IGNORE)
                {
                    dir[i].tdir_tag = TIFFTAG_IGNORE;
                    continue;
                }

                /*
                 * Check data type.
                 */
                TiffFieldInfo fip = m_fieldinfo[fix];
                while (dir[i].tdir_type != (unsigned short)fip.field_type && fix < m_nfields)
                {
                    if (fip.field_type == TIFF_ANY)
                    {
                        /* wildcard */
                        break;
                    }

                    fip = m_fieldinfo[++fix];
                    if (fix >= m_nfields || fip.field_tag != dir[i].tdir_tag)
                    {
                        WarningExt(this, m_clientdata, module, "%s: wrong data type %d for \"%s\"; tag ignored", m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].field_name);
                        dir[i].tdir_tag = TIFFTAG_IGNORE;
                        continue;
                    }
                }

                /*
                 * Check count if known in advance.
                 */
                if (fip.field_readcount != TIFF_VARIABLE && fip.field_readcount != TIFF_VARIABLE2)
                {
                    uint expected = (fip.field_readcount == TIFF_SPP) ? m_dir.td_samplesperpixel : fip.field_readcount;

                    if (!checkDirCount(dir[i], expected))
                    {
                        dir[i].tdir_tag = TIFFTAG_IGNORE;
                        continue;
                    }
                }
            
                /*
                * EXIF tags which need to be specifically processed.
                */
                switch (dir[i].tdir_tag) 
                {
                    case EXIFTAG_SUBJECTDISTANCE:
                        fetchSubjectDistance(dir[i]);
                        break;
                    default:
                        fetchNormalTag(dir[i]);
                        break;
                }
            }

            return true;
        }

        public bool WriteCustomDirectory(out uint pdiroff)
        {
            if (m_mode == O_RDONLY)
                return true;

            /*
            * Size the directory so that we can calculate
            * offsets for the data items that aren't kept
            * in-place in each field.
            */
            uint nfields = 0;
            for (unsigned int b = 0; b <= FIELD_LAST; b++)
            {
                if (fieldSet(b) && b != FIELD_CUSTOM)
                    nfields += (b < FIELD_SUBFILETYPE ? 2 : 1);
            }

            nfields += m_dir.td_customValueCount;
            int dirsize = nfields * sizeof (TiffDirEntry);
            TiffDirEntry* data = new TiffDirEntry[nfields];
            if (data == null) 
            {
                ErrorExt(this, m_clientdata, m_name, "Cannot write directory, out of space");
                return false;
            }

            /*
            * Put the directory  at the end of the file.
            */
            m_diroff = (seekFile(0, SEEK_END) + 1) & ~1;
            m_dataoff = (uint)(m_diroff + sizeof(UInt16) + dirsize + sizeof(uint));
            if ((m_dataoff & 1) != 0)
                m_dataoff++;

            seekFile(m_dataoff, SEEK_SET);
            TiffDirEntry* dir = data;
            
            /*
            * Setup external form of directory
            * entries and write data items.
            */
            unsigned int fields[TiffDirectory::FIELD_SETLONGS];
            memcpy(fields, m_dir.td_fieldsset, sizeof(unsigned int) * TiffDirectory::FIELD_SETLONGS);

            for (int fi = 0, nfi = m_nfields; nfi > 0; nfi--, fi++)
            {
                TiffFieldInfo fip = m_fieldinfo[fi];

                /*
                * For custom fields, we test to see if the custom field
                * is set or not.  For normal fields, we just use the
                * FieldSet test.
                */
                if (fip.field_bit == FIELD_CUSTOM)
                {
                    bool is_set = false;
                    for (int ci = 0; ci < m_dir.td_customValueCount; ci++)
                        is_set |= (m_dir.td_customValues[ci].info == fip);

                    if (!is_set)
                        continue;
                }
                else if (!fieldSet(fields, fip.field_bit))
                    continue;

                if (fip.field_bit != FIELD_CUSTOM)
                    resetFieldBit(fields, fip.field_bit);
            }

            /*
            * Write directory.
            */
            UInt16 dircount = (UInt16)nfields;
            pdiroff = m_nextdiroff;
            if ((m_flags & TIFF_SWAB) != 0)
            {
                /*
                * The file's byte order is opposite to the
                * native machine architecture.  We overwrite
                * the directory information with impunity
                * because it'll be released below after we
                * write it to the file.  Note that all the
                * other tag construction routines assume that
                * we do this byte-swapping; i.e. they only
                * byte-swap indirect data.
                */
                for (dir = data; dircount; dir++, dircount--)
                {
                    SwabShort(ref dir.tdir_tag);
                    SwabShort(ref dir.tdir_type);
                    SwabLong(ref dir.tdir_count);
                    SwabLong(ref dir.tdir_offset);
                }
                
                dircount = (UInt16) nfields;
                SwabShort(ref dircount);
                SwabLong(ref pdiroff);
            }

            seekFile(m_diroff, SEEK_SET);
            if (!writeUInt16OK(dircount))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory count");
                return false;
            }

            if (!writeDirEntryOK(data, dirsize / sizeof(TiffDirEntry)))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory contents");
                return false;
            }

            if (!writeIntOK(pdiroff))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory link");
                return false;
            }

            return true;
        }

        /*
        * EXIF is important special case of custom IFD, so we have a special
        * function to read it.
        */
        public bool ReadEXIFDirectory(uint diroff)
        {
            uint exifFieldInfoCount;
            const TiffFieldInfo* exifFieldInfo = getExifFieldInfo(exifFieldInfoCount);
            return ReadCustomDirectory(diroff, exifFieldInfo, exifFieldInfoCount);
        }

        /*
        * Return the number of bytes to read/write in a call to
        * one of the scanline-oriented i/o routines.  Note that
        * this number may be 1/samples-per-pixel if data is
        * stored as separate planes.
        */
        public int ScanlineSize()
        {
            int scanline;
            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG)
            {
                if (m_dir.td_photometric == PHOTOMETRIC_YCBCR && !IsUpSampled())
                {
                    UInt16 ycbcrsubsampling[2];
                    GetField(TIFFTAG_YCBCRSUBSAMPLING, &ycbcrsubsampling[0], &ycbcrsubsampling[1]);

                    if (ycbcrsubsampling[0] == 0)
                    {
                        ErrorExt(this, m_clientdata, m_name, "Invalid YCbCr subsampling");
                        return 0;
                    }

                    scanline = roundUp(m_dir.td_imagewidth, ycbcrsubsampling[0]);
                    scanline = howMany8(multiply(scanline, m_dir.td_bitspersample, "ScanlineSize"));
                    return summarize(scanline, multiply(2, scanline / ycbcrsubsampling[0], "VStripSize"), "VStripSize");
                }
                else
                {
                    scanline = multiply(m_dir.td_imagewidth, m_dir.td_samplesperpixel, "ScanlineSize");
                }
            }
            else
                scanline = m_dir.td_imagewidth;

            return howMany8(multiply(scanline, m_dir.td_bitspersample, "ScanlineSize"));
        }

        /*
        * Return the number of bytes required to store a complete
        * decoded and packed raster scanline (as opposed to the
        * I/O size returned by ScanlineSize which may be less
        * if data is store as separate planes).
        */
        public int RasterScanlineSize()
        {
            int scanline = multiply(m_dir.td_bitspersample, m_dir.td_imagewidth, "RasterScanlineSize");
            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG)
            {
                scanline = multiply(scanline, m_dir.td_samplesperpixel, "RasterScanlineSize");
                return howMany8(scanline);
            }
            
            return multiply(howMany8(scanline), m_dir.td_samplesperpixel, "RasterScanlineSize");
        }
        
        /*
        * Compute the # bytes in a (row-aligned) strip.
        *
        * Note that if RowsPerStrip is larger than the
        * recorded ImageLength, then the strip size is
        * truncated to reflect the actual space required
        * to hold the strip.
        */
        public int StripSize()
        {
            uint rps = m_dir.td_rowsperstrip;
            if (rps > m_dir.td_imagelength)
                rps = m_dir.td_imagelength;

            return VStripSize(rps);
        }
        
        /*
        * Compute the # bytes in a raw strip.
        */
        public int RawStripSize(uint strip)
        {
            int bytecount = m_dir.td_stripbytecount[strip];
            if (bytecount <= 0)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Invalid strip byte count, strip %lu", bytecount, strip);
                bytecount = (int)-1;
            }

            return bytecount;
        }
        
        /*
        * Compute the # bytes in a variable height, row-aligned strip.
        */
        public int VStripSize(uint nrows)
        {
            if (nrows == (uint)-1)
                nrows = m_dir.td_imagelength;

            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG && m_dir.td_photometric == PHOTOMETRIC_YCBCR && !IsUpSampled())
            {
                /*
                 * Packed YCbCr data contain one Cb+Cr for every
                 * HorizontalSampling * VerticalSampling Y values.
                 * Must also roundup width and height when calculating
                 * since images that are not a multiple of the
                 * horizontal/vertical subsampling area include
                 * YCbCr data for the extended image.
                 */
                UInt16 ycbcrsubsampling[2];
                GetField(TIFFTAG_YCBCRSUBSAMPLING, &ycbcrsubsampling[0], &ycbcrsubsampling[1]);

                int samplingarea = ycbcrsubsampling[0] * ycbcrsubsampling[1];
                if (samplingarea == 0)
                {
                    ErrorExt(this, m_clientdata, m_name, "Invalid YCbCr subsampling");
                    return 0;
                }

                int w = roundUp(m_dir.td_imagewidth, ycbcrsubsampling[0]);
                int scanline = howMany8(multiply(w, m_dir.td_bitspersample, "VStripSize"));
                nrows = roundUp(nrows, ycbcrsubsampling[1]);
                /* NB: don't need howMany here 'cuz everything is rounded */
                scanline = multiply(nrows, scanline, "VStripSize");
                return summarize(scanline, multiply(2, scanline / samplingarea, "VStripSize"), "VStripSize");
            }

            return multiply(nrows, ScanlineSize(), "VStripSize");
        }

        /*
        * Compute the # bytes in each row of a tile.
        */
        public int TileRowSize()
        {
            if (m_dir.td_tilelength == 0 || m_dir.td_tilewidth == 0)
                return 0;

            int rowsize = multiply(m_dir.td_bitspersample, m_dir.td_tilewidth, "TileRowSize");
            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG)
                rowsize = multiply(rowsize, m_dir.td_samplesperpixel, "TileRowSize");

            return howMany8(rowsize);
        }

        /*
        * Compute the # bytes in a row-aligned tile.
        */
        public int TileSize()
        {
            return VTileSize(m_dir.td_tilelength);
        }
                
        /*
        * Compute the # bytes in a variable length, row-aligned tile.
        */
        public int VTileSize(uint nrows)
        {
            if (m_dir.td_tilelength == 0 || m_dir.td_tilewidth == 0 || m_dir.td_tiledepth == 0)
                return 0;

            int tilesize;
            if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG && m_dir.td_photometric == PHOTOMETRIC_YCBCR && !IsUpSampled())
            {
                /*
                 * Packed YCbCr data contain one Cb+Cr for every
                 * HorizontalSampling*VerticalSampling Y values.
                 * Must also roundup width and height when calculating
                 * since images that are not a multiple of the
                 * horizontal/vertical subsampling area include
                 * YCbCr data for the extended image.
                 */
                int w = roundUp(m_dir.td_tilewidth, m_dir.td_ycbcrsubsampling[0]);
                int rowsize = howMany8(multiply(w, m_dir.td_bitspersample, "VTileSize"));
                int samplingarea = m_dir.td_ycbcrsubsampling[0] * m_dir.td_ycbcrsubsampling[1];
                if (samplingarea == 0)
                {
                    ErrorExt(this, m_clientdata, m_name, "Invalid YCbCr subsampling");
                    return 0;
                }

                nrows = roundUp(nrows, m_dir.td_ycbcrsubsampling[1]);
                /* NB: don't need howMany here 'cuz everything is rounded */
                tilesize = multiply(nrows, rowsize, "VTileSize");
                tilesize = summarize(tilesize, multiply(2, tilesize / samplingarea, "VTileSize"), "VTileSize");
            }
            else
                tilesize = multiply(nrows, TileRowSize(), "VTileSize");

            return multiply(tilesize, m_dir.td_tiledepth, "VTileSize");
        }

        /*
        * Compute a default strip size based on the image
        * characteristics and a requested value.  If the
        * request is <1 then we choose a strip size according
        * to certain heuristics.
        */
        public uint DefaultStripSize(uint request)
        {
            return m_currentCodec.tif_defstripsize(request);
        }

        /*
        * Compute a default tile size based on the image
        * characteristics and a requested value.  If a
        * request is <1 then we choose a size according
        * to certain heuristics.
        */
        public void DefaultTileSize(ref uint tw, ref uint th)
        {
            m_currentCodec.tif_deftilesize(tw, th);
        }
        
        /*
        * Return open file's clientdata.
        */
        public thandle_t Clientdata()
        {
            return m_clientdata;
        }

        /*
        * Set open file's clientdata, and return previous value.
        */
        public thandle_t SetClientdata(thandle_t newvalue)
        {
            thandle_t m = m_clientdata;
            m_clientdata = newvalue;
            return m;
        }

        /*
        * Return read/write mode.
        */
        public int GetMode()
        {
            return m_mode;
        }

        /*
        * Return read/write mode.
        */
        public int SetMode(int mode)
        {
            int old_mode = m_mode;
            m_mode = mode;
            return old_mode;
        }

        /*
        * Return nonzero if file is organized in
        * tiles; zero if organized as strips.
        */
        public bool IsTiled()
        {
            return ((m_flags & TIFF_ISTILED) != 0);
        }

        /*
        * Return nonzero if the file has byte-swapped data.
        */
        public bool IsByteSwapped()
        {
            return ((m_flags & TIFF_SWAB) != 0);
        }

        /*
        * Return nonzero if the data is returned up-sampled.
        */
        public bool IsUpSampled()
        {
            return ((m_flags & TIFF_UPSAMPLED) != 0);
        }

        /*
        * Return nonzero if the data is returned in MSB-to-LSB bit order.
        */
        public bool IsMSB2LSB()
        {
            return isFillOrder(FILLORDER_MSB2LSB);
        }

        /*
        * Return nonzero if given file was written in big-endian order.
        */
        public bool IsBigEndian()
        {
            return (m_header.tiff_magic == TIFF_BIGENDIAN);
        }

        public TiffStream GetStream()
        {
            return m_stream;
        }

        /*
        * Return current row being read/written.
        */
        public uint CurrentRow()
        {
            return m_row;
        }

        /*
        * Return index of the current directory.
        */
        public UInt16 CurrentDirectory()
        {
            return m_curdir;
        }

        /*
        * Count the number of directories in a file.
        */
        public UInt16 NumberOfDirectories()
        {
            uint nextdir = m_header.tiff_diroff;
            UInt16 n = 0;
            uint dummyOff;
            while (nextdir != 0 && advanceDirectory(nextdir, dummyOff))
                n++;

            return n;
        }

        /*
        * Return file offset of the current directory.
        */
        public uint CurrentDirOffset()
        {
            return m_diroff;
        }

        /*
        * Return current strip.
        */
        public uint CurrentStrip()
        {
            return m_curstrip;
        }

        /*
        * Return current tile.
        */
        public uint CurrentTile()
        {
            return m_curtile;
        }

        /*
        * Setup the raw data buffer in preparation for
        * reading a strip of raw data.  If the buffer
        * is specified as zero, then a buffer of appropriate
        * size is allocated by the library.  Otherwise,
        * the client must guarantee that the buffer is
        * large enough to hold any individual strip of
        * raw data.
        */
        public bool ReadBufferSetup(byte[] bp, int size)
        {
            const string module = "ReadBufferSetup";
            
            Debug.Assert((m_flags & TIFF_NOREADRAW) == 0);

            m_rawdata = null;
            
            if (bp != null)
            {
                m_rawdatasize = size;
                m_rawdata = bp;
                m_flags &= ~TIFF_MYBUFFER;
            }
            else
            {
                m_rawdatasize = roundUp(size, 1024);
                m_rawdata = new byte [m_rawdatasize];
                m_flags |= TIFF_MYBUFFER;
            }
            
            if (m_rawdata == null)
            {
                ErrorExt(this, m_clientdata, module, "%s: No space for data buffer at scanline %ld", m_name, m_row);
                m_rawdatasize = 0;
                return false;
            }

            return true;
        }

        /*
        * Setup the raw data buffer used for encoding.
        */
        public bool WriteBufferSetup(byte[] bp, int size)
        {
            const string module = "WriteBufferSetup";

            if (m_rawdata != null)
            {
                if ((m_flags & TIFF_MYBUFFER) != 0)
                    m_flags &= ~TIFF_MYBUFFER;

                m_rawdata = null;
            }
            
            if (size == (int)-1)
            {
                size = (IsTiled() ? m_tilesize : StripSize());

                /*
                 * Make raw data buffer at least 8K
                 */
                if (size < 8 * 1024)
                    size = 8 * 1024;

                bp = null; /* NB: force allocation */
            }
            
            if (bp == null)
            {
                bp = new byte [size];
                if (bp == null)
                {
                    ErrorExt(this, m_clientdata, module, "%s: No space for output buffer", m_name);
                    return false;
                }

                m_flags |= TIFF_MYBUFFER;
            }
            else
                m_flags &= ~TIFF_MYBUFFER;
            
            m_rawdata = bp;
            m_rawdatasize = size;
            m_rawcc = 0;
            m_rawcp = 0;
            m_flags |= TIFF_BUFFERSETUP;
            return true;
        }

        public bool SetupStrips()
        {
            if (IsTiled())
                m_dir.td_stripsperimage = isUnspecified(FIELD_TILEDIMENSIONS) ? m_dir.td_samplesperpixel : NumberOfTiles();
            else
                m_dir.td_stripsperimage = isUnspecified(FIELD_ROWSPERSTRIP) ? m_dir.td_samplesperpixel : NumberOfStrips();

            m_dir.td_nstrips = m_dir.td_stripsperimage;

            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                m_dir.td_stripsperimage /= m_dir.td_samplesperpixel;

            m_dir.td_stripoffset = new uint[m_dir.td_nstrips];
            m_dir.td_stripbytecount = new uint[m_dir.td_nstrips];
            if (m_dir.td_stripoffset == null || m_dir.td_stripbytecount == null)
                return false;

            /*
             * Place data at the end-of-file
             * (by setting offsets to zero).
             */
            memset(m_dir.td_stripoffset, 0, m_dir.td_nstrips * sizeof(uint));
            memset(m_dir.td_stripbytecount, 0, m_dir.td_nstrips * sizeof(uint));
            setFieldBit(FIELD_STRIPOFFSETS);
            setFieldBit(FIELD_STRIPBYTECOUNTS);
            return true;
        }
        
        /*
        * Verify file is writable and that the directory
        * information is setup properly.  In doing the latter
        * we also "freeze" the state of the directory so
        * that important information is not changed.
        */
        public bool WriteCheck(int tiles, string module)
        {
            if (m_mode == O_RDONLY)
            {
                ErrorExt(this, m_clientdata, module, "%s: File not open for writing", m_name);
                return false;
            }

            if (tiles ^ (int)IsTiled())
            {
                ErrorExt(this, m_clientdata, m_name, tiles ? "Can not write tiles to a stripped image": "Can not write scanlines to a tiled image");
                return false;
            }

            /*
             * On the first write verify all the required information
             * has been setup and initialize any data structures that
             * had to wait until directory information was set.
             * Note that a lot of our work is assumed to remain valid
             * because we disallow any of the important parameters
             * from changing after we start writing (i.e. once
             * TIFF_BEENWRITING is set, TIFFSetField will only allow
             * the image's length to be changed).
             */
            if (!fieldSet(FIELD_IMAGEDIMENSIONS))
            {
                ErrorExt(this, m_clientdata, module, "%s: Must set \"ImageWidth\" before writing data", m_name);
                return false;
            }

            if (m_dir.td_samplesperpixel == 1)
            {
                /* 
                 * Planarconfiguration is irrelevant in case of single band
                 * images and need not be included. We will set it anyway,
                 * because this field is used in other parts of library even
                 * in the single band case.
                 */
                if (!fieldSet(FIELD_PLANARCONFIG))
                    m_dir.td_planarconfig = PLANARCONFIG_CONTIG;
            }
            else
            {
                if (!fieldSet(FIELD_PLANARCONFIG))
                {
                    ErrorExt(this, m_clientdata, module, "%s: Must set \"PlanarConfiguration\" before writing data", m_name);
                    return false;
                }
            }

            if (m_dir.td_stripoffset == null && !SetupStrips())
            {
                m_dir.td_nstrips = 0;
                ErrorExt(this, m_clientdata, module, "%s: No space for %s arrays", m_name, IsTiled() ? "tile" : "strip");
                return false;
            }

            m_tilesize = IsTiled() ? TileSize() : (int)-1;
            m_scanlinesize = ScanlineSize();
            m_flags |= TIFF_BEENWRITING;
            return true;
        }
        
        /*
        * Release storage associated with a directory.
        */
        public void FreeDirectory()
        {
            if (m_dir != null)
            {
                clearFieldBit(FIELD_YCBCRSUBSAMPLING);
                clearFieldBit(FIELD_YCBCRPOSITIONING);

                m_dir = null;
            }
        }

        /*
        * Setup for a new directory.  Should we automatically call
        * WriteDirectory() if the current one is dirty?
        *
        * The newly created directory will not exist on the file till
        * WriteDirectory(), Flush() or Close() is called.
        */
        public void CreateDirectory()
        {
            setupDefaultDirectory();
            m_diroff = 0;
            m_nextdiroff = 0;
            m_curoff = 0;
            m_row = (uint)-1;
            m_curstrip = (uint)-1;
        }
        
        /*
        * Return an indication of whether or not we are
        * at the last directory in the file.
        */
        public bool LastDirectory()
        {
            return (m_nextdiroff == 0);
        }
        
        /*
        * Set the n-th directory as the current directory.
        * NB: Directories are numbered starting at 0.
        */
        public bool SetDirectory(UInt16 dirn)
        {
            UInt16 n;
            uint dummyOff;
            uint nextdir = m_header.tiff_diroff;
            for (n = dirn; n > 0 && nextdir != 0; n--)
            {
                if (!advanceDirectory(nextdir, dummyOff))
                    return false;
            }

            m_nextdiroff = nextdir;

            /*
             * Set curdir to the actual directory index.  The
             * -1 is because ReadDirectory will increment
             * m_curdir after successfully reading the directory.
             */
            m_curdir = (dirn - n) - 1;

            /*
             * Reset m_dirnumber counter and start new list of seen directories.
             * We need this to prevent IFD loops.
             */
            m_dirnumber = 0;
            return ReadDirectory();
        }

        /*
        * Set the current directory to be the directory
        * located at the specified file offset.  This interface
        * is used mainly to access directories linked with
        * the SubIFD tag (e.g. thumbnail images).
        */
        public bool SetSubDirectory(uint diroff)
        {
            m_nextdiroff = diroff;
            /*
             * Reset m_dirnumber counter and start new list of seen directories.
             * We need this to prevent IFD loops.
             */
            m_dirnumber = 0;
            return ReadDirectory();
        }

        /*
        * Unlink the specified directory from the directory chain.
        */
        public bool UnlinkDirectory(UInt16 dirn)
        {
            const string module = "UnlinkDirectory";

            if (m_mode == O_RDONLY)
            {
                ErrorExt(this, m_clientdata, module, "Can not unlink directory in read-only file");
                return false;
            }

            /*
             * Go to the directory before the one we want
             * to unlink and nab the offset of the link
             * field we'll need to patch.
             */
            uint nextdir = m_header.tiff_diroff;
            uint off = sizeof(UInt16) + sizeof(UInt16);
            for (UInt16 n = dirn - 1; n > 0; n--)
            {
                if (nextdir == 0)
                {
                    ErrorExt(this, m_clientdata, module, "Directory %d does not exist", dirn);
                    return false;
                }

                if (!advanceDirectory(nextdir, off))
                    return false;
            }

            /*
             * Advance to the directory to be unlinked and fetch
             * the offset of the directory that follows.
             */
            uint dummyOff;
            if (!advanceDirectory(nextdir, dummyOff))
                return false;

            /*
             * Go back and patch the link field of the preceding
             * directory to point to the offset of the directory
             * that follows.
             */
            seekFile(off, SEEK_SET);
            if ((m_flags & TIFF_SWAB) != 0)
                SwabLong(ref nextdir);
            
            if (!writeIntOK(nextdir))
            {
                ErrorExt(this, m_clientdata, module, "Error writing directory link");
                return false;
            }

            /*
             * Leave directory state setup safely.  We don't have
             * facilities for doing inserting and removing directories,
             * so it's safest to just invalidate everything.  This
             * means that the caller can only append to the directory
             * chain.
             */
            m_currentCodec.tif_cleanup();
            if ((m_flags & TIFF_MYBUFFER) != 0 && m_rawdata != null)
            {
                m_rawdata = null;
                m_rawcc = 0;
            }
            
            m_flags &= ~(TIFF_BEENWRITING | TIFF_BUFFERSETUP | TIFF_POSTENCODE);
            FreeDirectory();
            setupDefaultDirectory();
            m_diroff = 0; /* force link on next write */
            m_nextdiroff = 0; /* next write must be at end */
            m_curoff = 0;
            m_row = (uint)-1;
            m_curstrip = (uint)-1;
            return true;
        }
        
        /*
        * Record the value of a field in the
        * internal directory structure.  The
        * field will be written to the file
        * when/if the directory structure is
        * updated.
        */
        public bool SetField(uint tag, params object[] ap)
        {
            va_list ap;

            va_start(ap, tag);
            bool status = VSetField(tag, ap);
            va_end(ap);
            return status;
        }

        /*
        * Like SetField, but taking a varargs
        * parameter list.  This routine is useful
        * for building higher-level interfaces on
        * top of the library.
        */
        //public bool VSetField(uint tag, va_list ap);

        public bool WriteDirectory()
        {
            return writeDirectory(true);
        }
        
        /*
        * Similar to WriteDirectory(), writes the directory out
        * but leaves all data structures in memory so that it can be
        * written again.  This will make a partially written TIFF file
        * readable before it is successfully completed/closed.
        */
        public bool CheckpointDirectory()
        {
            /* Setup the strips arrays, if they haven't already been. */
            if (m_dir.td_stripoffset == null)
                SetupStrips();

            bool rc = writeDirectory(false);
            SetWriteOffset(seekFile(0, SEEK_END));
            return rc;
        }

        /*
        * Similar to WriteDirectory(), but if the directory has already
        * been written once, it is relocated to the end of the file, in case it
        * has changed in size.  Note that this will result in the loss of the 
        * previously used directory space. 
        */
        public bool RewriteDirectory()
        {
            const string module = "RewriteDirectory";

            /* We don't need to do anything special if it hasn't been written. */
            if (m_diroff == 0)
                return WriteDirectory();

            /*
             ** Find and zero the pointer to this directory, so that linkDirectory
             ** will cause it to be added after this directories current pre-link.
             */

            /* Is it the first directory in the file? */
            if (m_header.tiff_diroff == m_diroff)
            {
                m_header.tiff_diroff = 0;
                m_diroff = 0;

                seekFile((uint)(TiffHeader::TIFF_MAGIC_SIZE + TiffHeader::TIFF_VERSION_SIZE), SEEK_SET);
                if (!writeIntOK(m_header.tiff_diroff))
                {
                    ErrorExt(this, m_clientdata, m_name, "Error updating TIFF header");
                    return false;
                }
            }
            else
            {
                uint nextdir = m_header.tiff_diroff;
                do
                {
                    UInt16 dircount;
                    if (!seekOK(nextdir) || !readUInt16OK(out dircount))
                    {
                        ErrorExt(this, m_clientdata, module, "Error fetching directory count");
                        return false;
                    }
                    
                    if ((m_flags & TIFF_SWAB) != 0)
                        SwabShort(ref dircount);

                    seekFile(dircount * sizeof(TiffDirEntry), SEEK_CUR);
                    
                    if (!readIntOK(out nextdir))
                    {
                        ErrorExt(this, m_clientdata, module, "Error fetching directory link");
                        return false;
                    }

                    if ((m_flags & TIFF_SWAB) != 0)
                        SwabLong(ref nextdir);
                }
                while (nextdir != m_diroff && nextdir != 0);

                uint off = seekFile(0, SEEK_CUR); /* get current offset */
                seekFile(off - (uint)sizeof(uint), SEEK_SET);
                m_diroff = 0;
                
                if (!writeIntOK(m_diroff))
                {
                    ErrorExt(this, m_clientdata, module, "Error writing directory link");
                    return false;
                }
            }

            /*
             ** Now use WriteDirectory() normally.
             */
            return WriteDirectory();
        }
        
        /*
        * Print the contents of the current directory
        * to the specified stdio file stream.
        */
        public void PrintDirectory(Stream fd)
        {
            PrintDirectory(fd, TIFFPRINT_NONE);
        }

        public void PrintDirectory(Stream fd, TiffPrintDirectoryFlags flags)
        {
            fprintf(fd, "TIFF Directory at offset 0x%lx (%lu)\n", m_diroff, m_diroff);
    
            if (fieldSet(FIELD_SUBFILETYPE))
            {
                fprintf(fd, "  Subfile Type:");
                char* sep = " ";
                if (m_dir.td_subfiletype & FILETYPE_REDUCEDIMAGE)
                {
                    fprintf(fd, "%sreduced-resolution image", sep);
                    sep = "/";
                }

                if (m_dir.td_subfiletype & FILETYPE_PAGE)
                {
                    fprintf(fd, "%smulti-page document", sep);
                    sep = "/";
                }
                
                if (m_dir.td_subfiletype & FILETYPE_MASK)
                    fprintf(fd, "%stransparency mask", sep);
                
                fprintf(fd, " (%lu = 0x%lx)\n", m_dir.td_subfiletype, m_dir.td_subfiletype);
            }

            if (fieldSet(FIELD_IMAGEDIMENSIONS))
            {
                fprintf(fd, "  Image Width: %lu Image Length: %lu", m_dir.td_imagewidth, m_dir.td_imagelength);
                if (fieldSet(FIELD_IMAGEDEPTH))
                    fprintf(fd, " Image Depth: %lu", m_dir.td_imagedepth);
                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD_TILEDIMENSIONS))
            {
                fprintf(fd, "  Tile Width: %lu Tile Length: %lu", m_dir.td_tilewidth, m_dir.td_tilelength);
                if (fieldSet(FIELD_TILEDEPTH))
                    fprintf(fd, " Tile Depth: %lu", m_dir.td_tiledepth);
                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD_RESOLUTION))
            {
                fprintf(fd, "  Resolution: %g, %g", m_dir.td_xresolution, m_dir.td_yresolution);
                if (fieldSet(FIELD_RESOLUTIONUNIT))
                {
                    switch (m_dir.td_resolutionunit)
                    {
                        case RESUNIT_NONE:
                            fprintf(fd, " (unitless)");
                            break;
                        case RESUNIT_INCH:
                            fprintf(fd, " pixels/inch");
                            break;
                        case RESUNIT_CENTIMETER:
                            fprintf(fd, " pixels/cm");
                            break;
                        default:
                            fprintf(fd, " (unit %u = 0x%x)", m_dir.td_resolutionunit, m_dir.td_resolutionunit);
                            break;
                    }
                }
                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD_POSITION))
                fprintf(fd, "  Position: %g, %g\n", m_dir.td_xposition, m_dir.td_yposition);
            
            if (fieldSet(FIELD_BITSPERSAMPLE))
                fprintf(fd, "  Bits/Sample: %u\n", m_dir.td_bitspersample);
            
            if (fieldSet(FIELD_SAMPLEFORMAT))
            {
                fprintf(fd, "  Sample Format: ");
                switch (m_dir.td_sampleformat)
                {
                    case SAMPLEFORMAT_VOID:
                        fprintf(fd, "void\n");
                        break;
                    case SAMPLEFORMAT_INT:
                        fprintf(fd, "signed integer\n");
                        break;
                    case SAMPLEFORMAT_UINT:
                        fprintf(fd, "unsigned integer\n");
                        break;
                    case SAMPLEFORMAT_IEEEFP:
                        fprintf(fd, "IEEE floating point\n");
                        break;
                    case SAMPLEFORMAT_COMPLEXINT:
                        fprintf(fd, "complex signed integer\n");
                        break;
                    case SAMPLEFORMAT_COMPLEXIEEEFP:
                        fprintf(fd, "complex IEEE floating point\n");
                        break;
                    default:
                        fprintf(fd, "%u (0x%x)\n", m_dir.td_sampleformat, m_dir.td_sampleformat);
                        break;
                }
            }

            if (fieldSet(FIELD_COMPRESSION))
            {
                const TiffCodec* c = FindCodec(m_dir.td_compression);
                fprintf(fd, "  Compression Scheme: ");
                if (c != null)
                    fprintf(fd, "%s\n", c.m_name);
                else
                    fprintf(fd, "%u (0x%x)\n", m_dir.td_compression, m_dir.td_compression);
            }

            if (fieldSet(FIELD_PHOTOMETRIC))
            {
                fprintf(fd, "  Photometric Interpretation: ");
                if (m_dir.td_photometric < (sizeof(photoNames) / sizeof(photoNames[0])))
                    fprintf(fd, "%s\n", photoNames[m_dir.td_photometric]);
                else
                {
                    switch (m_dir.td_photometric)
                    {
                        case PHOTOMETRIC_LOGL:
                            fprintf(fd, "CIE Log2(L)\n");
                            break;
                        case PHOTOMETRIC_LOGLUV:
                            fprintf(fd, "CIE Log2(L) (u',v')\n");
                            break;
                        default:
                            fprintf(fd, "%u (0x%x)\n", m_dir.td_photometric, m_dir.td_photometric);
                            break;
                    }
                }
            }

            if (fieldSet(FIELD_EXTRASAMPLES) && m_dir.td_extrasamples)
            {
                fprintf(fd, "  Extra Samples: %u<", m_dir.td_extrasamples);
                char* sep = "";
                for (UInt16 i = 0; i < m_dir.td_extrasamples; i++)
                {
                    switch (m_dir.td_sampleinfo[i])
                    {
                        case EXTRASAMPLE_UNSPECIFIED:
                            fprintf(fd, "%sunspecified", sep);
                            break;
                        case EXTRASAMPLE_ASSOCALPHA:
                            fprintf(fd, "%sassoc-alpha", sep);
                            break;
                        case EXTRASAMPLE_UNASSALPHA:
                            fprintf(fd, "%sunassoc-alpha", sep);
                            break;
                        default:
                            fprintf(fd, "%s%u (0x%x)", sep, m_dir.td_sampleinfo[i], m_dir.td_sampleinfo[i]);
                            break;
                    }
                    sep = ", ";
                }
                fprintf(fd, ">\n");
            }

            if (fieldSet(FIELD_INKNAMES))
            {
                char* cp;
                fprintf(fd, "  Ink Names: ");
                UInt16 i = m_dir.td_samplesperpixel;
                char* sep = "";
                for (cp = m_dir.td_inknames; i > 0; cp = strchr(cp, '\0') + 1, i--)
                {
                    fputs(sep, fd);
                    printAscii(fd, cp);
                    sep = ", ";
                }
                fputs("\n", fd);
            }

            if (fieldSet(FIELD_THRESHHOLDING))
            {
                fprintf(fd, "  Thresholding: ");
                switch (m_dir.td_threshholding)
                {
                    case THRESHHOLD_BILEVEL:
                        fprintf(fd, "bilevel art scan\n");
                        break;
                    case THRESHHOLD_HALFTONE:
                        fprintf(fd, "halftone or dithered scan\n");
                        break;
                    case THRESHHOLD_ERRORDIFFUSE:
                        fprintf(fd, "error diffused\n");
                        break;
                    default:
                        fprintf(fd, "%u (0x%x)\n", m_dir.td_threshholding, m_dir.td_threshholding);
                        break;
                }
            }

            if (fieldSet(FIELD_FILLORDER))
            {
                fprintf(fd, "  FillOrder: ");
                switch (m_dir.td_fillorder)
                {
                    case FILLORDER_MSB2LSB:
                        fprintf(fd, "msb-to-lsb\n");
                        break;
                    case FILLORDER_LSB2MSB:
                        fprintf(fd, "lsb-to-msb\n");
                        break;
                    default:
                        fprintf(fd, "%u (0x%x)\n", m_dir.td_fillorder, m_dir.td_fillorder);
                        break;
                }
            }

            if (fieldSet(FIELD_YCBCRSUBSAMPLING))
            {
                /*
                 * For hacky reasons (see tif_jpeg.c - JPEGFixupTestSubsampling),
                 * we need to fetch this rather than trust what is in our
                 * structures.
                 */
                UInt16 subsampling[2];
                GetField(TIFFTAG_YCBCRSUBSAMPLING, &subsampling[0], &subsampling[1]);
                fprintf(fd, "  YCbCr Subsampling: %u, %u\n", subsampling[0], subsampling[1]);
            }

            if (fieldSet(FIELD_YCBCRPOSITIONING))
            {
                fprintf(fd, "  YCbCr Positioning: ");
                switch (m_dir.td_ycbcrpositioning)
                {
                    case YCBCRPOSITION_CENTERED:
                        fprintf(fd, "centered\n");
                        break;
                    case YCBCRPOSITION_COSITED:
                        fprintf(fd, "cosited\n");
                        break;
                    default:
                        fprintf(fd, "%u (0x%x)\n", m_dir.td_ycbcrpositioning, m_dir.td_ycbcrpositioning);
                        break;
                }
            }

            if (fieldSet(FIELD_HALFTONEHINTS))
                fprintf(fd, "  Halftone Hints: light %u dark %u\n", m_dir.td_halftonehints[0], m_dir.td_halftonehints[1]);
            
            if (fieldSet(FIELD_ORIENTATION))
            {
                fprintf(fd, "  Orientation: ");
                if (m_dir.td_orientation < (sizeof(orientNames) / sizeof(orientNames[0])))
                    fprintf(fd, "%s\n", orientNames[m_dir.td_orientation]);
                else
                    fprintf(fd, "%u (0x%x)\n", m_dir.td_orientation, m_dir.td_orientation);
            }

            if (fieldSet(FIELD_SAMPLESPERPIXEL))
                fprintf(fd, "  Samples/Pixel: %u\n", m_dir.td_samplesperpixel);
            
            if (fieldSet(FIELD_ROWSPERSTRIP))
            {
                fprintf(fd, "  Rows/Strip: ");
                if (m_dir.td_rowsperstrip == (uint)-1)
                    fprintf(fd, "(infinite)\n");
                else
                    fprintf(fd, "%lu\n", m_dir.td_rowsperstrip);
            }

            if (fieldSet(FIELD_MINSAMPLEVALUE))
                fprintf(fd, "  Min Sample Value: %u\n", m_dir.td_minsamplevalue);
            
            if (fieldSet(FIELD_MAXSAMPLEVALUE))
                fprintf(fd, "  Max Sample Value: %u\n", m_dir.td_maxsamplevalue);
            
            if (fieldSet(FIELD_SMINSAMPLEVALUE))
                fprintf(fd, "  SMin Sample Value: %g\n", m_dir.td_sminsamplevalue);
            
            if (fieldSet(FIELD_SMAXSAMPLEVALUE))
                fprintf(fd, "  SMax Sample Value: %g\n", m_dir.td_smaxsamplevalue);
            
            if (fieldSet(FIELD_PLANARCONFIG))
            {
                fprintf(fd, "  Planar Configuration: ");
                switch (m_dir.td_planarconfig)
                {
                    case PLANARCONFIG_CONTIG:
                        fprintf(fd, "single image plane\n");
                        break;
                    case PLANARCONFIG_SEPARATE:
                        fprintf(fd, "separate image planes\n");
                        break;
                    default:
                        fprintf(fd, "%u (0x%x)\n", m_dir.td_planarconfig, m_dir.td_planarconfig);
                        break;
                }
            }

            if (fieldSet(FIELD_PAGENUMBER))
                fprintf(fd, "  Page Number: %u-%u\n", m_dir.td_pagenumber[0], m_dir.td_pagenumber[1]);
            
            if (fieldSet(FIELD_COLORMAP))
            {
                fprintf(fd, "  Color Map: ");
                if ((flags & TIFFPRINT_COLORMAP) != 0)
                {
                    fprintf(fd, "\n");
                    int n = 1L << m_dir.td_bitspersample;
                    for (int l = 0; l < n; l++)
                        fprintf(fd, "   %5lu: %5u %5u %5u\n", l, m_dir.td_colormap[0][l], m_dir.td_colormap[1][l], m_dir.td_colormap[2][l]);
                }
                else
                    fprintf(fd, "(present)\n");
            }

            if (fieldSet(FIELD_TRANSFERFUNCTION))
            {
                fprintf(fd, "  Transfer Function: ");
                if ((flags & TIFFPRINT_CURVES) != null)
                {
                    fprintf(fd, "\n");
                    int n = 1L << m_dir.td_bitspersample;
                    for (int l = 0; l < n; l++)
                    {
                        fprintf(fd, "    %2lu: %5u", l, m_dir.td_transferfunction[0][l]);
                        for (UInt16 i = 1; i < m_dir.td_samplesperpixel; i++)
                            fprintf(fd, " %5u", m_dir.td_transferfunction[i][l]);
                        fputc('\n', fd);
                    }
                }
                else
                    fprintf(fd, "(present)\n");
            }

            if (fieldSet(FIELD_SUBIFD) && m_dir.td_subifd != null)
            {
                fprintf(fd, "  SubIFD Offsets:");
                for (UInt16 i = 0; i < m_dir.td_nsubifd; i++)
                    fprintf(fd, " %5lu", m_dir.td_subifd[i]);
                fputc('\n', fd);
            }

            /*
             ** Custom tag support.
             */
            int count = GetTagListCount();
            for (int i = 0; i < count; i++)
            {
                uint tag = GetTagListEntry(i);
                TiffFieldInfo fip = FieldWithTag(tag);
                if (fip == null)
                    continue;

                bool mem_alloc = false;
                byte[] raw_data = null;
                uint value_count;
                if (fip.field_passcount)
                {
                    if (GetField(tag, &value_count, &raw_data) != 1)
                        continue;
                }
                else
                {
                    if (fip.field_readcount == TIFF_VARIABLE || fip.field_readcount == TIFF_VARIABLE2)
                        value_count = 1;
                    else if (fip.field_readcount == TIFF_SPP)
                        value_count = m_dir.td_samplesperpixel;
                    else
                        value_count = fip.field_readcount;

                    if ((fip.field_type == TiffDataType.TIFF_ASCII || fip.field_readcount == TIFF_VARIABLE || fip.field_readcount == TIFF_VARIABLE2 || fip.field_readcount == TIFF_SPP || value_count > 1) && fip.field_tag != TIFFTAG_PAGENUMBER && fip.field_tag != TIFFTAG_HALFTONEHINTS && fip.field_tag != TIFFTAG_YCBCRSUBSAMPLING && fip.field_tag != TIFFTAG_DOTRANGE)
                    {
                        if (GetField(tag, &raw_data) != 1)
                            continue;
                    }
                    else if (fip.field_tag != TIFFTAG_PAGENUMBER && fip.field_tag != TIFFTAG_HALFTONEHINTS && fip.field_tag != TIFFTAG_YCBCRSUBSAMPLING && fip.field_tag != TIFFTAG_DOTRANGE)
                    {
                        raw_data = new byte [dataSize(fip.field_type) * value_count];
                        mem_alloc = true;
                        if (GetField(tag, raw_data) != 1)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        /* 
                         * XXX: Should be fixed and removed, see the
                         * notes related to TIFFTAG_PAGENUMBER,
                         * TIFFTAG_HALFTONEHINTS,
                         * TIFFTAG_YCBCRSUBSAMPLING and
                         * TIFFTAG_DOTRANGE tags in tif_dir.c. */
                        raw_data = new byte [dataSize(fip.field_type) * value_count];
                        mem_alloc = true;
                        if (GetField(tag, raw_data, &raw_data[dataSize(fip.field_type)]) != 1)
                        {
                            continue;
                        }
                    }
                }

                /*
                 * Catch the tags which needs to be specially handled and
                 * pretty print them. If tag not handled in
                 * prettyPrintField() fall down and print it as any other
                 * tag.
                 */
                if (prettyPrintField(fd, tag, value_count, raw_data))
                {
                    continue;
                }
                else
                    printField(fd, fip, value_count, raw_data);
            }

            m_tagmethods.printdir(this, fd, flags);

            if ((flags & TIFFPRINT_STRIPS) != 0 && fieldSet(FIELD_STRIPOFFSETS))
            {
                fprintf(fd, "  %lu %s:\n", m_dir.td_nstrips, IsTiled() ? "Tiles" : "Strips");
                for (uint s = 0; s < m_dir.td_nstrips; s++)
                    fprintf(fd, "    %3lu: [%8lu, %8lu]\n", s, m_dir.td_stripoffset[s], m_dir.td_stripbytecount[s]);
            }
        }

        public bool ReadScanline(byte[] buf, uint row)
        {
            return ReadScanline(buf, row, 0);
        }

        public bool ReadScanline(byte[] buf, uint row, UInt16 sample)
        {
            if (!checkRead(0))
                return false;

            bool e = seek(row, sample);
            if (e)
            {
                /*
                 * Decompress desired row into user buffer.
                 */
                e = m_currentCodec.tif_decoderow(buf, m_scanlinesize, sample);

                /* we are now poised at the beginning of the next row */
                m_row = row + 1;

                if (e)
                    postDecode(buf, m_scanlinesize);
            }

            return e;
        }

        public bool WriteScanline(byte[] buf, uint row)
        {
            return WriteScanline(buf, row, 0);
        }

        public bool WriteScanline(byte[] buf, uint row, UInt16 sample)
        {
            const string module = "WriteScanline";

            if (!writeCheckStrips(module))
                return false;

            /*
             * Handle delayed allocation of data buffer.  This
             * permits it to be sized more intelligently (using
             * directory information).
             */
            if (!bufferCheck())
                return false;
            
            /*
             * Extend image length if needed
             * (but only for PlanarConfig=1).
             */
            bool imagegrew = false;
            if (row >= m_dir.td_imagelength)
            {
                /* extend image */
                if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                {
                    ErrorExt(this, m_clientdata, m_name, "Can not change \"ImageLength\" when using separate planes");
                    return false;
                }

                m_dir.td_imagelength = row + 1;
                imagegrew = true;
            }
            /*
             * Calculate strip and check for crossings.
             */
            uint strip;
            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name, "%d: Sample out of range, max %d", sample, m_dir.td_samplesperpixel);
                    return false;
                }

                strip = sample * m_dir.td_stripsperimage + row / m_dir.td_rowsperstrip;
            }
            else
                strip = row / m_dir.td_rowsperstrip;
            /*
             * Check strip array to make sure there's space. We don't support
             * dynamically growing files that have data organized in separate
             * bitplanes because it's too painful.  In that case we require that
             * the imagelength be set properly before the first write (so that the
             * strips array will be fully allocated above).
             */
            if (strip >= m_dir.td_nstrips && !growStrips(1, module))
                return false;

            if (strip != m_curstrip)
            {
                /*
                 * Changing strips -- flush any data present.
                 */
                if (!FlushData())
                    return false;

                m_curstrip = strip;

                /*
                 * Watch out for a growing image.  The value of strips/image
                 * will initially be 1 (since it can't be deduced until the
                 * imagelength is known).
                 */
                if (strip >= m_dir.td_stripsperimage && imagegrew)
                    m_dir.td_stripsperimage = howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip);
                
                m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
                if ((m_flags & TIFF_CODERSETUP) == 0)
                {
                    if (!m_currentCodec.tif_setupencode())
                        return false;

                    m_flags |= TIFF_CODERSETUP;
                }

                m_rawcc = 0;
                m_rawcp = 0;

                if (m_dir.td_stripbytecount[strip] > 0)
                {
                    /* if we are writing over existing tiles, zero length */
                    m_dir.td_stripbytecount[strip] = 0;

                    /* this forces appendToStrip() to do a seek */
                    m_curoff = 0;
                }

                if (!m_currentCodec.tif_preencode(sample))
                    return false;

                m_flags |= TIFF_POSTENCODE;
            }

            /*
             * Ensure the write is either sequential or at the
             * beginning of a strip (or that we can randomly
             * access the data -- i.e. no encoding).
             */
            if (row != m_row)
            {
                if (row < m_row)
                {
                    /*
                     * Moving backwards within the same strip:
                     * backup to the start and then decode
                     * forward (below).
                     */
                    m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
                    m_rawcp = 0;
                }
                /*
                 * Seek forward to the desired row.
                 */
                if (!m_currentCodec.tif_seek(row - m_row))
                    return false;

                m_row = row;
            }

            /* swab if needed - note that source buffer will be altered */
            postDecode(buf, m_scanlinesize);

            bool status = m_currentCodec.tif_encoderow(buf, m_scanlinesize, sample);

            /* we are now poised at the beginning of the next row */
            m_row = row + 1;
            return status;
        }
        
        /*
        * Read the specified image into an ABGR-format raster. Use bottom left
        * origin for raster by default.
        */
        public bool ReadRGBAImage(uint rwidth, uint rheight, uint[] raster)
        {
            return ReadRGBAImage(rwidth, rheight, raster, false);
        }

        public bool ReadRGBAImage(uint rwidth, uint rheight, uint[] raster, bool stop)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, ORIENTATION_BOTLEFT, stop);
        }
        
        /*
        * Read the specified image into an ABGR-format raster taking in account
        * specified orientation.
        */
        public bool ReadRGBAImageOriented(uint rwidth, uint rheight, uint[] raster)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, ORIENTATION_BOTLEFT, false);
        }

        public bool ReadRGBAImageOriented(uint rwidth, uint rheight, uint[] raster, int orientation)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, orientation, false);
        }

        public bool ReadRGBAImageOriented(uint rwidth, uint rheight, uint[] raster, int orientation, bool stop)
        {
            char emsg[1024] = "";
            bool ok = true;
            if (RGBAImageOK(emsg))
            {
                TiffRGBAImage* img = TiffRGBAImage::Create(this, stop, emsg);
                if (img != null)
                {
                    img.req_orientation = (UInt16)orientation;
                    /* XXX verify rwidth and rheight against width and height */
                    ok = img.Get(raster, (rheight - img.height) * rwidth, rwidth, img.height);
                    delete img;
                }
            }
            else
            {
                ErrorExt(this, m_clientdata, FileName(), emsg);
                ok = false;
            }

            return ok;
        }

        /*
        * Read a whole strip off data from the file, and convert to RGBA form.
        * If this is the last strip, then it will only contain the portion of
        * the strip that is actually within the image space.  The result is
        * organized in bottom to top form.
        */
        public bool ReadRGBAStrip(uint row, uint[] raster)
        {
            if (IsTiled())
            {
                ErrorExt(this, m_clientdata, FileName(), "Can't use ReadRGBAStrip() with tiled file.");
                return false;
            }

            uint rowsperstrip;
            GetFieldDefaulted(TIFFTAG_ROWSPERSTRIP, &rowsperstrip);
            if ((row % rowsperstrip) != 0)
            {
                ErrorExt(this, m_clientdata, FileName(), "Row passed to ReadRGBAStrip() must be first in a strip.");
                return false;
            }

            bool ok = false;
            char emsg[1024] = "";
            if (RGBAImageOK(emsg))
            {
                TiffRGBAImage* img = TiffRGBAImage::Create(this, 0, emsg);
                if (img != null)
                {
                    img.row_offset = row;
                    img.col_offset = 0;

                    uint rows_to_read = rowsperstrip;
                    if (row + rowsperstrip > img.height)
                        rows_to_read = img.height - row;

                    ok = img.Get(raster, 0, img.width, rows_to_read);

                    delete img;
                }

                return true;
            }

            ErrorExt(this, m_clientdata, FileName(), emsg);
            return false;
        }

        /*
        * Read a whole tile off data from the file, and convert to RGBA form.
        * The returned RGBA data is organized from bottom to top of tile,
        * and may include zeroed areas if the tile extends off the image.
        */
        public bool ReadRGBATile(uint col, uint row, uint[] raster)
        {
            /*
             * Verify that our request is legal - on a tile file, and on a
             * tile boundary.
             */

            if (!IsTiled())
            {
                ErrorExt(this, m_clientdata, FileName(), "Can't use ReadRGBATile() with stripped file.");
                return false;
            }

            uint tile_xsize;
            GetFieldDefaulted(TIFFTAG_TILEWIDTH, &tile_xsize);
            uint tile_ysize;
            GetFieldDefaulted(TIFFTAG_TILELENGTH, &tile_ysize);

            if ((col % tile_xsize) != 0 || (row % tile_ysize) != 0)
            {
                ErrorExt(this, m_clientdata, FileName(), "Row/col passed to ReadRGBATile() must be top""left corner of a tile.");
                return false;
            }

            /*
             * Setup the RGBA reader.
             */
            char emsg[1024] = "";
            TiffRGBAImage* img = TiffRGBAImage::Create(this, 0, emsg);
            if (!RGBAImageOK(emsg) || !img)
            {
                if (img != null)
                    delete img;

                ErrorExt(this, m_clientdata, FileName(), emsg);
                return false;
            }

            /*
             * The TIFFRGBAImageGet() function doesn't allow us to get off the
             * edge of the image, even to fill an otherwise valid tile.  So we
             * figure out how much we can read, and fix up the tile buffer to
             * a full tile configuration afterwards.
             */
            uint read_ysize;
            if (row + tile_ysize > img.height)
                read_ysize = img.height - row;
            else
                read_ysize = tile_ysize;

            uint read_xsize;
            if (col + tile_xsize > img.width)
                read_xsize = img.width - col;
            else
                read_xsize = tile_xsize;

            /*
             * Read the chunk of imagery.
             */

            img.row_offset = row;
            img.col_offset = col;

            bool ok = img.Get(raster, 0, read_xsize, read_ysize);

            delete img;

            /*
             * If our read was incomplete we will need to fix up the tile by
             * shifting the data around as if a full tile of data is being returned.
             *
             * This is all the more complicated because the image is organized in
             * bottom to top format. 
             */

            if (read_xsize == tile_xsize && read_ysize == tile_ysize)
                return ok;

            for (uint i_row = 0; i_row < read_ysize; i_row++)
            {
                memmove(&raster[(tile_ysize - i_row - 1) * tile_xsize], &raster[(read_ysize - i_row - 1) * read_xsize], read_xsize * sizeof(uint));
                memset(&raster[(tile_ysize - i_row - 1) * tile_xsize + read_xsize], 0, sizeof(uint) * (tile_xsize - read_xsize));
            }

            for (uint i_row = read_ysize; i_row < tile_ysize; i_row++)
            {
                memset(&raster[(tile_ysize - i_row - 1) * tile_xsize], 0, sizeof(uint) * tile_xsize);
            }

            return ok;
        }
        
        /*
        * Check the image to see if ReadRGBAImage can deal with it.
        * true/false is returned according to whether or not the image can
        * be handled.  If false is returned, emsg contains the reason
        * why it is being rejected.
        */
        public bool RGBAImageOK(out string emsg)
        {
            if (!m_decodestatus)
            {
                sprintf(emsg, "Sorry, requested compression method is not configured");
                return false;
            }

            switch (m_dir.td_bitspersample)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                case 16:
                    break;
                default:
                    sprintf(emsg, "Sorry, can not handle images with %d-bit samples", m_dir.td_bitspersample);
                    return false;
            }
            
            int colorchannels = m_dir.td_samplesperpixel - m_dir.td_extrasamples;
            UInt16 photometric;
            if (!GetField(TIFFTAG_PHOTOMETRIC, &photometric))
            {
                switch (colorchannels)
                {
                    case 1:
                        photometric = PHOTOMETRIC_MINISBLACK;
                        break;
                    case 3:
                        photometric = PHOTOMETRIC_RGB;
                        break;
                    default:
                        sprintf(emsg, "Missing needed %s tag", TiffRGBAImage::photoTag);
                        return false;
                }
            }

            switch (photometric)
            {
                case PHOTOMETRIC_MINISWHITE:
                case PHOTOMETRIC_MINISBLACK:
                case PHOTOMETRIC_PALETTE:
                    if (m_dir.td_planarconfig == PLANARCONFIG_CONTIG && m_dir.td_samplesperpixel != 1 && m_dir.td_bitspersample < 8)
                    {
                        sprintf(emsg, "Sorry, can not handle contiguous data with %s=%d, ""and %s=%d and Bits/Sample=%d", TiffRGBAImage::photoTag, photometric, "Samples/pixel", m_dir.td_samplesperpixel, m_dir.td_bitspersample);
                        return false;
                    }
                    /*
                     * We should likely validate that any extra samples are either
                     * to be ignored, or are alpha, and if alpha we should try to use
                     * them.  But for now we won't bother with this. 
                     */
                    break;
                case PHOTOMETRIC_YCBCR:
                    /*
                    * TODO: if at all meaningful and useful, make more complete
                    * support check here, or better still, refactor to let supporting
                    * code decide whether there is support and what meaningfull
                    * error to return
                    */
                    break;
                case PHOTOMETRIC_RGB:
                    if (colorchannels < 3)
                    {
                        sprintf(emsg, "Sorry, can not handle RGB image with %s=%d", "Color channels", colorchannels);
                        return false;
                    }
                    break;
                case PHOTOMETRIC_SEPARATED:
                    {
                        UInt16 inkset;
                        GetFieldDefaulted(TIFFTAG_INKSET, &inkset);
                        if (inkset != INKSET_CMYK)
                        {
                            sprintf(emsg, "Sorry, can not handle separated image with %s=%d", "InkSet", inkset);
                            return false;
                        }
                        if (m_dir.td_samplesperpixel < 4)
                        {
                            sprintf(emsg, "Sorry, can not handle separated image with %s=%d", "Samples/pixel", m_dir.td_samplesperpixel);
                            return false;
                        }
                        break;
                    }
                case PHOTOMETRIC_LOGL:
                    if (m_dir.td_compression != COMPRESSION_SGILOG)
                    {
                        sprintf(emsg, "Sorry, LogL data must have %s=%d", "Compression", COMPRESSION_SGILOG);
                        return false;
                    }
                    break;
                case PHOTOMETRIC_LOGLUV:
                    if (m_dir.td_compression != COMPRESSION_SGILOG && m_dir.td_compression != COMPRESSION_SGILOG24)
                    {
                        sprintf(emsg, "Sorry, LogLuv data must have %s=%d or %d", "Compression", COMPRESSION_SGILOG, COMPRESSION_SGILOG24);
                        return false;
                    }
                    if (m_dir.td_planarconfig != PLANARCONFIG_CONTIG)
                    {
                        sprintf(emsg, "Sorry, can not handle LogLuv images with %s=%d", "Planarconfiguration", m_dir.td_planarconfig);
                        return false;
                    }
                    break;
                case PHOTOMETRIC_CIELAB:
                    break;
                default:
                    sprintf(emsg, "Sorry, can not handle image with %s=%d", TiffRGBAImage::photoTag, photometric);
                    return false;
            }

            return true;
        }

        /*
        * Return open file's name.
        */
        public string FileName()
        {
            return m_name;
        }

        /*
        * Set the file name.
        */
        public string SetFileName(string name)
        {
            const char* old_name = m_name;
            m_name = (char*)name;
            return old_name;
        }

        // "tif" parameter can be null
        public static void Error(Tiff tif, string module, string fmt, params object[] ap)
        {
            va_list ap;
            va_start(ap, fmt);

            m_errorHandler.ErrorHandler(tif, module, fmt, ap);
            m_errorHandler.ErrorHandlerExt(tif, 0, module, fmt, ap);

            va_end(ap);
        }

        public static void ErrorExt(Tiff tif, thandle_t fd, string module, string fmt, params object[] ap)
        {
            va_list ap;
            va_start(ap, fmt);

            m_errorHandler.ErrorHandler(tif, module, fmt, ap);
            m_errorHandler.ErrorHandlerExt(tif, fd, module, fmt, ap);

            va_end(ap);
        }

        public static void Warning(Tiff tif, string module, string fmt, params object[] ap)
        {
            va_list ap;
            va_start(ap, fmt);

            m_errorHandler.WarningHandler(tif, module, fmt, ap);
            m_errorHandler.WarningHandlerExt(tif, 0, module, fmt, ap);

            va_end(ap);
        }

        public static void WarningExt(Tiff tif, thandle_t fd, string module, string fmt, params object[] ap)
        {
            va_list ap;
            va_start(ap, fmt);

            m_errorHandler.WarningHandler(tif, module, fmt, ap);
            m_errorHandler.WarningHandlerExt(tif, fd, module, fmt, ap);

            va_end(ap);
        }

        public static void Error(string module, string fmt, params object[] ap)
        {
            Error(null, module, fmt, ap);
        }

        public static void ErrorExt(thandle_t fd, string module, string fmt, params object[] ap)
        {
            ErrorExt(null, fd, module, fmt, ap);
        }

        public static void Warning(string module, string fmt, params object[] ap)
        {
            Warning(null, module, fmt, ap);
        }

        public static void WarningExt(thandle_t fd, string module, string fmt, params object[] ap)
        {
            WarningExt(null, fd, module, fmt, ap);
        }

        public static TiffErrorHandler SetErrorHandler(TiffErrorHandler errorHandler)
        {
            TiffErrorHandler* prev = m_errorHandler;
            m_errorHandler = errorHandler;
            return prev;
        }

        //public static TiffExtendProc SetTagExtender(TiffExtendProc);

        /*
        * Compute which tile an (x,y,z,s) value is in.
        */
        public uint ComputeTile(uint x, uint y, uint z, UInt16 s)
        {
            if (m_dir.td_imagedepth == 1)
                z = 0;

            uint dx = m_dir.td_tilewidth;
            if (dx == (uint)-1)
                dx = m_dir.td_imagewidth;

            uint dy = m_dir.td_tilelength;
            if (dy == (uint)-1)
                dy = m_dir.td_imagelength;

            uint dz = m_dir.td_tiledepth;
            if (dz == (uint)-1)
                dz = m_dir.td_imagedepth;

            uint tile = 1;
            if (dx != 0 && dy != 0 && dz != 0)
            {
                uint xpt = howMany(m_dir.td_imagewidth, dx);
                uint ypt = howMany(m_dir.td_imagelength, dy);
                uint zpt = howMany(m_dir.td_imagedepth, dz);

                if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                    tile = (xpt * ypt * zpt) * s + (xpt * ypt) * (z / dz) + xpt * (y / dy) + x / dx;
                else
                    tile = (xpt * ypt) * (z / dz) + xpt * (y / dy) + x / dx;
            }

            return tile;
        }

        /*
        * Check an (x,y,z,s) coordinate
        * against the image bounds.
        */
        public bool CheckTile(uint x, uint y, uint z, UInt16 s)
        {
            if (x >= m_dir.td_imagewidth)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Col out of range, max %lu", x, m_dir.td_imagewidth - 1);
                return false;
            }

            if (y >= m_dir.td_imagelength)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Row out of range, max %lu", y, m_dir.td_imagelength - 1);
                return false;
            }

            if (z >= m_dir.td_imagedepth)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Depth out of range, max %lu", z, m_dir.td_imagedepth - 1);
                return false;
            }

            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE && s >= m_dir.td_samplesperpixel)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Sample out of range, max %lu", s, m_dir.td_samplesperpixel - 1);
                return false;
            }

            return true;
        }

        /*
        * Compute how many tiles are in an image.
        */
        public uint NumberOfTiles()
        {
            uint dx = m_dir.td_tilewidth;
            if (dx == (uint)-1)
                dx = m_dir.td_imagewidth;
            
            uint dy = m_dir.td_tilelength;
            if (dy == (uint)-1)
                dy = m_dir.td_imagelength;
            
            uint dz = m_dir.td_tiledepth;
            if (dz == (uint)-1)
                dz = m_dir.td_imagedepth;
            
            uint ntiles = (dx == 0 || dy == 0 || dz == 0) ? 0 : multiply(multiply(howMany(m_dir.td_imagewidth, dx), howMany(m_dir.td_imagelength, dy), "NumberOfTiles"), howMany(m_dir.td_imagedepth, dz), "NumberOfTiles");
            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                ntiles = multiply(ntiles, m_dir.td_samplesperpixel, "NumberOfTiles");
            
            return ntiles;
        }
        
        /*
        * Tile-oriented Read Support
        * Contributed by Nancy Cam (Silicon Graphics).
        */

        /*
        * Read and decompress a tile of data.  The
        * tile is selected by the (x,y,z,s) coordinates.
        */
        public int ReadTile(byte[] buf, int offset, uint x, uint y, uint z, UInt16 s)
        {
            if (!checkRead(1) || !CheckTile(x, y, z, s))
                return -1;

            return ReadEncodedTile(ComputeTile(x, y, z, s), buf, offset, -1);
        }

        /*
        * Read a tile of data and decompress the specified
        * amount into the user-supplied buffer.
        */
        public int ReadEncodedTile(uint tile, byte[] buf, int offset, int size)
        {
            if (!checkRead(1))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "%ld: Tile out of range, max %ld", tile, m_dir.td_nstrips);
                return -1;
            }
            
            if (size == (int)-1)
                size = m_tilesize;
            else if (size > m_tilesize)
                size = m_tilesize;
            
            byte[] tempBuf = new byte [size];
            memcpy(tempBuf, &buf[offset], size);

            if (fillTile(tile) && m_currentCodec.tif_decodetile(tempBuf, size, (UInt16)(tile / m_dir.td_stripsperimage)))
            {
                postDecode(tempBuf, size);
                memcpy(&buf[offset], tempBuf, size);
                return size;
            }

            return -1;
        }

        /*
        * Read a tile of data from the file.
        */
        public int ReadRawTile(uint tile, byte[] buf, int offset, int size)
        {
            const string module = "ReadRawTile";
    
            /*
            * FIXME: bytecount should have int type, but for now libtiff
            * defines int as a signed 32-bit integer and we are losing
            * ability to read arrays larger than 2^31 bytes. So we are using
            * uint instead of int here.
            */
            if (!checkRead(1))
                return -1;
            
            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Tile out of range, max %lu", tile, m_dir.td_nstrips);
                return -1;
            }
            
            if ((m_flags & TIFF_NOREADRAW) != 0)
            {
                ErrorExt(m_clientdata, m_name, "Compression scheme does not support access to raw uncompressed data");
                return -1;
            }

            uint bytecount = m_dir.td_stripbytecount[tile];
            if (size != (int)-1 && (uint)size < bytecount)
                bytecount = size;
            
            return readRawTile1(tile, buf, offset, bytecount, module);
        }

        /*
        * Write and compress a tile of data.  The
        * tile is selected by the (x,y,z,s) coordinates.
        */
        public int WriteTile(byte[] buf, uint x, uint y, uint z, UInt16 s)
        {
            if (!CheckTile(x, y, z, s))
                return -1;

            /*
             * NB: A tile size of -1 is used instead of m_tilesize knowing
             *     that WriteEncodedTile will clamp this to the tile size.
             *     This is done because the tile size may not be defined until
             *     after the output buffer is setup in WriteBufferSetup.
             */
            return WriteEncodedTile(ComputeTile(x, y, z, s), buf, -1);
        }
        
        /*
        * Compute which strip a (row,sample) value is in.
        */
        public uint ComputeStrip(uint row, UInt16 sample)
        {
            uint strip = row / m_dir.td_rowsperstrip;
            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name, "%lu: Sample out of range, max %lu", sample, m_dir.td_samplesperpixel);
                    return 0;
                }

                strip += sample * m_dir.td_stripsperimage;
            }

            return strip;
        }

        /*
        * Compute how many strips are in an image.
        */
        public uint NumberOfStrips()
        {
            uint nstrips = (m_dir.td_rowsperstrip == (uint)-1 ? 1: howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip));
            if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                nstrips = multiply(nstrips, m_dir.td_samplesperpixel, "NumberOfStrips");

            return nstrips;
        }
        
        /*
        * Read a strip of data and decompress the specified
        * amount into the user-supplied buffer.
        */
        public int ReadEncodedStrip(uint strip, byte[] buf, int offset, int size)
        {
            if (!checkRead(0))
                return -1;

            if (strip >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "%ld: Strip out of range, max %ld", strip, m_dir.td_nstrips);
                return -1;
            }

            /*
             * Calculate the strip size according to the number of
             * rows in the strip (check for truncated last strip on any
             * of the separations).
             */
            uint strips_per_sep;
            if (m_dir.td_rowsperstrip >= m_dir.td_imagelength)
                strips_per_sep = 1;
            else
                strips_per_sep = (m_dir.td_imagelength + m_dir.td_rowsperstrip - 1) / m_dir.td_rowsperstrip;

            uint sep_strip = strip % strips_per_sep;

            uint nrows = m_dir.td_imagelength % m_dir.td_rowsperstrip;
            if (sep_strip != strips_per_sep - 1 || nrows == 0)
                nrows = m_dir.td_rowsperstrip;

            int stripsize = VStripSize(nrows);
            if (size == (int)-1)
                size = stripsize;
            else if (size > stripsize)
                size = stripsize;
            
            byte[] tempBuf = new byte[size];
            memcpy(tempBuf, &buf[offset], size);

            if (fillStrip(strip) && m_currentCodec.tif_decodestrip(tempBuf, size, (UInt16)(strip / m_dir.td_stripsperimage)))
            {
                postDecode(tempBuf, size);
                memcpy(&buf[offset], tempBuf, size);
                return size;
            }

            return -1;
        }

        /*
        * Read a strip of data from the file.
        */
        public int ReadRawStrip(uint strip, byte[] buf, int offset, int size)
        {
            const string module = "ReadRawStrip";

            /*
            * FIXME: bytecount should have int type, but for now libtiff
            * defines int as a signed 32-bit integer and we are losing
            * ability to read arrays larger than 2^31 bytes. So we are using
            * uint instead of int here.
            */
            if (!checkRead(0))
                return -1;
            
            if (strip >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Strip out of range, max %lu", strip, m_dir.td_nstrips);
                return -1;
            }

            if ((m_flags & TIFF_NOREADRAW) != 0)
            {
                ErrorExt(this, m_clientdata, m_name, "Compression scheme does not support access to raw uncompressed data");
                return -1;
            }

            uint bytecount = m_dir.td_stripbytecount[strip];
            if (bytecount <= 0)
            {
                ErrorExt(this, m_clientdata, m_name, "%lu: Invalid strip byte count, strip %lu", bytecount, strip);
                return -1;
            }

            if (size != (int)-1 && (uint)size < bytecount)
                bytecount = size;
            
            return readRawStrip1(strip, buf, offset, bytecount, module);
        }

        /*
        * Encode the supplied data and write it to the
        * specified strip.
        *
        * NB: Image length must be setup before writing.
        */
        public int WriteEncodedStrip(uint strip, byte[] data, int cc)
        {
            const string module = "WriteEncodedStrip";
    
            if (!writeCheckStrips(module))
                return -1;

            /*
             * Check strip array to make sure there's space.
             * We don't support dynamically growing files that
             * have data organized in separate bitplanes because
             * it's too painful.  In that case we require that
             * the imagelength be set properly before the first
             * write (so that the strips array will be fully
             * allocated above).
             */
            if (strip >= m_dir.td_nstrips)
            {
                if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                {
                    ErrorExt(this, m_clientdata, m_name, "Can not grow image by strips when using separate planes");
                    return -1;
                }

                if (!growStrips(1, module))
                    return -1;

                m_dir.td_stripsperimage = howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip);
            }

            /*
             * Handle delayed allocation of data buffer.  This
             * permits it to be sized according to the directory
             * info.
             */
            if (!bufferCheck())
                return -1;

            m_curstrip = strip;
            m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
            if ((m_flags & TIFF_CODERSETUP) == 0)
            {
                if (!m_currentCodec.tif_setupencode())
                    return -1;

                m_flags |= TIFF_CODERSETUP;
            }

            m_rawcc = 0;
            m_rawcp = 0;

            if (m_dir.td_stripbytecount[strip] > 0)
            {
                /* this forces appendToStrip() to do a seek */
                m_curoff = 0;
            }

            m_flags &= ~TIFF_POSTENCODE;
            UInt16 sample = (UInt16)(strip / m_dir.td_stripsperimage);
            if (!m_currentCodec.tif_preencode(sample))
                return -1;

            /* swab if needed - note that source buffer will be altered */
            postDecode(data, cc);

            if (!m_currentCodec.tif_encodestrip(data, cc, sample))
                return 0;

            if (!m_currentCodec.tif_postencode())
                return -1;

            if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TIFF_NOBITREV) == 0)
                ReverseBits(m_rawdata, m_rawcc);

            if (m_rawcc > 0 && !appendToStrip(strip, m_rawdata, m_rawcc))
                return -1;

            m_rawcc = 0;
            m_rawcp = 0;
            return cc;
        }

        /*
        * Write the supplied data to the specified strip.
        *
        * NB: Image length must be setup before writing.
        */
        public int WriteRawStrip(uint strip, byte[] data, int cc)
        {
            const string module = "WriteRawStrip";

            if (!writeCheckStrips(module))
                return -1;

            /*
             * Check strip array to make sure there's space.
             * We don't support dynamically growing files that
             * have data organized in separate bitplanes because
             * it's too painful.  In that case we require that
             * the imagelength be set properly before the first
             * write (so that the strips array will be fully
             * allocated above).
             */
            if (strip >= m_dir.td_nstrips)
            {
                if (m_dir.td_planarconfig == PLANARCONFIG_SEPARATE)
                {
                    ErrorExt(this, m_clientdata, m_name, "Can not grow image by strips when using separate planes");
                    return -1;
                }

                /*
                 * Watch out for a growing image.  The value of
                 * strips/image will initially be 1 (since it
                 * can't be deduced until the imagelength is known).
                 */
                if (strip >= m_dir.td_stripsperimage)
                    m_dir.td_stripsperimage = howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip);

                if (!growStrips(1, module))
                    return -1;
            }

            m_curstrip = strip;
            m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
            return (appendToStrip(strip, data, cc) ? cc: -1);
        }

        /*
        * Encode the supplied data and write it to the
        * specified tile.  There must be space for the
        * data.  The function clamps individual writes
        * to a tile to the tile size, but does not (and
        * can not) check that multiple writes to the same
        * tile do not write more than tile size data.
        *
        * NB: Image length must be setup before writing; this
        *     interface does not support automatically growing
        *     the image on each write (as WriteScanline does).
        */
        public int WriteEncodedTile(uint tile, byte[] data, int cc)
        {
            const string module = "WriteEncodedTile";
    
            if (!writeCheckTiles(module))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, module, "%s: Tile %lu out of range, max %lu", m_name, tile, m_dir.td_nstrips);
                return -1;
            }

            /*
             * Handle delayed allocation of data buffer.  This
             * permits it to be sized more intelligently (using
             * directory information).
             */
            if (!bufferCheck())
                return -1;

            m_curtile = tile;

            m_rawcc = 0;
            m_rawcp = 0;

            if (m_dir.td_stripbytecount[tile] > 0)
            {
                /* this forces appendToStrip() to do a seek */
                m_curoff = 0;
            }

            /* 
             * Compute tiles per row & per column to compute
             * current row and column
             */
            m_row = (tile % howMany(m_dir.td_imagelength, m_dir.td_tilelength)) * m_dir.td_tilelength;
            m_col = (tile % howMany(m_dir.td_imagewidth, m_dir.td_tilewidth)) * m_dir.td_tilewidth;

            if ((m_flags & TIFF_CODERSETUP) == 0)
            {
                if (!m_currentCodec.tif_setupencode())
                    return -1;

                m_flags |= TIFF_CODERSETUP;
            }

            m_flags &= ~TIFF_POSTENCODE;
            UInt16 sample = (UInt16)(tile / m_dir.td_stripsperimage);
            if (!m_currentCodec.tif_preencode(sample))
                return -1;

            /*
             * Clamp write amount to the tile size.  This is mostly
             * done so that callers can pass in some large number
             * (e.g. -1) and have the tile size used instead.
             */
            if (cc < 1 || cc > m_tilesize)
                cc = m_tilesize;

            /* swab if needed - note that source buffer will be altered */
            postDecode(data, cc);

            if (!m_currentCodec.tif_encodetile(data, cc, sample))
                return 0;

            if (!m_currentCodec.tif_postencode())
                return -1;

            if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TIFF_NOBITREV) == 0)
                ReverseBits(m_rawdata, m_rawcc);

            if (m_rawcc > 0 && !appendToStrip(tile, m_rawdata, m_rawcc))
                return -1;

            m_rawcc = 0;
            m_rawcp = 0;
            return cc;
        }

        /*
        * Write the supplied data to the specified strip.
        * There must be space for the data; we don't check
        * if strips overlap!
        *
        * NB: Image length must be setup before writing; this
        *     interface does not support automatically growing
        *     the image on each write (as WriteScanline does).
        */
        public int WriteRawTile(uint tile, byte[] data, int cc)
        {
            const string module = "WriteRawTile";

            if (!writeCheckTiles(module))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, module, "%s: Tile %lu out of range, max %lu", m_name, tile, m_dir.td_nstrips);
                return -1;
            }

            return (appendToStrip(tile, data, cc) ? cc: -1);
        }

        /*
        * Set the current write offset.  This should only be
        * used to set the offset to a known previous location
        * (very carefully), or to 0 so that the next write gets
        * appended to the end of the file.
        */
        public void SetWriteOffset(uint off)
        {
            m_curoff = off;
        }

        /*
        * Return size of TiffDataType in bytes
        */
        public static int DataWidth(TiffDataType type)
        {
            switch (type)
            {
                case 0: /* nothing */
                case 1: /* TIFF_BYTE */
                case 2: /* TIFF_ASCII */
                case 6: /* TIFF_SBYTE */
                case 7: /* TIFF_UNDEFINED */
                    return 1;
                case 3: /* TIFF_SHORT */
                case 8: /* TIFF_SSHORT */
                    return 2;
                case 4: /* TIFF_LONG */
                case 9: /* TIFF_SLONG */
                case 11: /* TIFF_FLOAT */
                case 13: /* TIFF_IFD */
                    return 4;
                case 5: /* TIFF_RATIONAL */
                case 10: /* TIFF_SRATIONAL */
                case 12: /* TIFF_DOUBLE */
                    return 8;
                default:
                    return 0; /* will return 0 for unknown types */
            }
        }

        /*
        * TIFF Library Bit & Byte Swapping Support.
        *
        * XXX We assume short = 16-bits and long = 32-bits XXX
        */
        public static void SwabShort(ref UInt16 wp)
        {
            byte cp[2];
            cp[0] = (byte)wp;
            cp[1] = (byte)(wp >> 8);

            byte t = cp[1];
            cp[1] = cp[0];
            cp[0] = t;

            wp = cp[0] & 0xFF;
            wp += (cp[1] & 0xFF) << 8;
        }

        public static void SwabLong(ref int lp)
        {
            byte cp[4];
            cp[0] = (byte)lp;
            cp[1] = (byte)(lp >> 8);
            cp[2] = (byte)(lp >> 16);
            cp[3] = (byte)(lp >> 24);

            byte t = cp[3];
            cp[3] = cp[0];
            cp[0] = t;

            t = cp[2];
            cp[2] = cp[1];
            cp[1] = t;

            lp = cp[0] & 0xFF;
            lp += (cp[1] & 0xFF) << 8;
            lp += (cp[2] & 0xFF) << 16;
            lp += cp[3] << 24;
        }

        public static void SwabDouble(ref double dp)
        {
            uint[] lp = (uint*)dp;
            SwabArrayOfLong(lp, 2);

            uint t = lp[0];
            lp[0] = lp[1];
            lp[1] = t;
        }

        public static void SwabArrayOfShort(UInt16[] wp, int n)
        {
            for (int i = 0; i < n; i++)
            {
                byte cp[2];
                cp[0] = (byte)wp[i];
                cp[1] = (byte)(wp[i] >> 8);

                byte t = cp[1];
                cp[1] = cp[0];
                cp[0] = t;

                wp[i] = cp[0] & 0xFF;
                wp[i] += (cp[1] & 0xFF) << 8;
            }
        }

        public static void SwabArrayOfTriples(byte[] tp, int n)
        {
            /* XXX unroll loop some */
            int tpPos = 0;
            while (n-- > 0)
            {
                byte t = tp[tpPos + 2];
                tp[tpPos + 2] = tp[tpPos];
                tp[tpPos] = t;
                tpPos += 3;
            }
        }

        public static void SwabArrayOfLong(uint[] lp, int n)
        {
            for (int i = 0; i < n; i++)
            {
                byte cp[4];
                cp[0] = (byte)lp[i];
                cp[1] = (byte)(lp[i] >> 8);
                cp[2] = (byte)(lp[i] >> 16);
                cp[3] = (byte)(lp[i] >> 24);

                byte t = cp[3];
                cp[3] = cp[0];
                cp[0] = t;

                t = cp[2];
                cp[2] = cp[1];
                cp[1] = t;

                lp[i] = cp[0] & 0xFF;
                lp[i] += (cp[1] & 0xFF) << 8;
                lp[i] += (cp[2] & 0xFF) << 16;
                lp[i] += cp[3] << 24;
            }
        }

        public static void SwabArrayOfDouble(double[] dp, int n)
        {
            uint[] lp = (uint*)dp;
            SwabArrayOfLong(lp, n + n);

            int lpPos = 0;
            while (n-- > 0)
            {
                uint t = lp[lpPos];
                lp[lpPos] = lp[lpPos + 1];
                lp[lpPos + 1] = t;
                lpPos += 2;
            }
        }

        public static void ReverseBits(byte[] cp, int n)
        {
            int cpPos = 0;
            for (; n > 8; n -= 8)
            {
                cp[cpPos + 0] = TIFFBitRevTable[cp[cpPos + 0]];
                cp[cpPos + 1] = TIFFBitRevTable[cp[cpPos + 1]];
                cp[cpPos + 2] = TIFFBitRevTable[cp[cpPos + 2]];
                cp[cpPos + 3] = TIFFBitRevTable[cp[cpPos + 3]];
                cp[cpPos + 4] = TIFFBitRevTable[cp[cpPos + 4]];
                cp[cpPos + 5] = TIFFBitRevTable[cp[cpPos + 5]];
                cp[cpPos + 6] = TIFFBitRevTable[cp[cpPos + 6]];
                cp[cpPos + 7] = TIFFBitRevTable[cp[cpPos + 7]];
                cpPos += 8;
            }

            while (n-- > 0)
            {
                cp[cpPos] = TIFFBitRevTable[cp[cpPos]];
                cpPos++;
            }
        }

        public static byte[] GetBitRevTable(bool reversed)
        {
            return (reversed ? TIFFBitRevTable : TIFFNoBitRevTable);
        }
    }
}
