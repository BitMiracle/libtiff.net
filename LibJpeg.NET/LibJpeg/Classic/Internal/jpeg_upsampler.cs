/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.Classic.Internal
{
    /// <summary>
    /// Upsampling (note that upsampler must also call color converter)
    /// </summary>
    abstract class jpeg_upsampler
    {
        protected bool m_need_context_rows; /* true if need rows above & below */

        public abstract void start_pass();
        public abstract void upsample(ComponentBuffer[] input_buf, ref uint in_row_group_ctr, uint in_row_groups_avail, byte[][] output_buf, ref uint out_row_ctr, uint out_rows_avail);

        public bool NeedContextRows()
        {
            return m_need_context_rows;
        }
    }
}
