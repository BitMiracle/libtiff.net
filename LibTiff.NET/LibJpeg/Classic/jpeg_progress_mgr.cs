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
    class jpeg_progress_mgr
    {
        private int m_passCounter;
        private int m_passLimit;
        private int m_completedPasses;
        private int m_totalPasses;

        public event EventHandler OnProgress;

        // work units completed in this pass
        public int Pass_counter
        {
            get { return m_passCounter; }
            set { m_passCounter = value; }
        }
        
        // total number of work units in this pass
        public int Pass_limit
        {
            get { return m_passLimit; }
            set { m_passLimit = value; }
        }
        
        // passes completed so far
        public int Completed_passes
        {
            get { return m_completedPasses; }
            set { m_completedPasses = value; }
        }
        
        // total number of passes expected
        public int Total_passes
        {
            get { return m_totalPasses; }
            set { m_totalPasses = value; }
        }

        public void Updated()
        {
            if (OnProgress != null)
                OnProgress(this, new EventArgs());
        }
    }
}