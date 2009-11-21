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

using BitMiracle.LibTiff;

namespace BitMiracle.Tiff2Pdf
{
    class MyTiffStream : TiffStream
    {
        public override int Read(object fd, byte[] buf, int offset, int size)
        {
            return -1;
        }

        public override void Write(object fd, byte[] buf, int size)
        {
            Converter c = fd as Converter;
            if (c == null)
                throw new ArgumentException();

            if (!c.m_outputdisable && c.m_outputfile != null)
                c.m_outputfile.Write(buf, 0, size);
        }

        public override long Seek(object fd, long off, SeekOrigin whence)
        {
            Converter c = fd as Converter;
            if (c == null)
                throw new ArgumentException();

            if (!c.m_outputdisable && c.m_outputfile != null)
                return c.m_outputfile.Seek(off, whence);

            return off;
        }

        public override void Close(object fd)
        {
        }

        public override long Size(object fd)
        {
            return -1;
        }
    }
}
