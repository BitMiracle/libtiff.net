using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff;

namespace BitMiracle.Tiff2Pdf
{
    class MyStream : TiffStream
    {
        public override int Read(object fd, byte[] buf, int offset, int size)
        {
            return -1;
        }

        public override void Write(object fd, byte[] buf, int size)
        {
            T2P t2p = fd as T2P;
            if (!t2p.m_outputdisable && t2p.m_outputfile != null)
            {
                t2p.m_outputfile.Write(buf, 0, size);
                t2p.m_outputwritten += size;
            }
        }

        public override long Seek(object fd, long off, SeekOrigin whence)
        {
            T2P t2p = fd as T2P;
            if (!t2p.m_outputdisable && t2p.m_outputfile != null)
                return t2p.m_outputfile.Seek(off, whence);

            return off;
        }

        public override bool Close(object fd)
        {
            return true;
        }

        public override long Size(object fd)
        {
            return -1;
        }
    }
}
