using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

namespace BitMiracle.Docotic.PDFLib
{
    abstract class PDFStream
    {
        protected PDFStream()
        {
        }

	    public abstract PDFStream Clone();
	    public abstract void Clear();

        public byte[] GetBuffer()
        {
            int size = Size();
            if (size == 0)
                return null;

            byte[] result = new byte [size];
            int oldPos = Tell();
            Seek(0, SeekOrigin.Begin);
            Read(result, size);
            Seek(oldPos, SeekOrigin.Begin);
            return result;
        }

        public void WriteToStream(PDFStream dst)
        {
            byte[] buffer = GetBuffer();
            dst.Write(buffer, this.Size());
        }

        //public void WriteToStream(PDFStream dst, Filter filter)
        //{
        //    if (dst == null)
        //        throw new PdfException(PdfExceptionType.InvalidParameter);

        //    int byteSize = Size();
        //    if (byteSize == 0)
        //        return;

        //    Seek(0, SeekOrigin.Begin);

        //    byte[] data = new byte [byteSize];
        //    Array.Clear(data, 0, byteSize);
        //    byteSize = Read(data, byteSize);
        //    dst.Write(data, byteSize);
        //}
        
        public void WriteChar(char value)
        {
            byte[] bytes = { (byte)value };
            Write(bytes, 1);
        }

        public void WriteStr(string value)
        {
            byte[] bytes = Encoding.Default.GetBytes(value);
            Write(bytes, bytes.Length);
        }

        public void WriteUChar(byte value)
        {
            byte[] bytes = { value };
            Write(bytes, 1);
        }

        public void WriteInt(int value)
        {
            WriteStr(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteUInt(int value)
        {
            WriteInt((int)value);
        }

        public void WriteReal(float value)
        {
            WriteStr(FloatToString(value, 5, true));
        }

        public void WriteEscapeName(string value)
        {
            string escapeName = ToEscapeName(value);
            Write(Encoding.Default.GetBytes(escapeName), escapeName.Length);
        }

        public void WriteBinary(byte[] data, int len)
        {
            byte[] dataToWrite = data;
            StringBuilder sb = new StringBuilder();
            foreach (byte b in dataToWrite)
                sb.Append(b.ToString("X2"));

            WriteStr(sb.ToString());
        }
    
        public void WriteEscapeText(string text)
        {
            if (text == null)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append('(');

            byte[] bytes = Encoding.Default.GetBytes(text);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte c = bytes[i];
                if (NeedsToBeEscaped(c))
                {
                    sb.Append('\\');

                    string octal = Convert.ToString(c, 8);
                    if (octal.Length < 3)
                        sb.Append('0');

                    sb.Append(octal);
                }
                else
                    sb.Append(Encoding.Default.GetChars(new byte[] { c }));
            }

            sb.Append(')');

            byte[] escaped = Encoding.Default.GetBytes(sb.ToString());
            Write(escaped, escaped.Length);
        }

        public void Write(int first, int second, string text)
        {
            WriteStr(string.Format("{0} {1} {2}", first, second, text));
        }

        public void Write(float[] reals, int count, string text)
        {
            if (reals == null)
                throw new PdfException(PdfException.InvalidParameter);

            if (count < 1)
                throw new PdfException(PdfException.InvalidParameter);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append(FloatToString(reals[i], 5, true));
                sb.Append(' ');
            }

            WriteStr(sb.ToString());
        }

        public void Write(float first, float second, string text)
        {
            float[] reals = { first, second };
            Write(reals, 2, text);
        }

        public virtual int ReadByte()
        {
            byte[] b = new byte[1];
            int read = Read(b, 1);
            
            if (read == 0)
                return -1;

            return b[0];
        }

        public virtual int ReadShort()
        {
            return ReadShort(true);
        }

        public virtual int ReadShort(bool littleEndian)
        {
            byte[] b = new byte[2];
            int read = Read(b, 2);

            if (read < 2)
                return -1;

            if (littleEndian)
                return (b[0] + (b[1] << 8));

            return ((b[0] << 8) + b[1]);
        }

