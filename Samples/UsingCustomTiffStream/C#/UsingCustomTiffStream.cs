using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class UsingCustomTiffStream
    {
        static void Main(string[] args)
        {
            MyStream stream = new MyStream();

            // Open the TIFF image for reading
            using (Tiff image = Tiff.ClientOpen("custom", "r", null, stream))
            {
                if (image == null)
                    return;

                // Read image data here the same way
                // as if LibTiff.Net was using regular image file
                image.Close();
            }
        }

        /// <summary>
        /// Custom stream for LibTiff.Net.
        /// Please consult documentation for TiffStream class for method parameters meaning.
        /// </summary>
        class MyStream : TiffStream
        {
            // You may implement any constructor you want here.

            public override int Read(object clientData, byte[] buffer, int offset, int count)
            {
                // stub implementation
                return -1;
            }

            public override void Write(object clientData, byte[] buffer, int offset, int count)
            {
                // stub implementation
            }

            public override long Seek(object clientData, long offset, System.IO.SeekOrigin whence)
            {
                // stub implementation
                return -1;
            }

            public override void Close(object clientData)
            {
                // stub implementation
            }

            public override long Size(object clientData)
            {
                // stub implementation
                return -1;
            }
        }
    }
}
