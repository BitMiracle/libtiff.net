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

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// 
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffStream
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public virtual int Read(object fd, byte[] buf, int offset, int size)
        {
            Stream s = fd as Stream;
            if (s == null)
                throw new ArgumentException("Can't get stream to read from");

            return s.Read(buf, offset, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="buf"></param>
        /// <param name="size"></param>
        public virtual void Write(object fd, byte[] buf, int size)
        {
            Stream s = fd as Stream;
            if (s == null)
                throw new ArgumentException("Can't get stream to write to");
            
            s.Write(buf, 0, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="off"></param>
        /// <param name="whence"></param>
        /// <returns></returns>
        public virtual long Seek(object fd, long off, SeekOrigin whence)
        {
            /* we use this as a special code, so avoid accepting it */
            if (off == -1)
                return -1; // was 0xFFFFFFFF

            Stream s = fd as Stream;
            if (s == null)
                throw new ArgumentException("Can't get stream to seek in");

            return s.Seek(off, whence);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fd"></param>
        public virtual void Close(object fd)
        {
            Stream s = fd as Stream;
            if (s == null)
                throw new ArgumentException("Can't get stream to close");

            s.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fd"></param>
        /// <returns></returns>
        public virtual long Size(object fd)
        {
            Stream s = fd as Stream;
            if (s == null)
                throw new ArgumentException("Can't get stream to retrieve size from");

            return s.Length;
        }
    }
}
