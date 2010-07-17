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
using System.Globalization;

using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// RGBA-style image support. Provides methods for decoding images into RGBA (or other) format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TiffRgbaImage</b> provide a high-level interface through which TIFF images may be read
    /// into memory. Images may be strip- or tile-based and have a variety of different
    /// characteristics: bits/sample, samples/pixel, photometric, etc. The target raster format
    /// can be customized to a particular application's needs by installing custom methods that
    /// manipulate image data according to application requirements.
    /// </para><para>
    /// The default usage for this class: check if an image can be processed using
    /// <see cref="BitMiracle.LibTiff.Classic.Tiff.RGBAImageOK"/>, construct an instance of
    /// <b>TiffRgbaImage</b> using <see cref="Create"/> and then read and decode an image into a
    /// target raster using <see cref="GetRaster"/>. <see cref="GetRaster"/> can be called
    /// multiple times to decode an image using different state parameters. If multiple images
    /// are to be displayed and there is not enough space for each of the decoded rasters,
    /// multiple instances of <b>TiffRgbaImage</b> can be managed and then calls can be made to
    /// <see cref="GetRaster"/> as needed to display an image.</para>
    /// <para>
    /// To use the core support for reading and processing TIFF images, but write the resulting
    /// raster data in a different format one need only override the "put methods" used to store
    /// raster data. These methods are initially setup by <see cref="Create"/> to point to methods
    /// that pack raster data in the default ABGR pixel format. Two different methods are used
    /// according to the physical organization of the image data in the file: one for
    /// <see cref="TiffTag.PLANARCONFIG"/> = <see cref="PlanarConfig"/>.CONTIG (packed samples),
    /// and another for <see cref="TiffTag.PLANARCONFIG"/> = <see cref="PlanarConfig"/>.SEPARATE
    /// (separated samples). Note that this mechanism can be used to transform the data before 
    /// storing it in the raster. For example one can convert data to colormap indices for display
    /// on a colormap display.</para><para>
    /// To setup custom "put" method please use <see cref="PutContig"/> property for contiguously
    /// packed samples and/or <see cref="PutSeparate"/> property for separated samples.</para>
    /// <para>
    /// The methods of <b>TiffRgbaImage</b> support the most commonly encountered flavors of TIFF.
    /// It is possible to extend this support by overriding the "get method" invoked by
    /// <see cref="GetRaster"/> to read TIFF image data. Details of doing this are a bit involved,
    /// it is best to make a copy of an existing get method and modify it to suit the needs of an
    /// application. To setup custom "get" method please use <see cref="Get"/> property.</para>
    /// </remarks>
    public class TiffRgbaImage
    {       
        internal const string photoTag = "PhotometricInterpretation";

        /// <summary>
        /// image handle
        /// </summary>
        private Tiff tif;

        /// <summary>
        /// stop on read error
        /// </summary>
        private bool stoponerr;

        /// <summary>
        /// data is packed/separate
        /// </summary>
        private bool isContig;

        /// <summary>
        /// type of alpha data present
        /// </summary>
        private ExtraSample alpha;

        /// <summary>
        /// image width
        /// </summary>
        private int width;

        /// <summary>
        /// image height
        /// </summary>
        private int height;

        /// <summary>
        /// image bits/sample
        /// </summary>
        private short bitspersample;

        /// <summary>
        /// image samples/pixel
        /// </summary>
        private short samplesperpixel;

        /// <summary>
        /// image orientation
        /// </summary>
        private Orientation orientation;

        /// <summary>
        /// requested orientation
        /// </summary>
        private Orientation req_orientation;

        /// <summary>
        /// image photometric interp
        /// </summary>
        private Photometric photometric;

        /// <summary>
        /// colormap pallete
        /// </summary>
        private short[] redcmap;

        private short[] greencmap;

        private short[] bluecmap;

        private GetDelegate get;
        private PutContigDelegate putContig;
        private PutSeparateDelegate putSeparate;

        /// <summary>
        /// sample mapping array
        /// </summary>
        private byte[] Map;

        /// <summary>
        /// black and white map
        /// </summary>
        private int[][] BWmap;

        /// <summary>
        /// palette image map
        /// </summary>
        private int[][] PALmap;

        /// <summary>
        /// YCbCr conversion state
        /// </summary>
        private TiffYCbCrToRGB ycbcr;

        /// <summary>
        /// CIE L*a*b conversion state
        /// </summary>
        private TiffCIELabToRGB cielab;

        internal int row_offset;

        internal int col_offset;

        private static TiffDisplay display_sRGB = new TiffDisplay(
            // XYZ -> luminance matrix
            new float[] { 3.2410F, -1.5374F, -0.4986F },
            new float[] { -0.9692F, 1.8760F, 0.0416F },
            new float[] { 0.0556F, -0.2040F, 1.0570F },
            100.0F, 100.0F, 100.0F,  // Light o/p for reference white
            255, 255, 255,  // Pixel values for ref. white
            1.0F, 1.0F, 1.0F,  // Residual light o/p for black pixel
            2.4F, 2.4F, 2.4F  // Gamma values for the three guns
        );

        private const int A1 = 0xff << 24;

        // Helper constants used in Orientation tag handling
        private const int FLIP_VERTICALLY = 0x01;
        private const int FLIP_HORIZONTALLY = 0x02;

        /// <summary>
        /// Delegate for "put" method (the method that is called to pack pixel data in the raster)
        /// used when converting contiguously packed samples.
        /// </summary>
        /// <remarks><para>
        /// The image reading and conversion methods invoke "put" methods to copy/image/whatever
        /// tiles of raw image data. A default set of methods is provided to convert/copy raw
        /// image data to 8-bit packed ABGR format rasters. Applications can supply alternate
        /// methods that unpack the data into a different format or, for example, unpack the data
        /// and draw the unpacked raster on the display.
        /// </para><para>
        /// To setup custom "put" method for contiguously packed samples please use
        /// <see cref="PutContig"/> property.
        /// </para></remarks>
        public delegate void PutContigDelegate(
            TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew,
            int toskew, byte[] pp, int ppOffset);

        /// <summary>
        /// Delegate for "put" method (the method that is called to pack pixel data in the raster)
        /// used when converting separated samples.
        /// </summary>
        /// <remarks><para>
        /// The image reading and conversion methods invoke "put" methods to copy/image/whatever
        /// tiles of raw image data. A default set of methods is provided to convert/copy raw
        /// image data to 8-bit packed ABGR format rasters. Applications can supply alternate
        /// methods that unpack the data into a different format or, for example, unpack the data
        /// and draw the unpacked raster on the display.
        /// </para><para>
        /// To setup custom "put" method for separated samples please use
        /// <see cref="PutSeparate"/> property.
        /// </para></remarks>
        public delegate void PutSeparateDelegate(
            TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew,
            int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset);

        /// <summary>
        /// Delegate for "get" method (the method that is called to produce RGBA raster).
        /// </summary>
        /// <remarks><para>
        /// A default set of methods is provided to read and convert/copy raw image data to 8-bit
        /// packed ABGR format rasters. Applications can supply alternate method for this.
        /// </para><para>
        /// To setup custom "get" method please use <see cref="Get"/> property.
        /// </para></remarks>
        public delegate bool GetDelegate(TiffRgbaImage img, int[] raster, int offset, int width, int height);

        private TiffRgbaImage()
        {
        }

        /// <summary>
        /// Creates new instance of the <see cref="TiffRgbaImage"/> class.
        /// </summary>
        /// <param name="tif">
        /// The instance of the <see cref="BitMiracle.LibTiff.Classic"/> class used to retrieve
        /// image data.
        /// </param>
        /// <param name="stopOnError">
        /// if set to <c>true</c> then an error will terminate the conversion; otherwise "get"
        /// methods will continue processing data until all the possible data in the image have
        /// been requested.
        /// </param>
        /// <param name="errorMsg">The error message (if any) gets placed here.</param>
        /// <returns>
        /// New instance of the <see cref="TiffRgbaImage"/> class if the image specified
        /// by <paramref name="tif"/> can be converted to RGBA format; otherwise, <c>null</c> is
        /// returned and <paramref name="errorMsg"/> contains the reason why it is being
        /// rejected.
        /// </returns>
        public static TiffRgbaImage Create(Tiff tif, bool stopOnError, out string errorMsg)
        {
            errorMsg = null;

            // Initialize to normal values
            TiffRgbaImage img = new TiffRgbaImage();
            img.row_offset = 0;
            img.col_offset = 0;
            img.redcmap = null;
            img.greencmap = null;
            img.bluecmap = null;
            img.req_orientation = Orientation.BOTLEFT; // It is the default
            img.tif = tif;
            img.stoponerr = stopOnError;

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
                    errorMsg = string.Format(CultureInfo.InvariantCulture,
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
                        if (img.samplesperpixel > 3)
                        {
                            // Workaround for some images without correct info about alpha channel
                            img.alpha = ExtraSample.ASSOCALPHA;
                        }
                        break;

                    case ExtraSample.ASSOCALPHA:
                        // data is pre-multiplied
                    case ExtraSample.UNASSALPHA:
                        // data is not pre-multiplied
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
                        errorMsg = string.Format(CultureInfo.InvariantCulture, "Missing needed {0} tag", photoTag);
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
                        errorMsg = string.Format(CultureInfo.InvariantCulture, "Missing required \"Colormap\" tag");
                        return null;
                    }

                    short[] red_orig = result[0].ToShortArray();
                    short[] green_orig = result[1].ToShortArray();
                    short[] blue_orig = result[2].ToShortArray();

                    // copy the colormaps so we can modify them
                    int n_color = (1 << img.bitspersample);
                    img.redcmap = new short[n_color];
                    img.greencmap = new short[n_color];
                    img.bluecmap = new short[n_color];

                    Array.Copy(red_orig, img.redcmap, n_color);
                    Array.Copy(green_orig, img.greencmap, n_color);
                    Array.Copy(blue_orig, img.bluecmap, n_color);

                    if (planarconfig == PlanarConfig.CONTIG &&
                        img.samplesperpixel != 1 && img.bitspersample < 8)
                    {
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
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
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle contiguous data with {0}={1}, and {2}={3} and Bits/Sample={4}",
                            photoTag, img.photometric, "Samples/pixel", img.samplesperpixel, img.bitspersample);
                        return null;
                    }
                    break;

                case Photometric.YCBCR:
                    // It would probably be nice to have a reality check here.
                    if (planarconfig == PlanarConfig.CONTIG)
                    {
                        // can rely on LibJpeg.Net to convert to RGB
                        // XXX should restore current state on exit
                        switch (compress)
                        {
                            case Compression.JPEG:
                                // TODO: when complete tests verify complete desubsampling and
                                // YCbCr handling, remove use of JPEGCOLORMODE in favor of native
                                // handling
                                tif.SetField(TiffTag.JPEGCOLORMODE, JpegColorMode.RGB);
                                img.photometric = Photometric.RGB;
                                break;

                            default:
                                // do nothing
                                break;
                        }
                    }

                    // TODO: if at all meaningful and useful, make more complete support check
                    // here, or better still, refactor to let supporting code decide whether there
                    // is support and what meaningfull error to return
                    break;

                case Photometric.RGB:
                    if (colorchannels < 3)
                    {
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle RGB image with {0}={1}", "Color channels", colorchannels);
                        return null;
                    }
                    break;

                case Photometric.SEPARATED:
                    result = tif.GetFieldDefaulted(TiffTag.INKSET);
                    InkSet inkset = (InkSet)result[0].ToByte();

                    if (inkset != InkSet.CMYK)
                    {
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle separated image with {0}={1}", "InkSet", inkset);
                        return null;
                    }

                    if (img.samplesperpixel < 4)
                    {
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, can not handle separated image with {0}={1}", "Samples/pixel", img.samplesperpixel);
                        return null;
                    }
                    break;

                case Photometric.LOGL:
                    if (compress != Compression.SGILOG)
                    {
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
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
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
                            "Sorry, LogLuv data must have {0}={1} or {2}", "Compression", Compression.SGILOG, Compression.SGILOG24);
                        return null;
                    }

                    if (planarconfig != PlanarConfig.CONTIG)
                    {
                        errorMsg = string.Format(CultureInfo.InvariantCulture,
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
                    errorMsg = string.Format(CultureInfo.InvariantCulture,
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
                    errorMsg = "Sorry, can not handle image";
                    return null;
                }
            }
            else
            {
                if (!img.pickSeparateCase())
                {
                    errorMsg = "Sorry, can not handle image";
                    return null;
                }
            }

            return img;
        }

        /// <summary>
        /// Gets a value indicating whether image data has contiguous (packed) or separated samples.
        /// </summary>
        /// <value><c>true</c> if this image data has contiguous (packed) samples; otherwise,
        /// <c>false</c>.</value>
        public bool IsContig
        {
            get
            {
                return isContig;
            }
        }

        /// <summary>
        /// Gets the type of alpha data present.
        /// </summary>
        /// <value>The type of alpha data present.</value>
        public ExtraSample Alpha
        {
            get
            {
                return alpha;
            }
        }

        /// <summary>
        /// Gets the image width.
        /// </summary>
        /// <value>The image width.</value>
        public int Width
        {
            get
            {
                return width;
            }
        }

        /// <summary>
        /// Gets the image height.
        /// </summary>
        /// <value>The image height.</value>
        public int Height
        {
            get
            {
                return height;
            }
        }

        /// <summary>
        /// Gets the image bits per sample count.
        /// </summary>
        /// <value>The image bits per sample count.</value>
        public short BitsPerSample
        {
            get
            {
                return bitspersample;
            }
        }

        /// <summary>
        /// Gets the image samples per pixel count.
        /// </summary>
        /// <value>The image samples per pixel count.</value>
        public short SamplesPerPixel
        {
            get
            {
                return samplesperpixel;
            }
        }

        /// <summary>
        /// Gets the image orientation.
        /// </summary>
        /// <value>The image orientation.</value>
        public Orientation Orientation
        {
            get
            {
                return orientation;
            }
        }

        /// <summary>
        /// Gets or sets the requested orientation.
        /// </summary>
        /// <value>The requested orientation.</value>
        /// <remarks>The <see cref="GetRaster"/> method uses this value when placing converted
        /// image data into raster buffer.</remarks>
        public Orientation ReqOrientation
        {
            get
            {
                return req_orientation;
            }
            set
            {
                req_orientation = value;
            }
        }

        /// <summary>
        /// Gets the photometric interpretation of the image data.
        /// </summary>
        /// <value>The photometric interpretation of the image data.</value>
        public Photometric Photometric
        {
            get
            {
                return photometric;
            }
        }

        /// <summary>
        /// Gets or sets the "get" method (the method that is called to produce RGBA raster).
        /// </summary>
        /// <value>The "get" method.</value>
        public GetDelegate Get
        {
            get
            {
                return get;
            }
            set
            {
                get = value;
            }
        }

        /// <summary>
        /// Gets or sets the "put" method (the method that is called to pack pixel data in the
        /// raster) used when converting contiguously packed samples.
        /// </summary>
        /// <value>The "put" method used when converting contiguously packed samples.</value>
        public PutContigDelegate PutContig
        {
            get
            {
                return putContig;
            }
            set
            {
                putContig = value;
            }
        }

        /// <summary>
        /// Gets or sets the "put" method (the method that is called to pack pixel data in the
        /// raster) used when converting separated samples.
        /// </summary>
        /// <value>The "put" method used when converting separated samples.</value>
        public PutSeparateDelegate PutSeparate
        {
            get
            {
                return putSeparate;
            }
            set
            {
                putSeparate = value;
            }
        }

        /// <summary>
        /// Reads the underlaying TIFF image and decodes it into RGBA format raster.
        /// </summary>
        /// <param name="raster">The raster (the buffer to place decoded image data to).</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="raster"/> at which
        /// to begin storing decoded bytes.</param>
        /// <param name="width">The raster width.</param>
        /// <param name="height">The raster height.</param>
        /// <returns><c>true</c> if the image was successfully read and decoded; otherwise,
        /// <c>false</c>.</returns>
        /// <remarks><para>
        /// <b>GetRaster</b> reads image into memory using current "get" (<see cref="Get"/>) method,
        /// storing the result in the user supplied RGBA <paramref name="raster"/> using one of
        /// the "put" (<see cref="PutContig"/> or <see cref="PutSeparate"/>) methods. The raster
        /// is assumed to be an array of <paramref name="width"/> times <paramref name="height"/>
        /// 32-bit entries, where <paramref name="width"/> must be less than or equal to the width
        /// of the image (<paramref name="height"/> may be any non-zero size). If the raster
        /// dimensions are smaller than the image, the image data is cropped to the raster bounds.
        /// If the raster height is greater than that of the image, then the image data placement
        /// depends on the value of <see cref="ReqOrientation"/> property. Note that the raster is
        /// assumed to be organized such that the pixel at location (x, y) is
        /// <paramref name="raster"/>[y * width + x]; with the raster origin specified by the
        /// value of <see cref="ReqOrientation"/> property.
        /// </para><para>
        /// Raster pixels are 8-bit packed red, green, blue, alpha samples. The 
        /// <see cref="Tiff.GetR"/>, <see cref="Tiff.GetG"/>, <see cref="Tiff.GetB"/>, and
        /// <see cref="Tiff.GetA"/> should be used to access individual samples. Images without
        /// Associated Alpha matting information have a constant Alpha of 1.0 (255).
        /// </para><para>
        /// <b>GetRaster</b> converts non-8-bit images by scaling sample values. Palette,
        /// grayscale, bilevel, CMYK, and YCbCr images are converted to RGB transparently.
        /// Raster pixels are returned uncorrected by any colorimetry information present in
        /// the directory.
        /// </para><para>
        /// Samples must be either 1, 2, 4, 8, or 16 bits. Colorimetric samples/pixel must be
        /// either 1, 3, or 4 (i.e. SamplesPerPixel minus ExtraSamples).
        /// </para><para>
        /// Palette image colormaps that appear to be incorrectly written as 8-bit values are
        /// automatically scaled to 16-bits.
        /// </para><para>
        /// All error messages are directed to the current error handler.
        /// </para></remarks>
        public bool GetRaster(int[] raster, int offset, int width, int height)
        {
            if (get == null)
            {
                Tiff.ErrorExt(tif, tif.m_clientdata, tif.FileName(), "No \"get\" method setup");
                return false;
            }

            return get(this, raster, offset, width, height);
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

        /// <summary>
        /// Palette images with &lt;= 8 bits/sample are handled with a table to avoid lots of shifts
        /// and masks. The table is setup so that put*cmaptile (below) can retrieve 8 / bitspersample
        /// pixel values simply by indexing into the table with one number.
        /// </summary>
        private void CMAP(int x, int i, ref int j)
        {
            PALmap[i][j++] = PACK(redcmap[x] & 0xff, greencmap[x] & 0xff, bluecmap[x] & 0xff);
        }

        /// <summary>
        /// Greyscale images with less than 8 bits/sample are handled with a table to avoid lots
        /// of shifts and masks. The table is setup so that put*bwtile (below) can retrieve
        /// 8 / bitspersample pixel values simply by indexing into the table with one number.
        /// </summary>
        private void GREY(int x, int i, ref int j)
        {
            int c = Map[x];
            BWmap[i][j++] = PACK(c, c, c);
        }

        /// <summary>
        /// Get an tile-organized image that has
        /// PlanarConfiguration contiguous if SamplesPerPixel > 1
        ///  or
        /// SamplesPerPixel == 1
        /// </summary>
        private static bool gtTileContig(TiffRgbaImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            PutContigDelegate put = img.putContig;

            byte[] buf = new byte[tif.TileSize()];

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
                    if (tif.ReadTile(buf, 0, col + img.col_offset, row + img.row_offset, 0, 0) < 0 && img.stoponerr)
                    {
                        ret = false;
                        break;
                    }

                    int pos = ((row + img.row_offset) % th) * tif.TileRowSize();

                    if (col + tw > w)
                    {
                        // Tile is clipped horizontally. Calculate visible portion and
                        // skewing factors.
                        int npix = w - col;
                        int fromskew = tw - npix;
                        put(img, raster, offset + y * w + col, col, y, npix, nrow, fromskew, toskew + fromskew, buf, pos);
                    }
                    else
                    {
                        put(img, raster, offset + y * w + col, col, y, tw, nrow, 0, toskew, buf, pos);
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
        /// Get an tile-organized image that has
        /// SamplesPerPixel > 1
        /// PlanarConfiguration separated
        /// We assume that all such images are RGB.
        /// </summary>
        private static bool gtTileSeparate(TiffRgbaImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            PutSeparateDelegate put = img.putSeparate;

            int tilesize = tif.TileSize();
            byte[] buf = new byte[(img.alpha != 0 ? 4 : 3) * tilesize];

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
                        // Tile is clipped horizontally.
                        // Calculate visible portion and skewing factors.
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
        private static bool gtStripContig(TiffRgbaImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            PutContigDelegate put = img.putContig;

            byte[] buf = new byte[tif.StripSize()];

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

        /// <summary>
        /// Get a strip-organized image with
        ///  SamplesPerPixel > 1
        ///  PlanarConfiguration separated
        /// We assume that all such images are RGB.
        /// </summary>
        private static bool gtStripSeparate(TiffRgbaImage img, int[] raster, int offset, int w, int h)
        {
            Tiff tif = img.tif;
            PutSeparateDelegate put = img.putSeparate;

            int stripsize = tif.StripSize();
            byte[] buf = new byte[(img.alpha != 0 ? 4 : 3) * stripsize];

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

        /// <summary>
        /// Select the appropriate conversion routine for packed data.
        /// </summary>
        private bool pickContigCase()
        {
            get = tif.IsTiled() ? new GetDelegate(gtTileContig) : new GetDelegate(gtStripContig);
            putContig = null;

            switch (photometric)
            {
                case Photometric.RGB:
                    switch (bitspersample)
                    {
                        case 8:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                putContig = putRGBAAcontig8bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                putContig = putRGBUAcontig8bittile;
                            else
                                putContig = putRGBcontig8bittile;
                            break;

                        case 16:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                putContig = putRGBAAcontig16bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                putContig = putRGBUAcontig16bittile;
                            else
                                putContig = putRGBcontig16bittile;
                            break;
                    }
                    break;

                case Photometric.SEPARATED:
                    if (buildMap())
                    {
                        if (bitspersample == 8)
                        {
                            if (Map == null)
                                putContig = putRGBcontig8bitCMYKtile;
                            else
                                putContig = putRGBcontig8bitCMYKMaptile;
                        }
                    }
                    break;

                case Photometric.PALETTE:
                    if (buildMap())
                    {
                        switch (bitspersample)
                        {
                            case 8:
                                putContig = put8bitcmaptile;
                                break;
                            case 4:
                                putContig = put4bitcmaptile;
                                break;
                            case 2:
                                putContig = put2bitcmaptile;
                                break;
                            case 1:
                                putContig = put1bitcmaptile;
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
                                putContig = put16bitbwtile;
                                break;
                            case 8:
                                putContig = putgreytile;
                                break;
                            case 4:
                                putContig = put4bitbwtile;
                                break;
                            case 2:
                                putContig = put2bitbwtile;
                                break;
                            case 1:
                                putContig = put1bitbwtile;
                                break;
                        }
                    }
                    break;

                case Photometric.YCBCR:
                    if (bitspersample == 8)
                    {
                        if (initYCbCrConversion())
                        {
                            // The 6.0 spec says that subsampling must be one of 1, 2, or 4, and
                            // that vertical subsampling must always be <= horizontal subsampling;
                            // so there are only a few possibilities and we just enumerate the cases.
                            // Joris: added support for the [1, 2] case, nonetheless, to accommodate
                            // some OJPEG files
                            FieldValue[] result = tif.GetFieldDefaulted(TiffTag.YCBCRSUBSAMPLING);
                            short SubsamplingHor = result[0].ToShort();
                            short SubsamplingVer = result[1].ToShort();

                            switch (((ushort)SubsamplingHor << 4) | (ushort)SubsamplingVer)
                            {
                                case 0x44:
                                    putContig = putcontig8bitYCbCr44tile;
                                    break;
                                case 0x42:
                                    putContig = putcontig8bitYCbCr42tile;
                                    break;
                                case 0x41:
                                    putContig = putcontig8bitYCbCr41tile;
                                    break;
                                case 0x22:
                                    putContig = putcontig8bitYCbCr22tile;
                                    break;
                                case 0x21:
                                    putContig = putcontig8bitYCbCr21tile;
                                    break;
                                case 0x12:
                                    putContig = putcontig8bitYCbCr12tile;
                                    break;
                                case 0x11:
                                    putContig = putcontig8bitYCbCr11tile;
                                    break;
                            }
                        }
                    }
                    break;

                case Photometric.CIELAB:
                    if (buildMap())
                    {
                        if (bitspersample == 8)
                            putContig = initCIELabConversion();
                    }
                    break;
            }

            return (putContig != null);
        }

        /// <summary>
        /// Select the appropriate conversion routine for unpacked data.
        /// NB: we assume that unpacked single channel data is directed to the "packed routines.
        /// </summary>
        private bool pickSeparateCase()
        {
            get = tif.IsTiled() ? new GetDelegate(gtTileSeparate) : new GetDelegate(gtStripSeparate);
            putSeparate = null;

            switch (photometric)
            {
                case Photometric.RGB:
                    switch (bitspersample)
                    {
                        case 8:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                putSeparate = putRGBAAseparate8bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                putSeparate = putRGBUAseparate8bittile;
                            else
                                putSeparate = putRGBseparate8bittile;
                            break;

                        case 16:
                            if (alpha == ExtraSample.ASSOCALPHA)
                                putSeparate = putRGBAAseparate16bittile;
                            else if (alpha == ExtraSample.UNASSALPHA)
                                putSeparate = putRGBUAseparate16bittile;
                            else
                                putSeparate = putRGBseparate16bittile;
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
                                    putSeparate = putseparate8bitYCbCr11tile;
                                    break;
                                // TODO: add other cases here
                            }
                        }
                    }
                    break;
            }

            return (putSeparate != null);
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

        private PutContigDelegate initCIELabConversion()
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

        /// <summary>
        /// Construct any mapping table used by the associated put method.
        /// </summary>
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
                    // Convert 16-bit colormap to 8-bit
                    // (unless it looks like an old-style 8-bit colormap).
                    if (checkcmap() == 16)
                        cvtcmap();
                    else
                        Tiff.WarningExt(tif, tif.m_clientdata, tif.FileName(), "Assuming 8-bit colormap");

                    // Use mapping table and colormap to construct unpacking
                    // tables for samples < 8 bits.
                    if (bitspersample <= 8 && !makecmap())
                        return false;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Construct a mapping table to convert from the range of the data samples to [0, 255] -
        /// for display. This process also handles inverting B&amp;W images when needed.
        /// </summary>
        private bool setupMap()
        {
            int range = (1 << bitspersample) - 1;

            // treat 16 bit the same as eight bit
            if (bitspersample == 16)
                range = 255;

            Map = new byte[range + 1];

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
                // Use photometric mapping table to construct unpacking tables for samples <= 8 bits.
                if (!makebwmap())
                    return false;

                // no longer need Map
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
                PALmap[i] = new int[nsamples];

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
                BWmap[i] = new int[nsamples];

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

        /// <summary>
        /// YCbCr -> RGB conversion and packing routines.
        /// </summary>
        private void YCbCrtoRGB(out int dst, int Y, int Cb, int Cr)
        {
            int r, g, b;
            ycbcr.YCbCrtoRGB(Y, Cb, Cr, out r, out g, out b);
            dst = PACK(r, g, b);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////
        // The following routines move decoded data returned from the TIFF library into rasters
        // filled with packed ABGR pixels
        //
        // The routines have been created according to the most important cases and optimized.
        // pickTileContigCase and pickTileSeparateCase analyze the parameters and select the
        // appropriate "put" routine to use.

        /// <summary>
        /// 8-bit palette => colormap/RGB
        /// </summary>
        private static void put8bitcmaptile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put4bitcmaptile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put2bitcmaptile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put1bitcmaptile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int[][] PALmap = img.PALmap;
            fromskew /= 8;

            int cpPos = cpOffset;
            int ppPos = ppOffset;

            while (h-- > 0)
            {
                int[] bw;
                int bwPos = 0;

                int _x;
                for (_x = w; _x >= 8; _x -= 8)
                {
                    bw = PALmap[pp[ppPos++]];
                    bwPos = 0;

                    for (int i = 0; i < 8; i++)
                        cp[cpPos++] = bw[bwPos++];
                }

                if (_x > 0)
                {
                    bw = PALmap[pp[ppPos++]];
                    bwPos = 0;

                    if (_x <= 7 && _x > 0)
                    {
                        for (int i = 0; i < _x; i++)
                            cp[cpPos++] = bw[bwPos++];
                    }
                }

                cpPos += toskew;
                ppPos += fromskew;
            }
        }

        /// <summary>
        /// 8-bit greyscale => colormap/RGB
        /// </summary>
        private static void putgreytile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put16bitbwtile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put1bitbwtile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put2bitbwtile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
        private static void put4bitbwtile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed samples, no Map => RGB
        /// </summary>
        private static void putRGBcontig8bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed samples => RGBA w/ associated alpha (known to have Map == null)
        /// </summary>
        private static void putRGBAAcontig8bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed samples => RGBA w/ unassociated alpha (known to have Map == null)
        /// </summary>
        private static void putRGBUAcontig8bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 16-bit packed samples => RGB
        /// </summary>
        private static void putRGBcontig16bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;
            fromskew *= samplesperpixel;

            short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length);
            int wpPos = 0;

            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    cp[cpPos] = PACKW(wp[wpPos], wp[wpPos + 1], wp[wpPos + 2]);
                    cpPos++;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                wpPos += fromskew;
            }
        }

        /// <summary>
        /// 16-bit packed samples => RGBA w/ associated alpha (known to have Map == null)
        /// </summary>
        private static void putRGBAAcontig16bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length);
            int wpPos = 0;

            fromskew *= samplesperpixel;
            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
                {
                    cp[cpPos] = PACKW4(wp[wpPos], wp[wpPos + 1], wp[wpPos + 2], wp[wpPos + 3]);
                    cpPos++;
                    wpPos += samplesperpixel;
                }

                cpPos += toskew;
                wpPos += fromskew;
            }
        }

        /// <summary>
        /// 16-bit packed samples => RGBA w/ unassociated alpha (known to have Map == null)
        /// </summary>
        private static void putRGBUAcontig16bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int samplesperpixel = img.samplesperpixel;
            fromskew *= samplesperpixel;

            int cpPos = cpOffset;
            int ppPos = ppOffset;

            short[] wp = Tiff.ByteArrayToShorts(pp, ppPos, pp.Length);
            int wpPos = 0;

            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
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

        /// <summary>
        /// 8-bit packed CMYK samples w/o Map => RGB.
        /// NB: The conversion of CMYK->RGB is *very* crude.
        /// </summary>
        private static void putRGBcontig8bitCMYKtile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed CMYK samples w/Map => RGB
        /// NB: The conversion of CMYK->RGB is *very* crude.
        /// </summary>
        private static void putRGBcontig8bitCMYKMaptile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit unpacked samples => RGB
        /// </summary>
        private static void putRGBseparate8bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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

        /// <summary>
        /// 8-bit unpacked samples => RGBA w/ associated alpha
        /// </summary>
        private static void putRGBAAseparate8bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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

        /// <summary>
        /// 8-bit unpacked samples => RGBA w/ unassociated alpha
        /// </summary>
        private static void putRGBUAseparate8bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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

        /// <summary>
        /// 16-bit unpacked samples => RGB
        /// </summary>
        private static void putRGBseparate16bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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

        /// <summary>
        /// 16-bit unpacked samples => RGBA w/ associated alpha
        /// </summary>
        private static void putRGBAAseparate16bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
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

        /// <summary>
        /// 16-bit unpacked samples => RGBA w/ unassociated alpha
        /// </summary>
        private static void putRGBUAseparate16bittile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            short[] wrgba = Tiff.ByteArrayToShorts(rgba, 0, rgba.Length);

            int wrPos = rOffset / sizeof(short);
            int wgPos = gOffset / sizeof(short);
            int wbPos = bOffset / sizeof(short);
            int waPos = aOffset / sizeof(short);
            int cpPos = cpOffset;

            while (h-- > 0)
            {
                for (x = w; x-- > 0; )
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ no subsampling => RGB
        /// </summary>
        private static void putseparate8bitYCbCr11tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] rgba, int rOffset, int gOffset, int bOffset, int aOffset)
        {
            // TODO: naming of input vars is still off, change obfuscating declaration inside define, or resolve obfuscation
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

        /// <summary>
        /// 8-bit packed CIE L*a*b 1976 samples => RGB
        /// </summary>
        private static void putcontig8bitCIELab(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ 4,4 subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr44tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            int cp1 = cpPos + w + toskew;
            int cp2 = cp1 + w + toskew;
            int cp3 = cp2 + w + toskew;
            int incr = 3 * w + 4 * toskew;

            // adjust fromskew
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ 4,2 subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr42tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ 4,1 subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr41tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            // XXX adjust fromskew
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ 2,2 subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr22tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ 2,1 subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr21tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ no subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr11tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
        {
            int cpPos = cpOffset;
            int ppPos = ppOffset;

            fromskew *= 3;
            do
            {
                x = w; // was x = w >> 1; patched 2000/09/25 warmerda@home.com
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

        /// <summary>
        /// 8-bit packed YCbCr samples w/ 1,2 subsampling => RGB
        /// </summary>
        private static void putcontig8bitYCbCr12tile(TiffRgbaImage img, int[] cp, int cpOffset, int x, int y, int w, int h, int fromskew, int toskew, byte[] pp, int ppOffset)
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
    }
}
