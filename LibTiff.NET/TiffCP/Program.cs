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

/*
 *  Revised:  2/18/01 BAR -- added syntax for extracting single images from
 *                          multi-image TIFF files.
 *
 *    New syntax is:  sourceFileName,image#
 *
 * image# ranges from 0..<n-1> where n is the # of images in the file.
 * There may be no white space between the comma and the filename or
 * image number.
 *
 *    Example:   tiffcp source.tif,1 destination.tif
 *
 * Copies the 2nd image in source.tif to the destination.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

using BitMiracle.LibTiff;

namespace BitMiracle.TiffCP
{
    class Program
    {
        struct tagToCopy
        {
            public tagToCopy(TIFFTAG _tag, short _count, TiffDataType _type)
            {
                tag = _tag;
                count = _count;
                type = _type;
            }

            public TIFFTAG tag;
            public short count;
            public TiffDataType type;
        };

        static int g_outtiled = -1;
        static int g_tilewidth;
        static int g_tilelength;
        static PLANARCONFIG g_config;
        static COMPRESSION g_compression;
        static short g_predictor;
        static FILLORDER g_fillorder;
        static UInt16 g_orientation;
        static uint g_rowsperstrip;
        static GROUP3OPT g_g3opts;
        static bool g_ignore = false; /* if true, ignore read errors */
        static GROUP3OPT g_defg3opts = (GROUP3OPT)(-1);
        static int g_quality = 75; /* JPEG quality */
        static JPEGCOLORMODE g_jpegcolormode = JPEGCOLORMODE.JPEGCOLORMODE_RGB;
        static COMPRESSION g_defcompression = (COMPRESSION)(-1);
        static short g_defpredictor = -1;
        static char g_comma = ','; /* (default) comma separator character */
        static Tiff g_bias = null;
        static int g_pageNum = 0;
        static string[] g_stuff = 
        {
            "usage: tiffcp [options] input... output", 
            "where options are:",
            " -a		append to output instead of overwriting", 
            " -o offset	set initial directory offset", 
            " -p contig	pack samples contiguously (e.g. RGBRGB...)", 
            " -p separate	store samples separately (e.g. RRR...GGG...BBB...)", 
            " -s		write output in strips", 
            " -t		write output in tiles", 
            " -i		ignore read errors", 
            " -b file[,#]	bias (dark) monochrome image to be subtracted from all others", 
            " -,=%		use % rather than , to separate image #'s (per Note below)", 
            "", 
            " -r #		make each strip have no more than # rows", 
            " -w #		set output tile width (pixels)", 
            " -l #		set output tile length (pixels)", 
            "", 
            " -f lsb2msb	force lsb-to-msb FillOrder for output", 
            " -f msb2lsb	force msb-to-lsb FillOrder for output", 
            "", 
            " -c lzw[:opts]	compress output with Lempel-Ziv & Welch encoding", 
            " -c zip[:opts]	compress output with deflate encoding", 
            " -c jpeg[:opts]	compress output with JPEG encoding", 
            " -c packbits	compress output with packbits encoding", 
            " -c g3[:opts]	compress output with CCITT Group 3 encoding", 
            " -c g4		compress output with CCITT Group 4 encoding", 
            " -c none	use no compression algorithm on output", 
            "", 
            "Group 3 options:", 
            " 1d		use default CCITT Group 3 1D-encoding", 
            " 2d		use optional CCITT Group 3 2D-encoding", 
            " fill		byte-align EOL codes", 
            "For example, -c g3:2d:fill to get G3-2D-encoded data with byte-aligned EOLs", 
            "", 
            "JPEG options:", 
            " #		set compression quality level (0-100, default 75)", 
            " r		output color image as RGB rather than YCbCr", 
            "For example, -c jpeg:r:50 to get JPEG-encoded RGB data with 50% comp. quality", 
            "", 
            "LZW and deflate options:", 
            " #		set predictor value",
            "For example, -c lzw:2 to get LZW-encoded data with horizontal differencing",
            "",
            "Note that input filenames may be of the form filename,x,y,z",
            "where x, y, and z specify image numbers in the filename to copy.",
            "example:  tiffcp -c none -b esp.tif,1 esp.tif,0 test.tif",
            "  subtract 2nd image in esp.tif from 1st yielding uncompressed result test.tif",
            null
        };

        tagToCopy[] g_tags = 
        {
            new tagToCopy(TIFFTAG.TIFFTAG_SUBFILETYPE, 1, TiffDataType.TIFF_LONG), 
            new tagToCopy(TIFFTAG.TIFFTAG_THRESHHOLDING, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_DOCUMENTNAME, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_IMAGEDESCRIPTION, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_MAKE, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_MODEL, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_MINSAMPLEVALUE, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_MAXSAMPLEVALUE, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_XRESOLUTION, 1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_YRESOLUTION, 1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_PAGENAME, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_XPOSITION, 1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_YPOSITION, 1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_RESOLUTIONUNIT, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_SOFTWARE, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_DATETIME, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_ARTIST, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_HOSTCOMPUTER, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_WHITEPOINT, -1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_PRIMARYCHROMATICITIES, -1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_HALFTONEHINTS, 2, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_INKSET, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_DOTRANGE, 2, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_TARGETPRINTER, 1, TiffDataType.TIFF_ASCII), 
            new tagToCopy(TIFFTAG.TIFFTAG_SAMPLEFORMAT, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_YCBCRCOEFFICIENTS, -1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_YCBCRSUBSAMPLING, 2, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_YCBCRPOSITIONING, 1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_REFERENCEBLACKWHITE, -1, TiffDataType.TIFF_RATIONAL), 
            new tagToCopy(TIFFTAG.TIFFTAG_EXTRASAMPLES, -1, TiffDataType.TIFF_SHORT), 
            new tagToCopy(TIFFTAG.TIFFTAG_SMINSAMPLEVALUE, 1, TiffDataType.TIFF_DOUBLE), 
            new tagToCopy(TIFFTAG.TIFFTAG_SMAXSAMPLEVALUE, 1, TiffDataType.TIFF_DOUBLE), 
            new tagToCopy(TIFFTAG.TIFFTAG_STONITS, 1, TiffDataType.TIFF_DOUBLE), 
        };

        static void Main(string[] args)
        {
            char[] mode = new char[10];
            mode[0] = 'w';
            int mp = 1;

            FILLORDER deffillorder = 0;
            int deftilelength = -1;
            int diroff = 0;
            PLANARCONFIG defconfig = (PLANARCONFIG)(-1);
            uint defrowsperstrip = 0;
            int deftilewidth = -1;

            Stream stderr = Console.OpenStandardError();
            int argn = 0;
            for ( ; argn < args.Length; argn++)
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
                    case ',':
                        if (arg[1] != '=')
                            usage();

                        g_comma = arg[2];
                        break;
                    case 'b':
                        /* this file is bias image subtracted from others */
                        if (g_bias != null)
                        {
                            fputs("Only 1 bias image may be specified\n", stderr);
                            return;
                        }
                        
                        g_bias = openSrcImage(ref args[argn + 1]);
                        if (g_bias == null)
                            return;

                        if (g_bias.IsTiled())
                        {
                            fputs("Bias image must be organized in strips\n", stderr);
                            return;
                        }

                        object[] result = g_bias.GetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL);
                        short samples = (short)result[0];
                        if (samples != 1)
                        {
                            fputs("Bias image must be monochrome\n", stderr);
                            return;
                        }

                        break;
                    case 'a':
                        /* append to output */
                        mode[0] = 'a';
                        break;
                    case 'c':
                        /* compression scheme */
                        if (!processCompressOptions(optarg))
                            usage();
                        break;
                    case 'f':
                        /* fill order */
                        if (optarg == "lsb2msb")
                            deffillorder = FILLORDER.FILLORDER_LSB2MSB;
                        else if (optarg == "msb2lsb")
                            deffillorder = FILLORDER.FILLORDER_MSB2LSB;
                        else
                            usage();
                        break;
                    case 'i':
                        /* ignore errors */
                        g_ignore = true;
                        break;
                    case 'l':
                        /* tile length */
                        g_outtiled = 1;
                        deftilelength = int.Parse(optarg, CultureInfo.InvariantCulture);
                        break;
                    case 'o':
                        /* initial directory offset */
                        diroff = int.Parse(optarg, CultureInfo.InvariantCulture);
                        break;
                    case 'p':
                        /* planar configuration */
                        if (optarg == "separate")
                            defconfig = PLANARCONFIG.PLANARCONFIG_SEPARATE;
                        else if (optarg == "contig")
                            defconfig = PLANARCONFIG.PLANARCONFIG_CONTIG;
                        else
                            usage();
                        break;
                    case 'r':
                        /* rows/strip */
                        defrowsperstrip = uint.Parse(optarg, CultureInfo.InvariantCulture);
                        break;
                    case 's':
                        /* generate stripped output */
                        g_outtiled = 0;
                        break;
                    case 't':
                        /* generate tiled output */
                        g_outtiled = 1;
                        break;
                    case 'w':
                        /* tile width */
                        g_outtiled = 1;
                        deftilewidth = int.Parse(optarg, CultureInfo.InvariantCulture);
                        break;
                    case 'B':
                        mode[mp++] = 'b';
                        break;
                    case 'L':
                        mode[mp++] = 'l';
                        break;
                    case 'M':
                        mode[mp++] = 'm';
                        break;
                    case 'C':
                        mode[mp++] = 'c';
                        break;
                    case '?':
                        usage();
                        break;
                }
            }

            if (args.Length - argn < 2)
                usage();

            string smode = new string(mode, 0, mp);
            Tiff outImage = Tiff.Open(args[args.Length - 1], smode);
            if (outImage == null)
                return;

            if ((args.Length - argn) == 2)
                g_pageNum = -1;

            for ( ; argn < args.Length - 1; argn++)
            {
                string imageCursor = args[argn];
                Tiff inImage = openSrcImage(ref imageCursor);
                if (inImage == null)
                    return;

                if (diroff != 0 && !inImage.SetSubDirectory(diroff))
                {
                    Tiff.Error(inImage.FileName(), "Error, setting subdirectory at %#x", diroff);
                    return;
                }

                for ( ; ; )
                {
                    g_config = defconfig;
                    g_compression = g_defcompression;
                    g_predictor = g_defpredictor;
                    g_fillorder = deffillorder;
                    g_rowsperstrip = defrowsperstrip;
                    g_tilewidth = deftilewidth;
                    g_tilelength = deftilelength;
                    g_g3opts = g_defg3opts;
                    
                    if (!tiffcp(inImage, outImage) || !outImage.WriteDirectory())
                        return;
                    
                    if (imageCursor != null)
                    {
                         /* seek next image directory */
                        if (!nextSrcImage(inImage, ref imageCursor))
                            break;
                    }
                    else
                    {
                        if (!inImage.ReadDirectory())
                            break;
                    }
                }
            }
        }

        static void usage()
        {
            Stream stderr = Console.OpenStandardError();
            Tiff.fprintf(stderr, "%s\n\n", Tiff.GetVersion());
            for (int i = 0; g_stuff[i] != null; i++)
                Tiff.fprintf(stderr, "%s\n", g_stuff[i]);

            throw new Exception();
        }

        /*
        imageSpec points to a pointer to a filename followed by optional ,image#'s
        Open the TIFF file and assign *imageSpec to either null if there are
        no images specified, or a pointer to the next image number text
        */
        static Tiff openSrcImage(ref string imageSpec)
        {
            string fn = imageSpec;
            int n = fn.IndexOf(g_comma);
            if (n != -1)
                imageSpec = fn.Substring(0, n);
            else
                imageSpec = null;

            Tiff tif = null;
            if (imageSpec != null)
            {
                /* there is at least one image number specifier */
                tif = Tiff.Open(imageSpec, "r");
                
                /* but, ignore any single trailing comma */
                if (n == fn.Length - 1)
                {
                    imageSpec = null;
                    return tif;
                }

                if (tif != null)
                {
                    imageSpec = fn.Substring(n);
                    if (!nextSrcImage(tif, ref imageSpec))
                        tif = null;
                }
            }
            else
                tif = Tiff.Open(fn, "r");

            return tif;
        }

        /*
        seek to the next image specified in imageSpec
        returns 1 if success, 0 if no more images to process
        imageSpec=NULL if subsequent images should be processed in sequence
        */
        static bool nextSrcImage(Tiff tif, ref string imageSpec)
        {
            //if (imageSpec[0] == g_comma)
            //{
            //     /* if not @comma, we've done all images */
            //    string start = imageSpec.Substring(1);
            //    UInt16 nextImage = (UInt16)strtol(start, &imageSpec, 0);
                
            //    if (start == imageSpec)
            //        nextImage = tif.CurrentDirectory();

            //    if (imageSpec[0] != 0)
            //    {
            //        if (imageSpec[0] == g_comma)
            //        {
            //            /* a trailing comma denotes remaining images in sequence */
            //            if (imageSpec[1] == '\0')
            //                imageSpec = NULL;
            //        }
            //        else
            //        {
            //            fprintf(stderr, "Expected a %c separated image # list after %s\n", g_comma, tif.FileName());
            //            exit(-4); /* syntax error */
            //        }
            //    }

            //    if (tif.SetDirectory(nextImage))
            //        return true;
                
            //    fprintf(stderr, "%s%c%d not found!\n", tif.FileName(), g_comma, nextImage);
            //}

            return false;
        }

        static void fputs(string s, Stream stream)
        {
            byte[] bytes = Encoding.Default.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }

        static bool processCompressOptions(string opt)
        {
            if (opt == "none")
            {
                g_defcompression = COMPRESSION.COMPRESSION_NONE;
            }
            else if (opt == "packbits")
            {
                g_defcompression = COMPRESSION.COMPRESSION_PACKBITS;
            }
            else if (opt.StartsWith("jpeg"))
            {
                g_defcompression = COMPRESSION.COMPRESSION_JPEG;

                string[] options = opt.Split(new char[] { ':' });
                for (int i = 1; i < options.Length; i++)
                {
                    if (char.IsDigit(options[i][0]))
                        g_quality = int.Parse(options[i], CultureInfo.InvariantCulture);
                    else if (options[i] == "r")
                        g_jpegcolormode = JPEGCOLORMODE.JPEGCOLORMODE_RAW;
                    else
                        usage();
                }
            }
            else if (opt.StartsWith("g3"))
            {
                processG3Options(opt);
                g_defcompression = COMPRESSION.COMPRESSION_CCITTFAX3;
            }
            else if (opt == "g4")
            {
                g_defcompression = COMPRESSION.COMPRESSION_CCITTFAX4;
            }
            else if (opt.StartsWith("lzw"))
            {
                int n = opt.IndexOf(':');
                if (n != -1)
                    g_defpredictor = short.Parse(opt.Substring(n), CultureInfo.InvariantCulture);

                g_defcompression = COMPRESSION.COMPRESSION_LZW;
            }
            else if (opt.StartsWith("zip"))
            {
                int n = opt.IndexOf(':');
                if (n != -1)
                    g_defpredictor = short.Parse(opt.Substring(n), CultureInfo.InvariantCulture);

                g_defcompression = COMPRESSION.COMPRESSION_ADOBE_DEFLATE;
            }
            else
                return false;

            return true;
        }

        static void processG3Options(string cp)
        {
            string[] options = cp.Split(new char[] { ':' });
            if (options.Length > 1)
            {
                if (g_defg3opts == (GROUP3OPT)(-1))
                    g_defg3opts = 0;

                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i].StartsWith("1d"))
                        g_defg3opts &= ~GROUP3OPT.GROUP3OPT_2DENCODING;
                    else if (options[i].StartsWith("2d"))
                        g_defg3opts |= GROUP3OPT.GROUP3OPT_2DENCODING;
                    else if (options[i].StartsWith("fill"))
                        g_defg3opts |= GROUP3OPT.GROUP3OPT_FILLBITS;
                    else
                        usage();
                }
            }
        }

        static bool tiffcp(Tiff inImage, Tiff outImage)
        {
            uint width;
            if (inImage.GetField(TIFFTAG.TIFFTAG_IMAGEWIDTH, &width))
                outImage.SetField(TIFFTAG.TIFFTAG_IMAGEWIDTH, width);

            uint length;
            if (inImage.GetField(TIFFTAG.TIFFTAG_IMAGELENGTH, &length))
                outImage.SetField(TIFFTAG.TIFFTAG_IMAGELENGTH, length);

            UInt16 bitspersample;
            if (inImage.GetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE, &bitspersample))
                outImage.SetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE, bitspersample);

            UInt16 samplesperpixel;
            if (inImage.GetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL, &samplesperpixel))
                outImage.SetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL, samplesperpixel);
            
            if (g_compression != (UInt16)-1)
                outImage.SetField(TIFFTAG.TIFFTAG_COMPRESSION, g_compression);
            else
            {
                if (inImage.GetField(TIFFTAG.TIFFTAG_COMPRESSION, &g_compression))
                    outImage.SetField(TIFFTAG.TIFFTAG_COMPRESSION, g_compression);
            }

            if (g_compression == COMPRESSION_JPEG)
            {
                UInt16 input_compression;
                if (inImage.GetField(TIFFTAG.TIFFTAG_COMPRESSION, &input_compression) && input_compression == COMPRESSION_JPEG)
                    inImage.SetField(TIFFTAG.TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE_RGB);
                
                UInt16 input_photometric;
                if (inImage.GetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, &input_photometric))
                {
                    if (input_photometric == PHOTOMETRIC_RGB)
                    {
                        if (g_jpegcolormode == JPEGCOLORMODE_RGB)
                            outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, PHOTOMETRIC_YCBCR);
                        else
                            outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, PHOTOMETRIC_RGB);
                    }
                    else
                        outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, input_photometric);
                }
            }
            else if (g_compression == COMPRESSION_SGILOG || g_compression == COMPRESSION_SGILOG24)
                outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, samplesperpixel == 1 ? PHOTOMETRIC_LOGL: PHOTOMETRIC_LOGLUV);
            else
                CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_PHOTOMETRIC, 1, TIFF_SHORT);

            if (g_fillorder != 0)
                outImage.SetField(TIFFTAG.TIFFTAG_FILLORDER, g_fillorder);
            else
                CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FILLORDER, 1, TIFF_SHORT);

            /*
             * Will copy `Orientation' tag from input image
             */
            inImage.GetFieldDefaulted(TIFFTAG.TIFFTAG_ORIENTATION, &g_orientation);
            switch (g_orientation)
            {
                case ORIENTATION_BOTRIGHT:
                case ORIENTATION_RIGHTBOT:
                    Tiff.Warning(inImage.FileName(), "using bottom-left orientation");
                    g_orientation = ORIENTATION_BOTLEFT;
                    break;

                case ORIENTATION_LEFTBOT:
                case ORIENTATION_BOTLEFT:
                    break;

                case ORIENTATION_TOPRIGHT:
                case ORIENTATION_RIGHTTOP:
                default:
                    Tiff.Warning(inImage.FileName(), "using top-left orientation");
                    g_orientation = ORIENTATION_TOPLEFT;
                    break;

                case ORIENTATION_LEFTTOP:
                case ORIENTATION_TOPLEFT:
                    break;
            }

            outImage.SetField(TIFFTAG.TIFFTAG_ORIENTATION, g_orientation);
            
            /*
             * Choose tiles/strip for the output image according to
             * the command line arguments (-tiles, -strips) and the
             * structure of the input image.
             */
            if (g_outtiled == -1)
                g_outtiled = inImage.IsTiled();

            if (g_outtiled != 0)
            {
                /*
                 * Setup output file's tile width&height.  If either
                 * is not specified, use either the value from the
                 * input image or, if nothing is defined, use the
                 * library default.
                 */
                if (g_tilewidth == (uint)-1)
                    inImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH, &g_tilewidth);

                if (g_tilelength == (uint)-1)
                    inImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH, &g_tilelength);
                
                outImage.DefaultTileSize(g_tilewidth, g_tilelength);
                outImage.SetField(TIFFTAG.TIFFTAG_TILEWIDTH, g_tilewidth);
                outImage.SetField(TIFFTAG.TIFFTAG_TILELENGTH, g_tilelength);
            }
            else
            {
                /*
                 * RowsPerStrip is left unspecified: use either the
                 * value from the input image or, if nothing is defined,
                 * use the library default.
                 */
                if (g_rowsperstrip == 0)
                {
                    if (!inImage.GetField(TIFFTAG.TIFFTAG_ROWSPERSTRIP, &g_rowsperstrip))
                        g_rowsperstrip = outImage.DefaultStripSize(g_rowsperstrip);

                    if (g_rowsperstrip > length && g_rowsperstrip != (uint)-1)
                        g_rowsperstrip = length;
                }
                else if (g_rowsperstrip == (uint)-1)
                    g_rowsperstrip = length;

                outImage.SetField(TIFFTAG.TIFFTAG_ROWSPERSTRIP, g_rowsperstrip);
            }

            if (g_config != (UInt16)-1)
                outImage.SetField(TIFFTAG.TIFFTAG_PLANARCONFIG, g_config);
            else
            {
                if (inImage.GetField(TIFFTAG.TIFFTAG_PLANARCONFIG, &g_config))
                    outImage.SetField(TIFFTAG.TIFFTAG_PLANARCONFIG, g_config);
            }

            if (samplesperpixel <= 4)
                CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_TRANSFERFUNCTION, 4, TIFF_SHORT);

            CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_COLORMAP, 4, TIFF_SHORT);
            
            /* SMinSampleValue & SMaxSampleValue */
            switch (g_compression)
            {
                case COMPRESSION_JPEG:
                    outImage.SetField(TIFFTAG.TIFFTAG_JPEGQUALITY, g_quality);
                    outImage.SetField(TIFFTAG.TIFFTAG_JPEGCOLORMODE, g_jpegcolormode);
                    break;
                case COMPRESSION_LZW:
                case COMPRESSION_ADOBE_DEFLATE:
                case COMPRESSION_DEFLATE:
                    if (g_predictor != (UInt16)-1)
                        outImage.SetField(TIFFTAG.TIFFTAG_PREDICTOR, g_predictor);
                    else
                    {
                        if (inImage.GetField(TIFFTAG.TIFFTAG_PREDICTOR, &g_predictor))
                            outImage.SetField(TIFFTAG.TIFFTAG_PREDICTOR, g_predictor);
                    }
                    break;
                case COMPRESSION_CCITTFAX3:
                case COMPRESSION_CCITTFAX4:
                    if (g_compression == COMPRESSION_CCITTFAX3)
                    {
                        if (g_g3opts != (uint)-1)
                            outImage.SetField(TIFFTAG.TIFFTAG_GROUP3OPTIONS, g_g3opts);
                        else
                        {
                            if (inImage.GetField(TIFFTAG.TIFFTAG_GROUP3OPTIONS, &g_g3opts))
                                outImage.SetField(TIFFTAG.TIFFTAG_GROUP3OPTIONS, g_g3opts);
                        }
                    }
                    else
                        CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_GROUP4OPTIONS, 1, TIFF_LONG);

                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_BADFAXLINES, 1, TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_CLEANFAXDATA, 1, TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_CONSECUTIVEBADFAXLINES, 1, TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FAXRECVPARAMS, 1, TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FAXRECVTIME, 1, TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FAXSUBADDRESS, 1, TIFF_ASCII);
                    break;
            }

            uint len32;
            void** data;
            if (inImage.GetField(TIFFTAG.TIFFTAG_ICCPROFILE, &len32, &data))
                outImage.SetField(TIFFTAG.TIFFTAG_ICCPROFILE, len32, data);

            UInt16 ninks;
            if (inImage.GetField(TIFFTAG.TIFFTAG_NUMBEROFINKS, &ninks))
            {
                outImage.SetField(TIFFTAG.TIFFTAG_NUMBEROFINKS, ninks);

                string inknames;
                if (inImage.GetField(TIFFTAG.TIFFTAG_INKNAMES, &inknames))
                {
                    int inknameslen = strlen(inknames) + 1;
                    const char* cp = inknames;
                    while (ninks > 1)
                    {
                        cp = strchr(cp, '\0');
                        if (cp != NULL)
                        {
                            cp++;
                            inknameslen += (strlen(cp) + 1);
                        }
                        ninks--;
                    }
                    outImage.SetField(TIFFTAG.TIFFTAG_INKNAMES, inknameslen, inknames);
                }
            }

            ushort pg0;
            ushort pg1;
            if (inImage.GetField(TIFFTAG.TIFFTAG_PAGENUMBER, &pg0, &pg1))
            {
                if (g_pageNum < 0)
                {
                    /* only one input file */
                    outImage.SetField(TIFFTAG.TIFFTAG_PAGENUMBER, pg0, pg1);
                }
                else
                    outImage.SetField(TIFFTAG.TIFFTAG_PAGENUMBER, g_pageNum++, 0);
            }

            int NTAGS = g_tags.Length;
            for (int i = 0; i < NTAGS; i++)
            {
                tagToCopy p = g_tags[i];
                CopyTag(inImage, outImage, p.tag, p.count, p.type);
            }

            return pickFuncAndCopy(inImage, outImage, bitspersample, samplesperpixel, length, width);
        }

    }
}
