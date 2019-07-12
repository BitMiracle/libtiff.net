using System;
using System.Diagnostics;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ConvertToSingleStrip
    {
        public static void Main()
        {
            using (Tiff input = Tiff.Open(@"Sample Data\multipage.tif", "r"))
            {
                if (input == null)
                {
                    MessageBox.Show("Could not open incoming image");
                    return;
                }

                if (input.IsTiled())
                {
                    MessageBox.Show("Could not process tiled image");
                    return;
                }

                using (Tiff output = Tiff.Open("ConvertToSingleStrip.tif", "w"))
                {
                    int numberOfDirectories = input.NumberOfDirectories();
                    for (short i = 0; i < numberOfDirectories; ++i)
                    {
                        input.SetDirectory(i);

                        copyTags(input, output);
                        copyStrips(input, output);

                        output.WriteDirectory();
                    }
                }
            }

            using (Tiff result = Tiff.Open(@"ConvertToSingleStrip.tif", "rc"))
            {
                MessageBox.Show("Number of strips in result file: " + result.NumberOfStrips());
            }

            Process.Start("ConvertToSingleStrip.tif");
        }

        private static void copyTags(Tiff input, Tiff output)
        {
            for (ushort t = ushort.MinValue; t < ushort.MaxValue; ++t)
            {
                TiffTag tag = (TiffTag)t;
                FieldValue[] tagValue = input.GetField(tag);
                if (tagValue != null)
                    output.GetTagMethods().SetField(output, tag, tagValue);
            }

            int height = input.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            output.SetField(TiffTag.ROWSPERSTRIP, height);
        }

        private static void copyStrips(Tiff input, Tiff output)
        {
            bool encoded = false;
            FieldValue[] compressionTagValue = input.GetField(TiffTag.COMPRESSION);
            if (compressionTagValue != null)
                encoded = (compressionTagValue[0].ToInt() != (int)Compression.NONE);

            int numberOfStrips = input.NumberOfStrips();

            int offset = 0;
            byte[] stripsData = new byte[numberOfStrips * input.StripSize()];
            for (int i = 0; i < numberOfStrips; ++i)
            {
                int bytesRead = readStrip(input, i, stripsData, offset, encoded);
                offset += bytesRead;
            }

            writeStrip(output, stripsData, offset, encoded);
        }

        private static int readStrip(Tiff image, int stripNumber, byte[] buffer, int offset, bool encoded)
        {
            if (encoded)
                return image.ReadEncodedStrip(stripNumber, buffer, offset, buffer.Length - offset);
            else
                return image.ReadRawStrip(stripNumber, buffer, offset, buffer.Length - offset);
        }

        private static void writeStrip(Tiff image, byte[] stripsData, int count, bool encoded)
        {
            if (encoded)
                image.WriteEncodedStrip(0, stripsData, count);
            else
                image.WriteRawStrip(0, stripsData, count);
        }
    }
}
