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

using ComponentAce.Compression.Libs.zlib;

namespace BitMiracle.LibTiff.Internal
{
    class DeflateCodecTagMethods : TiffTagMethods
    {
        public override bool vsetfield(Tiff tif, TIFFTAG tag, params object[] ap)
        {
            DeflateCodec sp = tif.m_currentCodec as DeflateCodec;
            const string module = "ZIPVSetField";

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_ZIPQUALITY:
                    sp.m_zipquality = (int)ap[0];
                    if ((sp.m_state & DeflateCodec.ZSTATE_INIT_ENCODE) != 0)
                    {
                        if (sp.m_stream.deflateParams(sp.m_zipquality, zlibConst.Z_DEFAULT_STRATEGY) != zlibConst.Z_OK)
                        {
                            Tiff.ErrorExt(tif, tif.m_clientdata, module, "{0}: zlib error: {0}", tif.m_name, sp.m_stream.msg);
                            return false;
                        }
                    }

                    return true;
            }

            return base.vsetfield(tif, tag, ap);
        }

        public override object[] vgetfield(Tiff tif, TIFFTAG tag)
        {
            DeflateCodec sp = tif.m_currentCodec as DeflateCodec;

            switch (tag)
            {
                case TIFFTAG.TIFFTAG_ZIPQUALITY:
                    object[] result = new object[1];
                    result[0] = sp.m_zipquality;
                    return result;
            }

            return base.vgetfield(tif, tag);
        }
    }
}
