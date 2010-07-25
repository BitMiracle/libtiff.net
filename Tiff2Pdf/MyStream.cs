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

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.Tiff2Pdf
{
    class MyStream : TiffStream
    {
        public override int Read(object fd, byte[] buf, int offset, int size)
        {
            return -1;
        }

        public override void Write(object fd, byte[] buf, int offset, int size)
        {
            T2P t2p = fd as T2P;
            if (t2p == null)
                throw new ArgumentException();

            if (!t2p.m_outputdisable && t2p.m_outputfile != null)
            {
                t2p.m_outputfile.Write(buf, offset, size);
                t2p.m_outputwritten += size;
            }
        }

        public override long Seek(object fd, long off, SeekOrigin whence)
        {
            T2P t2p = fd as T2P;
            if (t2p == null)
                throw new ArgumentException();

            if (!t2p.m_outputdisable && t2p.m_outputfile != null)
                return t2p.m_outputfile.Seek(off, whence);

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
