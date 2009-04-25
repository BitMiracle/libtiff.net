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
    /// <summary>
    /// Coefficient buffer control
    /// </summary>
    public interface jpeg_c_coef_controller
    {
        void start_pass(J_BUF_MODE pass_mode);
        bool compress_data(byte[][][] input_buf);
    }
}
