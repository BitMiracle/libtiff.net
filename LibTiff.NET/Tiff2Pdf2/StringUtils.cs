using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace BitMiracle.Docotic.PDFLib
{
    /// <summary>
    /// Class for various functions that converts values to string
    /// </summary>
    class StringUtils
    {
        public static bool IsANSI(string text)
        {
            return allCodesLessEqualThan(255, text);
        }

	    public static bool IsASCII(string text)
        {
            return allCodesLessEqualThan(127, text);
        }

        /** Converts simple string to wide wstring
         */
        public static string ExtendToUnicode(string pText)
        {
            return pText;

            //int nLength = pText.Length;
            //string result;
            //for (int i = 0; i < nLength; ++i)
            //{
            //    result += (unsigned char)pText[i];
            //}
            //return result;
        }

        /** Converts simple string to wide wstring
         */
        public static string TruncateFromUnicode(string pText)
        {
            return pText;

            //int nLength = pText.Length;
            //string result;
            //for (int i = 0; i < nLength; ++i)
            //{
            //    result += (unsigned char)pText[i];
            //}
            //return result;
        }

        public static byte[] truncateToBytes(string text)
        {
            byte[] bytes = new byte[text.Length];

            for (int i = 0; i < text.Length; ++i)
                bytes[i] = (byte)((int)text[i] & 0x00FF);

            return bytes;
        }

        /** Case-insensitive compare of two strings.
         *  Note: May be not good for non-english strings
         */
        public static int icompare(string s1, string s2)
        {
            return string.Compare(s1, s2, true);
        }

        public static void trimLeft(ref string str)
        {
            trimLeft(ref str, new char[] {'\t', '\r', '\n', ' '});
        }

        public static void trimLeft(ref string str, string chars2remove)
        {
            trimLeft(ref str, chars2remove.ToCharArray());
        }

        public static void trimLeft(ref string str, char[] chars2remove)
        {
            str = str.TrimStart(chars2remove);
        }

        public static void trimRight(ref string str)
        {
            trimRight(ref str, new char[] {'\t', '\r', '\n', ' '});
        }

        public static void trimRight(ref string str, string chars2remove)
        {
            trimRight(ref str, chars2remove.ToCharArray());
        }

        public static void trimRight(ref string str, char[] chars2remove)
        {
            str = str.TrimEnd(chars2remove);
        }

        public static string right(string str, int count)
        {
            int length = str.Length;
            if (length <= count)
                return str;

            return str.Substring(length - count);
        }

        public static int HexToInt(string hex)
        {
            return int.Parse(hex, NumberStyles.AllowHexSpecifier);
        }

        //public static int OctToInt(string oct)
        //{
        //    return Convert.ToInt32(oct, 8);
        //}

        public static int StringToInt(string number)
        {
            return int.Parse(number);
        }
        
        public static float StringToFloat(string real)
        {
            bool successful = false;
            return StringToFloat(real, ref successful);
        }

        public static float StringToFloat(string real, ref bool successful)
        {
            float value = float.NaN;
            try
            {
                value = Convert.ToSingle(real, CultureInfo.InvariantCulture.NumberFormat);
                successful = true;
            }
            catch (System.Exception)
            {
            	successful = false;
            }

            return value;
        }

        public static string FloatToString(float real)
        {
            return FloatToString(real, 3, false);
        }

        public static string FloatToString(float real, int afterDotCount, bool agressiveZeroRemoval)
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

        public static string IntToString(int number)
        {
            return number.ToString();
        }

        public static string ToEscapeName(string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('/');

            byte[] bytes = Encoding.Default.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte c = bytes[i];
                if (PDFUtils.NeedsToBeEscaped(c) || (c == 32))
                {
                    sb.Append('#');
                    sb.Append(c.ToString("X2"));
                }
                else
                    sb.Append(Encoding.Default.GetChars(new byte[] { c }));
            }
        
            return sb.ToString();
        }

        public static bool IsDigit(byte b)
        {
            return IsDigit((char)b);
        }

        public static bool IsDigit(char c)
        {
            return ("0123456789".IndexOf(c) != -1);
        }

	    public static string ProcessPDFFontName(string fontName)
        {
            //fontName may be = XXXXXX+Arial
            int plusPosition = 6;
            int pos = fontName.IndexOf('+');
            if (pos != plusPosition)
                return fontName;

            return fontName.Substring(plusPosition + 1);
        }

        public static int FindSubstring(byte[] pData, int dataLength, string substring)
        {
            return FindSubstring(pData, 0, dataLength, substring);
        }

        public static int FindSubstring(byte[] pData, int offset, int dataLength, string substring)
        {
            byte[] substringBytes = Encoding.Default.GetBytes(substring);

            int substringLength = substringBytes.Length;
            if (offset + substringLength > dataLength)
                return -1;

            if ((offset + substringLength) == dataLength)
            {
                if (CUtils.memcmp(pData, offset, substringBytes, substringLength) == 0)
                    return 0;

                return -1;
            }

            int posFound = -1;
            for (int i = offset; i < dataLength - substringLength; i++)
            {
                if (CUtils.memcmp(pData, i, substringBytes, substringLength) == 0)
                {
                    posFound = i;
                    break;
                }
            }

            return posFound;
        }
    
        public static int FindLastSubstring(byte[] pData, int offset, int dataLength, string substring)
        {
            byte[] substringBytes = Encoding.Default.GetBytes(substring);

            int substringLength = substringBytes.Length;
            if (substringLength > (dataLength - offset))
                return -1;

            if (substringLength == (dataLength - offset))
            {
                if (CUtils.memcmp(pData, offset, substringBytes, substringLength) == 0)
                    return 0;

                return -1;
            }

            int posFound = -1;
            for (int i = dataLength - substringLength; i >= offset; i--)
            {
                if (CUtils.memcmp(pData, i, substringBytes, substringLength) == 0)
                {
                    posFound = i;
                    break;
                }
            }

            return posFound;
        }

        public static int getNumSpaces(string str)
        {
            return getNumSpaces(Encoding.Default.GetBytes(str));
        }

        public static int getNumSpaces(byte[] str)
        {
            int result = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                if (str[i] == 32)
                    ++result;
            }

            return result;
        }

        //private static int stringToInt(string str, char postfix)
        //{
        //    return 0;

        //    //string format;
        //    //StringUtils.format(format, "%d", str.Length);
        //    //format = "%" + format + postfix;

        //    //int result = 0;
        //    //sscanf(str, format, &result);
        //    //return result;
        //}

        private static bool allCodesLessEqualThan(int code, string text)
        {
            foreach (char c in text)
            {
                if ((int)c > code)
                    return false;
            }

            return true;
        }
    }
}
