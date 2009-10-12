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
    public class TiffCodec
    {
        protected Tiff m_tif;
        public int m_scheme;
        public string m_name;

        public TiffCodec(Tiff tif, int scheme, string name)
        {
            m_scheme = scheme;
            m_tif = tif;

            m_name = new char[name.Length + 1];
            strcpy(m_name, name);

            Init();
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
        public virtual bool tif_predecode(UInt16 s)
        {
            return true;
        }

        /* scanline decoding routine */
        public virtual bool tif_decoderow(byte[] pp, int cc, UInt16 s)
        {
            return noDecode("scanline");
        }

        /* strip decoding routine */
        public virtual bool tif_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            return noDecode("strip");
        }

        /* tile decoding routine */
        public virtual bool tif_decodetile(byte[] pp, int cc, UInt16 s)
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
        public virtual bool tif_preencode(UInt16 s)
        {
            return true;
        }

        /* post-row/strip/tile encoding */
        public virtual bool tif_postencode()
        {
            return true;
        }

        /* scanline encoding routine */
        public virtual bool tif_encoderow(byte[] pp, int cc, UInt16 s)
        {
            return noEncode("scanline");
        }

        /* strip encoding routine */
        public virtual bool tif_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            return noEncode("strip");
        }

        /* tile encoding routine */
        public virtual bool tif_encodetile(byte[] pp, int cc, UInt16 s)
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
        public virtual bool tif_seek(uint off)
        {
            ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "Compression algorithm does not support random access");
            return false;
        }

        /* cleanup state routine */
        public virtual void tif_cleanup()
        {
        }

        /* calculate/constrain strip size */
        public virtual uint tif_defstripsize(uint s)
        {
            if ((int)s < 1)
            {
                /*
                * If RowsPerStrip is unspecified, try to break the
                * image up into strips that are approximately
                * STRIP_SIZE_DEFAULT bytes long.
                */
                int scanline = m_tif.ScanlineSize();
                s = (uint)STRIP_SIZE_DEFAULT / (scanline == 0 ? 1 : scanline);
                if (s == 0)
                {
                    /* very wide images */
                    s = 1;
                }
            }

            return s;
        }

        /* calculate/constrain tile size */
        public virtual void tif_deftilesize(ref uint tw, ref uint th)
        {
            if ((int)tw < 1)
                tw = 256;
            
            if ((int)th < 1)
                th = 256;
            
            /* roundup to a multiple of 16 per the spec */
            if ((tw & 0xf) != 0)
                tw = roundUp(tw, 16);

            if ((th & 0xf) != 0)
                th = roundUp(th, 16);
        }

        private bool noEncode(string method)
        {
            const TiffCodec* c = m_tif.FindCodec(m_tif.m_dir.td_compression);
            if (c != null)
                ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "%s %s encoding is not implemented", c.m_name, method);
            else
                ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "Compression scheme %u %s encoding is not implemented", m_tif.m_dir.td_compression, method);

            return false;
        }

        private bool noDecode(string method)
        {
            const TiffCodec* c = m_tif.FindCodec(m_tif.m_dir.td_compression);

            if (c != null)
                ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "%s %s decoding is not implemented", c.m_name, method);
            else
                ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "Compression scheme %u %s decoding is not implemented", m_tif.m_dir.td_compression, method);

            return false;
        }
    }
}
