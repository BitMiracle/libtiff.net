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

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        internal const int STRIP_SIZE_DEFAULT = 8192;

        internal const int TIFF_FILLORDER = 0x0003;  /* natural bit fill order for machine */
        internal const int TIFF_DIRTYDIRECT = 0x0008;  /* current directory must be written */
        internal const int TIFF_BUFFERSETUP = 0x0010;  /* data buffers setup */
        internal const int TIFF_CODERSETUP = 0x0020;  /* encoder/decoder setup done */
        internal const int TIFF_BEENWRITING = 0x0040;  /* written 1+ scanlines to file */
        internal const int TIFF_SWAB = 0x0080;  /* byte swap file information */
        internal const int TIFF_NOBITREV = 0x0100;  /* inhibit bit reversal logic */
        internal const int TIFF_MYBUFFER = 0x0200;  /* my raw data buffer; free on close */
        internal const int TIFF_ISTILED = 0x0400;  /* file is tile, not strip- based */
        internal const int TIFF_POSTENCODE = 0x1000;  /* need call to postencode routine */
        internal const int TIFF_INSUBIFD = 0x2000;  /* currently writing a subifd */
        internal const int TIFF_UPSAMPLED = 0x4000;  /* library is doing data up-sampling */
        internal const int TIFF_STRIPCHOP = 0x8000;  /* enable strip chopping support */
        internal const int TIFF_HEADERONLY = 0x10000; /* read header only, do not process the first directory*/
        internal const int TIFF_NOREADRAW = 0x20000; /* skip reading of raw uncompressed image data*/

        internal enum PostDecodeMethodType
        {
            pdmNone,
            pdmSwab16Bit,
            pdmSwab24Bit,
            pdmSwab32Bit,
            pdmSwab64Bit
        };

        internal string m_name; /* name of open file */
        internal int m_mode; /* open mode (O_*) */
        internal int m_flags;

        /* the first directory */
        internal int m_diroff; /* file offset of current directory */

        /* directories to prevent IFD looping */
        internal TiffDirectory m_dir; /* internal rep of current directory */
        internal int m_row; /* current scanline */
        internal int m_curstrip; /* current strip for read/write */

        /* tiling support */
        internal int m_curtile; /* current tile for read/write */
        internal int m_tilesize; /* # of bytes in a tile */

        /* compression scheme hooks */
        internal TiffCodec m_currentCodec;

        /* input/output buffering */
        internal int m_scanlinesize; /* # of bytes in a scanline */
        internal byte[] m_rawdata; /* raw data buffer */
        internal int m_rawdatasize; /* # of bytes in raw data buffer */
        internal int m_rawcp; /* current spot in raw buffer */
        internal int m_rawcc; /* bytes unread from raw buffer */

        internal object m_clientdata; /* callback parameter */

        /* post-decoding support */
        internal PostDecodeMethodType m_postDecodeMethod;  /* post decoding method type */

        /* tag support */
        internal TiffTagMethods m_tagmethods; /* tag get/set/print routines */

        private class codecList
        {
            public codecList next;
            public TiffCodec codec;
        };

        private class clientInfoLink
        {
            public clientInfoLink next;
            public object data;
            public string name;
        };

        /* the first directory */
        private int m_nextdiroff; /* file offset of following directory */
        private int[] m_dirlist; /* list of offsets to already seen directories to prevent IFD looping */
        private int m_dirlistsize; /* number of entires in offset list */
        private short m_dirnumber; /* number of already seen directories */
        private TiffHeader m_header; /* file's header block */
        private int[] m_typeshift; /* data type shift counts */
        private int[] m_typemask; /* data type masks */
        private short m_curdir; /* current directory (index) */
        private int m_curoff; /* current offset for read/write */
        private int m_dataoff; /* current offset for writing dir */

        /* SubIFD support */
        private short m_nsubifd; /* remaining subifds to write */
        private int m_subifdoff; /* offset for patching SubIFD link */

        /* tiling support */
        private int m_col; /* current column (offset by row too) */

        /* compression scheme hooks */
        private bool m_decodestatus;

        /* tag support */
        private TiffFieldInfo[] m_fieldinfo; /* sorted table of registered tags */
        private int m_nfields; /* # entries in registered tag table */
        private TiffFieldInfo m_foundfield; /* cached pointer to already found tag */

        private clientInfoLink m_clientinfo; /* extra client information. */

        private TiffCodec[] m_builtInCodecs;
        private codecList m_registeredCodecs;

        private TiffTagMethods m_defaultTagMethods;

        private static TiffErrorHandler m_errorHandler;
        private TiffErrorHandler m_defaultErrorHandler;

        private bool m_disposed = false;
        private Stream m_fileStream = null;
 
        /*
        * Client Tag extension support (from Niles Ritter).
        */
        private static TiffExtendProc m_extender;

        private TiffStream m_stream; // stream used for read|write|etc.

        private Tiff()
        {
            m_name = null;
            m_mode = 0;
            m_flags = 0;

            m_diroff = 0;
            m_nextdiroff = 0;
            m_dirlist = null;
            m_dirlistsize = 0;

            m_dirnumber = 0;
            m_dir = null;
            m_typeshift = null;
            m_typemask = null;
            m_row = 0;
            m_curdir = 0;
            m_curstrip = 0;
            m_curoff = 0;
            m_dataoff = 0;

            m_nsubifd = 0;
            m_subifdoff = 0;

            m_col = 0;
            m_curtile = 0;
            m_tilesize = 0;

            m_decodestatus = false;

            m_currentCodec = null;

            m_scanlinesize = 0;
            m_rawdata = null;
            m_rawdatasize = 0;
            m_rawcp = 0;
            m_rawcc = 0;

            m_stream = null;

            m_clientdata = 0;

            m_postDecodeMethod = PostDecodeMethodType.pdmNone;

            m_fieldinfo = null;
            m_nfields = 0;
            m_foundfield = null;
            m_tagmethods = null;
            m_clientinfo = null;

            m_builtInCodecs = null;
            m_registeredCodecs = null;
            setupBuiltInCodecs();

            m_defaultTagMethods = new TiffTagMethods();

            m_defaultErrorHandler = null;
            if (m_errorHandler == null)
            {
                // user did not setup custom handler.
                // install default
                m_defaultErrorHandler = new TiffErrorHandler();
                m_errorHandler = m_defaultErrorHandler;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.m_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (m_fileStream != null)
                        m_fileStream.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.

                // Note disposing has been done.
                m_disposed = true;
            }
        }

        internal static TiffFieldInfo[] Realloc(TiffFieldInfo[] oldBuffer, int elementCount, int newElementCount)
        {
            TiffFieldInfo[] newBuffer = new TiffFieldInfo [newElementCount];

            if (oldBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
        }

        internal static TiffTagValue[] Realloc(TiffTagValue[] oldBuffer, int elementCount, int newElementCount)
        {
            TiffTagValue[] newBuffer = new TiffTagValue[newElementCount];

            if (oldBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
        }

        internal bool setCompressionScheme(Compression scheme)
        {
            TiffCodec c = FindCodec(scheme);
            if (c == null)
            {
                /*
                 * Don't treat an unknown compression scheme as an error.
                 * This permits applications to open files with data that
                 * the library does not have builtin support for, but which
                 * may still be meaningful.
                 */
                c = m_builtInCodecs[0];
            }

            m_decodestatus = c.CanDecode();
            m_flags &= ~(TIFF_NOBITREV | TIFF_NOREADRAW);

            m_currentCodec = c;
            return c.Init();
        }

        private void cleanUp()
        {
            if (m_mode != O_RDONLY)
            {
                /*
                * Flush buffered data and directory (if dirty).
                */
                Flush();
            }

            m_currentCodec.Cleanup();
            FreeDirectory();

            m_clientinfo = null;
        }

        /* post decoding routine */  
        private void postDecode(byte[] buf, int cc)
        {
            switch (m_postDecodeMethod)
            {
                case PostDecodeMethodType.pdmSwab16Bit:
                    swab16BitData(buf, cc);
                    break;
                case PostDecodeMethodType.pdmSwab24Bit:
                    swab24BitData(buf, cc);
                    break;
                case PostDecodeMethodType.pdmSwab32Bit:
                    swab32BitData(buf, cc);
                    break;
                case PostDecodeMethodType.pdmSwab64Bit:
                    swab64BitData(buf, cc);
                    break;
            }
        }
    }
}
