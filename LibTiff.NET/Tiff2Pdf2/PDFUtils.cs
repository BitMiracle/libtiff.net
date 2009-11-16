using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace BitMiracle.Docotic.PDFLib
{
    class PDFUtils
    {
        public static byte GetLowByte(ushort value)
        {
            return (byte)(value & 0xff);
        }

        public static byte GetHighByte(ushort value)
        {
            return (byte)(value >> 8);
        }

        public static bool checkValueBounds(float dValue, int lowerBound, int upperBound)
        {
            if (dValue >= lowerBound && dValue <= upperBound)
                return true;

            return false;
        }

        public static bool checkValueBounds(int value, int lowerBound, int upperBound)
        {
            if (value >= lowerBound && value <= upperBound)
                return true;

            return false;
        }

        public static void PdfStrCpy(ref string dest, string src, int count)
        {
            if (src != null)
                dest = src.Substring(0, Math.Min(src.Length, count));
            else
                dest = null;
        }

        public static bool NeedsToBeEscaped(byte c)
        {
            return (c < 32 || c == (byte)'\\' || c == (byte)'(' || c == (byte)')');
        }

        public static byte[] MD5HashAsBytes(string input)
        {
            return MD5HashAsBytes(input, false);
        }

        public static byte[] MD5HashAsBytes(string input, bool upperCase)
        {
            return Encoding.ASCII.GetBytes(MD5Hash(input, upperCase));
        }

        public static string MD5Hash(string input)
        {
            return MD5Hash(input, false);
        }

        public static string MD5Hash(string input, bool upperCase)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            string format = "x2";
            if (upperCase)
                format = "X2";

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString(format));

            return sb.ToString();
        }

        public static byte[] MD5HashRaw(byte[] input)
        {
            MD5 md5 = MD5.Create();
            return md5.ComputeHash(input);
        }
    }
}
