using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BitMiracle.LibJpeg
{
    class Utils
    {
        public static MemoryStream CopyStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("imageData");

            long positionBefore = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);

            MemoryStream result = new MemoryStream((int)stream.Length);

            byte[] block = new byte[2048];
            for (; ; )
            {
                int bytesRead = stream.Read(block, 0, 2048);
                result.Write(block, 0, bytesRead);
                if (bytesRead < 2048)
                    break;
            }

            stream.Seek(positionBefore, SeekOrigin.Begin);
            return result;
        }
    }
}
