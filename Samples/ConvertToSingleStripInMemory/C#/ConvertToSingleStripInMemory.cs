using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class ConvertToSingleStripInMemory
    {
        public static void Main()
        {
            byte[] inputBytes = File.ReadAllBytes(@"Sample Data\multipage.tif");
            TiffStreamForBytes byteStream = new TiffStreamForBytes(inputBytes);

            using (Tiff input = Tiff.ClientOpen("bytes", "r", null, byteStream))
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

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Tiff output = Tiff.ClientOpen("InMemory", "w", ms, new TiffStream()))
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

                    // retrieve bytes from memory stream and write them in a file
                    byte[] bytes = ms.ToArray();
                    File.WriteAllBytes("SavedBytes.tif", bytes);
                }
            }

            using (Tiff result = Tiff.Open("SavedBytes.tif", "rc"))
            {
                MessageBox.Show("Number of strips in result file: " + result.NumberOfStrips());
            }

            Process.Start("SavedBytes.tif");
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

    /// <summary>
    /// Custom read-only stream for byte buffer that can be used
    /// with Tiff.ClientOpen method.
    /// </summary>
    class TiffStreamForBytes : TiffStream
    {
        private byte[] m_bytes;
        private int m_position;

        public TiffStreamForBytes(byte[] bytes)
        {
            m_bytes = bytes;
            m_position = 0;
        }

        public override int Read(object clientData, byte[] buffer, int offset, int count)
        {
            if ((m_position + count) > m_bytes.Length)
                return -1;

            Buffer.BlockCopy(m_bytes, m_position, buffer, offset, count);
            m_position += count;
            return count;
        }

        public override void Write(object clientData, byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("This stream is read-only");
        }

        public override long Seek(object clientData, long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset > m_bytes.Length)
                        return -1;

                    m_position = (int)offset;
                    return m_position;

                case SeekOrigin.Current:
                    if ((offset + m_position) > m_bytes.Length)
                        return -1;

                    m_position += (int)offset;
                    return m_position;

                case SeekOrigin.End:
                    if ((m_bytes.Length - offset) < 0)
                        return -1;

                    m_position = (int)(m_bytes.Length - offset);
                    return m_position;
            }

            return -1;
        }

        public override void Close(object clientData)
        {
            // nothing to do
        }

        public override long Size(object clientData)
        {
            return m_bytes.Length;
        }
    }
}
