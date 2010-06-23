/* Copyright (C) 2008-2010, Bit Miracle
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
using System.IO;
using System.Globalization;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.Tiff2Rgba
{
    class Program
    {
        static string[] stuff =
        {
            "usage: tiff2rgba [-c comp] [-r rows] [-b] input... output",
            "where comp is one of the following compression algorithms:",
            " jpeg\t\tJPEG encoding",
            " zip\t\tLempel-Ziv & Welch encoding",
            " lzw\t\tLempel-Ziv & Welch encoding",
            " packbits\tPackBits encoding",
            " none\t\tno compression",
            "and the other options are:",
            " -r\trows/strip",
            " -b (progress by block rather than as a whole image)",
            " -n don't emit alpha component.",
            null
        };

        static Compression compression = Compression.PACKBITS;
        static int rowsperstrip = -1;

        /// <summary>
        /// default is whole image at once
        /// </summary>
        static bool process_by_block;
        static bool no_alpha;

        static void Main(string[] args)
        {
            int argn = 0;
            for (; argn < args.Length; argn++)
            {
                string arg = args[argn];
                if (arg[0] != '-')
                    break;

                string optarg = null;
                if (argn < (args.Length - 1))
                    optarg = args[argn + 1];

                arg = arg.Substring(1);
                switch (arg[0])
                {
                    case 'b':
                        process_by_block = true;
                        break;

                    case 'c':
                        if (optarg == "none")
                            compression = Compression.NONE;
                        else if (optarg == "packbits")
                            compression = Compression.PACKBITS;
                        else if (optarg == "lzw")
                            compression = Compression.LZW;
                        else if (optarg == "jpeg")
                            compression = Compression.JPEG;
                        else if (optarg == "zip")
                            compression = Compression.DEFLATE;
                        else
                        {
                            usage();
                            return;
                        }

                        argn++;
                        break;

                    case 'r':
                    case 't':
                        rowsperstrip = int.Parse(optarg, CultureInfo.InvariantCulture);
                        argn++;
                        break;

                    case 'n':
                        no_alpha = true;
                        break;

                    case '?':
                        usage();
                        return;
                }
            }

            if (args.Length - argn < 2)
            {
                usage();
                return;
            }

            using (Tiff outImage = Tiff.Open(args[args.Length - 1], "w"))
            {
                if (outImage == null)
                    return;

                for (; argn < args.Length - 1; argn++)
                {
                    using (Tiff inImage = Tiff.Open(args[argn], "r"))
                    {
                        if (inImage == null)
                            return;

                        do
                        {
                            if (!tiffcvt(inImage, outImage) || !outImage.WriteDirectory())
                                return;
                        } while (inImage.ReadDirectory());
                    }
                }
            }
        }

        static bool tiffcvt(Tiff inImage, Tiff outImage)
        {
            FieldValue[] result = inImage.GetField(TiffTag.IMAGEWIDTH);
            if (result == null)
                return false;
            int width = result[0].ToInt();
            
            result = inImage.GetField(TiffTag.IMAGELENGTH);
            if (result == null)
                return false;
            int height = result[0].ToInt();

            copyField(inImage, outImage, TiffTag.SUBFILETYPE);
            outImage.SetField(TiffTag.IMAGEWIDTH, width);
            outImage.SetField(TiffTag.IMAGELENGTH, height);
            outImage.SetField(TiffTag.BITSPERSAMPLE, 8);
            outImage.SetField(TiffTag.COMPRESSION, compression);
            outImage.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB);

            copyField(inImage, outImage, TiffTag.FILLORDER);
            outImage.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);

            if (no_alpha)
                outImage.SetField(TiffTag.SAMPLESPERPIXEL, 3);
            else
                outImage.SetField(TiffTag.SAMPLESPERPIXEL, 4);

            if (!no_alpha)
            {
                short[] v = new short[1];
                v[0] = (short)ExtraSample.ASSOCALPHA;
                outImage.SetField(TiffTag.EXTRASAMPLES, 1, v);
            }

            copyField(inImage, outImage, TiffTag.XRESOLUTION);
            copyField(inImage, outImage, TiffTag.YRESOLUTION);
            copyField(inImage, outImage, TiffTag.RESOLUTIONUNIT);
            outImage.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
            outImage.SetField(TiffTag.SOFTWARE, Tiff.GetVersion());
            copyField(inImage, outImage, TiffTag.DOCUMENTNAME);

            if (process_by_block && inImage.IsTiled())
                return (cvt_by_tile(inImage, outImage));
            else if (process_by_block)
                return (cvt_by_strip(inImage, outImage));

            return (cvt_whole_image(inImage, outImage));
        }

        static bool cvt_by_tile(Tiff inImage, Tiff outImage)
        {
        //    uint* raster;			/* retrieve RGBA image */
        //    uint  width, height;		/* image width & height */
        //    uint  tile_width, tile_height;
        //    uint  row, col;
        //    uint  *wrk_line;
        //    tsize_t raster_size;
        //    int	    ok = 1;

        //    TIFFGetField(in, TiffTag.IMAGEWIDTH, &width);
        //    TIFFGetField(in, TiffTag.IMAGELENGTH, &height);

        //    if( !TIFFGetField(in, TiffTag.TILEWIDTH, &tile_width)
        //        || !TIFFGetField(in, TiffTag.TILELENGTH, &tile_height) ) {
        //        TIFFError(TIFFFileName(in), "Source image not tiled");
        //        return (0);
        //    }

        //    TIFFSetField(out, TiffTag.TILEWIDTH, tile_width );
        //    TIFFSetField(out, TiffTag.TILELENGTH, tile_height );

        //    /*
        //     * Allocate tile buffer
        //     */
        //    raster_size = multiply(multiply(tile_width, tile_height), sizeof (uint));
        //    if (!raster_size) {
        //    TIFFError(TIFFFileName(in),
        //          "Can't allocate buffer for raster of size %lux%lu",
        //          (unsigned long) tile_width, (unsigned long) tile_height);
        //    return (0);
        //    }
        //    raster = (uint*)_TIFFmalloc(raster_size);
        //    if (raster == 0) {
        //        TIFFError(TIFFFileName(in), "No space for raster buffer");
        //        return (0);
        //    }

        //    /*
        //     * Allocate a scanline buffer for swapping during the vertical
        //     * mirroring pass.  (Request can't overflow given prior checks.)
        //     */
        //    wrk_line = (uint*)_TIFFmalloc(tile_width * sizeof (uint));
        //    if (!wrk_line) {
        //        TIFFError(TIFFFileName(in), "No space for raster scanline buffer");
        //        ok = 0;
        //    }

        //    /*
        //     * Loop over the tiles.
        //     */
        //    for( row = 0; ok && row < height; row += tile_height )
        //    {
        //        for( col = 0; ok && col < width; col += tile_width )
        //        {
        //            uint i_row;

        //            /* Read the tile into an RGBA array */
        //            if (!TIFFReadRGBATile(in, col, row, raster)) {
        //                ok = 0;
        //                break;
        //            }


        //        /*
        //         * XXX: raster array has 4-byte unsigned integer type, that is why
        //         * we should rearrange it here.
        //         */
        //#if HOST_BIGENDIAN
        //        TIFFSwabArrayOfLong(raster, tile_width * tile_height);
        //#endif

        //            /*
        //             * For some reason the TIFFReadRGBATile() function chooses the
        //             * lower left corner as the origin.  Vertically mirror scanlines.
        //             */
        //            for( i_row = 0; i_row < tile_height / 2; i_row++ )
        //            {
        //                uint	*top_line, *bottom_line;

        //                top_line = raster + tile_width * i_row;
        //                bottom_line = raster + tile_width * (tile_height-i_row-1);

        //                _TIFFmemcpy(wrk_line, top_line, 4*tile_width);
        //                _TIFFmemcpy(top_line, bottom_line, 4*tile_width);
        //                _TIFFmemcpy(bottom_line, wrk_line, 4*tile_width);
        //            }

        //            /*
        //             * Write out the result in a tile.
        //             */

        //            if( TIFFWriteEncodedTile( out,
        //                                      TIFFComputeTile( out, col, row, 0, 0),
        //                                      raster,
        //                                      4 * tile_width * tile_height ) == -1 )
        //            {
        //                ok = 0;
        //                break;
        //            }
        //        }
        //    }

        //    _TIFFfree( raster );
        //    _TIFFfree( wrk_line );

        //    return ok;
            return false;
        }

        static bool cvt_by_strip(Tiff inImage, Tiff outImage)
        {
        //    uint* raster;			/* retrieve RGBA image */
        //    uint  width, height;		/* image width & height */
        //    uint  row;
        //    uint  *wrk_line;
        //    tsize_t raster_size;
        //    int	    ok = 1;

        //    TIFFGetField(in, TiffTag.IMAGEWIDTH, &width);
        //    TIFFGetField(in, TiffTag.IMAGELENGTH, &height);

        //    if( !TIFFGetField(in, TiffTag.ROWSPERSTRIP, &rowsperstrip) ) {
        //        TIFFError(TIFFFileName(in), "Source image not in strips");
        //        return (0);
        //    }

        //    TIFFSetField(out, TiffTag.ROWSPERSTRIP, rowsperstrip);

        //    /*
        //     * Allocate strip buffer
        //     */
        //    raster_size = multiply(multiply(width, rowsperstrip), sizeof (uint));
        //    if (!raster_size) {
        //    TIFFError(TIFFFileName(in),
        //          "Can't allocate buffer for raster of size %lux%lu",
        //          (unsigned long) width, (unsigned long) rowsperstrip);
        //    return (0);
        //    }
        //    raster = (uint*)_TIFFmalloc(raster_size);
        //    if (raster == 0) {
        //        TIFFError(TIFFFileName(in), "No space for raster buffer");
        //        return (0);
        //    }

        //    /*
        //     * Allocate a scanline buffer for swapping during the vertical
        //     * mirroring pass.  (Request can't overflow given prior checks.)
        //     */
        //    wrk_line = (uint*)_TIFFmalloc(width * sizeof (uint));
        //    if (!wrk_line) {
        //        TIFFError(TIFFFileName(in), "No space for raster scanline buffer");
        //        ok = 0;
        //    }

        //    /*
        //     * Loop over the strips.
        //     */
        //    for( row = 0; ok && row < height; row += rowsperstrip )
        //    {
        //        int	rows_to_write, i_row;

        //        /* Read the strip into an RGBA array */
        //        if (!TIFFReadRGBAStrip(in, row, raster)) {
        //            ok = 0;
        //            break;
        //        }

        //    /*
        //     * XXX: raster array has 4-byte unsigned integer type, that is why
        //     * we should rearrange it here.
        //     */
        //#if HOST_BIGENDIAN
        //    TIFFSwabArrayOfLong(raster, width * rowsperstrip);
        //#endif

        //        /*
        //         * Figure out the number of scanlines actually in this strip.
        //         */
        //        if( row + rowsperstrip > height )
        //            rows_to_write = height - row;
        //        else
        //            rows_to_write = rowsperstrip;

        //        /*
        //         * For some reason the TIFFReadRGBAStrip() function chooses the
        //         * lower left corner as the origin.  Vertically mirror scanlines.
        //         */

        //        for( i_row = 0; i_row < rows_to_write / 2; i_row++ )
        //        {
        //            uint	*top_line, *bottom_line;

        //            top_line = raster + width * i_row;
        //            bottom_line = raster + width * (rows_to_write-i_row-1);

        //            _TIFFmemcpy(wrk_line, top_line, 4*width);
        //            _TIFFmemcpy(top_line, bottom_line, 4*width);
        //            _TIFFmemcpy(bottom_line, wrk_line, 4*width);
        //        }

        //        /*
        //         * Write out the result in a strip
        //         */

        //        if( TIFFWriteEncodedStrip( out, row / rowsperstrip, raster,
        //                                   4 * rows_to_write * width ) == -1 )
        //        {
        //            ok = 0;
        //            break;
        //        }
        //    }

        //    _TIFFfree( raster );
        //    _TIFFfree( wrk_line );

        //    return ok;
            return false;
        }

        ///*
        // * cvt_whole_image()
        // *
        // * read the whole image into one big RGBA buffer and then write out
        // * strips from that.  This is using the traditional TIFFReadRGBAImage()
        // * API that we trust.
        // */
        static bool cvt_whole_image(Tiff inImage, Tiff outImage)
        {
        //    uint* raster;			/* retrieve RGBA image */
        //    uint  width, height;		/* image width & height */
        //    uint  row;
        //    size_t pixel_count;

        //    TIFFGetField(in, TiffTag.IMAGEWIDTH, &width);
        //    TIFFGetField(in, TiffTag.IMAGELENGTH, &height);
        //    pixel_count = width * height;

        //    /* XXX: Check the integer overflow. */
        //    if (!width || !height || pixel_count / width != height) {
        //        TIFFError(TIFFFileName(in),
        //          "Malformed input file; can't allocate buffer for raster of %lux%lu size",
        //          (unsigned long)width, (unsigned long)height);
        //        return 0;
        //    }

        //    rowsperstrip = TIFFDefaultStripSize(out, rowsperstrip);
        //    TIFFSetField(out, TiffTag.ROWSPERSTRIP, rowsperstrip);

        //    raster = (uint*)_TIFFCheckMalloc(in, pixel_count, sizeof(uint), "raster buffer");
        //    if (raster == 0) {
        //        TIFFError(TIFFFileName(in), "Requested buffer size is %lu elements %lu each",
        //          (unsigned long)pixel_count, (unsigned long)sizeof(uint));
        //        return (0);
        //    }

        //    /* Read the image in one chunk into an RGBA array */
        //    if (!TIFFReadRGBAImageOriented(in, width, height, raster,
        //                                   ORIENTATION_TOPLEFT, 0)) {
        //        _TIFFfree(raster);
        //        return (0);
        //    }

        //    /*
        //     * XXX: raster array has 4-byte unsigned integer type, that is why
        //     * we should rearrange it here.
        //     */
        //#if HOST_BIGENDIAN
        //    TIFFSwabArrayOfLong(raster, width * height);
        //#endif

        //    /*
        //     * Do we want to strip away alpha components?
        //     */
        //    if (no_alpha)
        //    {
        //        size_t count = pixel_count;
        //        unsigned char *src, *dst;

        //    src = dst = (unsigned char *) raster;
        //        while (count > 0)
        //        {
        //        *(dst++) = *(src++);
        //        *(dst++) = *(src++);
        //        *(dst++) = *(src++);
        //        src++;
        //        count--;
        //        }
        //    }

        //    /*
        //     * Write out the result in strips
        //     */
        //    for (row = 0; row < height; row += rowsperstrip)
        //    {
        //        unsigned char * raster_strip;
        //        int	rows_to_write;
        //        int	bytes_per_pixel;

        //        if (no_alpha)
        //        {
        //            raster_strip = ((unsigned char *) raster) + 3 * row * width;
        //            bytes_per_pixel = 3;
        //        }
        //        else
        //        {
        //            raster_strip = (unsigned char *) (raster + row * width);
        //            bytes_per_pixel = 4;
        //        }

        //        if( row + rowsperstrip > height )
        //            rows_to_write = height - row;
        //        else
        //            rows_to_write = rowsperstrip;

        //        if( TIFFWriteEncodedStrip( out, row / rowsperstrip, raster_strip,
        //                             bytes_per_pixel * rows_to_write * width ) == -1 )
        //        {
        //            _TIFFfree( raster );
        //            return 0;
        //        }
        //    }

        //    _TIFFfree( raster );

            return true;
        }        

        private static void usage()
        {
            using (TextWriter stderr = Console.Error)
            {
                stderr.Write("{0}\n\n", Tiff.GetVersion());
                for (int i = 0; stuff[i] != null; i++)
                    stderr.Write("{0}\n", stuff[i]);
            }
        }

        private static void copyField(Tiff inImage, Tiff outImage, TiffTag tag)
        {
            FieldValue[] result = inImage.GetField(tag);
            if (result != null)
                outImage.SetField(tag, result[0]);
        }
    }
}
