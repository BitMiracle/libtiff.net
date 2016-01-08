/* 
 * Tiff2Pdf - converts a TIFF image to a PDF document
 *
 * Based on tiff2pdf. Copyright (c) 2003 Ross Finlayson
 *
 */

using System;
using System.Globalization;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.Tiff2Pdf
{
    public static class Program
    {
        // setting this to true will make program to always output identical 
        // PDF files for each given image.
        //
        // by default this is set false, so program will use different PDF 
        // trailers and timestamps each time it runs. this behavior is 
        // more correct if you don't use the program as a test utility.
        //
        // and yes, image data in produced PDFs is the same whether you 
        // use test friendly or not behavior.
        static public bool g_testFriendly = false;

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
        public static void Main(string[] args)
        {
            T2P t2p = new T2P();
            t2p.m_testFriendly = g_testFriendly;

            string outfilename = null;
            
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
                byte[] bytes = null;

                switch (arg[0])
                {
                    case 'o':
                        outfilename = optarg;
                        argn++;
                        break;

                    case 'j':  
                        t2p.m_pdf_defaultcompression = t2p_compress_t.T2P_COMPRESS_JPEG;
                        break;

                    case 'z':  
                        t2p.m_pdf_defaultcompression = t2p_compress_t.T2P_COMPRESS_ZIP;
                        break;
                    
                    case 'q': 
                        t2p.m_pdf_defaultcompressionquality = short.Parse(optarg, CultureInfo.InvariantCulture);
                        argn++;
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

                        argn++;
                        break;

                    case 'x': 
                        t2p.m_pdf_defaultxres = float.Parse(optarg, CultureInfo.InvariantCulture) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        argn++;
                        break;

                    case 'y':
                        t2p.m_pdf_defaultyres = float.Parse(optarg, CultureInfo.InvariantCulture) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        argn++;
                        break;

                    case 'w': 
                        t2p.m_pdf_overridepagesize = true;
                        t2p.m_pdf_defaultpagewidth = (float.Parse(optarg, CultureInfo.InvariantCulture) * Tiff2PdfConstants.PS_UNIT_SIZE) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        argn++;
                        break;

                    case 'l': 
                        t2p.m_pdf_overridepagesize = true;
                        t2p.m_pdf_defaultpagelength = (float.Parse(optarg, CultureInfo.InvariantCulture) * Tiff2PdfConstants.PS_UNIT_SIZE) / (t2p.m_pdf_centimeters ? 2.54F : 1.0F);
                        argn++;
                        break;

                    case 'r': 
                        if (optarg[0] == 'o')
                            t2p.m_pdf_overrideres = true;

                        argn++;
                        break;

                    case 'p': 
                        if (tiff2pdf_match_paper_size(out t2p.m_pdf_defaultpagewidth, out t2p.m_pdf_defaultpagelength, optarg))
                            t2p.m_pdf_overridepagesize = true;
                        else
                            Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "Unknown paper size {0}, ignoring option", optarg);

                        argn++;
                        break;

                    case 'i':
                        t2p.m_pdf_colorspace_invert = true;
                        break;

                    case 'f': 
                        t2p.m_pdf_fitwindow = true;
                        break;

                    case 'e':
                        t2p.m_pdf_datetime = new byte [17];
                        if (optarg.Length == 0)
                        {
                            t2p.m_pdf_datetime[0] = 0;
                        }
                        else
                        {
                            t2p.m_pdf_datetime[0] = (byte)'D';
                            t2p.m_pdf_datetime[1] = (byte)':';

                            bytes = T2P.Latin1Encoding.GetBytes(optarg);
                            Buffer.BlockCopy(bytes, 0, t2p.m_pdf_datetime, 2, Math.Min(bytes.Length, 14));
                        }

                        argn++;
                        break;

                    case 'c':
                        t2p.m_pdf_creator = T2P.Latin1Encoding.GetBytes(optarg);
                        argn++;
                        break;
                    
                    case 'a':
                        t2p.m_pdf_author = T2P.Latin1Encoding.GetBytes(optarg);
                        argn++;
                        break;

                    case 't':
                        t2p.m_pdf_title = T2P.Latin1Encoding.GetBytes(optarg);
                        argn++;
                        break;
                    
                    case 's':
                        t2p.m_pdf_subject = T2P.Latin1Encoding.GetBytes(optarg);
                        argn++;
                        break;

                    case 'k':
                        t2p.m_pdf_keywords = T2P.Latin1Encoding.GetBytes(optarg);
                        argn++;
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

            /*
             * Input
             */
            string inputFileName = null;
            if (args.Length > argn)
            {
                inputFileName = args[argn];
            }
            else
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No input file specified");
                tiff2pdf_usage();
                return;
            }

            using (Tiff input = Tiff.Open(inputFileName, "r"))
            {
                if (input == null)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't open input file {0} for reading", args[argn - 1]);
                    return;
                }

                if ((args.Length - 1) > argn)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for multiple input files");
                    tiff2pdf_usage();
                    return;
                }

                /*
                * Output
                */
                t2p.m_outputdisable = false;
                if (outfilename != null)
                {
                    try
                    {
                        t2p.m_outputfile = File.Open(outfilename, FileMode.Create, FileAccess.Write);
                    }
                    catch (Exception e)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't open output file {0} for writing. {1}", outfilename, e.Message);
                        return;
                    }
                }
                else
                {
                    outfilename = "-";
                    t2p.m_outputfile = Console.OpenStandardOutput();
                }

                using (Tiff output = Tiff.ClientOpen(outfilename, "w", t2p, t2p.m_stream))
                {
                    if (output == null)
                    {
                        t2p.m_outputfile.Dispose();
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't initialize output descriptor");
                        return;
                    }

                    /*
                    * Validate
                    */
                    t2p.validate();

                    object client = output.Clientdata();
                    TiffStream stream = output.GetStream();
                    stream.Seek(client, 0, SeekOrigin.Begin);

                    /*
                    * Write
                    */
                    t2p.write_pdf(input, output);
                    if (t2p.m_error)
                    {
                        t2p.m_outputfile.Dispose();
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "An error occurred creating output PDF file");
                        return;
                    }
                }

                t2p.m_outputfile.Dispose();
            }
        }

        static void tiff2pdf_usage()
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

            using (TextWriter stderr = Console.Error)
            {
                stderr.Write("{0}\n\n", Tiff.GetVersion());

                for (int i = 0; lines[i] != null; i++)
                    stderr.Write("{0}\n", lines[i]);
            }
        }

        static bool tiff2pdf_match_paper_size(out float width, out float length, string papersize)
        {
            width = 0;
            length = 0;

            string papersizeUpper = papersize.ToUpper(CultureInfo.InvariantCulture);
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
