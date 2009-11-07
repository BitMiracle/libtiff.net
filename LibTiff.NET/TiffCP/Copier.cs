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
using BitMiracle.LibTiff;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace BitMiracle.TiffCP
{
    class Copier
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

        static tagToCopy[] g_tags = 
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

        delegate bool readFunc(Tiff inImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp);
        delegate bool writeFunc(Tiff outImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp);

        public int m_outtiled = -1;
        public int m_tilewidth;
        public int m_tilelength;
        public PLANARCONFIG m_config;
        public COMPRESSION m_compression;
        public short m_predictor;
        public FILLORDER m_fillorder;
        public int m_rowsperstrip;
        public GROUP3OPT m_g3opts;
        public bool m_ignore = false; /* if true, ignore read errors */
        public GROUP3OPT m_defg3opts = GROUP3OPT.GROUP3OPT_UNKNOWN;
        public COMPRESSION m_defcompression = (COMPRESSION)(-1);
        public short m_defpredictor = -1;
        public Tiff m_bias = null;
        public int m_pageNum = 0;

        ORIENTATION m_orientation;
        int m_quality = 75; /* JPEG quality */
        JPEGCOLORMODE m_jpegcolormode = JPEGCOLORMODE.JPEGCOLORMODE_RGB;

        public bool ProcessCompressOptions(string opt)
        {
            if (opt == "none")
            {
                m_defcompression = COMPRESSION.COMPRESSION_NONE;
            }
            else if (opt == "packbits")
            {
                m_defcompression = COMPRESSION.COMPRESSION_PACKBITS;
            }
            else if (opt.StartsWith("jpeg"))
            {
                m_defcompression = COMPRESSION.COMPRESSION_JPEG;

                string[] options = opt.Split(new char[] { ':' });
                for (int i = 1; i < options.Length; i++)
                {
                    if (char.IsDigit(options[i][0]))
                        m_quality = int.Parse(options[i], CultureInfo.InvariantCulture);
                    else if (options[i] == "r")
                        m_jpegcolormode = JPEGCOLORMODE.JPEGCOLORMODE_RAW;
                    else
                        return false;
                }
            }
            else if (opt.StartsWith("g3"))
            {
                if (!processG3Options(opt))
                    return false;

                m_defcompression = COMPRESSION.COMPRESSION_CCITTFAX3;
            }
            else if (opt == "g4")
            {
                m_defcompression = COMPRESSION.COMPRESSION_CCITTFAX4;
            }
            else if (opt.StartsWith("lzw"))
            {
                int n = opt.IndexOf(':');
                if (n != -1 && n < (opt.Length - 1))
                    m_defpredictor = short.Parse(opt.Substring(n + 1));

                m_defcompression = COMPRESSION.COMPRESSION_LZW;
            }
            else if (opt.StartsWith("zip"))
            {
                int n = opt.IndexOf(':');
                if (n != -1 && n < (opt.Length - 1))
                    m_defpredictor = short.Parse(opt.Substring(n + 1));

                m_defcompression = COMPRESSION.COMPRESSION_ADOBE_DEFLATE;
            }
            else
                return false;

            return true;
        }

        bool processG3Options(string cp)
        {
            string[] options = cp.Split(new char[] { ':' });
            if (options.Length > 1)
            {
                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i].StartsWith("1d"))
                        m_defg3opts &= ~GROUP3OPT.GROUP3OPT_2DENCODING;
                    else if (options[i].StartsWith("2d"))
                        m_defg3opts |= GROUP3OPT.GROUP3OPT_2DENCODING;
                    else if (options[i].StartsWith("fill"))
                        m_defg3opts |= GROUP3OPT.GROUP3OPT_FILLBITS;
                    else
                        return false;
                }
            }

            return true;
        }

        public bool Copy(Tiff inImage, Tiff outImage)
        {
            int width = 0;
            FieldValue[] result = inImage.GetField(TIFFTAG.TIFFTAG_IMAGEWIDTH);
            if (result != null)
            {
                width = result[0].ToInt();
                outImage.SetField(TIFFTAG.TIFFTAG_IMAGEWIDTH, width);
            }

            int length = 0;
            result = inImage.GetField(TIFFTAG.TIFFTAG_IMAGELENGTH);
            if (result != null)
            {
                length = result[0].ToInt();
                outImage.SetField(TIFFTAG.TIFFTAG_IMAGELENGTH, length);
            }

            ushort bitspersample = 1;
            result = inImage.GetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE);
            if (result != null)
            {
                bitspersample = result[0].ToUShort();
                outImage.SetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE, bitspersample);
            }

            ushort samplesperpixel = 1;
            result = inImage.GetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL);
            if (result != null)
            {
                samplesperpixel = result[0].ToUShort();
                outImage.SetField(TIFFTAG.TIFFTAG_SAMPLESPERPIXEL, samplesperpixel);
            }

            if (m_compression != (COMPRESSION)(-1))
                outImage.SetField(TIFFTAG.TIFFTAG_COMPRESSION, m_compression);
            else
            {
                result = inImage.GetField(TIFFTAG.TIFFTAG_COMPRESSION);
                if (result != null)
                {
                    m_compression = (COMPRESSION)result[0].ToInt();
                    outImage.SetField(TIFFTAG.TIFFTAG_COMPRESSION, m_compression);
                }
            }

            if (m_compression == COMPRESSION.COMPRESSION_JPEG)
            {
                result = inImage.GetField(TIFFTAG.TIFFTAG_COMPRESSION);
                if (result != null)
                {
                    COMPRESSION input_compression = (COMPRESSION)result[0].ToInt();
                    if (input_compression == COMPRESSION.COMPRESSION_JPEG)
                        inImage.SetField(TIFFTAG.TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE.JPEGCOLORMODE_RGB);
                }

                result = inImage.GetField(TIFFTAG.TIFFTAG_PHOTOMETRIC);
                if (result != null)
                {
                    PHOTOMETRIC input_photometric = (PHOTOMETRIC)result[0].ToInt();
                    if (input_photometric == PHOTOMETRIC.PHOTOMETRIC_RGB)
                    {
                        if (m_jpegcolormode == JPEGCOLORMODE.JPEGCOLORMODE_RGB)
                            outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, PHOTOMETRIC.PHOTOMETRIC_YCBCR);
                        else
                            outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, PHOTOMETRIC.PHOTOMETRIC_RGB);
                    }
                    else
                        outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, input_photometric);
                }
            }
            else if (m_compression == COMPRESSION.COMPRESSION_SGILOG || m_compression == COMPRESSION.COMPRESSION_SGILOG24)
                outImage.SetField(TIFFTAG.TIFFTAG_PHOTOMETRIC, samplesperpixel == 1 ? PHOTOMETRIC.PHOTOMETRIC_LOGL : PHOTOMETRIC.PHOTOMETRIC_LOGLUV);
            else
                CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_PHOTOMETRIC, 1, TiffDataType.TIFF_SHORT);

            if (m_fillorder != 0)
                outImage.SetField(TIFFTAG.TIFFTAG_FILLORDER, m_fillorder);
            else
                CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FILLORDER, 1, TiffDataType.TIFF_SHORT);

            /*
             * Will copy `Orientation' tag from input image
             */
            result = inImage.GetFieldDefaulted(TIFFTAG.TIFFTAG_ORIENTATION);
            m_orientation = (ORIENTATION)result[0].ToByte();
            switch (m_orientation)
            {
                case ORIENTATION.ORIENTATION_BOTRIGHT:
                case ORIENTATION.ORIENTATION_RIGHTBOT:
                    Tiff.Warning(inImage.FileName(), "using bottom-left orientation");
                    m_orientation = ORIENTATION.ORIENTATION_BOTLEFT;
                    break;

                case ORIENTATION.ORIENTATION_LEFTBOT:
                case ORIENTATION.ORIENTATION_BOTLEFT:
                    break;

                case ORIENTATION.ORIENTATION_TOPRIGHT:
                case ORIENTATION.ORIENTATION_RIGHTTOP:
                default:
                    Tiff.Warning(inImage.FileName(), "using top-left orientation");
                    m_orientation = ORIENTATION.ORIENTATION_TOPLEFT;
                    break;

                case ORIENTATION.ORIENTATION_LEFTTOP:
                case ORIENTATION.ORIENTATION_TOPLEFT:
                    break;
            }

            outImage.SetField(TIFFTAG.TIFFTAG_ORIENTATION, m_orientation);

            /*
             * Choose tiles/strip for the output image according to
             * the command line arguments (-tiles, -strips) and the
             * structure of the input image.
             */
            if (m_outtiled == -1)
            {
                if (inImage.IsTiled())
                    m_outtiled = 1;
                else
                    m_outtiled = 0;
            }

            if (m_outtiled != 0)
            {
                /*
                 * Setup output file's tile width&height.  If either
                 * is not specified, use either the value from the
                 * input image or, if nothing is defined, use the
                 * library default.
                 */
                if (m_tilewidth == -1)
                {
                    result = inImage.GetFieldDefaulted(TIFFTAG.TIFFTAG_TILEWIDTH);
                    if (result != null)
                        m_tilewidth = result[0].ToInt();
                }

                if (m_tilelength == -1)
                {
                    result = inImage.GetFieldDefaulted(TIFFTAG.TIFFTAG_TILELENGTH);
                    if (result != null)
                        m_tilelength = result[0].ToInt();
                }

                outImage.DefaultTileSize(ref m_tilewidth, ref m_tilelength);
                outImage.SetField(TIFFTAG.TIFFTAG_TILEWIDTH, m_tilewidth);
                outImage.SetField(TIFFTAG.TIFFTAG_TILELENGTH, m_tilelength);
            }
            else
            {
                /*
                 * RowsPerStrip is left unspecified: use either the
                 * value from the input image or, if nothing is defined,
                 * use the library default.
                 */
                if (m_rowsperstrip == 0)
                {
                    result = inImage.GetField(TIFFTAG.TIFFTAG_ROWSPERSTRIP);
                    if (result == null)
                        m_rowsperstrip = outImage.DefaultStripSize(m_rowsperstrip);
                    else
                        m_rowsperstrip = result[0].ToInt();

                    if (m_rowsperstrip > length && m_rowsperstrip != -1)
                        m_rowsperstrip = length;
                }
                else if (m_rowsperstrip == -1)
                    m_rowsperstrip = length;

                outImage.SetField(TIFFTAG.TIFFTAG_ROWSPERSTRIP, m_rowsperstrip);
            }

            if (m_config != PLANARCONFIG.PLANARCONFIG_UNKNOWN)
                outImage.SetField(TIFFTAG.TIFFTAG_PLANARCONFIG, m_config);
            else
            {
                result = inImage.GetField(TIFFTAG.TIFFTAG_PLANARCONFIG);
                if (result != null)
                {
                    m_config = (PLANARCONFIG)result[0].ToShort();
                    outImage.SetField(TIFFTAG.TIFFTAG_PLANARCONFIG, m_config);
                }
            }

            if (samplesperpixel <= 4)
                CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_TRANSFERFUNCTION, 4, TiffDataType.TIFF_SHORT);

            CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_COLORMAP, 4, TiffDataType.TIFF_SHORT);

            /* SMinSampleValue & SMaxSampleValue */
            switch (m_compression)
            {
                case COMPRESSION.COMPRESSION_JPEG:
                    outImage.SetField(TIFFTAG.TIFFTAG_JPEGQUALITY, m_quality);
                    outImage.SetField(TIFFTAG.TIFFTAG_JPEGCOLORMODE, m_jpegcolormode);
                    break;
                case COMPRESSION.COMPRESSION_LZW:
                case COMPRESSION.COMPRESSION_ADOBE_DEFLATE:
                case COMPRESSION.COMPRESSION_DEFLATE:
                    if (m_predictor != -1)
                        outImage.SetField(TIFFTAG.TIFFTAG_PREDICTOR, m_predictor);
                    else
                    {
                        result = inImage.GetField(TIFFTAG.TIFFTAG_PREDICTOR);
                        if (result != null)
                        {
                            m_predictor = result[0].ToShort();
                            outImage.SetField(TIFFTAG.TIFFTAG_PREDICTOR, m_predictor);
                        }
                    }
                    break;
                case COMPRESSION.COMPRESSION_CCITTFAX3:
                case COMPRESSION.COMPRESSION_CCITTFAX4:
                    if (m_compression == COMPRESSION.COMPRESSION_CCITTFAX3)
                    {
                        if (m_g3opts != GROUP3OPT.GROUP3OPT_UNKNOWN)
                            outImage.SetField(TIFFTAG.TIFFTAG_GROUP3OPTIONS, m_g3opts);
                        else
                        {
                            result = inImage.GetField(TIFFTAG.TIFFTAG_GROUP3OPTIONS);
                            if (result != null)
                            {
                                m_g3opts = (GROUP3OPT)result[0].ToShort();
                                outImage.SetField(TIFFTAG.TIFFTAG_GROUP3OPTIONS, m_g3opts);
                            }
                        }
                    }
                    else
                        CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_GROUP4OPTIONS, 1, TiffDataType.TIFF_LONG);

                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_BADFAXLINES, 1, TiffDataType.TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_CLEANFAXDATA, 1, TiffDataType.TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_CONSECUTIVEBADFAXLINES, 1, TiffDataType.TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FAXRECVPARAMS, 1, TiffDataType.TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FAXRECVTIME, 1, TiffDataType.TIFF_LONG);
                    CopyTag(inImage, outImage, TIFFTAG.TIFFTAG_FAXSUBADDRESS, 1, TiffDataType.TIFF_ASCII);
                    break;
            }

            result = inImage.GetField(TIFFTAG.TIFFTAG_ICCPROFILE);
            if (result != null)
                outImage.SetField(TIFFTAG.TIFFTAG_ICCPROFILE, result[0], result[1]);

            result = inImage.GetField(TIFFTAG.TIFFTAG_NUMBEROFINKS);
            if (result != null)
            {
                ushort ninks = result[0].ToUShort();
                outImage.SetField(TIFFTAG.TIFFTAG_NUMBEROFINKS, ninks);

                result = inImage.GetField(TIFFTAG.TIFFTAG_INKNAMES);
                if (result != null)
                {
                    //string inknames = result[0].ToString();
                    //int inknameslen = strlen(inknames) + 1;
                    //const char* cp = inknames;
                    //while (ninks > 1)
                    //{
                    //    cp = strchr(cp, '\0');
                    //    if (cp != null)
                    //    {
                    //        cp++;
                    //        inknameslen += (strlen(cp) + 1);
                    //    }
                    //    ninks--;
                    //}
                    //outImage.SetField(TIFFTAG.TIFFTAG_INKNAMES, inknameslen, inknames);
                }
            }

            result = inImage.GetField(TIFFTAG.TIFFTAG_PAGENUMBER);
            if (result != null)
            {
                if (m_pageNum < 0)
                {
                    /* only one input file */
                    outImage.SetField(TIFFTAG.TIFFTAG_PAGENUMBER, result[0], result[1]);
                }
                else
                    outImage.SetField(TIFFTAG.TIFFTAG_PAGENUMBER, m_pageNum++, 0);
            }

            int NTAGS = g_tags.Length;
            for (int i = 0; i < NTAGS; i++)
            {
                tagToCopy p = g_tags[i];
                CopyTag(inImage, outImage, p.tag, p.count, p.type);
            }

            return pickFuncAndCopy(inImage, outImage, bitspersample, samplesperpixel, length, width);
        }

        /*
         * Select the appropriate copy function to use.
         */
        bool pickFuncAndCopy(Tiff inImage, Tiff outImage, ushort bitspersample, ushort samplesperpixel, int length, int width)
        {
            using (TextWriter stderr = Console.Error)
            {
                FieldValue[] result = inImage.GetField(TIFFTAG.TIFFTAG_PLANARCONFIG);
                PLANARCONFIG shortv = (PLANARCONFIG)result[0].ToShort();

                if (shortv != m_config && bitspersample != 8 && samplesperpixel > 1)
                {
                    stderr.Write("{0}: Cannot handle different planar configuration w/ bits/sample != 8\n", inImage.FileName());
                    return false;
                }

                result = inImage.GetField(TIFFTAG.TIFFTAG_IMAGEWIDTH);
                uint w = result[0].ToUInt();

                result = inImage.GetField(TIFFTAG.TIFFTAG_IMAGELENGTH);
                uint l = result[0].ToUInt();

                bool bychunk;
                if (!(outImage.IsTiled() || inImage.IsTiled()))
                {
                    result = inImage.GetField(TIFFTAG.TIFFTAG_ROWSPERSTRIP);
                    int irps = result[0].ToInt();

                    /* if biased, force decoded copying to allow image subtraction */
                    bychunk = (m_bias == null) && (m_rowsperstrip == irps);
                }
                else
                {
                    /* either inImage or outImage is tiled */
                    if (m_bias != null)
                    {
                        stderr.Write("{0}: Cannot handle tiled configuration w/bias image\n", inImage.FileName());
                        return false;
                    }

                    if (outImage.IsTiled())
                    {
                        uint tw;
                        result = inImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH);
                        if (result == null)
                            tw = w;
                        else
                            tw = result[0].ToUInt();

                        uint tl;
                        result = inImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH);
                        if (result == null)
                            tl = l;
                        else
                            tl = result[0].ToUInt();

                        bychunk = (tw == m_tilewidth && tl == m_tilelength);
                    }
                    else
                    {
                        /* outImage's not, so inImage must be tiled */
                        result = inImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH);
                        uint tw = result[0].ToUInt();

                        result = inImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH);
                        uint tl = result[0].ToUInt();

                        bychunk = (tw == w && tl == m_rowsperstrip);
                    }
                }

                if (inImage.IsTiled())
                {
                    if (outImage.IsTiled())
                    {
                        /* Tiles -> Tiles */
                        if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpContigTiles2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpContigTiles2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpSeparateTiles2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpSeparateTiles2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                    }
                    else
                    {
                        /* Tiles -> Strips */
                        if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpContigTiles2ContigStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpContigTiles2SeparateStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpSeparateTiles2ContigStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpSeparateTiles2SeparateStrips(inImage, outImage, length, width, samplesperpixel);
                    }
                }
                else
                {
                    if (outImage.IsTiled())
                    {
                        /* Strips -> Tiles */
                        if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpContigStrips2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpContigStrips2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpSeparateStrips2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpSeparateStrips2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                    }
                    else
                    {
                        /* Strips -> Strips */
                        if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG && !bychunk)
                        {
                            if (m_bias != null)
                                return cpBiasedContig2Contig(inImage, outImage, length, width, samplesperpixel);

                            return cpContig2ContigByRow(inImage, outImage, length, width, samplesperpixel);
                        }
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG && bychunk)
                            return cpDecodedStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_CONTIG && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpContig2SeparateByRow(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_CONTIG)
                            return cpSeparate2ContigByRow(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PLANARCONFIG.PLANARCONFIG_SEPARATE && m_config == PLANARCONFIG.PLANARCONFIG_SEPARATE)
                            return cpSeparate2SeparateByRow(inImage, outImage, length, width, samplesperpixel);
                    }
                }

                stderr.Write("tiffcp: {0}: Don't know how to copy/convert image.\n", inImage.FileName());
            }

            return false;
        }

        /*
         * Contig -> contig by scanline for rows/strip change.
         */
        bool cpContig2ContigByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] buf = new byte[inImage.ScanlineSize()];
            for (int row = 0; row < imagelength; row++)
            {
                if (!inImage.ReadScanline(buf, row, 0) && !m_ignore)
                {
                    Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                    return false;
                }

                if (!outImage.WriteScanline(buf, row, 0))
                {
                    Tiff.Error(outImage.FileName(), "Error, can't write scanline {0}", row);
                    return false;
                }
            }

            return true;
        }

        /*
         * Contig -> contig by scanline while subtracting a bias image.
         */
        bool cpBiasedContig2Contig(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            if (spp == 1)
            {
                int biasSize = m_bias.ScanlineSize();
                int bufSize = inImage.ScanlineSize();

                FieldValue[] result = m_bias.GetField(TIFFTAG.TIFFTAG_IMAGEWIDTH);
                uint biasWidth = result[0].ToUInt();

                result = m_bias.GetField(TIFFTAG.TIFFTAG_IMAGELENGTH);
                uint biasLength = result[0].ToUInt();

                if (biasSize == bufSize && imagelength == biasLength && imagewidth == biasWidth)
                {
                    result = inImage.GetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE);
                    ushort sampleBits = result[0].ToUShort();

                    if (sampleBits == 8 || sampleBits == 16 || sampleBits == 32)
                    {
                        byte[] buf = new byte[bufSize];
                        byte[] biasBuf = new byte[bufSize];

                        for (int row = 0; row < imagelength; row++)
                        {
                            if (!inImage.ReadScanline(buf, row, 0) && !m_ignore)
                            {
                                Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                                return false;
                            }

                            if (!m_bias.ReadScanline(biasBuf, row, 0) && !m_ignore)
                            {
                                Tiff.Error(inImage.FileName(), "Error, can't read biased scanline {0}", row);
                                return false;
                            }

                            if (sampleBits == 8)
                                subtract8(buf, biasBuf, imagewidth);
                            else if (sampleBits == 16)
                                subtract16(buf, biasBuf, imagewidth);
                            else if (sampleBits == 32)
                                subtract32(buf, biasBuf, imagewidth);

                            if (!outImage.WriteScanline(buf, row, 0))
                            {
                                Tiff.Error(outImage.FileName(), "Error, can't write scanline {0}", row);
                                return false;
                            }
                        }

                        m_bias.SetDirectory(m_bias.CurrentDirectory()); /* rewind */
                        return true;
                    }
                    else
                    {
                        Tiff.Error(inImage.FileName(), "No support for biasing {0} bit pixels\n", sampleBits);
                        return false;
                    }
                }

                Tiff.Error(inImage.FileName(), "Bias image {0},{1}\nis not the same size as {2},{3}\n",
                    m_bias.FileName(), m_bias.CurrentDirectory(), inImage.FileName(), inImage.CurrentDirectory());
                return false;
            }
            else
            {
                Tiff.Error(inImage.FileName(), "Can't bias {0},{1} as it has >1 Sample/Pixel\n",
                    inImage.FileName(), inImage.CurrentDirectory());
                return false;
            }
        }

        /*
         * Strip -> strip for change in encoding.
         */
        bool cpDecodedStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            int stripsize = inImage.StripSize();
            byte[] buf = new byte[stripsize];
            int ns = inImage.NumberOfStrips();
            int row = 0;
            for (int s = 0; s < ns; s++)
            {
                int cc = (row + m_rowsperstrip > imagelength) ? inImage.VStripSize(imagelength - row) : stripsize;
                if (inImage.ReadEncodedStrip(s, buf, 0, cc) < 0 && !m_ignore)
                {
                    Tiff.Error(inImage.FileName(), "Error, can't read strip {0}", s);
                    return false;
                }

                if (outImage.WriteEncodedStrip(s, buf, cc) < 0)
                {
                    Tiff.Error(outImage.FileName(), "Error, can't write strip {0}", s);
                    return false;
                }

                row += m_rowsperstrip;
            }

            return true;
        }

        /*
         * Separate -> separate by row for rows/strip change.
         */
        bool cpSeparate2SeparateByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] buf = new byte[inImage.ScanlineSize()];

            for (UInt16 s = 0; s < spp; s++)
            {
                for (int row = 0; row < imagelength; row++)
                {
                    if (!inImage.ReadScanline(buf, row, s) && !m_ignore)
                    {
                        Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                        return false;
                    }

                    if (!outImage.WriteScanline(buf, row, s))
                    {
                        Tiff.Error(outImage.FileName(), "Error, can't write scanline {0}", row);
                        return false;
                    }
                }
            }

            return true;
        }

        /*
         * Contig -> separate by row.
         */
        bool cpContig2SeparateByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] inbuf = new byte[inImage.ScanlineSize()];
            byte[] outbuf = new byte[outImage.ScanlineSize()];

            /* unpack channels */
            for (UInt16 s = 0; s < spp; s++)
            {
                for (int row = 0; row < imagelength; row++)
                {
                    if (!inImage.ReadScanline(inbuf, row, 0) && !m_ignore)
                    {
                        Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                        return false;
                    }

                    int inp = s;
                    int outp = 0;

                    for (int n = imagewidth; n-- > 0; )
                    {
                        outbuf[outp] = inbuf[inp];
                        outp++;
                        inp += spp;
                    }

                    if (!outImage.WriteScanline(outbuf, row, s))
                    {
                        Tiff.Error(outImage.FileName(), "Error, can't write scanline {0}", row);
                        return false;
                    }
                }
            }

            return true;
        }

        /*
         * Separate -> contig by row.
         */
        bool cpSeparate2ContigByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] inbuf = new byte[inImage.ScanlineSize()];
            byte[] outbuf = new byte[outImage.ScanlineSize()];

            for (int row = 0; row < imagelength; row++)
            {
                /* merge channels */
                for (ushort s = 0; s < spp; s++)
                {
                    if (!inImage.ReadScanline(inbuf, row, s) && !m_ignore)
                    {
                        Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                        return false;
                    }

                    int inp = 0;
                    int outp = s;

                    for (int n = imagewidth; n-- > 0; )
                    {
                        outbuf[outp] = inbuf[inp];
                        inp++;
                        outp += spp;
                    }
                }

                if (!outImage.WriteScanline(outbuf, row, 0))
                {
                    Tiff.Error(outImage.FileName(), "Error, can't write scanline {0}", row);
                    return false;
                }
            }

            return true;
        }

        static void cpStripToTile(byte[] outImage, int outOffset, byte[] inImage, int inOffset, int rows, int cols, int outskew, int inskew)
        {
            int outPos = outOffset;
            int inPos = inOffset;

            while (rows-- > 0)
            {
                int j = cols;
                while (j-- > 0)
                {
                    outImage[outPos] = inImage[inPos];
                    outPos++;
                    inPos++;
                }

                outPos += outskew;
                inPos += inskew;
            }
        }

        static void cpContigBufToSeparateBuf(byte[] outImage, byte[] inImage, int inOffset, int rows, int cols, int outskew, int inskew, UInt16 spp, int bytes_per_sample)
        {
            int outPos = 0;
            int inPos = inOffset;

            while (rows-- > 0)
            {
                int j = cols;
                while (j-- > 0)
                {
                    int n = bytes_per_sample;
                    while (n-- != 0)
                    {
                        outImage[outPos] = inImage[inPos];
                        outPos++;
                        inPos++;
                    }

                    inPos += (spp - 1) * bytes_per_sample;
                }

                outPos += outskew;
                inPos += inskew;
            }
        }

        static void cpSeparateBufToContigBuf(byte[] outImage, int outOffset, byte[] inImage, int rows, int cols, int outskew, int inskew, UInt16 spp, int bytes_per_sample)
        {
            int inPos = 0;
            int outPos = outOffset;

            while (rows-- > 0)
            {
                int j = cols;
                while (j-- > 0)
                {
                    int n = bytes_per_sample;
                    while (n-- != 0)
                    {
                        outImage[outPos] = inImage[inPos];
                        outPos++;
                        inPos++;
                    }

                    outPos += (spp - 1) * bytes_per_sample;
                }

                outPos += outskew;
                inPos += inskew;
            }
        }

        static bool cpImage(Tiff inImage, Tiff outImage, readFunc fin, writeFunc fout, int imagelength, int imagewidth, UInt16 spp)
        {
            bool status = false;

            int scanlinesize = inImage.RasterScanlineSize();
            int bytes = scanlinesize * imagelength;

            /*
             * XXX: Check for integer overflow.
             */
            if (scanlinesize != 0 && imagelength != 0 && (bytes / imagelength == scanlinesize))
            {
                byte[] buf = new byte[bytes];
                if (fin(inImage, buf, imagelength, imagewidth, spp))
                    status = fout(outImage, buf, imagelength, imagewidth, spp);
            }
            else
            {
                Tiff.Error(inImage.FileName(), "Error, no space for image buffer");
            }

            return status;
        }

        /*
         * Contig strips -> contig tiles.
         */
        bool cpContigStrips2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readContigStripsIntoBuffer),
                new writeFunc(writeBufferToContigTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Contig strips -> separate tiles.
         */
        bool cpContigStrips2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readContigStripsIntoBuffer),
                new writeFunc(writeBufferToSeparateTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Separate strips -> contig tiles.
         */
        bool cpSeparateStrips2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateStripsIntoBuffer),
                new writeFunc(writeBufferToContigTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Separate strips -> separate tiles.
         */
        bool cpSeparateStrips2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateStripsIntoBuffer),
                new writeFunc(writeBufferToSeparateTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Contig strips -> contig tiles.
         */
        bool cpContigTiles2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readContigTilesIntoBuffer),
                new writeFunc(writeBufferToContigTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Contig tiles -> separate tiles.
         */
        bool cpContigTiles2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readContigTilesIntoBuffer),
                new writeFunc(writeBufferToSeparateTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Separate tiles -> contig tiles.
         */
        bool cpSeparateTiles2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateTilesIntoBuffer),
                new writeFunc(writeBufferToContigTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Separate tiles -> separate tiles (tile dimension change).
         */
        bool cpSeparateTiles2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateTilesIntoBuffer),
                new writeFunc(writeBufferToSeparateTiles),
                imagelength, imagewidth, spp);
        }

        /*
         * Contig tiles -> contig tiles (tile dimension change).
         */
        bool cpContigTiles2ContigStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readContigTilesIntoBuffer),
                new writeFunc(writeBufferToContigStrips),
                imagelength, imagewidth, spp);
        }

        /*
         * Contig tiles -> separate strips.
         */
        bool cpContigTiles2SeparateStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readContigTilesIntoBuffer),
                new writeFunc(writeBufferToSeparateStrips),
                imagelength, imagewidth, spp);
        }

        /*
         * Separate tiles -> contig strips.
         */
        bool cpSeparateTiles2ContigStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateTilesIntoBuffer),
                new writeFunc(writeBufferToContigStrips),
                imagelength, imagewidth, spp);
        }

        /*
         * Separate tiles -> separate strips.
         */
        bool cpSeparateTiles2SeparateStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, UInt16 spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateTilesIntoBuffer),
                new writeFunc(writeBufferToSeparateStrips),
                imagelength, imagewidth, spp);
        }

        bool readContigStripsIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            int scanlinesize = inImage.ScanlineSize();
            byte[] scanline = new byte[scanlinesize];

            int bufp = 0;

            for (int row = 0; row < imagelength; row++)
            {
                if (!inImage.ReadScanline(scanline, row, 0) && !m_ignore)
                {
                    Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                    return false;
                }

                Array.Copy(scanline, 0, buf, bufp, scanlinesize);
                bufp += scanlinesize;
            }

            return true;
        }

        bool readSeparateStripsIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            int scanlinesize = inImage.ScanlineSize();
            if (scanlinesize == 0)
                return false;

            byte[] scanline = new byte[scanlinesize];
            int bufp = 0;
            for (int row = 0; row < imagelength; row++)
            {
                /* merge channels */
                for (UInt16 s = 0; s < spp; s++)
                {
                    if (!inImage.ReadScanline(scanline, row, s) && !m_ignore)
                    {
                        Tiff.Error(inImage.FileName(), "Error, can't read scanline {0}", row);
                        return false;
                    }

                    int n = scanlinesize;
                    int bp = s;
                    int sbuf = 0;
                    while (n-- > 0)
                    {
                        buf[bufp + bp] = scanline[sbuf];
                        sbuf++;
                        bp += spp;
                    }
                }

                bufp += scanlinesize * spp;
            }

            return true;
        }

        bool readContigTilesIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] tilebuf = new byte[inImage.TileSize()];

            FieldValue[] result = inImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH);
            int tw = result[0].ToInt();

            result = inImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH);
            int tl = result[0].ToInt();

            int imagew = inImage.ScanlineSize();
            int tilew = inImage.TileRowSize();
            int iskew = imagew - tilew;

            int bufp = 0;

            for (int row = 0; row < imagelength; row += tl)
            {
                int nrow = (row + tl > imagelength) ? imagelength - row : tl;
                int colb = 0;

                for (int col = 0; col < imagewidth; col += tw)
                {
                    if (inImage.ReadTile(tilebuf, 0, col, row, 0, 0) < 0 && !m_ignore)
                    {
                        Tiff.Error(inImage.FileName(), "Error, can't read tile at {0} {1}", col, row);
                        return false;
                    }

                    if (colb + tilew > imagew)
                    {
                        int width = imagew - colb;
                        int oskew = tilew - width;
                        cpStripToTile(buf, bufp + colb, tilebuf, 0, nrow, width, oskew + iskew, oskew);
                    }
                    else
                        cpStripToTile(buf, bufp + colb, tilebuf, 0, nrow, tilew, iskew, 0);

                    colb += tilew;
                }

                bufp += imagew * nrow;
            }

            return true;
        }

        bool readSeparateTilesIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] tilebuf = new byte[inImage.TileSize()];

            FieldValue[] result = inImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH);
            int tw = result[0].ToInt();

            result = inImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH);
            int tl = result[0].ToInt();

            result = inImage.GetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE);
            ushort bps = result[0].ToUShort();

            Debug.Assert(bps % 8 == 0);

            ushort bytes_per_sample = (ushort)(bps / 8);

            int imagew = inImage.RasterScanlineSize();
            int tilew = inImage.TileRowSize();
            int iskew = imagew - tilew * spp;

            int bufp = 0;

            for (int row = 0; row < imagelength; row += tl)
            {
                int nrow = (row + tl > imagelength) ? imagelength - row : tl;
                int colb = 0;

                for (int col = 0; col < imagewidth; col += tw)
                {
                    for (UInt16 s = 0; s < spp; s++)
                    {
                        if (inImage.ReadTile(tilebuf, 0, col, row, 0, s) < 0 && !m_ignore)
                        {
                            Tiff.Error(inImage.FileName(), "Error, can't read tile at {0} {1}, sample {2}", col, row, s);
                            return false;
                        }

                        /*
                         * Tile is clipped horizontally.  Calculate
                         * visible portion and skewing factors.
                         */
                        if (colb + tilew * spp > imagew)
                        {
                            int width = imagew - colb;
                            int oskew = tilew * spp - width;
                            cpSeparateBufToContigBuf(buf, bufp + colb + s * bytes_per_sample, tilebuf, nrow, width / (spp * bytes_per_sample), oskew + iskew, oskew / spp, spp, bytes_per_sample);
                        }
                        else
                            cpSeparateBufToContigBuf(buf, bufp + colb + s * bytes_per_sample, tilebuf, nrow, tw, iskew, 0, spp, bytes_per_sample);
                    }

                    colb += tilew * spp;
                }

                bufp += imagew * nrow;
            }

            return true;
        }

        static bool writeBufferToContigStrips(Tiff outImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            FieldValue[] result = outImage.GetFieldDefaulted(TIFFTAG.TIFFTAG_ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();

            int strip = 0;
            int bufPos = 0;
            for (int row = 0; row < imagelength; row += rowsperstrip)
            {
                int nrows = (row + rowsperstrip > imagelength) ? imagelength - row : rowsperstrip;
                int stripsize = outImage.VStripSize(nrows);

                byte[] stripBuf = new byte[stripsize];
                Array.Copy(buf, bufPos, stripBuf, 0, stripsize);

                if (outImage.WriteEncodedStrip(strip++, stripBuf, stripsize) < 0)
                {
                    Tiff.Error(outImage.FileName(), "Error, can't write strip {0}", strip - 1);
                    return false;
                }

                bufPos += stripsize;
            }

            return true;
        }

        static bool writeBufferToSeparateStrips(Tiff outImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] obuf = new byte[outImage.StripSize()];

            FieldValue[] result = outImage.GetFieldDefaulted(TIFFTAG.TIFFTAG_ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();

            int rowsize = imagewidth * spp;
            int strip = 0;

            for (UInt16 s = 0; s < spp; s++)
            {
                for (int row = 0; row < imagelength; row += rowsperstrip)
                {
                    int nrows = (row + rowsperstrip > imagelength) ? imagelength - row : rowsperstrip;
                    int stripsize = outImage.VStripSize(nrows);

                    cpContigBufToSeparateBuf(obuf, buf, row * rowsize + s, nrows, imagewidth, 0, 0, spp, 1);
                    if (outImage.WriteEncodedStrip(strip++, obuf, stripsize) < 0)
                    {
                        Tiff.Error(outImage.FileName(), "Error, can't write strip {0}", strip - 1);
                        return false;
                    }
                }
            }

            return true;
        }

        bool writeBufferToContigTiles(Tiff outImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] obuf = new byte[outImage.TileSize()];

            FieldValue[] result = outImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH);
            int tl = result[0].ToInt();

            result = outImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH);
            int tw = result[0].ToInt();

            int imagew = outImage.ScanlineSize();
            int tilew = outImage.TileRowSize();
            int iskew = imagew - tilew;

            int bufp = 0;

            for (int row = 0; row < imagelength; row += m_tilelength)
            {
                int nrow = (row + tl > imagelength) ? imagelength - row : tl;
                int colb = 0;

                for (int col = 0; col < imagewidth; col += tw)
                {
                    /*
                     * Tile is clipped horizontally.  Calculate
                     * visible portion and skewing factors.
                     */
                    if (colb + tilew > imagew)
                    {
                        int width = imagew - colb;
                        int oskew = tilew - width;
                        cpStripToTile(obuf, 0, buf, bufp + colb, nrow, width, oskew, oskew + iskew);
                    }
                    else
                        cpStripToTile(obuf, 0, buf, bufp + colb, nrow, tilew, 0, iskew);

                    if (outImage.WriteTile(obuf, col, row, 0, 0) < 0)
                    {
                        Tiff.Error(outImage.FileName(), "Error, can't write tile at {0} {1}", col, row);
                        return false;
                    }

                    colb += tilew;
                }

                bufp += nrow * imagew;
            }

            return true;
        }

        bool writeBufferToSeparateTiles(Tiff outImage, byte[] buf, int imagelength, int imagewidth, UInt16 spp)
        {
            byte[] obuf = new byte[outImage.TileSize()];

            FieldValue[] result = outImage.GetField(TIFFTAG.TIFFTAG_TILELENGTH);
            int tl = result[0].ToInt();

            result = outImage.GetField(TIFFTAG.TIFFTAG_TILEWIDTH);
            int tw = result[0].ToInt();

            result = outImage.GetField(TIFFTAG.TIFFTAG_BITSPERSAMPLE);
            ushort bps = result[0].ToUShort();

            Debug.Assert(bps % 8 == 0);

            ushort bytes_per_sample = (ushort)(bps / 8);

            int imagew = outImage.ScanlineSize();
            int tilew = outImage.TileRowSize();
            int iimagew = outImage.RasterScanlineSize();
            int iskew = iimagew - tilew * spp;

            int bufp = 0;

            for (int row = 0; row < imagelength; row += tl)
            {
                int nrow = (row + tl > imagelength) ? imagelength - row : tl;
                int colb = 0;

                for (int col = 0; col < imagewidth; col += tw)
                {
                    for (UInt16 s = 0; s < spp; s++)
                    {
                        /*
                         * Tile is clipped horizontally.  Calculate
                         * visible portion and skewing factors.
                         */
                        if (colb + tilew > imagew)
                        {
                            int width = imagew - colb;
                            int oskew = tilew - width;

                            cpContigBufToSeparateBuf(obuf, buf, bufp + (colb * spp) + s, nrow, width / bytes_per_sample, oskew, (oskew * spp) + iskew, spp, bytes_per_sample);
                        }
                        else
                            cpContigBufToSeparateBuf(obuf, buf, bufp + (colb * spp) + s, nrow, m_tilewidth, 0, iskew, spp, bytes_per_sample);

                        if (outImage.WriteTile(obuf, col, row, 0, s) < 0)
                        {
                            Tiff.Error(outImage.FileName(), "Error, can't write tile at {0} {1} sample {2}", col, row, s);
                            return false;
                        }
                    }

                    colb += tilew;
                }

                bufp += nrow * iimagew;
            }

            return true;
        }

        static void CopyTag(Tiff inImage, Tiff outImage, TIFFTAG tag, short count, TiffDataType type)
        {
            FieldValue[] result = null;
            switch (type)
            {
                case TiffDataType.TIFF_SHORT:
                    result = inImage.GetField(tag);
                    if (result != null)
                    {
                        if (count == 1)
                            outImage.SetField(tag, result[0]);
                        else if (count == 2)
                            outImage.SetField(tag, result[0], result[1]);
                        else if (count == 4)
                            outImage.SetField(tag, result[0], result[1], result[2]);
                        else if (count == -1)
                            outImage.SetField(tag, result[0], result[1]);
                    }
                    break;
                case TiffDataType.TIFF_LONG:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                case TiffDataType.TIFF_RATIONAL:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                case TiffDataType.TIFF_ASCII:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                case TiffDataType.TIFF_DOUBLE:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                default:
                    Tiff.Error(inImage.FileName(),
                        "Data type {0} is not supported, tag {1} skipped.", tag, type);
                    break;
            }
        }

        static void subtract8(byte[] image, byte[] bias, int pixels)
        {
            int imagePos = 0;
            int biasPos = 0;
            while (pixels-- != 0)
            {
                image[imagePos] = image[imagePos] > bias[biasPos] ? (byte)(image[imagePos] - bias[biasPos]) : (byte)0;
                imagePos++;
                biasPos++;
            }
        }

        static void subtract16(byte[] i, byte[] b, int pixels)
        {
            ushort[] image = Tiff.byteArrayToUInt16(i, 0, pixels * sizeof(UInt16));
            ushort[] bias = Tiff.byteArrayToUInt16(b, 0, pixels * sizeof(UInt16));
            int imagePos = 0;
            int biasPos = 0;

            while (pixels-- != 0)
            {
                image[imagePos] = image[imagePos] > bias[biasPos] ? (ushort)(image[imagePos] - bias[biasPos]) : (ushort)0;
                imagePos++;
                biasPos++;
            }

            Tiff.uint16ToByteArray(image, 0, pixels, i, 0);
        }

        static void subtract32(byte[] i, byte[] b, int pixels)
        {
            uint[] image = Tiff.byteArrayToUInt(i, 0, pixels * sizeof(uint));
            uint[] bias = Tiff.byteArrayToUInt(b, 0, pixels * sizeof(uint));
            int imagePos = 0;
            int biasPos = 0;

            while (pixels-- != 0)
            {
                image[imagePos] = image[imagePos] > bias[biasPos] ? image[imagePos] - bias[biasPos] : 0;
                imagePos++;
                biasPos++;
            }

            Tiff.uintToByteArray(image, 0, pixels, i, 0);
        }
    }
}
