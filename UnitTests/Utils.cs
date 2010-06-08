using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UnitTests
{
    class Utils
    {
        public static bool FilesAreEqual(string left, string right)
        {
            byte[] leftBytes = File.ReadAllBytes(left);
            byte[] rightBytes = File.ReadAllBytes(right);

            if (leftBytes == null || rightBytes == null)
                return false;

            if (leftBytes.Length != rightBytes.Length)
                return false;

            for (int i = 0; i < leftBytes.Length; i++)
            {
                if (leftBytes[i] != rightBytes[i])
                    return false;
            }

            return true;
        }
    }
}
