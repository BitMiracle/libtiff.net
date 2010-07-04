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
using BitMiracle.LibTiff.Classic;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace BitMiracle.TiffCP
{
    class Copier
    {
        struct tagToCopy
        {
            public tagToCopy(TiffTag _tag, short _count, TiffType _type)
            {
                tag = _tag;
                count = _count;
                type = _type;
            }

            public TiffTag tag;
            public short count;
            public TiffType type;
        };

        static tagToCopy[] g_tags = 
        {
            new tagToCopy(TiffTag.SUBFILETYPE, 1, TiffType.LONG), 
            new tagToCopy(TiffTag.THRESHHOLDING, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.DOCUMENTNAME, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.IMAGEDESCRIPTION, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.MAKE, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.MODEL, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.MINSAMPLEVALUE, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.MAXSAMPLEVALUE, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.XRESOLUTION, 1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.YRESOLUTION, 1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.PAGENAME, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.XPOSITION, 1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.YPOSITION, 1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.RESOLUTIONUNIT, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.SOFTWARE, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.DATETIME, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.ARTIST, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.HOSTCOMPUTER, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.WHITEPOINT, -1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.PRIMARYCHROMATICITIES, -1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.HALFTONEHINTS, 2, TiffType.SHORT), 
            new tagToCopy(TiffTag.INKSET, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.DOTRANGE, 2, TiffType.SHORT), 
            new tagToCopy(TiffTag.TARGETPRINTER, 1, TiffType.ASCII), 
            new tagToCopy(TiffTag.SAMPLEFORMAT, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.YCBCRCOEFFICIENTS, -1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.YCBCRSUBSAMPLING, 2, TiffType.SHORT), 
            new tagToCopy(TiffTag.YCBCRPOSITIONING, 1, TiffType.SHORT), 
            new tagToCopy(TiffTag.REFERENCEBLACKWHITE, -1, TiffType.RATIONAL), 
            new tagToCopy(TiffTag.EXTRASAMPLES, -1, TiffType.SHORT), 
            new tagToCopy(TiffTag.SMINSAMPLEVALUE, 1, TiffType.DOUBLE), 
            new tagToCopy(TiffTag.SMAXSAMPLEVALUE, 1, TiffType.DOUBLE), 
            new tagToCopy(TiffTag.STONITS, 1, TiffType.DOUBLE), 
        };

        delegate bool readFunc(Tiff inImage, byte[] buf, int imagelength, int imagewidth, short spp);
        delegate bool writeFunc(Tiff outImage, byte[] buf, int imagelength, int imagewidth, short spp);

        public int m_outtiled = -1;
        public int m_tilewidth;
        public int m_tilelength;
        public PlanarConfig m_config;
        public Compression m_compression;
        public short m_predictor;
        public FillOrder m_fillorder;
        public int m_rowsperstrip;
        public Group3Opt m_g3opts;
        public bool m_ignore = false; /* if true, ignore read errors */
        public Group3Opt m_defg3opts = Group3Opt.UNKNOWN;
        public Compression m_defcompression = (Compression)(-1);
        public short m_defpredictor = -1;
        public Tiff m_bias = null;
        public int m_pageNum = 0;
        public int m_pageInSeq = 0;

        Orientation m_orientation;
        int m_quality = 75; /* JPEG quality */
        JpegColorMode m_jpegcolormode = JpegColorMode.RGB;

        public bool ProcessCompressOptions(string opt)
        {
            if (opt == "none")
            {
                m_defcompression = Compression.NONE;
            }
            else if (opt == "packbits")
            {
                m_defcompression = Compression.PACKBITS;
            }
            else if (opt.StartsWith("jpeg"))
            {
                m_defcompression = Compression.JPEG;

                string[] options = opt.Split(new char[] { ':' });
                for (int i = 1; i < options.Length; i++)
                {
                    if (char.IsDigit(options[i][0]))
                        m_quality = int.Parse(options[i], CultureInfo.InvariantCulture);
                    else if (options[i] == "r")
                        m_jpegcolormode = JpegColorMode.RAW;
                    else
                        return false;
                }
            }
            else if (opt.StartsWith("g3"))
            {
                if (!processG3Options(opt))
                    return false;

                m_defcompression = Compression.CCITTFAX3;
            }
            else if (opt == "g4")
            {
                m_defcompression = Compression.CCITTFAX4;
            }
            else if (opt.StartsWith("lzw"))
            {
                int n = opt.IndexOf(':');
                if (n != -1 && n < (opt.Length - 1))
                    m_defpredictor = short.Parse(opt.Substring(n + 1));

                m_defcompression = Compression.LZW;
            }
            else if (opt.StartsWith("zip"))
            {
                int n = opt.IndexOf(':');
                if (n != -1 && n < (opt.Length - 1))
                    m_defpredictor = short.Parse(opt.Substring(n + 1));

                m_defcompression = Compression.ADOBE_DEFLATE;
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
                if (m_defg3opts == Group3Opt.UNKNOWN)
                    m_defg3opts = 0;

                for (int i = 1; i < options.Length; i++)
                {
                    if (options[i].StartsWith("1d"))
                        m_defg3opts &= ~Group3Opt.ENCODING2D;
                    else if (options[i].StartsWith("2d"))
                        m_defg3opts |= Group3Opt.ENCODING2D;
                    else if (options[i].StartsWith("fill"))
                        m_defg3opts |= Group3Opt.FILLBITS;
                    else
                        return false;
                }
            }

            return true;
        }

        public bool Copy(Tiff inImage, Tiff outImage)
        {
            int width = 0;
            FieldValue[] result = inImage.GetField(TiffTag.IMAGEWIDTH);
            if (result != null)
            {
                width = result[0].ToInt();
                outImage.SetField(TiffTag.IMAGEWIDTH, width);
            }

            int length = 0;
            result = inImage.GetField(TiffTag.IMAGELENGTH);
            if (result != null)
            {
                length = result[0].ToInt();
                outImage.SetField(TiffTag.IMAGELENGTH, length);
            }

            short bitspersample = 1;
            result = inImage.GetField(TiffTag.BITSPERSAMPLE);
            if (result != null)
            {
                bitspersample = result[0].ToShort();
                outImage.SetField(TiffTag.BITSPERSAMPLE, bitspersample);
            }

            short samplesperpixel = 1;
            result = inImage.GetField(TiffTag.SAMPLESPERPIXEL);
            if (result != null)
            {
                samplesperpixel = result[0].ToShort();
                outImage.SetField(TiffTag.SAMPLESPERPIXEL, samplesperpixel);
            }

            if (m_compression != (Compression)(-1))
                outImage.SetField(TiffTag.COMPRESSION, m_compression);
            else
            {
                result = inImage.GetField(TiffTag.COMPRESSION);
                if (result != null)
                {
                    m_compression = (Compression)result[0].ToInt();
                    outImage.SetField(TiffTag.COMPRESSION, m_compression);
                }
            }

            result = inImage.GetFieldDefaulted(TiffTag.COMPRESSION);
            Compression input_compression = (Compression)result[0].ToShort();

            result = inImage.GetFieldDefaulted(TiffTag.PHOTOMETRIC);
            Photometric input_photometric = (Photometric)result[0].ToShort();
    
            if (input_compression == Compression.JPEG)
            {
                /* Force conversion to RGB */
                inImage.SetField(TiffTag.JPEGCOLORMODE, JpegColorMode.RGB);
            }
            else if (input_photometric == Photometric.YCBCR)
            {
                /* Otherwise, can't handle subsampled input */
                result = inImage.GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
                short subsamplinghor = result[0].ToShort();
                short subsamplingver = result[1].ToShort();

                if (subsamplinghor != 1 || subsamplingver != 1)
                {
                    Console.Error.WriteLine("tiffcp: {0}: Can't copy/convert subsampled image.", inImage.FileName());
                    return false;
                }
            }

            if (m_compression == Compression.JPEG)
            {
                if (input_photometric == Photometric.RGB && m_jpegcolormode == JpegColorMode.RGB)
                    outImage.SetField(TiffTag.PHOTOMETRIC, Photometric.YCBCR);
                else
                    outImage.SetField(TiffTag.PHOTOMETRIC, input_photometric);
            }
            else if (m_compression == Compression.SGILOG || m_compression == Compression.SGILOG24)
            {
                outImage.SetField(TiffTag.PHOTOMETRIC, samplesperpixel == 1 ? Photometric.LOGL : Photometric.LOGLUV);
            }
            else
            {
                if (input_compression != Compression.JPEG)
                    copyTag(inImage, outImage, TiffTag.PHOTOMETRIC, 1, TiffType.SHORT);
            }

            if (m_fillorder != 0)
                outImage.SetField(TiffTag.FILLORDER, m_fillorder);
            else
                copyTag(inImage, outImage, TiffTag.FILLORDER, 1, TiffType.SHORT);

            /*
             * Will copy `Orientation' tag from input image
             */
            result = inImage.GetFieldDefaulted(TiffTag.ORIENTATION);
            m_orientation = (Orientation)result[0].ToByte();
            switch (m_orientation)
            {
                case Orientation.BOTRIGHT:
                case Orientation.RIGHTBOT:
                    Tiff.Warning(inImage.FileName(), "using bottom-left orientation");
                    m_orientation = Orientation.BOTLEFT;
                    break;

                case Orientation.LEFTBOT:
                case Orientation.BOTLEFT:
                    break;

                case Orientation.TOPRIGHT:
                case Orientation.RIGHTTOP:
                default:
                    Tiff.Warning(inImage.FileName(), "using top-left orientation");
                    m_orientation = Orientation.TOPLEFT;
                    break;

                case Orientation.LEFTTOP:
                case Orientation.TOPLEFT:
                    break;
            }

            outImage.SetField(TiffTag.ORIENTATION, m_orientation);

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
                    result = inImage.GetFieldDefaulted(TiffTag.TILEWIDTH);
                    if (result != null)
                        m_tilewidth = result[0].ToInt();
                }

                if (m_tilelength == -1)
                {
                    result = inImage.GetFieldDefaulted(TiffTag.TILELENGTH);
                    if (result != null)
                        m_tilelength = result[0].ToInt();
                }

                outImage.DefaultTileSize(ref m_tilewidth, ref m_tilelength);
                outImage.SetField(TiffTag.TILEWIDTH, m_tilewidth);
                outImage.SetField(TiffTag.TILELENGTH, m_tilelength);
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
                    result = inImage.GetField(TiffTag.ROWSPERSTRIP);
                    if (result == null)
                        m_rowsperstrip = outImage.DefaultStripSize(m_rowsperstrip);
                    else
                        m_rowsperstrip = result[0].ToInt();

                    if (m_rowsperstrip > length && m_rowsperstrip != -1)
                        m_rowsperstrip = length;
                }
                else if (m_rowsperstrip == -1)
                    m_rowsperstrip = length;

                outImage.SetField(TiffTag.ROWSPERSTRIP, m_rowsperstrip);
            }

            if (m_config != PlanarConfig.UNKNOWN)
                outImage.SetField(TiffTag.PLANARCONFIG, m_config);
            else
            {
                result = inImage.GetField(TiffTag.PLANARCONFIG);
                if (result != null)
                {
                    m_config = (PlanarConfig)result[0].ToShort();
                    outImage.SetField(TiffTag.PLANARCONFIG, m_config);
                }
            }

            if (samplesperpixel <= 4)
                copyTag(inImage, outImage, TiffTag.TRANSFERFUNCTION, 4, TiffType.SHORT);

            copyTag(inImage, outImage, TiffTag.COLORMAP, 4, TiffType.SHORT);

            /* SMinSampleValue & SMaxSampleValue */
            switch (m_compression)
            {
                case Compression.JPEG:
                    outImage.SetField(TiffTag.JPEGQUALITY, m_quality);
                    outImage.SetField(TiffTag.JPEGCOLORMODE, m_jpegcolormode);
                    break;
                case Compression.LZW:
                case Compression.ADOBE_DEFLATE:
                case Compression.DEFLATE:
                    if (m_predictor != -1)
                        outImage.SetField(TiffTag.PREDICTOR, m_predictor);
                    else
                    {
                        result = inImage.GetField(TiffTag.PREDICTOR);
                        if (result != null)
                        {
                            m_predictor = result[0].ToShort();
                            outImage.SetField(TiffTag.PREDICTOR, m_predictor);
                        }
                    }
                    break;
                case Compression.CCITTFAX3:
                case Compression.CCITTFAX4:
                    if (m_compression == Compression.CCITTFAX3)
                    {
                        if (m_g3opts != Group3Opt.UNKNOWN)
                            outImage.SetField(TiffTag.GROUP3OPTIONS, m_g3opts);
                        else
                        {
                            result = inImage.GetField(TiffTag.GROUP3OPTIONS);
                            if (result != null)
                            {
                                m_g3opts = (Group3Opt)result[0].ToShort();
                                outImage.SetField(TiffTag.GROUP3OPTIONS, m_g3opts);
                            }
                        }
                    }
                    else
                        copyTag(inImage, outImage, TiffTag.GROUP4OPTIONS, 1, TiffType.LONG);

                    copyTag(inImage, outImage, TiffTag.BADFAXLINES, 1, TiffType.LONG);
                    copyTag(inImage, outImage, TiffTag.CLEANFAXDATA, 1, TiffType.LONG);
                    copyTag(inImage, outImage, TiffTag.CONSECUTIVEBADFAXLINES, 1, TiffType.LONG);
                    copyTag(inImage, outImage, TiffTag.FAXRECVPARAMS, 1, TiffType.LONG);
                    copyTag(inImage, outImage, TiffTag.FAXRECVTIME, 1, TiffType.LONG);
                    copyTag(inImage, outImage, TiffTag.FAXSUBADDRESS, 1, TiffType.ASCII);
                    break;
            }

            result = inImage.GetField(TiffTag.ICCPROFILE);
            if (result != null)
                outImage.SetField(TiffTag.ICCPROFILE, result[0], result[1]);

            result = inImage.GetField(TiffTag.NUMBEROFINKS);
            if (result != null)
            {
                short ninks = result[0].ToShort();
                outImage.SetField(TiffTag.NUMBEROFINKS, ninks);

                result = inImage.GetField(TiffTag.INKNAMES);
                if (result != null)
                {
                    string inknames = result[0].ToString();
                    string[] parts = inknames.Split(new char[] { '\0' });

                    int inknameslen = 0;
                    foreach (string part in parts)
                        inknameslen += part.Length + 1;

                    outImage.SetField(TiffTag.INKNAMES, inknameslen, inknames);
                }
            }

            result = inImage.GetField(TiffTag.PAGENUMBER);
            if (m_pageInSeq == 1)
            {
                if (m_pageNum < 0)
                {
                    /* only one input file */ 
                    if (result != null) 
                        outImage.SetField(TiffTag.PAGENUMBER, result[0], result[1]);
                }
                else
                {
                    outImage.SetField(TiffTag.PAGENUMBER, m_pageNum++, 0);
                }
            }
            else
            {
                if (result != null)
                {
                    if (m_pageNum < 0)
                    {
                        /* only one input file */
                        outImage.SetField(TiffTag.PAGENUMBER, result[0], result[1]);
                    }
                    else
                    {
                        outImage.SetField(TiffTag.PAGENUMBER, m_pageNum++, 0);
                    }
                }
            }

            int NTAGS = g_tags.Length;
            for (int i = 0; i < NTAGS; i++)
            {
                tagToCopy p = g_tags[i];
                copyTag(inImage, outImage, p.tag, p.count, p.type);
            }

            return pickFuncAndCopy(inImage, outImage, bitspersample, samplesperpixel, length, width);
        }

        /*
         * Select the appropriate copy function to use.
         */
        bool pickFuncAndCopy(Tiff inImage, Tiff outImage, short bitspersample, short samplesperpixel, int length, int width)
        {
            using (TextWriter stderr = Console.Error)
            {
                FieldValue[] result = inImage.GetField(TiffTag.PLANARCONFIG);
                PlanarConfig shortv = (PlanarConfig)result[0].ToShort();

                if (shortv != m_config && bitspersample != 8 && samplesperpixel > 1)
                {
                    stderr.Write("{0}: Cannot handle different planar configuration w/ bits/sample != 8\n", inImage.FileName());
                    return false;
                }

                result = inImage.GetField(TiffTag.IMAGEWIDTH);
                int w = result[0].ToInt();

                result = inImage.GetField(TiffTag.IMAGELENGTH);
                int l = result[0].ToInt();

                bool bychunk;
                if (!(outImage.IsTiled() || inImage.IsTiled()))
                {
                    result = inImage.GetField(TiffTag.ROWSPERSTRIP);
                    if (result != null)
                    {
                        int irps = result[0].ToInt();

                        /* if biased, force decoded copying to allow image subtraction */
                        bychunk = (m_bias == null) && (m_rowsperstrip == irps);
                    }
                    else
                        bychunk = false;
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
                        int tw;
                        result = inImage.GetField(TiffTag.TILEWIDTH);
                        if (result == null)
                            tw = w;
                        else
                            tw = result[0].ToInt();

                        int tl;
                        result = inImage.GetField(TiffTag.TILELENGTH);
                        if (result == null)
                            tl = l;
                        else
                            tl = result[0].ToInt();

                        bychunk = (tw == m_tilewidth && tl == m_tilelength);
                    }
                    else
                    {
                        /* outImage's not, so inImage must be tiled */
                        result = inImage.GetField(TiffTag.TILEWIDTH);
                        int tw = result[0].ToInt();

                        result = inImage.GetField(TiffTag.TILELENGTH);
                        int tl = result[0].ToInt();

                        bychunk = (tw == w && tl == m_rowsperstrip);
                    }
                }

                if (inImage.IsTiled())
                {
                    if (outImage.IsTiled())
                    {
                        /* Tiles -> Tiles */
                        if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.CONTIG)
                            return cpContigTiles2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.SEPARATE)
                            return cpContigTiles2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.CONTIG)
                            return cpSeparateTiles2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.SEPARATE)
                            return cpSeparateTiles2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                    }
                    else
                    {
                        /* Tiles -> Strips */
                        if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.CONTIG)
                            return cpContigTiles2ContigStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.SEPARATE)
                            return cpContigTiles2SeparateStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.CONTIG)
                            return cpSeparateTiles2ContigStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.SEPARATE)
                            return cpSeparateTiles2SeparateStrips(inImage, outImage, length, width, samplesperpixel);
                    }
                }
                else
                {
                    if (outImage.IsTiled())
                    {
                        /* Strips -> Tiles */
                        if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.CONTIG)
                            return cpContigStrips2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.SEPARATE)
                            return cpContigStrips2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.CONTIG)
                            return cpSeparateStrips2ContigTiles(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.SEPARATE)
                            return cpSeparateStrips2SeparateTiles(inImage, outImage, length, width, samplesperpixel);
                    }
                    else
                    {
                        /* Strips -> Strips */
                        if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.CONTIG && !bychunk)
                        {
                            if (m_bias != null)
                                return cpBiasedContig2Contig(inImage, outImage, length, width, samplesperpixel);

                            return cpContig2ContigByRow(inImage, outImage, length, width, samplesperpixel);
                        }
                        else if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.CONTIG && bychunk)
                            return cpDecodedStrips(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.CONTIG && m_config == PlanarConfig.SEPARATE)
                            return cpContig2SeparateByRow(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.CONTIG)
                            return cpSeparate2ContigByRow(inImage, outImage, length, width, samplesperpixel);
                        else if (shortv == PlanarConfig.SEPARATE && m_config == PlanarConfig.SEPARATE)
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
        bool cpContig2ContigByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpBiasedContig2Contig(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
        {
            if (spp == 1)
            {
                int biasSize = m_bias.ScanlineSize();
                int bufSize = inImage.ScanlineSize();

                FieldValue[] result = m_bias.GetField(TiffTag.IMAGEWIDTH);
                int biasWidth = result[0].ToInt();

                result = m_bias.GetField(TiffTag.IMAGELENGTH);
                int biasLength = result[0].ToInt();

                if (biasSize == bufSize && imagelength == biasLength && imagewidth == biasWidth)
                {
                    result = inImage.GetField(TiffTag.BITSPERSAMPLE);
                    short sampleBits = result[0].ToShort();

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
        bool cpDecodedStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparate2SeparateByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
        {
            byte[] buf = new byte[inImage.ScanlineSize()];

            for (short s = 0; s < spp; s++)
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
        bool cpContig2SeparateByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
        {
            byte[] inbuf = new byte[inImage.ScanlineSize()];
            byte[] outbuf = new byte[outImage.ScanlineSize()];

            /* unpack channels */
            for (short s = 0; s < spp; s++)
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
        bool cpSeparate2ContigByRow(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
        {
            byte[] inbuf = new byte[inImage.ScanlineSize()];
            byte[] outbuf = new byte[outImage.ScanlineSize()];

            for (int row = 0; row < imagelength; row++)
            {
                /* merge channels */
                for (short s = 0; s < spp; s++)
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

        static void cpContigBufToSeparateBuf(byte[] outImage, byte[] inImage, int inOffset, int rows, int cols, int outskew, int inskew, short spp, int bytes_per_sample)
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

        static void cpSeparateBufToContigBuf(byte[] outImage, int outOffset, byte[] inImage, int rows, int cols, int outskew, int inskew, short spp, int bytes_per_sample)
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

        static bool cpImage(Tiff inImage, Tiff outImage, readFunc fin, writeFunc fout, int imagelength, int imagewidth, short spp)
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
        bool cpContigStrips2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpContigStrips2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparateStrips2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparateStrips2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpContigTiles2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpContigTiles2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparateTiles2ContigTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparateTiles2SeparateTiles(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpContigTiles2ContigStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpContigTiles2SeparateStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparateTiles2ContigStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
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
        bool cpSeparateTiles2SeparateStrips(Tiff inImage, Tiff outImage, int imagelength, int imagewidth, short spp)
        {
            return cpImage(
                inImage, outImage,
                new readFunc(readSeparateTilesIntoBuffer),
                new writeFunc(writeBufferToSeparateStrips),
                imagelength, imagewidth, spp);
        }

        bool readContigStripsIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, short spp)
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

        bool readSeparateStripsIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            int scanlinesize = inImage.ScanlineSize();
            if (scanlinesize == 0)
                return false;

            byte[] scanline = new byte[scanlinesize];
            int bufp = 0;
            for (int row = 0; row < imagelength; row++)
            {
                /* merge channels */
                for (short s = 0; s < spp; s++)
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

        bool readContigTilesIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            byte[] tilebuf = new byte[inImage.TileSize()];

            FieldValue[] result = inImage.GetField(TiffTag.TILEWIDTH);
            int tw = result[0].ToInt();

            result = inImage.GetField(TiffTag.TILELENGTH);
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

        bool readSeparateTilesIntoBuffer(Tiff inImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            byte[] tilebuf = new byte[inImage.TileSize()];

            FieldValue[] result = inImage.GetField(TiffTag.TILEWIDTH);
            int tw = result[0].ToInt();

            result = inImage.GetField(TiffTag.TILELENGTH);
            int tl = result[0].ToInt();

            result = inImage.GetField(TiffTag.BITSPERSAMPLE);
            short bps = result[0].ToShort();

            Debug.Assert(bps % 8 == 0);

            short bytes_per_sample = (short)(bps / 8);

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
                    for (short s = 0; s < spp; s++)
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

        static bool writeBufferToContigStrips(Tiff outImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            FieldValue[] result = outImage.GetFieldDefaulted(TiffTag.ROWSPERSTRIP);
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

        static bool writeBufferToSeparateStrips(Tiff outImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            byte[] obuf = new byte[outImage.StripSize()];

            FieldValue[] result = outImage.GetFieldDefaulted(TiffTag.ROWSPERSTRIP);
            int rowsperstrip = result[0].ToInt();

            int rowsize = imagewidth * spp;
            int strip = 0;

            for (short s = 0; s < spp; s++)
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

        bool writeBufferToContigTiles(Tiff outImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            byte[] obuf = new byte[outImage.TileSize()];

            FieldValue[] result = outImage.GetField(TiffTag.TILELENGTH);
            int tl = result[0].ToInt();

            result = outImage.GetField(TiffTag.TILEWIDTH);
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

        bool writeBufferToSeparateTiles(Tiff outImage, byte[] buf, int imagelength, int imagewidth, short spp)
        {
            byte[] obuf = new byte[outImage.TileSize()];

            FieldValue[] result = outImage.GetField(TiffTag.TILELENGTH);
            int tl = result[0].ToInt();

            result = outImage.GetField(TiffTag.TILEWIDTH);
            int tw = result[0].ToInt();

            result = outImage.GetField(TiffTag.BITSPERSAMPLE);
            short bps = result[0].ToShort();

            Debug.Assert(bps % 8 == 0);

            short bytes_per_sample = (short)(bps / 8);

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
                    for (short s = 0; s < spp; s++)
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

        private static void copyTag(Tiff inImage, Tiff outImage, TiffTag tag, short count, TiffType type)
        {
            FieldValue[] result = null;
            switch (type)
            {
                case TiffType.SHORT:
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
                case TiffType.LONG:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                case TiffType.RATIONAL:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                case TiffType.ASCII:
                    result = inImage.GetField(tag);
                    if (result != null)
                        outImage.SetField(tag, result[0]);
                    break;
                case TiffType.DOUBLE:
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
            short[] image = Tiff.ByteArrayToShorts(i, 0, pixels * sizeof(short));
            short[] bias = Tiff.ByteArrayToShorts(b, 0, pixels * sizeof(short));
            int imagePos = 0;
            int biasPos = 0;

            while (pixels-- != 0)
            {
                image[imagePos] = image[imagePos] > bias[biasPos] ? (short)(image[imagePos] - bias[biasPos]) : (short)0;
                imagePos++;
                biasPos++;
            }

            Tiff.ShortsToByteArray(image, 0, pixels, i, 0);
        }

        static void subtract32(byte[] i, byte[] b, int pixels)
        {
            int[] image = Tiff.ByteArrayToInts(i, 0, pixels * sizeof(int));
            int[] bias = Tiff.ByteArrayToInts(b, 0, pixels * sizeof(int));
            int imagePos = 0;
            int biasPos = 0;

            while (pixels-- != 0)
            {
                image[imagePos] = image[imagePos] > bias[biasPos] ? image[imagePos] - bias[biasPos] : 0;
                imagePos++;
                biasPos++;
            }

            Tiff.IntsToByteArray(image, 0, pixels, i, 0);
        }
    }
}
