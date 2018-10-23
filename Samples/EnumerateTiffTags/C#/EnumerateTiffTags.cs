using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class EnumerateTiffTags
    {
        public static void Main()
        {
            const string fileName = @"Sample data\multipage.tif";

            using (Tiff image = Tiff.Open(fileName, "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                using (StreamWriter writer = new StreamWriter("EnumerateTiffTags.txt"))
                {
                    short numberOfDirectories = image.NumberOfDirectories();
                    for (short d = 0; d < numberOfDirectories; ++d)
                    {
                        if (d != 0)
                            writer.WriteLine("---------------------------------");

                        image.SetDirectory((short)d);

                        writer.WriteLine("Image {0}, page {1} has following tags set:\n", fileName, d);
                        for (ushort t = ushort.MinValue; t < ushort.MaxValue; ++t)
                        {
                            TiffTag tag = (TiffTag)t;
                            FieldValue[] value = image.GetField(tag);
                            if (value != null)
                            {
                                for (int j = 0; j < value.Length; j++)
                                {
                                    writer.WriteLine("{0}", tag.ToString());
                                    writer.WriteLine("{0} : {1}", value[j].Value.GetType().ToString(), value[j].ToString());
                                }

                                writer.WriteLine();
                            }
                        }
                    }
                }
            }

            Process.Start("EnumerateTiffTags.txt");
        }
    }
}