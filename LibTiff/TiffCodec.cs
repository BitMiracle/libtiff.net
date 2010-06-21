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

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// A CODEC is a software package that implements decoding,
    /// encoding, or decoding+encoding of a compression algorithm.
    /// The library provides a collection of builtin codecs.
    /// More codecs may be registered through calls to the library
    /// and/or the builtin implementations may be overridden.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffCodec
    {
        /// <summary>
        /// An instance of <see cref="Tiff"/>.
        /// </summary>
        protected Tiff m_tif;

        /// <summary>
        /// Compression scheme.
        /// </summary>
        protected internal Compression m_scheme;

        /// <summary>
        /// Codec name.
        /// </summary>
        protected internal string m_name;

        /// <summary>
        /// Initializes a new instance of the <see cref="TiffCodec"/> class.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="scheme">The scheme.</param>
        /// <param name="name">The name.</param>
        public TiffCodec(Tiff tif, Compression scheme, string name)
        {
            m_scheme = scheme;
            m_tif = tif;

            m_name = name;
        }

        /// <summary>
        /// Determines whether this instance can encode.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance can encode; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanEncode()
        {
            return false;
        }

        /// <summary>
        /// Determines whether this instance can decode.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance can decode; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanDecode()
        {
            return false;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <returns><c>true</c> if initialized successfully</returns>
        public virtual bool Init()
        {
            return true;
        }

        // decode part

        /// <summary>
        /// Setups the decode.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Called once before <see cref="TiffCodec.PreDecode(System.Int16)"/>.</remarks>
        public virtual bool SetupDecode()
        {
            return true;
        }

        /// <summary>
        /// Called before decoding.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public virtual bool PreDecode(short s)
        {
            return true;
        }

        /// <summary>
        /// Decodes the row.
        /// </summary>
        /// <param name="pp">The pp.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if decoded successfully.</returns>
        public virtual bool DecodeRow(byte[] pp, int cc, short s)
        {
            return noDecode("scanline");
        }

        /// <summary>
        /// Decodes the strip.
        /// </summary>
        /// <param name="pp">The pp.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if decoded successfully.</returns>
        public virtual bool DecodeStrip(byte[] pp, int cc, short s)
        {
            return noDecode("strip");
        }

        /// <summary>
        /// Decodes the tile.
        /// </summary>
        /// <param name="pp">The pp.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if decoded successfully.</returns>
        public virtual bool DecodeTile(byte[] pp, int cc, short s)
        {
            return noDecode("tile");
        }

        // encode part

        /// <summary>
        /// Setups the encode.
        /// </summary>
        /// <returns><c>true</c> if setup successfully.</returns>
        /// <remarks>Called once before <see cref="TiffCodec.PreEncode(System.Int16)"/>.</remarks>
        public virtual bool SetupEncode()
        {
            return true;
        }

        /// <summary>
        /// Called before decoding.
        /// </summary>
        /// <param name="s">s</param>
        /// <returns><c>true</c> if succeed.</returns>
        public virtual bool PreEncode(short s)
        {
            return true;
        }

        /// <summary>
        /// Called after encoding.
        /// </summary>
        /// <returns><c>true</c> if succeed.</returns>
        public virtual bool PostEncode()
        {
            return true;
        }

        /// <summary>
        /// Encodes the row.
        /// </summary>
        /// <param name="pp">The pp.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if encoded successfully.</returns>
        public virtual bool EncodeRow(byte[] pp, int cc, short s)
        {
            return noEncode("scanline");
        }

        /// <summary>
        /// Encodes the strip.
        /// </summary>
        /// <param name="pp">The pp.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if encoded successfully.</returns>
        public virtual bool EncodeStrip(byte[] pp, int cc, short s)
        {
            return noEncode("strip");
        }

        /// <summary>
        /// Encodes the tile.
        /// </summary>
        /// <param name="pp">The pp.</param>
        /// <param name="cc">The cc.</param>
        /// <param name="s">The s.</param>
        /// <returns><c>true</c> if encoded successfully.</returns>
        public virtual bool EncodeTile(byte[] pp, int cc, short s)
        {
            return noEncode("tile");
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public virtual void Close()
        {
        }

        /// <summary>
        /// Seeks the specified offset in the current strip.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><c>true</c> if succeed.</returns>
        public virtual bool Seek(int offset)
        {
            Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                "Compression algorithm does not support random access");
            return false;
        }

        /// <summary>
        /// Cleanups state of this instance.
        /// </summary>
        public virtual void Cleanup()
        {
        }

        /// <summary>
        /// Calculates/constrains strip size
        /// </summary>
        /// <param name="s">s</param>
        /// <returns>Strip size</returns>
        public virtual int DefStripSize(int s)
        {
            if (s < 1)
            {
                /*
                * If RowsPerStrip is unspecified, try to break the
                * image up into strips that are approximately
                * STRIP_SIZE_DEFAULT bytes long.
                */
                int scanline = m_tif.ScanlineSize();
                s = Tiff.STRIP_SIZE_DEFAULT / (scanline == 0 ? 1 : scanline);
                if (s == 0)
                {
                    /* very wide images */
                    s = 1;
                }
            }

            return s;
        }

        /// <summary>
        /// Calculate/constrain tile size
        /// </summary>
        /// <param name="tw">Output tile width.</param>
        /// <param name="th">Output tile height.</param>
        public virtual void DefTileSize(ref int tw, ref int th)
        {
            if (tw < 1)
                tw = 256;
            
            if (th < 1)
                th = 256;
            
            /* roundup to a multiple of 16 per the spec */
            if ((tw & 0xf) != 0)
                tw = Tiff.roundUp(tw, 16);

            if ((th & 0xf) != 0)
                th = Tiff.roundUp(th, 16);
        }

        private bool noEncode(string method)
        {
            TiffCodec c = m_tif.FindCodec(m_tif.m_dir.td_compression);
            if (c != null)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                    "{0} {1} encoding is not implemented", c.m_name, method);
            }
            else
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                    "Compression scheme {0} {1} encoding is not implemented",
                    m_tif.m_dir.td_compression, method);
            }

            return false;
        }

        private bool noDecode(string method)
        {
            TiffCodec c = m_tif.FindCodec(m_tif.m_dir.td_compression);
            if (c != null)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                    "{0} {1} decoding is not implemented", c.m_name, method);
            }
            else
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name,
                    "Compression scheme {0} {1} decoding is not implemented",
                    m_tif.m_dir.td_compression, method);
            }

            return false;
        }
    }
}
