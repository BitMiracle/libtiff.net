/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.Classic
{
    /// <summary>
    /// Describes a result of read operation
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum ReadResult
    {
        JPEG_SUSPENDED = 0, /* Suspended due to lack of input data */
        JPEG_HEADER_OK = 1, /* Found valid image datastream */
        JPEG_HEADER_TABLES_ONLY = 2, /* Found valid table-specs-only datastream */
        JPEG_REACHED_SOS = 3, /* Reached start of new scan */
        JPEG_REACHED_EOI = 4, /* Reached end of image */
        JPEG_ROW_COMPLETED = 5, /* Completed one iMCU row */
        JPEG_SCAN_COMPLETED = 6 /* Completed last iMCU row of a scan */
    }
}
