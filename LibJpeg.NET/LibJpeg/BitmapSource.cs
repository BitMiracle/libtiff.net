using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

using LibJpeg.Classic;


namespace LibJpeg
{
    interface INonCompressedImage
    {
        int Width
        { get; }

        int Height
        { get; }

        Colorspace Colorspace
        { get; }

        int ComponentsPerPixel
        { get; }

        int DataPrecision
        { get; }

        byte DensityUnit
        { get; }

        int DensityX
        { get; }

        int DensityY
        { get; }

        void Start();
        byte[] GetPixelRow();
        void Finish();
    }

    class DotNetBitmapSource : INonCompressedImage
    {
        private Bitmap m_bitmap;
        private int m_scanLine = 0;

        public DotNetBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            m_bitmap = bitmap;
        }

        public int Width
        {
            get
            {
                return m_bitmap.Width;
            }
        }

        public int Height
        {
            get
            {
                return m_bitmap.Height;
            }
        }

        public Colorspace Colorspace
        {
            get
            {
                return Colorspace.RGB;
            }
        }

        public int ComponentsPerPixel
        {
            get
            {
                return 3;
            }
        }

        public int DataPrecision
        {
            get
            {
                return m_bitmap.Flags;
            }
        }

        public byte DensityUnit
        {
            get
            {
                return 0;
            }
        }

        public int DensityX
        {
            get
            {
                return 1;
            }
        }

        public int DensityY
        {
            get
            {
                return 1;
            }
        }

        public void Start()
        {
            m_scanLine = 0;
        }

        public byte[] GetPixelRow()
        {
            if (m_scanLine == m_bitmap.Height)
                return null;

            byte[] rgbValues = new byte[m_bitmap.Width * 3];
            for (int i = 0; i < m_bitmap.Width; ++i)
            {
                Color color = m_bitmap.GetPixel(i, m_scanLine);
                rgbValues[i * 3] = color.R;
                rgbValues[i * 3 + 1] = color.G;
                rgbValues[i * 3 + 2] = color.B; 
            }
            ++m_scanLine;
            return rgbValues;
        }

        public void Finish()
        {
        }
    }
}
