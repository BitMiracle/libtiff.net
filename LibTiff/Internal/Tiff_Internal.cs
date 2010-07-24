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
using System.IO;

using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
#if EXPOSE_LIBTIFF
    public
#endif
    partial class Tiff
    {
        internal const int STRIP_SIZE_DEFAULT = 8192;

        internal enum PostDecodeMethodType
        {
            pdmNone,
            pdmSwab16Bit,
            pdmSwab24Bit,
            pdmSwab32Bit,
            pdmSwab64Bit
        };

        /// <summary>
        /// name of open file
        /// </summary>
        internal string m_name;

        /// <summary>
        /// open mode (O_*)
        /// </summary>
        internal int m_mode;
        internal TiffFlags m_flags;

        //
        // the first directory
        //

        /// <summary>
        /// file offset of current directory
        /// </summary>
        internal uint m_diroff;

        // directories to prevent IFD looping

        /// <summary>
        /// internal rep of current directory
        /// </summary>
        internal TiffDirectory m_dir;

        /// <summary>
        /// current scanline
        /// </summary>
        internal int m_row;

        /// <summary>
        /// current strip for read/write
        /// </summary>
        internal int m_curstrip;

        // tiling support

        /// <summary>
        /// current tile for read/write
        /// </summary>
        internal int m_curtile;

        /// <summary>
        /// # of bytes in a tile
        /// </summary>
        internal int m_tilesize;

        // compression scheme hooks
        internal TiffCodec m_currentCodec;

        // input/output buffering

        /// <summary>
        /// # of bytes in a scanline
        /// </summary>
        internal int m_scanlinesize;

        /// <summary>
        /// raw data buffer
        /// </summary>
        internal byte[] m_rawdata;

        /// <summary>
        /// # of bytes in raw data buffer
        /// </summary>
        internal int m_rawdatasize;

        /// <summary>
        /// current spot in raw buffer
        /// </summary>
        internal int m_rawcp;

        /// <summary>
        /// bytes unread from raw buffer
        /// </summary>
        internal int m_rawcc;

        /// <summary>
        /// callback parameter
        /// </summary>
        internal object m_clientdata;

        // post-decoding support

        /// <summary>
        /// post decoding method type
        /// </summary>
        internal PostDecodeMethodType m_postDecodeMethod;

        // tag support

        /// <summary>
        /// tag get/set/print routines
        /// </summary>
        internal TiffTagMethods m_tagmethods;

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

        // the first directory

        /// <summary>
        /// file offset of following directory
        /// </summary>
        private uint m_nextdiroff;

        /// <summary>
        /// list of offsets to already seen directories to prevent IFD looping
        /// </summary>
        private uint[] m_dirlist;

        /// <summary>
        /// number of entires in offset list
        /// </summary>
        private int m_dirlistsize;

        /// <summary>
        /// number of already seen directories
        /// </summary>
        private short m_dirnumber;

        /// <summary>
        /// file's header block
        /// </summary>
        private TiffHeader m_header;

        /// <summary>
        /// data type shift counts
        /// </summary>
        private int[] m_typeshift;

        /// <summary>
        /// data type masks
        /// </summary>
        private uint[] m_typemask;

        /// <summary>
        /// current directory (index)
        /// </summary>
        private short m_curdir;

        /// <summary>
        /// current offset for read/write
        /// </summary>
        private uint m_curoff;

        /// <summary>
        /// current offset for writing dir
        /// </summary>
        private uint m_dataoff;

        //
        // SubIFD support
        // 

        /// <summary>
        /// remaining subifds to write
        /// </summary>
        private short m_nsubifd;

        /// <summary>
        /// offset for patching SubIFD link
        /// </summary>
        private uint m_subifdoff;

        // tiling support

        /// <summary>
        /// current column (offset by row too)
        /// </summary>
        private int m_col;

        // compression scheme hooks

        private bool m_decodestatus;

        // tag support

        /// <summary>
        /// sorted table of registered tags
        /// </summary>
        private TiffFieldInfo[] m_fieldinfo;

        /// <summary>
        /// # entries in registered tag table
        /// </summary>
        private int m_nfields;

        /// <summary>
        /// cached pointer to already found tag
        /// </summary>
        private TiffFieldInfo m_foundfield;

        /// <summary>
        /// extra client information.
        /// </summary>
        private clientInfoLink m_clientinfo;

        private TiffCodec[] m_builtInCodecs;
        private codecList m_registeredCodecs;

        private TiffTagMethods m_defaultTagMethods;

        private static TiffErrorHandler m_errorHandler;
        private TiffErrorHandler m_defaultErrorHandler;

        private bool m_disposed;
        private Stream m_fileStream;
 
        //
        // Client Tag extension support (from Niles Ritter).
        //
        private static TiffExtendProc m_extender;

        /// <summary>
        /// stream used for read|write|etc.
        /// </summary>
        private TiffStream m_stream;

        private Tiff()
        {
            m_clientdata = 0;
            m_postDecodeMethod = PostDecodeMethodType.pdmNone;

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

        private void Dispose(bool disposing)
        {
            if (!this.m_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Close();

                    if (m_fileStream != null)
                        m_fileStream.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.

                // Note disposing has been done.
                m_disposed = true;
            }
        }

        internal static void SwabUInt(ref uint lp)
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

            lp = (uint)(cp[0] & 0xFF);
            lp += (uint)((cp[1] & 0xFF) << 8);
            lp += (uint)((cp[2] & 0xFF) << 16);
            lp += (uint)(cp[3] << 24);
        }

        internal static uint[] Realloc(uint[] oldBuffer, int elementCount, int newElementCount)
        {
            uint[] newBuffer = new uint[newElementCount];
            if (oldBuffer != null)
            {
                int copyLength = Math.Min(elementCount, newElementCount);
                Array.Copy(oldBuffer, newBuffer, copyLength);
            }

            return newBuffer;
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

            m_decodestatus = c.CanDecode;
            m_flags &= ~(TiffFlags.NOBITREV | TiffFlags.NOREADRAW);

            m_currentCodec = c;
            return c.Init();
        }

        /// <summary>
        /// post decoding routine
        /// </summary>
        private void postDecode(byte[] buffer, int offset, int count)
        {
            switch (m_postDecodeMethod)
            {
                case PostDecodeMethodType.pdmSwab16Bit:
                    swab16BitData(buffer, offset, count);
                    break;
                case PostDecodeMethodType.pdmSwab24Bit:
                    swab24BitData(buffer, offset, count);
                    break;
                case PostDecodeMethodType.pdmSwab32Bit:
                    swab32BitData(buffer, offset, count);
                    break;
                case PostDecodeMethodType.pdmSwab64Bit:
                    swab64BitData(buffer, offset, count);
                    break;
            }
        }
    }
}
