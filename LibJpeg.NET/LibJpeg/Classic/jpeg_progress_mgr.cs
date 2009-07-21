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

namespace BitMiracle.LibJpeg.Classic
{
    // Progress monitor object
#if EXPOSE_LIBJPEG
    public
#endif
    abstract class jpeg_progress_mgr
    {
        internal int m_pass_counter;
        internal int m_pass_limit;
        internal int m_completed_passes;
        internal int m_total_passes;

        // work units completed in this pass
        public int Pass_counter
        {
            get { return m_pass_counter; }
            set { m_pass_counter = value; }
        }
        
        // total number of work units in this pass
        public int Pass_limit
        {
            get { return m_pass_limit; }
            set { m_pass_limit = value; }
        }
        
        // passes completed so far
        public int Completed_passes
        {
            get { return m_completed_passes; }
            set { m_completed_passes = value; }
        }
        
        // total number of passes expected
        public int Total_passes
        {
            get { return m_total_passes; }
            set { m_total_passes = value; }
        }

        public abstract void progress_monitor();
    }
}
