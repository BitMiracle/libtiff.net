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
 * tiff2pdf - converts a TIFF image to a PDF document
 *
 * Copyright (c) 2003 Ross Finlayson
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff;

namespace BitMiracle.Tiff2Pdf
{
    class Program
    {
        static string[] sizes = 
        {
            "LETTER", "A4", "LEGAL", "EXECUTIVE", "LETTER", "LEGAL", "LEDGER", 
            "TABLOID", "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "A10", 
            "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "A1", "A0", "2A0", 
            "4A0", "2A", "4A", "B10", "B9", "B8", "B7", "B6", "B5", "B4", "B3", 
            "B2", "B1", "B0", "JISB10", "JISB9", "JISB8", "JISB7", "JISB6", "JISB5", 
            "JISB4", "JISB3", "JISB2", "JISB1", "JISB0", "C10", "C9", "C8", "C7", 
            "C6", "C5", "C4", "C3", "C2", "C1", "C0", "RA2", "RA1", "RA0", "SRA4", 
            "SRA3", "SRA2", "SRA1", "SRA0", "A3EXTRA", "A4EXTRA", "STATEMENT", 
            "FOLIO", "QUARTO", null
        };

        static int[] widths = 
        {
            612, 595, 612, 522, 612, 612, 792, 792, 612, 792, 1224, 1584, 2448, 
            2016, 792, 2016, 2448, 2880, 74, 105, 147, 210, 298, 420, 595, 842, 
            1191, 1684, 2384, 3370, 4768, 3370, 4768, 88, 125, 176, 249, 354, 499, 
            709, 1001, 1417, 2004, 2835, 91, 128, 181, 258, 363, 516, 729, 1032, 
            1460, 2064, 2920, 79, 113, 162, 230, 323, 459, 649, 918, 1298, 1298, 
            2599, 1219, 1729, 2438, 638, 907, 1276, 1814, 2551, 914, 667, 396, 
            612, 609, 0
        };

        static int[] lengths = 
        {
            792, 842, 1008, 756, 792, 1008, 1224, 1224, 792, 1224, 1584, 2448, 
            3168, 2880, 6480, 10296, 12672, 10296, 105, 147, 210, 298, 420, 595, 
            842, 1191, 1684, 2384, 3370, 4768, 6741, 4768, 6741, 125, 176, 249, 
            354, 499, 709, 1001, 1417, 2004, 2835, 4008, 128, 181, 258, 363, 516, 
            729, 1032, 1460, 2064, 2920, 4127, 113, 162, 230, 323, 459, 649, 918, 
            1298, 1837, 1837, 3677, 1729, 2438, 3458, 907, 1276, 1814, 2551, 3628, 
            1262, 914, 612, 936, 780, 0
        };

