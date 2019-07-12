using System;
using System.Text;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class NumberOfPages
    {
        public static void Main()
        {
            const string fileName = "Sample Data/multipage.tif";

            using (Tiff image = Tiff.Open(fileName, "r"))
            {
                if (image == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                StringBuilder message = new StringBuilder();
                message.AppendFormat("Tiff.NumberOfDirectories() returns {0} pages\n", image.NumberOfDirectories());
                message.AppendFormat("Enumerated {0} pages", CalculatePageNumber(image));
                
                MessageBox.Show(message.ToString());
            }
        }

        private static int CalculatePageNumber(Tiff image)
        {
            int pageCount = 0;
            do
            {
                ++pageCount;
            } while (image.ReadDirectory());

            return pageCount;
        }
    }
}
