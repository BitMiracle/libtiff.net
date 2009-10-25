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
using System.Collections;

using BitMiracle.LibTiff.Internal;

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
    public partial class Tiff : IDisposable
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

        public delegate void TiffExtendProc(Tiff tif);

        public const int TIFF_VERSION = 42;
        public const int TIFF_BIGTIFF_VERSION = 43;

        public const ushort TIFF_BIGENDIAN = 0x4d4d;
        public const ushort TIFF_LITTLEENDIAN = 0x4949;
        public const ushort MDI_LITTLEENDIAN = 0x5045;

        /* reference white */
        public const float D50_X0 = 96.4250F;
        public const float D50_Y0 = 100.0F;
        public const float D50_Z0 = 82.4680F;

        internal const short TIFF_VARIABLE = -1; /* marker for variable length tags */
        internal const short TIFF_SPP = -2; /* marker for SamplesPerPixel tags */
        internal const short TIFF_VARIABLE2 = -3; /* marker for uint var-length tags */

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
        public TiffCodec FindCodec(COMPRESSION scheme)
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
            cd.codec = codec;
            cd.next = m_registeredCodecs;
            m_registeredCodecs = cd;

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

            ErrorExt(this, 0, "UnRegisterCodec",
                "Cannot remove compression scheme {0}; not registered", c.m_name);
        }

        /**
        * Check whether we have working codec for the specific coding scheme.
        * @return returns true if the codec is configured and working. Otherwise
        * false will be returned.
        */
        public bool IsCodecConfigured(COMPRESSION scheme)
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
                if (m_builtInCodecs[i] != null && IsCodecConfigured(m_builtInCodecs[i].m_scheme))
                    totalCodecs++;
            }

            for (codecList cd = m_registeredCodecs; cd != null; cd = cd.next)
                totalCodecs++;

            TiffCodec[] codecs = new TiffCodec [totalCodecs + 1];

            int codecPos = 0;
            for (codecList cd = m_registeredCodecs; cd != null; cd = cd.next)
                codecs[codecPos++] = cd.codec;

            for (int i = 0; m_builtInCodecs[i] != null; i++)
            {
                if (m_builtInCodecs[i] != null && IsCodecConfigured(m_builtInCodecs[i].m_scheme))
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

            int copyLength = Math.Min(elementCount, newElementCount);
            Array.Copy(oldBuffer, newBuffer, copyLength);

            return newBuffer;
        }

        public static int[] Realloc(int[] oldBuffer, int elementCount, int newElementCount)
        {
            int[] newBuffer = new int[newElementCount];
            //memset(newBuffer, 0, newElementCount * sizeof(uint));

            if (oldBuffer == null)
                return newBuffer;

            int copyLength = Math.Min(elementCount, newElementCount);
            Array.Copy(oldBuffer, newBuffer, copyLength);

            return newBuffer;
        }

        public static int Compare(ushort[] p1, ushort[] p2, int elementCount)
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
        public static Tiff Open(string fileName, string mode)
        {
            const string module = "Open";

            FileMode m;
            FileAccess a;
            getMode(mode, module, out m, out a);

            FileStream fd = null;
            try
            {
                if (a == FileAccess.Read)
                    fd = File.Open(fileName, m, a, FileShare.Read);
                else
                    fd = File.Open(fileName, m, a);
            }
            catch (System.Exception)
            {
                fd = null;
            }

            if (fd == null)
            {
                ErrorExt(null, 0, module, "{0}: Cannot open", fileName);
                return null;
            }

            Tiff tif = ClientOpen(fileName, mode, fd, new TiffStream());
            if (tif == null)
                fd.Dispose();
            else
                tif.m_fileStream = fd;

            return tif;
        }

        public static Tiff ClientOpen(string name, string mode, object clientdata, TiffStream stream)
        {
            const string module = "ClientOpen";

            if (mode == null || mode.Length == 0)
            {
                ErrorExt(null, clientdata, module, "{0}: mode string should contain at least one char", name);
                return null;
            }

            FileMode fm;
            FileAccess fa;
            int m = getMode(mode, module, out fm, out fa);

            Tiff tif = new Tiff();
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

            /* setup default state */
            tif.m_currentCodec = tif.m_builtInCodecs[0];

            /*
             * Default is to return data MSB2LSB and enable the
             * use of memory-mapped files and strip chopping when
             * a file is opened read-only.
             */
            tif.m_flags = (uint)FILLORDER.FILLORDER_MSB2LSB;

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
                            tif.m_flags |= Tiff.TIFF_SWAB;
                        break;
                    case 'l':
                        break;
                    case 'B':
                        tif.m_flags = (tif.m_flags & ~TIFF_FILLORDER) | (uint)FILLORDER.FILLORDER_MSB2LSB;
                        break;
                    case 'L':
                        tif.m_flags = (tif.m_flags & ~TIFF_FILLORDER) | (uint)FILLORDER.FILLORDER_LSB2MSB;
                        break;
                    case 'H':
                        tif.m_flags = (tif.m_flags & ~TIFF_FILLORDER) | (uint)FILLORDER.FILLORDER_LSB2MSB;
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

            if ((tif.m_mode & O_TRUNC) != 0 || !tif.readHeaderOk(ref tif.m_header))
            {
                if (tif.m_mode == O_RDONLY)
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Cannot read TIFF header");
                    return tif.safeOpenFailed();
                }

                /*
                 * Setup header and write.
                 */
                tif.m_header.tiff_magic = (tif.m_flags & Tiff.TIFF_SWAB) != 0 ? TIFF_BIGENDIAN : TIFF_LITTLEENDIAN;
                tif.m_header.tiff_version = TIFF_VERSION;
                if ((tif.m_flags & Tiff.TIFF_SWAB) != 0)
                    SwabShort(ref tif.m_header.tiff_version);

                tif.m_header.tiff_diroff = 0; /* filled in later */

                tif.seekFile(0, SeekOrigin.Begin);

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
                ErrorExt(tif, tif.m_clientdata, name,
                    "Not a TIFF or MDI file, bad magic number {0} (0x{1:x})",
                    tif.m_header.tiff_magic, tif.m_header.tiff_magic);
                return tif.safeOpenFailed();
            }

            tif.initOrder(tif.m_header.tiff_magic);

            /*
             * Swap header if required.
             */
            if ((tif.m_flags & Tiff.TIFF_SWAB) != 0)
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
                ErrorExt(tif, tif.m_clientdata, name,
                    "Not a TIFF file, bad version number {0} (0x{1:x})",
                    tif.m_header.tiff_version, tif.m_header.tiff_version);
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

        public int GetTagListEntry(int tag_index)
        {
            if (tag_index < 0 || tag_index >= m_dir.td_customValueCount)
                return -1;
            else
                return (int)m_dir.td_customValues[tag_index].info.field_tag;
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
                TiffFieldInfo fip = FindFieldInfo(info[i].field_tag, info[i].field_type);

                /* only add definitions that aren't already present */
                if (fip == null)
                {
                    m_fieldinfo[m_nfields] = info[i];
                    m_nfields++;
                }
            }

            /* Sort the field info by tag number */
            IComparer myComparer = new TagCompare();
            Array.Sort(m_fieldinfo, myComparer);
        }

        public TiffFieldInfo FindFieldInfo(TIFFTAG tag, TiffDataType dt)
        {
            if (m_foundfield != null && m_foundfield.field_tag == tag && (dt == TiffDataType.TIFF_ANY || dt == m_foundfield.field_type))
                return m_foundfield;

            /* If we are invoked with no field information, then just return. */
            if (m_fieldinfo == null)
                return null;

            m_foundfield = null;

            foreach (TiffFieldInfo info in m_fieldinfo)
            {
                if (info != null && info.field_tag == tag && (dt == TiffDataType.TIFF_ANY || dt == info.field_type))
                {
                    m_foundfield = info;
                    break;
                }
            }

            return m_foundfield;
        }

        public TiffFieldInfo FindFieldInfoByName(string field_name, TiffDataType dt)
        {
            if (m_foundfield != null && m_foundfield.field_name == field_name && (dt == TiffDataType.TIFF_ANY || dt == m_foundfield.field_type))
                return m_foundfield;

            /* If we are invoked with no field information, then just return. */
            if (m_fieldinfo == null)
                return null;

            m_foundfield = null;

            foreach (TiffFieldInfo info in m_fieldinfo)
            {
                if (info != null && info.field_name == field_name && (dt == TiffDataType.TIFF_ANY || dt == info.field_type))
                {
                    m_foundfield = info;
                    break;
                }
            }

            return m_foundfield;
        }

        public TiffFieldInfo FieldWithTag(TIFFTAG tag)
        {
            TiffFieldInfo fip = FindFieldInfo(tag, TiffDataType.TIFF_ANY);
            if (fip == null)
            {
                ErrorExt(this, m_clientdata, "FieldWithTag",
                    "Internal error, unknown tag 0x{0:x}", tag);
                Debug.Assert(false);
                /*NOTREACHED*/
            }

            return fip;
        }

        public TiffFieldInfo FieldWithName(string field_name)
        {
            TiffFieldInfo fip = FindFieldInfoByName(field_name, TiffDataType.TIFF_ANY);
            if (fip == null)
            {
                ErrorExt(this, m_clientdata, "FieldWithName", "Internal error, unknown tag {0}", field_name);
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

            while (link != null && link.name != name)
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
            while (link != null && link.name != name)
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
            link.next = m_clientinfo;
            link.name = name.Clone() as string;
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
        public FieldValue[] GetField(TIFFTAG tag)
        {
            TiffFieldInfo fip = FindFieldInfo(tag, TiffDataType.TIFF_ANY);
            if (fip != null && (isPseudoTag(tag) || fieldSet(fip.field_bit)))
                return m_tagmethods.vgetfield(this, tag);
            
            return null;
        }

        /*
        * Like GetField, but return any default
        * value if the tag is not present in the directory.
        *  
        *  NB: We use the value in the directory, rather than
        *  explicit values so that defaults exist only one
        *  place in the library -- in setupDefaultDirectory.
        */
        public FieldValue[] GetFieldDefaulted(TIFFTAG tag)
        {
            TiffDirectory td = m_dir;

            FieldValue[] result = GetField(tag);
            if (result != null)
                return result;

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_SUBFILETYPE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_subfiletype);
                    break;
                case TIFFTAG.TIFFTAG_BITSPERSAMPLE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_bitspersample);
                    break;
                case TIFFTAG.TIFFTAG_THRESHHOLDING:
                    result = new FieldValue[1];
                    result[0].Set(td.td_threshholding);
                    break;
                case TIFFTAG.TIFFTAG_FILLORDER:
                    result = new FieldValue[1];
                    result[0].Set(td.td_fillorder);
                    break;
                case TIFFTAG.TIFFTAG_ORIENTATION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_orientation);
                    break;
                case TIFFTAG.TIFFTAG_SAMPLESPERPIXEL:
                    result = new FieldValue[1];
                    result[0].Set(td.td_samplesperpixel);
                    break;
                case TIFFTAG.TIFFTAG_ROWSPERSTRIP:
                    result = new FieldValue[1];
                    result[0].Set(td.td_rowsperstrip);
                    break;
                case TIFFTAG.TIFFTAG_MINSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_minsamplevalue);
                    break;
                case TIFFTAG.TIFFTAG_MAXSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_maxsamplevalue);
                    break;
                case TIFFTAG.TIFFTAG_PLANARCONFIG:
                    result = new FieldValue[1];
                    result[0].Set(td.td_planarconfig);
                    break;
                case TIFFTAG.TIFFTAG_RESOLUTIONUNIT:
                    result = new FieldValue[1];
                    result[0].Set(td.td_resolutionunit);
                    break;
                case TIFFTAG.TIFFTAG_PREDICTOR:
                    CodecWithPredictor sp = m_currentCodec as CodecWithPredictor;
                    if (sp != null)
                    {
                        result = new FieldValue[1];
                        result[0].Set(sp.GetPredictorValue());
                    }
                    break;
                case TIFFTAG.TIFFTAG_DOTRANGE:
                    result = new FieldValue[2];
                    result[0].Set(0);
                    result[1].Set((1 << td.td_bitspersample) - 1);
                    break;
                case TIFFTAG.TIFFTAG_INKSET:
                    result = new FieldValue[1];
                    result[0].Set(INKSET.INKSET_CMYK);
                    break;
                case TIFFTAG.TIFFTAG_NUMBEROFINKS:
                    result = new FieldValue[1];
                    result[0].Set(4);
                    break;
                case TIFFTAG.TIFFTAG_EXTRASAMPLES:
                    result = new FieldValue[2];
                    result[0].Set(td.td_extrasamples);
                    result[1].Set(td.td_sampleinfo);
                    break;
                case TIFFTAG.TIFFTAG_MATTEING:
                    result = new FieldValue[1];
                    result[0].Set((td.td_extrasamples == 1 && td.td_sampleinfo[0] == EXTRASAMPLE.EXTRASAMPLE_ASSOCALPHA));
                    break;
                case TIFFTAG.TIFFTAG_TILEDEPTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_tiledepth);
                    break;
                case TIFFTAG.TIFFTAG_DATATYPE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_sampleformat - 1);
                    break;
                case TIFFTAG.TIFFTAG_SAMPLEFORMAT:
                    result = new FieldValue[1];
                    result[0].Set(td.td_sampleformat);
                    break;
                case TIFFTAG.TIFFTAG_IMAGEDEPTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_imagedepth);
                    break;
                case TIFFTAG.TIFFTAG_YCBCRCOEFFICIENTS:
                    {
                        /* defaults are from CCIR Recommendation 601-1 */
                        float[] ycbcrcoeffs = new float [3];
                        ycbcrcoeffs[0] = 0.299f;
                        ycbcrcoeffs[1] = 0.587f;
                        ycbcrcoeffs[2] = 0.114f;

                        result = new FieldValue[1];
                        result[0].Set(ycbcrcoeffs);
                        break;
                    }
                case TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING:
                    result = new FieldValue[2];
                    result[0].Set(td.td_ycbcrsubsampling[0]);
                    result[1].Set(td.td_ycbcrsubsampling[1]);
                    break;
                case TIFFTAG.TIFFTAG_YCBCRPOSITIONING:
                    result = new FieldValue[1];
                    result[0].Set(td.td_ycbcrpositioning);
                    break;
                case TIFFTAG.TIFFTAG_WHITEPOINT:
                    {
                        /* TIFF 6.0 specification tells that it is no default
                        value for the WhitePoint, but AdobePhotoshop TIFF
                        Technical Note tells that it should be CIE D50. */
                        float[] whitepoint = new float[2];
                        whitepoint[0] = D50_X0 / (D50_X0 + D50_Y0 + D50_Z0);
                        whitepoint[1] = D50_Y0 / (D50_X0 + D50_Y0 + D50_Z0);

                        result = new FieldValue[1];
                        result[0].Set(whitepoint);
                        break;
                    }
                case TIFFTAG.TIFFTAG_TRANSFERFUNCTION:
                    if (td.td_transferfunction[0] == null && !defaultTransferFunction(td))
                    {
                        ErrorExt(this, m_clientdata, m_name, "No space for \"TransferFunction\" tag");
                        return null;
                    }

                    result = new FieldValue[3];
                    result[0].Set(td.td_transferfunction[0]);
                    if (td.td_samplesperpixel - td.td_extrasamples > 1)
                    {
                        result[1].Set(td.td_transferfunction[1]);
                        result[2].Set(td.td_transferfunction[2]);
                    }
                    break;
                case TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE:
                    {
                        float[] ycbcr_refblackwhite = new float [6];
                        ycbcr_refblackwhite[0] = 0.0F;
                        ycbcr_refblackwhite[1] = 255.0F;
                        ycbcr_refblackwhite[2] = 128.0F;
                        ycbcr_refblackwhite[3] = 255.0F;
                        ycbcr_refblackwhite[4] = 128.0F;
                        ycbcr_refblackwhite[5] = 255.0F;
                        
                        float[] rgb_refblackwhite = new float[6];
                        for (int i = 0; i < 3; i++)
                        {
                            rgb_refblackwhite[2 * i + 0] = 0.0F;
                            rgb_refblackwhite[2 * i + 1] = (float)((1L << td.td_bitspersample) - 1L);
                        }

                        result = new FieldValue[1];
                        if (td.td_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR)
                        {
                            /*
                             * YCbCr (Class Y) images must have the
                             * ReferenceBlackWhite tag set. Fix the
                             * broken images, which lacks that tag.
                             */
                            result[0].Set(ycbcr_refblackwhite);
                        }
                        else
                        {
                            /*
                             * Assume RGB (Class R)
                             */
                            result[0].Set(rgb_refblackwhite);
                        }

                        break;
                    }
            }

            return result;
        }
        
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
            TiffDirEntry[] dir;
            ushort dircount = fetchDirectory(m_nextdiroff, out dir, out m_nextdiroff);
            if (dircount == 0)
            {
                ErrorExt(this, m_clientdata, module, "{0}: Failed to read directory at offset {1}", m_name, m_nextdiroff);
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
            SetField(TIFFTAG.TIFFTAG_PLANARCONFIG, PLANARCONFIG.PLANARCONFIG_CONTIG);

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
                TiffDirEntry dp = dir[i];
                if ((m_flags & Tiff.TIFF_SWAB) != 0)
                {
                    ushort temp = (ushort)dp.tdir_tag;
                    SwabShort(ref temp);
                    dp.tdir_tag = (TIFFTAG)temp;

                    temp = (ushort)dp.tdir_type;
                    SwabShort(ref temp);
                    dp.tdir_type = (TiffDataType)temp;

                    SwabLong(ref dp.tdir_count);
                    SwabLong(ref dp.tdir_offset);
                }
                
                if (dp.tdir_tag == TIFFTAG.TIFFTAG_SAMPLESPERPIXEL)
                {
                    if (!fetchNormalTag(dir[i]))
                        return readDirectoryFailed(dir);

                    dp.tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                }
            }

            /*
             * First real pass over the directory.
             */
            uint fix = 0;
            bool diroutoforderwarning = false;
            for (int i = 0; i < dircount; i++)
            {
                if (fix >= m_nfields || dir[i].tdir_tag == TIFFTAG.TIFFTAG_IGNORE)
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
                        WarningExt(this, m_clientdata, module, "{0}: invalid TIFF directory; tags are not sorted in ascending order", m_name);
                        diroutoforderwarning = true;
                    }

                    fix = 0; /* O(n^2) */
                }

                while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                    fix++;

                if (fix >= m_nfields || m_fieldinfo[fix].field_tag != dir[i].tdir_tag)
                {
                    WarningExt(this, m_clientdata, module,
                        "{0}: unknown field with tag {1} (0x{2:x}) encountered",
                        m_name, dir[i].tdir_tag, dir[i].tdir_tag);

                    TiffFieldInfo[] arr = new TiffFieldInfo[1];
                    arr[0] = createAnonFieldInfo(dir[i].tdir_tag, dir[i].tdir_type);
                    MergeFieldInfo(arr, 1);

                    fix = 0;
                    while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                        fix++;
                }

                /*
                 * null out old tags that we ignore.
                 */
                if (m_fieldinfo[fix].field_bit == FIELD.FIELD_IGNORE)
                {
                    dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                    continue;
                }

                /*
                 * Check data type.
                 */
                TiffFieldInfo fip = m_fieldinfo[fix];
                while (dir[i].tdir_type != fip.field_type && fix < m_nfields)
                {
                    if (fip.field_type == TiffDataType.TIFF_ANY)
                    {
                        /* wildcard */
                        break;
                    }

                    fip = m_fieldinfo[++fix];
                    if (fix >= m_nfields || fip.field_tag != dir[i].tdir_tag)
                    {
                        WarningExt(this, m_clientdata, module, "{0}: wrong data type {1} for \"{2}\"; tag ignored", m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].field_name);
                        dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                        continue;
                    }
                }

                /*
                 * Check count if known in advance.
                 */
                if (fip.field_readcount != TIFF_VARIABLE && fip.field_readcount != TIFF_VARIABLE2)
                {
                    int expected = fip.field_readcount;
                    if (fip.field_readcount == TIFF_SPP)
                        expected = m_dir.td_samplesperpixel;

                    if (!checkDirCount(dir[i], expected))
                    {
                        dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                        continue;
                    }
                }

                switch (dir[i].tdir_tag)
                {
                    case TIFFTAG.TIFFTAG_COMPRESSION:
                        /*
                         * The 5.0 spec says the Compression tag has
                         * one value, while earlier specs say it has
                         * one value per sample.  Because of this, we
                         * accept the tag if one value is supplied.
                         */
                        if (dir[i].tdir_count == 1)
                        {
                            int v = extractData(dir[i]);
                            if (!SetField(dir[i].tdir_tag, (ushort)v))
                                return readDirectoryFailed(dir);
                            
                            break;
                            /* XXX: workaround for broken TIFFs */
                        }
                        else if (dir[i].tdir_type == TiffDataType.TIFF_LONG)
                        {
                            int v;
                            if (!fetchPerSampleLongs(dir[i], out v) || !SetField(dir[i].tdir_tag, (ushort)v))
                                return readDirectoryFailed(dir);
                        }
                        else
                        {
                            ushort iv;
                            if (!fetchPerSampleShorts(dir[i], out iv) || !SetField(dir[i].tdir_tag, iv))
                                return readDirectoryFailed(dir);
                        }
                        dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                        break;
                    case TIFFTAG.TIFFTAG_STRIPOFFSETS:
                    case TIFFTAG.TIFFTAG_STRIPBYTECOUNTS:
                    case TIFFTAG.TIFFTAG_TILEOFFSETS:
                    case TIFFTAG.TIFFTAG_TILEBYTECOUNTS:
                        setFieldBit(fip.field_bit);
                        break;
                    case TIFFTAG.TIFFTAG_IMAGEWIDTH:
                    case TIFFTAG.TIFFTAG_IMAGELENGTH:
                    case TIFFTAG.TIFFTAG_IMAGEDEPTH:
                    case TIFFTAG.TIFFTAG_TILELENGTH:
                    case TIFFTAG.TIFFTAG_TILEWIDTH:
                    case TIFFTAG.TIFFTAG_TILEDEPTH:
                    case TIFFTAG.TIFFTAG_PLANARCONFIG:
                    case TIFFTAG.TIFFTAG_ROWSPERSTRIP:
                    case TIFFTAG.TIFFTAG_EXTRASAMPLES:
                        if (!fetchNormalTag(dir[i]))
                            return readDirectoryFailed(dir);
                        dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
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
            if ((m_dir.td_compression == COMPRESSION.COMPRESSION_OJPEG) && (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)) 
            {
                int dpIndex = readDirectoryFind(dir, dircount, TIFFTAG.TIFFTAG_STRIPOFFSETS);
                if (dpIndex != -1 && dir[dpIndex].tdir_count == 1) 
                {
                    dpIndex = readDirectoryFind(dir, dircount, TIFFTAG.TIFFTAG_STRIPBYTECOUNTS);
                    if (dpIndex != -1 && dir[dpIndex].tdir_count == 1) 
                    {
                        m_dir.td_planarconfig = PLANARCONFIG.PLANARCONFIG_CONTIG;
                        WarningExt(this, m_clientdata, "ReadDirectory", "Planarconfig tag value assumed incorrect, assuming data is contig instead of chunky");
                    }
                }
            }

            /*
             * Allocate directory structure and setup defaults.
             */
            if (!fieldSet(FIELD.FIELD_IMAGEDIMENSIONS))
            {
                missingRequired("ImageLength");
                return readDirectoryFailed(dir);
            }

            /* 
             * Setup appropriate structures (by strip or by tile)
             */
            if (!fieldSet(FIELD.FIELD_TILEDIMENSIONS))
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
                ErrorExt(this, m_clientdata, module, "{0}: cannot handle zero number of {1}", m_name, IsTiled() ? "tiles" : "strips");
                return readDirectoryFailed(dir);
            }

            m_dir.td_stripsperimage = m_dir.td_nstrips;
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                m_dir.td_stripsperimage /= m_dir.td_samplesperpixel;

            if (!fieldSet(FIELD.FIELD_STRIPOFFSETS))
            {
                if ((m_dir.td_compression == COMPRESSION.COMPRESSION_OJPEG) && !IsTiled() && (m_dir.td_nstrips == 1)) 
                {
                    /*
                    * XXX: OJPEG hack.
                    * If a) compression is OJPEG, b) it's not a tiled TIFF,
                    * and c) the number of strips is 1,
                    * then we tolerate the absence of stripoffsets tag,
                    * because, presumably, all required data is in the
                    * JpegInterchangeFormat stream.
                    */
                    setFieldBit(FIELD.FIELD_STRIPOFFSETS);
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
                if (dir[i].tdir_tag == TIFFTAG.TIFFTAG_IGNORE)
                    continue;
                
                switch (dir[i].tdir_tag)
                {
                    case TIFFTAG.TIFFTAG_MINSAMPLEVALUE:
                    case TIFFTAG.TIFFTAG_MAXSAMPLEVALUE:
                    case TIFFTAG.TIFFTAG_BITSPERSAMPLE:
                    case TIFFTAG.TIFFTAG_DATATYPE:
                    case TIFFTAG.TIFFTAG_SAMPLEFORMAT:
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
                            int v = extractData(dir[i]);
                            if (!SetField(dir[i].tdir_tag, (ushort)v))
                                return readDirectoryFailed(dir);
                            /* XXX: workaround for broken TIFFs */
                        }
                        else if (dir[i].tdir_tag == TIFFTAG.TIFFTAG_BITSPERSAMPLE && dir[i].tdir_type == TiffDataType.TIFF_LONG)
                        {
                            int v;
                            if (!fetchPerSampleLongs(dir[i], out v) || !SetField(dir[i].tdir_tag, (ushort)v))
                                return readDirectoryFailed(dir);
                        }
                        else
                        {
                            ushort iv;
                            if (!fetchPerSampleShorts(dir[i], out iv) || !SetField(dir[i].tdir_tag, iv))
                                return readDirectoryFailed(dir);
                        }
                        break;
                    case TIFFTAG.TIFFTAG_SMINSAMPLEVALUE:
                    case TIFFTAG.TIFFTAG_SMAXSAMPLEVALUE:
                        {
                            double dv;
                            if (!fetchPerSampleAnys(dir[i], out dv) || !SetField(dir[i].tdir_tag, dv))
                                return readDirectoryFailed(dir);
                        }
                        break;
                    case TIFFTAG.TIFFTAG_STRIPOFFSETS:
                    case TIFFTAG.TIFFTAG_TILEOFFSETS:
                        if (!fetchStripThing(dir[i], m_dir.td_nstrips, ref m_dir.td_stripoffset))
                            return readDirectoryFailed(dir);
                        break;
                    case TIFFTAG.TIFFTAG_STRIPBYTECOUNTS:
                    case TIFFTAG.TIFFTAG_TILEBYTECOUNTS:
                        if (!fetchStripThing(dir[i], m_dir.td_nstrips, ref m_dir.td_stripbytecount))
                            return readDirectoryFailed(dir);
                        break;
                    case TIFFTAG.TIFFTAG_COLORMAP:
                    case TIFFTAG.TIFFTAG_TRANSFERFUNCTION:
                        {
                            /*
                             * TransferFunction can have either 1x or 3x
                             * data values; Colormap can have only 3x
                             * items.
                             */
                            int v = 1 << m_dir.td_bitspersample;
                            if (dir[i].tdir_tag == TIFFTAG.TIFFTAG_COLORMAP || dir[i].tdir_count != v)
                            {
                                if (!checkDirCount(dir[i], 3 * v))
                                    break;
                            }

                            byte[] cp = new byte [dir[i].tdir_count * sizeof(ushort)];
                            if (fetchData(dir[i], cp) != 0)
                            {
                                int c = 1 << m_dir.td_bitspersample;
                                if (dir[i].tdir_count == c)
                                {
                                    /*
                                    * This deals with there being
                                    * only one array to apply to
                                    * all samples.
                                    */
                                    ushort[] u = byteArrayToUInt16(cp, 0, dir[i].tdir_count * sizeof(ushort));
                                    SetField(dir[i].tdir_tag, u, u, u);
                                }
                                else
                                {
                                    v *= sizeof(ushort);
                                    ushort[] u0 = byteArrayToUInt16(cp, 0, v);
                                    ushort[] u1 = byteArrayToUInt16(cp, v, v);
                                    ushort[] u2 = byteArrayToUInt16(cp, 2 * v, v);
                                    SetField(dir[i].tdir_tag, u0, u1, u2);
                                }
                            }
                            break;
                        }
                    case TIFFTAG.TIFFTAG_PAGENUMBER:
                    case TIFFTAG.TIFFTAG_HALFTONEHINTS:
                    case TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING:
                    case TIFFTAG.TIFFTAG_DOTRANGE:
                        fetchShortPair(dir[i]);
                        break;
                    case TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE:
                        fetchRefBlackWhite(dir[i]);
                        break;
                        /* BEGIN REV 4.0 COMPATIBILITY */
                    case TIFFTAG.TIFFTAG_OSUBFILETYPE:
                        {
                            FILETYPE v = 0;
                            switch ((OFILETYPE)extractData(dir[i]))
                            {
                                case OFILETYPE.OFILETYPE_REDUCEDIMAGE:
                                    v = FILETYPE.FILETYPE_REDUCEDIMAGE;
                                    break;
                                case OFILETYPE.OFILETYPE_PAGE:
                                    v = FILETYPE.FILETYPE_PAGE;
                                    break;
                            }

                            if (v != 0)
                                SetField(TIFFTAG.TIFFTAG_SUBFILETYPE, v);
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
            if (m_dir.td_compression == COMPRESSION.COMPRESSION_OJPEG)
            {
                if (!fieldSet(FIELD.FIELD_PHOTOMETRIC))
                {
                    WarningExt(this, m_clientdata, "ReadDirectory", "Photometric tag is missing, assuming data is YCbCr");
                    if (!SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, PHOTOMETRIC.PHOTOMETRIC_YCBCR))
                        return readDirectoryFailed(dir);
                }
                else if (m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_RGB)
                {
                    m_dir.td_photometric = PHOTOMETRIC.PHOTOMETRIC_YCBCR;
                    WarningExt(this, m_clientdata, "ReadDirectory", "Photometric tag value assumed incorrect, assuming data is YCbCr instead of RGB");
                }
                
                if (!fieldSet(FIELD.FIELD_BITSPERSAMPLE))
                {
                    WarningExt(this, m_clientdata, "ReadDirectory", "BitsPerSample tag is missing, assuming 8 bits per sample");
                    if (!SetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE, 8))
                        return readDirectoryFailed(dir);
                }

                if (!fieldSet(FIELD.FIELD_SAMPLESPERPIXEL))
                {
                    if ((m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_RGB) || (m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR))
                    {
                        WarningExt(this, m_clientdata, "ReadDirectory", "SamplesPerPixel tag is missing, assuming correct SamplesPerPixel value is 3");
                        if (!SetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL, 3))
                            return readDirectoryFailed(dir);
                    }
                    else if ((m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_MINISWHITE) || (m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_MINISBLACK))
                    {
                        WarningExt(this, m_clientdata, "ReadDirectory", "SamplesPerPixel tag is missing, assuming correct SamplesPerPixel value is 1");
                        if (!SetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL, 1))
                            return readDirectoryFailed(dir);
                    }
                }
            }

            /*
             * Verify Palette image has a Colormap.
             */
            if (m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_PALETTE && !fieldSet(FIELD.FIELD_COLORMAP))
            {
                missingRequired("Colormap");
                return readDirectoryFailed(dir);
            }

            /*
            * OJPEG hack:
            * We do no further messing with strip/tile offsets/bytecounts in OJPEG
            * TIFFs
            */
            if (m_dir.td_compression != COMPRESSION.COMPRESSION_OJPEG)
            {
                /*
                 * Attempt to deal with a missing StripByteCounts tag.
                 */
                if (!fieldSet(FIELD.FIELD_STRIPBYTECOUNTS))
                {
                    /*
                     * Some manufacturers violate the spec by not giving
                     * the size of the strips.  In this case, assume there
                     * is one uncompressed strip of data.
                     */
                    if ((m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_dir.td_nstrips > 1) || 
                        (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_dir.td_nstrips != m_dir.td_samplesperpixel))
                    {
                        missingRequired("StripByteCounts");
                        return readDirectoryFailed(dir);
                    }

                    WarningExt(this, m_clientdata, module, "{0}: TIFF directory is missing required \"{1}\" field, calculating from imagelength", m_name, FieldWithTag(TIFFTAG.TIFFTAG_STRIPBYTECOUNTS).field_name);
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
                    WarningExt(this, m_clientdata, module, "{0}: Bogus \"{1}\" field, ignoring and calculating from imagelength", m_name, FieldWithTag(TIFFTAG.TIFFTAG_STRIPBYTECOUNTS).field_name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return readDirectoryFailed(dir);
                }
                else if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_dir.td_nstrips > 2 && m_dir.td_compression == COMPRESSION.COMPRESSION_NONE && m_dir.td_stripbytecount[0] != m_dir.td_stripbytecount[1])
                {
                    /*
                     * XXX: Some vendors fill StripByteCount array with absolutely
                     * wrong values (it can be equal to StripOffset array, for
                     * example). Catch this case here.
                     */
                    WarningExt(this, m_clientdata, module, "{0}: Wrong \"{1}\" field, ignoring and calculating from imagelength", m_name, FieldWithTag(TIFFTAG.TIFFTAG_STRIPBYTECOUNTS).field_name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return readDirectoryFailed(dir);
                }
            }

            dir = null;

            if (!fieldSet(FIELD.FIELD_MAXSAMPLEVALUE))
                m_dir.td_maxsamplevalue = (ushort)((1L << m_dir.td_bitspersample) - 1);

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

            if (!fieldSet(FIELD.FIELD_COMPRESSION))
                SetField(TIFFTAG.TIFFTAG_COMPRESSION, COMPRESSION.COMPRESSION_NONE);

            /*
             * Some manufacturers make life difficult by writing
             * large amounts of uncompressed data as a single strip.
             * This is contrary to the recommendations of the spec.
             * The following makes an attempt at breaking such images
             * into strips closer to the recommended 8k bytes.  A
             * side effect, however, is that the RowsPerStrip tag
             * value may be changed.
             */
            if (m_dir.td_nstrips == 1 && m_dir.td_compression == COMPRESSION.COMPRESSION_NONE && (m_flags & (TIFF_STRIPCHOP | TIFF_ISTILED)) == TIFF_STRIPCHOP)
                chopUpSingleUncompressedStrip();

            /*
             * Reinitialize i/o since we are starting on a new directory.
             */
            m_row = -1;
            m_curstrip = -1;
            m_col = -1;
            m_curtile = -1;
            m_tilesize = (int)-1;

            m_scanlinesize = ScanlineSize();
            if (m_scanlinesize == 0)
            {
                ErrorExt(this, m_clientdata, module, "{0}: cannot handle zero scanline size", m_name);
                return false;
            }

            if (IsTiled())
            {
                m_tilesize = TileSize();
                if (m_tilesize == 0)
                {
                    ErrorExt(this, m_clientdata, module, "{0}: cannot handle zero tile size", m_name);
                    return false;
                }
            }
            else
            {
                if (StripSize() == 0)
                {
                    ErrorExt(this, m_clientdata, module, "{0}: cannot handle zero strip size", m_name);
                    return false;
                }
            }

            return true;
        }
        
        /* 
        * Read custom directory from the arbitrary offset.
        * The code is very similar to ReadDirectory().
        */
        public bool ReadCustomDirectory(int diroff, TiffFieldInfo[] info, int n)
        {
            const string module = "ReadCustomDirectory";

            setupFieldInfo(info, n);

            int dummyNextDirOff;
            TiffDirEntry[] dir;
            ushort dircount = fetchDirectory(diroff, out dir, out dummyNextDirOff);
            if (dircount == 0)
            {
                ErrorExt(this, m_clientdata, module, "{0}: Failed to read custom directory at offset {1}", m_name, diroff);
                return false;
            }

            FreeDirectory();
            m_dir = new TiffDirectory();

            uint fix = 0;
            for (ushort i = 0; i < dircount; i++)
            {
                if ((m_flags & Tiff.TIFF_SWAB) != 0)
                {
                    ushort temp = (ushort)dir[i].tdir_tag;
                    SwabShort(ref temp);
                    dir[i].tdir_tag = (TIFFTAG)temp;

                    temp = (ushort)dir[i].tdir_type;
                    SwabShort(ref temp);
                    dir[i].tdir_type = (TiffDataType)temp;

                    SwabLong(ref dir[i].tdir_count);
                    SwabLong(ref dir[i].tdir_offset);
                }

                if (fix >= m_nfields || dir[i].tdir_tag == TIFFTAG.TIFFTAG_IGNORE)
                    continue;

                while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                    fix++;

                if (fix >= m_nfields || m_fieldinfo[fix].field_tag != dir[i].tdir_tag)
                {
                    WarningExt(this, m_clientdata, module,
                        "{0}: unknown field with tag {1} (0x{2:x}) encountered",
                        m_name, dir[i].tdir_tag, dir[i].tdir_tag);

                    TiffFieldInfo[] arr = new TiffFieldInfo[1];
                    arr[0] = createAnonFieldInfo(dir[i].tdir_tag, dir[i].tdir_type);
                    MergeFieldInfo(arr, 1);

                    fix = 0;
                    while (fix < m_nfields && m_fieldinfo[fix].field_tag < dir[i].tdir_tag)
                        fix++;
                }

                /*
                 * null out old tags that we ignore.
                 */
                if (m_fieldinfo[fix].field_bit == FIELD.FIELD_IGNORE)
                {
                    dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                    continue;
                }

                /*
                 * Check data type.
                 */
                TiffFieldInfo fip = m_fieldinfo[fix];
                while (dir[i].tdir_type != fip.field_type && fix < m_nfields)
                {
                    if (fip.field_type == TiffDataType.TIFF_ANY)
                    {
                        /* wildcard */
                        break;
                    }

                    fip = m_fieldinfo[++fix];
                    if (fix >= m_nfields || fip.field_tag != dir[i].tdir_tag)
                    {
                        WarningExt(this, m_clientdata, module, "{0}: wrong data type {1} for \"{2}\"; tag ignored", m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].field_name);
                        dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                        continue;
                    }
                }

                /*
                 * Check count if known in advance.
                 */
                if (fip.field_readcount != TIFF_VARIABLE && fip.field_readcount != TIFF_VARIABLE2)
                {
                    int expected = fip.field_readcount;
                    if (fip.field_readcount == TIFF_SPP)
                        expected = m_dir.td_samplesperpixel;

                    if (!checkDirCount(dir[i], expected))
                    {
                        dir[i].tdir_tag = TIFFTAG.TIFFTAG_IGNORE;
                        continue;
                    }
                }
            
                /*
                * EXIF tags which need to be specifically processed.
                */
                switch (dir[i].tdir_tag) 
                {
                    case TIFFTAG.EXIFTAG_SUBJECTDISTANCE:
                        fetchSubjectDistance(dir[i]);
                        break;
                    default:
                        fetchNormalTag(dir[i]);
                        break;
                }
            }

            return true;
        }

        public bool WriteCustomDirectory(out int pdiroff)
        {
            pdiroff = -1;

            if (m_mode == O_RDONLY)
                return true;

            /*
            * Size the directory so that we can calculate
            * offsets for the data items that aren't kept
            * in-place in each field.
            */
            int nfields = 0;
            for (int b = 0; b <= FIELD.FIELD_LAST; b++)
            {
                if (fieldSet(b) && b != FIELD.FIELD_CUSTOM)
                    nfields += (b < FIELD.FIELD_SUBFILETYPE ? 2 : 1);
            }

            nfields += m_dir.td_customValueCount;
            int dirsize = nfields * TiffDirEntry.SizeInBytes;
            TiffDirEntry[] data = new TiffDirEntry[nfields];

            /*
            * Put the directory  at the end of the file.
            */
            m_diroff = (seekFile(0, SeekOrigin.End) + 1) & ~1;
            m_dataoff = m_diroff + sizeof(ushort) + dirsize + sizeof(uint);
            if ((m_dataoff & 1) != 0)
                m_dataoff++;

            seekFile(m_dataoff, SeekOrigin.Begin);
            TiffDirEntry[] dir = data;
            
            /*
            * Setup external form of directory
            * entries and write data items.
            */
            uint[] fields = new uint[FIELD.FIELD_SETLONGS];
            Array.Copy(m_dir.td_fieldsset, fields, FIELD.FIELD_SETLONGS);

            for (int fi = 0, nfi = m_nfields; nfi > 0; nfi--, fi++)
            {
                TiffFieldInfo fip = m_fieldinfo[fi];

                /*
                * For custom fields, we test to see if the custom field
                * is set or not.  For normal fields, we just use the
                * FieldSet test.
                */
                if (fip.field_bit == FIELD.FIELD_CUSTOM)
                {
                    bool is_set = false;
                    for (int ci = 0; ci < m_dir.td_customValueCount; ci++)
                        is_set |= (m_dir.td_customValues[ci].info == fip);

                    if (!is_set)
                        continue;
                }
                else if (!fieldSet(fields, fip.field_bit))
                    continue;

                if (fip.field_bit != FIELD.FIELD_CUSTOM)
                    resetFieldBit(fields, fip.field_bit);
            }

            /*
            * Write directory.
            */
            ushort dircount = (ushort)nfields;
            pdiroff = m_nextdiroff;
            if ((m_flags & Tiff.TIFF_SWAB) != 0)
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
                for (int i = 0; i < dircount; i++)
                {
                    TiffDirEntry dirEntry = data[i];

                    ushort temp = (ushort)dirEntry.tdir_tag;
                    SwabShort(ref temp);
                    dirEntry.tdir_tag = (TIFFTAG)temp;

                    temp = (ushort)dirEntry.tdir_type;
                    SwabShort(ref temp);
                    dirEntry.tdir_type = (TiffDataType)temp;

                    SwabLong(ref dirEntry.tdir_count);
                    SwabLong(ref dirEntry.tdir_offset);
                }
                
                dircount = (ushort) nfields;
                SwabShort(ref dircount);
                SwabLong(ref pdiroff);
            }

            seekFile(m_diroff, SeekOrigin.Begin);
            if (!writeUInt16OK(dircount))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory count");
                return false;
            }

            if (!writeDirEntryOK(data, dirsize / TiffDirEntry.SizeInBytes))
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
        public bool ReadEXIFDirectory(int diroff)
        {
            int exifFieldInfoCount;
            TiffFieldInfo[] exifFieldInfo = getExifFieldInfo(out exifFieldInfoCount);
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
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
            {
                if (m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && !IsUpSampled())
                {
                    FieldValue[] result = GetField(TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING);
                    ushort ycbcrsubsampling0 = result[0].ToUShort();

                    if (ycbcrsubsampling0 == 0)
                    {
                        ErrorExt(this, m_clientdata, m_name, "Invalid YCbCr subsampling");
                        return 0;
                    }

                    scanline = roundUp(m_dir.td_imagewidth, ycbcrsubsampling0);
                    scanline = howMany8(multiply(scanline, m_dir.td_bitspersample, "ScanlineSize"));
                    return summarize(scanline, multiply(2, scanline / ycbcrsubsampling0, "VStripSize"), "VStripSize");
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
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
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
            int rps = m_dir.td_rowsperstrip;
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
                ErrorExt(this, m_clientdata, m_name,
                    "{0}: Invalid strip byte count, strip {1}", bytecount, strip);
                bytecount = (int)-1;
            }

            return bytecount;
        }
        
        /*
        * Compute the # bytes in a variable height, row-aligned strip.
        */
        public int VStripSize(int nrows)
        {
            if (nrows == -1)
                nrows = m_dir.td_imagelength;

            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && !IsUpSampled())
            {
                /*
                 * Packed YCbCr data contain one Cb+Cr for every
                 * HorizontalSampling * VerticalSampling Y values.
                 * Must also roundup width and height when calculating
                 * since images that are not a multiple of the
                 * horizontal/vertical subsampling area include
                 * YCbCr data for the extended image.
                 */
                FieldValue[] result = GetField(TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING);
                ushort ycbcrsubsampling0 = result[0].ToUShort();
                ushort ycbcrsubsampling1 = result[1].ToUShort();

                int samplingarea = ycbcrsubsampling0 * ycbcrsubsampling1;
                if (samplingarea == 0)
                {
                    ErrorExt(this, m_clientdata, m_name, "Invalid YCbCr subsampling");
                    return 0;
                }

                int w = roundUp(m_dir.td_imagewidth, ycbcrsubsampling0);
                int scanline = howMany8(multiply(w, m_dir.td_bitspersample, "VStripSize"));
                nrows = roundUp(nrows, ycbcrsubsampling1);
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
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG)
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
        public int VTileSize(int nrows)
        {
            if (m_dir.td_tilelength == 0 || m_dir.td_tilewidth == 0 || m_dir.td_tiledepth == 0)
                return 0;

            int tilesize;
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_dir.td_photometric == PHOTOMETRIC.PHOTOMETRIC_YCBCR && !IsUpSampled())
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
        public int DefaultStripSize(int request)
        {
            return m_currentCodec.tif_defstripsize(request);
        }

        /*
        * Compute a default tile size based on the image
        * characteristics and a requested value.  If a
        * request is <1 then we choose a size according
        * to certain heuristics.
        */
        public void DefaultTileSize(ref int tw, ref int th)
        {
            m_currentCodec.tif_deftilesize(ref tw, ref th);
        }
        
        /*
        * Return open file's clientdata.
        */
        public object Clientdata()
        {
            return m_clientdata;
        }

        /*
        * Set open file's clientdata, and return previous value.
        */
        public object SetClientdata(object newvalue)
        {
            object m = m_clientdata;
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
            return ((m_flags & Tiff.TIFF_SWAB) != 0);
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
            return isFillOrder(FILLORDER.FILLORDER_MSB2LSB);
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
        public int CurrentRow()
        {
            return m_row;
        }

        /*
        * Return index of the current directory.
        */
        public short CurrentDirectory()
        {
            return m_curdir;
        }

        /*
        * Count the number of directories in a file.
        */
        public ushort NumberOfDirectories()
        {
            int nextdir = m_header.tiff_diroff;
            ushort n = 0;
            int dummyOff;
            while (nextdir != 0 && advanceDirectory(ref nextdir, out dummyOff))
                n++;

            return n;
        }

        /*
        * Return file offset of the current directory.
        */
        public int CurrentDirOffset()
        {
            return m_diroff;
        }

        /*
        * Return current strip.
        */
        public int CurrentStrip()
        {
            return m_curstrip;
        }

        /*
        * Return current tile.
        */
        public int CurrentTile()
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
        public void ReadBufferSetup(byte[] bp, int size)
        {
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
        }

        /*
        * Setup the raw data buffer used for encoding.
        */
        public void WriteBufferSetup(byte[] bp, int size)
        {
            if (m_rawdata != null)
            {
                if ((m_flags & TIFF_MYBUFFER) != 0)
                    m_flags &= ~TIFF_MYBUFFER;

                m_rawdata = null;
            }
            
            if (size == -1)
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
                m_flags |= TIFF_MYBUFFER;
            }
            else
                m_flags &= ~TIFF_MYBUFFER;
            
            m_rawdata = bp;
            m_rawdatasize = size;
            m_rawcc = 0;
            m_rawcp = 0;
            m_flags |= TIFF_BUFFERSETUP;
        }

        public bool SetupStrips()
        {
            if (IsTiled())
                m_dir.td_stripsperimage = isUnspecified(FIELD.FIELD_TILEDIMENSIONS) ? m_dir.td_samplesperpixel : NumberOfTiles();
            else
                m_dir.td_stripsperimage = isUnspecified(FIELD.FIELD_ROWSPERSTRIP) ? m_dir.td_samplesperpixel : NumberOfStrips();

            m_dir.td_nstrips = m_dir.td_stripsperimage;

            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                m_dir.td_stripsperimage /= m_dir.td_samplesperpixel;

            m_dir.td_stripoffset = new int[m_dir.td_nstrips];
            m_dir.td_stripbytecount = new int[m_dir.td_nstrips];

            setFieldBit(FIELD.FIELD_STRIPOFFSETS);
            setFieldBit(FIELD.FIELD_STRIPBYTECOUNTS);
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
                ErrorExt(this, m_clientdata, module, "{0}: File not open for writing", m_name);
                return false;
            }

            int temp = 0;
            if (IsTiled())
                temp = 1;

            if ((tiles ^ temp) != 0)
            {
                ErrorExt(this, m_clientdata, m_name, tiles != 0 ? "Can not write tiles to a stripped image": "Can not write scanlines to a tiled image");
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
            if (!fieldSet(FIELD.FIELD_IMAGEDIMENSIONS))
            {
                ErrorExt(this, m_clientdata, module, "{0}: Must set \"ImageWidth\" before writing data", m_name);
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
                if (!fieldSet(FIELD.FIELD_PLANARCONFIG))
                    m_dir.td_planarconfig = PLANARCONFIG.PLANARCONFIG_CONTIG;
            }
            else
            {
                if (!fieldSet(FIELD.FIELD_PLANARCONFIG))
                {
                    ErrorExt(this, m_clientdata, module, "{0}: Must set \"PlanarConfiguration\" before writing data", m_name);
                    return false;
                }
            }

            if (m_dir.td_stripoffset == null && !SetupStrips())
            {
                m_dir.td_nstrips = 0;
                ErrorExt(this, m_clientdata, module, "{0}: No space for {1} arrays", m_name, IsTiled() ? "tile" : "strip");
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
                clearFieldBit(FIELD.FIELD_YCBCRSUBSAMPLING);
                clearFieldBit(FIELD.FIELD_YCBCRPOSITIONING);

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
            m_row = -1;
            m_curstrip = -1;
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
        public bool SetDirectory(short dirn)
        {
            short n;
            int dummyOff;
            int nextdir = m_header.tiff_diroff;
            for (n = dirn; n > 0 && nextdir != 0; n--)
            {
                if (!advanceDirectory(ref nextdir, out dummyOff))
                    return false;
            }

            m_nextdiroff = nextdir;

            /*
             * Set curdir to the actual directory index.  The
             * -1 is because ReadDirectory will increment
             * m_curdir after successfully reading the directory.
             */
            m_curdir = (short)(dirn - n - 1);

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
        public bool SetSubDirectory(int diroff)
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
        public bool UnlinkDirectory(ushort dirn)
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
            int nextdir = m_header.tiff_diroff;
            int off = sizeof(ushort) + sizeof(ushort);
            for (int n = dirn - 1; n > 0; n--)
            {
                if (nextdir == 0)
                {
                    ErrorExt(this, m_clientdata, module,
                        "Directory {0} does not exist", dirn);
                    return false;
                }

                if (!advanceDirectory(ref nextdir, out off))
                    return false;
            }

            /*
             * Advance to the directory to be unlinked and fetch
             * the offset of the directory that follows.
             */
            int dummyOff;
            if (!advanceDirectory(ref nextdir, out dummyOff))
                return false;

            /*
             * Go back and patch the link field of the preceding
             * directory to point to the offset of the directory
             * that follows.
             */
            seekFile(off, SeekOrigin.Begin);
            if ((m_flags & Tiff.TIFF_SWAB) != 0)
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
            m_row = -1;
            m_curstrip = -1;
            return true;
        }
        
        /*
        * Record the value of a field in the
        * internal directory structure.  The
        * field will be written to the file
        * when/if the directory structure is
        * updated.
        */
        public bool SetField(TIFFTAG tag, params object[] ap)
        {
            return okToChangeTag(tag) ? m_tagmethods.vsetfield(this, tag, FieldValue.FromParams(ap)) : false;
        }

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
            SetWriteOffset(seekFile(0, SeekOrigin.End));
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

                seekFile(TiffHeader.TIFF_MAGIC_SIZE + TiffHeader.TIFF_VERSION_SIZE, SeekOrigin.Begin);
                if (!writeIntOK(m_header.tiff_diroff))
                {
                    ErrorExt(this, m_clientdata, m_name, "Error updating TIFF header");
                    return false;
                }
            }
            else
            {
                int nextdir = m_header.tiff_diroff;
                do
                {
                    ushort dircount;
                    if (!seekOK(nextdir) || !readUInt16OK(out dircount))
                    {
                        ErrorExt(this, m_clientdata, module, "Error fetching directory count");
                        return false;
                    }
                    
                    if ((m_flags & Tiff.TIFF_SWAB) != 0)
                        SwabShort(ref dircount);

                    seekFile(dircount * TiffDirEntry.SizeInBytes, SeekOrigin.Current);
                    
                    if (!readIntOK(out nextdir))
                    {
                        ErrorExt(this, m_clientdata, module, "Error fetching directory link");
                        return false;
                    }

                    if ((m_flags & Tiff.TIFF_SWAB) != 0)
                        SwabLong(ref nextdir);
                }
                while (nextdir != m_diroff && nextdir != 0);

                int off = seekFile(0, SeekOrigin.Current); /* get current offset */
                seekFile(off - sizeof(int), SeekOrigin.Begin);
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
            PrintDirectory(fd, TiffPrintDirectoryFlags.TIFFPRINT_NONE);
        }

        public void PrintDirectory(Stream fd, TiffPrintDirectoryFlags flags)
        {
            fprintf(fd, "TIFF Directory at offset 0x{0:x} ({1})\n", m_diroff, m_diroff);
    
            if (fieldSet(FIELD.FIELD_SUBFILETYPE))
            {
                fprintf(fd, "  Subfile Type:");
                string sep = " ";
                if ((m_dir.td_subfiletype & FILETYPE.FILETYPE_REDUCEDIMAGE) != 0)
                {
                    fprintf(fd, "{0}reduced-resolution image", sep);
                    sep = "/";
                }

                if ((m_dir.td_subfiletype & FILETYPE.FILETYPE_PAGE) != 0)
                {
                    fprintf(fd, "{0}multi-page document", sep);
                    sep = "/";
                }
                
                if ((m_dir.td_subfiletype & FILETYPE.FILETYPE_MASK) != 0)
                    fprintf(fd, "{0}transparency mask", sep);

                fprintf(fd, " ({0} = 0x{1:x})\n", m_dir.td_subfiletype, m_dir.td_subfiletype);
            }

            if (fieldSet(FIELD.FIELD_IMAGEDIMENSIONS))
            {
                fprintf(fd, "  Image Width: {0} Image Length: {1}", m_dir.td_imagewidth, m_dir.td_imagelength);
                if (fieldSet(FIELD.FIELD_IMAGEDEPTH))
                    fprintf(fd, " Image Depth: {0}", m_dir.td_imagedepth);
                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD.FIELD_TILEDIMENSIONS))
            {
                fprintf(fd, "  Tile Width: {0} Tile Length: {1}", m_dir.td_tilewidth, m_dir.td_tilelength);
                if (fieldSet(FIELD.FIELD_TILEDEPTH))
                    fprintf(fd, " Tile Depth: {0}", m_dir.td_tiledepth);
                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD.FIELD_RESOLUTION))
            {
                fprintf(fd, "  Resolution: {0:G}, {1:G}", m_dir.td_xresolution, m_dir.td_yresolution);
                if (fieldSet(FIELD.FIELD_RESOLUTIONUNIT))
                {
                    switch (m_dir.td_resolutionunit)
                    {
                        case RESUNIT.RESUNIT_NONE:
                            fprintf(fd, " (unitless)");
                            break;
                        case RESUNIT.RESUNIT_INCH:
                            fprintf(fd, " pixels/inch");
                            break;
                        case RESUNIT.RESUNIT_CENTIMETER:
                            fprintf(fd, " pixels/cm");
                            break;
                        default:
                            fprintf(fd, " (unit {0} = 0x{1:x})", m_dir.td_resolutionunit, m_dir.td_resolutionunit);
                            break;
                    }
                }
                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD.FIELD_POSITION))
                fprintf(fd, "  Position: {0:G}, {1:G}\n", m_dir.td_xposition, m_dir.td_yposition);
            
            if (fieldSet(FIELD.FIELD_BITSPERSAMPLE))
                fprintf(fd, "  Bits/Sample: {0}\n", m_dir.td_bitspersample);
            
            if (fieldSet(FIELD.FIELD_SAMPLEFORMAT))
            {
                fprintf(fd, "  Sample Format: ");
                switch (m_dir.td_sampleformat)
                {
                    case SAMPLEFORMAT.SAMPLEFORMAT_VOID:
                        fprintf(fd, "void\n");
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_INT:
                        fprintf(fd, "signed integer\n");
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_UINT:
                        fprintf(fd, "unsigned integer\n");
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_IEEEFP:
                        fprintf(fd, "IEEE floating point\n");
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_COMPLEXINT:
                        fprintf(fd, "complex signed integer\n");
                        break;
                    case SAMPLEFORMAT.SAMPLEFORMAT_COMPLEXIEEEFP:
                        fprintf(fd, "complex IEEE floating point\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_sampleformat, m_dir.td_sampleformat);
                        break;
                }
            }

            if (fieldSet(FIELD.FIELD_COMPRESSION))
            {
                TiffCodec c = FindCodec(m_dir.td_compression);
                fprintf(fd, "  Compression Scheme: ");
                if (c != null)
                    fprintf(fd, "{0}\n", c.m_name);
                else
                    fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_compression, m_dir.td_compression);
            }

            if (fieldSet(FIELD.FIELD_PHOTOMETRIC))
            {
                fprintf(fd, "  Photometric Interpretation: ");
                if ((int)m_dir.td_photometric < photoNames.Length)
                    fprintf(fd, "{0}\n", photoNames[(int)m_dir.td_photometric]);
                else
                {
                    switch (m_dir.td_photometric)
                    {
                        case PHOTOMETRIC.PHOTOMETRIC_LOGL:
                            fprintf(fd, "CIE Log2(L)\n");
                            break;
                        case PHOTOMETRIC.PHOTOMETRIC_LOGLUV:
                            fprintf(fd, "CIE Log2(L) (u',v')\n");
                            break;
                        default:
                            fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_photometric, m_dir.td_photometric);
                            break;
                    }
                }
            }

            if (fieldSet(FIELD.FIELD_EXTRASAMPLES) && m_dir.td_extrasamples != 0)
            {
                fprintf(fd, "  Extra Samples: {0}<", m_dir.td_extrasamples);
                string sep = "";
                for (ushort i = 0; i < m_dir.td_extrasamples; i++)
                {
                    switch (m_dir.td_sampleinfo[i])
                    {
                        case EXTRASAMPLE.EXTRASAMPLE_UNSPECIFIED:
                            fprintf(fd, "{0}unspecified", sep);
                            break;
                        case EXTRASAMPLE.EXTRASAMPLE_ASSOCALPHA:
                            fprintf(fd, "{0}assoc-alpha", sep);
                            break;
                        case EXTRASAMPLE.EXTRASAMPLE_UNASSALPHA:
                            fprintf(fd, "{0}unassoc-alpha", sep);
                            break;
                        default:
                            fprintf(fd, "{0}{1} (0x{2:x})", sep, m_dir.td_sampleinfo[i], m_dir.td_sampleinfo[i]);
                            break;
                    }
                    sep = ", ";
                }
                fprintf(fd, ">\n");
            }

            if (fieldSet(FIELD.FIELD_INKNAMES))
            {
                fprintf(fd, "  Ink Names: ");
                
                string[] names = m_dir.td_inknames.Split(new char[] { '\0' });
                for (int i = 0; i < names.Length; i++)
                {
                    printAscii(fd, names[i]);
                    fprintf(fd, ", ");
                }

                fprintf(fd, "\n");
            }

            if (fieldSet(FIELD.FIELD_THRESHHOLDING))
            {
                fprintf(fd, "  Thresholding: ");
                switch (m_dir.td_threshholding)
                {
                    case THRESHHOLD.THRESHHOLD_BILEVEL:
                        fprintf(fd, "bilevel art scan\n");
                        break;
                    case THRESHHOLD.THRESHHOLD_HALFTONE:
                        fprintf(fd, "halftone or dithered scan\n");
                        break;
                    case THRESHHOLD.THRESHHOLD_ERRORDIFFUSE:
                        fprintf(fd, "error diffused\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_threshholding, m_dir.td_threshholding);
                        break;
                }
            }

            if (fieldSet(FIELD.FIELD_FILLORDER))
            {
                fprintf(fd, "  FillOrder: ");
                switch (m_dir.td_fillorder)
                {
                    case FILLORDER.FILLORDER_MSB2LSB:
                        fprintf(fd, "msb-to-lsb\n");
                        break;
                    case FILLORDER.FILLORDER_LSB2MSB:
                        fprintf(fd, "lsb-to-msb\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_fillorder, m_dir.td_fillorder);
                        break;
                }
            }

            if (fieldSet(FIELD.FIELD_YCBCRSUBSAMPLING))
            {
                /*
                 * For hacky reasons (see tif_jpeg.c - JPEGFixupTestSubsampling),
                 * we need to fetch this rather than trust what is in our
                 * structures.
                 */
                FieldValue[] result = GetField(TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING);
                ushort subsampling0 = result[0].ToUShort();
                ushort subsampling1 = result[1].ToUShort();
                fprintf(fd, "  YCbCr Subsampling: {0}, {1}\n", subsampling0, subsampling1);
            }

            if (fieldSet(FIELD.FIELD_YCBCRPOSITIONING))
            {
                fprintf(fd, "  YCbCr Positioning: ");
                switch (m_dir.td_ycbcrpositioning)
                {
                    case YCBCRPOSITION.YCBCRPOSITION_CENTERED:
                        fprintf(fd, "centered\n");
                        break;
                    case YCBCRPOSITION.YCBCRPOSITION_COSITED:
                        fprintf(fd, "cosited\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_ycbcrpositioning, m_dir.td_ycbcrpositioning);
                        break;
                }
            }

            if (fieldSet(FIELD.FIELD_HALFTONEHINTS))
                fprintf(fd, "  Halftone Hints: light {0} dark {1}\n", m_dir.td_halftonehints[0], m_dir.td_halftonehints[1]);
            
            if (fieldSet(FIELD.FIELD_ORIENTATION))
            {
                fprintf(fd, "  Orientation: ");
                if ((int)m_dir.td_orientation < orientNames.Length)
                    fprintf(fd, "{0}\n", orientNames[(int)m_dir.td_orientation]);
                else
                    fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_orientation, m_dir.td_orientation);
            }

            if (fieldSet(FIELD.FIELD_SAMPLESPERPIXEL))
                fprintf(fd, "  Samples/Pixel: {0}\n", m_dir.td_samplesperpixel);
            
            if (fieldSet(FIELD.FIELD_ROWSPERSTRIP))
            {
                fprintf(fd, "  Rows/Strip: ");
                if (m_dir.td_rowsperstrip == -1)
                    fprintf(fd, "(infinite)\n");
                else
                    fprintf(fd, "{0}\n", m_dir.td_rowsperstrip);
            }

            if (fieldSet(FIELD.FIELD_MINSAMPLEVALUE))
                fprintf(fd, "  Min Sample Value: {0}\n", m_dir.td_minsamplevalue);
            
            if (fieldSet(FIELD.FIELD_MAXSAMPLEVALUE))
                fprintf(fd, "  Max Sample Value: {0}\n", m_dir.td_maxsamplevalue);
            
            if (fieldSet(FIELD.FIELD_SMINSAMPLEVALUE))
                fprintf(fd, "  SMin Sample Value: {0:G}\n", m_dir.td_sminsamplevalue);
            
            if (fieldSet(FIELD.FIELD_SMAXSAMPLEVALUE))
                fprintf(fd, "  SMax Sample Value: {0:G}\n", m_dir.td_smaxsamplevalue);
            
            if (fieldSet(FIELD.FIELD_PLANARCONFIG))
            {
                fprintf(fd, "  Planar Configuration: ");
                switch (m_dir.td_planarconfig)
                {
                    case PLANARCONFIG.PLANARCONFIG_CONTIG:
                        fprintf(fd, "single image plane\n");
                        break;
                    case PLANARCONFIG.PLANARCONFIG_SEPARATE:
                        fprintf(fd, "separate image planes\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_planarconfig, m_dir.td_planarconfig);
                        break;
                }
            }

            if (fieldSet(FIELD.FIELD_PAGENUMBER))
                fprintf(fd, "  Page Number: {0}-{1}\n", m_dir.td_pagenumber[0], m_dir.td_pagenumber[1]);
            
            if (fieldSet(FIELD.FIELD_COLORMAP))
            {
                fprintf(fd, "  Color Map: ");
                if ((flags & TiffPrintDirectoryFlags.TIFFPRINT_COLORMAP) != 0)
                {
                    fprintf(fd, "\n");
                    int n = 1 << m_dir.td_bitspersample;
                    for (int l = 0; l < n; l++)
                        fprintf(fd, "   {0,5}: {1,5} {2,5} {3,5}\n", l, m_dir.td_colormap[0][l], m_dir.td_colormap[1][l], m_dir.td_colormap[2][l]);
                }
                else
                    fprintf(fd, "(present)\n");
            }

            if (fieldSet(FIELD.FIELD_TRANSFERFUNCTION))
            {
                fprintf(fd, "  Transfer Function: ");
                if ((flags & TiffPrintDirectoryFlags.TIFFPRINT_CURVES) != 0)
                {
                    fprintf(fd, "\n");
                    int n = 1 << m_dir.td_bitspersample;
                    for (int l = 0; l < n; l++)
                    {
                        fprintf(fd, "    {0,2}: {0,5}", l, m_dir.td_transferfunction[0][l]);
                        for (ushort i = 1; i < m_dir.td_samplesperpixel; i++)
                            fprintf(fd, " {0,5}", m_dir.td_transferfunction[i][l]);
                        fprintf(fd, "\n");
                    }
                }
                else
                    fprintf(fd, "(present)\n");
            }

            if (fieldSet(FIELD.FIELD_SUBIFD) && m_dir.td_subifd != null)
            {
                fprintf(fd, "  SubIFD Offsets:");
                for (ushort i = 0; i < m_dir.td_nsubifd; i++)
                    fprintf(fd, " {0,5}", m_dir.td_subifd[i]);
                fprintf(fd, "\n");
            }

            /*
             ** Custom tag support.
             */
            int count = GetTagListCount();
            for (int i = 0; i < count; i++)
            {
                TIFFTAG tag = (TIFFTAG)GetTagListEntry(i);
                TiffFieldInfo fip = FieldWithTag((TIFFTAG)tag);
                if (fip == null)
                    continue;

                byte[] raw_data = null;
                int value_count;
                if (fip.field_passcount)
                {
                    FieldValue[] result = GetField(tag);
                    if (result == null)
                        continue;

                    value_count = result[0].ToInt();
                    raw_data = result[1].ToByteArray();
                }
                else
                {
                    if (fip.field_readcount == TIFF_VARIABLE || fip.field_readcount == TIFF_VARIABLE2)
                        value_count = 1;
                    else if (fip.field_readcount == TIFF_SPP)
                        value_count = m_dir.td_samplesperpixel;
                    else
                        value_count = fip.field_readcount;

                    if ((fip.field_type == TiffDataType.TIFF_ASCII || fip.field_readcount == TIFF_VARIABLE || fip.field_readcount == TIFF_VARIABLE2 || fip.field_readcount == TIFF_SPP || value_count > 1) && fip.field_tag != TIFFTAG.TIFFTAG_PAGENUMBER && fip.field_tag != TIFFTAG.TIFFTAG_HALFTONEHINTS && fip.field_tag != TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING && fip.field_tag != TIFFTAG.TIFFTAG_DOTRANGE)
                    {
                        FieldValue[] result = GetField(tag);
                        if (result == null)
                            continue;

                        raw_data = result[0].ToByteArray();
                    }
                    else if (fip.field_tag != TIFFTAG.TIFFTAG_PAGENUMBER && fip.field_tag != TIFFTAG.TIFFTAG_HALFTONEHINTS && fip.field_tag != TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING && fip.field_tag != TIFFTAG.TIFFTAG_DOTRANGE)
                    {
                        raw_data = new byte [dataSize(fip.field_type) * value_count];

                        FieldValue[] result = GetField(tag);
                        if (result == null)
                            continue;

                        raw_data = result[0].ToByteArray();
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

                        FieldValue[] result = GetField(tag);
                        if (result == null)
                            continue;

                        byte[] first = result[0].ToByteArray();
                        byte[] second = result[1].ToByteArray();

                        Array.Copy(first, raw_data, first.Length);
                        Array.Copy(second, 0, raw_data, dataSize(fip.field_type), second.Length);
                    }
                }

                /*
                 * Catch the tags which needs to be specially handled and
                 * pretty print them. If tag not handled in
                 * prettyPrintField() fall down and print it as any other
                 * tag.
                 */
                if (prettyPrintField(fd, tag, value_count, raw_data))
                    continue;
                else
                    printField(fd, fip, value_count, raw_data);
            }

            m_tagmethods.printdir(this, fd, flags);

            if ((flags & TiffPrintDirectoryFlags.TIFFPRINT_STRIPS) != 0 && fieldSet(FIELD.FIELD_STRIPOFFSETS))
            {
                fprintf(fd, "  {0} {1}:\n", m_dir.td_nstrips, IsTiled() ? "Tiles" : "Strips");
                for (uint s = 0; s < m_dir.td_nstrips; s++)
                    fprintf(fd, "    {0,3}: [{0,8}, {0,8}]\n", s, m_dir.td_stripoffset[s], m_dir.td_stripbytecount[s]);
            }
        }

        public bool ReadScanline(byte[] buf, int row)
        {
            return ReadScanline(buf, row, 0);
        }

        public bool ReadScanline(byte[] buf, int row, ushort sample)
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

        public bool WriteScanline(byte[] buf, int row)
        {
            return WriteScanline(buf, row, 0);
        }

        public bool WriteScanline(byte[] buf, int row, ushort sample)
        {
            const string module = "WriteScanline";

            if (!writeCheckStrips(module))
                return false;

            /*
             * Handle delayed allocation of data buffer.  This
             * permits it to be sized more intelligently (using
             * directory information).
             */
            bufferCheck();
            
            /*
             * Extend image length if needed
             * (but only for PlanarConfig=1).
             */
            bool imagegrew = false;
            if (row >= m_dir.td_imagelength)
            {
                /* extend image */
                if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
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
            int strip;
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name,
                        "{0}: Sample out of range, max {1}", sample, m_dir.td_samplesperpixel);
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
        public bool ReadRGBAImage(int rwidth, int rheight, uint[] raster)
        {
            return ReadRGBAImage(rwidth, rheight, raster, false);
        }

        public bool ReadRGBAImage(int rwidth, int rheight, uint[] raster, bool stop)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, ORIENTATION.ORIENTATION_BOTLEFT, stop);
        }
        
        /*
        * Read the specified image into an ABGR-format raster taking in account
        * specified orientation.
        */
        public bool ReadRGBAImageOriented(int rwidth, int rheight, uint[] raster)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, ORIENTATION.ORIENTATION_BOTLEFT, false);
        }

        public bool ReadRGBAImageOriented(int rwidth, int rheight, uint[] raster, ORIENTATION orientation)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, orientation, false);
        }

        public bool ReadRGBAImageOriented(int rwidth, int rheight, uint[] raster, ORIENTATION orientation, bool stop)
        {
            bool ok = true;
            string emsg;
            if (RGBAImageOK(out emsg))
            {
                TiffRGBAImage img = TiffRGBAImage.Create(this, stop, out emsg);
                if (img != null)
                {
                    img.req_orientation = orientation;
                    /* XXX verify rwidth and rheight against width and height */
                    ok = img.Get(raster, (rheight - img.height) * rwidth, rwidth, img.height);
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
        public bool ReadRGBAStrip(int row, uint[] raster)
        {
            if (IsTiled())
            {
                ErrorExt(this, m_clientdata, FileName(), "Can't use ReadRGBAStrip() with tiled file.");
                return false;
            }

            FieldValue[] result = GetFieldDefaulted(TIFFTAG.TIFFTAG_ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();
            if ((row % rowsperstrip) != 0)
            {
                ErrorExt(this, m_clientdata, FileName(), "Row passed to ReadRGBAStrip() must be first in a strip.");
                return false;
            }

            bool ok = false;
            string emsg;
            if (RGBAImageOK(out emsg))
            {
                TiffRGBAImage img = TiffRGBAImage.Create(this, false, out emsg);
                if (img != null)
                {
                    img.row_offset = row;
                    img.col_offset = 0;

                    int rows_to_read = rowsperstrip;
                    if (row + rowsperstrip > img.height)
                        rows_to_read = img.height - row;

                    ok = img.Get(raster, 0, img.width, rows_to_read);
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
        public bool ReadRGBATile(int col, int row, uint[] raster)
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

            FieldValue[] result = GetFieldDefaulted(TIFFTAG.TIFFTAG_TILEWIDTH);
            int tile_xsize = result[0].ToInt();
            result = GetFieldDefaulted(TIFFTAG.TIFFTAG_TILELENGTH);
            int tile_ysize = result[0].ToInt();

            if ((col % tile_xsize) != 0 || (row % tile_ysize) != 0)
            {
                ErrorExt(this, m_clientdata, FileName(), "Row/col passed to ReadRGBATile() must be topleft corner of a tile.");
                return false;
            }

            /*
             * Setup the RGBA reader.
             */
            string emsg;
            TiffRGBAImage img = TiffRGBAImage.Create(this, false, out emsg);
            if (!RGBAImageOK(out emsg) || img == null)
            {
                ErrorExt(this, m_clientdata, FileName(), emsg);
                return false;
            }

            /*
             * The TIFFRGBAImageGet() function doesn't allow us to get off the
             * edge of the image, even to fill an otherwise valid tile.  So we
             * figure out how much we can read, and fix up the tile buffer to
             * a full tile configuration afterwards.
             */
            int read_ysize;
            if (row + tile_ysize > img.height)
                read_ysize = img.height - row;
            else
                read_ysize = tile_ysize;

            int read_xsize;
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

            /*
             * If our read was incomplete we will need to fix up the tile by
             * shifting the data around as if a full tile of data is being returned.
             *
             * This is all the more complicated because the image is organized in
             * bottom to top format. 
             */

            if (read_xsize == tile_xsize && read_ysize == tile_ysize)
                return ok;

            for (int i_row = 0; i_row < read_ysize; i_row++)
            {
                Array.Copy(raster, (read_ysize - i_row - 1) * read_xsize, raster, (tile_ysize - i_row - 1) * tile_xsize, read_xsize);
                Array.Clear(raster, (tile_ysize - i_row - 1) * tile_xsize + read_xsize, tile_xsize - read_xsize);
            }

            for (int i_row = read_ysize; i_row < tile_ysize; i_row++)
            {
                Array.Clear(raster, (tile_ysize - i_row - 1) * tile_xsize, tile_xsize);
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
            emsg = null;

            if (!m_decodestatus)
            {
                emsg = "Sorry, requested compression method is not configured";
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
                    emsg = string.Format("Sorry, can not handle images with {0}-bit samples", m_dir.td_bitspersample);
                    return false;
            }
            
            int colorchannels = m_dir.td_samplesperpixel - m_dir.td_extrasamples;
            PHOTOMETRIC photometric = PHOTOMETRIC.PHOTOMETRIC_RGB;
            FieldValue[] result = GetField(TIFFTAG.TIFFTAG_PHOTOMETRIC);
            if (result == null)
            {
                switch (colorchannels)
                {
                    case 1:
                        photometric = PHOTOMETRIC.PHOTOMETRIC_MINISBLACK;
                        break;
                    case 3:
                        photometric = PHOTOMETRIC.PHOTOMETRIC_RGB;
                        break;
                    default:
                        emsg = string.Format("Missing needed {0} tag", TiffRGBAImage.photoTag);
                        return false;
                }
            }

            switch (photometric)
            {
                case PHOTOMETRIC.PHOTOMETRIC_MINISWHITE:
                case PHOTOMETRIC.PHOTOMETRIC_MINISBLACK:
                case PHOTOMETRIC.PHOTOMETRIC_PALETTE:
                    if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_CONTIG && m_dir.td_samplesperpixel != 1 && m_dir.td_bitspersample < 8)
                    {
                        emsg = string.Format(
                            "Sorry, can not handle contiguous data with {0}={1}, and {2}={3} and Bits/Sample={4}", 
                            TiffRGBAImage.photoTag, photometric, "Samples/pixel", m_dir.td_samplesperpixel, 
                            m_dir.td_bitspersample);

                        return false;
                    }
                    /*
                     * We should likely validate that any extra samples are either
                     * to be ignored, or are alpha, and if alpha we should try to use
                     * them.  But for now we won't bother with this. 
                     */
                    break;
                case PHOTOMETRIC.PHOTOMETRIC_YCBCR:
                    /*
                    * TODO: if at all meaningful and useful, make more complete
                    * support check here, or better still, refactor to let supporting
                    * code decide whether there is support and what meaningfull
                    * error to return
                    */
                    break;
                case PHOTOMETRIC.PHOTOMETRIC_RGB:
                    if (colorchannels < 3)
                    {
                        emsg = string.Format(
                            "Sorry, can not handle RGB image with {0}={1}", 
                            "Color channels", colorchannels);

                        return false;
                    }
                    break;
                case PHOTOMETRIC.PHOTOMETRIC_SEPARATED:
                    {
                        result = GetFieldDefaulted(TIFFTAG.TIFFTAG_INKSET);
                        INKSET inkset = (INKSET)result[0].ToByte();
                        if (inkset != INKSET.INKSET_CMYK)
                        {
                            emsg = string.Format(
                                "Sorry, can not handle separated image with {0}={1}", "InkSet", inkset);
                            return false;
                        }
                        if (m_dir.td_samplesperpixel < 4)
                        {
                            emsg = string.Format("Sorry, can not handle separated image with {0}={1}",
                                "Samples/pixel", m_dir.td_samplesperpixel);
                            return false;
                        }
                        break;
                    }
                case PHOTOMETRIC.PHOTOMETRIC_LOGL:
                    if (m_dir.td_compression != COMPRESSION.COMPRESSION_SGILOG)
                    {
                        emsg = string.Format("Sorry, LogL data must have {0}={1}",
                            "Compression", COMPRESSION.COMPRESSION_SGILOG);
                        return false;
                    }
                    break;
                case PHOTOMETRIC.PHOTOMETRIC_LOGLUV:
                    if (m_dir.td_compression != COMPRESSION.COMPRESSION_SGILOG &&
                        m_dir.td_compression != COMPRESSION.COMPRESSION_SGILOG24)
                    {
                        emsg = string.Format("Sorry, LogLuv data must have {0}={1} or {2}",
                            "Compression", COMPRESSION.COMPRESSION_SGILOG, COMPRESSION.COMPRESSION_SGILOG24);
                        return false;
                    }

                    if (m_dir.td_planarconfig != PLANARCONFIG.PLANARCONFIG_CONTIG)
                    {
                        emsg = string.Format("Sorry, can not handle LogLuv images with {0}={1}",
                            "Planarconfiguration", m_dir.td_planarconfig);
                        return false;
                    }
                    break;
                case PHOTOMETRIC.PHOTOMETRIC_CIELAB:
                    break;
                default:
                    emsg = string.Format("Sorry, can not handle image with {0}={1}",
                        TiffRGBAImage.photoTag, photometric);
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
            string old_name = m_name.Clone() as string;
            m_name = name.Clone() as string;
            return old_name;
        }

        // "tif" parameter can be null
        public static void Error(Tiff tif, string module, string fmt, params object[] ap)
        {
            m_errorHandler.ErrorHandler(tif, module, fmt, ap);
            m_errorHandler.ErrorHandlerExt(tif, null, module, fmt, ap);
        }

        public static void ErrorExt(Tiff tif, object fd, string module, string fmt, params object[] ap)
        {
            m_errorHandler.ErrorHandler(tif, module, fmt, ap);
            m_errorHandler.ErrorHandlerExt(tif, fd, module, fmt, ap);
        }

        public static void Warning(Tiff tif, string module, string fmt, params object[] ap)
        {
            m_errorHandler.WarningHandler(tif, module, fmt, ap);
            m_errorHandler.WarningHandlerExt(tif, null, module, fmt, ap);
        }

        public static void WarningExt(Tiff tif, object fd, string module, string fmt, params object[] ap)
        {
            m_errorHandler.WarningHandler(tif, module, fmt, ap);
            m_errorHandler.WarningHandlerExt(tif, fd, module, fmt, ap);
        }

        public static void Error(string module, string fmt, params object[] ap)
        {
            Error(null, module, fmt, ap);
        }

        public static void ErrorExt(object fd, string module, string fmt, params object[] ap)
        {
            ErrorExt(null, fd, module, fmt, ap);
        }

        public static void Warning(string module, string fmt, params object[] ap)
        {
            Warning(null, module, fmt, ap);
        }

        public static void WarningExt(object fd, string module, string fmt, params object[] ap)
        {
            WarningExt(null, fd, module, fmt, ap);
        }

        public static TiffErrorHandler SetErrorHandler(TiffErrorHandler errorHandler)
        {
            TiffErrorHandler prev = m_errorHandler;
            m_errorHandler = errorHandler;
            return prev;
        }

        public static TiffExtendProc SetTagExtender(TiffExtendProc proc)
        {
            TiffExtendProc prev = m_extender;
            m_extender = proc;
            return prev;
        }

        /*
        * Compute which tile an (x,y,z,s) value is in.
        */
        public int ComputeTile(int x, int y, int z, ushort s)
        {
            if (m_dir.td_imagedepth == 1)
                z = 0;

            int dx = m_dir.td_tilewidth;
            if (dx == -1)
                dx = m_dir.td_imagewidth;

            int dy = m_dir.td_tilelength;
            if (dy == -1)
                dy = m_dir.td_imagelength;

            int dz = m_dir.td_tiledepth;
            if (dz == -1)
                dz = m_dir.td_imagedepth;

            int tile = 1;
            if (dx != 0 && dy != 0 && dz != 0)
            {
                int xpt = howMany(m_dir.td_imagewidth, dx);
                int ypt = howMany(m_dir.td_imagelength, dy);
                int zpt = howMany(m_dir.td_imagedepth, dz);

                if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
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
        public bool CheckTile(int x, int y, int z, ushort s)
        {
            if (x >= m_dir.td_imagewidth)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Col out of range, max {1}", x, m_dir.td_imagewidth - 1);
                return false;
            }

            if (y >= m_dir.td_imagelength)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Row out of range, max {1}", y, m_dir.td_imagelength - 1);
                return false;
            }

            if (z >= m_dir.td_imagedepth)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Depth out of range, max {1}", z, m_dir.td_imagedepth - 1);
                return false;
            }

            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE && s >= m_dir.td_samplesperpixel)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Sample out of range, max {1}", s, m_dir.td_samplesperpixel - 1);
                return false;
            }

            return true;
        }

        /*
        * Compute how many tiles are in an image.
        */
        public int NumberOfTiles()
        {
            int dx = m_dir.td_tilewidth;
            if (dx == -1)
                dx = m_dir.td_imagewidth;
            
            int dy = m_dir.td_tilelength;
            if (dy == -1)
                dy = m_dir.td_imagelength;
            
            int dz = m_dir.td_tiledepth;
            if (dz == -1)
                dz = m_dir.td_imagedepth;
            
            int ntiles = (dx == 0 || dy == 0 || dz == 0) ? 0 : multiply(multiply(howMany(m_dir.td_imagewidth, dx), howMany(m_dir.td_imagelength, dy), "NumberOfTiles"), howMany(m_dir.td_imagedepth, dz), "NumberOfTiles");
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
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
        public int ReadTile(byte[] buf, int offset, int x, int y, int z, ushort s)
        {
            if (!checkRead(1) || !CheckTile(x, y, z, s))
                return -1;

            return ReadEncodedTile(ComputeTile(x, y, z, s), buf, offset, -1);
        }

        /*
        * Read a tile of data and decompress the specified
        * amount into the user-supplied buffer.
        */
        public int ReadEncodedTile(int tile, byte[] buf, int offset, int size)
        {
            if (!checkRead(1))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Tile out of range, max {1}", tile, m_dir.td_nstrips);
                return -1;
            }
            
            if (size == (int)-1)
                size = m_tilesize;
            else if (size > m_tilesize)
                size = m_tilesize;
            
            byte[] tempBuf = new byte [size];
            Array.Copy(buf, offset, tempBuf, 0, size);

            if (fillTile(tile) && m_currentCodec.tif_decodetile(tempBuf, size, (ushort)(tile / m_dir.td_stripsperimage)))
            {
                postDecode(tempBuf, size);
                Array.Copy(tempBuf, 0, buf, offset, size);
                return size;
            }

            return -1;
        }

        /*
        * Read a tile of data from the file.
        */
        public int ReadRawTile(int tile, byte[] buf, int offset, int size)
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
                ErrorExt(this, m_clientdata, m_name, "{0}: Tile out of range, max {1}", tile, m_dir.td_nstrips);
                return -1;
            }
            
            if ((m_flags & TIFF_NOREADRAW) != 0)
            {
                ErrorExt(m_clientdata, m_name, "Compression scheme does not support access to raw uncompressed data");
                return -1;
            }

            int bytecount = m_dir.td_stripbytecount[tile];
            if (size != (int)-1 && (uint)size < bytecount)
                bytecount = size;
            
            return readRawTile1(tile, buf, offset, bytecount, module);
        }

        /*
        * Write and compress a tile of data.  The
        * tile is selected by the (x,y,z,s) coordinates.
        */
        public int WriteTile(byte[] buf, int x, int y, int z, ushort s)
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
        public int ComputeStrip(int row, ushort sample)
        {
            int strip = row / m_dir.td_rowsperstrip;
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name, "{0}: Sample out of range, max {1}", sample, m_dir.td_samplesperpixel);
                    return 0;
                }

                strip += sample * m_dir.td_stripsperimage;
            }

            return strip;
        }

        /*
        * Compute how many strips are in an image.
        */
        public int NumberOfStrips()
        {
            int nstrips = (m_dir.td_rowsperstrip == -1 ? 1: howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip));
            if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                nstrips = multiply(nstrips, m_dir.td_samplesperpixel, "NumberOfStrips");

            return nstrips;
        }
        
        /*
        * Read a strip of data and decompress the specified
        * amount into the user-supplied buffer.
        */
        public int ReadEncodedStrip(int strip, byte[] buf, int offset, int size)
        {
            if (!checkRead(0))
                return -1;

            if (strip >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Strip out of range, max {1}", strip, m_dir.td_nstrips);
                return -1;
            }

            /*
             * Calculate the strip size according to the number of
             * rows in the strip (check for truncated last strip on any
             * of the separations).
             */
            int strips_per_sep;
            if (m_dir.td_rowsperstrip >= m_dir.td_imagelength)
                strips_per_sep = 1;
            else
                strips_per_sep = (m_dir.td_imagelength + m_dir.td_rowsperstrip - 1) / m_dir.td_rowsperstrip;

            int sep_strip = strip % strips_per_sep;

            int nrows = m_dir.td_imagelength % m_dir.td_rowsperstrip;
            if (sep_strip != strips_per_sep - 1 || nrows == 0)
                nrows = m_dir.td_rowsperstrip;

            int stripsize = VStripSize(nrows);
            if (size == (int)-1)
                size = stripsize;
            else if (size > stripsize)
                size = stripsize;
            
            byte[] tempBuf = new byte[size];
            Array.Copy(buf, offset, tempBuf, 0, size);

            if (fillStrip(strip) && m_currentCodec.tif_decodestrip(tempBuf, size, (ushort)(strip / m_dir.td_stripsperimage)))
            {
                postDecode(tempBuf, size);
                Array.Copy(tempBuf, 0, buf, offset, size);
                return size;
            }

            return -1;
        }

        /*
        * Read a strip of data from the file.
        */
        public int ReadRawStrip(int strip, byte[] buf, int offset, int size)
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
                ErrorExt(this, m_clientdata, m_name, "{0}: Strip out of range, max {1}", strip, m_dir.td_nstrips);
                return -1;
            }

            if ((m_flags & TIFF_NOREADRAW) != 0)
            {
                ErrorExt(this, m_clientdata, m_name, "Compression scheme does not support access to raw uncompressed data");
                return -1;
            }

            int bytecount = m_dir.td_stripbytecount[strip];
            if (bytecount <= 0)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Invalid strip byte count, strip {1}", bytecount, strip);
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
        public int WriteEncodedStrip(int strip, byte[] data, int cc)
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
                if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
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
            bufferCheck();

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
            ushort sample = (ushort)(strip / m_dir.td_stripsperimage);
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
        public int WriteRawStrip(int strip, byte[] data, int cc)
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
                if (m_dir.td_planarconfig == PLANARCONFIG.PLANARCONFIG_SEPARATE)
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
        public int WriteEncodedTile(int tile, byte[] data, int cc)
        {
            const string module = "WriteEncodedTile";
    
            if (!writeCheckTiles(module))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, module, "{0}: Tile {1} out of range, max {2}", m_name, tile, m_dir.td_nstrips);
                return -1;
            }

            /*
             * Handle delayed allocation of data buffer.  This
             * permits it to be sized more intelligently (using
             * directory information).
             */
            bufferCheck();

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
            ushort sample = (ushort)(tile / m_dir.td_stripsperimage);
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
        public int WriteRawTile(int tile, byte[] data, int cc)
        {
            const string module = "WriteRawTile";

            if (!writeCheckTiles(module))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, module, "{0}: Tile {1} out of range, max {2}", m_name, tile, m_dir.td_nstrips);
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
        public void SetWriteOffset(int off)
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
                case TiffDataType.TIFF_NOTYPE:
                case TiffDataType.TIFF_BYTE:
                case TiffDataType.TIFF_ASCII:
                case TiffDataType.TIFF_SBYTE:
                case TiffDataType.TIFF_UNDEFINED:
                    return 1;
                case TiffDataType.TIFF_SHORT:
                case TiffDataType.TIFF_SSHORT:
                    return 2;
                case TiffDataType.TIFF_LONG:
                case TiffDataType.TIFF_SLONG:
                case TiffDataType.TIFF_FLOAT:
                case TiffDataType.TIFF_IFD:
                    return 4;
                case TiffDataType.TIFF_RATIONAL:
                case TiffDataType.TIFF_SRATIONAL:
                case TiffDataType.TIFF_DOUBLE:
                    return 8;
                default:
                    /* will return 0 for unknown types */
                    return 0;
            }
        }

        /*
        * TIFF Library Bit & Byte Swapping Support.
        *
        * XXX We assume short = 16-bits and long = 32-bits XXX
        */
        public static void SwabShort(ref ushort wp)
        {
            byte[] cp = new byte[2];
            cp[0] = (byte)wp;
            cp[1] = (byte)(wp >> 8);

            byte t = cp[1];
            cp[1] = cp[0];
            cp[0] = t;

            wp = (ushort)(cp[0] & 0xFF);
            wp += (ushort)((cp[1] & 0xFF) << 8);
        }

        public static void SwabLong(ref int lp)
        {
            byte[] cp = new byte[4];
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
            byte[] bytes = BitConverter.GetBytes(dp);
            int[] lp = new int[2];
            lp[0] = BitConverter.ToInt32(bytes, 0);
            lp[0] = BitConverter.ToInt32(bytes, sizeof(int));

            SwabArrayOfLong(lp, 2);

            int t = lp[0];
            lp[0] = lp[1];
            lp[1] = t;

            Array.Copy(BitConverter.GetBytes(lp[0]), bytes, 0);
            Array.Copy(BitConverter.GetBytes(lp[1]), bytes, sizeof(int));
            dp = BitConverter.ToDouble(bytes, 0);
        }

        public static void SwabArrayOfShort(ushort[] wp, int n)
        {
            byte[] cp = new byte[2];
            for (int i = 0; i < n; i++)
            {
                cp[0] = (byte)wp[i];
                cp[1] = (byte)(wp[i] >> 8);

                byte t = cp[1];
                cp[1] = cp[0];
                cp[0] = t;

                wp[i] = (ushort)(cp[0] & 0xFF);
                wp[i] += (ushort)((cp[1] & 0xFF) << 8);
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

        public static void SwabArrayOfLong(int[] lp, int n)
        {
            byte[] cp = new byte[4];

            for (int i = 0; i < n; i++)
            {
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
            byte[] bytes = new byte[n * sizeof(double)];
            for (int i = 0; i < n; i++)
                Array.Copy(BitConverter.GetBytes(dp[i]), 0, bytes, i * sizeof(double), sizeof(double));

            int[] lp = byteArrayToInt(bytes, 0, n * sizeof(double));
            SwabArrayOfLong(lp, n + n);

            int lpPos = 0;
            while (n-- > 0)
            {
                int t = lp[lpPos];
                lp[lpPos] = lp[lpPos + 1];
                lp[lpPos + 1] = t;
                lpPos += 2;
            }

            intToByteArray(lp, 0, n + n, bytes, 0);
            for (int i = 0; i < n; i++)
                dp[i] = BitConverter.ToDouble(bytes, i * sizeof(double));
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

        public static int[] byteArrayToInt(byte[] b, int byteStartOffset, int byteCount)
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

        public static void intToByteArray(int[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
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

        public static uint[] byteArrayToUInt(byte[] b, int byteStartOffset, int byteCount)
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

        public static void uintToByteArray(uint[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
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

        public static short[] byteArrayToInt16(byte[] b, int byteStartOffset, int byteCount)
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

        public static void int16ToByteArray(Int16[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
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

        public static ushort[] byteArrayToUInt16(byte[] b, int byteStartOffset, int byteCount)
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

        public static void uint16ToByteArray(ushort[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
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
    }
}
