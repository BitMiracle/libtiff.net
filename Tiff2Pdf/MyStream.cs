using System;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.Tiff2Pdf
{
    class MyStream : TiffStream
    {
        public override int Read(object clientData, byte[] buffer, int offset, int count)
        {
            return -1;
        }

        public override void Write(object clientData, byte[] buffer, int offset, int count)
        {
            T2P t2p = clientData as T2P;
            if (t2p == null)
                throw new ArgumentException(Tiff2PdfConstants.UnexpectedClientData, "clientData");

            if (!t2p.m_outputdisable && t2p.m_outputfile != null)
            {
                t2p.m_outputfile.Write(buffer, offset, count);
                t2p.m_outputwritten += count;
            }
        }

        public override long Seek(object clientData, long offset, SeekOrigin origin)
        {
            T2P t2p = clientData as T2P;
            if (t2p == null)
                throw new ArgumentException(Tiff2PdfConstants.UnexpectedClientData, "clientData");

            if (!t2p.m_outputdisable && t2p.m_outputfile != null)
                return t2p.m_outputfile.Seek(offset, origin);

            return offset;
        }

        public override void Close(object clientData)
        {
        }

        public override long Size(object clientData)
        {
            return -1;
        }
    }
}
