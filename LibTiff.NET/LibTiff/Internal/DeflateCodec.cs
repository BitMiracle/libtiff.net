/*
 * TIFF Library.
 *
 * ZIP (aka Deflate) Compression Support
 *
 * This file is simply an interface to the zlib library written by
 * Jean-loup Gailly and Mark Adler.  You must use version 1.0 or later
 * of the library: this code assumes the 1.0 API and also depends on
 * the ability to write the zlib header multiple times (one per strip)
 * which was not possible with versions prior to 0.95.  Note also that
 * older versions of this codec avoided this bug by supressing the header
 * entirely.  This means that files written with the old library cannot
 * be read; they should be converted to a different compression scheme
 * and then reconverted.
 *
 * The data format used by the zlib library is described in the files
 * zlib-3.1.doc, deflate-1.1.doc and gzip-4.1.doc, available in the
 * directory ftp://ftp.uu.net/pub/archiving/zip/doc.  The library was
 * last found at ftp://ftp.uu.net/pub/archiving/zip/zlib/zlib-0.99.tar.gz.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    class DeflateCodec : CodecWithPredictor
    {
        public const int ZSTATE_INIT_DECODE = 0x01;
        public const int ZSTATE_INIT_ENCODE = 0x02;

        //public z_stream m_stream;
        public int m_zipquality; /* compression level */
        public int m_state; /* state flags */

        private static TiffFieldInfo[] zipFieldInfo = 
        {
            TiffFieldInfo(TIFFTAG_ZIPQUALITY, 0, 0, TIFF_ANY, FIELD_PSEUDO, true, false, ""), 
        };

        private TiffTagMethods m_tagMethods;

        public DeflateCodec(Tiff tif, COMPRESSION scheme, string name)
            : base(tif, scheme, name)
        {
            m_tagMethods = new DeflateCodecTagMethods();
        }

        public override bool Init()
        {
            assert((m_scheme == COMPRESSION_DEFLATE) || (m_scheme == COMPRESSION_ADOBE_DEFLATE));

            /*
            * Merge codec-specific tag information and
            * override parent get/set field methods.
            */
            m_tif.MergeFieldInfo(zipFieldInfo, sizeof(zipFieldInfo) / sizeof(zipFieldInfo[0]));

            /*
             * Allocate state block so tag methods have storage to record values.
             */
            m_stream.zalloc = NULL;
            m_stream.zfree = NULL;
            m_stream.opaque = NULL;
            m_stream.data_type = Z_BINARY;

            /* Default values for codec-specific fields */
            m_zipquality = Z_DEFAULT_COMPRESSION; /* default comp. level */
            m_state = 0;

            /*
             * Setup predictor setup.
             */
            TIFFPredictorInit(m_tagMethods);
            return true;
        }

        public override bool CanEncode()
        {
            return true;
        }

        public override bool CanDecode()
        {
            return true;
        }

        public override bool tif_predecode(UInt16 s)
        {
            return ZIPPreDecode(s);
        }

        public override bool tif_preencode(UInt16 s)
        {
            return ZIPPreEncode(s);
        }

        public override bool tif_postencode()
        {
            return ZIPPostEncode();
        }

        public override void tif_cleanup()
        {
            return ZIPCleanup();
        }

        // CodecWithPredictor overrides

        public override bool predictor_setupdecode()
        {
            return ZIPSetupDecode();
        }

        public override bool predictor_decoderow(byte[] pp, int cc, UInt16 s)
        {
            return ZIPDecode(pp, cc, s);
        }

        public override bool predictor_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            return ZIPDecode(pp, cc, s);
        }

        public override bool predictor_decodetile(byte[] pp, int cc, UInt16 s)
        {
            return ZIPDecode(pp, cc, s);
        }

        public override bool predictor_setupencode()
        {
            return ZIPSetupEncode();
        }

        public override bool predictor_encoderow(byte[] pp, int cc, UInt16 s)
        {
            return ZIPEncode(pp, cc, s);
        }

        public override bool predictor_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            return ZIPEncode(pp, cc, s);
        }

        public override bool predictor_encodetile(byte[] pp, int cc, UInt16 s)
        {
            return ZIPEncode(pp, cc, s);
        }

        private void ZIPCleanup()
        {
            CodecWithPredictor::TIFFPredictorCleanup();

            if ((m_state & ZSTATE_INIT_ENCODE) != 0)
            {
                deflateEnd(&m_stream);
                m_state = 0;
            }
            else if ((m_state & ZSTATE_INIT_DECODE) != 0)
            {
                inflateEnd(&m_stream);
                m_state = 0;
            }
        }

        private bool ZIPDecode(byte[] op, int occ, UInt16 s)
        {
            static const char module[] = "ZIPDecode";

            s;
            assert(m_state == ZSTATE_INIT_DECODE);
            m_stream.next_out = op;
            m_stream.avail_out = occ;
            do
            {
                int state = inflate(&m_stream, Z_PARTIAL_FLUSH);
                if (state == Z_STREAM_END)
                    break;

                if (state == Z_DATA_ERROR)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: Decoding error at scanline %d, %s", m_tif.m_name, m_tif.m_row, m_stream.msg);
                    if (inflateSync(&m_stream) != Z_OK)
                        return false;
                    
                    continue;
                }

                if (state != Z_OK)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: zlib error: %s", m_tif.m_name, m_stream.msg);
                    return false;
                }
            }
            while (m_stream.avail_out > 0);

            if (m_stream.avail_out != 0)
            {
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: Not enough data at scanline %d (short %d bytes)", m_tif.m_name, m_tif.m_row, m_stream.avail_out);
                return false;
            }

            return true;
        }

        /*
        * Encode a chunk of pixels.
        */
        private bool ZIPEncode(byte[] bp, int cc, UInt16 s)
        {
            static const char module[] = "ZIPEncode";

            s;

            assert(m_state == ZSTATE_INIT_ENCODE);

            m_stream.next_in = bp;
            m_stream.avail_in = cc;
            do
            {
                if (deflate(&m_stream, Z_NO_FLUSH) != Z_OK)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: Encoder error: %s", m_tif.m_name, m_stream.msg);
                    return false;
                }

                if (m_stream.avail_out == 0)
                {
                    m_tif.m_rawcc = m_tif.m_rawdatasize;
                    m_tif.flushData1();
                    m_stream.next_out = m_tif.m_rawdata;
                    m_stream.avail_out = m_tif.m_rawdatasize;
                }
            }
            while (m_stream.avail_in > 0);

            return true;
        }

        /*
        * Finish off an encoded strip by flushing the last
        * string and tacking on an End Of Information code.
        */
        private bool ZIPPostEncode()
        {
            static const char module[] = "ZIPPostEncode";
            int state;

            m_stream.avail_in = 0;
            do
            {
                state = deflate(&m_stream, Z_FINISH);
                switch (state)
                {
                    case Z_STREAM_END:
                    case Z_OK:
                        if ((int)m_stream.avail_out != (int)m_tif.m_rawdatasize)
                        {
                            m_tif.m_rawcc = m_tif.m_rawdatasize - m_stream.avail_out;
                            m_tif.flushData1();
                            m_stream.next_out = m_tif.m_rawdata;
                            m_stream.avail_out = m_tif.m_rawdatasize;
                        }
                        break;
                    default:
                        Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: zlib error: %s", m_tif.m_name, m_stream.msg);
                        return false;
                }
            }
            while (state != Z_STREAM_END);

            return true;
        }

        /*
        * Setup state for decoding a strip.
        */
        private bool ZIPPreDecode(UInt16 s)
        {
            if ((m_state & ZSTATE_INIT_DECODE) == 0)
                tif_setupdecode();

            m_stream.next_in = m_tif.m_rawdata;
            m_stream.avail_in = m_tif.m_rawcc;
            return (inflateReset(&m_stream) == Z_OK);
        }

        /*
        * Reset encoding state at the start of a strip.
        */
        private bool ZIPPreEncode(UInt16 s)
        {
            if (m_state != ZSTATE_INIT_ENCODE)
                tif_setupencode();

            m_stream.next_out = m_tif.m_rawdata;
            m_stream.avail_out = m_tif.m_rawdatasize;
            return (deflateReset(&m_stream) == Z_OK);
        }

        private bool ZIPSetupDecode()
        {
            static const char module[] = "ZIPSetupDecode";

            /* if we were last encoding, terminate this mode */
            if ((m_state & ZSTATE_INIT_ENCODE) != 0)
            {
                deflateEnd(&m_stream);
                m_state = 0;
            }

            if (inflateInit(&m_stream) != Z_OK)
            {
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: %s", m_tif.m_name, m_stream.msg);
                return false;
            }

            m_state |= ZSTATE_INIT_DECODE;
            return true;
        }

        private bool ZIPSetupEncode()
        {
            static const char module[] = "ZIPSetupEncode";

            if ((m_state & ZSTATE_INIT_DECODE) != 0)
            {
                inflateEnd(&m_stream);
                m_state = 0;
            }

            if (deflateInit(&m_stream, m_zipquality) != Z_OK)
            {
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "%s: %s", m_tif.m_name, m_stream.msg);
                return false;
            }

            m_state |= ZSTATE_INIT_ENCODE;
            return true;
        }
    }
}
