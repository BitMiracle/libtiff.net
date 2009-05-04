using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.NET
{
    /// <summary>
    /// Upsampling (note that upsampler must also call color converter)
    /// </summary>
    abstract class jpeg_upsampler
    {
        protected bool m_need_context_rows; /* true if need rows above & below */

        public abstract void start_pass();
        public abstract void upsample(ComponentBuffer input_buf, ref uint in_row_group_ctr, uint in_row_groups_avail, byte[][] output_buf, ref uint out_row_ctr, uint out_rows_avail);

        public bool NeedContextRows()
        {
            return m_need_context_rows;
        }
    }
}
