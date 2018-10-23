using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ReadExifMetadata
    {
        public static void Main()
        {
            using (Tiff image = Tiff.Open(@"Sample data\dscf0013.tif", "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                FieldValue[] exifIFDTag = image.GetField(TiffTag.EXIFIFD);
                if (exifIFDTag == null)
                {
                    MessageBox.Show("Exif metadata not found");
                    return;
                }

                int exifIFDOffset = exifIFDTag[0].ToInt();
                if (!image.ReadEXIFDirectory(exifIFDOffset))
                {
                    MessageBox.Show("Could not read EXIF IFD");
                    return;
                }

                using (StreamWriter writer = new StreamWriter("ReadExifMetadata.txt"))
                {
                    for (TiffTag tag = TiffTag.EXIF_EXPOSURETIME; tag <= TiffTag.EXIF_IMAGEUNIQUEID; ++tag)
                    {
                        FieldValue[] value = image.GetField(tag);
                        if (value != null)
                        {
                            for (int i = 0; i < value.Length; i++)
                            {
                                writer.WriteLine("{0}", tag.ToString());
                                writer.WriteLine("{0} : {1}", value[i].Value.GetType().ToString(), value[i].ToString());
                            }

                            writer.WriteLine();
                        }
                    }
                }
            }

            Process.Start("ReadExifMetadata.txt");
        }
    }
}
