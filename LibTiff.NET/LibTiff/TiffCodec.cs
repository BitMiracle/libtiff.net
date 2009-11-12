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

namespace BitMiracle.LibTiff
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
        protected Tiff m_tif;
        public Compression m_scheme;
        public string m_name;

        public TiffCodec(Tiff tif, Compression scheme, string name)
        {
            m_scheme = scheme;
            m_tif = tif;

            m_name = name.Clone() as string;
        }

        public virtual bool CanEncode()
        {
            return false;
        }

        public virtual bool CanDecode()
        {
            return false;
        }

        public virtual bool Init()
        {
            return true;
        }

        // decode part

        /* called once before predecode */
        public virtual bool tif_setupdecode()
        {
            return true;
        }

        /* pre-row/strip/tile decoding */
        public virtual bool tif_predecode(short s)
        {
            return true;
        }

        /* scanline decoding routine */
        public virtual bool tif_decoderow(byte[] pp, int cc, short s)
        {
            return noDecode("scanline");
        }

        /* strip decoding routine */
        public virtual bool tif_decodestrip(byte[] pp, int cc, short s)
        {
            return noDecode("strip");
        }

        /* tile decoding routine */
        public virtual bool tif_decodetile(byte[] pp, int cc, short s)
        {
            return noDecode("tile");
        }

        // encode part

        /* called once before preencode */
        public virtual bool tif_setupencode()
        {
            return true;
        }

        /* pre-row/strip/tile encoding */
        public virtual bool tif_preencode(short s)
        {
            return true;
        }

        /* post-row/strip/tile encoding */
        public virtual bool tif_postencode()
        {
            return true;
        }

        /* scanline encoding routine */
        public virtual bool tif_encoderow(byte[] pp, int cc, short s)
        {
            return noEncode("scanline");
        }

        /* strip encoding routine */
        public virtual bool tif_encodestrip(byte[] pp, int cc, short s)
        {
            return noEncode("strip");
        }

        /* tile encoding routine */
        public virtual bool tif_encodetile(byte[] pp, int cc, short s)
        {
            return noEncode("tile");
        }

        /* cleanup-on-close routine */
        public virtual void tif_close()
        {
        }

        /* position within a strip routine
         * Seek forwards nrows in the current strip.
         */
        public virtual bool tif_seek(int off)
        {
            Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "Compression algorithm does not support random access");
            return false;
        }

        /* cleanup state routine */
        public virtual void tif_cleanup()
        {
        }

        /* calculate/constrain strip size */
        public virtual int tif_defstripsize(int s)
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

        /* calculate/constrain tile size */
        public virtual void tif_deftilesize(ref int tw, ref int th)
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
