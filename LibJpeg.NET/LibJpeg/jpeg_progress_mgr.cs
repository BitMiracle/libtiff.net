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

namespace LibJpeg.NET
{
    // Progress monitor object
    public abstract class jpeg_progress_mgr
    {
        // work units completed in this pass
        public long m_pass_counter;

        // total number of work units in this pass
        public long m_pass_limit;

        // passes completed so far
        public int m_completed_passes;

        // total number of passes expected
        public int m_total_passes;

        public abstract void progress_monitor();
    }
}
