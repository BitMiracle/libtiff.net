using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Tiff2Pdf
{
    class Tiff2PdfConstants
    {
        public const string TIFF2PDF_MODULE = "tiff2pdf";
        public const float PS_UNIT_SIZE	= 72.0F;
    }

    /* This struct defines a logical page of a TIFF. */
    struct T2P_PAGE
    {
        public UInt16 page_directory;
        public uint page_number;
        public uint page_tilecount;
        public uint page_extra;
    };

    /* This struct defines a PDF rectangle's coordinates. */
    class T2P_BOX
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;
        public float[] mat = new float[9];
    };

    /* This struct defines a tile of a PDF.  */
    struct T2P_TILE
    {
        public T2P_BOX tile_box;
    };

    /* This struct defines information about the tiles on a PDF page. */
    struct T2P_TILES
    {
        public uint tiles_tilecount;
        public uint tiles_tilewidth;
        public uint tiles_tilelength;
        public uint tiles_tilecountx;
        public uint tiles_tilecounty;
        public uint tiles_edgetilewidth;
        public uint tiles_edgetilelength;
        public T2P_TILE tiles_tiles;
    };

    /* This type is of PDF color spaces. */
    enum t2p_cs_t
    {
        T2P_CS_UNKNOWN = 0,
        T2P_CS_BILEVEL = 0x01,  /* Bilevel, black and white */
        T2P_CS_GRAY = 0x02,  /* Single channel */
        T2P_CS_RGB = 0x04,  /* Three channel tristimulus RGB */
        T2P_CS_CMYK = 0x08,  /* Four channel CMYK print inkset */
        T2P_CS_LAB = 0x10,  /* Three channel L*a*b* color space */
        T2P_CS_PALETTE = 0x1000,  /* One of the above with a color map */
        T2P_CS_CALGRAY = 0x20,  /* Calibrated single channel */
        T2P_CS_CALRGB = 0x40,  /* Calibrated three channel tristimulus RGB */
        T2P_CS_ICCBASED = 0x80 /* ICC profile color specification */
    };

    /* This type is of PDF compression types.  */
    enum t2p_compress_t
    {
        T2P_COMPRESS_NONE = 0x00,
        T2P_COMPRESS_G4 = 0x01,
        T2P_COMPRESS_JPEG = 0x02,
        T2P_COMPRESS_ZIP = 0x04
    };

    /* This type is whether TIFF image data can be used in PDF without transcoding. */
    enum t2p_transcode_t
    {
        T2P_TRANSCODE_UNKNOWN = 0,
        T2P_TRANSCODE_RAW = 0x01,  /* The raw data from the input can be used without recompressing */
        T2P_TRANSCODE_ENCODE = 0x02 /* The data from the input is perhaps unencoded and reencoded */
    };

    /* This type is of information about the data samples of the input image. */
    enum t2p_sample_t
    {
        T2P_SAMPLE_NOTHING = 0x0000,  /* The unencoded samples are normal for the output colorspace */
        T2P_SAMPLE_ABGR_TO_RGB = 0x0001,  /* The unencoded samples are the result of ReadRGBAImage */
        T2P_SAMPLE_RGBA_TO_RGB = 0x0002,  /* The unencoded samples are contiguous RGBA */
        T2P_SAMPLE_RGBAA_TO_RGB = 0x0004,  /* The unencoded samples are RGBA with premultiplied alpha */
        T2P_SAMPLE_YCBCR_TO_RGB = 0x0008, 
        T2P_SAMPLE_YCBCR_TO_LAB = 0x0010, 
        T2P_SAMPLE_REALIZE_PALETTE = 0x0020,  /* The unencoded samples are indexes into the color map */
        T2P_SAMPLE_SIGNED_TO_UNSIGNED = 0x0040,  /* The unencoded samples are signed instead of unsignd */
        T2P_SAMPLE_LAB_SIGNED_TO_UNSIGNED = 0x0040,  /* The L*a*b* samples have a* and b* signed */
        T2P_SAMPLE_PLANAR_SEPARATE_TO_CONTIG = 0x0100 /* The unencoded samples are separate instead of contiguous */
    };
}