        /*
            This is the main function.

            The program converts one TIFF file to one PDF file, including multiple page 
            TIFF files, tiled TIFF files, black and white. grayscale, and color TIFF 
            files that contain data of TIFF photometric interpretations of bilevel, 
            grayscale, RGB, YCbCr, CMYK separation, and ICC L*a*b* as supported by 
            libtiff and PDF.

            If you have multiple TIFF files to convert into one PDF file then use tiffcp 
            or other program to concatenate the files into a multiple page TIFF file.  
            If the input TIFF file is of huge dimensions (greater than 10000 pixels height
            or width) convert the input image to a tiled TIFF if it is not already.

            The standard output is standard output.  Set the output file name with the 
            "-o output.pdf" option.

            All black and white files are compressed into a single strip CCITT G4 Fax 
            compressed PDF, unless tiled, where tiled black and white images are 
            compressed into tiled CCITT G4 Fax compressed PDF, libtiff CCITT support 
            is assumed.

            Color and grayscale data can be compressed using either JPEG compression, 
            ITU-T T.81, or Zip/Deflate LZ77 compression, per PNG 1.2 and RFC 1951.  Set 
            the compression type using the -j or -z options.  JPEG compression support 
            requires that libtiff be configured with JPEG support, and Zip/Deflate 
            compression support requires that libtiff is configured with Zip support, 
            in tiffconf.h.  Use only one or the other of -j and -z.  The -q option 
            sets the image compression quality, that is 1-100 with libjpeg JPEG 
            compression and one of 1, 10, 11, 12, 13, 14, or 15 for PNG group compression 
            predictor methods, add 100, 200, ..., 900 to set zlib compression quality 1-9.
            PNG Group differencing predictor methods are not currently implemented.

            If the input TIFF contains single strip CCITT G4 Fax compressed information, 
            then that is written to the PDF file without transcoding, unless the options 
            of no compression and no passthrough are set, -d and -n.

            If the input TIFF contains JPEG or single strip Zip/Deflate compressed 
            information, and they are configured, then that is written to the PDF file 
            without transcoding, unless the options of no compression and no passthrough 
            are set.

            The default page size upon which the TIFF image is placed is determined by 
            the resolution and extent of the image data.  Default values for the TIFF 
            image resolution can be set using the -x and -y options.  The page size can 
            be set using the -p option for paper size, or -w and -l for paper width and 
            length, then each page of the TIFF image is centered on its page.  The 
            distance unit for default resolution and page width and length can be set 
            by the -u option, the default unit is inch.

            Various items of the output document information can be set with the -e, -c, 
            -a, -t, -s, and -k tags.  Setting the argument of the option to "" for these 
            tags causes the relevant document information field to be not written.  Some 
            of the document information values otherwise get their information from the 
            input TIFF image, the software, author, document name, and image description.

            The output PDF file conforms to the PDF 1.1 specification or PDF 1.2 if using 
            Zip/Deflate compression.  

            The Portable Document Format (PDF) specification is copyrighted by Adobe 
            Systems, Incorporated.  Todos derechos reservados.

            Here is a listing of the usage example and the options to the tiff2pdf 
            program that is part of the libtiff distribution.  Options followed by 
            a colon have a required argument.

            usage:  tiff2pdf [options] input.tif

            options:
            -o: output to file name

            -j: compress with JPEG (requires libjpeg configured with libtiff)
            -z: compress with Zip/Deflate (requires zlib configured with libtiff)
            -q: compression quality
            -n: no compressed data passthrough
            -d: do not compress (decompress)
            -i: invert colors
            -u: set distance unit, 'i' for inch, 'm' for centimeter
            -x: set x resolution default
            -y: set y resolution default
            -w: width in units
            -l: length in units
            -r: 'd' for resolution default, 'o' for resolution override
            -p: paper size, eg "letter", "legal", "a4"
            -f: set pdf "fit window" user preference
            -b:	set PDF "Interpolate" user preference
            -e: date, overrides image or current date/time default, YYYYMMDDHHMMSS
            -c: creator, overrides image software default
            -a: author, overrides image artist default
            -t: title, overrides image document name default
            -s: subject, overrides image image description default
            -k: keywords

            -h: usage

            examples:

            tiff2pdf -o output.pdf input.tiff

            The above example would generate the file output.pdf from input.tiff.

            tiff2pdf input.tiff

            The above example would generate PDF output from input.tiff and write it 
            to standard output.

            tiff2pdf -j -p letter -o output.pdf input.tiff

            The above example would generate the file output.pdf from input.tiff, 
            putting the image pages on a letter sized page, compressing the output 
            with JPEG.
        */
        static void Main(string[] args)
        {
            T2P t2p = new T2P();
            if (t2p == null)
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't initialize context");

            int c;
            string outfilename = null;
            bool failed = false;
            
            while (argv && (c = getopt(argc, argv, "o:q:u:x:y:w:l:r:p:e:c:a:t:s:k:jzndifbh")) != -1)
            {
                switch (c)
                {
                    case 'o':
                        outfilename = optarg;
                        break;

                    case 'j':  
                        t2p.m_pdf_defaultcompression = t2p_compress_t.T2P_COMPRESS_JPEG;
                        break;

                    case 'z':  
                        t2p.m_pdf_defaultcompression = t2p_compress_t.T2P_COMPRESS_ZIP;
                        break;
                    
                    case 'q': 
                        t2p.m_pdf_defaultcompressionquality = (UInt16)atoi(optarg);
                        break;
                    
                    case 'n': 
                        t2p.m_pdf_nopassthrough = true;
                        break;
                    
                    case 'd': 
                        t2p.m_pdf_defaultcompression = t2p_compress_t.T2P_COMPRESS_NONE;
                        break;
                    
                    case 'u': 
                        if (optarg[0] == 'm')
                            t2p.m_pdf_centimeters = true;
                        break;

                    case 'x': 
                        t2p.m_pdf_defaultxres = (float)atof(optarg) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        break;

                    case 'y': 
                        t2p.m_pdf_defaultyres = (float)atof(optarg) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        break;

                    case 'w': 
                        t2p.m_pdf_overridepagesize = true;
                        t2p.m_pdf_defaultpagewidth = ((float)atof(optarg) * Tiff2PdfConstants.PS_UNIT_SIZE) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        break;

                    case 'l': 
                        t2p.m_pdf_overridepagesize = true;
                        t2p.m_pdf_defaultpagelength = ((float)atof(optarg) * Tiff2PdfConstants.PS_UNIT_SIZE) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        break;

                    case 'r': 
                        if (optarg[0] == 'o')
                            t2p.m_pdf_overrideres = true;
                        break;

                    case 'p': 
                        if (tiff2pdf_match_paper_size(t2p.m_pdf_defaultpagewidth, t2p.m_pdf_defaultpagelength, optarg))
                            t2p.m_pdf_overridepagesize = true;
                        else
                            Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "Unknown paper size %s, ignoring option", optarg);

                        break;

                    case 'i':
                        t2p.m_pdf_colorspace_invert = true;
                        break;

                    case 'f': 
                        t2p.m_pdf_fitwindow = true;
                        break;

                    case 'e':
                        t2p.m_pdf_datetime = new byte [17];
                        if (t2p.m_pdf_datetime == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for main", 17); 
                            failed = true;
                            break;
                        }

                        if (strlen(optarg) == 0)
                        {
                            t2p.m_pdf_datetime[0] = 0;
                        }
                        else
                        {
                            if (strlen(optarg) > 14)
                                optarg[14] = 0;

                            t2p.m_pdf_datetime[0] = 'D';
                            t2p.m_pdf_datetime[1] = ':';
                            strcpy((char *)t2p.m_pdf_datetime + 2, optarg);
                        }
                        break;

                    case 'c': 
                        t2p.m_pdf_creator = new byte [strlen(optarg) + 1];
                        if (t2p.m_pdf_creator == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for main", strlen(optarg) + 1); 
                            failed = true;
                            break;
                        }

                        strcpy((char *)t2p.m_pdf_creator, optarg);
                        t2p.m_pdf_creator[strlen(optarg)] = 0;
                        break;
                    
                    case 'a': 
                        t2p.m_pdf_author = new byte [strlen(optarg) + 1];
                        if (t2p.m_pdf_author == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for main", strlen(optarg) + 1); 
                            failed = true;
                            break;
                        }

                        strcpy((char *)t2p.m_pdf_author, optarg);
                        t2p.m_pdf_author[strlen(optarg)] = 0;
                        break;

                    case 't': 
                        t2p.m_pdf_title = new byte [strlen(optarg) + 1];
                        if (t2p.m_pdf_title == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for main", strlen(optarg) + 1); 
                            failed = true;
                            break;
                        }

                        strcpy((char *)t2p.m_pdf_title, optarg);
                        t2p.m_pdf_title[strlen(optarg)] = 0;
                        break;
                    
                    case 's': 
                        t2p.m_pdf_subject = new byte [strlen(optarg) + 1];
                        if (t2p.m_pdf_subject == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for main", strlen(optarg) + 1);
                            failed = true;
                            break;
                        }

                        strcpy((char *)t2p.m_pdf_subject, optarg);
                        t2p.m_pdf_subject[strlen(optarg)] = 0;
                        break;

                    case 'k': 
                        t2p.m_pdf_keywords = new byte [strlen(optarg) + 1];
                        if (t2p.m_pdf_keywords == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for main", strlen(optarg) + 1); 
                            failed = true;
                            break;
                        }

                        strcpy((char *)t2p.m_pdf_keywords, optarg);
                        t2p.m_pdf_keywords[strlen(optarg)] = 0;
                        break;

                    case 'b':
                        t2p.m_pdf_image_interpolate = true;
                        break;

                    case 'h': 
                    case '?': 
                        tiff2pdf_usage();
                        return;
                }
            }

