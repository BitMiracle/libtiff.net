/* Copyright (C) 2008-2011, Bit Miracle
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

namespace BitMiracle.LibTiff.Classic.Internal
{
    enum OJPEGStateOutState
    {
        ososSoi,

        ososQTable0,
        ososQTable1,
        ososQTable2,
        ososQTable3,

        ososDcTable0,
        ososDcTable1,
        ososDcTable2,
        ososDcTable3,

        ososAcTable0,
        ososAcTable1,
        ososAcTable2,
        ososAcTable3,

        ososDri,
        ososSof,
        ososSos,
        ososCompressed,
        ososRst,
        ososEoi
    }
}
