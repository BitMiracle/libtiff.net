using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibTiff;

namespace BitMiracle.Tiff2Pdf
{
    class Tiff2PdfStream : TiffStream
    {
        public override int Read(object fd, byte[] buf, int offset, int size)
        {
            return -1;
        }

        public override int Write(object fd, byte[] buf, int size)
        {
            T2P t2p = fd as T2P;
            //if (!t2p.m_outputdisable && t2p.m_outputfile != null)
            //{
            //    int written = fwrite(buf, 1, size, t2p.m_outputfile);
            //    t2p.m_outputwritten += written;
            //    return written;
            //}
            return size;
        }

        public override int Seek(object fd, int off, int whence)
        {
            T2P t2p = fd as T2P;
            //if (!t2p.m_outputdisable && t2p.m_outputfile != null)
            //    return fseekt2p.m_outputfile, off, whence);

            return off;
        }

        public override bool Close(object fd)
        {
            return true;
        }

        public override int Size(object fd)
        {
            return -1;
        }
    }
}
