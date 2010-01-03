using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitMiracle.LibJpeg
{
    class BitStream : IDisposable
    {
        private bool m_alreadyDisposed = false;

        private const int bitsInByte = 8;
        private Stream m_stream;
        private int m_positionInByte;

        private int m_size;

        public BitStream()
        {
            m_stream = new MemoryStream();
            m_positionInByte = 0;

            m_size = 0;
        }

        public BitStream(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            m_stream = new MemoryStream(buffer);
            m_positionInByte = 0;

            m_size = bitsAllocated();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_alreadyDisposed)
            {
                if (disposing)
                {
                    if (m_stream != null)
                        m_stream.Dispose();
                }

                m_stream = null;
                m_alreadyDisposed = true;
            }
        }

        public int Size()
        {
            return m_size;
        }

        public Stream UnderlyingStream
        {
            get
            {
                return m_stream;
            }
        }

        public virtual int Read(int bitCount)
        {
            if (Tell() + bitCount > bitsAllocated())
                throw new ArgumentOutOfRangeException("bitCount");

            return read(bitCount);
        }

        public int Write(int bitStorage, int bitCount)
        {
            if (bitCount == 0)
                return 0;

            const int maxBitsInStorage = sizeof(int) * bitsInByte;
            if (bitCount > maxBitsInStorage)
                throw new ArgumentOutOfRangeException("bitCount");

            for (int i = 0; i < bitCount; ++i)
            {
                byte bit = (byte)((bitStorage << (maxBitsInStorage - (bitCount - i))) >> (maxBitsInStorage - 1));
                if (!writeBit(bit))
                    return i;
            }

            return bitCount;
        }

        public void Seek(int pos, SeekOrigin mode)
        {
            switch (mode)
            {
                case SeekOrigin.Begin:
                    seekSet(pos);
                    break;

                case SeekOrigin.Current:
                    seekCurrent(pos);
                    break;

                case SeekOrigin.End:
                    seekSet(Size() + pos);
                    break;
            }
        }

        public int Tell()
        {
            return (int)m_stream.Position * bitsInByte + m_positionInByte;
        }

        private int bitsAllocated()
        {
            return (int)m_stream.Length * bitsInByte;
        }

        private int read(int bitsCount)
        {
            //Codes are packed into a continuous bit stream, high-order bit first. 
            //This stream is then divided into 8-bit bytes, high-order bit first. 
            //Thus, codes can straddle byte boundaries arbitrarily. After the EOD marker (code value 257), 
            //any leftover bits in the final byte are set to 0.

            if (bitsCount < 0 || bitsCount > 32)
                throw new ArgumentOutOfRangeException("bitsCount");

            if (bitsCount == 0)
                return 0;

            int bitsRead = 0;
            int result = 0;
            byte[] bt = new byte[1];
            while (bitsRead == 0 || (bitsRead - m_positionInByte < bitsCount))
            {
                m_stream.Read(bt, 0, 1);

                result = (result << bitsInByte);
                result += bt[0];

                bitsRead += 8;
            }

            m_positionInByte = (m_positionInByte + bitsCount) % 8;
            if (m_positionInByte != 0)
            {
                result = (result >> (bitsInByte - m_positionInByte));

                m_stream.Seek(-1, SeekOrigin.Current);
            }

            if (bitsCount < 32)
            {
                int mask = ((1 << bitsCount) - 1);
                result = result & mask;
            }

            return result;
        }

        private bool writeBit(byte bit)
        {
            if (m_stream.Position == m_stream.Length)
            {
                byte[] bt = { (byte)(bit << (bitsInByte - 1)) };
                m_stream.Write(bt, 0, 1);
                m_stream.Seek(-1, SeekOrigin.Current);
            }
            else
            {
                byte[] bt = { 0 };
                m_stream.Read(bt, 0, 1);
                m_stream.Seek(-1, SeekOrigin.Current);

                int shift = (bitsInByte - m_positionInByte - 1) % bitsInByte;
                byte maskByte = (byte)(bit << shift);

                bt[0] |= maskByte;
                m_stream.Write(bt, 0, 1);
                m_stream.Seek(-1, SeekOrigin.Current);
            }

            Seek(1, SeekOrigin.Current);

            int currentPosition = Tell();
            if (currentPosition > m_size)
                m_size = currentPosition;

            return true;
        }

        private void seekSet(int pos)
        {
            if (pos < 0)
                throw new ArgumentOutOfRangeException("pos");

            int byteDisplacement = pos / bitsInByte;
            m_stream.Seek(byteDisplacement, SeekOrigin.Begin);

            int shiftInByte = pos - byteDisplacement * bitsInByte;
            m_positionInByte = shiftInByte;
        }

        private void seekCurrent(int pos)
        {
            int result = Tell() + pos;
            if (result < 0 || result > bitsAllocated())
                throw new ArgumentOutOfRangeException("pos");

            seekSet(result);
        }
    }
}
