using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg
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
#if EXPOSE_LIBJPEG
    public
#endif
    enum DCTMethod
    {
        IntegerSlow,     /* slow but accurate integer algorithm */
        IntegerFast,     /* faster, less accurate integer method */
        Float            /* floating-point: accurate, fast on fast HW */
    }

    /// <summary>
    /// Dithering options for decompression.
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum DitherMode
    {
        None,               /* no dithering */
        Ordered,            /* simple ordered dither */
        FloydSteinberg      /* Floyd-Steinberg error diffusion dither */
    }

    /// <summary>
    /// Describes a result of read operation
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum Read
    {
        Suspended = 0, /* Suspended due to lack of input data */
        HeaderOK = 1, /* Found valid image datastream */
        HeaderTablesOnly = 2, /* Found valid table-specs-only datastream */
        ReachedSOS = 3, /* Reached start of new scan */
        ReachedEOI = 4, /* Reached end of image */
        RowCompleted = 5, /* Completed one iMCU row */
        ScanCompleted = 6 /* Completed last iMCU row of a scan */
    }
}