        public virtual int ReadInt()
        {
            return ReadInt(true);
        }

        public virtual int ReadInt(bool littleEndian)
        {
            byte[] b = new byte[4];
            Read(b, 4);

            if (littleEndian)
                return (int)(b[3] << 24) | (b[2] << 16) | (b[1] << 8) | b[0];

            return (int)(b[0] << 24 | (b[1] & 0xff) << 16 | (b[2] & 0xff) << 8 | (b[3] & 0xff));
        }

        public void Skip(int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            Read(bytes, bytes.Length);
        }
        
        public abstract int Read(byte[] ptr, int size);
        public abstract int Write(byte[] ptr, int size);

        public abstract void Seek(int pos, SeekOrigin mode);
        public abstract int Tell();

        public abstract int Size();

        public virtual bool Equals(PDFStream stream)
        {
            if (this == stream)
                return true;

            if (stream == null)
                return false;

            if (Size() != stream.Size())
                return false;
            
            int oldPosFirst = Tell();
            int oldPosSecond = stream.Tell();
            Seek(0, SeekOrigin.Begin);
            stream.Seek(0, SeekOrigin.Begin);

            int size = Size();
            byte[] streamBytes = new byte[size];
            byte[] thisBytes = new byte[size];

            stream.Read(streamBytes, size);
            Read(thisBytes, size);

            bool res = true;
            for (int i = 0; i < size; i++)
            {
                if (streamBytes[i] != thisBytes[i])
                {
                    res = false;
                    break;
                }
            }
                
            stream.Seek(oldPosSecond, SeekOrigin.Begin);
            Seek(oldPosFirst, SeekOrigin.Begin);

            return res;
        }

        private static string FloatToString(float real)
        {
            return FloatToString(real, 3, false);
        }

        private static string FloatToString(float real, int afterDotCount, bool agressiveZeroRemoval)
        {
            // add 0.000005 to compensate rounding error of float values
            string s = (real + 0.000005).ToString("F" + afterDotCount.ToString(), CultureInfo.InvariantCulture);

            // remove excessive zeros an end
            int dotPos = s.LastIndexOf('.');
            int excessiveZeros = 0;
            if (dotPos != -1)
            {
                if (agressiveZeroRemoval)
                {
                    // start count zeros from digit before last digit
                    // it means we'll remove any 5th digit after decimal dot if 
                    // 4th digit after decimal dot is zero
                    // "x.00001" -> "x", but
                    // "x.00011" -> "x.00011"
                    for (int i = s.Length - 2; i > dotPos; i--)
                    {
                        if (s[i] != '0')
                            break;

                        excessiveZeros++;
                    }

                    if (excessiveZeros > 0)
                    {
                        // take into account the 5th digit
                        excessiveZeros++;
                    }
                }
                else
                {
                    for (int i = s.Length - 1; i > dotPos; i--)
                    {
                        if (s[i] != '0')
                            break;

                        excessiveZeros++;
                    }
                }

                int removeCharCount = excessiveZeros;
                if ((s.Length - excessiveZeros - 1) == dotPos)
                {
                    // we removed all digits after dot,
                    // remove dot too
                    removeCharCount++;
                }

                return s.Substring(0, s.Length - removeCharCount);
            }

            return s;
        }

        private static string ToEscapeName(string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('/');

            byte[] bytes = Encoding.Default.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte c = bytes[i];
                if (NeedsToBeEscaped(c) || (c == 32))
                {
                    sb.Append('#');
                    sb.Append(c.ToString("X2"));
                }
                else
                    sb.Append(Encoding.Default.GetChars(new byte[] { c }));
            }

            return sb.ToString();
        }

        private static bool NeedsToBeEscaped(byte c)
        {
            return (c < 32 || c == (byte)'\\' || c == (byte)'(' || c == (byte)')');
        }
    }
}
