using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BitMiracle.LibTiff.Classic
{
    class Enc28591
    {
        public virtual char[] GetChars(byte[] bytes)
        {
            return GetChars(bytes, 0, bytes.Length);
        }

        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            var tmp = GetChars(bytes, byteIndex, byteCount);
            Array.Copy(tmp, 0, chars, charIndex, tmp.Length);
            return tmp.Length;
        }

        public virtual string GetString(byte[] bytes)
        {
            return GetString(bytes, 0, bytes.Length);
        }

        public virtual string GetString(byte[] bytes, int start, int count)
        {
            return new string(GetChars(bytes, start, count));
        }

        public virtual byte[] GetBytes(char[] chars)
        {
            return GetBytes(chars, 0, chars.Length);
        }

        public virtual byte[] GetBytes(string s)
        {
            var tmp = s.ToCharArray();
            return GetBytes(tmp, 0, tmp.Length);
        }

        public virtual int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            var tmp = GetBytes(chars, charIndex, charCount);
            Array.Copy(tmp, 0, bytes, byteIndex, tmp.Length);
            return tmp.Length;
        }

        public virtual int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
        }

        public virtual int GetByteCount(char[] chars)
        {
            return GetByteCount(chars, 0, chars.Length);
        }

        public virtual int GetByteCount(String s)
        {
            var tmp = s.ToCharArray();
            return GetByteCount(tmp, 0, tmp.Length);
        }

        public virtual int GetCharCount(byte[] bytes)
        {
            return GetCharCount(bytes, 0, bytes.Length);
        }

        public char[] GetChars(byte[] bytes, int index, int count)
        {
            var result = new char[count];
            for (var i = 0; i < result.Length; i++)
                result[i] = (char)bytes[i + index];
            return result;
        }

        public byte[] GetBytes(char[] chars, int index, int count)
        {
            var result = new byte[count];
            for (var i = 0; i < result.Length; i++)
            {
                var c = chars[i + index];
                result[i] = (c > 255) ? (byte)'?' : (byte)c;
            }
            return result;
        }

        public int GetByteCount(char[] chars, int index, int count)
        {
            return count;
        }

        public int GetCharCount(byte[] bytes, int index, int count)
        {
            return count;
        }

        public int CodePage
        {
            get { return 28591; }
        }

        public string EncodingName
        {
            get { return "Western European (ISO)"; }
        }

        public string WebName
        {
            get { return "iso-8859-1"; }
        }
    }
}
