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

    /// <summary>
    /// This list defines the known output image formats
    /// (not all of which need be supported by a given version).
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum BitmapFormat
    {
        Windows, /* BMP format (Windows flavor) */
        OS2 /* BMP format (OS/2 flavor) */
    }

    enum ADDON_MESSAGE_CODE
    {
        // Must be first entry!
        JMSG_FIRSTADDONCODE = 1000,

        JERR_BMP_BADCMAP,
        JERR_BMP_BADDEPTH,
        JERR_BMP_BADHEADER,
        JERR_BMP_BADPLANES,
        JERR_BMP_COLORSPACE,
        JERR_BMP_COMPRESSED,
        JERR_BMP_NOT,
        JTRC_BMP,
        JTRC_BMP_MAPPED,
        JTRC_BMP_OS2,
        JTRC_BMP_OS2_MAPPED,

        JERR_GIF_BUG,
        JERR_GIF_CODESIZE,
        JERR_GIF_COLORSPACE,
        JERR_GIF_IMAGENOTFOUND,
        JERR_GIF_NOT,
        JTRC_GIF,
        JTRC_GIF_BADVERSION,
        JTRC_GIF_EXTENSION,
        JTRC_GIF_NONSQUARE,
        JWRN_GIF_BADDATA,
        JWRN_GIF_CHAR,
        JWRN_GIF_ENDCODE,
        JWRN_GIF_NOMOREDATA,

        JERR_BAD_CMAP_FILE,
        JERR_TOO_MANY_COLORS,
        JERR_UNGETC_FAILED,
        JERR_UNKNOWN_FORMAT,
        JERR_UNSUPPORTED_FORMAT,

        JMSG_LASTADDONCODE
    }
}
