using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class PrintDirectory
    {
        public static void Main()
        {
            using (Tiff image = Tiff.Open(@"Sample Data\multipage.tif", "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                byte[] endOfLine = { (byte)'\r', (byte)'\n' };
                using (FileStream stream = new FileStream("PrintDirectory.txt", FileMode.Create))
                {
                    do
                    {
                        image.PrintDirectory(stream);

                        stream.Write(endOfLine, 0, endOfLine.Length);

                    } while (image.ReadDirectory());
                }
            }

            Process.Start("PrintDirectory.txt");
        }
    }
}
