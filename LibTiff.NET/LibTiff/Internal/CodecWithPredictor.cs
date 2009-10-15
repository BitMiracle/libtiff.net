/*
 * TIFF Library.
 *
 * Predictor Tag Support (used by multiple codecs).
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// Codecs that want to support the Predictor tag should inherit from 
    /// this class instead of TiffCodec. 
    /// 
    /// Such codecs should not override default TiffCodec's methods for 
    /// decode|encode setup and encoding|decoding of row|tile|strip. 
    /// Codecs with predictor support should override equivalent methods 
    /// provided by this class.
    /// 
    /// If codec wants to provide custom tag get|set|print methods, then
    /// it should pass pointer to a object derived from TiffTagMethods
    /// as parameter to TIFFPredictorInit
    /// </summary>
    class CodecWithPredictor : TiffCodec
    {
        public const int FIELD_PREDICTOR = (FIELD.FIELD_CODEC + 0);
        
        private enum PredictorType
        {
            ptNone,
            ptHorAcc8,
            ptHorAcc16,
            ptHorAcc32,
            ptSwabHorAcc16,
            ptSwabHorAcc32,
            ptHorDiff8,
            ptHorDiff16,
            ptHorDiff32,
            ptFpAcc,
            ptFpDiff,
        };

        private static TiffFieldInfo[] predictFieldInfo = 
        {
            new TiffFieldInfo(TIFFTAG.TIFFTAG_PREDICTOR, 1, 1, TiffDataType.TIFF_SHORT, CodecWithPredictor.FIELD_PREDICTOR, false, false, "Predictor"), 
        };

        private int predictor; /* predictor tag value */
        private int stride; /* sample stride over data */
        private int rowsize; /* tile/strip row size */

        private TiffTagMethods m_parentTagMethods;
        private TiffTagMethods m_tagMethods;
        private TiffTagMethods m_childTagMethods; // could be NULL

        private bool m_passThruDecode;
        private bool m_passThruEncode;

        private PredictorType m_predictorType; /* horizontal differencer/accumulator */

        public CodecWithPredictor(Tiff tif, COMPRESSION scheme, string name)
            : base(tif, scheme, name)
        {
            m_tagMethods = new CodecWithPredictorTagMethods();
        }

        // tagMethods can be NULL
        public void TIFFPredictorInit(TiffTagMethods tagMethods)
        {
            /*
            * Merge codec-specific tag information and
            * override parent get/set field methods.
            */
            m_tif.MergeFieldInfo(predictFieldInfo, sizeof(predictFieldInfo) / sizeof(predictFieldInfo[0]));
            m_childTagMethods = tagMethods;
            m_parentTagMethods = m_tif.m_tagmethods;
            m_tif.m_tagmethods = m_tagMethods;

            predictor = 1; /* default value */
            m_predictorType = ptNone; /* no predictor routine */
        }

        public void TIFFPredictorCleanup()
        {
            m_tif.m_tagmethods = m_parentTagMethods;
        }

        //////////////////////////////////////////////////////////////////////////
        // WARNING: do not override this methods!
        //          please override their equivalents listed below

        public override bool tif_setupdecode()
        {
            return PredictorSetupDecode();
        }

        public override bool tif_decoderow(byte[] pp, int cc, UInt16 s)
        {
            if (!m_passThruDecode)
                return PredictorDecodeRow(pp, cc, s);

            return predictor_decoderow(pp, cc, s);
        }

        public override bool tif_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            if (!m_passThruDecode)
                return PredictorDecodeTile(pp, cc, s);

            return predictor_decodestrip(pp, cc, s);
        }

        public override bool tif_decodetile(byte[] pp, int cc, UInt16 s)
        {
            if (!m_passThruDecode)
                return PredictorDecodeTile(pp, cc, s);

            return predictor_decodetile(pp, cc, s);
        }

        public override bool tif_setupencode()
        {
            return PredictorSetupEncode();
        }

        public override bool tif_encoderow(byte[] pp, int cc, UInt16 s)
        {
            if (!m_passThruEncode)
                return PredictorEncodeRow(pp, cc, s);

            return predictor_encoderow(pp, cc, s);
        }

        public override bool tif_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            if (!m_passThruEncode)
                return PredictorEncodeTile(pp, cc, s);

            return predictor_encodestrip(pp, cc, s);
        }

        public override bool tif_encodetile(byte[] pp, int cc, UInt16 s)
        {
            if (!m_passThruEncode)
                return PredictorEncodeTile(pp, cc, s);

            return predictor_encodetile(pp, cc, s);
        }

        //////////////////////////////////////////////////////////////////////////
        // derived class should override methods below instead of 
        // TiffCodec's methods

        public virtual bool predictor_setupdecode()
        {
            return TiffCodec::tif_setupdecode();
        }

        public virtual bool predictor_decoderow(byte[] pp, int cc, UInt16 s)
        {
            return TiffCodec::tif_decoderow(pp, cc, s);
        }

        public virtual bool predictor_decodestrip(byte[] pp, int cc, UInt16 s)
        {
            return TiffCodec::tif_decodestrip(pp, cc, s);
        }

        public virtual bool predictor_decodetile(byte[] pp, int cc, UInt16 s)
        {
            return TiffCodec::tif_decodetile(pp, cc, s);
        }

        public virtual bool predictor_setupencode()
        {
            return TiffCodec::tif_setupencode();
        }

        public virtual bool predictor_encoderow(byte[] pp, int cc, UInt16 s)
        {
            return TiffCodec::tif_encoderow(pp, cc, s);
        }

        public virtual bool predictor_encodestrip(byte[] pp, int cc, UInt16 s)
        {
            return TiffCodec::tif_encodestrip(pp, cc, s);
        }

        public virtual bool predictor_encodetile(byte[] pp, int cc, UInt16 s)
        {
            return TiffCodec::tif_encodetile(pp, cc, s);
        }

        public int GetPredictorValue()
        {
            return predictor;
        }

        public void SetPredictorValue(int value)
        {
            predictor = value;
        }

        // retrieves child object's tag methods (could be NULL)
        public TiffTagMethods GetChildTagMethods()
        {
            return m_childTagMethods;
        }

        private void predictorFunc(byte[] cp0, int offset, int cc)
        {
            switch (m_predictorType)
            {
                case ptHorAcc8:
                    horAcc8(cp0, offset, cc);
                    break;
                case ptHorAcc16:
                    horAcc16(cp0, offset, cc);
                    break;
                case ptHorAcc32:
                    horAcc32(cp0, offset, cc);
                    break;
                case ptSwabHorAcc16:
                    swabHorAcc16(cp0, offset, cc);
                    break;
                case ptSwabHorAcc32:
                    swabHorAcc32(cp0, offset, cc);
                    break;
                case ptHorDiff8:
                    horDiff8(cp0, offset, cc);
                    break;
                case ptHorDiff16:
                    horDiff16(cp0, offset, cc);
                    break;
                case ptHorDiff32:
                    horDiff32(cp0, offset, cc);
                    break;
                case ptFpAcc:
                    fpAcc(cp0, offset, cc);
                    break;
                case ptFpDiff:
                    fpDiff(cp0, offset, cc);
                    break;
            }
        }

        private void horAcc8(byte[] cp0, int offset, int cc)
        {
            int cp = offset;
            if (cc > stride)
            {
                cc -= stride;
                /*
                * Pipeline the most common cases.
                */
                if (stride == 3)
                {
                    unsigned int cr = cp0[cp];
                    unsigned int cg = cp0[cp + 1];
                    unsigned int cb = cp0[cp + 2];
                    do
                    {
                        cc -= 3;
                        cp += 3;

                        cr += cp0[cp];
                        cp0[cp] = (byte)cr;
                        
                        cg += cp0[cp + 1];
                        cp0[cp + 1] = (byte)cg;

                        cb += cp0[cp + 2];
                        cp0[cp + 2] = (byte)cb;
                    }
                    while (cc > 0);
                }
                else if (stride == 4)
                {
                    unsigned int cr = cp0[cp];
                    unsigned int cg = cp0[cp + 1];
                    unsigned int cb = cp0[cp + 2];
                    unsigned int ca = cp0[cp + 3];
                    do
                    {
                        cc -= 4;
                        cp += 4;

                        cr += cp0[cp];
                        cp0[cp] = (byte)cr;

                        cg += cp0[cp + 1];
                        cp0[cp + 1] = (byte)cg;

                        cb += cp0[cp + 2];
                        cp0[cp + 2] = (byte)cb;

                        ca += cp0[cp + 3];
                        cp0[cp + 3] = (byte)ca;
                    }
                    while (cc > 0);
                }
                else
                {
                    do
                    {
                        if (stride > 4 || stride < 0)
                        {
                            for (int i = stride - 4; i > 0; i--)
                            {
                                cp0[cp + stride] = cp0[cp + stride] + cp0[cp];
                                cp++;
                            }
                        }
                        else
                        {
                            for (int i = stride; i > 0; i--)
                            {
                                cp0[cp + stride] = cp0[cp + stride] + cp0[cp];
                                cp++;
                            }
                        }

                        cc -= stride;
                    }
                    while (cc > 0);
                }
            }
        }

        private void horAcc16(byte[] cp0, int offset, int cc)
        {
            UInt16* wp = Tiff::byteArrayToUInt16(cp0, offset, cc);
            int wpPos = 0;

            int wc = cc / 2;
            if (wc > stride)
            {
                wc -= stride;
                do
                {
                    if (stride > 4 || stride < 0)
                    {
                        for (int i = stride - 4; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }
                    else
                    {
                        for (int i = stride; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }

                    wc -= stride;
                }
                while ((int)wc > 0);
            }

            Tiff::uint16ToByteArray(wp, 0, cc / 2, cp0, offset);
            delete[] wp;
        }

        private void horAcc32(byte[] cp0, int offset, int cc)
        {
            uint* wp = Tiff::byteArrayToUInt(cp0, offset, cc);
            int wpPos = 0;

            int wc = cc / 4;
            if (wc > stride)
            {
                wc -= stride;
                do
                {
                    if (stride > 4 || stride < 0)
                    {
                        for (int i = stride - 4; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }
                    else
                    {
                        for (int i = stride; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }

                    wc -= stride;
                } while ((int) wc > 0);
            }

            Tiff::uintToByteArray(wp, 0, cc / 4, cp0, offset);
            delete[] wp;
        }

        private void swabHorAcc16(byte[] cp0, int offset, int cc)
        {
            UInt16* wp = Tiff::byteArrayToUInt16(cp0, offset, cc);
            int wpPos= 0;
            
            int wc = cc / 2;
            if (wc > stride)
            {
                Tiff::SwabArrayOfShort(wp, wc);
                wc -= stride;
                do
                {
                    if (stride > 4 || stride < 0)
                    {
                        for (int i = stride - 4; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }
                    else
                    {
                        for (int i = stride; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }

                    wc -= stride;
                }
                while ((int)wc > 0);
            }

            Tiff::uint16ToByteArray(wp, 0, cc / 2, cp0, offset);
            delete[] wp;
        }
        
        private void swabHorAcc32(byte[] cp0, int offset, int cc)
        {
            uint* wp = Tiff::byteArrayToUInt(cp0, offset, cc);
            int wpPos = 0;

            int wc = cc / 4;
            if (wc > stride)
            {
                m_tif.SwabArrayOfLong(wp, wc);
                wc -= stride;
                do
                {
                    if (stride > 4 || stride < 0)
                    {
                        for (int i = stride - 4; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }
                    else
                    {
                        for (int i = stride; i > 0; i--)
                        {
                            wp[wpPos + stride] += wp[wpPos];
                            wpPos++;
                        }
                    }

                    wc -= stride;
                } while ((int) wc > 0);
            }

            Tiff::uintToByteArray(wp, 0, cc / 4, cp0, offset);
            delete[] wp;
        }

        private void horDiff8(byte[] cp0, int offset, int cc)
        {
            if (cc > stride)
            {
                cc -= stride;
                int cp = offset;

                /*
                * Pipeline the most common cases.
                */
                if (stride == 3)
                {
                    int r2 = cp0[cp];
                    int g2 = cp0[cp + 1];
                    int b2 = cp0[cp + 2];
                    do
                    {
                        int r1 = cp0[cp + 3];
                        cp0[cp + 3] = (byte)(r1 - r2);
                        r2 = r1;

                        int g1 = cp0[cp + 4];
                        cp0[cp + 4] = (byte)(g1 - g2);
                        g2 = g1;

                        int b1 = cp0[cp + 5];
                        cp0[cp + 5] = (byte)(b1 - b2);
                        b2 = b1;

                        cp += 3;
                    }
                    while ((cc -= 3) > 0);
                }
                else if (stride == 4)
                {
                    int r2 = cp0[cp];
                    int g2 = cp0[cp + 1];
                    int b2 = cp0[cp + 2];
                    int a2 = cp0[cp + 3];
                    do
                    {
                        int r1 = cp0[cp + 4];
                        cp0[cp + 4] = (byte)(r1 - r2);
                        r2 = r1;

                        int g1 = cp0[cp + 5];
                        cp0[cp + 5] = (byte)(g1 - g2);
                        g2 = g1;

                        int b1 = cp0[cp + 6];
                        cp0[cp + 6] = (byte)(b1 - b2);
                        b2 = b1;

                        int a1 = cp0[cp + 7];
                        cp0[cp + 7] = (byte)(a1 - a2);
                        a2 = a1;

                        cp += 4;
                    }
                    while ((cc -= 4) > 0);
                }
                else
                {
                    cp += cc - 1;
                    do
                    {
                        if (stride > 4 || stride < 0)
                        {
                            for (int i = stride - 4; i > 0; i--)
                            {
                                cp0[cp + stride] -= cp0[cp];
                                cp--;
                            }
                        }
                        else
                        {
                            for (int i = stride; i > 0; i--)
                            {
                                cp0[cp + stride] -= cp0[cp];
                                cp--;
                            }
                        }
                    }
                    while ((cc -= stride) > 0);
                }
            }
        }

        private void horDiff16(byte[] cp0, int offset, int cc)
        {
            Int16* wp = Tiff::byteArrayToInt16(cp0, offset, cc);
            int wpPos = 0;

            int wc = cc / 2;
            if (wc > stride)
            {
                wc -= stride;
                wpPos += wc - 1;
                do
                {
                    if (stride > 4 || stride < 0)
                    {
                        for (int i = stride - 4; i > 0; i--)
                        {
                            wp[wpPos + stride] -= wp[wpPos];
                            wpPos--;
                        }
                    }
                    else
                    {
                        for (int i = stride; i > 0; i--)
                        {
                            wp[wpPos + stride] -= wp[wpPos];
                            wpPos--;
                        }
                    }

                    wc -= stride;
                }
                while ((int)wc > 0);
            }

            Tiff::int16ToByteArray(wp, 0, cc / 2, cp0, offset);
            delete[] wp;
        }

        private void horDiff32(byte[] cp0, int offset, int cc)
        {
            int* wp = Tiff::byteArrayToInt(cp0, offset, cc);
            int wpPos = 0;

            int wc = cc / 4;
            if (wc > stride)
            {
                wc -= stride;
                wpPos += wc - 1;
                do
                {
                    if (stride > 4 || stride < 0)
                    {
                        for (int i = stride - 4; i > 0; i--)
                        {
                            wp[wpPos + stride] -= wp[wpPos];
                            wpPos--;
                        }
                    }
                    else
                    {
                        for (int i = stride; i > 0; i--)
                        {
                            wp[wpPos + stride] -= wp[wpPos];
                            wpPos--;
                        }
                    }

                    wc -= stride;
                } while ((int) wc > 0);
            }

            Tiff::intToByteArray(wp, 0, cc / 4, cp0, offset);
            delete[] wp;
        }
        
        /*
        * Floating point predictor accumulation routine.
        */
        private void fpAcc(byte[] cp0, int offset, int cc)
        {
            uint bps = m_tif.m_dir.td_bitspersample / 8;
            int wc = cc / bps;
            byte[] tmp = new byte [cc];
            if (!tmp)
                return ;

            int count = cc;
            int cp = offset;

            while (count > stride)
            {
                if (stride > 4 || stride < 0)
                {
                    for (int i = stride - 4; i > 0; i--)
                    {
                        cp0[cp + stride] += cp0[cp];
                        cp++;
                    }
                }
                else
                {
                    for (int i = stride; i > 0; i--)
                    {
                        cp0[cp + stride] += cp0[cp];
                        cp++;
                    }
                }

                count -= stride;
            }

            memcpy(tmp, cp0 + offset, cc);
            for (count = 0; count < wc; count++)
            {
                uint byte;
                for (byte = 0; byte < bps; byte++)
                {
                    cp0[offset + bps * count + byte] = tmp[(bps - byte - 1) * wc + count];
                }
            }

            delete tmp;
        }

        /*
        * Floating point predictor differencing routine.
        */
        private void fpDiff(byte[] cp0, int offset, int cc)
        {
            byte[] tmp = new byte [cc];
            if (!tmp)
                return ;

            memcpy(tmp, cp0 + offset, cc);

            uint bps = m_tif.m_dir.td_bitspersample / 8;
            int wc = cc / bps;
            for (int count = 0; count < wc; count++)
            {
                uint byte;
                for (byte = 0; byte < bps; byte++)
                {
                    cp0[offset + (bps - byte - 1) * wc + count] = tmp[bps * count + byte];
                }
            }

            delete tmp;

            int cp = offset + cc - stride - 1;
            for (int count = cc; count > stride; count -= stride)
            {
                if (stride > 4 || stride < 0)
                {
                    for (int i = stride - 4; i > 0; i--)
                    {
                        cp0[cp + stride] -= cp0[cp];
                        cp--;
                    }
                }
                else
                {
                    for (int i = stride; i > 0; i--)
                    {
                        cp0[cp + stride] -= cp0[cp];
                        cp--;
                    }
                }
            }
        }
                
        /*
        * Decode a scanline and apply the predictor routine.
        */
        private bool PredictorDecodeRow(byte[] op0, int occ0, UInt16 s)
        {
            assert(m_predictorType != ptNone);

            if (predictor_decoderow(op0, occ0, s))
            {
                predictorFunc(op0, 0, occ0);
                return true;
            }

            return false;
        }

        /*
        * Decode a tile/strip and apply the predictor routine.
        * Note that horizontal differencing must be done on a
        * row-by-row basis.  The width of a "row" has already
        * been calculated at pre-decode time according to the
        * strip/tile dimensions.
        */
        private bool PredictorDecodeTile(byte[] op0, int occ0, UInt16 s)
        {
            if (predictor_decodetile(op0, occ0, s))
            {
                assert(rowsize > 0);
                assert(m_predictorType != ptNone);

                int offset = 0;
                while (occ0 > 0)
                {
                    predictorFunc(op0, offset, rowsize);
                    occ0 -= rowsize;
                    offset += rowsize;
                }

                return true;
            }

            return false;
        }

        private bool PredictorEncodeRow(byte[] op0, int occ0, UInt16 s)
        {
            assert(m_predictorType != ptNone);

            /* XXX horizontal differencing alters user's data XXX */
            predictorFunc(bp, 0, cc);
            return predictor_encoderow(bp, cc, s);
        }

        private bool PredictorEncodeTile(byte[] op0, int occ0, UInt16 s)
        {
            static const char module[] = "PredictorEncodeTile";
            assert(m_predictorType != ptNone);

            /* 
            * Do predictor manipulation in a working buffer to avoid altering
            * the callers buffer. http://trac.osgeo.org/gdal/ticket/1965
            */
            byte[] working_copy = new byte [cc0];
            if (working_copy == NULL)
            {
                Tiff::ErrorExt(m_tif.m_clientdata, module, "Out of memory allocating %d byte temp buffer.", cc0);
                return false;
            }

            memcpy(working_copy, bp0, cc0);

            assert(rowsize > 0);
            assert((cc0 % rowsize) == 0);

            int cc = cc0;
            int offset = 0;
            while (cc > 0)
            {
                predictorFunc(working_copy, offset, rowsize);
                cc -= rowsize;
                offset += rowsize;
            }

            bool result_code = predictor_encodetile(working_copy, cc0, s);
            delete working_copy;
            return result_code;
        }

        private bool PredictorSetupDecode()
        {
            if (!predictor_setupdecode() || !PredictorSetup())
                return false;

            CodecWithPredictor::m_passThruDecode = true;
            if (predictor == 2)
            {
                switch (m_tif.m_dir.td_bitspersample)
                {
                case 8:
                    m_predictorType = ptHorAcc8;
                    break;
                case 16:
                    m_predictorType = ptHorAcc16;
                    break;
                case 32:
                    m_predictorType = ptHorAcc32;
                    break;
                }
                /*
                * Override default decoding method with one that does the
                * predictor stuff.
                */
                CodecWithPredictor::m_passThruDecode = false;
                /*
                * If the data is horizontally differenced 16-bit data that
                * requires byte-swapping, then it must be byte swapped before
                * the accumulation step.  We do this with a special-purpose
                * routine and override the normal post decoding logic that
                * the library setup when the directory was read.
                */
                if ((m_tif.m_flags & Tiff::TIFF_SWAB) != 0)
                {
                    if (m_predictorType == ptHorAcc16)
                    {
                        m_predictorType = ptSwabHorAcc16;
                        m_tif.m_postDecodeMethod = Tiff::pdmNone;
                    }
                    else if (m_predictorType == ptHorAcc32)
                    {
                        m_predictorType = ptSwabHorAcc32;
                        m_tif.m_postDecodeMethod = Tiff::pdmNone;
                    }
                }
            }
            else if (predictor == 3)
            {
                m_predictorType = ptFpAcc;
                
                /*
                * Override default decoding method with one that does the
                * predictor stuff.
                */
                CodecWithPredictor::m_passThruDecode = false;
                
                /*
                * The data should not be swapped outside of the floating
                * point predictor, the accumulation routine should return
                * byres in the native order.
                */
                if ((m_tif.m_flags & Tiff::TIFF_SWAB) != 0)
                    m_tif.m_postDecodeMethod = Tiff::pdmNone;

                /*
                * Allocate buffer to keep the decoded bytes before
                * rearranging in the right order
                */
            }

            return true;
        }

        private bool PredictorSetupEncode()
        {
            if (!predictor_setupencode() || !PredictorSetup())
                return false;

            CodecWithPredictor::m_passThruEncode = true;
            if (predictor == 2)
            {
                switch (m_tif.m_dir.td_bitspersample)
                {
                case 8:
                    m_predictorType = ptHorDiff8;
                    break;
                case 16:
                    m_predictorType = ptHorDiff16;
                    break;
                case 32:
                    m_predictorType = ptHorDiff32;
                    break;
                }
                /*
                * Override default encoding method with one that does the
                * predictor stuff.
                */
                CodecWithPredictor::m_passThruEncode = false;
            }
            else if (predictor == 3)
            {
                m_predictorType = ptFpDiff;
                /*
                * Override default encoding method with one that does the
                * predictor stuff.
                */
                CodecWithPredictor::m_passThruEncode = false;
            }

            return true;
        }

        private bool PredictorSetup()
        {
            static const char module[] = "PredictorSetup";
            TiffDirectory* td = m_tif.m_dir;

            switch (predictor) /* no differencing */
            {
            case PREDICTOR_NONE:
                return true;
            case PREDICTOR_HORIZONTAL:
                if (td.td_bitspersample != 8 && td.td_bitspersample != 16 && td.td_bitspersample != 32)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "Horizontal differencing \"Predictor\" not supported with %d-bit samples", td.td_bitspersample);
                    return false;
                }
                break;
            case PREDICTOR_FLOATINGPOINT:
                if (td.td_sampleformat != SAMPLEFORMAT_IEEEFP)
                {
                    Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "Floating point \"Predictor\" not supported with %d data format", td.td_sampleformat);
                    return false;
                }
                break;
            default:
                Tiff::ErrorExt(m_tif, m_tif.m_clientdata, module, "\"Predictor\" value %d not supported", predictor);
                return false;
            }
            stride = (td.td_planarconfig == PLANARCONFIG_CONTIG ? td.td_samplesperpixel : 1);
            /*
            * Calculate the scanline/tile-width size in bytes.
            */
            if (m_tif.IsTiled())
                rowsize = m_tif.TileRowSize();
            else
                rowsize = m_tif.ScanlineSize();

            return true;
        }
    }
}
