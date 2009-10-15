using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff
{
    /// <summary>
    /// RGBA-style image support.
    /// </summary>
    class TiffRGBAImage
    {
        /*
         * The image reading and conversion routines invoke
         * ``put routines'' to copy/image/whatever tiles of
         * raw image data.  A default set of routines are 
         * provided to convert/copy raw image data to 8-bit
         * packed ABGR format rasters.  Applications can supply
         * alternate routines that unpack the data into a
         * different format or, for example, unpack the data
         * and draw the unpacked raster on the display.
         */
        public delegate void tileContigRoutine(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset);
        public delegate void tileSeparateRoutine(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset);

        public const string photoTag = "PhotometricInterpretation";

        public Tiff tif; /* image handle */
        public bool stoponerr; /* stop on read error */
        public bool isContig; /* data is packed/separate */
        public int alpha; /* type of alpha data present */
        public int width; /* image width */
        public int height; /* image height */
        public UInt16 bitspersample; /* image bits/sample */
        public UInt16 samplesperpixel; /* image samples/pixel */
        public UInt16 orientation; /* image orientation */
        public UInt16 req_orientation; /* requested orientation */
        public UInt16 photometric; /* image photometric interp */
        public UInt16[] redcmap; /* colormap pallete */
        public UInt16[] greencmap;
        public UInt16[] bluecmap;

        /* get image data routine */
        public delegate bool get(TiffRGBAImage img, uint[] raster, int offset, uint w, uint h);

        // put image data routine
        //public void(*put)(TiffRGBAImage);
        public tileContigRoutine contig;
        public tileSeparateRoutine separate;

        public byte[] Map; /* sample mapping array */
        public uint[][] BWmap; /* black&white map */
        public uint[][] PALmap; /* palette image map */
        public TiffYCbCrToRGB ycbcr; /* YCbCr conversion state */
        public TiffCIELabToRGB cielab; /* CIE L*a*b conversion state */

        public int row_offset;
        public int col_offset;

        public static TiffRGBAImage Create(Tiff tif, bool stop, out string emsg)
        {
            TiffRGBAImage* img = new TiffRGBAImage();
            /* Initialize to normal values */
            img->row_offset = 0;
            img->col_offset = 0;
            img->redcmap = NULL;
            img->greencmap = NULL;
            img->bluecmap = NULL;
            img->req_orientation = ORIENTATION_BOTLEFT; /* It is the default */

            img->tif = tif;
            img->stoponerr = stop;
            tif->GetFieldDefaulted(TIFFTAG_BITSPERSAMPLE, &img->bitspersample);
            switch (img->bitspersample)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                case 16:
                    break;
                default:
                    sprintf(emsg, "Sorry, can not handle images with %d-bit samples", img->bitspersample);
                    delete img;
                    return NULL;
            }

            img->alpha = 0;
            tif->GetFieldDefaulted(TIFFTAG_SAMPLESPERPIXEL, &img->samplesperpixel);

            UInt16* sampleinfo;
            UInt16 extrasamples;
            tif->GetFieldDefaulted(TIFFTAG_EXTRASAMPLES, &extrasamples, &sampleinfo);
            if (extrasamples >= 1)
            {
                switch (sampleinfo[0])
                {
                    case EXTRASAMPLE_UNSPECIFIED:
                        /* Workaround for some images without */
                        if (img->samplesperpixel > 3)
                            /* correct info about alpha channel */
                            img->alpha = EXTRASAMPLE_ASSOCALPHA;
                        break;
                    case EXTRASAMPLE_ASSOCALPHA:
                        /* data is pre-multiplied */
                    case EXTRASAMPLE_UNASSALPHA:
                        /* data is not pre-multiplied */
                        img->alpha = sampleinfo[0];
                        break;
                }
            }

            if (Tiff.DEFAULT_EXTRASAMPLE_AS_ALPHA)
            {
                if (!tif->GetField(TIFFTAG_PHOTOMETRIC, &img->photometric))
                    img->photometric = PHOTOMETRIC_MINISWHITE;

                if (extrasamples == 0 && img->samplesperpixel == 4 && img->photometric == PHOTOMETRIC_RGB)
                {
                    img->alpha = EXTRASAMPLE_ASSOCALPHA;
                    extrasamples = 1;
                }
            }

            int colorchannels = img->samplesperpixel - extrasamples;
            
            UInt16 compress;
            tif->GetFieldDefaulted(TIFFTAG_COMPRESSION, &compress);
            
            UInt16 planarconfig;
            tif->GetFieldDefaulted(TIFFTAG_PLANARCONFIG, &planarconfig);
            if (!tif->GetField(TIFFTAG_PHOTOMETRIC, &img->photometric))
            {
                switch (colorchannels)
                {
                case 1:
                    if (img->isCCITTCompression())
                        img->photometric = PHOTOMETRIC_MINISWHITE;
                    else
                        img->photometric = PHOTOMETRIC_MINISBLACK;
                    break;
                case 3:
                    img->photometric = PHOTOMETRIC_RGB;
                    break;
                default:
                    sprintf(emsg, "Missing needed %s tag", photoTag);
                    delete img;
                    return NULL;
                }
            }
            switch (img->photometric)
            {
            case PHOTOMETRIC_PALETTE:
                {
                    UInt16* red_orig;
                    UInt16* green_orig;
                    UInt16* blue_orig;
                    if (!tif->GetField(TIFFTAG_COLORMAP, &red_orig, &green_orig, &blue_orig))
                    {
                        sprintf(emsg, "Missing required \"Colormap\" tag");
                        delete img;
                        return NULL;
                    }

                    /* copy the colormaps so we can modify them */
                    int n_color = (1L << img->bitspersample);
                    img->redcmap = new UInt16 [n_color];
                    img->greencmap = new UInt16 [n_color];
                    img->bluecmap = new UInt16 [n_color];
                    if (img->redcmap == NULL || img->greencmap == NULL || img->bluecmap == NULL)
                    {
                        sprintf(emsg, "Out of memory for colormap copy");
                        delete img;
                        return NULL;
                    }

                    memcpy(img->redcmap, red_orig, n_color * 2);
                    memcpy(img->greencmap, green_orig, n_color * 2);
                    memcpy(img->bluecmap, blue_orig, n_color * 2);

                    if (planarconfig == PLANARCONFIG_CONTIG && img->samplesperpixel != 1 && img->bitspersample < 8)
                    {
                        sprintf(emsg, "Sorry, can not handle contiguous data with %s=%d, ""and %s=%d and Bits/Sample=%d", photoTag, img->photometric, "Samples/pixel", img->samplesperpixel, img->bitspersample);
                        delete img;
                        return NULL;
                    }
                }
                break;

            case PHOTOMETRIC_MINISWHITE:
            case PHOTOMETRIC_MINISBLACK:
                if (planarconfig == PLANARCONFIG_CONTIG && img->samplesperpixel != 1 && img->bitspersample < 8)
                {
                    sprintf(emsg, "Sorry, can not handle contiguous data with %s=%d, ""and %s=%d and Bits/Sample=%d", photoTag, img->photometric, "Samples/pixel", img->samplesperpixel, img->bitspersample);
                    delete img;
                    return NULL;
                }
                break;
            case PHOTOMETRIC_YCBCR:
                /* It would probably be nice to have a reality check here. */
                if (planarconfig == PLANARCONFIG_CONTIG)
                {
                    /* can rely on libjpeg to convert to RGB */
                    /* XXX should restore current state on exit */
                    switch (compress)
                    {
                        case COMPRESSION_JPEG:
                            /*
                            * TODO: when complete tests verify complete desubsampling
                            * and YCbCr handling, remove use of TIFFTAG_JPEGCOLORMODE in
                            * favor of native handling
                            */
                            tif->SetField(TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE_RGB);
                            img->photometric = PHOTOMETRIC_RGB;
                            break;

                        default:
                            /* do nothing */;
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
            case PHOTOMETRIC_RGB:
                if (colorchannels < 3)
                {
                    sprintf(emsg, "Sorry, can not handle RGB image with %s=%d", "Color channels", colorchannels);
                    delete img;
                    return NULL;
                }
                break;
            case PHOTOMETRIC_SEPARATED:
                {
                    UInt16 inkset;
                    tif->GetFieldDefaulted(TIFFTAG_INKSET, &inkset);
                    if (inkset != INKSET_CMYK)
                    {
                        sprintf(emsg, "Sorry, can not handle separated image with %s=%d", "InkSet", inkset);
                        delete img;
                        return NULL;
                    }
                    if (img->samplesperpixel < 4)
                    {
                        sprintf(emsg, "Sorry, can not handle separated image with %s=%d", "Samples/pixel", img->samplesperpixel);
                        delete img;
                        return NULL;
                    }
                    break;
                }
            case PHOTOMETRIC_LOGL:
                if (compress != COMPRESSION_SGILOG)
                {
                    sprintf(emsg, "Sorry, LogL data must have %s=%d", "Compression", COMPRESSION_SGILOG);
                    delete img;
                    return NULL;
                }
                tif->SetField(TIFFTAG_SGILOGDATAFMT, SGILOGDATAFMT_8BIT);
                img->photometric = PHOTOMETRIC_MINISBLACK; /* little white lie */
                img->bitspersample = 8;
                break;
            case PHOTOMETRIC_LOGLUV:
                if (compress != COMPRESSION_SGILOG && compress != COMPRESSION_SGILOG24)
                {
                    sprintf(emsg, "Sorry, LogLuv data must have %s=%d or %d", "Compression", COMPRESSION_SGILOG, COMPRESSION_SGILOG24);
                    delete img;
                    return NULL;
                }

                if (planarconfig != PLANARCONFIG_CONTIG)
                {
                    sprintf(emsg, "Sorry, can not handle LogLuv images with %s=%d", "Planarconfiguration", planarconfig);
                    delete img;
                    return NULL;
                }
                
                tif->SetField(TIFFTAG_SGILOGDATAFMT, SGILOGDATAFMT_8BIT);
                img->photometric = PHOTOMETRIC_RGB; /* little white lie */
                img->bitspersample = 8;
                break;
            case PHOTOMETRIC_CIELAB:
                break;
            default:
                sprintf(emsg, "Sorry, can not handle image with %s=%d", photoTag, img->photometric);
                delete img;
                return NULL;
            }
            img->Map = NULL;
            img->BWmap = NULL;
            img->PALmap = NULL;
            img->ycbcr = NULL;
            img->cielab = NULL;
            tif->GetField(TIFFTAG_IMAGEWIDTH, &img->width);
            tif->GetField(TIFFTAG_IMAGELENGTH, &img->height);
            tif->GetFieldDefaulted(TIFFTAG_ORIENTATION, &img->orientation);
            
            img->isContig = !(planarconfig == PLANARCONFIG_SEPARATE && colorchannels > 1);
            if (img->isContig)
            {
                if (!img->pickContigCase())
                {
                    sprintf(emsg, "Sorry, can not handle image");
                    delete img;
                    return NULL;
                }
            }
            else
            {
                if (!img->pickSeparateCase())
                {
                    sprintf(emsg, "Sorry, can not handle image");
                    delete img;
                    return NULL;
                }
            }

            return img;
        }

        public bool Get(uint[] raster, int offset, int w, int h)
        {
            if (get == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No \"get\" routine setup");
                return false;
            }
            
            //if (put == NULL)
            //{
            //    Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No \"put\" routine setup; probably can not handle image format");
            //    return false;
            //}

            return (*get)(this, raster, offset, w, h);
        }

        protected TiffRGBAImage()
        {
            tif = NULL;
            redcmap = NULL;
            greencmap = NULL;
            bluecmap = NULL;

            Map = NULL;
            BWmap = NULL;
            PALmap = NULL;
            ycbcr = NULL;
            cielab = NULL;
        }

        private static TiffDisplay display_sRGB;
        private const uint A1 = (((uint)0xffL)<<24);
        
        /* 
        * Helper constants used in Orientation tag handling
        */
        private const int FLIP_VERTICALLY = 0x01;
        private const int FLIP_HORIZONTALLY = 0x02;

        private static uint PACK(uint r, uint g, uint b)
        {
            return ((uint)r | ((uint)g << 8) | ((uint)b << 16) | A1);
        }

        private static uint PACK4(uint r, uint g, uint b, uint a)
        {
            return ((uint)r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24));
        }

        private static uint W2B(UInt16 v)
        {
            return ((v >> 8) & 0xff);
        }

        private static uint PACKW(UInt16 r, UInt16 g, UInt16 b)
        {
            return ((uint)W2B(r) | ((uint)W2B(g) << 8) | ((uint)W2B(b) << 16) | A1);
        }

        private static uint PACKW4(UInt16 r, UInt16 g, UInt16 b, UInt16 a)
        {
            return ((uint)W2B(r) | ((uint)W2B(g) << 8) | ((uint)W2B(b) << 16) | ((uint)W2B(a) << 24));
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
            byte c = (byte)x;
            PALmap[i][j++] = PACK(redcmap[c] & 0xff, greencmap[c] & 0xff, bluecmap[c] & 0xff);
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
            byte c = Map[x];
            BWmap[i][j++] = PACK(c, c, c);
        }

        /*
        * Get an tile-organized image that has
        *  PlanarConfiguration contiguous if SamplesPerPixel > 1
        * or
        *  SamplesPerPixel == 1
        */
        private static bool gtTileContig(TiffRGBAImage img, uint[] raster, int offset, uint w, uint h)
        {
            Tiff* tif = img->tif;
            tileContigRoutine put = img->contig;

            byte* buf = new byte [tif->TileSize()];
            if (buf == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for tile buffer");
                return false;
            }

            memset(buf, 0, tif->TileSize());

            uint tw;
            tif->GetField(TIFFTAG_TILEWIDTH, &tw);

            uint th;
            tif->GetField(TIFFTAG_TILELENGTH, &th);

            int flip = img->setorientation();
            uint y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(int)(tw + w);
            }
            else
            {
                y = 0;
                toskew = -(int)(tw - w);
            }

            bool ret = true;
            for (uint row = 0; row < h; )
            {
                uint rowstoread = th - (row + img->row_offset) % th;
                uint nrow = (row + rowstoread > h ? h - row: rowstoread);
                for (uint col = 0; col < w; col += tw)
                {
                    if (tif->ReadTile(buf, 0, col + img->col_offset, row + img->row_offset, 0, 0) < 0 && img->stoponerr)
                    {
                        ret = false;
                        break;
                    }

                    uint pos = ((row + img->row_offset) % th) * tif->TileRowSize();

                    if (col + tw > w)
                    {
                        /*
                        * Tile is clipped horizontally.  Calculate
                        * visible portion and skewing factors.
                        */
                        uint npix = w - col;
                        int fromskew = tw - npix;
                        (*put)(img, raster, offset + y * w + col, col, y, npix, nrow, fromskew, toskew + fromskew, buf, pos);
                    }
                    else
                    {
                        (*put)(img, raster, offset + y * w + col, col, y, tw, nrow, 0, toskew, buf, pos);
                    }
                }

                y += ((flip & FLIP_VERTICALLY) != 0 ?  -(int)nrow : (int)nrow);
                row += nrow;
            }

            delete buf;

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (uint line = 0; line < h; line++)
                {
                    uint left = offset + line * w;
                    uint right = left + w - 1;

                    while (left < right)
                    {
                        uint temp = raster[left];
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
        private static bool gtTileSeparate(TiffRGBAImage img, uint[] raster, int offset, uint w, uint h)
        {
            Tiff* tif = img->tif;
            tileSeparateRoutine put = img->separate;

            int tilesize = tif->TileSize();
            byte* buf = new byte [(img->alpha != 0 ? 4 : 3) * tilesize];
            if (buf == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for tile buffer");
                return false;
            }

            memset(buf, 0, (img->alpha != 0 ? 4 : 3) * tilesize);
            int p0 = 0;
            int p1 = p0 + tilesize;
            int p2 = p1 + tilesize;
            int pa = (img->alpha != 0 ? (p2 + tilesize) : -1);
            
            uint tw;
            tif->GetField(TIFFTAG_TILEWIDTH, &tw);

            uint th;
            tif->GetField(TIFFTAG_TILELENGTH, &th);

            int flip = img->setorientation();
            uint y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(int)(tw + w);
            }
            else
            {
                y = 0;
                toskew = -(int)(tw - w);
            }

            bool ret = true;
            for (uint row = 0; row < h; )
            {
                uint rowstoread = th - (row + img->row_offset) % th;
                uint nrow = (row + rowstoread > h ? h - row : rowstoread);
                for (uint col = 0; col < w; col += tw)
                {
                    if (tif->ReadTile(buf, p0, col + img->col_offset, row + img->row_offset, 0, 0) < 0 && img->stoponerr)
                    {
                        ret = false;
                        break;
                    }

                    if (tif->ReadTile(buf, p1, col + img->col_offset, row + img->row_offset, 0, 1) < 0 && img->stoponerr)
                    {
                        ret = false;
                        break;
                    }
                    
                    if (tif->ReadTile(buf, p2, col + img->col_offset, row + img->row_offset, 0, 2) < 0 && img->stoponerr)
                    {
                        ret = false;
                        break;
                    }
                    
                    if (img->alpha != 0)
                    {
                        if (tif->ReadTile(buf, pa, col + img->col_offset, row + img->row_offset, 0, 3) < 0 && img->stoponerr)
                        {
                            ret = false;
                            break;
                        }
                    }

                    uint pos = ((row + img->row_offset) % th) * tif->TileRowSize();

                    if (col + tw > w)
                    {
                        /*
                        * Tile is clipped horizontally.  Calculate
                        * visible portion and skewing factors.
                        */
                        uint npix = w - col;
                        int fromskew = tw - npix;
                        (*put)(img, raster, offset + y * w + col, col, y, npix, nrow, fromskew, toskew + fromskew, buf, p0 + pos, p1 + pos, p2 + pos, img->alpha != 0 ? (pa + pos) : -1);
                    }
                    else
                    {
                        (*put)(img, raster, offset + y * w + col, col, y, tw, nrow, 0, toskew, buf, p0 + pos, p1 + pos, p2 + pos, img->alpha != 0 ? (pa + pos) : -1);
                    }
                }

                y += ((flip & FLIP_VERTICALLY) != 0 ? -(int)nrow : (int)nrow);
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (uint line = 0; line < h; line++)
                {
                    uint left = offset + line * w;
                    uint right = left + w - 1;

                    while (left < right)
                    {
                        uint temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            delete buf;
            return ret;
        }

        /*
        * Get a strip-organized image that has
        *  PlanarConfiguration contiguous if SamplesPerPixel > 1
        * or
        *  SamplesPerPixel == 1
        */
        private static bool gtStripContig(TiffRGBAImage img, uint[] raster, int offset, uint w, uint h)
        {
            Tiff* tif = img->tif;
            tileContigRoutine put = img->contig;

            byte* buf = new byte [tif->StripSize()];
            if (buf == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for strip buffer");
                return false;
            }

            memset(buf, 0, tif->StripSize());

            int flip = img->setorientation();
            uint y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(int)(w + w);
            }
            else
            {
                y = 0;
                toskew = -(int)(w - w);
            }

            uint rowsperstrip;
            tif->GetFieldDefaulted(TIFFTAG_ROWSPERSTRIP, &rowsperstrip);

            UInt16 subsamplinghor;
            UInt16 subsamplingver;
            tif->GetFieldDefaulted(TIFFTAG_YCBCRSUBSAMPLING, &subsamplinghor, &subsamplingver);

            int scanline = tif->newScanlineSize();
            int fromskew = (w < img->width ? img->width - w : 0);
            bool ret = true;

            for (uint row = 0; row < h; )
            {
                uint rowstoread = rowsperstrip - (row + img->row_offset) % rowsperstrip;
                uint nrow = (row + rowstoread > h ? h - row : rowstoread);
                uint nrowsub = nrow;
                if ((nrowsub % subsamplingver) != 0)
                    nrowsub += subsamplingver - nrowsub % subsamplingver;

                if (tif->ReadEncodedStrip(tif->ComputeStrip(row + img->row_offset, 0), buf, 0, ((row + img->row_offset) % rowsperstrip + nrowsub) * scanline) < 0 && img->stoponerr)
                {
                    ret = false;
                    break;
                }

                uint pos = ((row + img->row_offset) % rowsperstrip) * scanline;
                (*put)(img, raster, offset + y * w, 0, y, w, nrow, fromskew, toskew, buf, pos);
                y += (flip & FLIP_VERTICALLY ?  -(int)nrow: (int)nrow);
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (uint line = 0; line < h; line++)
                {
                    uint left = offset + line * w;
                    uint right = left + w - 1;

                    while (left < right)
                    {
                        uint temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            delete buf;
            return ret;
        }

        /*
        * Get a strip-organized image with
        *   SamplesPerPixel > 1
        *   PlanarConfiguration separated
        * We assume that all such images are RGB.
        */
        private static bool gtStripSeparate(TiffRGBAImage img, uint[] raster, int offset, uint w, uint h)
        {
            Tiff* tif = img->tif;
            tileSeparateRoutine put = img->separate;

            int stripsize = tif->StripSize();
            byte* buf = new byte [(img->alpha != 0 ? 4 : 3) * stripsize];
            if (buf == 0)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for tile buffer");
                return false;
            }

            memset(buf, 0, (img->alpha != 0 ? 4 : 3) * stripsize);
            int p0 = 0;
            int p1 = p0 + stripsize;
            int p2 = p1 + stripsize;
            int pa = p2 + stripsize;
            pa = (img->alpha != 0 ? (p2 + stripsize) : -1);

            int flip = img->setorientation();
            uint y;
            int toskew;
            if ((flip & FLIP_VERTICALLY) != 0)
            {
                y = h - 1;
                toskew = -(int)(w + w);
            }
            else
            {
                y = 0;
                toskew = -(int)(w - w);
            }

            uint rowsperstrip;
            tif->GetFieldDefaulted(TIFFTAG_ROWSPERSTRIP, &rowsperstrip);

            int scanline = tif->ScanlineSize();
            int fromskew = (w < img->width ? img->width - w : 0);
            bool ret = true;
            for (uint row = 0; row < h; )
            {
                uint rowstoread = rowsperstrip - (row + img->row_offset) % rowsperstrip;
                uint nrow = (row + rowstoread > h ? h - row : rowstoread);
                uint offset_row = row + img->row_offset;
                
                if (tif->ReadEncodedStrip(tif->ComputeStrip(offset_row, 0), buf, p0, ((row + img->row_offset) % rowsperstrip + nrow) * scanline) < 0 && img->stoponerr)
                {
                    ret = false;
                    break;
                }
                
                if (tif->ReadEncodedStrip(tif->ComputeStrip(offset_row, 1), buf, p1, ((row + img->row_offset) % rowsperstrip + nrow) * scanline) < 0 && img->stoponerr)
                {
                    ret = false;
                    break;
                }
                
                if (tif->ReadEncodedStrip(tif->ComputeStrip(offset_row, 2), buf, p2, ((row + img->row_offset) % rowsperstrip + nrow) * scanline) < 0 && img->stoponerr)
                {
                    ret = false;
                    break;
                }
                
                if (img->alpha != 0)
                {
                    if ((tif->ReadEncodedStrip(tif->ComputeStrip(offset_row, 3), buf, pa, ((row + img->row_offset) % rowsperstrip + nrow) * scanline) < 0 && img->stoponerr))
                    {
                        ret = false;
                        break;
                    }
                }
                
                uint pos = ((row + img->row_offset) % rowsperstrip) * scanline;
                (*put)(img, raster, offset + y * w, 0, y, w, nrow, fromskew, toskew, buf, p0 + pos, p1 + pos, p2 + pos, img->alpha != 0 ? (pa + pos) : -1);
                y += ((flip & FLIP_VERTICALLY) != 0 ? -(int)nrow : (int)nrow);
                row += nrow;
            }

            if ((flip & FLIP_HORIZONTALLY) != 0)
            {
                for (uint line = 0; line < h; line++)
                {
                    uint left = offset + line * w;
                    uint right = left + w - 1;

                    while (left < right)
                    {
                        uint temp = raster[left];
                        raster[left] = raster[right];
                        raster[right] = temp;
                        left++;
                        right--;
                    }
                }
            }

            delete buf;
            return ret;
        }

        private bool isCCITTCompression()
        {
            UInt16 compress;
            tif->GetField(TIFFTAG_COMPRESSION, &compress);

            return (compress == COMPRESSION_CCITTFAX3 || compress == COMPRESSION_CCITTFAX4 || compress == COMPRESSION_CCITTRLE || compress == COMPRESSION_CCITTRLEW);
        }

        private int setorientation()
        {
            switch (orientation)
            {
                case ORIENTATION_TOPLEFT:
                case ORIENTATION_LEFTTOP:
                    if (req_orientation == ORIENTATION_TOPRIGHT || req_orientation == ORIENTATION_RIGHTTOP)
                        return FLIP_HORIZONTALLY;
                    else if (req_orientation == ORIENTATION_BOTRIGHT || req_orientation == ORIENTATION_RIGHTBOT)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;
                    else if (req_orientation == ORIENTATION_BOTLEFT || req_orientation == ORIENTATION_LEFTBOT)
                        return FLIP_VERTICALLY;

                    return 0;

                case ORIENTATION_TOPRIGHT:
                case ORIENTATION_RIGHTTOP:
                    if (req_orientation == ORIENTATION_TOPLEFT || req_orientation == ORIENTATION_LEFTTOP)
                        return FLIP_HORIZONTALLY;
                    else if (req_orientation == ORIENTATION_BOTRIGHT || req_orientation == ORIENTATION_RIGHTBOT)
                        return FLIP_VERTICALLY;
                    else if (req_orientation == ORIENTATION_BOTLEFT || req_orientation == ORIENTATION_LEFTBOT)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;

                    return 0;

                case ORIENTATION_BOTRIGHT:
                case ORIENTATION_RIGHTBOT:
                    if (req_orientation == ORIENTATION_TOPLEFT || req_orientation == ORIENTATION_LEFTTOP)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;
                    else if (req_orientation == ORIENTATION_TOPRIGHT || req_orientation == ORIENTATION_RIGHTTOP)
                        return FLIP_VERTICALLY;
                    else if (req_orientation == ORIENTATION_BOTLEFT || req_orientation == ORIENTATION_LEFTBOT)
                        return FLIP_HORIZONTALLY;

                    return 0;

                case ORIENTATION_BOTLEFT:
                case ORIENTATION_LEFTBOT:
                    if (req_orientation == ORIENTATION_TOPLEFT || req_orientation == ORIENTATION_LEFTTOP)
                        return FLIP_VERTICALLY;
                    else if (req_orientation == ORIENTATION_TOPRIGHT || req_orientation == ORIENTATION_RIGHTTOP)
                        return FLIP_HORIZONTALLY | FLIP_VERTICALLY;
                    else if (req_orientation == ORIENTATION_BOTRIGHT || req_orientation == ORIENTATION_RIGHTBOT)
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
            get = tif->IsTiled() ? gtTileContig : gtStripContig;
            contig = NULL;

            switch (photometric)
            {
                case PHOTOMETRIC_RGB:
                    switch (bitspersample)
                    {
                        case 8:
                            if (alpha == EXTRASAMPLE_ASSOCALPHA)
                                contig = putRGBAAcontig8bittile;
                            else if (alpha == EXTRASAMPLE_UNASSALPHA)
                                contig = putRGBUAcontig8bittile;
                            else
                                contig = putRGBcontig8bittile;
                            break;
                        case 16:
                            if (alpha == EXTRASAMPLE_ASSOCALPHA)
                                contig = putRGBAAcontig16bittile;
                            else if (alpha == EXTRASAMPLE_UNASSALPHA)
                                contig = putRGBUAcontig16bittile;
                            else
                                contig = putRGBcontig16bittile;
                            break;
                    }
                    break;
                case PHOTOMETRIC_SEPARATED:
                    if (buildMap())
                    {
                        if (bitspersample == 8)
                        {
                            if (Map == NULL)
                                contig = putRGBcontig8bitCMYKtile;
                            else
                                contig = putRGBcontig8bitCMYKMaptile;
                        }
                    }
                    break;
                case PHOTOMETRIC_PALETTE:
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
                case PHOTOMETRIC_MINISWHITE:
                case PHOTOMETRIC_MINISBLACK:
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
                case PHOTOMETRIC_YCBCR:
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
                            UInt16 SubsamplingHor;
                            UInt16 SubsamplingVer;
                            tif->GetFieldDefaulted(TIFFTAG_YCBCRSUBSAMPLING, &SubsamplingHor, &SubsamplingVer);

                            switch ((SubsamplingHor << 4) | SubsamplingVer)
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
                case PHOTOMETRIC_CIELAB:
                    if (buildMap())
                    {
                        if (bitspersample == 8)
                            contig = initCIELabConversion();
                        break;
                    }
            }

            return ((get != NULL) && (contig != NULL));
        }

        /*
        * Select the appropriate conversion routine for unpacked data.
        *
        * NB: we assume that unpacked single channel data is directed
        *   to the "packed routines.
        */
        private bool pickSeparateCase()
        {
            get = tif->IsTiled() ? gtTileSeparate : gtStripSeparate;
            separate = NULL;

            switch (photometric)
            {
                case PHOTOMETRIC_RGB:
                    switch (bitspersample)
                    {
                        case 8:
                            if (alpha == EXTRASAMPLE_ASSOCALPHA)
                                separate = putRGBAAseparate8bittile;
                            else if (alpha == EXTRASAMPLE_UNASSALPHA)
                                separate = putRGBUAseparate8bittile;
                            else
                                separate = putRGBseparate8bittile;
                            break;
                        case 16:
                            if (alpha == EXTRASAMPLE_ASSOCALPHA)
                                separate = putRGBAAseparate16bittile;
                            else if (alpha == EXTRASAMPLE_UNASSALPHA)
                                separate = putRGBUAseparate16bittile;
                            else
                                separate = putRGBseparate16bittile;
                            break;
                    }
                    break;
                case PHOTOMETRIC_YCBCR:
                    if ((bitspersample == 8) && (samplesperpixel == 3))
                    {
                        if (initYCbCrConversion())
                        {
                            UInt16 hs;
                            UInt16 vs;
                            tif->GetFieldDefaulted(TIFFTAG_YCBCRSUBSAMPLING, &hs, &vs);
                            switch ((hs << 4) | vs)
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

            return ((get != NULL) && (separate != NULL));
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
        private static void put8bitcmaptile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** PALmap = img->PALmap;
            int samplesperpixel = img->samplesperpixel;

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

        /*
        * 4-bit palette => colormap/RGB
        */
        private static void put4bitcmaptile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** PALmap = img->PALmap;
            fromskew /= 2;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint* bw = NULL;
                int bwPos = 0;

                uint _x;
                for (_x = w; _x >= 2; _x -= 2)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 2; rc++)
                    {
                        cp[cpPos] = bw[bwPos];
                        cpPos++;
                        bwPos++;
                    }
                }

                if (_x != 0)
                {
                    bwPos = 0;
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    cp[cpPos] = bw[bwPos];
                    cpPos++;
                    bwPos++;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 2-bit palette => colormap/RGB
        */
        private static void put2bitcmaptile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** PALmap = img->PALmap;
            fromskew /= 4;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint* bw = NULL;
                int bwPos = 0;

                uint _x;
                for (_x = w; _x >= 4; _x -= 4)
                {
                    bw = PALmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 4; rc++)
                    {
                        cp[cpPos] = bw[bwPos];
                        cpPos++;
                        bwPos++;
                    }
                }

                if (_x > 0)
                {
                    bwPos = 0;
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 3 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = bw[bwPos];
                            cpPos++;
                            bwPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 1-bit palette => colormap/RGB
        */
        private static void put1bitcmaptile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** PALmap = img->PALmap;
            fromskew /= 8;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint* bw = NULL;
                int bwPos = 0;

                uint _x;
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
                    bwPos = 0;
                    bw = PALmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = bw[bwPos];
                            cpPos++;
                            bwPos++;
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
        private static void putgreytile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            uint** BWmap = img->BWmap;
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

        /*
        * 16-bit greyscale => colormap/RGB
        */
        private static void put16bitbwtile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            uint** BWmap = img->BWmap;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                UInt16* wp = Tiff::byteArrayToUInt16(pp, ppPos, sizeof(pp) / sizeof(pp[0]));
                int wpPos = 0;

                for (x = w; x-- > 0;)
                {
                    /* use high order byte of 16bit value */

                    cp[cpPos] = BWmap[wp[wpPos] >> 8][0];
                    cpPos++;
                    ppPos += 2 * samplesperpixel;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                ppPos += fromskew;

                delete[] wp;
            }
        }

        /*
        * 1-bit bilevel => colormap/RGB
        */
        private static void put1bitbwtile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** BWmap = img->BWmap;
            fromskew /= 8;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint* bw = NULL;
                int bwPos = 0;

                uint _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    bw = BWmap[pp[ppPos]];
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
                    bwPos = 0;
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = bw[bwPos];
                            cpPos++;
                            bwPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 2-bit greyscale => colormap/RGB
        */
        private static void put2bitbwtile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** BWmap = img->BWmap;
            fromskew /= 4;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint* bw = NULL;
                int bwPos = 0;

                uint _x;
                for (_x = w; _x >= 4; _x -= 4)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 4; rc++)
                    {
                        cp[cpPos] = bw[bwPos];
                        cpPos++;
                        bwPos++;
                    }
                }

                if (_x > 0)
                {
                    bwPos = 0;
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    if (_x <= 3 && _x > 0)
                    {
                        for (int i = _x; i > 0; i--)
                        {
                            cp[cpPos] = bw[bwPos];
                            cpPos++;
                            bwPos++;
                        }
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 4-bit greyscale => colormap/RGB
        */
        private static void put4bitbwtile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            uint** BWmap = img->BWmap;
            fromskew /= 2;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint* bw = NULL;
                int bwPos = 0;

                uint _x;
                for (_x = w; _x >= 2; _x -= 2)
                {
                    bw = BWmap[pp[ppPos]];
                    ppPos++;
                    for (int rc = 0; rc < 2; rc++)
                    {
                        cp[cpPos] = bw[bwPos];
                        cpPos++;
                        bwPos++;
                    }
                }

                if (_x != 0)
                {
                    bwPos = 0;
                    bw = BWmap[pp[ppPos]];
                    ppPos++;

                    cp[cpPos] = bw[bwPos];
                    cpPos++;
                    bwPos++;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /*
        * 8-bit packed samples, no Map => RGB
        */
        private static void putRGBcontig8bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            fromskew *= samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint _x;
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
        * (known to have Map == NULL)
        */
        private static void putRGBAAcontig8bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            fromskew *= samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                uint _x;
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
        * (known to have Map == NULL)
        */
        private static void putRGBUAcontig8bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            fromskew *= samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    uint a = pp[ppPos + 3];
                    uint r = (pp[ppPos] * a + 127) / 255;
                    uint g = (pp[ppPos + 1] * a + 127) / 255;
                    uint b = (pp[ppPos + 2] * a + 127) / 255;
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
        private static void putRGBcontig16bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            fromskew *= samplesperpixel;

            UInt16* wp = Tiff::byteArrayToUInt16(pp, ppPos, sizeof(pp) / sizeof(pp[0]));
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

            delete wp;
        }

        /*
        * 16-bit packed samples => RGBA w/ associated alpha
        * (known to have Map == NULL)
        */
        private static void putRGBAAcontig16bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            UInt16* wp = Tiff::byteArrayToUInt16(pp, ppPos, sizeof(pp) / sizeof(pp[0]));
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

            delete wp;
        }

        /*
        * 16-bit packed samples => RGBA w/ unassociated alpha
        * (known to have Map == NULL)
        */
        private static void putRGBUAcontig16bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            fromskew *= samplesperpixel;
            
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            UInt16* wp = Tiff::byteArrayToUInt16(pp, ppPos, sizeof(pp) / sizeof(pp[0]));
            int wpPos = 0;
            
            while (h-- > 0)
            {
                for (x = w; x-- > 0;)
                {
                    uint a = W2B(wp[wpPos + 3]);
                    uint r = (W2B(wp[wpPos]) * a + 127) / 255;
                    uint g = (W2B(wp[wpPos + 1]) * a + 127) / 255;
                    uint b = (W2B(wp[wpPos + 2]) * a + 127) / 255;
                    cp[cpPos] = PACK4(r, g, b, a);
                    cpPos++;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                wpPos += fromskew;
            }

            delete wp;
        }

        /*
        * 8-bit packed CMYK samples w/o Map => RGB
        *
        * NB: The conversion of CMYK->RGB is *very* crude.
        */
        private static void putRGBcontig8bitCMYKtile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            fromskew *= samplesperpixel;

            int cpPos = cpOffset;
            int ppPos = ppOffset;

            while (h-- > 0)
            {
                uint _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    for (int rc = 0; rc < 8; rc++)
                    {
                        UInt16 k = 255 - pp[ppPos + 3];
                        UInt16 r = (k * (255 - pp[ppPos])) / 255;
                        UInt16 g = (k * (255 - pp[ppPos + 1])) / 255;
                        UInt16 b = (k * (255 - pp[ppPos + 2])) / 255;
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
                            UInt16 k = 255 - pp[ppPos + 3];
                            UInt16 r = (k * (255 - pp[ppPos])) / 255;
                            UInt16 g = (k * (255 - pp[ppPos + 1])) / 255;
                            UInt16 b = (k * (255 - pp[ppPos + 2])) / 255;
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
        private static void putRGBcontig8bitCMYKMaptile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img->samplesperpixel;
            byte* Map = img->Map;
            fromskew *= samplesperpixel;

            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    UInt16 k = 255 - pp[ppPos + 3];
                    UInt16 r = (k * (255 - pp[ppPos])) / 255;
                    UInt16 g = (k * (255 - pp[ppPos + 1])) / 255;
                    UInt16 b = (k * (255 - pp[ppPos + 2])) / 255;
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
        private static void putRGBseparate8bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            int cpPos = cpOffset;
            int rPos = rOffset;
            int gPos = gOffset;
            int bPos = bOffset;

            while (h-- > 0)
            {
                uint _x;
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
        private static void putRGBAAseparate8bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            int cpPos = cpOffset;
            int rPos = rOffset;
            int gPos = gOffset;
            int bPos = bOffset;
            int aPos = aOffset;
            while (h-- > 0)
            {
                uint _x;
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
        private static void putRGBUAseparate8bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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
                    uint av = rgba[aPos];
                    uint rv = (rgba[rPos] * av + 127) / 255;
                    uint gv = (rgba[gPos] * av + 127) / 255;
                    uint bv = (rgba[bPos] * av + 127) / 255;
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
        private static void putRGBseparate16bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            UInt16* wrgba = Tiff::byteArrayToUInt16(rgba, 0, sizeof(rgba) / sizeof(rgba[0]));
    
            int wrPos = rOffset / sizeof(UInt16);
            int wgPos = gOffset / sizeof(UInt16);
            int wbPos = bOffset / sizeof(UInt16);
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

            delete wrgba;
        }

        /*
        * 16-bit unpacked samples => RGBA w/ associated alpha
        */
        private static void putRGBAAseparate16bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            UInt16* wrgba = Tiff::byteArrayToUInt16(rgba, 0, sizeof(rgba) / sizeof(rgba[0]));
    
            int wrPos = rOffset / sizeof(UInt16);
            int wgPos = gOffset / sizeof(UInt16);
            int wbPos = bOffset / sizeof(UInt16);
            int waPos = aOffset / sizeof(UInt16);
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

            delete wrgba;
        }

        /*
        * 16-bit unpacked samples => RGBA w/ unassociated alpha
        */
        private static void putRGBUAseparate16bittile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            UInt16* wrgba = Tiff::byteArrayToUInt16(rgba, 0, sizeof(rgba) / sizeof(rgba[0]));

            int wrPos = rOffset / sizeof(UInt16);
            int wgPos = gOffset / sizeof(UInt16);
            int wbPos = bOffset / sizeof(UInt16);
            int waPos = aOffset / sizeof(UInt16);
            int cpPos = cpOffset;

            while (h-- > 0)
            {
                for (x = w; x-- > 0;)
                {
                    uint a = W2B(wrgba[waPos]);
                    uint r = (W2B(wrgba[wrPos]) * a + 127) / 255;
                    uint g = (W2B(wrgba[wgPos]) * a + 127) / 255;
                    uint b = (W2B(wrgba[wbPos]) * a + 127) / 255;
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

            delete wrgba;
        }

        /*
        * 8-bit packed YCbCr samples w/ no subsampling => RGB
        */
        private static void putseparate8bitYCbCr11tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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
                    uint dr, dg, db;
                    img->ycbcr->YCbCrtoRGB(rgba[rPos], rgba[gPos], rgba[bPos], dr, dg, db);

                    cp[cpPos] = PACK(dr, dg, db);
                    cpPos++;
                    rPos++;
                    gPos++;
                    bPos++;
                } while (--x);

                rPos += fromskew;
                gPos += fromskew;
                bPos += fromskew;
                cpPos += toskew;
            }
        }

        private bool initYCbCrConversion()
        {
            static char module[] = "initYCbCrConversion";

            if (ycbcr == NULL)
            {
                ycbcr = new TiffYCbCrToRGB();

                if (ycbcr == NULL)
                {
                    Tiff::ErrorExt(tif, tif->m_clientdata, module, "No space for YCbCr->RGB conversion state");
                    return false;
                }
            }

            float* luma;
            tif->GetFieldDefaulted(TIFFTAG_YCBCRCOEFFICIENTS, &luma);

            float* refBlackWhite;
            tif->GetFieldDefaulted(TIFFTAG_REFERENCEBLACKWHITE, &refBlackWhite);

            ycbcr->Init(luma, refBlackWhite);
            return true;
        }

        private tileContigRoutine initCIELabConversion()
        {
            static char module[] = "initCIELabConversion";    

            if (cielab == NULL)
            {
                cielab = new TiffCIELabToRGB();
                if (cielab == NULL)
                {
                    Tiff::ErrorExt(tif, tif->m_clientdata, module, "No space for CIE L*a*b*->RGB conversion state.");
                    return NULL;
                }
            }

            float* whitePoint;
            tif->GetFieldDefaulted(TIFFTAG_WHITEPOINT, &whitePoint);
            
            float refWhite[3];
            refWhite[1] = 100.0F;
            refWhite[0] = whitePoint[0] / whitePoint[1] * refWhite[1];
            refWhite[2] = (1.0F - whitePoint[0] - whitePoint[1]) / whitePoint[1] * refWhite[1];
            cielab->Init(display_sRGB, refWhite);

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
            case PHOTOMETRIC_RGB:
            case PHOTOMETRIC_YCBCR:
            case PHOTOMETRIC_SEPARATED:
                if (bitspersample == 8)
                    break;
                if (!setupMap())
                    return false;
                break;

            case PHOTOMETRIC_MINISBLACK:
            case PHOTOMETRIC_MINISWHITE:
                if (!setupMap())
                    return false;
                break;

            case PHOTOMETRIC_PALETTE:
                /*
                * Convert 16-bit colormap to 8-bit (unless it looks
                * like an old-style 8-bit colormap).
                */
                if (checkcmap() == 16)
                    cvtcmap();
                else
                    Tiff::WarningExt(tif, tif->m_clientdata, tif->FileName(), "Assuming 8-bit colormap");
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
            int range = (int)((1L << bitspersample) - 1);

            /* treat 16 bit the same as eight bit */
            if (bitspersample == 16)
                range = 255;

            Map = new byte [range + 1];
            if (Map == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for photometric conversion table");
                return false;
            }

            if (photometric == PHOTOMETRIC_MINISWHITE)
            {
                for (int x = 0; x <= range; x++)
                    Map[x] = (byte)(((range - x) * 255) / range);
            }
            else
            {
                for (int x = 0; x <= range; x++)
                    Map[x] = (byte)((x * 255) / range);
            }
            
            if (bitspersample <= 16 && (photometric == PHOTOMETRIC_MINISBLACK || photometric == PHOTOMETRIC_MINISWHITE))
            {
                /*
                * Use photometric mapping table to construct
                * unpacking tables for samples <= 8 bits.
                */
                if (!makebwmap())
                    return false;
                
                /* no longer need Map, free it */
                delete Map;
                Map = NULL;
            }

            return true;
        }

        private int checkcmap()
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int n = 1L << bitspersample;
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
            for (int i = (1L << bitspersample) - 1; i >= 0; i--)
            {
                redcmap[i] = (UInt16)(redcmap[i] >> 8);
                greencmap[i] = (UInt16)(greencmap[i] >> 8);
                bluecmap[i] = (UInt16)(bluecmap[i] >> 8);
            }
        }

        private bool makecmap()
        {
            int nsamples = 8 / bitspersample;

            PALmap = new uint* [256];
            if (PALmap == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for Palette mapping table");
                return false;
            }

            for (int i = 0; i < 256; i++)
            {
                PALmap[i] = new uint [nsamples];
                if (PALmap[i] == NULL)
                {
                    for (int j = i - 1; i >= 0; i--)
                        delete PALmap[j];

                    delete PALmap;
                    Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for Palette mapping table");
                    return false;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                int j = 0;
                switch (bitspersample)
                {
                case 1:
                    CMAP(i >> 7, i, j);
                    CMAP((i >> 6) & 1, i, j);
                    CMAP((i >> 5) & 1, i, j);
                    CMAP((i >> 4) & 1, i, j);
                    CMAP((i >> 3) & 1, i, j);
                    CMAP((i >> 2) & 1, i, j);
                    CMAP((i >> 1) & 1, i, j);
                    CMAP(i & 1, i, j);
                    break;
                case 2:
                    CMAP(i >> 6, i, j);
                    CMAP((i >> 4) & 3, i, j);
                    CMAP((i >> 2) & 3, i, j);
                    CMAP(i & 3, i, j);
                    break;
                case 4:
                    CMAP(i >> 4, i, j);
                    CMAP(i & 0xf, i, j);
                    break;
                case 8:
                    CMAP(i, i, j);
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

            BWmap = new uint* [256];
            if (BWmap == NULL)
            {
                Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for B&W mapping table");
                return false;
            }

            for (int i = 0; i < 256; i++)
            {
                BWmap[i] = new uint [nsamples];
                if (BWmap[i] == NULL)
                {
                    for (int j = i - 1; i >= 0; i--)
                        delete BWmap[j];

                    delete BWmap;
                    Tiff::ErrorExt(tif, tif->m_clientdata, tif->FileName(), "No space for B&W mapping table");
                    return false;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                int j = 0;
                switch (bitspersample)
                {
                case 1:
                    GREY(i >> 7, i, j);
                    GREY((i >> 6) & 1, i, j);
                    GREY((i >> 5) & 1, i, j);
                    GREY((i >> 4) & 1, i, j);
                    GREY((i >> 3) & 1, i, j);
                    GREY((i >> 2) & 1, i, j);
                    GREY((i >> 1) & 1, i, j);
                    GREY(i & 1, i, j);
                    break;
                case 2:
                    GREY(i >> 6, i, j);
                    GREY((i >> 4) & 3, i, j);
                    GREY((i >> 2) & 3, i, j);
                    GREY(i & 3, i, j);
                    break;
                case 4:
                    GREY(i >> 4, i, j);
                    GREY(i & 0xf, i, j);
                    break;
                case 8:
                case 16:
                    GREY(i, i, j);
                    break;
                }
            }

            return true;
        }

        /*
        * 8-bit packed CIE L*a*b 1976 samples => RGB
        */
        private static void putcontig8bitCIELab(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            fromskew *= 3;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    float X, Y, Z;
                    img->cielab->CIELabToXYZ(pp[ppPos], pp[ppPos + 1], pp[ppPos + 2], X, Y, Z);

                    uint r, g, b;
                    img->cielab->XYZToRGB(X, Y, Z, r, g, b);

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
        private static void putcontig8bitYCbCr44tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
                for (; h >= 4; h -= 4)
                {
                    x = w >> 2;
                    do
                    {
                        int Cb = pp[ppPos + 16];
                        int Cr = pp[ppPos + 17];

                        img->YCbCrtoRGB(cp[cpPos], pp[ppPos + 0], Cb, Cr);
                        img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                        img->YCbCrtoRGB(cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                        img->YCbCrtoRGB(cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp2 + 0], pp[ppPos + 8], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp2 + 1], pp[ppPos + 9], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp2 + 2], pp[ppPos + 10], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp2 + 3], pp[ppPos + 11], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp3 + 0], pp[ppPos + 12], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp3 + 1], pp[ppPos + 13], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp3 + 2], pp[ppPos + 14], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp3 + 3], pp[ppPos + 15], Cb, Cr);

                        cpPos += 4;
                        cp1 += 4;
                        cp2 += 4;
                        cp3 += 4;
                        ppPos += 18;
                    }
                    while (--x);

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
                                img->YCbCrtoRGB(cp[cp3 + 3], pp[ppPos + 15], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp2 + 3], pp[ppPos + 11], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img->YCbCrtoRGB(cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);

                            x_goOn = true;
                        }

                        if (x == 3 || x_goOn)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img->YCbCrtoRGB(cp[cp3 + 2], pp[ppPos + 14], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp2 + 2], pp[ppPos + 10], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img->YCbCrtoRGB(cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);

                            x_goOn = true;
                        }

                        if (x == 2 || x_goOn)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img->YCbCrtoRGB(cp[cp3 + 1], pp[ppPos + 13], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp2 + 1], pp[ppPos + 9], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                        }

                        if (x == 1 || x_goOn)
                        {
                            // order of if's is important
                            h_goOn = false;
                            if (h < 1 || h > 3)
                            {
                                img->YCbCrtoRGB(cp[cp3 + 0], pp[ppPos + 12], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 3 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp2 + 0], pp[ppPos + 8], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 2 || h_goOn)
                            {
                                img->YCbCrtoRGB(cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);
                                h_goOn = true;
                            }

                            if (h == 1 || h_goOn)
                                img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
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
        private static void putcontig8bitYCbCr42tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            int cp1 = cpPos + w + toskew;
            int incr = 2 * toskew + w;

            fromskew = (fromskew * 10) / 4;
            if ((h & 3) == 0 && (w & 1) == 0)
            {
                for (; h >= 2; h -= 2)
                {
                    x = w >> 2;
                    do
                    {
                        int Cb = pp[ppPos + 8];
                        int Cr = pp[ppPos + 9];

                        img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                        img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                        img->YCbCrtoRGB(cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                        img->YCbCrtoRGB(cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);
                        img->YCbCrtoRGB(cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);

                        cpPos += 4;
                        cp1 += 4;
                        ppPos += 10;
                    }
                    while (--x);

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
                                img->YCbCrtoRGB(cp[cp1 + 3], pp[ppPos + 7], Cb, Cr);

                            img->YCbCrtoRGB(cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);
                            x_goOn = true;
                        }

                        if (x == 3 || x_goOn)
                        {
                            if (h != 1)
                                img->YCbCrtoRGB(cp[cp1 + 2], pp[ppPos + 6], Cb, Cr);

                            img->YCbCrtoRGB(cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                            x_goOn = true;
                        }

                        if (x == 2 || x_goOn)
                        {
                            if (h != 1)
                                img->YCbCrtoRGB(cp[cp1 + 1], pp[ppPos + 5], Cb, Cr);

                            img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                            x_goOn = true;
                        }

                        if (x == 1 || x_goOn)
                        {
                            if (h != 1)
                                img->YCbCrtoRGB(cp[cp1 + 0], pp[ppPos + 4], Cb, Cr);

                            img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
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

                        pp += 10;
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
        private static void putcontig8bitYCbCr41tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                    img->YCbCrtoRGB(cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);
                    img->YCbCrtoRGB(cp[cpPos + 3], pp[ppPos + 3], Cb, Cr);

                    cpPos += 4;
                    ppPos += 6;
                }
                while (--x);

                if ((w & 3) != 0)
                {
                    int Cb = pp[ppPos + 4];
                    int Cr = pp[ppPos + 5];

                    uint x = w & 3;
                    if (x == 3)
                        img->YCbCrtoRGB(cp[cpPos + 2], pp[ppPos + 2], Cb, Cr);

                    if (x == 3 || x == 2)
                        img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);

                    if (x == 3 || x == 2 || x == 1)
                        img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);

                    cpPos += x;
                    ppPos += 6;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
            while (--h);
        }

        /*
        * 8-bit packed YCbCr samples w/ 2,2 subsampling => RGB
        */
        private static void putcontig8bitYCbCr22tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
                    uint Cb = pp[ppPos + 4];
                    uint Cr = pp[ppPos + 5];
                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                    img->YCbCrtoRGB(cp[cp2 + 0], pp[ppPos + 2], Cb, Cr);
                    img->YCbCrtoRGB(cp[cp2 + 1], pp[ppPos + 3], Cb, Cr);
                    cpPos += 2;
                    cp2 += 2;
                    ppPos += 6;
                    x -= 2;
                }

                if (x == 1)
                {
                    uint Cb = pp[ppPos + 4];
                    uint Cr = pp[ppPos + 5];
                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img->YCbCrtoRGB(cp[cp2 + 0], pp[ppPos + 2], Cb, Cr);
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
                    uint Cb = pp[ppPos + 4];
                    uint Cr = pp[ppPos + 5];
                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);
                    cpPos += 2;
                    cp2 += 2;
                    ppPos += 6;
                    x -= 2;
                }

                if (x == 1)
                {
                    uint Cb = pp[ppPos + 4];
                    uint Cr = pp[ppPos + 5];
                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                }
            }
        }

        /*
        * 8-bit packed YCbCr samples w/ 2,1 subsampling => RGB
        */
        private static void putcontig8bitYCbCr21tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img->YCbCrtoRGB(cp[cpPos + 1], pp[ppPos + 1], Cb, Cr);

                    cpPos += 2;
                    ppPos += 4;
                }
                while (--x);

                if ((w & 1) != 0)
                {
                    int Cb = pp[ppPos + 2];
                    int Cr = pp[ppPos + 3];

                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);

                    cpPos += 1;
                    ppPos += 4;
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
            while (--h);
        }

        /*
        * 8-bit packed YCbCr samples w/ no subsampling => RGB
        */
        private static void putcontig8bitYCbCr11tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

                    img->YCbCrtoRGB(cp[cpPos], pp[ppPos + 0], Cb, Cr);
                    cpPos++;
                    ppPos += 3;
                }
                while (--x);

                cpPos += toskew;
                ppPos += fromskew;
            }
            while (--h);
        }

        /*
        * 8-bit packed YCbCr samples w/ 1,2 subsampling => RGB
        */
        private static void putcontig8bitYCbCr12tile(TiffRGBAImage img, uint[] cp, int cpOffset, uint x, uint y, uint w, uint h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
                    uint Cb = pp[ppPos + 2];
                    uint Cr = pp[ppPos + 3];
                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    img->YCbCrtoRGB(cp[cp2 + 0], pp[ppPos + 1], Cb, Cr);
                    cpPos++;
                    cp2++;
                    ppPos += 4;
                } while (--x);

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
                    uint Cb = pp[ppPos + 2];
                    uint Cr = pp[ppPos + 3];
                    img->YCbCrtoRGB(cp[cpPos + 0], pp[ppPos + 0], Cb, Cr);
                    cpPos++;
                    ppPos += 4;
                } while (--x);
            }
        }

        /*
        * YCbCr -> RGB conversion and packing routines.
        */
        private void YCbCrtoRGB(out uint dst, uint Y, int Cb, int Cr)
        {
            uint r, g, b;
            ycbcr->YCbCrtoRGB(Y, Cb, Cr, r, g, b);
            dst = PACK(r, g, b);
        }
    }
}
