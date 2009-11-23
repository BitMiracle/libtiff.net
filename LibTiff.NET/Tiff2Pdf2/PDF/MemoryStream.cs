using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BitMiracle.Docotic.PDFLib
{
    class MemoryStream : PDFStream
    {
        private byte[] m_pBuffer;        //!< pointer to a buffer with file contents

        private int m_streamSize;          //!< file (buffer) size
        private int m_Position;	    //!< current position within file
        private int m_bufferBytesAllocated;		    //!< allocated buffer size (may be equal or greater then m_Size)

        public MemoryStream()
        {
            initialize(null, 0);
        }

        public MemoryStream(int buf_siz)
        {
            initialize(null, buf_siz);
        }

        public MemoryStream(byte[] buffer, int size)
        {
            initialize(buffer, size);
        }

        public MemoryStream(MemoryStream stream)
        {
            clear();

            m_streamSize = stream.m_streamSize;
            m_Position = stream.m_Position;
            m_bufferBytesAllocated = stream.m_bufferBytesAllocated;

            m_pBuffer = new byte[stream.m_streamSize];
            Array.Copy(stream.m_pBuffer, m_pBuffer, stream.m_streamSize);
        }

        //public MemoryStream operator=(MemoryStream file);
    	
        //public virtual ~MemoryStream();

        public override PDFStream Clone()
        {
            return new MemoryStream(this);
        }

        public override void Clear()
        {
            clear();
            initialize(null, 0);
        }

        public override int Read(byte[] ptr, int size)
        {
            if (ptr == null)
                return 0;

            if (m_pBuffer == null)
                return 0;

            if (m_Position >= m_streamSize)
                return 0;

            if (size == 0)
                return 0;

            int nRead;
            if (m_Position + size > m_streamSize)
                nRead = (m_streamSize - m_Position);
            else
                nRead = size;

            Array.Copy(m_pBuffer, m_Position, ptr, 0, nRead);
            m_Position += nRead;

            return nRead;
        }

        public string Read(int bytesCount)
        {
            byte[] buffer = new byte[bytesCount];
            Read(buffer, bytesCount);
            return Encoding.Default.GetString(buffer);
        }

        public void Write(Stream stream)
        {
            stream.Write(m_pBuffer, 0, m_streamSize);
        }

        public override int Write(byte[] ptr, int size)
        {
            if (ptr == null)
                return 0;

            if (size == 0)
                return 0;

            if (m_Position + size > m_bufferBytesAllocated)
                setSizeTo(m_Position + size);

            Array.Copy(ptr, 0, m_pBuffer, m_Position, size);
            m_Position += size;

            if (m_Position > m_streamSize)
                m_streamSize = m_Position;

            return size;
        }

        public override void Seek(int pos, SeekOrigin mode)
        {
            int newPos = 0;
            switch (mode)
            {
	            case SeekOrigin.Begin:
                    newPos = pos;
                    break;

	            case SeekOrigin.Current:
                    newPos = m_Position + pos;
                    break;

                case SeekOrigin.End:
                    newPos = m_Position - pos;
                    break;
	        }

            if (newPos < 0)
                newPos = 0;

            m_Position = newPos;
        }

        public override int Tell()
        {
            return m_Position;
        }

        /** Retrieves number of bytes within file
        *  @return number of bytes within file
        */
        public override int Size()
        {
            return m_streamSize;
        }

        private void initialize(byte[] buffer, int size)
        {
            if (buffer != null && size == 0)
                throw new PdfException(PdfException.InvalidParameter);

            m_Position = 0;

            if (buffer == null)
            {
                if (size == 0)
                    m_bufferBytesAllocated = 4096; // 4 Kbytes
                else
                    m_bufferBytesAllocated = size;

                m_streamSize = 0;
                m_pBuffer = new byte[m_bufferBytesAllocated];
            }
            else
            {
                m_bufferBytesAllocated = size;
                m_streamSize = size;
                m_pBuffer = new byte[m_streamSize];
                Array.Copy(buffer, m_pBuffer, m_streamSize);
            }
        }

        private void clear()
        {
            m_pBuffer = null;
            m_streamSize = 0;
        }

        /** Resizes file to specified number of bytes
        *  NOTE: this does not changes m_Size.
        */
        private void setSizeTo(int nBytes)
        {
            if (nBytes > m_bufferBytesAllocated)
            {
                // find new buffer size
                int newBufferSize = (((nBytes >> 12) + 1) << 12);

                byte[] pTemp = new byte[newBufferSize];
                if (pTemp != null)
                {
                    Array.Copy(m_pBuffer, pTemp, m_bufferBytesAllocated);
                    m_pBuffer = pTemp;
                }

                m_bufferBytesAllocated = newBufferSize;
            }
        }
    }
}
