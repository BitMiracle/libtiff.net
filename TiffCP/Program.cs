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
using System.Globalization;
using System.IO;
using System.Text;

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

            StringBuilder mode = new StringBuilder();
            mode.Append('w');

            char imageNumberSeparator = ','; // (default) comma separator character
            FillOrder defaultFillOrder = 0;
            int defaultTileLength = -1;
            int initialDirectoryOffset = 0;
            PlanarConfig defaultPlanarConfig = PlanarConfig.UNKNOWN;
            int defaultRowsPerStrip = 0;
            int defaultTileWidth = -1;

            int argn = 0;
            for (; argn < args.Length; argn++)
            {
                string option = args[argn];
                if (option[0] == '-')
                    option = option.Substring(1);
                else
                    break;

                string optionArg = null;
                if (argn < (args.Length - 1))
                    optionArg = args[argn + 1];
                    
                switch (option[0])
                {
                    case ',':
                        if (option[1] != '=')
                        {
                            usage();
                            return;
                        }

                        imageNumberSeparator = option[2];
                        break;
                    case 'b':
                        // this file is bias image subtracted from others
                        if (c.m_bias != null)
                        {
                            Console.Error.Write("Only 1 bias image may be specified\n");
                            return;
                        }

                        string biasName = args[argn + 1];
                        c.m_bias = Tiff.Open(biasName, "r");
                        if (c.m_bias == null)
                        {
                            Console.Error.WriteLine("Failed to open '{0}' as input.", biasName);
                            return;
                        }

                        if (c.m_bias.IsTiled())
                        {
                            Console.Error.Write("Bias image must be organized in strips\n");
                            return;
                        }

                        FieldValue[] result = c.m_bias.GetField(TiffTag.SAMPLESPERPIXEL);
                        short samples = result[0].ToShort();
                        if (samples != 1)
                        {
                            Console.Error.Write("Bias image must be monochrome\n");
                            return;
                        }

                        argn++;
                        break;
                    case 'a':
                        // append to output
                        mode[0] = 'a';
                        break;
                    case 'c':
                        // compression scheme
                        if (!c.ProcessCompressOptions(optionArg))
                        {
                            usage();
                            return;
                        }

                        argn++;
                        break;
                    case 'f':
                        // fill order
                        if (optionArg == "lsb2msb")
                            defaultFillOrder = FillOrder.LSB2MSB;
                        else if (optionArg == "msb2lsb")
                            defaultFillOrder = FillOrder.MSB2LSB;
                        else
                        {
                            usage();
                            return;
                        }

                        argn++;
                        break;
                    case 'i':
                        // ignore errors
                        c.m_ignore = true;
                        break;
                    case 'l':
                        // tile length
                        c.m_outtiled = 1;
                        defaultTileLength = int.Parse(optionArg, CultureInfo.InvariantCulture);
                        argn++;
                        break;
                    case 'o':
                        // initial directory offset
                        initialDirectoryOffset = int.Parse(optionArg, CultureInfo.InvariantCulture);
                        break;
                    case 'p':
                        // planar configuration
                        if (optionArg == "separate")
                            defaultPlanarConfig = PlanarConfig.SEPARATE;
                        else if (optionArg == "contig")
                            defaultPlanarConfig = PlanarConfig.CONTIG;
                        else
                        {
                            usage();
                            return;
                        }

                        argn++;
                        break;
                    case 'r':
                        // rows/strip
                        defaultRowsPerStrip = int.Parse(optionArg, CultureInfo.InvariantCulture);
                        argn++;
                        break;
                    case 's':
                        // generate stripped output
                        c.m_outtiled = 0;
                        break;
                    case 't':
                        // generate tiled output
                        c.m_outtiled = 1;
                        break;
                    case 'w':
                        // tile width
                        c.m_outtiled = 1;
                        defaultTileWidth = int.Parse(optionArg, CultureInfo.InvariantCulture);
                        argn++;
                        break;
                    case 'B':
                        mode.Append('b');
                        break;
                    case 'L':
                        mode.Append('l');
                        break;
                    case 'M':
                        mode.Append('m');
                        break;
                    case 'C':
                        mode.Append('c');
                        break;
                    case 'x':
                        c.m_pageInSeq = 1;
                        break;
                    case '?':
                        usage();
                        return;
                }
            }

            if (args.Length - argn < 2)
            {
                // there must be at least one input and one output image names after options
                usage();
                return;
            }

            using (Tiff outImage = Tiff.Open(args[args.Length - 1], mode.ToString()))
            {
                if (outImage == null)
                {
                    Console.Error.WriteLine("Failed to open '{0}' as output.", args[args.Length - 1]);
                    return;
                }

                if ((args.Length - argn) == 2)
                    c.m_pageNum = -1;

                for (; argn < args.Length - 1; argn++)
                {
                    string[] fileAndPageNums = args[argn].Split(new char[] { imageNumberSeparator });
                    
                    using (Tiff inImage = Tiff.Open(fileAndPageNums[0], "r"))
                    {
                        if (inImage == null)
                            return;

                        if (initialDirectoryOffset != 0 && !inImage.SetSubDirectory(initialDirectoryOffset))
                        {
                            Tiff.Error(inImage.FileName(), "Error, setting subdirectory at 0x{0:x}", initialDirectoryOffset);
                            inImage.Dispose();
                            break;
                        }

                        int initialPage = 0;
                        int pageNumPos = 1;
                        
                        if (pageNumPos < fileAndPageNums.Length && !string.IsNullOrEmpty(fileAndPageNums[pageNumPos]))
                            initialPage = int.Parse(fileAndPageNums[pageNumPos]);

                        int totalPages = inImage.NumberOfDirectories();
                        for (int i = initialPage; i < totalPages; )
                        {
                            c.m_config = defaultPlanarConfig;
                            c.m_compression = c.m_defcompression;
                            c.m_predictor = c.m_defpredictor;
                            c.m_fillorder = defaultFillOrder;
                            c.m_rowsperstrip = defaultRowsPerStrip;
                            c.m_tilewidth = defaultTileWidth;
                            c.m_tilelength = defaultTileLength;
                            c.m_g3opts = c.m_defg3opts;

                            if (!inImage.SetDirectory((short)i))
                            {
                                Console.Error.Write("{0}{1}{2} not found!\n",
                                    inImage.FileName(), imageNumberSeparator, i);
                                return;
                            }

                            if (!c.Copy(inImage, outImage) || !outImage.WriteDirectory())
                                return;

                            // if we have at least one page specifier and current specifier is not empty.
                            // specifier is empty when trailing separator used like this: "file,num,"
                            if (pageNumPos < fileAndPageNums.Length && !string.IsNullOrEmpty(fileAndPageNums[pageNumPos]))
                            {
                                // move to next page specifier
                                pageNumPos++;

                                if (pageNumPos < fileAndPageNums.Length)
                                {
                                    // new page specifier position is valid

                                    if (!string.IsNullOrEmpty(fileAndPageNums[pageNumPos]))
                                    {
                                        // new page specifier is not empty. use specified page number
                                        i = int.Parse(fileAndPageNums[pageNumPos]);
                                    }
                                    else
                                    {
                                        // new page specifier is empty. just move to the next page
                                        i++;
                                    }
                                }
                                else
                                {
                                    // new page specifier position is invalid. done all pages.
                                    break;
                                }
                            }
                            else
                            {
                                // we have no page specifiers or current page specifier is empty
                                // just move to the next page
                                i++;
                            }
                        }
                    }
                }
            }
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
