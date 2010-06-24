#if SILVERLIGHT
// Some extension methods are required to emulate missing methods

using System;
using System.Text;

namespace BitMiracle.LibTiff.Classic
{
    static class Extensions
    {
        static public string GetString(this Encoding self, byte[] bytes)
        {
            // Should never happen as long as this Extensions class is not used directly.
            System.Diagnostics.Debug.Assert(self != null);

            int length = bytes.Length;
            return self.GetString(bytes, 0, length);
        }
    }
}
#endif