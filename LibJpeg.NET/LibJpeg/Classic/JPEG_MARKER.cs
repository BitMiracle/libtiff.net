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
    /// <summary>
    /// JPEG marker codes
    /// </summary>
#if EXPOSE_LIBJPEG
    public
#endif
    enum JPEG_MARKER
    {
        SOF0 = 0xc0,
        SOF1 = 0xc1,
        SOF2 = 0xc2,
        SOF3 = 0xc3,
        SOF5 = 0xc5,
        SOF6 = 0xc6,
        SOF7 = 0xc7,
        JPG = 0xc8,
        SOF9 = 0xc9,
        SOF10 = 0xca,
        SOF11 = 0xcb,
        SOF13 = 0xcd,
        SOF14 = 0xce,
        SOF15 = 0xcf,
        DHT = 0xc4,
        DAC = 0xcc,
        RST0 = 0xd0, /* RST0 marker code */
        RST1 = 0xd1,
        RST2 = 0xd2,
        RST3 = 0xd3,
        RST4 = 0xd4,
        RST5 = 0xd5,
        RST6 = 0xd6,
        RST7 = 0xd7,
        SOI = 0xd8,
        EOI = 0xd9, /* EOI marker code */
        SOS = 0xda,
        DQT = 0xdb,
        DNL = 0xdc,
        DRI = 0xdd,
        DHP = 0xde,
        EXP = 0xdf,
        APP0 = 0xe0, /* APP0 marker code */
        APP1 = 0xe1,
        APP2 = 0xe2,
        APP3 = 0xe3,
        APP4 = 0xe4,
        APP5 = 0xe5,
        APP6 = 0xe6,
        APP7 = 0xe7,
        APP8 = 0xe8,
        APP9 = 0xe9,
        APP10 = 0xea,
        APP11 = 0xeb,
        APP12 = 0xec,
        APP13 = 0xed,
        APP14 = 0xee,
        APP15 = 0xef,
        JPG0 = 0xf0,
        JPG13 = 0xfd,
        COM = 0xfe, /* COM marker code */
        TEM = 0x01,
        ERROR = 0x100
    }
}
