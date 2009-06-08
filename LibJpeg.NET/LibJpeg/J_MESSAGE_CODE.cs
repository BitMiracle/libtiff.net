/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * Copyright (C) 1994-1996, Thomas G. Lane.
 * This file is part of the Independent JPEG Group's software.
 * For conditions of distribution and use, see the accompanying README file.
 *
 */

/*
 * This file defines the error and message codes for the JPEG library.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace LibJpeg.Classic
{
    public enum J_MESSAGE_CODE
    {
        // Must be first entry!
        JMSG_NOMESSAGE,

        // For maintenance convenience, list is alphabetical by message code name
        JERR_ARITH_NOTIMPL,
        JERR_BAD_BUFFER_MODE,
        JERR_BAD_COMPONENT_ID,
        JERR_BAD_DCT_COEF,
        JERR_BAD_DCTSIZE,
        JERR_BAD_HUFF_TABLE,
        JERR_BAD_IN_COLORSPACE,
        JERR_BAD_J_COLORSPACE,
        JERR_BAD_LENGTH,
        JERR_BAD_MCU_SIZE,
        JERR_BAD_PRECISION,
        JERR_BAD_PROGRESSION,
        JERR_BAD_PROG_SCRIPT,
        JERR_BAD_SAMPLING,
        JERR_BAD_SCAN_SCRIPT,
        JERR_BAD_STATE,
        JERR_BAD_VIRTUAL_ACCESS,
        JERR_BUFFER_SIZE,
        JERR_CANT_SUSPEND,
        JERR_CCIR601_NOTIMPL,
        JERR_COMPONENT_COUNT,
        JERR_CONVERSION_NOTIMPL,
        JERR_DHT_INDEX,
        JERR_DQT_INDEX,
        JERR_EMPTY_IMAGE,
        JERR_EOI_EXPECTED,
        JERR_FILE_WRITE,
        JERR_FRACT_SAMPLE_NOTIMPL,
        JERR_HUFF_CLEN_OVERFLOW,
        JERR_HUFF_MISSING_CODE,
        JERR_IMAGE_TOO_BIG,
        JERR_INPUT_EMPTY,
        JERR_INPUT_EOF,
        JERR_MISMATCHED_QUANT_TABLE,
        JERR_MISSING_DATA,
        JERR_MODE_CHANGE,
        JERR_NOTIMPL,
        JERR_NOT_COMPILED,
        JERR_NO_HUFF_TABLE,
        JERR_NO_IMAGE,
        JERR_NO_QUANT_TABLE,
        JERR_NO_SOI,
        JERR_OUT_OF_MEMORY,
        JERR_QUANT_COMPONENTS,
        JERR_QUANT_FEW_COLORS,
        JERR_QUANT_MANY_COLORS,
        JERR_SOF_DUPLICATE,
        JERR_SOF_NO_SOS,
        JERR_SOF_UNSUPPORTED,
        JERR_SOI_DUPLICATE,
        JERR_SOS_NO_SOF,
        JERR_TOO_LITTLE_DATA,
        JERR_UNKNOWN_MARKER,
        JERR_WIDTH_OVERFLOW,
        JTRC_16BIT_TABLES,
        JTRC_ADOBE,
        JTRC_APP0,
        JTRC_APP14,
        JTRC_DHT,
        JTRC_DQT,
        JTRC_DRI,
        JTRC_EOI,
        JTRC_HUFFBITS,
        JTRC_JFIF,
        JTRC_JFIF_BADTHUMBNAILSIZE,
        JTRC_JFIF_EXTENSION,
        JTRC_JFIF_THUMBNAIL,
        JTRC_MISC_MARKER,
        JTRC_PARMLESS_MARKER,
        JTRC_QUANTVALS,
        JTRC_QUANT_3_NCOLORS,
        JTRC_QUANT_NCOLORS,
        JTRC_QUANT_SELECTED,
        JTRC_RECOVERY_ACTION,
        JTRC_RST,
        JTRC_SMOOTH_NOTIMPL,
        JTRC_SOF,
        JTRC_SOF_COMPONENT,
        JTRC_SOI,
        JTRC_SOS,
        JTRC_SOS_COMPONENT,
        JTRC_SOS_PARAMS,
        JTRC_THUMB_JPEG,
        JTRC_THUMB_PALETTE,
        JTRC_THUMB_RGB,
        JTRC_UNKNOWN_IDS,
        JWRN_ADOBE_XFORM,
        JWRN_BOGUS_PROGRESSION,
        JWRN_EXTRANEOUS_DATA,
        JWRN_HIT_MARKER,
        JWRN_HUFF_BAD_CODE,
        JWRN_JFIF_MAJOR,
        JWRN_JPEG_EOF,
        JWRN_MUST_RESYNC,
        JWRN_NOT_SEQUENTIAL,
        JWRN_TOO_MUCH_DATA,
        JMSG_UNKNOWNMSGCODE,
        JMSG_LASTMSGCODE
    }
}
