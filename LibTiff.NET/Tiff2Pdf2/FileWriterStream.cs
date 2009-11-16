using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BitMiracle.Docotic.PDFLib
{
    class FileWriterStream : PDFStream
    {
        private FileStream m_file;

        public FileWriterStream(string fname)
        {
            m_file = File.Open(fname, FileMode.OpenOrCreate);
            if (m_file == null)
                throw new PdfException(PdfExceptionType.FileOpenFailed);
        }

        public override PDFStream Clone()
        {
            throw new PdfException(PdfExceptionType.FileIOError);
        }

        public override void Clear()
        {
        }

        public void Close()
        {
            m_file.Close();
            m_file.Dispose();
        }

        public override int Read(byte[] ptr, int size)
        {
            throw new PdfException(PdfExceptionType.FileIOError);
        }

        public override int Write(byte[] ptr, int size)
        {
            try
            {
                m_file.Write(ptr, 0, size);
            }
            catch (System.IO.IOException)
            {
                throw new PdfException(PdfExceptionType.FileIOError);
            }

            return size;
        }

        public override void Seek(int pos, SeekOrigin mode)
        {
            throw new PdfException(PdfExceptionType.FileIOError);
        }

        public override int Tell()
        {
            throw new PdfException(PdfExceptionType.FileIOError);
        }

        public override int Size()
        {
            return (int)m_file.Length;
        }
    }
}