            Tiff input = null;
            Tiff output = null;

            if (!failed)
            {
                /*
                * Input
                */
                if (argc > optind)
                {
                    input = Tiff.Open(argv[optind++], "r");
                    if (input == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't open input file %s for reading", argv[optind - 1]);
                        failed = true;
                    }
                }
                else
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No input file specified");
                    tiff2pdf_usage();
                    failed = true;
                }

                if (!failed && argc > optind)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for multiple input files"); 
                    tiff2pdf_usage();
                    failed = true;
                }

                if (!failed)
                {
                    /*
                    * Output
                    */
                    t2p.m_outputdisable = false;
                    if (outfilename != null)
                    {
                        t2p.m_outputfile = fopen(outfilename, "wb");
                        if (t2p.m_outputfile == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't open output file %s for writing", outfilename);
                            failed = true;
                        }
                    } 
                    else
                    {
                        outfilename = "-";
                        t2p.m_outputfile = stdout;
                    }

                    if (!failed)
                    {
                        output = Tiff.ClientOpen(outfilename, "w", (thandle_t)t2p, t2p.m_stream);
                        if (output == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't initialize output descriptor");
                            failed = true;
                        }

                        if (!failed)
                        {
                            /*
                            * Validate
                            */
                            t2p.validate();

                            thandle_t client = output.Clientdata();
                            TiffStream* stream = output.GetStream();
                            if (stream != null)
                                stream.Seek(client, 0, SEEK_SET);

                            /*
                            * Write
                            */
                            t2p.write_pdf(input, output);
                            if (t2p.m_error)
                            {
                                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "An error occurred creating output PDF file");
                                failed = true;
                            }
                        }
                    }
                }
            }
        }

        void tiff2pdf_usage()
        {
            string[] lines = 
            {
                "usage:  tiff2pdf [options] input.tiff", "options:",
                " -o: output to file name",
                " -j: compress with JPEG",
                " -z: compress with Zip/Deflate",
                " -q: compression quality",
                " -n: no compressed data passthrough",
                " -d: do not compress (decompress)",
                " -i: invert colors",
                " -u: set distance unit, 'i' for inch, 'm' for centimeter",
                " -x: set x resolution default in dots per unit",
                " -y: set y resolution default in dots per unit",
                " -w: width in units",
                " -l: length in units",
                " -r: 'd' for resolution default, 'o' for resolution override",
                " -p: paper size, eg \"letter\", \"legal\", \"A4\"",
                " -f: set PDF \"Fit Window\" user preference",
                " -e: date, overrides image or current date/time default, YYYYMMDDHHMMSS",
                " -c: sets document creator, overrides image software default",
                " -a: sets document author, overrides image artist default",
                " -t: sets document title, overrides image document name default",
                " -s: sets document subject, overrides image image description default",
                " -k: sets document keywords",
                " -b: set PDF \"Interpolate\" user preference",
                " -h: usage",
                null
            };

            Stream stderr = Console.OpenStandardError();
            Tiff.fprintf(stderr, "%s\n\n", Tiff.GetVersion());

            for (int i = 0; lines[i] != null; i++)
                Tiff.fprintf(stderr, "%s\n", lines[i]);
        }

        bool tiff2pdf_match_paper_size(out float width, out float length, string papersize)
        {
            width = 0;
            length = 0;

            

            string papersizeUpper = papersize.ToUpper();
            for (int i = 0; sizes[i] != null; i++)
            {
                if (papersizeUpper == sizes[i])
                {
                    width = (float)widths[i];
                    length = (float)lengths[i];
                    return true;
                }
            }

            return false;
        }
    }
}
