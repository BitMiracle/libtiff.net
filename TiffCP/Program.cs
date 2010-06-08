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
using System.Diagnostics;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.TiffCP
{
    public class Program
    {
        static string[] m_stuff = 
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

        public static void Main(string[] args)
        {
            Copier c = new Copier();

            char[] mode = new char[10];
            mode[0] = 'w';
            int mp = 1;

            char comma = ','; /* (default) comma separator character */
            FillOrder deffillorder = 0;
            int deftilelength = -1;
            int diroff = 0;
            PlanarConfig defconfig = PlanarConfig.UNKNOWN;
            int defrowsperstrip = 0;
            int deftilewidth = -1;

            int argn = 0;
            using (TextWriter stderr = Console.Error)
            {
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
                        case ',':
                            if (arg[1] != '=')
                            {
                                usage();
                                return;
                            }

                            comma = arg[2];
                            break;
                        case 'b':
                            /* this file is bias image subtracted from others */
                            if (c.m_bias != null)
                            {
                                stderr.Write("Only 1 bias image may be specified\n");
                                return;
                            }

                            string[] fileAndPageNums = args[argn + 1].Split(new char[] { comma });
                            int pageNumberIndex = 1;
                            openSrcImage(ref c.m_bias, fileAndPageNums, ref pageNumberIndex, comma);
                            if (c.m_bias == null)
                                return;

                            if (c.m_bias.IsTiled())
                            {
                                stderr.Write("Bias image must be organized in strips\n");
                                return;
                            }

                            FieldValue[] result = c.m_bias.GetField(TiffTag.SAMPLESPERPIXEL);
                            short samples = result[0].ToShort();
                            if (samples != 1)
                            {
                                stderr.Write("Bias image must be monochrome\n");
                                return;
                            }

                            argn++;
                            break;
                        case 'a':
                            /* append to output */
                            mode[0] = 'a';
                            break;
                        case 'c':
                            /* compression scheme */
                            if (!c.ProcessCompressOptions(optarg))
                            {
                                usage();
                                return;
                            }

                            argn++;
                            break;
                        case 'f':
                            /* fill order */
                            if (optarg == "lsb2msb")
                                deffillorder = FillOrder.LSB2MSB;
                            else if (optarg == "msb2lsb")
                                deffillorder = FillOrder.MSB2LSB;
                            else
                            {
                                usage();
                                return;
                            }

                            argn++;
                            break;
                        case 'i':
                            /* ignore errors */
                            c.m_ignore = true;
                            break;
                        case 'l':
                            /* tile length */
                            c.m_outtiled = 1;
                            deftilelength = int.Parse(optarg, CultureInfo.InvariantCulture);
                            argn++;
                            break;
                        case 'o':
                            /* initial directory offset */
                            diroff = int.Parse(optarg, CultureInfo.InvariantCulture);
                            break;
                        case 'p':
                            /* planar configuration */
                            if (optarg == "separate")
                                defconfig = PlanarConfig.SEPARATE;
                            else if (optarg == "contig")
                                defconfig = PlanarConfig.CONTIG;
                            else
                            {
                                usage();
                                return;
                            }

                            argn++;
                            break;
                        case 'r':
                            /* rows/strip */
                            defrowsperstrip = int.Parse(optarg, CultureInfo.InvariantCulture);
                            argn++;
                            break;
                        case 's':
                            /* generate stripped output */
                            c.m_outtiled = 0;
                            break;
                        case 't':
                            /* generate tiled output */
                            c.m_outtiled = 1;
                            break;
                        case 'w':
                            /* tile width */
                            c.m_outtiled = 1;
                            deftilewidth = int.Parse(optarg, CultureInfo.InvariantCulture);
                            argn++;
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
                            return;
                    }
                }
            }

            if (args.Length - argn < 2)
            {
                usage();
                return;
            }

            string smode = new string(mode, 0, mp);
            using (Tiff outImage = Tiff.Open(args[args.Length - 1], smode))
            {
                if (outImage == null)
                    return;

                if ((args.Length - argn) == 2)
                    c.m_pageNum = -1;

                for (; argn < args.Length - 1; argn++)
                {
                    string[] fileAndPageNums = args[argn].Split(new char[] { comma });
                    int pageNumberIndex = 1;
                    Tiff inImage = null;
                    try
                    {
                        openSrcImage(ref inImage, fileAndPageNums, ref pageNumberIndex, comma);
                        if (inImage == null)
                            return;

                        if (diroff != 0 && !inImage.SetSubDirectory(diroff))
                        {
                            Tiff.Error(inImage.FileName(), "Error, setting subdirectory at 0x{0:x}", diroff);
                            inImage.Dispose();
                            break;
                        }

                        for ( ; ; )
                        {
                            c.m_config = defconfig;
                            c.m_compression = c.m_defcompression;
                            c.m_predictor = c.m_defpredictor;
                            c.m_fillorder = deffillorder;
                            c.m_rowsperstrip = defrowsperstrip;
                            c.m_tilewidth = deftilewidth;
                            c.m_tilelength = deftilelength;
                            c.m_g3opts = c.m_defg3opts;

                            if (!c.Copy(inImage, outImage) || !outImage.WriteDirectory())
                            {
                                inImage.Dispose();
                                return;
                            }

                            /* seek next image directory */
                            if (!openSrcImage(ref inImage, fileAndPageNums, ref pageNumberIndex, comma))
                                break;
                        }
                    }
                    finally
                    {
                        if (inImage != null)
                            inImage.Dispose();
                    }
                }
            }
        }

        static bool openSrcImage(ref Tiff tif, string[] fileAndPageNums, ref int pageNumberIndex, char commaChar)
        {
            if (fileAndPageNums.Length == 0)
                return false;

            if (pageNumberIndex >= fileAndPageNums.Length && tif != null)
            {
                // we processed all images already
                return false;
            }

            if (tif == null)
                tif = Tiff.Open(fileAndPageNums[0], "r");

            if (tif == null)
                return false;

            if (fileAndPageNums.Length > 1)
            {
                // we have at least one page number specifier

                string pageNumStr = fileAndPageNums[pageNumberIndex];
                if (pageNumStr.Length == 0)
                {
                    // position "after trailing comma". we should process all
                    // remaining directories, so read next directory
                    return tif.ReadDirectory();
                }
                else
                {
                    // parse page number and set appropriate image directory
                    short pageNum = short.Parse(pageNumStr);
                    if (!tif.SetDirectory(pageNum))
                    {
                        Console.Error.Write("{0}{1}{2} not found!\n", tif.FileName(), commaChar, pageNum);
                        return false;
                    }

                    pageNumberIndex++;
                }
            }

            return true;
        }

        static void usage()
        {
            using (TextWriter stderr = Console.Error)
            {
                stderr.Write("{0}\n\n", Tiff.GetVersion());
                for (int i = 0; m_stuff[i] != null; i++)
                    stderr.Write("{0}\n", m_stuff[i]);
            }
        }
    }
}
