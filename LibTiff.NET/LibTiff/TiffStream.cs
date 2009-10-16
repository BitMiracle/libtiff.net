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

using thandle_t = System.Object;

namespace BitMiracle.LibTiff
{
    public class TiffStream
    {
        public virtual int Read(thandle_t fd, byte[] buf, int offset, int size)
        {
            //DWORD dwSizeRead;
            //if (!ReadFile(fd, &buf[offset], size, &dwSizeRead, null))
            //    return 0;

            //return dwSizeRead;
            return 0;
        }

        public virtual int Write(thandle_t fd, byte[] buf, int size)
        {
            //DWORD dwSizeWritten;
            //if (!WriteFile(fd, buf, size, &dwSizeWritten, null))
            //    return 0;

            //return dwSizeWritten;
            return 0;
        }

        public virtual int Seek(thandle_t fd, int off, int whence)
        {
            ///* we use this as a special code, so avoid accepting it */
            //if (off == 0xFFFFFFFF)
            //    return 0xFFFFFFFF;

            //DWORD dwMoveMethod = FILE_BEGIN;
            //switch (whence)
            //{
            //    case Tiff.SEEK_SET:
            //        dwMoveMethod = FILE_BEGIN;
            //        break;
            //    case Tiff.SEEK_CUR:
            //        dwMoveMethod = FILE_CURRENT;
            //        break;
            //    case Tiff.SEEK_END:
            //        dwMoveMethod = FILE_END;
            //        break;
            //}

            //DWORD dwMoveHigh = 0;
            //return SetFilePointer(fd, (LONG)off, (PLONG) & dwMoveHigh, dwMoveMethod);
            return 0;
        }

        public virtual bool Close(thandle_t fd)
        {
            //return (CloseHandle(fd) ? true : false);
            return false;
        }

        public virtual uint Size(thandle_t fd)
        {
            //return GetFileSize(fd, null);
            return 0;
        }
    }
}
