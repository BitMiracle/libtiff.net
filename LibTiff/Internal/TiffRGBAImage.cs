/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace BitMiracle.LibTiff.Classic.Internal
{
    /// <summary>
    /// RGBA-style image support.
    /// </summary>
    class TiffRGBAImage
    {
        // The image reading and conversion routines invoke
        // "put routines" to copy/image/whatever tiles of
        // raw image data.  A default set of routines are 
        // provided to convert/copy raw image data to 8-bit
        // packed ABGR format rasters.  Applications can supply
        // alternate routines that unpack the data into a
        // different format or, for example, unpack the data
        // and draw the unpacked raster on the display.

        public delegate void tileContigRoutine(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset);
        public delegate void tileSeparateRoutine(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset);

        public const string photoTag = "PhotometricInterpretation";

        public Tiff tif; /* image handle */
        public bool stoponerr; /* stop on read error */
        public bool isContig; /* data is packed/separate */
        public ExtraSample alpha; /* type of alpha data present */
        public int width; /* image width */
        public int height; /* image height */
        public short bitspersample; /* image bits/sample */
        public short samplesperpixel; /* image samples/pixel */
        public Orientation orientation; /* image orientation */
        public Orientation req_orientation; /* requested orientation */
        public Photometric photometric; /* image photometric interp */
        public short[] redcmap; /* colormap pallete */
        public short[] greencmap;
        public short[] bluecmap;

        /* get image data routine */
        public delegate bool getRoutine(TiffRGBAImage img, int[] raster, int offset, int w, int h);
        public getRoutine getRoutineInstance;

        public tileContigRoutine contig;
        public tileSeparateRoutine separate;

        public byte[] Map; /* sample mapping array */
        public int[][] BWmap; /* black&white map */
        public int[][] PALmap; /* palette image map */
        public TiffYCbCrToRGB ycbcr; /* YCbCr conversion state */
        public TiffCIELabToRGB cielab; /* CIE L*a*b conversion state */

        public int row_offset;
        public int col_offset;

        private static TiffDisplay display_sRGB = new TiffDisplay(
            /* XYZ -> luminance matrix */
            new float[] { 3.2410F, -1.5374F, -0.4986F },
            new float[] { -0.9692F, 1.8760F, 0.0416F },
            new float[] { 0.0556F, -0.2040F, 1.0570F },
            100.0F, 100.0F, 100.0F,  /* Light o/p for reference white */
            255, 255, 255,  /* Pixel values for ref. white */
            1.0F, 1.0F, 1.0F,  /* Residual light o/p for black pixel */
            2.4F, 2.4F, 2.4F  /* Gamma values for the three guns */
        );

        private const int A1 = 0xff << 24;

        /* 
        * Helper constants used in Orientation tag handling
        */
        private const int FLIP_VERTICALLY = 0x01;
        private const int FLIP_HORIZONTALLY = 0x02;

        public static TiffRGBAImage Create(Tiff tif, bool stop, out string emsg)
        {
            emsg = null;
            TiffRGBAImage img = new TiffRGBAImage();
            /* Initialize to normal values */
            img.row_offset = 0;
            img.col_offset = 0;
            img.redcmap = null;
            img.greencmap = null;
            img.bluecmap = null;
            img.req_orientation = Orientation.BOTLEFT; /* It is the default */

            img.tif = tif;
            img.stoponerr = stop;

            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.BITSPERSAMPLE);
            img.bitspersample = result[0].ToShort();
            switch (img.bitspersample)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                case 16:
                    break;
                
                default:
                    emsg = string.Format(CultureInfo.InvariantCulture,
                        "Sorry, can not handle images with {0}-bit samples", img.bitspersample);
                    return null;
            }

            img.alpha = 0;
            result = tif.GetFieldDefaulted(TiffTag.SAMPLESPERPIXEL);
            img.samplesperpixel = result[0].ToShort();

            result = tif.GetFieldDefaulted(TiffTag.EXTRASAMPLES);
            short extrasamples = result[0].ToShort();
            byte[] sampleinfo = result[1].ToByteArray();

            if (extrasamples >= 1)
            {
                switch ((ExtraSample)sampleinfo[0])
                {
                    case ExtraSample.UNSPECIFIED:
                        /* Workaround for some images without */
                        if (img.samplesperpixel > 3)
                        {
                            /* correct info about alpha channel */
                            img.alpha = ExtraSample.ASSOCALPHA;
                        }
                        break;

                    case ExtraSample.ASSOCALPHA:
                        /* data is pre-multiplied */
                    case ExtraSample.UNASSALPHA:
                        /* data is not pre-multiplied */
                        img.alpha = (ExtraSample)sampleinfo[0];
                        break;
                }
            }

            if (Tiff.DEFAULT_EXTRASAMPLE_AS_ALPHA)
            {
                result = tif.GetField(TiffTag.PHOTOMETRIC);
                if (result == null)
                    img.photometric = Photometric.MINISWHITE;

                if (extrasamples == 0 && img.samplesperpixel == 4 && img.photometric == Photometric.RGB)
                {
                    img.alpha = ExtraSample.ASSOCALPHA;
                    extrasamples = 1;
                }
            }

            int colorchannels = img.samplesperpixel - extrasamples;
            
            result = tif.GetFieldDefaulted(TiffTag.COMPRESSION);
            Compression compress = (Compression)result[0].ToInt();

            result = tif.GetFieldDefaulted(TiffTag.PLANARCONFIG);
            PlanarConfig planarconfig = (PlanarConfig)result[0].ToShort();

            result = tif.GetField(TiffTag.PHOTOMETRIC);
            if (result == null)
            {
                switch (colorchannels)
                {
                    case 1:
                        if (img.isCCITTCompression())
                            img.photometric = Photometric.MINISWHITE;
                        else
                            img.photometric = Photometric.MINISBLACK;
                        break;

                    case 3:
                        img.photometric = Photometric.RGB;
                        break;

                    default:
                        emsg = string.Format(CultureInfo.InvariantCulture, "Missing needed {0} tag", photoTag);
                        return null;
                }
            }
            else
                img.photometric = (Photometric)result[0].ToInt();

            switch (img.photometric)
            {
                case Photometric.PALETTE:
                    result = tif.GetField(TiffTag.COLORMAP);
                    if (result == null)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, "Missing required \"Colormap\" tag");
                        return null;
                    }

                    short[] red_orig = result[0].ToShortArray();
                    short[] green_orig = result[1].ToShortArray();
                    short[] blue_orig = result[2].ToShortArray();

                    /* copy the colormaps so we can modify them */
                    int n_color = (1 << img.bitspersample);
                    img.redcmap = new short [n_color];
                    img.greencmap = new short [n_color];
                    img.bluecmap = new short [n_color];

                    Array.Copy(red_orig, img.redcmap, n_color);
                    Array.Copy(green_orig, img.greencmap, n_color);
                    Array.Copy(blue_orig, img.bluecmap, n_color);

                    if (planarconfig == PlanarConfig.CONTIG && 
                        img.samplesperpixel != 1 && img.bitspersample < 8)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle contiguous data with {0}={1}, and {2}={3} and Bits/Sample={4}",
                            photoTag, img.photometric, "Samples/pixel", img.samplesperpixel, img.bitspersample);
                        return null;
                    }
                    break;

                case Photometric.MINISWHITE:
                case Photometric.MINISBLACK:
                    if (planarconfig == PlanarConfig.CONTIG && 
                        img.samplesperpixel != 1 && img.bitspersample < 8)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle contiguous data with {0}={1}, and {2}={3} and Bits/Sample={4}",
                            photoTag, img.photometric, "Samples/pixel", img.samplesperpixel, img.bitspersample);
                        return null;
                    }
                    break;

                case Photometric.YCBCR:
                    /* It would probably be nice to have a reality check here. */
                    if (planarconfig == PlanarConfig.CONTIG)
                    {
                        /* can rely on libjpeg to convert to RGB */
                        /* XXX should restore current state on exit */
                        switch (compress)
                        {
                            case Compression.JPEG:
                                /*
                                * TODO: when complete tests verify complete desubsampling
                                * and YCbCr handling, remove use of JPEGCOLORMODE in
                                * favor of native handling
                                */
                                tif.SetField(TiffTag.JPEGCOLORMODE, JpegColorMode.RGB);
                                img.photometric = Photometric.RGB;
                                break;

                            default:
                                /* do nothing */
                                break;
                        }
                    }

                    /*
                    * TODO: if at all meaningful and useful, make more complete
                    * support check here, or better still, refactor to let supporting
                    * code decide whether there is support and what meaningfull
                    * error to return
                    */
                    break;

                case Photometric.RGB:
                    if (colorchannels < 3)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, 
                            "Sorry, can not handle RGB image with {0}={1}", "Color channels", colorchannels);
                        return null;
                    }
                    break;

                case Photometric.SEPARATED:
                    result = tif.GetFieldDefaulted(TiffTag.INKSET);
                    InkSet inkset = (InkSet)result[0].ToByte();

                    if (inkset != InkSet.CMYK)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, 
                            "Sorry, can not handle separated image with {0}={1}", "InkSet", inkset);
                        return null;
                    }

                    if (img.samplesperpixel < 4)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, 
                            "Sorry, can not handle separated image with {0}={1}", "Samples/pixel", img.samplesperpixel);
                        return null;
                    }
                    break;

                case Photometric.LOGL:
                    if (compress != Compression.SGILOG)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, 
                            "Sorry, LogL data must have {0}={1}", "Compression", Compression.SGILOG);
                        return null;
                    }

                    tif.SetField(TiffTag.SGILOGDATAFMT, SGILogDataFmt.FMT8BIT);
                    img.photometric = Photometric.MINISBLACK; /* little white lie */
                    img.bitspersample = 8;
                    break;

                case Photometric.LOGLUV:
                    if (compress != Compression.SGILOG && compress != Compression.SGILOG24)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, 
                            "Sorry, LogLuv data must have {0}={1} or {2}", "Compression", Compression.SGILOG, Compression.SGILOG24);
                        return null;
                    }

                    if (planarconfig != PlanarConfig.CONTIG)
                    {
                        emsg = string.Format(CultureInfo.InvariantCulture, 
                            "Sorry, can not handle LogLuv images with {0}={1}", "Planarconfiguration", planarconfig);
                        return null;
                    }

                    tif.SetField(TiffTag.SGILOGDATAFMT, SGILogDataFmt.FMT8BIT);
                    img.photometric = Photometric.RGB; /* little white lie */
                    img.bitspersample = 8;
                    break;

                case Photometric.CIELAB:
                    break;
                
                default:
                    emsg = string.Format(CultureInfo.InvariantCulture, 
                        "Sorry, can not handle image with {0}={1}", photoTag, img.photometric);
                    return null;
            }

            img.Map = null;
            img.BWmap = null;
            img.PALmap = null;
            img.ycbcr = null;
            img.cielab = null;

            result = tif.GetField(TiffTag.IMAGEWIDTH);
            img.width = result[0].ToInt();

            result = tif.GetField(TiffTag.IMAGELENGTH);
            img.height = result[0].ToInt();

            result = tif.GetFieldDefaulted(TiffTag.ORIENTATION);
            img.orientation = (Orientation)result[0].ToByte();
            
            img.isContig = !(planarconfig == PlanarConfig.SEPARATE && colorchannels > 1);
            if (img.isContig)
            {
                if (!img.pickContigCase())
                {
                    emsg = "Sorry, can not handle image";
                    return null;
                }
            }
            else
            {
                if (!img.pickSeparateCase())
                {
                    emsg = "Sorry, can not handle image";
                    return null;
                }
            }

            return img;
        }

        public bool Get(int[] raster, int offset, int w, int h)
        {
            if (getRoutineInstance == null)
            {
                Tiff.ErrorExt(tif, tif.m_clientdata, tif.FileName(), "No \"get\" routine setup");
                return false;
            }
            
            return getRoutineInstance(this, raster, offset, w, h);
        }

        protected TiffRGBAImage()
        {
        }

        private static int PACK(int r, int g, int b)
        {
            return (r | (g << 8) | (b << 16) | A1);
        }

        private static int PACK4(int r, int g, int b, int a)
        {
            return (r | (g << 8) | (b << 16) | (a << 24));
        }

        private static int W2B(short v)
        {
            return ((v >> 8) & 0xff);
        }

        private static int PACKW(short r, short g, short b)
        {
            return (W2B(r) | (W2B(g) << 8) | (W2B(b) << 16) | A1);
        }

        private static int PACKW4(short r, short g, short b, short a)
        {
            return (W2B(r) | (W2B(g) << 8) | (W2B(b) << 16) | (W2B(a) << 24));
        }

        /*
        * Palette images with <= 8 bits/sample are handled
        * with a table to avoid lots of shifts and masks.  The table
        * is setup so that put*cmaptile (below) can retrieve 8/bitspersample
        * pixel values simply by indexing into the table with one
        * number.
        */
        private void CMAP(int x, int i, ref int j)
        {
            PALmap[i][j++] = PACK(redcmap[x] & 0xff, greencmap[x] & 0xff, bluecmap[x] & 0xff);
        }

        /*
        * Greyscale images with less than 8 bits/sample are handled
        * with a table to avoid lots of shifts and masks.  The table
        * is setup so that put*bwtile (below) can retrieve 8/bitspersample
        * pixel values simply by indexing into the table with one
        * number.
        */
        private void GREY(int x, int i, ref int j)
        {
            int c = Map[x];
            BWmap[i][j++] = PACK(c, c, c);
        }

        /*
        * Get an tile-organized image that has
        *  PlanarConfiguration contiguous if SamplesPerPixel > 1
        * or
        *  SamplesPerPixel == 1
        */
        private static bool gtTileContig(TiffRGBAImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            tileContigRoutine put = img.contig;

            byte[] buf = new byte [tif.TileSize()];

            FieldValue[] result = tif.GetField(TiffTag.TILEWIDTH);
            int tw = result[0].ToInt();

            result = tif.GetField(TiffTag.TILELENGTH);
            int th = result[0].ToInt();

            int flip = img.setorientation();
            int y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(tw + w);
            }
            else
            {
                y = 0;
                toskew = -(tw - w);
            }

            bool ret = true;
            for (int row = 0; row < h; )
            {
                int rowstoread = th - (row + img.row_offset) % th;
                int nrow = (row + rowstoread > h ? h - row: rowstoread);
                for (int col = 0; col < w; col += tw)
                {
                    if (tif.ReadTile(buf, 0, col + img.col_offset, row + img.row_offset, 0, 0) < 0 && img.stoponerr)
                    {
                        ret = false;
                        break;
                    }

                    int pos = ((row + img.row_offset) % th) * tif.TileRowSize();

                    if (col + tw > w)
                    {
                        /*
                        * Tile is clipped horizontally.  Calculate
                        * visible portion and skewing factors.
                        */
                        int npix = w - col;
                        int fromskew = tw - npix;
                        put(img, raster, offset + y * w + col, col, y, npix, nrow, fromskew, toskew + fromskew, buf, pos);
                    }
                    else
                    {
                        put(img, raster, offset + y * w + col, col, y, tw, nrow, 0, toskew, buf, pos);
                    }
                }

                y += ((flip & FLIP_VERTICALLY) != 0 ?  -nrow : nrow);
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (int line = 0; line < h; line++)
                {
                    int left = offset + line * w;
                    int right = left + w - 1;

                    while (left < right)
                    {
                        int temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            return ret;
        }

        /*
        * Get an tile-organized image that has
        *   SamplesPerPixel > 1
        *   PlanarConfiguration separated
        * We assume that all such images are RGB.
        */
        private static bool gtTileSeparate(TiffRGBAImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            tileSeparateRoutine put = img.separate;

            int tilesize = tif.TileSize();
            byte[] buf = new byte [(img.alpha != 0 ? 4 : 3) * tilesize];

            int p0 = 0;
            int p1 = p0 + tilesize;
            int p2 = p1 + tilesize;
            int pa = (img.alpha != 0 ? (p2 + tilesize) : -1);
            
            FieldValue[] result = tif.GetField(TiffTag.TILEWIDTH);
            int tw = result[0].ToInt();

            result = tif.GetField(TiffTag.TILELENGTH);
            int th = result[0].ToInt();

            int flip = img.setorientation();
            int y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(tw + w);
            }
            else
            {
                y = 0;
                toskew = -(tw - w);
            }

            bool ret = true;
            for (int row = 0; row < h; )
            {
                int rowstoread = th - (row + img.row_offset) % th;
                int nrow = (row + rowstoread > h ? h - row : rowstoread);
                for (int col = 0; col < w; col += tw)
                {
                    if (tif.ReadTile(buf, p0, col + img.col_offset, row + img.row_offset, 0, 0) < 0 && img.stoponerr)
                    {
                        ret = false;
                        break;
                    }

                    if (tif.ReadTile(buf, p1, col + img.col_offset, row + img.row_offset, 0, 1) < 0 && img.stoponerr)
                    {
                        ret = false;
                        break;
                    }
                    
                    if (tif.ReadTile(buf, p2, col + img.col_offset, row + img.row_offset, 0, 2) < 0 && img.stoponerr)
                    {
                        ret = false;
                        break;
                    }
                    
                    if (img.alpha != 0)
                    {
                        if (tif.ReadTile(buf, pa, col + img.col_offset, row + img.row_offset, 0, 3) < 0 && img.stoponerr)
                        {
                            ret = false;
                            break;
                        }
                    }

                    int pos = ((row + img.row_offset) % th) * tif.TileRowSize();

                    if (col + tw > w)
                    {
                        /*
                        * Tile is clipped horizontally.  Calculate
                        * visible portion and skewing factors.
                        */
                        int npix = w - col;
                        int fromskew = tw - npix;
                        put(img, raster, offset + y * w + col, col, y, npix, nrow, fromskew, toskew + fromskew, buf, p0 + pos, p1 + pos, p2 + pos, img.alpha != 0 ? (pa + pos) : -1);
                    }
                    else
                    {
                        put(img, raster, offset + y * w + col, col, y, tw, nrow, 0, toskew, buf, p0 + pos, p1 + pos, p2 + pos, img.alpha != 0 ? (pa + pos) : -1);
                    }
                }

                y += ((flip & FLIP_VERTICALLY) != 0 ? -nrow : nrow);
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (int line = 0; line < h; line++)
                {
                    int left = offset + line * w;
                    int right = left + w - 1;

                    while (left < right)
                    {
                        int temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Get a strip-organized image that has 
        /// PlanarConfiguration contiguous if SamplesPerPixel > 1
        ///  or
        /// SamplesPerPixel == 1
        /// </summary>
        private static bool gtStripContig(TiffRGBAImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            tileContigRoutine put = img.contig;

            byte[] buf = new byte [tif.StripSize()];

            int flip = img.setorientation();
            int y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(w + w);
            }
            else
            {
                y = 0;
                toskew = -(w - w);
            }

            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();
            if (rowsperstrip == -1)
            {
                // San Chen <bigsan.chen@gmail.com>
                // HACK: should be UInt32.MaxValue
                rowsperstrip = Int32.MaxValue;
            }

            result = tif.GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
            short subsamplingver = result[1].ToShort();

            int scanline = tif.newScanlineSize();
            int fromskew = (w < img.width ? img.width - w : 0);
            bool ret = true;

            for (int row = 0; row < h; )
            {
                int rowstoread = rowsperstrip - (row + img.row_offset) % rowsperstrip;
                int nrow = (row + rowstoread > h ? h - row : rowstoread);
                int nrowsub = nrow;
                if ((nrowsub % subsamplingver) != 0)
                    nrowsub += subsamplingver - nrowsub % subsamplingver;

                if (tif.ReadEncodedStrip(tif.ComputeStrip(row + img.row_offset, 0), buf, 0, ((row + img.row_offset) % rowsperstrip + nrowsub) * scanline) < 0 && img.stoponerr)
                {
                    ret = false;
                    break;
                }

                int pos = ((row + img.row_offset) % rowsperstrip) * scanline;
                put(img, raster, offset + y * w, 0, y, w, nrow, fromskew, toskew, buf, pos);
                y += (flip & FLIP_VERTICALLY) != 0 ? -nrow : nrow;
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (int line = 0; line < h; line++)
                {
                    int left = offset + line * w;
                    int right = left + w - 1;

                    while (left < right)
                    {
                        int temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            return ret;
        }

        /*
        * Get a strip-organized image with
        *   SamplesPerPixel > 1
        *   PlanarConfiguration separated
        * We assume that all such images are RGB.
        */
        private static bool gtStripSeparate(TiffRGBAImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            tileSeparateRoutine put = img.separate;

            int stripsize = tif.StripSize();
            byte[] buf = new byte [(img.alpha != 0 ? 4 : 3) * stripsize];

            int p0 = 0;
            int p1 = p0 + stripsize;
            int p2 = p1 + stripsize;
            int pa = p2 + stripsize;
            pa = (img.alpha != 0 ? (p2 + stripsize) : -1);

            int flip = img.setorientation();
            int y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(w + w);
            }
            else
            {
                y = 0;
                toskew = -(w - w);
            }

            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();

            int scanline = tif.ScanlineSize();
            int fromskew = (w < img.width ? img.width - w : 0);
            bool ret = true;
            for (int row = 0; row < h; )
            {
                int rowstoread = rowsperstrip - (row + img.row_offset) % rowsperstrip;
                int nrow = (row + rowstoread > h ? h - row : rowstoread);
                int offset_row = row + img.row_offset;
                
                if (tif.ReadEncodedStrip(tif.ComputeStrip(offset_row, 0), buf, p0, ((row + img.row_offset) % rowsperstrip + nrow) * scanline) < 0 && img.stoponerr)
                {
                    ret = false;
                    break;
                }
                
                if (tif.ReadEncodedStrip(tif.ComputeStrip(offset_row, 1), buf, p1, ((row + img.row_offset) % rowsperstrip + nrow) * scanline) < 0 && img.stoponerr)
                {
                    ret = false;
                    break;
                }
                
                if (tif.ReadEncodedStrip(tif.ComputeStrip(offset_row, 2), buf, p2, ((row + img.row_offset) % rowsperstrip + nrow) * scanline) < 0 && img.stoponerr)
                {
                    ret = false;
                    break;
                }
                
                if (img.alpha != 0)
                {
                    if ((tif.ReadEncodedStrip(tif.ComputeStrip(offset_row, 3), buf, pa, ((row + img.row_offset) % rowsperstrip + nrow) * scanline) < 0 && img.stoponerr))
                    {
                        ret = false;
                        break;
                    }
                }
                
                int pos = ((row + img.row_offset) % rowsperstrip) * scanline;
                put(img, raster, offset + y * w, 0, y, w, nrow, fromskew, toskew, buf, p0 + pos, p1 + pos, p2 + pos, img.alpha != 0 ? (pa + pos) : -1);
                y += (flip & FLIP_VERTICALLY) != 0 ? -nrow : nrow;
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (int line = 0; line < h; line++)
                {
                    int left = offset + line * w;
                    int right = left + w - 1;

                    while (left < right)
                    {
                        int temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            return ret;
        }

        private bool isCCITTCompression()
        {
            FieldValue[] result = tif.GetField(TiffTag.COMPRESSION);
            Compression compress = (Compression)result[0].ToInt();

            return (compress == Compression.CCITTFAX3 || 
                compress == Compression.CCITTFAX4 || 
                compress == Compression.CCITTRLE || 
                compress == Compression.CCITTRLEW);
        }

        private int setorientation()
        {
            switch (orientation)
            {
                case Orientation.TOPLEFT:
                case Orientation.LEFTTOP:
                    if (req_orientation == Orientation.TOPRIGHT || req_orientation == Orientation.RIGHTTOP)
                        return FLIP_HORIZONTALLY;
                    else if (req_orientation == Orientation.BOTRIGHT || req_orientation == Orientation.RIGHTBOT)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;
                    else if (req_orientation == Orientation.BOTLEFT || req_orientation == Orientation.LEFTBOT)
                        return FLIP_VERTICALLY;

                    return 0;

                case Orientation.TOPRIGHT:
                case Orientation.RIGHTTOP:
                    if (req_orientation == Orientation.TOPLEFT || req_orientation == Orientation.LEFTTOP)
                        return FLIP_HORIZONTALLY;
                    else if (req_orientation == Orientation.BOTRIGHT || req_orientation == Orientation.RIGHTBOT)
                        return FLIP_VERTICALLY;
                    else if (req_orientation == Orientation.BOTLEFT || req_orientation == Orientation.LEFTBOT)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;

                    return 0;

                case Orientation.BOTRIGHT:
                case Orientation.RIGHTBOT:
                    if (req_orientation == Orientation.TOPLEFT || req_orientation == Orientation.LEFTTOP)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;
                    else if (req_orientation == Orientation.TOPRIGHT || req_orientation == Orientation.RIGHTTOP)
                        return FLIP_VERTICALLY;
                    else if (req_orientation == Orientation.BOTLEFT || req_orientation == Orientation.LEFTBOT)
                        return FLIP_HORIZONTALLY;

                    return 0;

                case Orientation.BOTLEFT:
                case Orientation.LEFTBOT:
                    if (req_orientation == Orientation.TOPLEFT || req_orientation == Orientation.LEFTTOP)
                        return FLIP_VERTICALLY;
                    else if (req_orientation == Orientation.TOPRIGHT || req_orientation == Orientation.RIGHTTOP)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;
                    else if (req_orientation == Orientation.BOTRIGHT || req_orientation == Orientation.RIGHTBOT)
                        return FLIP_HORIZONTALLY;

                    return 0;
            }

            return 0;
        }

        /*
        * Select the appropriate conversion routine for packed data.
        */
        private bool pickContigCase()
        {
            getRoutineInstance = tif.IsTiled() ? new getRoutine(gtTileContig) : new getRoutine(gtStripContig);
            contig = null;

            switch (photometric)
            {
                case Photometric.RGB:
                    switch (bitspersample)
                    {
                        case 8:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                contig = putRGBAAcontig8bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                contig = putRGBUAcontig8bittile;
                            else
                                contig = putRGBcontig8bittile;
                            break;

                        case 16:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                contig = putRGBAAcontig16bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                contig = putRGBUAcontig16bittile;
                            else
                                contig = putRGBcontig16bittile;
                            break;
                    }
                    break;

                case Photometric.SEPARATED:
                    if (buildMap())
                    {
                        if (bitspersample == 8)
                        {
                            if (Map == null)
                                contig = putRGBcontig8bitCMYKtile;
                            else
                                contig = putRGBcontig8bitCMYKMaptile;
                        }
                    }
                    break;

                case Photometric.PALETTE:
                    if (buildMap())
                    {
                        switch (bitspersample)
                        {
                            case 8:
                                contig = put8bitcmaptile;
                                break;
                            case 4:
                                contig = put4bitcmaptile;
                                break;
                            case 2:
                                contig = put2bitcmaptile;
                                break;
                            case 1:
                                contig = put1bitcmaptile;
                                break;
                        }
                    }
                    break;

                case Photometric.MINISWHITE:
                case Photometric.MINISBLACK:
                    if (buildMap())
                    {
                        switch (bitspersample)
                        {
                            case 16:
                                contig = put16bitbwtile;
                                break;
                            case 8:
                                contig = putgreytile;
                                break;
                            case 4:
                                contig = put4bitbwtile;
                                break;
                            case 2:
                                contig = put2bitbwtile;
                                break;
                            case 1:
                                contig = put1bitbwtile;
                                break;
                        }
                    }
                    break;

                case Photometric.YCBCR:
                    if (bitspersample == 8)
                    {
                        if (initYCbCrConversion())
                        {
                            /*
                            * The 6.0 spec says that subsampling must be
                            * one of 1, 2, or 4, and that vertical subsampling
                            * must always be <= horizontal subsampling; so
                            * there are only a few possibilities and we just
                            * enumerate the cases.
                            * Joris: added support for the [1,2] case, nonetheless, to accommodate
                            * some OJPEG files
                            */
                            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
                            short SubsamplingHor = result[0].ToShort();
                            short SubsamplingVer = result[1].ToShort();

                            switch (((ushort)SubsamplingHor << 4) | (ushort)SubsamplingVer)
                            {
                                case 0x44:
                                    contig = putcontig8bitYCbCr44tile;
                                    break;
                                case 0x42:
                                    contig = putcontig8bitYCbCr42tile;
                                    break;
                                case 0x41:
                                    contig = putcontig8bitYCbCr41tile;
                                    break;
                                case 0x22:
                                    contig = putcontig8bitYCbCr22tile;
                                    break;
                                case 0x21:
                                    contig = putcontig8bitYCbCr21tile;
                                    break;
                                case 0x12:
                                    contig = putcontig8bitYCbCr12tile;
                                    break;
                                case 0x11:
                                    contig = putcontig8bitYCbCr11tile;
                                    break;
                            }
                        }
                    }
                    break;

                case Photometric.CIELAB:
                    if (buildMap())
                    {
                        if (bitspersample == 8)
                            contig = initCIELabConversion();
                    }
                    break;
            }

            return (contig != null);
        }

        /*
        * Select the appropriate conversion routine for unpacked data.
        *
        * NB: we assume that unpacked single channel data is directed
        *   to the "packed routines.
        */
        private bool pickSeparateCase()
        {
            getRoutineInstance = tif.IsTiled() ? new getRoutine(gtTileSeparate) : new getRoutine(gtStripSeparate);
            separate = null;

            switch (photometric)
            {
                case Photometric.RGB:
                    switch (bitspersample)
                    {
                        case 8:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                separate = putRGBAAseparate8bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                separate = putRGBUAseparate8bittile;
                            else
                                separate = putRGBseparate8bittile;
                            break;

                        case 16:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                separate = putRGBAAseparate16bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                separate = putRGBUAseparate16bittile;
                            else
                                separate = putRGBseparate16bittile;
                            break;
                    }
                    break;

                case Photometric.YCBCR:
                    if ((bitspersample == 8) && (samplesperpixel == 3))
                    {
                        if (initYCbCrConversion())
                        {
                            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
                            short hs = result[0].ToShort();
                            short vs = result[0].ToShort();

                            switch (((ushort)hs << 4) | (ushort)vs)
                            {
                                case 0x11:
                                    separate = putseparate8bitYCbCr11tile;
                                    break;
                                /* TODO: add other cases here */
                            }
                        }
                    }
                    break;
            }

            return (separate != null);
        }

        /*
        * The following routines move decoded data returned
        * from the TIFF library into rasters filled with packed
        * ABGR pixels (i.e. suitable for passing to lrecwrite.)
        *
        * The routines have been created according to the most
        * important cases and optimized.  pickTileContigCase and
        * pickTileSeparateCase analyze the parameters and select
        * the appropriate "put" routine to use.
        */

        /*
        * 8-bit palette => colormap/RGB
        */
        private static void put8bitcmaptile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] PALmap = img.PALmap;
            int samplesperpixel = img.samplesperpixel;

            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    cp[cpPos] = PALmap[pp[ppPos]][0];
                    cpPos++;
                    ppPos += samplesperpixel;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 4-bit palette => colormap/RGB
        /// </summary>
        private static void put4bitcmaptile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] PALmap = img.PALmap;
            fromskew /= 2;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int[] bw = null;

                int _x;
                for (_x = w; _x >= 2; _x -= 2)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 2; rc++)
                    {
                        cp[cpPos] = bw[rc];
                        cpPos++;
                    }
                }

                if (_x != 0)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    cp[cpPos] = bw[0];
                    cpPos++;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 2-bit palette => colormap/RGB
        /// </summary>
        private static void put2bitcmaptile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] PALmap = img.PALmap;
            fromskew /= 4;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int[] bw = null;

                int _x;
                for (_x = w; _x >= 4; _x -= 4)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 4; rc++)
                    {
                        cp[cpPos] = bw[rc];
                        cpPos++;
                    }
                }

                if (_x > 0)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 3 && _x > 0)
                    {
                        for (int i = 0; i < _x; i++)
                        {
                            cp[cpPos] = bw[i];
                            cpPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 1-bit palette => colormap/RGB
        /// </summary>
        private static void put1bitcmaptile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] PALmap = img.PALmap;
            fromskew /= 8;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int[] bw = null;
                int bwPos = 0;

                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    for (int rc = 0; rc < 8; rc++)
                    {
                        cp[cpPos] = bw[bwPos];
                        cpPos++;
                        bwPos++;
                    }
                }

                if (_x > 0)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = 0; i < _x; i++)
                        {
                            cp[cpPos] = bw[i];
                            cpPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit greyscale => colormap/RGB
        */
        private static void putgreytile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            int[][] BWmap = img.BWmap;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    cp[cpPos] = BWmap[pp[ppPos]][0];
                    cpPos++;
                    ppPos += samplesperpixel;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 16-bit greyscale => colormap/RGB
        /// </summary>
        private static void put16bitbwtile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            int[][] BWmap = img.BWmap;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length - ppPos);
                int wpPos = 0;

                for (x = w; x-- > 0; )
                {
                    // use high order byte of 16bit value

                    cp[cpPos] = BWmap[(wp[wpPos] & 0xffff) >> 8][0];
                    cpPos++;
                    ppPos += 2 * samplesperpixel;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 1-bit bilevel => colormap/RGB
        /// </summary>
        private static void put1bitbwtile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] BWmap = img.BWmap;
            fromskew /= 8;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int[] bw = null;

                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    for (int rc = 0; rc < 8; rc++)
                    {
                        cp[cpPos] = bw[rc];
                        cpPos++;
                    }
                }

                if (_x > 0)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = 0; i < _x; i++)
                        {
                            cp[cpPos] = bw[i];
                            cpPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 2-bit greyscale => colormap/RGB
        /// </summary>
        private static void put2bitbwtile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] BWmap = img.BWmap;
            fromskew /= 4;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int[] bw = null;

                int _x;
                for (_x = w; _x >= 4; _x -= 4)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 4; rc++)
                    {
                        cp[cpPos] = bw[rc];
                        cpPos++;
                    }
                }

                if (_x > 0)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 3 && _x > 0)
                    {
                        for (int i = 0; i < _x; i++)
                        {
                            cp[cpPos] = bw[i];
                            cpPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 4-bit greyscale => colormap/RGB
        /// </summary>
        private static void put4bitbwtile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] BWmap = img.BWmap;
            fromskew /= 2;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int[] bw = null;

                int _x;
                for (_x = w; _x >= 2; _x -= 2)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 2; rc++)
                    {
                        cp[cpPos] = bw[rc];
                        cpPos++;
                    }
                }

                if (_x != 0)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    cp[cpPos] = bw[0];
                    cpPos++;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit packed samples, no Map => RGB
        */
        private static void putRGBcontig8bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            fromskew *= samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    for (int rc = 0; rc < 8; rc++)
                    {
                        cp[cpPos] = PACK(pp[ppPos], pp[ppPos + 1], pp[ppPos + 2]);
                        cpPos++;
                        ppPos += samplesperpixel;
                    }
                }

                if (_x > 0)
                {
                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = PACK(pp[ppPos], pp[ppPos + 1], pp[ppPos + 2]);
                            cpPos++;
                            ppPos += samplesperpixel;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit packed samples => RGBA w/ associated alpha
        * (known to have Map == null)
        */
        private static void putRGBAAcontig8bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            fromskew *= samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    for (int rc = 0; rc < 8; rc++)
                    {
                        cp[cpPos] = PACK4(pp[ppPos], pp[ppPos + 1], pp[ppPos + 2], pp[ppPos + 3]);
                        cpPos++;
                        ppPos += samplesperpixel;
                    }
                }

                if (_x > 0)
                {
                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = PACK4(pp[ppPos], pp[ppPos + 1], pp[ppPos + 2], pp[ppPos + 3]);
                            cpPos++;
                            ppPos += samplesperpixel;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit packed samples => RGBA w/ unassociated alpha
        * (known to have Map == null)
        */
        private static void putRGBUAcontig8bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            fromskew *= samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    int a = pp[ppPos + 3];
                    int r = (pp[ppPos] * a + 127) / 255;
                    int g = (pp[ppPos + 1] * a + 127) / 255;
                    int b = (pp[ppPos + 2] * a + 127) / 255;
                    cp[cpPos] = PACK4(r, g, b, a);
                    cpPos++;
                    ppPos += samplesperpixel;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 16-bit packed samples => RGB
        */
        private static void putRGBcontig16bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            fromskew *= samplesperpixel;

            short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length);
            int wpPos = 0;

            while (h-- > 0)
            {
                for (x = w; x-- > 0;)
                {
                    cp[cpPos] = PACKW(wp[wpPos], wp[wpPos + 1], wp[wpPos + 2]);
                    cpPos++;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                wpPos += fromskew;
            }
        }

        /*
        * 16-bit packed samples => RGBA w/ associated alpha
        * (known to have Map == null)
        */
        private static void putRGBAAcontig16bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length);
            int wpPos = 0;

            fromskew *= samplesperpixel;
            while (h-- > 0)
            {
                for (x = w; x-- > 0;)
                {
                    cp[cpPos] = PACKW4(wp[wpPos], wp[wpPos + 1], wp[wpPos + 2], wp[wpPos + 3]);
                    cpPos++;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                wpPos += fromskew;
            }
        }

        /*
        * 16-bit packed samples => RGBA w/ unassociated alpha
        * (known to have Map == null)
        */
        private static void putRGBUAcontig16bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            fromskew *= samplesperpixel;
            
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length);
            int wpPos = 0;
            
            while (h-- > 0)
            {
                for (x = w; x-- > 0;)
                {
                    int a = W2B(wp[wpPos + 3]);
                    int r = (W2B(wp[wpPos]) * a + 127) / 255;
                    int g = (W2B(wp[wpPos + 1]) * a + 127) / 255;
                    int b = (W2B(wp[wpPos + 2]) * a + 127) / 255;
                    cp[cpPos] = PACK4(r, g, b, a);
                    cpPos++;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                wpPos += fromskew;
            }
        }

        /*
        * 8-bit packed CMYK samples w/o Map => RGB
        *
        * NB: The conversion of CMYK->RGB is *very* crude.
        */
        private static void putRGBcontig8bitCMYKtile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            fromskew *= samplesperpixel;

            int cpPos = cpOffset;
            int ppPos = ppOffset;

            while (h-- > 0)
            {
                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    for (int rc = 0; rc < 8; rc++)
                    {
                        short k = (short)(255 - pp[ppPos + 3]);
                        short r = (short)((k * (255 - pp[ppPos])) / 255);
                        short g = (short)((k * (255 - pp[ppPos + 1])) / 255);
                        short b = (short)((k * (255 - pp[ppPos + 2])) / 255);
                        cp[cpPos] = PACK(r, g, b);
                        cpPos++;
                        ppPos += samplesperpixel;
                    }
                }

                if (_x > 0)
                {
                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            short k = (short)(255 - pp[ppPos + 3]);
                            short r = (short)((k * (255 - pp[ppPos])) / 255);
                            short g = (short)((k * (255 - pp[ppPos + 1])) / 255);
                            short b = (short)((k * (255 - pp[ppPos + 2])) / 255);
                            cp[cpPos] = PACK(r, g, b);
                            cpPos++;
                            ppPos += samplesperpixel;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit packed CMYK samples w/Map => RGB
        *
        * NB: The conversion of CMYK->RGB is *very* crude.
        */
        private static void putRGBcontig8bitCMYKMaptile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            byte[] Map = img.Map;
            fromskew *= samplesperpixel;

            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    short k = (short)(255 - pp[ppPos + 3]);
                    short r = (short)((k * (255 - pp[ppPos])) / 255);
                    short g = (short)((k * (255 - pp[ppPos + 1])) / 255);
                    short b = (short)((k * (255 - pp[ppPos + 2])) / 255);
                    cp[cpPos] = PACK(Map[r], Map[g], Map[b]);
                    cpPos++;
                    ppPos += samplesperpixel;
                }

                ppPos += fromskew;
                cpPos += toskew;
            }
        }

        /*
        * 8-bit unpacked samples => RGB
        */
        private static void putRGBseparate8bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            int cpPos = cpOffset;
            int rPos = rOffset;
            int gPos = gOffset;
            int bPos = bOffset;

            while (h-- > 0)
            {
                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    for (int rc = 0; rc < 8; rc++)
                    {
                        cp[cpPos] = PACK(rgba[rPos], rgba[gPos], rgba[bPos]);
                        cpPos++;
                        rPos++;
                        gPos++;
                        bPos++;
                    }
                }

                if (_x > 0)
                {
                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = PACK(rgba[rPos], rgba[gPos], rgba[bPos]);
                            cpPos++;
                            rPos++;
                            gPos++;
                            bPos++;
                        }
                    }
                }

                rPos += fromskew;
                gPos += fromskew;
                bPos += fromskew;
                cpPos += toskew;
            }
        }

        /*
        * 8-bit unpacked samples => RGBA w/ associated alpha
        */
        private static void putRGBAAseparate8bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            int cpPos = cpOffset;
            int rPos = rOffset;
            int gPos = gOffset;
            int bPos = bOffset;
            int aPos = aOffset;
            while (h-- > 0)
            {
                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    for (int rc = 0; rc < 8; rc++)
                    {
                        cp[cpPos] = PACK4(rgba[rPos], rgba[gPos], rgba[bPos], rgba[aPos]);
                        cpPos++;
                        rPos++;
                        gPos++;
                        bPos++;
                        aPos++;
                    }
                }

                if (_x > 0)
                {
                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = PACK4(rgba[rPos], rgba[gPos], rgba[bPos], rgba[aPos]);
                            cpPos++;
                            rPos++;
                            gPos++;
                            bPos++;
                            aPos++;
                        }
                    }
                }

                rPos += fromskew;
                gPos += fromskew;
                bPos += fromskew;
                aPos += fromskew;

                cpPos += toskew;
            }
        }

        /*
        * 8-bit unpacked samples => RGBA w/ unassociated alpha
        */
        private static void putRGBUAseparate8bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            int cpPos = cpOffset;
            int rPos = rOffset;
            int gPos = gOffset;
            int bPos = bOffset;
            int aPos = aOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    int av = rgba[aPos];
                    int rv = (rgba[rPos] * av + 127) / 255;
                    int gv = (rgba[gPos] * av + 127) / 255;
                    int bv = (rgba[bPos] * av + 127) / 255;
                    cp[cpPos] = PACK4(rv, gv, bv, av);
                    cpPos++;
                    rPos++;
                    gPos++;
                    bPos++;
                    aPos++;
                }

                rPos += fromskew;
                gPos += fromskew;
                bPos += fromskew;
                aPos += fromskew;

                cpPos += toskew;
            }
        }

        /*
        * 16-bit unpacked samples => RGB
        */
        private static void putRGBseparate16bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            short[] wrgba = Tiff.ByteArrayToShorts(rgba, 0, rgba.Length);
    
            int wrPos = rOffset / sizeof(short);
            int wgPos = gOffset / sizeof(short);
            int wbPos = bOffset / sizeof(short);
            int cpPos = cpOffset;

            while (h-- > 0)
            {
                for (x = 0; x < w; x++)
                {
                    cp[cpPos] = PACKW(wrgba[wrPos], wrgba[wgPos], wrgba[wbPos]);
                    cpPos++;
                    wrPos++;
                    wgPos++;
                    wbPos++;
                }

                wrPos += fromskew;
                wgPos += fromskew;
                wbPos += fromskew;
                cpPos += toskew;
            }
        }

        /*
        * 16-bit unpacked samples => RGBA w/ associated alpha
        */
        private static void putRGBAAseparate16bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            short[] wrgba = Tiff.ByteArrayToShorts(rgba, 0, rgba.Length);
    
            int wrPos = rOffset / sizeof(short);
            int wgPos = gOffset / sizeof(short);
            int wbPos = bOffset / sizeof(short);
            int waPos = aOffset / sizeof(short);
            int cpPos = cpOffset;

            while (h-- > 0)
            {
                for (x = 0; x < w; x++)
                {
                    cp[cpPos] = PACKW4(wrgba[wrPos], wrgba[wgPos], wrgba[wbPos], wrgba[waPos]);
                    cpPos++;
                    wrPos++;
                    wgPos++;
                    wbPos++;
                    waPos++;
                }

                wrPos += fromskew;
                wgPos += fromskew;
                wbPos += fromskew;
                waPos += fromskew;

                cpPos += toskew;
            }
        }

        /*
        * 16-bit unpacked samples => RGBA w/ unassociated alpha
        */
        private static void putRGBUAseparate16bittile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            short[] wrgba = Tiff.ByteArrayToShorts(rgba, 0, rgba.Length);

            int wrPos = rOffset / sizeof(short);
            int wgPos = gOffset / sizeof(short);
            int wbPos = bOffset / sizeof(short);
            int waPos = aOffset / sizeof(short);
            int cpPos = cpOffset;

            while (h-- > 0)
            {
                for (x = w; x-- > 0;)
                {
                    int a = W2B(wrgba[waPos]);
                    int r = (W2B(wrgba[wrPos]) * a + 127) / 255;
                    int g = (W2B(wrgba[wgPos]) * a + 127) / 255;
                    int b = (W2B(wrgba[wbPos]) * a + 127) / 255;
                    cp[cpPos] = PACK4(r, g, b, a);
                    cpPos++;
                    wrPos++;
                    wgPos++;
                    wbPos++;
                    waPos++;
                }

                wrPos += fromskew;
                wgPos += fromskew;
                wbPos += fromskew;
                waPos += fromskew;

                cpPos += toskew;
            }
        }

        /*
        * 8-bit packed YCbCr samples w/ no subsampling => RGB
        */
        private static void putseparate8bitYCbCr11tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            /* TODO: naming of input vars is still off, change obfuscating declaration inside define, or resolve obfuscation */
            int cpPos = cpOffset;
            int rPos = rOffset;
            int gPos = gOffset;
            int bPos = bOffset;
            while (h-- > 0)
            {
                x = w;
                do
                {
                    int dr, dg, db;
                    img.ycbcr.YCbCrtoRGB(rgba[rPos], rgba[gPos], rgba[bPos], out dr, out dg, out db);

                    cp[cpPos] = PACK(dr, dg, db);
                    cpPos++;
                    rPos++;
                    gPos++;
                    bPos++;
                } while (--x != 0);

                rPos += fromskew;
                gPos += fromskew;
                bPos += fromskew;
                cpPos += toskew;
            }
        }

        private bool initYCbCrConversion()
        {
            if (ycbcr == null)
                ycbcr = new TiffYCbCrToRGB();

            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.YCBCRCOEFFICIENTS);
            float[] luma = result[0].ToFloatArray();

            result = tif.GetFieldDefaulted(TiffTag.REFERENCEBLACKWHITE);
            float[] refBlackWhite = result[0].ToFloatArray();

            ycbcr.Init(luma, refBlackWhite);
            return true;
        }

        private tileContigRoutine initCIELabConversion()
        {
            if (cielab == null)
                cielab = new TiffCIELabToRGB();

            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.WHITEPOINT);
            float[] whitePoint = result[0].ToFloatArray();

            float[] refWhite = new float[3];
            refWhite[1] = 100.0F;
            refWhite[0] = whitePoint[0] / whitePoint[1] * refWhite[1];
            refWhite[2] = (1.0F - whitePoint[0] - whitePoint[1]) / whitePoint[1] * refWhite[1];
            cielab.Init(display_sRGB, refWhite);

            return putcontig8bitCIELab;
        }

        /* 
        * Construct any mapping table used
        * by the associated put routine.
        */
        private bool buildMap()
        {
            switch (photometric)
            {
                case Photometric.RGB:
                case Photometric.YCBCR:
                case Photometric.SEPARATED:
                    if (bitspersample == 8)
                        break;
                    if (!setupMap())
                        return false;
                    break;

                case Photometric.MINISBLACK:
                case Photometric.MINISWHITE:
                    if (!setupMap())
                        return false;
                    break;

                case Photometric.PALETTE:
                    /*
                    * Convert 16-bit colormap to 8-bit (unless it looks
                    * like an old-style 8-bit colormap).
                    */
                    if (checkcmap() == 16)
                        cvtcmap();
                    else
                        Tiff.WarningExt(tif, tif.m_clientdata, tif.FileName(), "Assuming 8-bit colormap");
                    /*
                    * Use mapping table and colormap to construct
                    * unpacking tables for samples < 8 bits.
                    */
                    if (bitspersample <= 8 && !makecmap())
                        return false;
                    break;
            }

            return true;
        }

        /*
        * Construct a mapping table to convert from the range
        * of the data samples to [0,255] --for display.  This
        * process also handles inverting B&W images when needed.
        */
        private bool setupMap()
        {
            int range = (1 << bitspersample) - 1;

            /* treat 16 bit the same as eight bit */
            if (bitspersample == 16)
                range = 255;

            Map = new byte [range + 1];

            if (photometric == Photometric.MINISWHITE)
            {
                for (int x = 0; x <= range; x++)
                    Map[x] = (byte)(((range - x) * 255) / range);
            }
            else
            {
                for (int x = 0; x <= range; x++)
                    Map[x] = (byte)((x * 255) / range);
            }
            
            if (bitspersample <= 16 && (photometric == Photometric.MINISBLACK || photometric == Photometric.MINISWHITE))
            {
                /*
                * Use photometric mapping table to construct
                * unpacking tables for samples <= 8 bits.
                */
                if (!makebwmap())
                    return false;
                
                /* no longer need Map, free it */
                Map = null;
            }

            return true;
        }

        private int checkcmap()
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int n = 1 << bitspersample;
            while (n-- > 0)
            {
                if (redcmap[r] >= 256 || greencmap[g] >= 256 || bluecmap[b] >= 256)
                    return 16;

                r++;
                g++;
                b++;
            }

            return 8;
        }

        private void cvtcmap()
        {
            for (int i = (1 << bitspersample) - 1; i >= 0; i--)
            {
                redcmap[i] = (short)(redcmap[i] >> 8);
                greencmap[i] = (short)(greencmap[i] >> 8);
                bluecmap[i] = (short)(bluecmap[i] >> 8);
            }
        }

        private bool makecmap()
        {
            int nsamples = 8 / bitspersample;

            PALmap = new int[256][];
            for (int i = 0; i < 256; i++)
                PALmap[i] = new int [nsamples];

            for (int i = 0; i < 256; i++)
            {
                int j = 0;
                switch (bitspersample)
                {
                    case 1:
                        CMAP(i >> 7, i, ref j);
                        CMAP((i >> 6) & 1, i, ref j);
                        CMAP((i >> 5) & 1, i, ref j);
                        CMAP((i >> 4) & 1, i, ref j);
                        CMAP((i >> 3) & 1, i, ref j);
                        CMAP((i >> 2) & 1, i, ref j);
                        CMAP((i >> 1) & 1, i, ref j);
                        CMAP(i & 1, i, ref j);
                        break;
                    case 2:
                        CMAP(i >> 6, i, ref j);
                        CMAP((i >> 4) & 3, i, ref j);
                        CMAP((i >> 2) & 3, i, ref j);
                        CMAP(i & 3, i, ref j);
                        break;
                    case 4:
                        CMAP(i >> 4, i, ref j);
                        CMAP(i & 0xf, i, ref j);
                        break;
                    case 8:
                        CMAP(i, i, ref j);
                        break;
                }
            }
            
            return true;
        }

        private bool makebwmap()
        {
            int nsamples = 8 / bitspersample;
            if (nsamples == 0)
                nsamples = 1;

            BWmap = new int[256][];
            for (int i = 0; i < 256; i++)
                BWmap[i] = new int [nsamples];

            for (int i = 0; i < 256; i++)
            {
                int j = 0;
                switch (bitspersample)
                {
                    case 1:
                        GREY(i >> 7, i, ref j);
                        GREY((i >> 6) & 1, i, ref j);
                        GREY((i >> 5) & 1, i, ref j);
                        GREY((i >> 4) & 1, i, ref j);
                        GREY((i >> 3) & 1, i, ref j);
                        GREY((i >> 2) & 1, i, ref j);
                        GREY((i >> 1) & 1, i, ref j);
                        GREY(i & 1, i, ref j);
                        break;
                    case 2:
                        GREY(i >> 6, i, ref j);
                        GREY((i >> 4) & 3, i, ref j);
                        GREY((i >> 2) & 3, i, ref j);
                        GREY(i & 3, i, ref j);
                        break;
                    case 4:
                        GREY(i >> 4, i, ref j);
                        GREY(i & 0xf, i, ref j);
                        break;
                    case 8:
                    case 16:
                        GREY(i, i, ref j);
                        break;
                }
            }

            return true;
        }

        /*
        * 8-bit packed CIE L*a*b 1976 samples => RGB
        */
        private static void putcontig8bitCIELab(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            fromskew *= 3;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    float X, Y, Z;
                    img.cielab.CIELabToXYZ(pp[ppPos], pp[ppPos + 1], pp[ppPos + 2], out X, out Y, out Z);

                    int r, g, b;
                    img.cielab.XYZToRGB(X, Y, Z, out r, out g, out b);

                    cp[cpPos] = PACK(r, g, b);
                    cpPos++;
                    ppPos += 3;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit packed YCbCr samples w/ 4,4 subsampling => RGB
        */
        private static void putcontig8bitYCbCr44tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            int cp1 = cpPos + w + toskew;
            int cp2 = cp1 + w + toskew;
            int cp3 = cp2 + w + toskew;
            int incr = 3 * w + 4 * toskew;

            /* adjust fromskew */
            fromskew = (fromskew * 18) / 4;
            if ((h & 3) == 0 && (w & 3) == 0)
            {
                for ( ; h >= 4; h -= 4)
                {
                    x = w >> 2;
                    do
                    {
                        int Cb = pp[ppPos + 16];
                        int Cr = pp[ppPos + 17];

                        img.YCbCrtoRGB(out cp[cpPos], pp[ppPos + 0], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp2 + 0], pp[ppPos + 8], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp2 + 1], pp[ppPos + 9], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp2 + 2], pp[ppPos + 10], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp2 + 3], pp[ppPos + 11], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp3 + 0], pp[ppPos + 12], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp3 + 1], pp[ppPos + 13], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp3 + 2], pp[ppPos + 14], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp3 + 3], pp[ppPos + 15], Cb, Cr);

                        cpPos += 4;
                        cp1 += 4;
                        cp2 += 4;
                        cp3 += 4;
                        ppPos += 18;
                    }
                    while (--x != 0);

                    cpPos += incr;
                    cp1 += incr;
                    cp2 += incr;
                    cp3 += incr;
                    ppPos += fromskew;
                }
            }
            else
            {
                while (h > 0)
                {
                    for (x = w; x > 0; )
                    {
                        int Cb = pp[ppPos + 16];
                        int Cr = pp[ppPos + 17];

                        bool h_goOn = false;
                        bool x_goOn = false;

                        // order of if's is important
                        if (x < 1 || x > 3)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img.YCbCrtoRGB(out cp[cp3 + 3], pp[ppPos + 15], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp2 + 3], pp[ppPos + 11], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img.YCbCrtoRGB(out cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);

                            x_goOn = true;
                        }

                        if (x == 3 || x_goOn)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img.YCbCrtoRGB(out cp[cp3 + 2], pp[ppPos + 14], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp2 + 2], pp[ppPos + 10], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img.YCbCrtoRGB(out cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);

                            x_goOn = true;
                        }

                        if (x == 2 || x_goOn)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img.YCbCrtoRGB(out cp[cp3 + 1], pp[ppPos + 13], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp2 + 1], pp[ppPos + 9], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                        }

                        if (x == 1 || x_goOn)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img.YCbCrtoRGB(out cp[cp3 + 0], pp[ppPos + 12], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp2 + 0], pp[ppPos + 8], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img.YCbCrtoRGB(out cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                        }

                        if (x < 4)
                        {
                            cpPos += x;
                            cp1 += x;
                            cp2 += x;
                            cp3 += x;
                            x = 0;
                        }
                        else
                        {
                            cpPos += 4;
                            cp1 += 4;
                            cp2 += 4;
                            cp3 += 4;
                            x -= 4;
                        }

                        ppPos += 18;
                    }

                    if (h <= 4)
                        break;

                    h -= 4;
                    cpPos += incr;
                    cp1 += incr;
                    cp2 += incr;
                    cp3 += incr;
                    ppPos += fromskew;
                }
            }
        }

        /*
        * 8-bit packed YCbCr samples w/ 4,2 subsampling => RGB
        */
        private static void putcontig8bitYCbCr42tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            int cp1 = cpPos + w + toskew;
            int incr = 2 * toskew + w;

            fromskew = (fromskew * 10) / 4;
            if ((h & 3) == 0 && (w & 1) == 0)
            {
                for ( ; h >= 2; h -= 2)
                {
                    x = w >> 2;
                    do
                    {
                        int Cb = pp[ppPos + 8];
                        int Cr = pp[ppPos + 9];

                        img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);
                        img.YCbCrtoRGB(out cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);

                        cpPos += 4;
                        cp1 += 4;
                        ppPos += 10;
                    }
                    while (--x != 0);

                    cpPos += incr;
                    cp1 += incr;
                    ppPos += fromskew;
                }
            }
            else
            {
                while (h > 0)
                {
                    for (x = w; x > 0; )
                    {
                        int Cb = pp[ppPos + 8];
                        int Cr = pp[ppPos + 9];

                        bool x_goOn = false;
                        if (x < 1 || x > 3)
                        {
                            if (h != 1)
                                img.YCbCrtoRGB(out cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);

                            img.YCbCrtoRGB(out cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);
                            x_goOn = true;
                        }

                        if (x == 3 || x_goOn)
                        {
                            if (h != 1)
                                img.YCbCrtoRGB(out cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);

                            img.YCbCrtoRGB(out cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                            x_goOn = true;
                        }

                        if (x == 2 || x_goOn)
                        {
                            if (h != 1)
                                img.YCbCrtoRGB(out cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);

                            img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                            x_goOn = true;
                        }

                        if (x == 1 || x_goOn)
                        {
                            if (h != 1)
                                img.YCbCrtoRGB(out cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);

                            img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                        }

                        if (x < 4)
                        {
                            cpPos += x;
                            cp1 += x;
                            x = 0;
                        }
                        else
                        {
                            cpPos += 4;
                            cp1 += 4;
                            x -= 4;
                        }

                        ppPos += 10;
                    }

                    if (h <= 2)
                        break;

                    h -= 2;
                    cpPos += incr;
                    cp1 += incr;
                    ppPos += fromskew;
                }
            }
        }

        /*
        * 8-bit packed YCbCr samples w/ 4,1 subsampling => RGB
        */
        private static void putcontig8bitYCbCr41tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            /* XXX adjust fromskew */
            do
            {
                x = w >> 2;
                do
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];

                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);

                    cpPos += 4;
                    ppPos += 6;
                }
                while (--x != 0);

                if ((w & 3) != 0)
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];

                    int xx = w & 3;
                    if (xx == 3)
                        img.YCbCrtoRGB(out cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);

                    if (xx == 3 || xx == 2)
                        img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);

                    if (xx == 3 || xx == 2 || xx == 1)
                        img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);

                    cpPos += xx;
                    ppPos += 6;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
            while (--h != 0);
        }

        /*
        * 8-bit packed YCbCr samples w/ 2,2 subsampling => RGB
        */
        private static void putcontig8bitYCbCr22tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            fromskew = (fromskew / 2) * 6;
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            int cp2 = cpPos + w + toskew;

            while (h >= 2)
            {
                x = w;
                while (x >= 2)
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];
                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cp2 + 0], pp[ppPos + 2], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cp2 + 1], pp[ppPos + 3], Cb, Cr);
                    cpPos += 2;
                    cp2 += 2;
                    ppPos += 6;
                    x -= 2;
                }

                if (x == 1)
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];
                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cp2 + 0], pp[ppPos + 2], Cb, Cr);
                    cpPos++;
                    cp2++;
                    ppPos += 6;
                }

                cpPos += toskew * 2 + w;
                cp2 += toskew * 2 + w;
                ppPos += fromskew;
                h -= 2;
            }

            if (h == 1)
            {
                x = w;
                while (x >= 2)
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];
                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                    cpPos += 2;
                    cp2 += 2;
                    ppPos += 6;
                    x -= 2;
                }

                if (x == 1)
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];
                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                }
            }
        }

        /*
        * 8-bit packed YCbCr samples w/ 2,1 subsampling => RGB
        */
        private static void putcontig8bitYCbCr21tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            fromskew = (fromskew * 4) / 2;
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            do
            {
                x = w >> 1;
                do
                {
                    int Cb = pp[ppPos + 2];
                    int Cr = pp[ppPos + 3];

                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);

                    cpPos += 2;
                    ppPos += 4;
                }
                while (--x != 0);

                if ((w & 1) != 0)
                {
                    int Cb = pp[ppPos + 2];
                    int Cr = pp[ppPos + 3];

                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);

                    cpPos += 1;
                    ppPos += 4;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
            while (--h != 0);
        }

        /*
        * 8-bit packed YCbCr samples w/ no subsampling => RGB
        */
        private static void putcontig8bitYCbCr11tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            fromskew *= 3;
            do
            {
                x = w; /* was x = w>>1; patched 2000/09/25 warmerda@home.com */
                do
                {
                    int Cb = pp[ppPos + 1];
                    int Cr = pp[ppPos + 2];

                    img.YCbCrtoRGB(out cp[cpPos], pp[ppPos + 0], Cb, Cr);
                    cpPos++;
                    ppPos += 3;
                }
                while (--x != 0);

                cpPos += toskew;
                ppPos += fromskew;
            }
            while (--h != 0);
        }

        /*
        * 8-bit packed YCbCr samples w/ 1,2 subsampling => RGB
        */
        private static void putcontig8bitYCbCr12tile(TiffRGBAImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            fromskew = (fromskew / 2) * 4;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            int cp2 = cpPos + w + toskew;
            while (h >= 2)
            {
                x = w;
                do
                {
                    int Cb = pp[ppPos + 2];
                    int Cr = pp[ppPos + 3];
                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img.YCbCrtoRGB(out cp[cp2 + 0], pp[ppPos + 1], Cb, Cr);
                    cpPos++;
                    cp2++;
                    ppPos += 4;
                } while (--x != 0);

                cpPos += toskew * 2 + w;
                cp2 += toskew * 2 + w;
                ppPos += fromskew;
                h -= 2;
            }

            if (h == 1)
            {
                x = w;
                do
                {
                    int Cb = pp[ppPos + 2];
                    int Cr = pp[ppPos + 3];
                    img.YCbCrtoRGB(out cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    cpPos++;
                    ppPos += 4;
                } while (--x != 0);
            }
        }

        /*
        * YCbCr -> RGB conversion and packing routines.
        */
        private void YCbCrtoRGB(out int dst, int Y, int Cb, int Cr)
        {
            int r, g, b;
            ycbcr.YCbCrtoRGB(Y, Cb, Cr, out r, out g, out b);
            dst = PACK(r, g, b);
        }
    }
}
