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

namespace BitMiracle.LibTiff
{
    public class TiffStream
    {
        public virtual int Read(object fd, byte[] buf, int offset, int size)
        {
            Stream s = fd as Stream;
            int read = s.Read(buf, offset, size);
            return read;
        }

        public virtual int Write(object fd, byte[] buf, int size)
        {
            //DWORD dwSizeWritten;
            //if (!WriteFile(fd, buf, size, &dwSizeWritten, null))
            //    return 0;

            //return dwSizeWritten;
            return 0;
        }

        public virtual long Seek(object fd, long off, SeekOrigin whence)
        {
            /* we use this as a special code, so avoid accepting it */
            if (off == -1)
                return -1; // was 0xFFFFFFFF

            Stream s = fd as Stream;
            return s.Seek(off, whence);
        }

        public virtual bool Close(object fd)
        {
            //return (CloseHandle(fd) ? true : false);
            return false;
        }

        public virtual int Size(object fd)
        {
            //return GetFileSize(fd, null);
            return 0;
        }
    }
}
