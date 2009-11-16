using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace BitMiracle.Docotic.PDFLib
{
    class CUtils
    {
        static public bool AreNumbersEqual(float number1, float number2)
        {
            return AreNumbersEqual(number1, number2, 0.001f);
        }

        static public bool AreNumbersEqual(float number1, float number2, float eps)
        {
            return Math.Abs(number1 - number2) < eps;
        }

        public static int memcmp(byte[] left, byte[] right, int length)
        {
            return memcmp(left, 0, right, length);
        }

        public static int memcmp(byte[] left, int offset, byte[] right, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (left[offset + i] != right[i])
                    return left[offset + i] - right[i];
            }

            return 0;
        }

        public static bool AreEqual(byte[] left, byte[] right, int length)
        {
            return (memcmp(left, right, length) == 0);
        }

        /** Retrieves the library name and version
	    *  @return the library name and version
	    */
        public static string getLibraryTagString()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionString = version.Major.ToString() + "." + version.Minor.ToString() + "." + version.Build.ToString();

            return "BitMiracle.Docotic " + versionString;
        }

        public static int UShiftRight(int left, int right)
        {
            if (right < 1)
                return left;

            return unchecked((int)((uint)left >> right));
        }
    }
}
