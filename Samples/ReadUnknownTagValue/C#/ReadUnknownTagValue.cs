using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ReadUnknownTagValue
    {
        public static void Main()
        {
            using (Tiff image = Tiff.Open(@"Sample Data\pc260001.tif", "r"))
            {
                // read auto-registered tag 50341
                FieldValue[] value = image.GetField((TiffTag)50341);
                System.Console.Out.WriteLine("Tag value(s) are as follows:");
                for (int i = 0; i < value.Length; i++)
                    System.Console.Out.WriteLine("{0} : {1}", i, value[i].ToString());
            }
        }
    }
}
