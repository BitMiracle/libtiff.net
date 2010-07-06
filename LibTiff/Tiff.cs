/* Copyright (C) 2008-2010, Bit Miracle
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
using System.Reflection;
using System.Globalization;

using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Tag Image File Format (TIFF)
    /// </summary>
    /// <remarks>Based on Rev 6.0 from <see href="http://partners.adobe.com/asn/developer/PDFS/TN/TIFF6.pdf" target="_blank"/></remarks>
#if EXPOSE_LIBTIFF
    public
#endif
 partial class Tiff : IDisposable
    {
        /// <summary>
        /// Support strip chopping (whether or not to convert single-strip 
        /// uncompressed images to mutiple strips of ~8Kb to reduce memory usage)
        /// </summary>
        internal const TiffFlags STRIPCHOP_DEFAULT = TiffFlags.STRIPCHOP;

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

        internal const string TIFFLIB_VERSION_STR = "LibTiff.NET, Version {0}\nCopyright (C) 2008-2010, Bit Miracle.";

        //
        // These constants can be used in code that requires
        // compilation-related definitions specific to a
        // version or versions of the library.  Runtime
        // version checking should be done based on the
        // string returned by GetVersion.

        /// <summary>
        /// TiffExtendProc
        /// </summary>
        public delegate void TiffExtendProc(Tiff tif);

        /// <summary>
        /// To override the default routine used to image decoded spans one can use the pseudo
        /// tag FAXFILLFUNC with an instance of this delegate.
        /// </summary>
        /// <param name="buf">place to set the bits</param>
        /// <param name="startOffset">index of first byte to process</param>
        /// <param name="runs">the array of black and white run lengths (white then black)</param>
        /// <param name="thisrun">current row's run array index</param>
        /// <param name="erun">the index of last run in the array</param>
        /// <param name="lastx">the width of the row in pixels</param>
        /// <remarks>Fill routines can assume the run array has room for at least
        /// <paramref name="lastx"/> runs and can overwrite data in the run array as
        /// needed (e.g. to append zero runs to bring the count up to a nice multiple).</remarks>
        public delegate void FaxFillFunc(byte[] buf, int startOffset, int[] runs, int thisrun, int erun, int lastx);

        private const int TIFF_VERSION = 42;
        private const int TIFF_BIGTIFF_VERSION = 43;

        private const short TIFF_BIGENDIAN = 0x4d4d;
        private const short TIFF_LITTLEENDIAN = 0x4949;
        private const short MDI_LITTLEENDIAN = 0x5045;

        // reference white
        private const float D50_X0 = 96.4250F;
        private const float D50_Y0 = 100.0F;
        private const float D50_Z0 = 82.4680F;

#if !SILVERLIGHT
        internal static Encoding Latin1Encoding = Encoding.GetEncoding("Latin1");
#else
        // Encoding.GetEncoding("Latin1") is not supported in Silverlight. Will throw exceptions at runtime.
        internal static Enc28591 Latin1Encoding = new Enc28591();
#endif
        /// <summary>
        /// Gets the library version.
        /// </summary>
        /// <returns>Library version.</returns>
        public static string GetVersion()
        {
            return string.Format(CultureInfo.InvariantCulture, TIFFLIB_VERSION_STR, AssemblyVersion);
        }

        /// <summary>
        /// Retrieves the version of assembly.
        /// </summary>
        public static string AssemblyVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                string versionString = version.Major.ToString(CultureInfo.InvariantCulture) +
                    "." + version.Minor.ToString(CultureInfo.InvariantCulture);

                versionString += "." + version.Build.ToString(CultureInfo.InvariantCulture);
                versionString += "." + version.Revision.ToString(CultureInfo.InvariantCulture);

                return versionString;
            }
        }

        /// <summary>
        /// Gets the R component from ABGR form returned by 
        /// <see cref="M:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage(System.Int32,System.Int32,System.Int32[])">ReadRGBAImage</see>.
        /// </summary>
        /// <param name="abgr">The ABGR.</param>
        /// <returns>R component.</returns>
        public static int GetR(int abgr)
        {
            return (abgr & 0xff);
        }

        /// <summary>
        /// Gets the G component from ABGR form returned by 
        /// <see cref="M:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage(System.Int32,System.Int32,System.Int32[])">ReadRGBAImage</see>.
        /// </summary>
        /// <param name="abgr">The ABGR.</param>
        /// <returns>G component.</returns>
        public static int GetG(int abgr)
        {
            return ((abgr >> 8) & 0xff);
        }

        /// <summary>
        /// Gets the B component from ABGR form returned by 
        /// <see cref="M:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage(System.Int32,System.Int32,System.Int32[])">ReadRGBAImage</see>.
        /// </summary>
        /// <param name="abgr">The ABGR.</param>
        /// <returns>B component.</returns>
        public static int GetB(int abgr)
        {
            return ((abgr >> 16) & 0xff);
        }

        /// <summary>
        /// Gets the A component from ABGR form returned by 
        /// <see cref="M:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage(System.Int32,System.Int32,System.Int32[])">ReadRGBAImage</see>.
        /// </summary>
        /// <param name="abgr">The ABGR.</param>
        /// <returns>A component.</returns>
        public static int GetA(int abgr)
        {
            return ((abgr >> 24) & 0xff);
        }

        /// <summary>
        /// Retrieves codec corresponding to compression scheme.
        /// </summary>
        /// <param name="scheme">The compression scheme.</param>
        /// <returns>Found codec.</returns>
        /// <remarks>Other compression schemes may be registered. 
        /// Registered schemes can also override the built in versions 
        /// provided by this library.
        /// </remarks>
        public TiffCodec FindCodec(Compression scheme)
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

        /// <summary>
        /// Registers codec.
        /// </summary>
        /// <param name="codec">Codec.</param>
        /// <returns><c>true</c> if registered successfully.</returns>
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

        /// <summary>
        /// Unregisters codec.
        /// </summary>
        /// <param name="c">Codec for unregistering.</param>
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

        /// <summary>
        /// Checks whether we have working codec for the specific coding scheme.
        /// </summary>
        /// <param name="scheme">The scheme to check.</param>
        /// <returns>
        /// <c>true</c> if the codec is configured and working; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCodecConfigured(Compression scheme)
        {
            TiffCodec codec = FindCodec(scheme);

            if (codec == null)
                return false;

            if (codec.CanEncode() != false || codec.CanDecode() != false)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the array of configured codecs, both built-in and
        /// registered by user.
        /// </summary>
        /// <returns>The array of configured codecs or <c>null</c> if function
        /// have failed.</returns>
        /// <remarks>The last record in array is null.</remarks>
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

            TiffCodec[] codecs = new TiffCodec[totalCodecs + 1];

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

        //
        // Auxiliary functions.
        //

        /// <summary>
        /// Re-allocates byte array and copies data from old to new array.
        /// </summary>
        /// <param name="oldBuffer">The old buffer.</param>
        /// <param name="elementCount">The element count.</param>
        /// <param name="newElementCount">The new element count.</param>
        /// <returns>The new allocated array.</returns>
        /// <remarks>Size is in elements, not bytes!</remarks>
        public static byte[] Realloc(byte[] oldBuffer, int elementCount, int newElementCount)
        {
            byte[] newBuffer = new byte[newElementCount];
            if (oldBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
        }

        /// <summary>
        /// Re-allocates int array and copies data from old to new array.
        /// </summary>
        /// <param name="oldBuffer">The old buffer.</param>
        /// <param name="elementCount">The element count.</param>
        /// <param name="newElementCount">The new element count.</param>
        /// <returns>The new allocated array.</returns>
        /// <remarks>Size is in elements, not bytes!</remarks>
        public static int[] Realloc(int[] oldBuffer, int elementCount, int newElementCount)
        {
            int[] newBuffer = new int[newElementCount];
            if (oldBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
        }

        /// <summary>
        /// Compares two arrays
        /// </summary>
        /// <param name="p1">The first array.</param>
        /// <param name="p2">The second array.</param>
        /// <param name="elementCount">The number of compared elements.</param>
        /// <returns>The difference between first different elements or 
        /// 0 if arrays are equal.</returns>
        public static int Compare(short[] p1, short[] p2, int elementCount)
        {
            for (int i = 0; i < elementCount; i++)
            {
                if (p1[i] != p2[i])
                    return p1[i] - p2[i];
            }

            return 0;
        }

        /// <summary>
        /// Initializes new instance of <see cref="Tiff"/> class.
        /// </summary>
        /// <param name="fileName">The target tiff file name.</param>
        /// <param name="mode">Can be "r", "w" or "a" for reading, writing and 
        /// appending correspondingly.</param>
        /// <returns>The created instance.</returns>
        public static Tiff Open(string fileName, string mode)
        {
            const string module = "Open";

            FileMode m;
            FileAccess a;
            getMode(mode, module, out m, out a);

            FileStream fd = null;
            if (a == FileAccess.Read)
                fd = File.Open(fileName, m, a, FileShare.Read);
            else
                fd = File.Open(fileName, m, a);

            Tiff tif = ClientOpen(fileName, mode, fd, new TiffStream());
            if (tif == null)
                fd.Dispose();
            else
                tif.m_fileStream = fd;

            return tif;
        }

        /// <summary>
        /// Similar to <see cref="Tiff.Open"/>, but more low-level.
        /// </summary>
        /// <param name="name">The target tiff file name.</param>
        /// <param name="mode">Can be "r", "w" or "a" for reading, writing and 
        /// appending correspondingly.</param>
        /// <param name="clientdata">Some client data.</param>
        /// <param name="stream">The tiff stream.</param>
        /// <returns>The created instance.</returns>
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
            tif.m_name = name;

            tif.m_mode = m & ~(O_CREAT | O_TRUNC);
            tif.m_curdir = -1; // non-existent directory
            tif.m_curoff = 0;
            tif.m_curstrip = -1; // invalid strip
            tif.m_row = -1; // read/write pre-increment
            tif.m_clientdata = clientdata;

            if (stream == null)
            {
                ErrorExt(tif, clientdata, module, "TiffStream is null pointer.");
                return null;
            }

            tif.m_stream = stream;

            // setup default state
            tif.m_currentCodec = tif.m_builtInCodecs[0];

            // Default is to return data MSB2LSB and enable the use of
            // memory-mapped files and strip chopping when a file is opened read-only.
            tif.m_flags = TiffFlags.MSB2LSB;

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
                            tif.m_flags |= TiffFlags.SWAB;
                        break;
                    case 'l':
                        break;
                    case 'B':
                        tif.m_flags = (tif.m_flags & ~TiffFlags.FILLORDER) | TiffFlags.MSB2LSB;
                        break;
                    case 'L':
                        tif.m_flags = (tif.m_flags & ~TiffFlags.FILLORDER) | TiffFlags.LSB2MSB;
                        break;
                    case 'H':
                        tif.m_flags = (tif.m_flags & ~TiffFlags.FILLORDER) | TiffFlags.LSB2MSB;
                        break;
                    case 'C':
                        if (m == O_RDONLY)
                            tif.m_flags |= TiffFlags.STRIPCHOP;
                        break;
                    case 'c':
                        if (m == O_RDONLY)
                            tif.m_flags &= ~TiffFlags.STRIPCHOP;
                        break;
                    case 'h':
                        tif.m_flags |= TiffFlags.HEADERONLY;
                        break;
                }
            }

            // Read in TIFF header.

            if ((tif.m_mode & O_TRUNC) != 0 || !tif.readHeaderOk(ref tif.m_header))
            {
                if (tif.m_mode == O_RDONLY)
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Cannot read TIFF header");
                    return tif.safeOpenFailed();
                }

                // Setup header and write.

                tif.m_header.tiff_magic = (tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB ? TIFF_BIGENDIAN : TIFF_LITTLEENDIAN;
                tif.m_header.tiff_version = TIFF_VERSION;
                if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                    SwabShort(ref tif.m_header.tiff_version);

                tif.m_header.tiff_diroff = 0; /* filled in later */

                tif.seekFile(0, SeekOrigin.Begin);

                if (!tif.writeHeaderOK(tif.m_header))
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Error writing TIFF header");
                    return tif.safeOpenFailed();
                }

                // Setup the byte order handling.
                tif.initOrder(tif.m_header.tiff_magic);

                // Setup default directory.
                tif.setupDefaultDirectory();
                tif.m_diroff = 0;
                tif.m_dirlist = null;
                tif.m_dirlistsize = 0;
                tif.m_dirnumber = 0;
                return tif;
            }

            // Setup the byte order handling.
            if (tif.m_header.tiff_magic != TIFF_BIGENDIAN && tif.m_header.tiff_magic != TIFF_LITTLEENDIAN && tif.m_header.tiff_magic != MDI_LITTLEENDIAN)
            {
                ErrorExt(tif, tif.m_clientdata, name,
                    "Not a TIFF or MDI file, bad magic number {0} (0x{1:x})",
                    tif.m_header.tiff_magic, tif.m_header.tiff_magic);
                return tif.safeOpenFailed();
            }

            tif.initOrder(tif.m_header.tiff_magic);

            // Swap header if required.
            if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
            {
                SwabShort(ref tif.m_header.tiff_version);
                SwabUInt(ref tif.m_header.tiff_diroff);
            }

            // Now check version (if needed, it's been byte-swapped).
            // Note that this isn't actually a version number, it's a
            // magic number that doesn't change (stupid).
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

            tif.m_flags |= TiffFlags.MYBUFFER;
            tif.m_rawcp = 0;
            tif.m_rawdata = null;
            tif.m_rawdatasize = 0;

            // Sometimes we do not want to read the first directory (for example,
            // it may be broken) and want to proceed to other directories. I this
            // case we use the HEADERONLY flag to open file and return
            // immediately after reading TIFF header.
            if ((tif.m_flags & TiffFlags.HEADERONLY) == TiffFlags.HEADERONLY)
                return tif;

            // Setup initial directory.
            switch (mode[0])
            {
                case 'r':
                    tif.m_nextdiroff = tif.m_header.tiff_diroff;

                    if (tif.ReadDirectory())
                    {
                        tif.m_rawcc = -1;
                        tif.m_flags |= TiffFlags.BUFFERSETUP;
                        return tif;
                    }
                    break;
                case 'a':
                    // New directories are automatically append to the end of
                    // the directory chain when they are written out (see WriteDirectory).
                    tif.setupDefaultDirectory();
                    return tif;
            }

            return tif.safeOpenFailed();
        }

        /// <summary>
        /// Closes a previously opened TIFF file.
        /// <remarks>This method closes a file or stream that was previously opened with
        /// <see cref="Open"/> or <see cref="ClientOpen"/>. Any buffered data are flushed to the
        /// file/stream, including the contents of the current directory (if modified); and all
        /// resources are reclaimed.</remarks>
        /// </summary>
        public void Close()
        {
            // Flush buffered data and directory (if dirty).
            Flush();

            m_stream.Close(m_clientdata);

            if (m_fileStream != null)
                m_fileStream.Close();
        }

        /// <summary>
        /// Frees and releases all resources allocated by this <see cref="Tiff"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //
        // Stuff, related to tag handling and creating custom tags.
        //

        /// <summary>
        /// Gets the number of elements in the tag list.
        /// </summary>
        /// <returns>The number of tags.</returns>
        public int GetTagListCount()
        {
            return m_dir.td_customValueCount;
        }

        /// <summary>
        /// Gets the required tag.
        /// </summary>
        /// <param name="tag_index">The index of required tag.</param>
        /// <returns>The required tag.</returns>
        public int GetTagListEntry(int tag_index)
        {
            if (tag_index < 0 || tag_index >= m_dir.td_customValueCount)
                return -1;
            else
                return (int)m_dir.td_customValues[tag_index].info.Tag;
        }

        /// <summary>
        /// Merges the field data.
        /// </summary>
        /// <param name="info">The array of field info.</param>
        /// <param name="n">The number of merged items from array.</param>
        public void MergeFieldInfo(TiffFieldInfo[] info, int n)
        {
            m_foundfield = null;

            if (m_nfields > 0)
                m_fieldinfo = Realloc(m_fieldinfo, m_nfields, m_nfields + n);
            else
                m_fieldinfo = new TiffFieldInfo[n];

            for (int i = 0; i < n; i++)
            {
                TiffFieldInfo fip = FindFieldInfo(info[i].Tag, info[i].Type);

                // only add definitions that aren't already present
                if (fip == null)
                {
                    m_fieldinfo[m_nfields] = info[i];
                    m_nfields++;
                }
            }

            // Sort the field info by tag number
            IComparer myComparer = new TagCompare();
            Array.Sort(m_fieldinfo, myComparer);
        }

        /// <summary>
        /// Retrieves field information for specified tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="dt">The tiff type.</param>
        /// <returns>The field information or <c>null</c> if the field isn't found.</returns>
        public TiffFieldInfo FindFieldInfo(TiffTag tag, TiffType dt)
        {
            if (m_foundfield != null && m_foundfield.Tag == tag && (dt == TiffType.ANY || dt == m_foundfield.Type))
                return m_foundfield;

            /* If we are invoked with no field information, then just return. */
            if (m_fieldinfo == null)
                return null;

            m_foundfield = null;

            foreach (TiffFieldInfo info in m_fieldinfo)
            {
                if (info != null && info.Tag == tag && (dt == TiffType.ANY || dt == info.Type))
                {
                    m_foundfield = info;
                    break;
                }
            }

            return m_foundfield;
        }

        /// <summary>
        /// Retrieves field information by field name.
        /// </summary>
        /// <param name="field_name">The field name.</param>
        /// <param name="dt">The tiff type.</param>
        /// <returns>The field information or <c>null</c> if the field isn't found.</returns>
        public TiffFieldInfo FindFieldInfoByName(string field_name, TiffType dt)
        {
            if (m_foundfield != null && m_foundfield.Name == field_name &&
                (dt == TiffType.ANY || dt == m_foundfield.Type))
            {
                return m_foundfield;
            }

            // If we are invoked with no field information, then just return.
            if (m_fieldinfo == null)
                return null;

            m_foundfield = null;

            foreach (TiffFieldInfo info in m_fieldinfo)
            {
                if (info != null && info.Name == field_name &&
                    (dt == TiffType.ANY || dt == info.Type))
                {
                    m_foundfield = info;
                    break;
                }
            }

            return m_foundfield;
        }

        /// <summary>
        /// Retrieves field information for specified tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>The field information or <c>null</c> if the field isn't found.</returns>
        public TiffFieldInfo FieldWithTag(TiffTag tag)
        {
            TiffFieldInfo fip = FindFieldInfo(tag, TiffType.ANY);
            if (fip != null)
                return fip;

            ErrorExt(this, m_clientdata, "FieldWithTag", "Internal error, unknown tag 0x{0:x}", tag);
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Retrieves field information by field name.
        /// </summary>
        /// <param name="field_name">The field name.</param>
        /// <returns>The field information or <c>null</c> if the field isn't found.</returns>
        public TiffFieldInfo FieldWithName(string field_name)
        {
            TiffFieldInfo fip = FindFieldInfoByName(field_name, TiffType.ANY);
            if (fip != null)
                return fip;

            ErrorExt(this, m_clientdata, "FieldWithName", "Internal error, unknown tag {0}", field_name);
            Debug.Assert(false);
            return null;
        }

        /// <summary>
        /// Gets the tag methods.
        /// </summary>
        /// <returns>Tag methods.</returns>
        public TiffTagMethods GetTagMethods()
        {
            return m_tagmethods;
        }

        /// <summary>
        /// Sets the tag methods.
        /// </summary>
        /// <param name="tagMethods">Tag methods.</param>
        /// <returns>Previous tag methods.</returns>
        public TiffTagMethods SetTagMethods(TiffTagMethods tagMethods)
        {
            TiffTagMethods oldTagMethods = m_tagmethods;

            if (tagMethods != null)
                m_tagmethods = tagMethods;

            return oldTagMethods;
        }

        /// <summary>
        /// Gets the information about client.
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Data</returns>
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

        /// <summary>
        /// Sets information about client.
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="name">Name</param>
        public void SetClientInfo(object data, string name)
        {
            clientInfoLink link = m_clientinfo;

            // Do we have an existing link with this name? If so, just set it.
            while (link != null && link.name != name)
                link = link.next;

            if (link != null)
            {
                link.data = data;
                return;
            }

            // Create a new link.
            link = new clientInfoLink();
            link.next = m_clientinfo;
            link.name = name;
            link.data = data;

            m_clientinfo = link;
        }

        /// <summary>
        /// Flushes tiff content to file.
        /// </summary>
        /// <returns><c>true</c> if succeed</returns>
        public bool Flush()
        {
            if (m_mode != O_RDONLY)
            {
                if (!FlushData())
                    return false;

                if ((m_flags & TiffFlags.DIRTYDIRECT) == TiffFlags.DIRTYDIRECT && !WriteDirectory())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Flushes buffered data to the file.
        /// </summary>
        /// <returns><c>true</c> if succeed.</returns>
        public bool FlushData()
        {
            if ((m_flags & TiffFlags.BEENWRITING) != TiffFlags.BEENWRITING)
                return false;

            if ((m_flags & TiffFlags.POSTENCODE) == TiffFlags.POSTENCODE)
            {
                m_flags &= ~TiffFlags.POSTENCODE;
                if (!m_currentCodec.PostEncode())
                    return false;
            }

            return flushData1();
        }

        /// <summary>
        /// Gets the value of field corresponding to speciefed tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>The value of a field in the internal directory structure.</returns>
        public FieldValue[] GetField(TiffTag tag)
        {
            TiffFieldInfo fip = FindFieldInfo(tag, TiffType.ANY);
            if (fip != null && (isPseudoTag(tag) || fieldSet(fip.Bit)))
                return m_tagmethods.GetField(this, tag);

            return null;
        }

        /// <summary>
        /// Like <see cref="Tiff.GetField"/>, but returns any default value 
        /// if the tag is not present in the directory.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns>Default value for tag.</returns>
        public FieldValue[] GetFieldDefaulted(TiffTag tag)
        {
            TiffDirectory td = m_dir;

            FieldValue[] result = GetField(tag);
            if (result != null)
                return result;

            switch (tag)
            {
                case TiffTag.SUBFILETYPE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_subfiletype);
                    break;
                case TiffTag.BITSPERSAMPLE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_bitspersample);
                    break;
                case TiffTag.THRESHHOLDING:
                    result = new FieldValue[1];
                    result[0].Set(td.td_threshholding);
                    break;
                case TiffTag.FILLORDER:
                    result = new FieldValue[1];
                    result[0].Set(td.td_fillorder);
                    break;
                case TiffTag.ORIENTATION:
                    result = new FieldValue[1];
                    result[0].Set(td.td_orientation);
                    break;
                case TiffTag.SAMPLESPERPIXEL:
                    result = new FieldValue[1];
                    result[0].Set(td.td_samplesperpixel);
                    break;
                case TiffTag.ROWSPERSTRIP:
                    result = new FieldValue[1];
                    result[0].Set(td.td_rowsperstrip);
                    break;
                case TiffTag.MINSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_minsamplevalue);
                    break;
                case TiffTag.MAXSAMPLEVALUE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_maxsamplevalue);
                    break;
                case TiffTag.PLANARCONFIG:
                    result = new FieldValue[1];
                    result[0].Set(td.td_planarconfig);
                    break;
                case TiffTag.RESOLUTIONUNIT:
                    result = new FieldValue[1];
                    result[0].Set(td.td_resolutionunit);
                    break;
                case TiffTag.PREDICTOR:
                    CodecWithPredictor sp = m_currentCodec as CodecWithPredictor;
                    if (sp != null)
                    {
                        result = new FieldValue[1];
                        result[0].Set(sp.GetPredictorValue());
                    }
                    break;
                case TiffTag.DOTRANGE:
                    result = new FieldValue[2];
                    result[0].Set(0);
                    result[1].Set((1 << td.td_bitspersample) - 1);
                    break;
                case TiffTag.INKSET:
                    result = new FieldValue[1];
                    result[0].Set(InkSet.CMYK);
                    break;
                case TiffTag.NUMBEROFINKS:
                    result = new FieldValue[1];
                    result[0].Set(4);
                    break;
                case TiffTag.EXTRASAMPLES:
                    result = new FieldValue[2];
                    result[0].Set(td.td_extrasamples);
                    result[1].Set(td.td_sampleinfo);
                    break;
                case TiffTag.MATTEING:
                    result = new FieldValue[1];
                    result[0].Set((td.td_extrasamples == 1 && td.td_sampleinfo[0] == ExtraSample.ASSOCALPHA));
                    break;
                case TiffTag.TILEDEPTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_tiledepth);
                    break;
                case TiffTag.DATATYPE:
                    result = new FieldValue[1];
                    result[0].Set(td.td_sampleformat - 1);
                    break;
                case TiffTag.SAMPLEFORMAT:
                    result = new FieldValue[1];
                    result[0].Set(td.td_sampleformat);
                    break;
                case TiffTag.IMAGEDEPTH:
                    result = new FieldValue[1];
                    result[0].Set(td.td_imagedepth);
                    break;
                case TiffTag.YCBCRCOEFFICIENTS:
                    {
                        /* defaults are from CCIR Recommendation 601-1 */
                        float[] ycbcrcoeffs = new float[3];
                        ycbcrcoeffs[0] = 0.299f;
                        ycbcrcoeffs[1] = 0.587f;
                        ycbcrcoeffs[2] = 0.114f;

                        result = new FieldValue[1];
                        result[0].Set(ycbcrcoeffs);
                        break;
                    }
                case TiffTag.YCBCRSUBSAMPLING:
                    result = new FieldValue[2];
                    result[0].Set(td.td_ycbcrsubsampling[0]);
                    result[1].Set(td.td_ycbcrsubsampling[1]);
                    break;
                case TiffTag.YCBCRPOSITIONING:
                    result = new FieldValue[1];
                    result[0].Set(td.td_ycbcrpositioning);
                    break;
                case TiffTag.WHITEPOINT:
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
                case TiffTag.TRANSFERFUNCTION:
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
                case TiffTag.REFERENCEBLACKWHITE:
                    if (td.td_refblackwhite == null)
                        defaultRefBlackWhite(td);

                    result = new FieldValue[1];
                    result[0].Set(td.td_refblackwhite);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Read the next TIFF directory from a file and convert it to the internal format.
        /// </summary>
        /// <remarks>We read directories sequentially.</remarks>
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
            m_currentCodec.Cleanup();
            m_curdir++;
            TiffDirEntry[] dir;
            short dircount = fetchDirectory(m_nextdiroff, out dir, out m_nextdiroff);
            if (dircount == 0)
            {
                ErrorExt(this, m_clientdata, module, "{0}: Failed to read directory at offset {1}", m_name, m_nextdiroff);
                return false;
            }

            m_flags &= ~TiffFlags.BEENWRITING; /* reset before new dir */

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
            SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

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
                if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                {
                    short temp = (short)dp.tdir_tag;
                    SwabShort(ref temp);
                    dp.tdir_tag = (TiffTag)(ushort)temp;

                    temp = (short)dp.tdir_type;
                    SwabShort(ref temp);
                    dp.tdir_type = (TiffType)temp;

                    SwabLong(ref dp.tdir_count);
                    SwabUInt(ref dp.tdir_offset);
                }

                if (dp.tdir_tag == TiffTag.SAMPLESPERPIXEL)
                {
                    if (!fetchNormalTag(dir[i]))
                        return false;

                    dp.tdir_tag = TiffTag.IGNORE;
                }
            }

            /*
             * First real pass over the directory.
             */
            int fix = 0;
            bool diroutoforderwarning = false;
            bool haveunknowntags = false;
            for (int i = 0; i < dircount; i++)
            {
                if (dir[i].tdir_tag == TiffTag.IGNORE)
                    continue;

                if (fix >= m_nfields)
                    fix = 0;

                /*
                 * Silicon Beach (at least) writes unordered
                 * directory tags (violating the spec).  Handle
                 * it here, but be obnoxious (maybe they'll fix it?).
                 */
                if (dir[i].tdir_tag < m_fieldinfo[fix].Tag)
                {
                    if (!diroutoforderwarning)
                    {
                        WarningExt(this, m_clientdata, module, "{0}: invalid TIFF directory; tags are not sorted in ascending order", m_name);
                        diroutoforderwarning = true;
                    }

                    fix = 0; /* O(n^2) */
                }

                while (fix < m_nfields && m_fieldinfo[fix].Tag < dir[i].tdir_tag)
                    fix++;

                if (fix >= m_nfields || m_fieldinfo[fix].Tag != dir[i].tdir_tag)
                {
                    /* Unknown tag ... we'll deal with it below */
                    haveunknowntags = true;
                    continue;
                }

                /*
                 * null out old tags that we ignore.
                 */
                if (m_fieldinfo[fix].Bit == FieldBit.Ignore)
                {
                    dir[i].tdir_tag = TiffTag.IGNORE;
                    continue;
                }

                /*
                 * Check data type.
                 */
                TiffFieldInfo fip = m_fieldinfo[fix];
                while (dir[i].tdir_type != fip.Type && fix < m_nfields)
                {
                    if (fip.Type == TiffType.ANY)
                    {
                        /* wildcard */
                        break;
                    }

                    fip = m_fieldinfo[++fix];
                    if (fix >= m_nfields || fip.Tag != dir[i].tdir_tag)
                    {
                        WarningExt(this, m_clientdata, module, "{0}: wrong data type {1} for \"{2}\"; tag ignored", m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].Name);
                        dir[i].tdir_tag = TiffTag.IGNORE;
                        continue;
                    }
                }

                // Check count if known in advance.
                if (fip.ReadCount != TiffFieldInfo.Variable &&
                    fip.ReadCount != TiffFieldInfo.Variable2)
                {
                    int expected = fip.ReadCount;
                    if (fip.ReadCount == TiffFieldInfo.Spp)
                        expected = m_dir.td_samplesperpixel;

                    if (!checkDirCount(dir[i], expected))
                    {
                        dir[i].tdir_tag = TiffTag.IGNORE;
                        continue;
                    }
                }

                switch (dir[i].tdir_tag)
                {
                    case TiffTag.COMPRESSION:
                        // The 5.0 spec says the Compression tag has one value,
                        // while earlier specs say it has one value per sample.
                        // Because of this, we accept the tag if one value is supplied.
                        if (dir[i].tdir_count == 1)
                        {
                            int v = extractData(dir[i]);
                            if (!SetField(dir[i].tdir_tag, v))
                                return false;

                            break;
                            // XXX: workaround for broken TIFFs
                        }
                        else if (dir[i].tdir_type == TiffType.LONG)
                        {
                            int v;
                            if (!fetchPerSampleLongs(dir[i], out v) || !SetField(dir[i].tdir_tag, v))
                                return false;
                        }
                        else
                        {
                            short iv;
                            if (!fetchPerSampleShorts(dir[i], out iv) || !SetField(dir[i].tdir_tag, iv))
                                return false;
                        }
                        dir[i].tdir_tag = TiffTag.IGNORE;
                        break;
                    case TiffTag.STRIPOFFSETS:
                    case TiffTag.STRIPBYTECOUNTS:
                    case TiffTag.TILEOFFSETS:
                    case TiffTag.TILEBYTECOUNTS:
                        setFieldBit(fip.Bit);
                        break;
                    case TiffTag.IMAGEWIDTH:
                    case TiffTag.IMAGELENGTH:
                    case TiffTag.IMAGEDEPTH:
                    case TiffTag.TILELENGTH:
                    case TiffTag.TILEWIDTH:
                    case TiffTag.TILEDEPTH:
                    case TiffTag.PLANARCONFIG:
                    case TiffTag.ROWSPERSTRIP:
                    case TiffTag.EXTRASAMPLES:
                        if (!fetchNormalTag(dir[i]))
                            return false;
                        dir[i].tdir_tag = TiffTag.IGNORE;
                        break;
                }
            }

            /*
             * If we saw any unknown tags, make an extra pass over the directory
             * to deal with them.  This must be done separately because the tags
             * could have become known when we registered a codec after finding
             * the Compression tag.  In a correctly-sorted directory there's
             * no problem because Compression will come before any codec-private
             * tags, but if the sorting is wrong that might not hold.
             */
            if (haveunknowntags)
            {
                fix = 0;
                for (int i = 0; i < dircount; i++)
                {
                    if (dir[i].tdir_tag == TiffTag.IGNORE)
                        continue;

                    if (fix >= m_nfields || dir[i].tdir_tag < m_fieldinfo[fix].Tag)
                    {
                        /* O(n^2) */
                        fix = 0;
                    }

                    while (fix < m_nfields && m_fieldinfo[fix].Tag < dir[i].tdir_tag)
                        fix++;

                    if (fix >= m_nfields || m_fieldinfo[fix].Tag != dir[i].tdir_tag)
                    {
                        Tiff.WarningExt(this, m_clientdata, module,
                            "{0}: unknown field with tag {1} (0x{2:x}) encountered",
                            m_name, (ushort)dir[i].tdir_tag, (ushort)dir[i].tdir_tag);

                        TiffFieldInfo[] arr = new TiffFieldInfo[1];
                        arr[0] = createAnonFieldInfo(dir[i].tdir_tag, dir[i].tdir_type);
                        MergeFieldInfo(arr, 1);

                        fix = 0;
                        while (fix < m_nfields && m_fieldinfo[fix].Tag < dir[i].tdir_tag)
                            fix++;
                    }

                    /*
                     * Check data type.
                     */
                    TiffFieldInfo fip = m_fieldinfo[fix];
                    while (dir[i].tdir_type != fip.Type && fix < m_nfields)
                    {
                        if (fip.Type == TiffType.ANY)
                        {
                            /* wildcard */
                            break;
                        }

                        fip = m_fieldinfo[++fix];
                        if (fix >= m_nfields || fip.Tag != dir[i].tdir_tag)
                        {
                            Tiff.WarningExt(this, m_clientdata, module,
                                "{0}: wrong data type {1} for \"{2}\"; tag ignored",
                                m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].Name);

                            dir[i].tdir_tag = TiffTag.IGNORE;
                            break;
                        }
                    }
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
            if ((m_dir.td_compression == Compression.OJPEG) && (m_dir.td_planarconfig == PlanarConfig.SEPARATE))
            {
                int dpIndex = readDirectoryFind(dir, dircount, TiffTag.STRIPOFFSETS);
                if (dpIndex != -1 && dir[dpIndex].tdir_count == 1)
                {
                    dpIndex = readDirectoryFind(dir, dircount, TiffTag.STRIPBYTECOUNTS);
                    if (dpIndex != -1 && dir[dpIndex].tdir_count == 1)
                    {
                        m_dir.td_planarconfig = PlanarConfig.CONTIG;
                        WarningExt(this, m_clientdata, "ReadDirectory",
                            "Planarconfig tag value assumed incorrect, assuming data is contig instead of chunky");
                    }
                }
            }

            /*
             * Allocate directory structure and setup defaults.
             */
            if (!fieldSet(FieldBit.ImageDimensions))
            {
                missingRequired("ImageLength");
                return false;
            }

            /* 
             * Setup appropriate structures (by strip or by tile)
             */
            if (!fieldSet(FieldBit.TileDimensions))
            {
                m_dir.td_nstrips = NumberOfStrips();
                m_dir.td_tilewidth = m_dir.td_imagewidth;
                m_dir.td_tilelength = m_dir.td_rowsperstrip;
                m_dir.td_tiledepth = m_dir.td_imagedepth;
                m_flags &= ~TiffFlags.ISTILED;
            }
            else
            {
                m_dir.td_nstrips = NumberOfTiles();
                m_flags |= TiffFlags.ISTILED;
            }

            if (m_dir.td_nstrips == 0)
            {
                ErrorExt(this, m_clientdata, module, "{0}: cannot handle zero number of {1}", m_name, IsTiled() ? "tiles" : "strips");
                return false;
            }

            m_dir.td_stripsperimage = m_dir.td_nstrips;
            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                m_dir.td_stripsperimage /= m_dir.td_samplesperpixel;

            if (!fieldSet(FieldBit.StripOffsets))
            {
                if ((m_dir.td_compression == Compression.OJPEG) && !IsTiled() && (m_dir.td_nstrips == 1))
                {
                    /*
                    * XXX: OJPEG hack.
                    * If a) compression is OJPEG, b) it's not a tiled TIFF,
                    * and c) the number of strips is 1,
                    * then we tolerate the absence of stripoffsets tag,
                    * because, presumably, all required data is in the
                    * JpegInterchangeFormat stream.
                    */
                    setFieldBit(FieldBit.StripOffsets);
                }
                else
                {
                    missingRequired(IsTiled() ? "TileOffsets" : "StripOffsets");
                    return false;
                }
            }

            /*
             * Second pass: extract other information.
             */
            for (int i = 0; i < dircount; i++)
            {
                if (dir[i].tdir_tag == TiffTag.IGNORE)
                    continue;

                switch (dir[i].tdir_tag)
                {
                    case TiffTag.MINSAMPLEVALUE:
                    case TiffTag.MAXSAMPLEVALUE:
                    case TiffTag.BITSPERSAMPLE:
                    case TiffTag.DATATYPE:
                    case TiffTag.SAMPLEFORMAT:
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
                            if (!SetField(dir[i].tdir_tag, v))
                                return false;
                            /* XXX: workaround for broken TIFFs */
                        }
                        else if (dir[i].tdir_tag == TiffTag.BITSPERSAMPLE && dir[i].tdir_type == TiffType.LONG)
                        {
                            int v;
                            if (!fetchPerSampleLongs(dir[i], out v) || !SetField(dir[i].tdir_tag, v))
                                return false;
                        }
                        else
                        {
                            short iv;
                            if (!fetchPerSampleShorts(dir[i], out iv) || !SetField(dir[i].tdir_tag, iv))
                                return false;
                        }
                        break;
                    case TiffTag.SMINSAMPLEVALUE:
                    case TiffTag.SMAXSAMPLEVALUE:
                        double dv;
                        if (!fetchPerSampleAnys(dir[i], out dv) || !SetField(dir[i].tdir_tag, dv))
                            return false;
                        break;
                    case TiffTag.STRIPOFFSETS:
                    case TiffTag.TILEOFFSETS:
                        if (!fetchStripThing(dir[i], m_dir.td_nstrips, ref m_dir.td_stripoffset))
                            return false;
                        break;
                    case TiffTag.STRIPBYTECOUNTS:
                    case TiffTag.TILEBYTECOUNTS:
                        if (!fetchStripThing(dir[i], m_dir.td_nstrips, ref m_dir.td_stripbytecount))
                            return false;
                        break;
                    case TiffTag.COLORMAP:
                    case TiffTag.TRANSFERFUNCTION:
                        {
                            /*
                             * TransferFunction can have either 1x or 3x
                             * data values; Colormap can have only 3x
                             * items.
                             */
                            int v = 1 << m_dir.td_bitspersample;
                            if (dir[i].tdir_tag == TiffTag.COLORMAP || dir[i].tdir_count != v)
                            {
                                if (!checkDirCount(dir[i], 3 * v))
                                    break;
                            }

                            byte[] cp = new byte[dir[i].tdir_count * sizeof(short)];
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
                                    short[] u = ByteArrayToShorts(cp, 0, dir[i].tdir_count * sizeof(short));
                                    SetField(dir[i].tdir_tag, u, u, u);
                                }
                                else
                                {
                                    v *= sizeof(short);
                                    short[] u0 = ByteArrayToShorts(cp, 0, v);
                                    short[] u1 = ByteArrayToShorts(cp, v, v);
                                    short[] u2 = ByteArrayToShorts(cp, 2 * v, v);
                                    SetField(dir[i].tdir_tag, u0, u1, u2);
                                }
                            }
                            break;
                        }
                    case TiffTag.PAGENUMBER:
                    case TiffTag.HALFTONEHINTS:
                    case TiffTag.YCBCRSUBSAMPLING:
                    case TiffTag.DOTRANGE:
                        fetchShortPair(dir[i]);
                        break;
                    case TiffTag.REFERENCEBLACKWHITE:
                        fetchRefBlackWhite(dir[i]);
                        break;
                    /* BEGIN REV 4.0 COMPATIBILITY */
                    case TiffTag.OSUBFILETYPE:
                        FileType ft = 0;
                        switch ((OFileType)extractData(dir[i]))
                        {
                            case OFileType.REDUCEDIMAGE:
                                ft = FileType.REDUCEDIMAGE;
                                break;
                            case OFileType.PAGE:
                                ft = FileType.PAGE;
                                break;
                        }

                        if (ft != 0)
                            SetField(TiffTag.SUBFILETYPE, ft);

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
            if (m_dir.td_compression == Compression.OJPEG)
            {
                if (!fieldSet(FieldBit.Photometric))
                {
                    WarningExt(this, m_clientdata, "ReadDirectory", "Photometric tag is missing, assuming data is YCbCr");
                    if (!SetField(TiffTag.PHOTOMETRIC, Photometric.YCBCR))
                        return false;
                }
                else if (m_dir.td_photometric == Photometric.RGB)
                {
                    m_dir.td_photometric = Photometric.YCBCR;
                    WarningExt(this, m_clientdata, "ReadDirectory", "Photometric tag value assumed incorrect, assuming data is YCbCr instead of RGB");
                }

                if (!fieldSet(FieldBit.BitsPerSample))
                {
                    WarningExt(this, m_clientdata, "ReadDirectory", "BitsPerSample tag is missing, assuming 8 bits per sample");
                    if (!SetField(TiffTag.BITSPERSAMPLE, 8))
                        return false;
                }

                if (!fieldSet(FieldBit.SamplesPerPixel))
                {
                    if ((m_dir.td_photometric == Photometric.RGB) || (m_dir.td_photometric == Photometric.YCBCR))
                    {
                        WarningExt(this, m_clientdata, "ReadDirectory", "SamplesPerPixel tag is missing, assuming correct SamplesPerPixel value is 3");
                        if (!SetField(TiffTag.SAMPLESPERPIXEL, 3))
                            return false;
                    }
                    else if ((m_dir.td_photometric == Photometric.MINISWHITE) || (m_dir.td_photometric == Photometric.MINISBLACK))
                    {
                        WarningExt(this, m_clientdata, "ReadDirectory", "SamplesPerPixel tag is missing, assuming correct SamplesPerPixel value is 1");
                        if (!SetField(TiffTag.SAMPLESPERPIXEL, 1))
                            return false;
                    }
                }
            }

            /*
             * Verify Palette image has a Colormap.
             */
            if (m_dir.td_photometric == Photometric.PALETTE && !fieldSet(FieldBit.ColorMap))
            {
                missingRequired("Colormap");
                return false;
            }

            /*
            * OJPEG hack:
            * We do no further messing with strip/tile offsets/bytecounts in OJPEG
            * TIFFs
            */
            if (m_dir.td_compression != Compression.OJPEG)
            {
                /*
                 * Attempt to deal with a missing StripByteCounts tag.
                 */
                if (!fieldSet(FieldBit.StripByteCounts))
                {
                    /*
                     * Some manufacturers violate the spec by not giving
                     * the size of the strips.  In this case, assume there
                     * is one uncompressed strip of data.
                     */
                    if ((m_dir.td_planarconfig == PlanarConfig.CONTIG && m_dir.td_nstrips > 1) ||
                        (m_dir.td_planarconfig == PlanarConfig.SEPARATE && m_dir.td_nstrips != m_dir.td_samplesperpixel))
                    {
                        missingRequired("StripByteCounts");
                        return false;
                    }

                    WarningExt(this, m_clientdata, module, "{0}: TIFF directory is missing required \"{1}\" field, calculating from imagelength", m_name, FieldWithTag(TiffTag.STRIPBYTECOUNTS).Name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return false;
                }
                else if (m_dir.td_nstrips == 1 && m_dir.td_stripoffset[0] != 0 && byteCountLooksBad(m_dir))
                {
                    /*
                     * XXX: Plexus (and others) sometimes give a value of zero for
                     * a tag when they don't know what the correct value is!  Try
                     * and handle the simple case of estimating the size of a one
                     * strip image.
                     */
                    WarningExt(this, m_clientdata, module, "{0}: Bogus \"{1}\" field, ignoring and calculating from imagelength", m_name, FieldWithTag(TiffTag.STRIPBYTECOUNTS).Name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return false;
                }
                else if (m_dir.td_planarconfig == PlanarConfig.CONTIG && m_dir.td_nstrips > 2 && m_dir.td_compression == Compression.NONE && m_dir.td_stripbytecount[0] != m_dir.td_stripbytecount[1])
                {
                    /*
                     * XXX: Some vendors fill StripByteCount array with absolutely
                     * wrong values (it can be equal to StripOffset array, for
                     * example). Catch this case here.
                     */
                    WarningExt(this, m_clientdata, module, "{0}: Wrong \"{1}\" field, ignoring and calculating from imagelength", m_name, FieldWithTag(TiffTag.STRIPBYTECOUNTS).Name);
                    if (!estimateStripByteCounts(dir, dircount))
                        return false;
                }
            }

            dir = null;

            if (!fieldSet(FieldBit.MaxSampleValue))
                m_dir.td_maxsamplevalue = (short)((1 << m_dir.td_bitspersample) - 1);

            /*
             * Setup default compression scheme.
             */

            /*
             * XXX: We can optimize checking for the strip bounds using the sorted
             * bytecounts array. See also comments for appendToStrip() function.
             */
            if (m_dir.td_nstrips > 1)
            {
                m_dir.td_stripbytecountsorted = true;
                for (int strip = 1; strip < m_dir.td_nstrips; strip++)
                {
                    if (m_dir.td_stripoffset[strip - 1] > m_dir.td_stripoffset[strip])
                    {
                        m_dir.td_stripbytecountsorted = false;
                        break;
                    }
                }
            }

            if (!fieldSet(FieldBit.Compression))
                SetField(TiffTag.COMPRESSION, Compression.NONE);

            // Some manufacturers make life difficult by writing large amounts
            // of uncompressed data as a single strip. This is contrary to the
            // recommendations of the spec. The following makes an attempt at
            // breaking such images into strips closer to the recommended 8k
            // bytes. A side effect, however, is that the RowsPerStrip tag
            // value may be changed.
            if (m_dir.td_nstrips == 1 && m_dir.td_compression == Compression.NONE &&
                (m_flags & TiffFlags.STRIPCHOP) == TiffFlags.STRIPCHOP &&
                (m_flags & TiffFlags.ISTILED) != TiffFlags.ISTILED)
            {
                chopUpSingleUncompressedStrip();
            }

            // Reinitialize i/o since we are starting on a new directory.
            m_row = -1;
            m_curstrip = -1;
            m_col = -1;
            m_curtile = -1;
            m_tilesize = -1;

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

        /// <summary>
        /// Read custom directory from the arbitrary offset.
        /// </summary>
        /// <param name="diroff">Directory offset.</param>
        /// <param name="info">Field info.</param>
        /// <param name="n">The number of elements in field info array.</param>
        /// <returns><c>true</c> if succeed.</returns>
        /// <remarks>The code is very similar to <see cref="Tiff.ReadDirectory"/>.</remarks>
        public bool ReadCustomDirectory(long diroff, TiffFieldInfo[] info, int n)
        {
            const string module = "ReadCustomDirectory";

            setupFieldInfo(info, n);

            uint dummyNextDirOff;
            TiffDirEntry[] dir;
            short dircount = fetchDirectory((uint)diroff, out dir, out dummyNextDirOff);
            if (dircount == 0)
            {
                ErrorExt(this, m_clientdata, module,
                    "{0}: Failed to read custom directory at offset {1}", m_name, diroff);
                return false;
            }

            FreeDirectory();
            m_dir = new TiffDirectory();

            int fix = 0;
            for (short i = 0; i < dircount; i++)
            {
                if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                {
                    short temp = (short)dir[i].tdir_tag;
                    SwabShort(ref temp);
                    dir[i].tdir_tag = (TiffTag)(ushort)temp;

                    temp = (short)dir[i].tdir_type;
                    SwabShort(ref temp);
                    dir[i].tdir_type = (TiffType)temp;

                    SwabLong(ref dir[i].tdir_count);
                    SwabUInt(ref dir[i].tdir_offset);
                }

                if (fix >= m_nfields || dir[i].tdir_tag == TiffTag.IGNORE)
                    continue;

                while (fix < m_nfields && m_fieldinfo[fix].Tag < dir[i].tdir_tag)
                    fix++;

                if (fix >= m_nfields || m_fieldinfo[fix].Tag != dir[i].tdir_tag)
                {
                    WarningExt(this, m_clientdata, module,
                        "{0}: unknown field with tag {1} (0x{2:x}) encountered",
                        m_name, (ushort)dir[i].tdir_tag, (ushort)dir[i].tdir_tag);

                    TiffFieldInfo[] arr = new TiffFieldInfo[1];
                    arr[0] = createAnonFieldInfo(dir[i].tdir_tag, dir[i].tdir_type);
                    MergeFieldInfo(arr, 1);

                    fix = 0;
                    while (fix < m_nfields && m_fieldinfo[fix].Tag < dir[i].tdir_tag)
                        fix++;
                }

                /*
                 * null out old tags that we ignore.
                 */
                if (m_fieldinfo[fix].Bit == FieldBit.Ignore)
                {
                    dir[i].tdir_tag = TiffTag.IGNORE;
                    continue;
                }

                /*
                 * Check data type.
                 */
                TiffFieldInfo fip = m_fieldinfo[fix];
                while (dir[i].tdir_type != fip.Type && fix < m_nfields)
                {
                    if (fip.Type == TiffType.ANY)
                    {
                        /* wildcard */
                        break;
                    }

                    fip = m_fieldinfo[++fix];
                    if (fix >= m_nfields || fip.Tag != dir[i].tdir_tag)
                    {
                        WarningExt(this, m_clientdata, module, "{0}: wrong data type {1} for \"{2}\"; tag ignored", m_name, dir[i].tdir_type, m_fieldinfo[fix - 1].Name);
                        dir[i].tdir_tag = TiffTag.IGNORE;
                        continue;
                    }
                }

                // Check count if known in advance.
                if (fip.ReadCount != TiffFieldInfo.Variable &&
                    fip.ReadCount != TiffFieldInfo.Variable2)
                {
                    int expected = fip.ReadCount;
                    if (fip.ReadCount == TiffFieldInfo.Spp)
                        expected = m_dir.td_samplesperpixel;

                    if (!checkDirCount(dir[i], expected))
                    {
                        dir[i].tdir_tag = TiffTag.IGNORE;
                        continue;
                    }
                }

                /*
                * EXIF tags which need to be specifically processed.
                */
                switch (dir[i].tdir_tag)
                {
                    case TiffTag.EXIF_SUBJECTDISTANCE:
                        fetchSubjectDistance(dir[i]);
                        break;
                    default:
                        fetchNormalTag(dir[i]);
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Writes custom directory.
        /// </summary>
        /// <param name="pdiroff">Output directory offset.</param>
        /// <returns><c>true</c> if succeed.</returns>
        public bool WriteCustomDirectory(out long pdiroff)
        {
            pdiroff = -1;

            if (m_mode == O_RDONLY)
                return true;

            // Size the directory so that we can calculate offsets for the data
            // items that aren't kept in-place in each field.
            int nfields = 0;
            for (int b = 0; b <= FieldBit.Last; b++)
            {
                if (fieldSet(b) && b != FieldBit.Custom)
                    nfields += (b < FieldBit.SubFileType ? 2 : 1);
            }

            nfields += m_dir.td_customValueCount;
            int dirsize = nfields * TiffDirEntry.SizeInBytes;
            TiffDirEntry[] data = new TiffDirEntry[nfields];

            // Put the directory at the end of the file.
            m_diroff = (uint)((seekFile(0, SeekOrigin.End) + 1) & ~1);
            m_dataoff = m_diroff + sizeof(short) + (uint)dirsize + sizeof(int);
            if ((m_dataoff & 1) != 0)
                m_dataoff++;

            seekFile(m_dataoff, SeekOrigin.Begin);

            // Setup external form of directory entries and write data items.
            int[] fields = new int[FieldBit.SetLongs];
            Array.Copy(m_dir.td_fieldsset, fields, FieldBit.SetLongs);

            for (int fi = 0, nfi = m_nfields; nfi > 0; nfi--, fi++)
            {
                TiffFieldInfo fip = m_fieldinfo[fi];

                // For custom fields, we test to see if the custom field
                // is set or not.  For normal fields, we just use the FieldSet test.
                if (fip.Bit == FieldBit.Custom)
                {
                    bool is_set = false;
                    for (int ci = 0; ci < m_dir.td_customValueCount; ci++)
                        is_set |= (m_dir.td_customValues[ci].info == fip);

                    if (!is_set)
                        continue;
                }
                else if (!fieldSet(fields, fip.Bit))
                    continue;

                if (fip.Bit != FieldBit.Custom)
                    resetFieldBit(fields, fip.Bit);
            }

            // Write directory.

            short dircount = (short)nfields;
            pdiroff = m_nextdiroff;
            if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
            {
                // The file's byte order is opposite to the native machine
                // architecture. We overwrite the directory information with
                // impunity because it'll be released below after we write it
                // to the file. Note that all the other tag construction
                // routines assume that we do this byte-swapping; i.e. they only
                // byte-swap indirect data.
                for (int i = 0; i < dircount; i++)
                {
                    TiffDirEntry dirEntry = data[i];

                    short temp = (short)dirEntry.tdir_tag;
                    SwabShort(ref temp);
                    dirEntry.tdir_tag = (TiffTag)(ushort)temp;

                    temp = (short)dirEntry.tdir_type;
                    SwabShort(ref temp);
                    dirEntry.tdir_type = (TiffType)temp;

                    SwabLong(ref dirEntry.tdir_count);
                    SwabUInt(ref dirEntry.tdir_offset);
                }

                dircount = (short)nfields;
                SwabShort(ref dircount);

                int tempOff = (int)pdiroff;
                SwabLong(ref tempOff);
                pdiroff = tempOff;
            }

            seekFile(m_diroff, SeekOrigin.Begin);
            if (!writeShortOK(dircount))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory count");
                return false;
            }

            if (!writeDirEntryOK(data, dirsize / TiffDirEntry.SizeInBytes))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory contents");
                return false;
            }

            if (!writeIntOK((int)pdiroff))
            {
                ErrorExt(this, m_clientdata, m_name, "Error writing directory link");
                return false;
            }

            return true;
        }

        /// <summary>
        /// EXIF is important special case of custom IFD, so we have a special function to read it.
        /// </summary>
        /// <param name="diroff">Directory offset.</param>
        /// <returns><c>true</c> if read successfully.</returns>
        public bool ReadEXIFDirectory(long diroff)
        {
            int exifFieldInfoCount;
            TiffFieldInfo[] exifFieldInfo = getExifFieldInfo(out exifFieldInfoCount);
            return ReadCustomDirectory(diroff, exifFieldInfo, exifFieldInfoCount);
        }

        /// <summary>
        /// Return the number of bytes to read/write in a call to one of the
        /// scanline-oriented i/o routines.
        /// Note that this number may be 1/samples-per-pixel if data is
        /// stored as separate planes.
        /// </summary>
        /// <returns>The number of bytes to read/write.</returns>
        public int ScanlineSize()
        {
            int scanline;
            if (m_dir.td_planarconfig == PlanarConfig.CONTIG)
            {
                if (m_dir.td_photometric == Photometric.YCBCR && !IsUpSampled())
                {
                    FieldValue[] result = GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
                    short ycbcrsubsampling0 = result[0].ToShort();

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

        /// <summary>
        /// Return the number of bytes required to store a complete decoded and 
        /// packed raster scanline.
        /// </summary>
        /// <returns>The number of bytes required to store a complete decoded and 
        /// packed raster scanline (as opposed to the I/O size returned by ScanlineSize 
        /// which may be less if data is store as separate planes).</returns>
        public int RasterScanlineSize()
        {
            int scanline = multiply(m_dir.td_bitspersample, m_dir.td_imagewidth, "RasterScanlineSize");
            if (m_dir.td_planarconfig == PlanarConfig.CONTIG)
            {
                scanline = multiply(scanline, m_dir.td_samplesperpixel, "RasterScanlineSize");
                return howMany8(scanline);
            }

            return multiply(howMany8(scanline), m_dir.td_samplesperpixel, "RasterScanlineSize");
        }

        /*
        * 
        *
        * 
        */
        /// <summary>
        /// Compute the number bytes in a row-aligned strip.
        /// </summary>
        /// <returns>The number bytes in a row-aligned strip</returns>
        /// <remarks>Note that if the value of field corresponding to
        /// <see cref="TiffTag.ROWSPERSTRIP"/> is larger than the recorded 
        /// <see cref="TiffTag.IMAGELENGTH"/>, then the strip size is truncated 
        /// to reflect the actual space required to hold the strip.
        /// </remarks>
        public int StripSize()
        {
            int rps = m_dir.td_rowsperstrip;
            if (rps > m_dir.td_imagelength)
                rps = m_dir.td_imagelength;

            return VStripSize(rps);
        }

        /// <summary>
        /// Compute the number of bytes in a raw strip.
        /// </summary>
        /// <param name="strip">The index of strip.</param>
        /// <returns>The number of bytes in a raw strip.</returns>
        public long RawStripSize(int strip)
        {
            long bytecount = m_dir.td_stripbytecount[strip];
            if (bytecount <= 0)
            {
                ErrorExt(this, m_clientdata, m_name,
                    "{0}: Invalid strip byte count, strip {1}", bytecount, strip);
                bytecount = -1;
            }

            return bytecount;
        }

        /// <summary>
        /// Compute the number of bytes in a variable height, row-aligned strip.
        /// </summary>
        /// <param name="nrows">The number of rows in a strip.</param>
        /// <returns>The number of bytes in a variable height, row-aligned strip.</returns>
        public int VStripSize(int nrows)
        {
            if (nrows == -1)
                nrows = m_dir.td_imagelength;

            if (m_dir.td_planarconfig == PlanarConfig.CONTIG && m_dir.td_photometric == Photometric.YCBCR && !IsUpSampled())
            {
                /*
                 * Packed YCbCr data contain one Cb+Cr for every
                 * HorizontalSampling * VerticalSampling Y values.
                 * Must also roundup width and height when calculating
                 * since images that are not a multiple of the
                 * horizontal/vertical subsampling area include
                 * YCbCr data for the extended image.
                 */
                FieldValue[] result = GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
                short ycbcrsubsampling0 = result[0].ToShort();
                short ycbcrsubsampling1 = result[1].ToShort();

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

        /// <summary>
        /// Compute the number of bytes in each row of a tile.
        /// </summary>
        /// <returns>The number of bytes in the each row of a tile.</returns>
        public int TileRowSize()
        {
            if (m_dir.td_tilelength == 0 || m_dir.td_tilewidth == 0)
                return 0;

            int rowsize = multiply(m_dir.td_bitspersample, m_dir.td_tilewidth, "TileRowSize");
            if (m_dir.td_planarconfig == PlanarConfig.CONTIG)
                rowsize = multiply(rowsize, m_dir.td_samplesperpixel, "TileRowSize");

            return howMany8(rowsize);
        }

        /// <summary>
        /// Compute the number of bytes in a row-aligned tile.
        /// </summary>
        /// <returns>The number of bytes in a row-aligned tile.</returns>
        public int TileSize()
        {
            return VTileSize(m_dir.td_tilelength);
        }

        /// <summary>
        /// Computes the number of bytes in a variable length, row-aligned tile.
        /// </summary>
        /// <param name="nrows">The number of rows in a tile.</param>
        /// <returns>
        /// The number of bytes in a variable length, row-aligned tile.
        /// </returns>
        public int VTileSize(int nrows)
        {
            if (m_dir.td_tilelength == 0 || m_dir.td_tilewidth == 0 || m_dir.td_tiledepth == 0)
                return 0;

            int tilesize;
            if (m_dir.td_planarconfig == PlanarConfig.CONTIG && m_dir.td_photometric == Photometric.YCBCR && !IsUpSampled())
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
            {
                tilesize = multiply(nrows, TileRowSize(), "VTileSize");
            }

            return multiply(tilesize, m_dir.td_tiledepth, "VTileSize");
        }

        /*
        *   
        */
        /// <summary>
        /// Computes a default strip size based on the image characteristics and a requested value.
        /// </summary>
        /// <param name="request">The requested value.</param>
        /// <returns>The default strip size</returns>
        /// <remarks>If the request is &lt;1 then we choose a 
        /// strip size according to certain heuristics.</remarks>
        public int DefaultStripSize(int request)
        {
            return m_currentCodec.DefStripSize(request);
        }

        /*
        *   If a
        * request is <1 then we choose a size according
        * to certain heuristics.
        */
        /// <summary>
        /// Computes a default tile size based on the image characteristics and a requested value.
        /// </summary>
        /// <param name="tw">Result tile width.</param>
        /// <param name="th">Result tile height.</param>
        public void DefaultTileSize(ref int tw, ref int th)
        {
            m_currentCodec.DefTileSize(ref tw, ref th);
        }

        /// <summary>
        /// Returns the custom client data.
        /// </summary>
        /// <returns>The client data.</returns>
        public object Clientdata()
        {
            return m_clientdata;
        }

        /// <summary>
        /// Sets the custom client data.
        /// </summary>
        /// <param name="newvalue">The client data.</param>
        /// <returns>Previous value.</returns>
        public object SetClientdata(object newvalue)
        {
            object m = m_clientdata;
            m_clientdata = newvalue;
            return m;
        }

        /// <summary>
        /// Gets the read/write mode.
        /// </summary>
        /// <returns>Read/write mode</returns>
        public int GetMode()
        {
            return m_mode;
        }

        /// <summary>
        /// Sets the read/write mode.
        /// </summary>
        /// <param name="mode">Read/write mode</param>
        /// <returns>Previous mode.</returns>
        public int SetMode(int mode)
        {
            int old_mode = m_mode;
            m_mode = mode;
            return old_mode;
        }

        /// <summary>
        /// Determines whether this tiff file is organized in tiles.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance is tiled; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTiled()
        {
            return ((m_flags & TiffFlags.ISTILED) == TiffFlags.ISTILED);
        }

        /// <summary>
        /// Determines whether the file has byte-swapped data.
        /// </summary>
        /// <returns><c>true</c> if the file has byte-swapped data; otherwise, <c>false</c>.</returns>
        public bool IsByteSwapped()
        {
            return ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB);
        }

        /// <summary>
        /// Determines whether the data is returned up-sampled.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if the data is returned up-sampled; otherwise, <c>false</c>.
        /// </returns>
        public bool IsUpSampled()
        {
            return ((m_flags & TiffFlags.UPSAMPLED) == TiffFlags.UPSAMPLED);
        }

        /// <summary>
        /// Determines whether the data is returned in MSB-to-LSB bit order.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if the data is returned in MSB-to-LSB bit order; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMSB2LSB()
        {
            return isFillOrder(FillOrder.MSB2LSB);
        }

        /// <summary>
        /// Determines whether given file was written in big-endian order.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if given file was written in big-endian order; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBigEndian()
        {
            return (m_header.tiff_magic == TIFF_BIGENDIAN);
        }

        /// <summary>
        /// Gets the tiff stream.
        /// </summary>
        /// <returns>The tiff stream.</returns>
        public TiffStream GetStream()
        {
            return m_stream;
        }

        /// <summary>
        /// Current row being read/written.
        /// </summary>
        /// <returns>Current row being read/written.</returns>
        public int CurrentRow()
        {
            return m_row;
        }

        /// <summary>
        /// Gets the index of the current directory.
        /// </summary>
        /// <returns>index of the current directory</returns>
        public short CurrentDirectory()
        {
            return m_curdir;
        }

        /// <summary>
        /// Gets the number of directories in a file.
        /// </summary>
        /// <returns>The number of directories in a file.</returns>
        public short NumberOfDirectories()
        {
            uint nextdir = m_header.tiff_diroff;
            short n = 0;
            long dummyOff;
            while (nextdir != 0 && advanceDirectory(ref nextdir, out dummyOff))
                n++;

            return n;
        }

        /// <summary>
        /// Returns file offset of the current directory.
        /// </summary>
        /// <returns>File offset of the current directory.</returns>
        public long CurrentDirOffset()
        {
            return m_diroff;
        }

        /// <summary>
        /// Returns current strip index.
        /// </summary>
        /// <returns>Current strip index.</returns>
        public int CurrentStrip()
        {
            return m_curstrip;
        }

        /// <summary>
        /// Returns current tile index.
        /// </summary>
        /// <returns>Current tile index.</returns>
        public int CurrentTile()
        {
            return m_curtile;
        }

        /// <summary>
        /// Setup the raw data buffer in preparation for reading a strip of raw data.
        /// </summary>
        /// <param name="bp">The data buffer.</param>
        /// <param name="size">The buffer size.</param>
        /// <remarks>If the buffer is specified as <c>null</c>, then a buffer of 
        /// appropriate size is allocated by the library. Otherwise,
        /// the client must guarantee that the buffer is large enough 
        /// to hold any individual strip of raw data.
        /// </remarks>
        public void ReadBufferSetup(byte[] bp, int size)
        {
            Debug.Assert((m_flags & TiffFlags.NOREADRAW) != TiffFlags.NOREADRAW);
            m_rawdata = null;

            if (bp != null)
            {
                m_rawdatasize = size;
                m_rawdata = bp;
                m_flags &= ~TiffFlags.MYBUFFER;
            }
            else
            {
                m_rawdatasize = roundUp(size, 1024);
                if (m_rawdatasize > 0)
                {
                    m_rawdata = new byte[m_rawdatasize];
                }
                else
                {
                    Tiff.ErrorExt(this, m_clientdata,
                        "ReadBufferSetup", "{0}: No space for data buffer at scanline {1}", m_name, m_row);
                    m_rawdatasize = 0;
                }

                m_flags |= TiffFlags.MYBUFFER;
            }
        }

        /// <summary>
        /// Setup the raw data buffer used for encoding.
        /// </summary>
        /// <param name="bp">The data buffer.</param>
        /// <param name="size">The buffer size.</param>
        public void WriteBufferSetup(byte[] bp, int size)
        {
            if (m_rawdata != null)
            {
                if ((m_flags & TiffFlags.MYBUFFER) == TiffFlags.MYBUFFER)
                    m_flags &= ~TiffFlags.MYBUFFER;

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
                bp = new byte[size];
                m_flags |= TiffFlags.MYBUFFER;
            }
            else
                m_flags &= ~TiffFlags.MYBUFFER;

            m_rawdata = bp;
            m_rawdatasize = size;
            m_rawcc = 0;
            m_rawcp = 0;
            m_flags |= TiffFlags.BUFFERSETUP;
        }

        /// <summary>
        /// Setups the strips.
        /// </summary>
        /// <returns><c>true</c> if setup successfully; otherwise, <c>false</c></returns>
        public bool SetupStrips()
        {
            if (IsTiled())
                m_dir.td_stripsperimage = isUnspecified(FieldBit.TileDimensions) ? m_dir.td_samplesperpixel : NumberOfTiles();
            else
                m_dir.td_stripsperimage = isUnspecified(FieldBit.RowsPerStrip) ? m_dir.td_samplesperpixel : NumberOfStrips();

            m_dir.td_nstrips = m_dir.td_stripsperimage;

            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                m_dir.td_stripsperimage /= m_dir.td_samplesperpixel;

            m_dir.td_stripoffset = new uint[m_dir.td_nstrips];
            m_dir.td_stripbytecount = new uint[m_dir.td_nstrips];

            setFieldBit(FieldBit.StripOffsets);
            setFieldBit(FieldBit.StripByteCounts);
            return true;
        }

        /*
        *   In doing the latter
        * we also "freeze" the state of the directory so
        * that important information is not changed.
        */
        /// <summary>
        /// Verify file is writable and that the directory information is setup properly.
        /// </summary>
        /// <param name="tiles">The number of tiles.</param>
        /// <param name="module">The module.</param>
        /// <returns><c>true</c> if checked successfully; otherwise, <c>false</c></returns>
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
                ErrorExt(this, m_clientdata, m_name, tiles != 0 ? "Can not write tiles to a stripped image" : "Can not write scanlines to a tiled image");
                return false;
            }

            /*
             * On the first write verify all the required information
             * has been setup and initialize any data structures that
             * had to wait until directory information was set.
             * Note that a lot of our work is assumed to remain valid
             * because we disallow any of the important parameters
             * from changing after we start writing (i.e. once
             * BEENWRITING is set, TIFFSetField will only allow
             * the image's length to be changed).
             */
            if (!fieldSet(FieldBit.ImageDimensions))
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
                if (!fieldSet(FieldBit.PlanarConfig))
                    m_dir.td_planarconfig = PlanarConfig.CONTIG;
            }
            else
            {
                if (!fieldSet(FieldBit.PlanarConfig))
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

            m_tilesize = IsTiled() ? TileSize() : -1;
            m_scanlinesize = ScanlineSize();
            m_flags |= TiffFlags.BEENWRITING;
            return true;
        }

        /// <summary>
        /// Release storage associated with a directory.
        /// </summary>
        public void FreeDirectory()
        {
            if (m_dir != null)
            {
                clearFieldBit(FieldBit.YCbCrSubsampling);
                clearFieldBit(FieldBit.YCbCrPositioning);

                m_dir = null;
            }
        }

        /// <summary>
        /// Creates a new directory.
        /// </summary>
        /// <remarks>The newly created directory will not exist on the file till
        /// WriteDirectory(), Flush() or Close() is called.</remarks>
        public void CreateDirectory()
        {
            // Should we automatically call WriteDirectory()
            // if the current one is dirty?

            setupDefaultDirectory();
            m_diroff = 0;
            m_nextdiroff = 0;
            m_curoff = 0;
            m_row = -1;
            m_curstrip = -1;
        }

        /// <summary>
        /// Returns an indication of whether the current directory is the last directory
        /// in the file.
        /// </summary>
        /// <returns><c>true</c> if current directory is the last directory in the file.</returns>
        public bool LastDirectory()
        {
            return (m_nextdiroff == 0);
        }

        /// <summary>
        /// Set the directory with specified number as the current directory.
        /// </summary>
        /// <param name="dirn">The directory number.</param>
        /// <returns><c>true</c> if set successfully</returns>
        /// <remarks>Directories are numbered starting at 0.</remarks>
        public bool SetDirectory(short dirn)
        {
            short n;
            long dummyOff;
            uint nextdir = m_header.tiff_diroff;
            for (n = dirn; n > 0 && nextdir != 0; n--)
            {
                if (!advanceDirectory(ref nextdir, out dummyOff))
                    return false;
            }

            m_nextdiroff = nextdir;

            // Set curdir to the actual directory index. The -1 is because
            // ReadDirectory will increment m_curdir after successfully reading
            // the directory.
            m_curdir = (short)(dirn - n - 1);

            // Reset m_dirnumber counter and start new list of seen directories.
            // We need this to prevent IFD loops.
            m_dirnumber = 0;
            return ReadDirectory();
        }

        /// <summary>
        /// Set the current directory to be the directory located at the
        /// specified file offset.
        /// </summary>
        /// <param name="diroff">The file offset of the directory to set
        /// as current.</param>
        /// <returns><c>true</c> if set successfully</returns>
        /// <remarks>This method is used mainly to access directories linked
        /// with the SubIFD tag (e.g. thumbnail images).</remarks>
        public bool SetSubDirectory(long diroff)
        {
            m_nextdiroff = (uint)diroff;

            // Reset m_dirnumber counter and start new list of seen directories.
            // We need this to prevent IFD loops.
            m_dirnumber = 0;
            return ReadDirectory();
        }

        /// <summary>
        /// Unlinks the specified directory from the directory chain.
        /// </summary>
        /// <param name="dirn">The directory number.</param>
        /// <returns><c>true</c> if unlinked successfully</returns>
        public bool UnlinkDirectory(short dirn)
        {
            const string module = "UnlinkDirectory";

            if (m_mode == O_RDONLY)
            {
                ErrorExt(this, m_clientdata, module, "Can not unlink directory in read-only file");
                return false;
            }

            // Go to the directory before the one we want
            // to unlink and nab the offset of the link
            // field we'll need to patch.
            uint nextdir = m_header.tiff_diroff;
            long off = sizeof(short) + sizeof(short);
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
            long dummyOff;
            if (!advanceDirectory(ref nextdir, out dummyOff))
                return false;

            /*
             * Go back and patch the link field of the preceding
             * directory to point to the offset of the directory
             * that follows.
             */
            seekFile(off, SeekOrigin.Begin);
            if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                SwabUInt(ref nextdir);

            if (!writeIntOK((int)nextdir))
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
            m_currentCodec.Cleanup();
            if ((m_flags & TiffFlags.MYBUFFER) == TiffFlags.MYBUFFER && m_rawdata != null)
            {
                m_rawdata = null;
                m_rawcc = 0;
            }

            m_flags &= ~(TiffFlags.BEENWRITING | TiffFlags.BUFFERSETUP | TiffFlags.POSTENCODE);
            FreeDirectory();
            setupDefaultDirectory();
            m_diroff = 0; /* force link on next write */
            m_nextdiroff = 0; /* next write must be at end */
            m_curoff = 0;
            m_row = -1;
            m_curstrip = -1;
            return true;
        }

        /// <summary>
        /// Records the value of a field in the internal directory structure.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="ap">The field value(s).</param>
        /// <returns><c>true</c> if set successfully</returns>
        /// <remarks>The field will be written to the file
        /// when/if the directory structure is updated.
        /// </remarks>
        public bool SetField(TiffTag tag, params object[] ap)
        {
            if (okToChangeTag(tag))
                return m_tagmethods.SetField(this, tag, FieldValue.FromParams(ap));

            return false;
        }

        /// <summary>
        /// Writes the directory.
        /// </summary>
        /// <returns><c>true</c> if written successfully.</returns>
        public bool WriteDirectory()
        {
            return writeDirectory(true);
        }

        /*
        *   
        */
        /// <summary>
        /// Similar to <see cref="Tiff.WriteDirectory"/>, writes the directory out
        /// but leaves all data structures in memory so that it can be written again.
        /// </summary>
        /// <returns><c>true</c> if written successfully.</returns>
        /// <remarks>This will make a partially written TIFF file 
        /// readable before it is successfully completed/closed.</remarks>
        public bool CheckpointDirectory()
        {
            /* Setup the strips arrays, if they haven't already been. */
            if (m_dir.td_stripoffset == null)
                SetupStrips();

            bool rc = writeDirectory(false);
            SetWriteOffset(seekFile(0, SeekOrigin.End));
            return rc;
        }

        /// <summary>
        /// Similar to <see cref="Tiff.WriteDirectory"/>, but if the directory has already
        /// been written once, it is relocated to the end of the file, in case it
        /// has changed in size.
        /// </summary>
        /// <returns><c>true</c> if written successfully.</returns>
        /// <remarks>Note that this will result in the loss of the 
        /// previously used directory space.</remarks>
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
                if (!writeIntOK((int)m_header.tiff_diroff))
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
                    short dircount;
                    if (!seekOK(nextdir) || !readShortOK(out dircount))
                    {
                        ErrorExt(this, m_clientdata, module, "Error fetching directory count");
                        return false;
                    }

                    if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                        SwabShort(ref dircount);

                    seekFile(dircount * TiffDirEntry.SizeInBytes, SeekOrigin.Current);

                    if (!readUIntOK(out nextdir))
                    {
                        ErrorExt(this, m_clientdata, module, "Error fetching directory link");
                        return false;
                    }

                    if ((m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                        SwabUInt(ref nextdir);
                }
                while (nextdir != m_diroff && nextdir != 0);

                // get current offset
                long off = seekFile(0, SeekOrigin.Current);
                seekFile(off - sizeof(int), SeekOrigin.Begin);
                m_diroff = 0;

                if (!writeIntOK((int)m_diroff))
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

        /// <summary>
        /// Prints formatted description of the contents of the current directory to the
        /// specified stream.
        /// </summary>
        /// <overloads>
        /// Prints formatted description of the contents of the current directory to the
        /// specified stream.
        /// </overloads>
        /// <param name="fd">The stream.</param>
        public void PrintDirectory(Stream fd)
        {
            PrintDirectory(fd, TiffPrintFlags.NONE);
        }

        /// <summary>
        /// Prints formatted description of the contents of the current directory to the
        /// specified stream using specified formatting options.
        /// </summary>
        /// <param name="fd">The stream.</param>
        /// <param name="flags">The flags.</param>
        public void PrintDirectory(Stream fd, TiffPrintFlags flags)
        {
            fprintf(fd, "TIFF Directory at offset 0x{0:x} ({1})\n", m_diroff, m_diroff);

            if (fieldSet(FieldBit.SubFileType))
            {
                fprintf(fd, "  Subfile Type:");
                string sep = " ";
                if ((m_dir.td_subfiletype & FileType.REDUCEDIMAGE) != 0)
                {
                    fprintf(fd, "{0}reduced-resolution image", sep);
                    sep = "/";
                }

                if ((m_dir.td_subfiletype & FileType.PAGE) != 0)
                {
                    fprintf(fd, "{0}multi-page document", sep);
                    sep = "/";
                }

                if ((m_dir.td_subfiletype & FileType.MASK) != 0)
                    fprintf(fd, "{0}transparency mask", sep);

                fprintf(fd, " ({0} = 0x{1:x})\n", m_dir.td_subfiletype, m_dir.td_subfiletype);
            }

            if (fieldSet(FieldBit.ImageDimensions))
            {
                fprintf(fd, "  Image Width: {0} Image Length: {1}", m_dir.td_imagewidth, m_dir.td_imagelength);
                if (fieldSet(FieldBit.ImageDepth))
                    fprintf(fd, " Image Depth: {0}", m_dir.td_imagedepth);
                fprintf(fd, "\n");
            }

            if (fieldSet(FieldBit.TileDimensions))
            {
                fprintf(fd, "  Tile Width: {0} Tile Length: {1}", m_dir.td_tilewidth, m_dir.td_tilelength);
                if (fieldSet(FieldBit.TileDepth))
                    fprintf(fd, " Tile Depth: {0}", m_dir.td_tiledepth);
                fprintf(fd, "\n");
            }

            if (fieldSet(FieldBit.Resolution))
            {
                fprintf(fd, "  Resolution: {0:G}, {1:G}", m_dir.td_xresolution, m_dir.td_yresolution);
                if (fieldSet(FieldBit.ResolutionUnit))
                {
                    switch (m_dir.td_resolutionunit)
                    {
                        case ResUnit.NONE:
                            fprintf(fd, " (unitless)");
                            break;
                        case ResUnit.INCH:
                            fprintf(fd, " pixels/inch");
                            break;
                        case ResUnit.CENTIMETER:
                            fprintf(fd, " pixels/cm");
                            break;
                        default:
                            fprintf(fd, " (unit {0} = 0x{1:x})", m_dir.td_resolutionunit, m_dir.td_resolutionunit);
                            break;
                    }
                }
                fprintf(fd, "\n");
            }

            if (fieldSet(FieldBit.Position))
                fprintf(fd, "  Position: {0:G}, {1:G}\n", m_dir.td_xposition, m_dir.td_yposition);

            if (fieldSet(FieldBit.BitsPerSample))
                fprintf(fd, "  Bits/Sample: {0}\n", m_dir.td_bitspersample);

            if (fieldSet(FieldBit.SampleFormat))
            {
                fprintf(fd, "  Sample Format: ");
                switch (m_dir.td_sampleformat)
                {
                    case SampleFormat.VOID:
                        fprintf(fd, "void\n");
                        break;
                    case SampleFormat.INT:
                        fprintf(fd, "signed integer\n");
                        break;
                    case SampleFormat.UINT:
                        fprintf(fd, "unsigned integer\n");
                        break;
                    case SampleFormat.IEEEFP:
                        fprintf(fd, "IEEE floating point\n");
                        break;
                    case SampleFormat.COMPLEXINT:
                        fprintf(fd, "complex signed integer\n");
                        break;
                    case SampleFormat.COMPLEXIEEEFP:
                        fprintf(fd, "complex IEEE floating point\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_sampleformat, m_dir.td_sampleformat);
                        break;
                }
            }

            if (fieldSet(FieldBit.Compression))
            {
                TiffCodec c = FindCodec(m_dir.td_compression);
                fprintf(fd, "  Compression Scheme: ");
                if (c != null)
                    fprintf(fd, "{0}\n", c.m_name);
                else
                    fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_compression, m_dir.td_compression);
            }

            if (fieldSet(FieldBit.Photometric))
            {
                fprintf(fd, "  Photometric Interpretation: ");
                if ((int)m_dir.td_photometric < photoNames.Length)
                    fprintf(fd, "{0}\n", photoNames[(int)m_dir.td_photometric]);
                else
                {
                    switch (m_dir.td_photometric)
                    {
                        case Photometric.LOGL:
                            fprintf(fd, "CIE Log2(L)\n");
                            break;
                        case Photometric.LOGLUV:
                            fprintf(fd, "CIE Log2(L) (u',v')\n");
                            break;
                        default:
                            fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_photometric, m_dir.td_photometric);
                            break;
                    }
                }
            }

            if (fieldSet(FieldBit.ExtraSamples) && m_dir.td_extrasamples != 0)
            {
                fprintf(fd, "  Extra Samples: {0}<", m_dir.td_extrasamples);
                string sep = "";
                for (short i = 0; i < m_dir.td_extrasamples; i++)
                {
                    switch (m_dir.td_sampleinfo[i])
                    {
                        case ExtraSample.UNSPECIFIED:
                            fprintf(fd, "{0}unspecified", sep);
                            break;
                        case ExtraSample.ASSOCALPHA:
                            fprintf(fd, "{0}assoc-alpha", sep);
                            break;
                        case ExtraSample.UNASSALPHA:
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

            if (fieldSet(FieldBit.InkNames))
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

            if (fieldSet(FieldBit.Thresholding))
            {
                fprintf(fd, "  Thresholding: ");
                switch (m_dir.td_threshholding)
                {
                    case Threshold.BILEVEL:
                        fprintf(fd, "bilevel art scan\n");
                        break;
                    case Threshold.HALFTONE:
                        fprintf(fd, "halftone or dithered scan\n");
                        break;
                    case Threshold.ERRORDIFFUSE:
                        fprintf(fd, "error diffused\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_threshholding, m_dir.td_threshholding);
                        break;
                }
            }

            if (fieldSet(FieldBit.FillOrder))
            {
                fprintf(fd, "  FillOrder: ");
                switch (m_dir.td_fillorder)
                {
                    case FillOrder.MSB2LSB:
                        fprintf(fd, "msb-to-lsb\n");
                        break;
                    case FillOrder.LSB2MSB:
                        fprintf(fd, "lsb-to-msb\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_fillorder, m_dir.td_fillorder);
                        break;
                }
            }

            if (fieldSet(FieldBit.YCbCrSubsampling))
            {
                /*
                 * For hacky reasons (see tif_jpeg.c - JPEGFixupTestSubsampling),
                 * we need to fetch this rather than trust what is in our
                 * structures.
                 */
                FieldValue[] result = GetField(TiffTag.YCBCRSUBSAMPLING);
                short subsampling0 = result[0].ToShort();
                short subsampling1 = result[1].ToShort();
                fprintf(fd, "  YCbCr Subsampling: {0}, {1}\n", subsampling0, subsampling1);
            }

            if (fieldSet(FieldBit.YCbCrPositioning))
            {
                fprintf(fd, "  YCbCr Positioning: ");
                switch (m_dir.td_ycbcrpositioning)
                {
                    case YCbCrPosition.CENTERED:
                        fprintf(fd, "centered\n");
                        break;
                    case YCbCrPosition.COSITED:
                        fprintf(fd, "cosited\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_ycbcrpositioning, m_dir.td_ycbcrpositioning);
                        break;
                }
            }

            if (fieldSet(FieldBit.HalftoneHints))
                fprintf(fd, "  Halftone Hints: light {0} dark {1}\n", m_dir.td_halftonehints[0], m_dir.td_halftonehints[1]);

            if (fieldSet(FieldBit.Orientation))
            {
                fprintf(fd, "  Orientation: ");
                if ((int)m_dir.td_orientation < orientNames.Length)
                    fprintf(fd, "{0}\n", orientNames[(int)m_dir.td_orientation]);
                else
                    fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_orientation, m_dir.td_orientation);
            }

            if (fieldSet(FieldBit.SamplesPerPixel))
                fprintf(fd, "  Samples/Pixel: {0}\n", m_dir.td_samplesperpixel);

            if (fieldSet(FieldBit.RowsPerStrip))
            {
                fprintf(fd, "  Rows/Strip: ");
                if (m_dir.td_rowsperstrip == -1)
                    fprintf(fd, "(infinite)\n");
                else
                    fprintf(fd, "{0}\n", m_dir.td_rowsperstrip);
            }

            if (fieldSet(FieldBit.MinSampleValue))
                fprintf(fd, "  Min Sample Value: {0}\n", m_dir.td_minsamplevalue);

            if (fieldSet(FieldBit.MaxSampleValue))
                fprintf(fd, "  Max Sample Value: {0}\n", m_dir.td_maxsamplevalue);

            if (fieldSet(FieldBit.SMinSampleValue))
                fprintf(fd, "  SMin Sample Value: {0:G}\n", m_dir.td_sminsamplevalue);

            if (fieldSet(FieldBit.SMaxSampleValue))
                fprintf(fd, "  SMax Sample Value: {0:G}\n", m_dir.td_smaxsamplevalue);

            if (fieldSet(FieldBit.PlanarConfig))
            {
                fprintf(fd, "  Planar Configuration: ");
                switch (m_dir.td_planarconfig)
                {
                    case PlanarConfig.CONTIG:
                        fprintf(fd, "single image plane\n");
                        break;
                    case PlanarConfig.SEPARATE:
                        fprintf(fd, "separate image planes\n");
                        break;
                    default:
                        fprintf(fd, "{0} (0x{1:x})\n", m_dir.td_planarconfig, m_dir.td_planarconfig);
                        break;
                }
            }

            if (fieldSet(FieldBit.PageNumber))
                fprintf(fd, "  Page Number: {0}-{1}\n", m_dir.td_pagenumber[0], m_dir.td_pagenumber[1]);

            if (fieldSet(FieldBit.ColorMap))
            {
                fprintf(fd, "  Color Map: ");
                if ((flags & TiffPrintFlags.COLORMAP) != 0)
                {
                    fprintf(fd, "\n");
                    int n = 1 << m_dir.td_bitspersample;
                    for (int l = 0; l < n; l++)
                        fprintf(fd, "   {0,5}: {1,5} {2,5} {3,5}\n", l, m_dir.td_colormap[0][l], m_dir.td_colormap[1][l], m_dir.td_colormap[2][l]);
                }
                else
                    fprintf(fd, "(present)\n");
            }

            if (fieldSet(FieldBit.TransferFunction))
            {
                fprintf(fd, "  Transfer Function: ");
                if ((flags & TiffPrintFlags.CURVES) != 0)
                {
                    fprintf(fd, "\n");
                    int n = 1 << m_dir.td_bitspersample;
                    for (int l = 0; l < n; l++)
                    {
                        fprintf(fd, "    {0,2}: {0,5}", l, m_dir.td_transferfunction[0][l]);
                        for (short i = 1; i < m_dir.td_samplesperpixel; i++)
                            fprintf(fd, " {0,5}", m_dir.td_transferfunction[i][l]);
                        fprintf(fd, "\n");
                    }
                }
                else
                    fprintf(fd, "(present)\n");
            }

            if (fieldSet(FieldBit.SubIFD) && m_dir.td_subifd != null)
            {
                fprintf(fd, "  SubIFD Offsets:");
                for (short i = 0; i < m_dir.td_nsubifd; i++)
                    fprintf(fd, " {0,5}", m_dir.td_subifd[i]);
                fprintf(fd, "\n");
            }

            // Custom tag support.

            int count = GetTagListCount();
            for (int i = 0; i < count; i++)
            {
                TiffTag tag = (TiffTag)GetTagListEntry(i);
                TiffFieldInfo fip = FieldWithTag(tag);
                if (fip == null)
                    continue;

                byte[] raw_data = null;
                int value_count;
                if (fip.PassCount)
                {
                    FieldValue[] result = GetField(tag);
                    if (result == null)
                        continue;

                    value_count = result[0].ToInt();
                    raw_data = result[1].ToByteArray();
                }
                else
                {
                    if (fip.ReadCount == TiffFieldInfo.Variable ||
                        fip.ReadCount == TiffFieldInfo.Variable2)
                    {
                        value_count = 1;
                    }
                    else if (fip.ReadCount == TiffFieldInfo.Spp)
                    {
                        value_count = m_dir.td_samplesperpixel;
                    }
                    else
                    {
                        value_count = fip.ReadCount;
                    }

                    if ((fip.Type == TiffType.ASCII ||
                        fip.ReadCount == TiffFieldInfo.Variable ||
                        fip.ReadCount == TiffFieldInfo.Variable2 ||
                        fip.ReadCount == TiffFieldInfo.Spp ||
                        value_count > 1) &&
                        fip.Tag != TiffTag.PAGENUMBER &&
                        fip.Tag != TiffTag.HALFTONEHINTS &&
                        fip.Tag != TiffTag.YCBCRSUBSAMPLING &&
                        fip.Tag != TiffTag.DOTRANGE)
                    {
                        FieldValue[] result = GetField(tag);
                        if (result == null)
                            continue;

                        raw_data = result[0].ToByteArray();
                    }
                    else if (fip.Tag != TiffTag.PAGENUMBER &&
                        fip.Tag != TiffTag.HALFTONEHINTS &&
                        fip.Tag != TiffTag.YCBCRSUBSAMPLING &&
                        fip.Tag != TiffTag.DOTRANGE)
                    {
                        raw_data = new byte[dataSize(fip.Type) * value_count];

                        FieldValue[] result = GetField(tag);
                        if (result == null)
                            continue;

                        raw_data = result[0].ToByteArray();
                    }
                    else
                    {
                        // XXX: Should be fixed and removed, see the notes
                        // related to PAGENUMBER, HALFTONEHINTS,
                        // YCBCRSUBSAMPLING and DOTRANGE tags
                        raw_data = new byte[dataSize(fip.Type) * value_count];

                        FieldValue[] result = GetField(tag);
                        if (result == null)
                            continue;

                        byte[] first = result[0].ToByteArray();
                        byte[] second = result[1].ToByteArray();

                        Array.Copy(first, raw_data, first.Length);
                        Array.Copy(second, 0, raw_data, dataSize(fip.Type), second.Length);
                    }
                }

                // Catch the tags which needs to be specially handled and
                // pretty print them. If tag not handled in prettyPrintField()
                // fall down and print it as any other tag.
                if (prettyPrintField(fd, tag, value_count, raw_data))
                    continue;
                else
                    printField(fd, fip, value_count, raw_data);
            }

            m_tagmethods.PrintDir(this, fd, flags);

            if ((flags & TiffPrintFlags.STRIPS) != 0 && fieldSet(FieldBit.StripOffsets))
            {
                fprintf(fd, "  {0} {1}:\n", m_dir.td_nstrips, IsTiled() ? "Tiles" : "Strips");
                for (int s = 0; s < m_dir.td_nstrips; s++)
                    fprintf(fd, "    {0,3}: [{0,8}, {0,8}]\n", s, m_dir.td_stripoffset[s], m_dir.td_stripbytecount[s]);
            }
        }

        /// <summary>
        /// Reads the scanline.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="row">The row.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        public bool ReadScanline(byte[] buf, int row)
        {
            return ReadScanline(buf, row, 0);
        }

        /// <summary>
        /// Reads the scanline.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="row">The row.</param>
        /// <param name="sample">The sample.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        public bool ReadScanline(byte[] buf, int row, short sample)
        {
            if (!checkRead(0))
                return false;

            bool e = seek(row, sample);
            if (e)
            {
                // Decompress desired row into user buffer.
                e = m_currentCodec.DecodeRow(buf, m_scanlinesize, sample);

                // we are now poised at the beginning of the next row
                m_row = row + 1;

                if (e)
                    postDecode(buf, m_scanlinesize);
            }

            return e;
        }

        /// <summary>
        /// Writes the scanline.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="row">The row.</param>
        /// <returns><c>true</c> if written successfully; otherwise, <c>false</c></returns>
        public bool WriteScanline(byte[] buf, int row)
        {
            return WriteScanline(buf, row, 0);
        }

        /// <summary>
        /// Writes the scanline.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="row">The row.</param>
        /// <param name="sample">The sample.</param>
        /// <returns><c>true</c> if written successfully; otherwise, <c>false</c></returns>
        public bool WriteScanline(byte[] buf, int row, short sample)
        {
            const string module = "WriteScanline";

            if (!writeCheckStrips(module))
                return false;

            // Handle delayed allocation of data buffer. This permits it to be
            // sized more intelligently (using directory information).
            bufferCheck();

            // Extend image length if needed (but only for PlanarConfig.CONTIG).
            bool imagegrew = false;
            if (row >= m_dir.td_imagelength)
            {
                // extend image
                if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                {
                    ErrorExt(this, m_clientdata, m_name, "Can not change \"ImageLength\" when using separate planes");
                    return false;
                }

                m_dir.td_imagelength = row + 1;
                imagegrew = true;
            }

            // Calculate strip and check for crossings.
            int strip;
            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
            {
                if (sample >= m_dir.td_samplesperpixel)
                {
                    ErrorExt(this, m_clientdata, m_name,
                        "{0}: Sample out of range, max {1}", sample, m_dir.td_samplesperpixel);
                    return false;
                }

                if (m_dir.td_rowsperstrip != -1)
                    strip = sample * m_dir.td_stripsperimage + row / m_dir.td_rowsperstrip;
                else
                    strip = 0;
            }
            else
            {
                if (m_dir.td_rowsperstrip != -1)
                    strip = row / m_dir.td_rowsperstrip;
                else
                    strip = 0;
            }

            // Check strip array to make sure there's space. We don't support
            // dynamically growing files that have data organized in separate
            // bitplanes because it's too painful.  In that case we require that
            // the imagelength be set properly before the first write (so that
            // the strips array will be fully allocated above).
            if (strip >= m_dir.td_nstrips && !growStrips(1))
                return false;

            if (strip != m_curstrip)
            {
                // Changing strips - flush any data present.
                if (!FlushData())
                    return false;

                m_curstrip = (int)strip;

                // Watch out for a growing image. The value of strips/image 
                // will initially be 1 (since it can't be deduced until the
                // imagelength is known).
                if (strip >= m_dir.td_stripsperimage && imagegrew)
                    m_dir.td_stripsperimage = howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip);

                m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
                if ((m_flags & TiffFlags.CODERSETUP) != TiffFlags.CODERSETUP)
                {
                    if (!m_currentCodec.SetupEncode())
                        return false;

                    m_flags |= TiffFlags.CODERSETUP;
                }

                m_rawcc = 0;
                m_rawcp = 0;

                if (m_dir.td_stripbytecount[strip] > 0)
                {
                    // if we are writing over existing tiles, zero length
                    m_dir.td_stripbytecount[strip] = 0;

                    // this forces appendToStrip() to do a seek
                    m_curoff = 0;
                }

                if (!m_currentCodec.PreEncode(sample))
                    return false;

                m_flags |= TiffFlags.POSTENCODE;
            }

            // Ensure the write is either sequential or at the beginning of a
            // strip (or that we can randomly access the data - i.e. no encoding).
            if (row != m_row)
            {
                if (row < m_row)
                {
                    // Moving backwards within the same strip:
                    // backup to the start and then decode forward (below).
                    m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
                    m_rawcp = 0;
                }

                // Seek forward to the desired row.
                if (!m_currentCodec.Seek(row - m_row))
                    return false;

                m_row = row;
            }

            // swab if needed - note that source buffer will be altered
            postDecode(buf, m_scanlinesize);

            bool status = m_currentCodec.EncodeRow(buf, m_scanlinesize, sample);

            // we are now poised at the beginning of the next row
            m_row = row + 1;
            return status;
        }

        /// <summary>
        /// Reads the specified image into an ABGR-format raster.
        /// </summary>
        /// <param name="rwidth">The raster width.</param>
        /// <param name="rheight">The raster height.</param>
        /// <param name="raster">The raster.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        /// <remarks>Uses bottom left origin for raster by default.</remarks>
        public bool ReadRGBAImage(int rwidth, int rheight, int[] raster)
        {
            return ReadRGBAImage(rwidth, rheight, raster, false);
        }

        /// <summary>
        /// Reads the RGBA image.
        /// </summary>
        /// <param name="rwidth">The raster width.</param>
        /// <param name="rheight">The raster height.</param>
        /// <param name="raster">The raster.</param>
        /// <param name="stop">Stop.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        public bool ReadRGBAImage(int rwidth, int rheight, int[] raster, bool stop)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, Orientation.BOTLEFT, stop);
        }

        /*
        * Read the specified image into an ABGR-format raster taking in account
        * specified orientation.
        */
        /// <summary>
        /// Reads the RGBA image oriented.
        /// </summary>
        /// <param name="rwidth">The raster width.</param>
        /// <param name="rheight">The raster height.</param>
        /// <param name="raster">The raster.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        public bool ReadRGBAImageOriented(int rwidth, int rheight, int[] raster)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, Orientation.BOTLEFT, false);
        }

        /// <summary>
        /// Reads the RGBA image oriented.
        /// </summary>
        /// <param name="rwidth">The raster width.</param>
        /// <param name="rheight">The raster height.</param>
        /// <param name="raster">The raster.</param>
        /// <param name="orientation">The orientation.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        public bool ReadRGBAImageOriented(int rwidth, int rheight, int[] raster, Orientation orientation)
        {
            return ReadRGBAImageOriented(rwidth, rheight, raster, orientation, false);
        }

        /// <summary>
        /// Reads the RGBA image oriented.
        /// </summary>
        /// <param name="rwidth">The raster width.</param>
        /// <param name="rheight">The raster height.</param>
        /// <param name="raster">The raster.</param>
        /// <param name="orientation">The orientation.</param>
        /// <param name="stop">if set to <c>true</c> [stop].</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        public bool ReadRGBAImageOriented(int rwidth, int rheight, int[] raster, Orientation orientation, bool stop)
        {
            bool ok = false;
            string emsg;
            if (RGBAImageOK(out emsg))
            {
                TiffRGBAImage img = TiffRGBAImage.Create(this, stop, out emsg);
                if (img != null)
                {
                    img.ReqOrientation = orientation;
                    // XXX verify rwidth and rheight against width and height
                    ok = img.Get(raster, (rheight - img.Height) * rwidth, rwidth, img.Height);
                }
            }
            else
            {
                ErrorExt(this, m_clientdata, FileName(), "{0}", emsg);
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Reads a whole strip of a strip-based image, decodes it and converts it to RGBA format.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="raster">The RGBA raster.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        /// <remarks>
        /// <para>
        /// ReadRGBAStrip reads a single strip of a strip-based image into memory, storing
        /// the result in the user supplied RGBA <paramref name="raster"/>. If specified strip is
        /// the last strip, then it will only contain the portion of the strip that is actually
        /// within the image space. The raster is assumed to be an array of width times
        /// rowsperstrip 32-bit entries, where width is the width of the image
        /// (<see cref="TiffTag.IMAGEWIDTH"/>) and rowsperstrip is the maximum lines in a strip
        /// (<see cref="TiffTag.ROWSPERSTRIP"/>).
        /// </para>
        /// <para>
        /// The <paramref name="row"/> value should be the row of the first row in the strip
        /// (strip * rowsperstrip, zero based).
        /// </para>
        /// <para>
        /// Note that the raster is assume to be organized such that the pixel at location (x, y)
        /// is raster[y * width + x]; with the raster origin in the lower-left hand corner of the
        /// strip. That is bottom to top organization. When reading a partial last strip in the
        /// file the last line of the image will begin at the beginning of the buffer.
        /// </para>
        /// <para>
        /// Raster pixels are 8-bit packed red, green, blue, alpha samples. The
        /// <see cref="Tiff.GetR"/>, <see cref="Tiff.GetG"/>, <see cref="Tiff.GetB"/>, and
        /// <see cref="Tiff.GetA"/> should be used to access individual samples. Images without
        /// Associated Alpha matting information have a constant Alpha of 1.0 (255).
        /// </para>
        /// <para>
        /// See <see cref="O:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage"/> for more details
        /// on how various image types are converted to RGBA values.
        /// </para>
        /// <para>
        /// Samples must be either 1, 2, 4, 8, or 16 bits. Colorimetric samples/pixel must be
        /// either 1, 3, or 4 (i.e. SamplesPerPixel minus ExtraSamples).
        /// </para>
        /// <para>
        /// Palette image colormaps that appear to be incorrectly written as 8-bit values are
        /// automatically scaled to 16-bits.
        /// </para>
        /// <para>
        /// ReadRGBAStrip's main advantage over the similar
        /// <see cref="O:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage"/> function is that for
        /// large images a single buffer capable of holding the whole image doesn't need to be
        /// allocated, only enough for one strip. The <see cref="ReadRGBATile"/> function does a
        /// similar operation for tiled images.
        /// </para>
        /// <para>
        /// All error messages are directed to the current error handler.
        /// </para>
        /// </remarks>
        public bool ReadRGBAStrip(int row, int[] raster)
        {
            if (IsTiled())
            {
                ErrorExt(this, m_clientdata, FileName(), "Can't use ReadRGBAStrip() with tiled file.");
                return false;
            }

            FieldValue[] result = GetFieldDefaulted(TiffTag.ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();
            if ((row % rowsperstrip) != 0)
            {
                ErrorExt(this, m_clientdata, FileName(), "Row passed to ReadRGBAStrip() must be first in a strip.");
                return false;
            }

            bool ok;
            string emsg;
            if (RGBAImageOK(out emsg))
            {
                TiffRGBAImage img = TiffRGBAImage.Create(this, false, out emsg);
                if (img != null)
                {
                    img.row_offset = row;
                    img.col_offset = 0;

                    int rows_to_read = rowsperstrip;
                    if (row + rowsperstrip > img.Height)
                        rows_to_read = img.Height - row;

                    ok = img.Get(raster, 0, img.Width, rows_to_read);
                    return ok;
                }

                return true;
            }

            ErrorExt(this, m_clientdata, FileName(), "{0}", emsg);
            return false;
        }

        /// <summary>
        /// Reads a whole tile off data from the file, and convert to RGBA form.
        /// </summary>
        /// <param name="col">The column.</param>
        /// <param name="row">The row.</param>
        /// <param name="raster">The raster.</param>
        /// <returns><c>true</c> if read successfully; otherwise, <c>false</c></returns>
        /// <remarks>The returned RGBA data is organized from bottom to top of tile,
        /// and may include zeroed areas if the tile extends off the image.</remarks>
        public bool ReadRGBATile(int col, int row, int[] raster)
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

            FieldValue[] result = GetFieldDefaulted(TiffTag.TILEWIDTH);
            int tile_xsize = result[0].ToInt();
            result = GetFieldDefaulted(TiffTag.TILELENGTH);
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
                ErrorExt(this, m_clientdata, FileName(), "{0}", emsg);
                return false;
            }

            /*
             * The TIFFRGBAImageGet() function doesn't allow us to get off the
             * edge of the image, even to fill an otherwise valid tile.  So we
             * figure out how much we can read, and fix up the tile buffer to
             * a full tile configuration afterwards.
             */
            int read_ysize;
            if (row + tile_ysize > img.Height)
                read_ysize = img.Height - row;
            else
                read_ysize = tile_ysize;

            int read_xsize;
            if (col + tile_xsize > img.Width)
                read_xsize = img.Width - col;
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

        /// <summary>
        /// Check the image to see if 
        /// <see cref="M:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage(System.Int32,System.Int32,System.Int32[])">ReadRGBAImage</see>
        /// can deal with it.
        /// </summary>
        /// <param name="emsg">The error message.</param>
        /// <returns><c>true</c> if the image can
        /// be handled by <see cref="M:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage(System.Int32,System.Int32,System.Int32[])">ReadRGBAImage</see>. 
        /// If <c>false</c> is returned, emsg contains the reason
        /// why it is being rejected.</returns>
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
                    emsg = string.Format(CultureInfo.InvariantCulture, "Sorry, can not handle images with {0}-bit samples", m_dir.td_bitspersample);
                    return false;
            }

            int colorchannels = m_dir.td_samplesperpixel - m_dir.td_extrasamples;
            Photometric photometric = Photometric.RGB;
            FieldValue[] result = GetField(TiffTag.PHOTOMETRIC);
            if (result == null)
            {
                switch (colorchannels)
                {
                    case 1:
                        photometric = Photometric.MINISBLACK;
                        break;
                    case 3:
                        photometric = Photometric.RGB;
                        break;
                    default:
                        emsg = string.Format(CultureInfo.InvariantCulture, "Missing needed {0} tag", TiffRGBAImage.photoTag);
                        return false;
                }
            }
            else
            {
                // San Chen <bigsan.chen@gmail.com>
                photometric = (Photometric)result[0].Value;
            }

            switch (photometric)
            {
                case Photometric.MINISWHITE:
                case Photometric.MINISBLACK:
                case Photometric.PALETTE:
                    if (m_dir.td_planarconfig == PlanarConfig.CONTIG && m_dir.td_samplesperpixel != 1 && m_dir.td_bitspersample < 8)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
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
                case Photometric.YCBCR:
                    /*
                    * TODO: if at all meaningful and useful, make more complete
                    * support check here, or better still, refactor to let supporting
                    * code decide whether there is support and what meaningfull
                    * error to return
                    */
                    break;
                case Photometric.RGB:
                    if (colorchannels < 3)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle RGB image with {0}={1}",
                            "Color channels", colorchannels);

                        return false;
                    }
                    break;
                case Photometric.SEPARATED:
                    result = GetFieldDefaulted(TiffTag.INKSET);
                    InkSet inkset = (InkSet)result[0].ToByte();
                    if (inkset != InkSet.CMYK)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle separated image with {0}={1}", "InkSet", inkset);
                        return false;
                    }
                    if (m_dir.td_samplesperpixel < 4)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle separated image with {0}={1}",
                            "Samples/pixel", m_dir.td_samplesperpixel);
                        return false;
                    }
                    break;
                case Photometric.LOGL:
                    if (m_dir.td_compression != Compression.SGILOG)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, LogL data must have {0}={1}",
                            "Compression", Compression.SGILOG);
                        return false;
                    }
                    break;
                case Photometric.LOGLUV:
                    if (m_dir.td_compression != Compression.SGILOG &&
                        m_dir.td_compression != Compression.SGILOG24)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, LogLuv data must have {0}={1} or {2}",
                            "Compression", Compression.SGILOG, Compression.SGILOG24);
                        return false;
                    }

                    if (m_dir.td_planarconfig != PlanarConfig.CONTIG)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle LogLuv images with {0}={1}",
                            "Planarconfiguration", m_dir.td_planarconfig);
                        return false;
                    }
                    break;
                case Photometric.CIELAB:
                    break;
                default:
                    emsg = string.Format(CultureInfo.InvariantCulture,
                        "Sorry, can not handle image with {0}={1}",
                        TiffRGBAImage.photoTag, photometric);
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Open file's name.
        /// </summary>
        /// <returns>File name.</returns>
        public string FileName()
        {
            return m_name;
        }

        /// <summary>
        /// Sets the name of the file.
        /// </summary>
        /// <param name="name">The file name.</param>
        /// <returns>Previous file name.</returns>
        public string SetFileName(string name)
        {
            string old_name = m_name;
            m_name = name;
            return old_name;
        }

        /// <summary>
        /// Forces the error.
        /// </summary>
        /// <param name="tif">The tif. Can be <c>null</c></param>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void Error(Tiff tif, string module, string fmt, params object[] ap)
        {
            m_errorHandler.ErrorHandler(tif, module, fmt, ap);
            m_errorHandler.ErrorHandlerExt(tif, null, module, fmt, ap);
        }

        /// <summary>
        /// Forces the error.
        /// </summary>
        /// <param name="tif">The tif. Can be <c>null</c></param>
        /// <param name="fd">The fd.</param>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void ErrorExt(Tiff tif, object fd, string module, string fmt, params object[] ap)
        {
            m_errorHandler.ErrorHandler(tif, module, fmt, ap);
            m_errorHandler.ErrorHandlerExt(tif, fd, module, fmt, ap);
        }

        /// <summary>
        /// Forces the warning.
        /// </summary>
        /// <param name="tif">The tif. Can be <c>null</c></param>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void Warning(Tiff tif, string module, string fmt, params object[] ap)
        {
            m_errorHandler.WarningHandler(tif, module, fmt, ap);
            m_errorHandler.WarningHandlerExt(tif, null, module, fmt, ap);
        }

        /// <summary>
        /// Forces the warning.
        /// </summary>
        /// <param name="tif">The tif. Can be <c>null</c></param>
        /// <param name="fd">The fd.</param>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void WarningExt(Tiff tif, object fd, string module, string fmt, params object[] ap)
        {
            m_errorHandler.WarningHandler(tif, module, fmt, ap);
            m_errorHandler.WarningHandlerExt(tif, fd, module, fmt, ap);
        }

        /// <summary>
        /// Forces the error.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void Error(string module, string fmt, params object[] ap)
        {
            Error(null, module, fmt, ap);
        }

        /// <summary>
        /// Forces the error.
        /// </summary>
        /// <param name="fd">The fd.</param>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void ErrorExt(object fd, string module, string fmt, params object[] ap)
        {
            ErrorExt(null, fd, module, fmt, ap);
        }

        /// <summary>
        /// Forces the warning.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void Warning(string module, string fmt, params object[] ap)
        {
            Warning(null, module, fmt, ap);
        }

        /// <summary>
        /// Forces the warning.
        /// </summary>
        /// <param name="fd">The fd.</param>
        /// <param name="module">The module.</param>
        /// <param name="fmt">The message format.</param>
        /// <param name="ap">The optional formatted message arguments.</param>
        public static void WarningExt(object fd, string module, string fmt, params object[] ap)
        {
            WarningExt(null, fd, module, fmt, ap);
        }

        /// <summary>
        /// Sets the error handler.
        /// </summary>
        /// <param name="errorHandler">The error handler.</param>
        /// <returns>Previous error handler.</returns>
        public static TiffErrorHandler SetErrorHandler(TiffErrorHandler errorHandler)
        {
            TiffErrorHandler prev = m_errorHandler;
            m_errorHandler = errorHandler;
            return prev;
        }

        /// <summary>
        /// Sets the tag extender.
        /// </summary>
        /// <param name="proc">The tag extender.</param>
        /// <returns>Previous tag extender.</returns>
        public static TiffExtendProc SetTagExtender(TiffExtendProc proc)
        {
            TiffExtendProc prev = m_extender;
            m_extender = proc;
            return prev;
        }

        /*
        * Compute which tile an (x,y,z,s) value is in.
        */
        /// <summary>
        /// Computes which tile an (x,y,z,s) value is in.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="s">The s.</param>
        /// <returns>Which tile an (x,y,z,s) value is in.</returns>
        public int ComputeTile(int x, int y, int z, short s)
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

                if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                    tile = (xpt * ypt * zpt) * s + (xpt * ypt) * (z / dz) + xpt * (y / dy) + x / dx;
                else
                    tile = (xpt * ypt) * (z / dz) + xpt * (y / dy) + x / dx;
            }

            return tile;
        }

        /// <summary>
        /// Checks an (x,y,z,s) coordinate against the image bounds.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if values are correct.</returns>
        public bool CheckTile(int x, int y, int z, short s)
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

            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE && s >= m_dir.td_samplesperpixel)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Sample out of range, max {1}", s, m_dir.td_samplesperpixel - 1);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Computes how many tiles are in an image.
        /// </summary>
        /// <returns>The number of tiles.</returns>
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
            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                ntiles = multiply(ntiles, m_dir.td_samplesperpixel, "NumberOfTiles");

            return ntiles;
        }

        /*
        * Tile-oriented Read Support
        * Contributed by Nancy Cam (Silicon Graphics).
        */

        /// <summary>
        /// Reads and decompresses a tile of data.
        /// </summary>
        /// <param name="buf">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="s">The s.</param>
        /// <returns>The tile size.</returns>
        /// <remarks>The tile is selected by the (x,y,z,s) coordinates.</remarks>
        public int ReadTile(byte[] buf, int offset, int x, int y, int z, short s)
        {
            if (!checkRead(1) || !CheckTile(x, y, z, s))
                return -1;

            return ReadEncodedTile(ComputeTile(x, y, z, s), buf, offset, -1);
        }

        /// <summary>
        /// Reads a tile of data and decompress the specified
        /// amount into the user-supplied buffer.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="buf">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        /// <returns>The tile size.</returns>
        public int ReadEncodedTile(int tile, byte[] buf, int offset, int size)
        {
            if (!checkRead(1))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Tile out of range, max {1}", tile, m_dir.td_nstrips);
                return -1;
            }

            if (size == -1)
                size = m_tilesize;
            else if (size > m_tilesize)
                size = m_tilesize;

            byte[] tempBuf = new byte[size];
            Array.Copy(buf, offset, tempBuf, 0, size);

            if (fillTile(tile) && m_currentCodec.DecodeTile(tempBuf, size, (short)(tile / m_dir.td_stripsperimage)))
            {
                postDecode(tempBuf, size);
                Array.Copy(tempBuf, 0, buf, offset, size);
                return size;
            }

            return -1;
        }

        /// <summary>
        /// Reads a tile of data from the file.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="buf">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        /// <returns>The tile size.</returns>
        public int ReadRawTile(int tile, byte[] buf, int offset, int size)
        {
            const string module = "ReadRawTile";

            if (!checkRead(1))
                return -1;

            if (tile >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Tile out of range, max {1}", tile, m_dir.td_nstrips);
                return -1;
            }

            if ((m_flags & TiffFlags.NOREADRAW) == TiffFlags.NOREADRAW)
            {
                ErrorExt(m_clientdata, m_name, "Compression scheme does not support access to raw uncompressed data");
                return -1;
            }

            uint bytecount = m_dir.td_stripbytecount[tile];
            if (size != -1 && (uint)size < bytecount)
                bytecount = (uint)size;

            return readRawTile1(tile, buf, offset, (int)bytecount, module);
        }

        /// <summary>
        /// Writes and compress a tile of data.
        /// </summary>
        /// <param name="buf">The buf.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <param name="s">The s.</param>
        /// <returns>The tile size.</returns>
        /// <remarks>The tile is selected by the (x,y,z,s) coordinates.</remarks>
        public int WriteTile(byte[] buf, int x, int y, int z, short s)
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

        /// <summary>
        /// Computes which strip a (row,sample) value is in.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="sample">The sample.</param>
        /// <returns>The number of strip.</returns>
        public int ComputeStrip(int row, short sample)
        {
            int strip = 0;
            if (m_dir.td_rowsperstrip != -1)
                strip = row / m_dir.td_rowsperstrip;

            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
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

        /// <summary>
        /// Computes how many strips are in an image.
        /// </summary>
        /// <returns>The number of strips.</returns>
        public int NumberOfStrips()
        {
            int nstrips = (m_dir.td_rowsperstrip == -1 ? 1 : howMany(m_dir.td_imagelength, m_dir.td_rowsperstrip));
            if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                nstrips = multiply(nstrips, m_dir.td_samplesperpixel, "NumberOfStrips");

            return nstrips;
        }

        /// <summary>
        /// Read a strip of data and decompress the specified amount into the user-supplied buffer.
        /// </summary>
        /// <param name="strip">The strip.</param>
        /// <param name="buf">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        /// <returns>The strip size.</returns>
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
            if (size == -1)
                size = stripsize;
            else if (size > stripsize)
                size = stripsize;

            byte[] tempBuf = new byte[size];
            Array.Copy(buf, offset, tempBuf, 0, size);

            if (fillStrip(strip) && m_currentCodec.DecodeStrip(tempBuf, size, (short)(strip / m_dir.td_stripsperimage)))
            {
                postDecode(tempBuf, size);
                Array.Copy(tempBuf, 0, buf, offset, size);
                return size;
            }

            return -1;
        }

        /// <summary>
        /// Read a strip of data from the file.
        /// </summary>
        /// <param name="strip">The strip.</param>
        /// <param name="buf">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        /// <returns>The strip size.</returns>
        public int ReadRawStrip(int strip, byte[] buf, int offset, int size)
        {
            const string module = "ReadRawStrip";

            if (!checkRead(0))
                return -1;

            if (strip >= m_dir.td_nstrips)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Strip out of range, max {1}", strip, m_dir.td_nstrips);
                return -1;
            }

            if ((m_flags & TiffFlags.NOREADRAW) == TiffFlags.NOREADRAW)
            {
                ErrorExt(this, m_clientdata, m_name, "Compression scheme does not support access to raw uncompressed data");
                return -1;
            }

            uint bytecount = m_dir.td_stripbytecount[strip];
            if (bytecount <= 0)
            {
                ErrorExt(this, m_clientdata, m_name, "{0}: Invalid strip byte count, strip {1}", bytecount, strip);
                return -1;
            }

            if (size != -1 && (uint)size < bytecount)
                bytecount = (uint)size;

            return readRawStrip1(strip, buf, offset, (int)bytecount, module);
        }

        /// <summary>
        /// Encode the supplied data and write it to the specified strip.
        /// </summary>
        /// <param name="strip">The strip.</param>
        /// <param name="data">The data.</param>
        /// <param name="cc">The size.</param>
        /// <returns>The strip size.</returns>
        /// <remarks>Image length must be setup before writing.</remarks>
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
                if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
                {
                    ErrorExt(this, m_clientdata, m_name, "Can not grow image by strips when using separate planes");
                    return -1;
                }

                if (!growStrips(1))
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
            if ((m_flags & TiffFlags.CODERSETUP) != TiffFlags.CODERSETUP)
            {
                if (!m_currentCodec.SetupEncode())
                    return -1;

                m_flags |= TiffFlags.CODERSETUP;
            }

            m_rawcc = 0;
            m_rawcp = 0;

            if (m_dir.td_stripbytecount[strip] > 0)
            {
                /* this forces appendToStrip() to do a seek */
                m_curoff = 0;
            }

            m_flags &= ~TiffFlags.POSTENCODE;
            short sample = (short)(strip / m_dir.td_stripsperimage);
            if (!m_currentCodec.PreEncode(sample))
                return -1;

            /* swab if needed - note that source buffer will be altered */
            postDecode(data, cc);

            if (!m_currentCodec.EncodeStrip(data, cc, sample))
                return 0;

            if (!m_currentCodec.PostEncode())
                return -1;

            if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TiffFlags.NOBITREV) != TiffFlags.NOBITREV)
                ReverseBits(m_rawdata, m_rawcc);

            if (m_rawcc > 0 && !appendToStrip(strip, m_rawdata, m_rawcc))
                return -1;

            m_rawcc = 0;
            m_rawcp = 0;
            return cc;
        }

        /// <summary>
        /// Writes the supplied data to the specified strip.
        /// </summary>
        /// <param name="strip">The strip.</param>
        /// <param name="data">The data.</param>
        /// <param name="cc">The size.</param>
        /// <returns>The strip size.</returns>
        /// <remarks>Image length must be setup before writing.</remarks>
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
                if (m_dir.td_planarconfig == PlanarConfig.SEPARATE)
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

                if (!growStrips(1))
                    return -1;
            }

            m_curstrip = strip;
            m_row = (strip % m_dir.td_stripsperimage) * m_dir.td_rowsperstrip;
            return (appendToStrip(strip, data, cc) ? cc : -1);
        }

        /// <summary>
        /// Encodes the supplied data and write it to the specified tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="data">The data.</param>
        /// <param name="cc">The size.</param>
        /// <returns>The tile size.</returns>
        /// <remarks>
        ///     <para>There must be space for the data. The function clamps 
        ///     individual writes to a tile to the tile size, but does not 
        ///     (and can not) check that multiple writes to the same tile 
        ///     do not write more than tile size data.
        ///     </para>
        ///     <para>Image length must be setup before writing; this 
        ///     interface does not support automatically growing the 
        ///     image on each write (as 
        ///     <see cref="M:BitMiracle.LibTiff.Classic.Tiff.WriteScanline(System.Byte[],System.Int32,System.Int16)">WriteScanline</see>
        ///     does).
        ///     </para>
        /// </remarks>
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

            if ((m_flags & TiffFlags.CODERSETUP) != TiffFlags.CODERSETUP)
            {
                if (!m_currentCodec.SetupEncode())
                    return -1;

                m_flags |= TiffFlags.CODERSETUP;
            }

            m_flags &= ~TiffFlags.POSTENCODE;
            short sample = (short)(tile / m_dir.td_stripsperimage);
            if (!m_currentCodec.PreEncode(sample))
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

            if (!m_currentCodec.EncodeTile(data, cc, sample))
                return 0;

            if (!m_currentCodec.PostEncode())
                return -1;

            if (!isFillOrder(m_dir.td_fillorder) && (m_flags & TiffFlags.NOBITREV) != TiffFlags.NOBITREV)
                ReverseBits(m_rawdata, m_rawcc);

            if (m_rawcc > 0 && !appendToStrip(tile, m_rawdata, m_rawcc))
                return -1;

            m_rawcc = 0;
            m_rawcp = 0;
            return cc;
        }

        /// <summary>
        /// Writes the supplied data to the specified strip.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="data">The data.</param>
        /// <param name="cc">The cc.</param>
        /// <returns>The tile size.</returns>
        /// <remarks>
        ///     <para>There must be space for the data; we don't check if strips overlap!</para>
        ///     <para>Image length must be setup before writing; this interface does not 
        ///     support automatically growing the image on each write (as 
        ///     <see cref="M:BitMiracle.LibTiff.Classic.Tiff.WriteScanline(System.Byte[],System.Int32,System.Int16)">WriteScanline</see>
        ///     does).
        ///     </para>
        /// </remarks>
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

            return (appendToStrip(tile, data, cc) ? cc : -1);
        }

        /// <summary>
        /// Sets the current write offset.
        /// </summary>
        /// <param name="off">The write offset.</param>
        /// <remarks>This should only be used to set the offset to a known
        /// previous location (very carefully), or to 0 so that the next write
        /// gets appended to the end of the file.</remarks>
        public void SetWriteOffset(long off)
        {
            m_curoff = (uint)off;
        }

        /// <summary>
        /// Gets the size of a TiffType in bytes.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The size of TiffType in bytes.</returns>
        public static int DataWidth(TiffType type)
        {
            switch (type)
            {
                case TiffType.NOTYPE:
                case TiffType.BYTE:
                case TiffType.ASCII:
                case TiffType.SBYTE:
                case TiffType.UNDEFINED:
                    return 1;

                case TiffType.SHORT:
                case TiffType.SSHORT:
                    return 2;

                case TiffType.LONG:
                case TiffType.SLONG:
                case TiffType.FLOAT:
                case TiffType.IFD:
                    return 4;

                case TiffType.RATIONAL:
                case TiffType.SRATIONAL:
                case TiffType.DOUBLE:
                    return 8;

                default:
                    // will return 0 for unknown types
                    return 0;
            }
        }

        /*
        * TIFF Library Bit & Byte Swapping Support.
        *
        * XXX We assume short = 16-bits and long = 32-bits XXX
        */
        /// <summary>
        /// Swaps the short.
        /// </summary>
        /// <param name="wp">The short number.</param>
        public static void SwabShort(ref short wp)
        {
            byte[] cp = new byte[2];
            cp[0] = (byte)wp;
            cp[1] = (byte)(wp >> 8);

            byte t = cp[1];
            cp[1] = cp[0];
            cp[0] = t;

            wp = (short)(cp[0] & 0xFF);
            wp += (short)((cp[1] & 0xFF) << 8);
        }

        /// <summary>
        /// Swaps the integer.
        /// </summary>
        /// <param name="lp">The integer number.</param>
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

        /// <summary>
        /// Swaps the double.
        /// </summary>
        /// <param name="dp">The double number.</param>
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

        /// <summary>
        /// Swaps the array of short.
        /// </summary>
        /// <param name="wp">The array of short numbers.</param>
        /// <param name="n">The array length.</param>
        public static void SwabArrayOfShort(short[] wp, int n)
        {
            byte[] cp = new byte[2];
            for (int i = 0; i < n; i++)
            {
                cp[0] = (byte)wp[i];
                cp[1] = (byte)(wp[i] >> 8);

                byte t = cp[1];
                cp[1] = cp[0];
                cp[0] = t;

                wp[i] = (short)(cp[0] & 0xFF);
                wp[i] += (short)((cp[1] & 0xFF) << 8);
            }
        }

        /// <summary>
        /// Swaps the array of triples.
        /// </summary>
        /// <param name="tp">The array of triples.</param>
        /// <param name="n">The array length.</param>
        public static void SwabArrayOfTriples(byte[] tp, int n)
        {
            // XXX unroll loop some
            int tpPos = 0;
            while (n-- > 0)
            {
                byte t = tp[tpPos + 2];
                tp[tpPos + 2] = tp[tpPos];
                tp[tpPos] = t;
                tpPos += 3;
            }
        }

        /// <summary>
        /// Swaps the array of integers.
        /// </summary>
        /// <param name="lp">The array of integers.</param>
        /// <param name="n">The array length.</param>
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

        /// <summary>
        /// Swabs the array of doubles.
        /// </summary>
        /// <param name="dp">The array of doubles.</param>
        /// <param name="n">The array length.</param>
        public static void SwabArrayOfDouble(double[] dp, int n)
        {
            byte[] bytes = new byte[n * sizeof(double)];
            for (int i = 0; i < n; i++)
                Array.Copy(BitConverter.GetBytes(dp[i]), 0, bytes, i * sizeof(double), sizeof(double));

            int[] lp = ByteArrayToInts(bytes, 0, n * sizeof(double));
            SwabArrayOfLong(lp, n + n);

            int lpPos = 0;
            while (n-- > 0)
            {
                int t = lp[lpPos];
                lp[lpPos] = lp[lpPos + 1];
                lp[lpPos + 1] = t;
                lpPos += 2;
            }

            IntsToByteArray(lp, 0, n + n, bytes, 0);
            for (int i = 0; i < n; i++)
                dp[i] = BitConverter.ToDouble(bytes, i * sizeof(double));
        }

        /// <summary>
        /// Reverses the bits.
        /// </summary>
        /// <param name="cp">The byte array.</param>
        /// <param name="n">The array length.</param>
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

        /// <summary>
        /// Gets the bit reversal table.
        /// </summary>
        /// <param name="reversed">if set to <c>true</c> reversed table will be retrieved.</param>
        /// <returns>The reversal table.</returns>
        public static byte[] GetBitRevTable(bool reversed)
        {
            return (reversed ? TIFFBitRevTable : TIFFNoBitRevTable);
        }

        /// <summary>
        /// Converts byte array to array of integers.
        /// </summary>
        /// <param name="b">The byte array.</param>
        /// <param name="byteStartOffset">The start offset.</param>
        /// <param name="byteCount">The byte count.</param>
        /// <returns>The array of integers.</returns>
        public static int[] ByteArrayToInts(byte[] b, int byteStartOffset, int byteCount)
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

        /// <summary>
        /// Convert array of integers to array of bytes.
        /// </summary>
        /// <param name="integers">The array of integers.</param>
        /// <param name="intStartOffset">The start offset.</param>
        /// <param name="intCount">The number of integers.</param>
        /// <param name="bytes">The byte array.</param>
        /// <param name="byteStartOffset">The start offset in byte array.</param>
        public static void IntsToByteArray(int[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
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

        /// <summary>
        /// Convert array of bytes to array of shorts.
        /// </summary>
        /// <param name="b">The byte array.</param>
        /// <param name="byteStartOffset">The start offset.</param>
        /// <param name="byteCount">The byte count.</param>
        /// <returns>The array of short.</returns>
        public static short[] ByteArrayToShorts(byte[] b, int byteStartOffset, int byteCount)
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

        /// <summary>
        /// Converts array of shorts to array of bytes.
        /// </summary>
        /// <param name="integers">The array of integers.</param>
        /// <param name="intStartOffset">The start offset.</param>
        /// <param name="intCount">The integers count.</param>
        /// <param name="bytes">The bytes array.</param>
        /// <param name="byteStartOffset">The start offset in array of bytes.</param>
        public static void ShortsToByteArray(short[] integers, int intStartOffset, int intCount, byte[] bytes, int byteStartOffset)
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
    }
}
