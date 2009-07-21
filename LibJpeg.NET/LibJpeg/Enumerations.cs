using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibJpeg
{
    /// <summary>
    /// Known color spaces.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum Colorspace
    {
        Unknown,    /* error/unspecified */
        Grayscale,  /* monochrome */
        RGB,        /* red/green/blue */
        YCbCr,      /* Y/Cb/Cr (also known as YUV) */
        CMYK,       /* C/M/Y/K */
        YCCK        /* Y/Cb/Cr/K */
    }

    /// <summary>
    /// DCT/IDCT algorithm options.
    /// </summary>
    enum DCTMethod
    {
        IntegerSlow,     /* slow but accurate integer algorithm */
        IntegerFast,     /* faster, less accurate integer method */
        Float            /* floating-point: accurate, fast on fast HW */
    }

    /// <summary>
    /// Dithering options for decompression.
    /// </summary>
    enum DitherMode
    {
        None,               /* no dithering */
        Ordered,            /* simple ordered dither */
        FloydSteinberg      /* Floyd-Steinberg error diffusion dither */
    }
}
