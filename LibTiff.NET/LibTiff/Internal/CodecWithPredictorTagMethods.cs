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
using System.IO;
using System.Diagnostics;

namespace BitMiracle.LibTiff.Internal
{
    class CodecWithPredictorTagMethods : TiffTagMethods
    {
        public override bool vsetfield(Tiff tif, TIFFTAG tag, params object[] ap)
        {
            CodecWithPredictor sp = tif.m_currentCodec as CodecWithPredictor;
            Debug.Assert(sp != null);

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_PREDICTOR:
                    sp.SetPredictorValue((PREDICTOR)ap[0]);
                    tif.setFieldBit(CodecWithPredictor.FIELD_PREDICTOR);
                    tif.m_flags |= Tiff.TIFF_DIRTYDIRECT;
                    return true;
            }

            TiffTagMethods childMethods = sp.GetChildTagMethods();
            if (childMethods != null)
                return childMethods.vsetfield(tif, tag, ap);

            return base.vsetfield(tif, tag, ap);
        }

        public override object[] vgetfield(Tiff tif, TIFFTAG tag)
        {
            CodecWithPredictor sp = tif.m_currentCodec as CodecWithPredictor;
            Debug.Assert(sp != null);

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_PREDICTOR:
                    object[] result = new object[1];
                    result[0] = sp.GetPredictorValue();
                    return result;
            }

            TiffTagMethods childMethods = sp.GetChildTagMethods();
            if (childMethods != null)
                return childMethods.vgetfield(tif, tag);

            return base.vgetfield(tif, tag);
        }

        public override void printdir(Tiff tif, Stream fd, TiffPrintDirectoryFlags flags)
        {
            CodecWithPredictor sp = tif.m_currentCodec as CodecWithPredictor;

            if (tif.fieldSet(CodecWithPredictor.FIELD_PREDICTOR))
            {
                Tiff.fprintf(fd, "  Predictor: ");
                PREDICTOR predictor = sp.GetPredictorValue();
                switch (predictor)
                {
                    case PREDICTOR.PREDICTOR_NONE:
                        Tiff.fprintf(fd, "none ");
                        break;
                    case PREDICTOR.PREDICTOR_HORIZONTAL:
                        Tiff.fprintf(fd, "horizontal differencing ");
                        break;
                    case PREDICTOR.PREDICTOR_FLOATINGPOINT:
                        Tiff.fprintf(fd, "floating point predictor ");
                        break;
                }

                Tiff.fprintf(fd, "%u (0x%x)\n", predictor, predictor);
            }

            TiffTagMethods childMethods = sp.GetChildTagMethods();
            if (childMethods != null)
                childMethods.printdir(tif, fd, flags);
            else
                base.printdir(tif, fd, flags);
        }
    }
}
