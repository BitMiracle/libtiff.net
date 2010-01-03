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

using System.Collections;
using System.Diagnostics;

namespace BitMiracle.LibTiff.Classic.Internal
{
    internal class TagCompare : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            TiffFieldInfo ta = x as TiffFieldInfo;
            TiffFieldInfo tb = y as TiffFieldInfo;

            Debug.Assert(ta != null);
            Debug.Assert(tb != null);

            /* NB: be careful of return values for 16-bit platforms */
            if (ta.Field_tag != tb.Field_tag)
                return ((int)ta.Field_tag - (int)tb.Field_tag);

            return (ta.Field_type == TiffType.ANY) ? 0 : ((int)tb.Field_type - (int)ta.Field_type);
        }
    }
}
