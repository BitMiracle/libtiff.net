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
using BitMiracle.Docotic.PDFLib;

namespace BitMiracle.Tiff2Pdf
{
    class MyTiffStream : TiffStream
    {
        private bool m_disabled = false;
        private PDFStream m_stream = null;
        
        public bool Disabled
        {
            get { return m_disabled; }
            set { m_disabled = value; }
        }

        public PDFStream OutputStream
        {
            get { return m_stream; }
            set { m_stream = value; }
        }

        public override int Read(object fd, byte[] buf, int offset, int size)
        {
            return -1;
        }

        public override void Write(object fd, byte[] buf, int size)
        {
            Converter c = fd as Converter;
            if (c == null)
                throw new ArgumentException();

            if (!m_disabled && m_stream != null)
                m_stream.Write(buf, size);
        }

        public override long Seek(object fd, long off, SeekOrigin whence)
        {
            Converter c = fd as Converter;
            if (c == null)
                throw new ArgumentException();

            if (!m_disabled && m_stream != null)
                m_stream.Seek((int)off, whence);

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
