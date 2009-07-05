using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibJpeg
{
    class BitStream
    {
        private const int bitsInByte = 8;
        private Stream m_stream;
        private int m_positionInByte;

        public BitStream(byte[] buffer)
        {
            m_stream = new MemoryStream(buffer);
            m_positionInByte = 0;
        }

        /*public BitStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            m_stream = stream.Clone();
            m_stream.Seek(0, SeekOrigin.Begin);
            m_positionInByte = 0;
        }*/

        //public virtual ~BitStream();

        public int Size()
        {
            return (int)m_stream.Length * bitsInByte;
        }

        public virtual int Read(int bitCount)
        {
            if (Tell() + bitCount > Size())
                throw new ArgumentException("Can't read bitCount bits");

            return read(bitCount);
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
            }
        }

        public int Tell()
        {
            return (int)m_stream.Position * bitsInByte + m_positionInByte;
        }

        private int read(int bitsCount)
        {
            //Codes are packed into a continuous bit stream, high-order bit first. 
            //This stream is then divided into 8-bit bytes, high-order bit first. 
            //Thus, codes can straddle byte boundaries arbitrarily. After the EOD marker (code value 257), 
            //any leftover bits in the final byte are set to 0.

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

            int mask = ((1 << bitsCount) - 1);
            result = result & mask;

            return result;
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
            if (result < 0 || result > Size())
                throw new ArgumentException("Wrong position");

            seekSet(result);
        }
    }
}
