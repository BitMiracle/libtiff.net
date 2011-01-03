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

/* WARNING: The type of JPEG encapsulation defined by the TIFF Version 6.0
   specification is now totally obsolete and deprecated for new applications and
   images. This file was was created solely in order to read unconverted images
   still present on some users' computer systems. It will never be extended
   to write such files. Writing new-style JPEG compressed TIFFs is implemented
   in tif_jpeg.c.

   The code is carefully crafted to robustly read all gathered JPEG-in-TIFF
   testfiles, and anticipate as much as possible all other... But still, it may
   fail on some. If you encounter problems, please report them on the TIFF
   mailing list and/or to Joris Van Damme <info@awaresystems.be>.

   Please read the file called "TIFF Technical Note #2" if you need to be
   convinced this compression scheme is bad and breaks TIFF. That document
   is linked to from the LibTiff site <http://www.remotesensing.org/libtiff/>
   and from AWare Systems' TIFF section
   <http://www.awaresystems.be/imaging/tiff.html>. It is also absorbed
   in Adobe's specification supplements, marked "draft" up to this day, but
   supported by the TIFF community.

   This file interfaces with Release 6B of the JPEG Library written by the
   Independent JPEG Group. Previous versions of this file required a hack inside
   the LibJpeg library. This version no longer requires that. Remember to
   remove the hack if you update from the old version.

   Copyright (c) Joris Van Damme <info@awaresystems.be>
   Copyright (c) AWare Systems <http://www.awaresystems.be/>
*/

/* What is what, and what is not?

   This decoder starts with an input stream, that is essentially the JpegInterchangeFormat
   stream, if any, followed by the strile data, if any. This stream is read in
   OJPEGReadByte and related functions.

   It analyzes the start of this stream, until it encounters non-marker data, i.e.
   compressed image data. Some of the header markers it sees have no actual content,
   like the SOI marker, and APP/COM markers that really shouldn't even be there. Some
   other markers do have content, and the valuable bits and pieces of information
   in these markers are saved, checking all to verify that the stream is more or
   less within expected bounds. This happens inside the OJPEGReadHeaderInfoSecStreamXxx
   functions.

   Some OJPEG imagery contains no valid JPEG header markers. This situation is picked
   up on if we've seen no SOF marker when we're at the start of the compressed image
   data. In this case, the tables are read from JpegXxxTables tags, and the other
   bits and pieces of information is initialized to its most basic value. This is
   implemented in the OJPEGReadHeaderInfoSecTablesXxx functions.

   When this is complete, a good and valid JPEG header can be assembled, and this is
   passed through to LibJpeg. When that's done, the remainder of the input stream, i.e.
   the compressed image data, can be passed through unchanged. This is done in
   OJPEGWriteStream functions.

   LibTiff rightly expects to know the subsampling values before decompression. Just like
   in new-style JPEG-in-TIFF, though, or even more so, actually, the YCbCrsubsampling
   tag is notoriously unreliable. To correct these tag values with the ones inside
   the JPEG stream, the first part of the input stream is pre-scanned in
   OJPEGSubsamplingCorrect, making no note of any other data, reporting no warnings
   or errors, up to the point where either these values are read, or it's clear they
   aren't there. This means that some of the data is read twice, but we feel speed
   in correcting these values is important enough to warrant this sacrifice. Allthough
   there is currently no define or other configuration mechanism to disable this behaviour,
   the actual header scanning is build to robustly respond with error report if it
   should encounter an uncorrected mismatch of subsampling values. See
   OJPEGReadHeaderInfoSecStreamSof.

   The restart interval and restart markers are the most tricky part... The restart
   interval can be specified in a tag. It can also be set inside the input JPEG stream.
   It can be used inside the input JPEG stream. If reading from strile data, we've
   consistenly discovered the need to insert restart markers in between the different
   striles, as is also probably the most likely interpretation of the original TIFF 6.0
   specification. With all this setting of interval, and actual use of markers that is not
   predictable at the time of valid JPEG header assembly, the restart thing may turn
   out the Achilles heel of this implementation. Fortunately, most OJPEG writer vendors
   succeed in reading back what they write, which may be the reason why we've been able
   to discover ways that seem to work.

   Some special provision is made for planarconfig separate OJPEG files. These seem
   to consistently contain header info, a SOS marker, a plane, SOS marker, plane, SOS,
   and plane. This may or may not be a valid JPEG configuration, we don't know and don't
   care. We want LibTiff to be able to access the planes individually, without huge
   buffering inside LibJpeg, anyway. So we compose headers to feed to LibJpeg, in this
   case, that allow us to pass a single plane such that LibJpeg sees a valid
   single-channel JPEG stream. Locating subsequent SOS markers, and thus subsequent
   planes, is done inside OJPEGReadSecondarySos.

   The benefit of the scheme is... that it works, basically. We know of no other that
   does. It works without checking software tag, or otherwise going about things in an
   OJPEG flavor specific manner. Instead, it is a single scheme, that covers the cases
   with and without JpegInterchangeFormat, with and without striles, with part of
   the header in JpegInterchangeFormat and remainder in first strile, etc. It is forgiving
   and robust, may likely work with OJPEG flavors we've not seen yet, and makes most out
   of the data.

   Another nice side-effect is that a complete JPEG single valid stream is build if
   planarconfig is not separate (vast majority). We may one day use that to build
   converters to JPEG, and/or to new-style JPEG compression inside TIFF.

   A dissadvantage is the lack of random access to the individual striles. This is the
   reason for much of the complicated restart-and-position stuff inside OJPEGPreDecode.
   Applications would do well accessing all striles in order, as this will result in
   a single sequential scan of the input stream, and no restarting of LibJpeg decoding
   session.
*/

/* Configuration defines here are:
 * JPEG_ENCAP_EXTERNAL: The normal way to call libjpeg, uses longjump. In some environments,
 * 	like eg LibTiffDelphi, this is not possible. For this reason, the actual calls to
 * 	libjpeg, with longjump stuff, are encapsulated in dedicated functions. When
 * 	JPEG_ENCAP_EXTERNAL is defined, these encapsulating functions are declared external
 * 	to this unit, and can be defined elsewhere to use stuff other then longjump.
 * 	The default mode, without JPEG_ENCAP_EXTERNAL, implements the call encapsulators
 * 	here, internally, with normal longjump.
 * SETJMP, LONGJMP, JMP_BUF: On some machines/environments a longjump equivalent is
 * 	conviniently available, but still it may be worthwhile to use _setjmp or sigsetjmp
 * 	in place of plain setjmp. These macros will make it easier. It is useless
 * 	to fiddle with these if you define JPEG_ENCAP_EXTERNAL.
 * OJPEG_BUFFER: Define the size of the desired buffer here. Should be small enough so as to guarantee
 * 	instant processing, optimal streaming and optimal use of processor cache, but also big
 * 	enough so as to not result in significant call overhead. It should be at least a few
 * 	bytes to accomodate some structures (this is verified in asserts), but it would not be
 * 	sensible to make it this small anyway, and it should be at most 64K since it is indexed
 * 	with ushort. We recommend 2K.
 * EGYPTIANWALK: You could also define EGYPTIANWALK here, but it is not used anywhere and has
 * 	absolutely no effect. That is why most people insist the EGYPTIANWALK is a bit silly.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

using BitMiracle.LibJpeg.Classic;

namespace BitMiracle.LibTiff.Classic.Internal
{
    class OJpegCodec : TiffCodec
    {
        public const int FIELD_OJPEG_JPEGINTERCHANGEFORMAT = (FieldBit.Codec + 0);
        public const int FIELD_OJPEG_JPEGINTERCHANGEFORMATLENGTH = (FieldBit.Codec + 1);
        public const int FIELD_OJPEG_JPEGQTABLES = (FieldBit.Codec + 2);
        public const int FIELD_OJPEG_JPEGDCTABLES = (FieldBit.Codec + 3);
        public const int FIELD_OJPEG_JPEGACTABLES = (FieldBit.Codec + 4);
        public const int FIELD_OJPEG_JPEGPROC = (FieldBit.Codec + 5);
        public const int FIELD_OJPEG_JPEGRESTARTINTERVAL = (FieldBit.Codec + 6);
        public const int FIELD_OJPEG_COUNT = 7;

        private static TiffFieldInfo[] ojpeg_field_info =
        {
            new TiffFieldInfo(TiffTag.JPEGIFOFFSET, 1, 1, TiffType.LONG, FIELD_OJPEG_JPEGINTERCHANGEFORMAT, true, false, "JpegInterchangeFormat"),
            new TiffFieldInfo(TiffTag.JPEGIFBYTECOUNT, 1, 1, TiffType.LONG, FIELD_OJPEG_JPEGINTERCHANGEFORMATLENGTH, true, false, "JpegInterchangeFormatLength"),
            new TiffFieldInfo(TiffTag.JPEGQTABLES, -1, -1, TiffType.LONG, FIELD_OJPEG_JPEGQTABLES, false, true, "JpegQTables"),
            new TiffFieldInfo(TiffTag.JPEGDCTABLES, -1, -1, TiffType.LONG, FIELD_OJPEG_JPEGDCTABLES, false, true, "JpegDcTables"),
            new TiffFieldInfo(TiffTag.JPEGACTABLES, -1, -1, TiffType.LONG, FIELD_OJPEG_JPEGACTABLES, false, true, "JpegAcTables"),
            new TiffFieldInfo(TiffTag.JPEGPROC, 1, 1, TiffType.SHORT, FIELD_OJPEG_JPEGPROC, false, false, "JpegProc"),
            new TiffFieldInfo(TiffTag.JPEGRESTARTINTERVAL, 1, 1, TiffType.SHORT, FIELD_OJPEG_JPEGRESTARTINTERVAL, false, false, "JpegRestartInterval"),
        };

        private TiffTagMethods m_tagMethods;
        private TiffTagMethods m_parentTagMethods;

        internal OJPEGState sp;

        public OJpegCodec(Tiff tif, Compression scheme, string name)
            : base(tif, scheme, name)
        {
            m_tagMethods = new OJpegCodecTagMethods();
        }

        public override bool Init()
        {
            Debug.Assert(m_scheme == Compression.OJPEG);

            /*
             * Merge codec-specific tag information.
             */
            m_tif.MergeFieldInfo(ojpeg_field_info, ojpeg_field_info.Length);

            sp = new OJPEGState();
            sp.m_tif = m_tif;
            sp.m_jpeg_proc = 1;
            sp.m_subsampling_hor = 2;
            sp.m_subsampling_ver = 2;

            m_tif.SetField(TiffTag.YCBCRSUBSAMPLING, 2, 2);

            /* tif tag methods */
            m_parentTagMethods = m_tif.m_tagmethods;
            m_tif.m_tagmethods = m_tagMethods;

            /* Some OJPEG files don't have strip or tile offsets or bytecounts
             * tags. Some others do, but have totally meaningless or corrupt
             * values in these tags. In these cases, the JpegInterchangeFormat
             * stream is reliable. In any case, this decoder reads the
             * compressed data itself, from the most reliable locations, and
             * we need to notify encapsulating LibTiff not to read raw strips
             * or tiles for us.
             */
            m_tif.m_flags |= TiffFlags.NOREADRAW;
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether this codec can encode data.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this codec can encode data; otherwise, <c>false</c>.
        /// </value>
        public override bool CanEncode
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this codec can decode data.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this codec can decode data; otherwise, <c>false</c>.
        /// </value>
        public override bool CanDecode
        {
            get
            {
                return true;
            }
        }

        public Tiff GetTiff()
        {
            return m_tif;
        }

        /// <summary>
        /// Setups the decoder part of the codec.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this codec successfully setup its decoder part and can decode data;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// 	<b>SetupDecode</b> is called once before
        /// <see cref="PreDecode"/>.</remarks>
        public override bool SetupDecode()
        {
            return OJPEGSetupDecode();
        }

        /// <summary>
        /// Prepares the decoder part of the codec for a decoding.
        /// </summary>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if this codec successfully prepared its decoder part and ready
        /// to decode data; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// 	<b>PreDecode</b> is called after <see cref="SetupDecode"/> and before decoding.
        /// </remarks>
        public override bool PreDecode(short plane)
        {
            return OJPEGPreDecode(plane);
        }

        /// <summary>
        /// Decodes one row of image data.
        /// </summary>
        /// <param name="buffer">The buffer to place decoded image data to.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at
        /// which to begin storing decoded bytes.</param>
        /// <param name="count">The number of decoded bytes that should be placed
        /// to <paramref name="buffer"/></param>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if image data was decoded successfully; otherwise, <c>false</c>.
        /// </returns>
        public override bool DecodeRow(byte[] buffer, int offset, int count, short plane)
        {
            return OJPEGDecode(buffer, offset, count, plane);
        }

        /// <summary>
        /// Decodes one strip of image data.
        /// </summary>
        /// <param name="buffer">The buffer to place decoded image data to.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at
        /// which to begin storing decoded bytes.</param>
        /// <param name="count">The number of decoded bytes that should be placed
        /// to <paramref name="buffer"/></param>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if image data was decoded successfully; otherwise, <c>false</c>.
        /// </returns>
        public override bool DecodeStrip(byte[] buffer, int offset, int count, short plane)
        {
            return OJPEGDecode(buffer, offset, count, plane);
        }

        /// <summary>
        /// Decodes one tile of image data.
        /// </summary>
        /// <param name="buffer">The buffer to place decoded image data to.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at
        /// which to begin storing decoded bytes.</param>
        /// <param name="count">The number of decoded bytes that should be placed
        /// to <paramref name="buffer"/></param>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if image data was decoded successfully; otherwise, <c>false</c>.
        /// </returns>
        public override bool DecodeTile(byte[] buffer, int offset, int count, short plane)
        {
            return OJPEGDecode(buffer, offset, count, plane);
        }

        /// <summary>
        /// Setups the encoder part of the codec.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this codec successfully setup its encoder part and can encode data;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// 	<b>SetupEncode</b> is called once before
        /// <see cref="PreEncode"/>.</remarks>
        public override bool SetupEncode()
        {
            return OJpegEncodeIsUnsupported();
        }

        /// <summary>
        /// Prepares the encoder part of the codec for a encoding.
        /// </summary>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if this codec successfully prepared its encoder part and ready
        /// to encode data; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// 	<b>PreEncode</b> is called after <see cref="SetupEncode"/> and before encoding.
        /// </remarks>
        public override bool PreEncode(short plane)
        {
            return OJpegEncodeIsUnsupported();
        }

        /// <summary>
        /// Performs any actions after encoding required by the codec.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if all post-encode actions succeeded; otherwise, <c>false</c>
        /// </returns>
        /// <remarks>
        /// 	<b>PostEncode</b> is called after encoding and can be used to release any external
        /// resources needed during encoding.
        /// </remarks>
        public override bool PostEncode()
        {
            return OJpegEncodeIsUnsupported();
        }

        /// <summary>
        /// Encodes one row of image data.
        /// </summary>
        /// <param name="buffer">The buffer with image data to be encoded.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at
        /// which to begin read image data.</param>
        /// <param name="count">The maximum number of encoded bytes that can be placed
        /// to <paramref name="buffer"/></param>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if image data was encoded successfully; otherwise, <c>false</c>.
        /// </returns>
        public override bool EncodeRow(byte[] buffer, int offset, int count, short plane)
        {
            return OJpegEncodeIsUnsupported();
        }

        /// <summary>
        /// Encodes one strip of image data.
        /// </summary>
        /// <param name="buffer">The buffer with image data to be encoded.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at
        /// which to begin read image data.</param>
        /// <param name="count">The maximum number of encoded bytes that can be placed
        /// to <paramref name="buffer"/></param>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if image data was encoded successfully; otherwise, <c>false</c>.
        /// </returns>
        public override bool EncodeStrip(byte[] buffer, int offset, int count, short plane)
        {
            return OJpegEncodeIsUnsupported();
        }

        /// <summary>
        /// Encodes one tile of image data.
        /// </summary>
        /// <param name="buffer">The buffer with image data to be encoded.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at
        /// which to begin read image data.</param>
        /// <param name="count">The maximum number of encoded bytes that can be placed
        /// to <paramref name="buffer"/></param>
        /// <param name="plane">The zero-based sample plane index.</param>
        /// <returns>
        /// 	<c>true</c> if image data was encoded successfully; otherwise, <c>false</c>.
        /// </returns>
        public override bool EncodeTile(byte[] buffer, int offset, int count, short plane)
        {
            return OJpegEncodeIsUnsupported();
        }

        /// <summary>
        /// Cleanups the state of the codec.
        /// </summary>
        /// <remarks>
        /// 	<b>Cleanup</b> is called when codec is no longer needed (won't be used) and can be
        /// used for example to restore tag methods that were substituted.</remarks>
        public override void Cleanup()
        {
            OJPEGCleanup();
        }

        private bool OJPEGSetupDecode()
        {
            Tiff.WarningExt(m_tif.m_clientdata, "OJPEGSetupDecode",
                "Depreciated and troublesome old-style JPEG compression mode, please convert to new-style JPEG compression and notify vendor of writing software");

            return true;
        }

        private bool OJPEGPreDecode(short s)
        {
            uint m;
            if (sp.m_subsamplingcorrect_done == 0)
                OJPEGSubsamplingCorrect();

            if (sp.m_readheader_done == 0)
            {
                if (OJPEGReadHeaderInfo() == 0)
                    return false;
            }

            if (sp.m_sos_end[s].m_log == 0)
            {
                if (OJPEGReadSecondarySos(s) == 0)
                    return false;
            }

            if (m_tif.IsTiled())
                m = (uint)m_tif.m_curtile;
            else
                m = (uint)m_tif.m_curstrip;

            if ((sp.m_writeheader_done != 0) && ((sp.m_write_cursample != s) || (sp.m_write_curstrile > m)))
            {
                if (sp.m_libjpeg_session_active != 0)
                    OJPEGLibjpegSessionAbort();
                sp.m_writeheader_done = 0;
            }

            if (sp.m_writeheader_done == 0)
            {
                sp.m_plane_sample_offset = (byte)s;
                sp.m_write_cursample = s;
                sp.m_write_curstrile = (uint)(s * m_tif.m_dir.td_stripsperimage);
                if ((sp.m_in_buffer_file_pos_log == 0) ||
                    (sp.m_in_buffer_file_pos - sp.m_in_buffer_togo != sp.m_sos_end[s].m_in_buffer_file_pos))
                {
                    sp.m_in_buffer_source = sp.m_sos_end[s].m_in_buffer_source;
                    sp.m_in_buffer_next_strile = sp.m_sos_end[s].m_in_buffer_next_strile;
                    sp.m_in_buffer_file_pos = sp.m_sos_end[s].m_in_buffer_file_pos;
                    sp.m_in_buffer_file_pos_log = 0;
                    sp.m_in_buffer_file_togo = sp.m_sos_end[s].m_in_buffer_file_togo;
                    sp.m_in_buffer_togo = 0;
                    sp.m_in_buffer_cur = 0;
                }
                if (OJPEGWriteHeaderInfo() == 0)
                    return false;
            }

            while (sp.m_write_curstrile < m)
            {
                if (sp.m_libjpeg_jpeg_query_style == 0)
                {
                    if (OJPEGPreDecodeSkipRaw() == 0)
                        return false;
                }
                else
                {
                    if (OJPEGPreDecodeSkipScanlines() == 0)
                        return false;
                }
                sp.m_write_curstrile++;
            }

            return true;
        }

        private bool OJPEGDecode(byte[] buf, int offset, int cc, short s)
        {
            if (sp.m_libjpeg_jpeg_query_style == 0)
            {
                if (OJPEGDecodeRaw(buf, cc) == 0)
                    return false;
            }
            else
            {
                if (OJPEGDecodeScanlines(buf, cc) == 0)
                    return false;
            }
            return true;
        }

        //    tif.tif_postdecode=OJPEGPostDecode;

        //static void
        //OJPEGPostDecode(TIFF* tif, byte[] buf, int cc)
        //{
        //    OJPEGState* sp=(OJPEGState*)tif.tif_data;
        //    (void)buf;
        //    (void)cc;
        //    sp.write_curstrile++;
        //    if (sp.write_curstrile%tif.tif_dir.td_stripsperimage==0)
        //    {
        //        assert(sp.libjpeg_session_active!=0);
        //        OJPEGLibjpegSessionAbort(tif);
        //        sp.writeheader_done=0;
        //    }
        //}

        private bool OJpegEncodeIsUnsupported()
        {
            Tiff.ErrorExt(m_tif.m_clientdata, "OJPEGSetupEncode",
                "OJPEG encoding not supported; use new-style JPEG compression instead");

            return false;
        }

        private void OJPEGCleanup()
        {
            m_tif.m_tagmethods = m_parentTagMethods;
            if (sp.m_libjpeg_session_active != 0)
                OJPEGLibjpegSessionAbort();
        }

        private int OJPEGPreDecodeSkipRaw()
        {
            uint m;
            m = sp.m_lines_per_strile;
            if (sp.m_subsampling_convert_state != 0)
            {
                if (sp.m_subsampling_convert_clines - sp.m_subsampling_convert_state >= m)
                {
                    sp.m_subsampling_convert_state += m;
                    if (sp.m_subsampling_convert_state == sp.m_subsampling_convert_clines)
                        sp.m_subsampling_convert_state = 0;
                    return (1);
                }
                m -= sp.m_subsampling_convert_clines - sp.m_subsampling_convert_state;
                sp.m_subsampling_convert_state = 0;
            }
            while (m >= sp.m_subsampling_convert_clines)
            {
                if (jpeg_read_raw_data_encap(sp.m_subsampling_ver * 8) == 0)
                    return (0);
                m -= sp.m_subsampling_convert_clines;
            }
            if (m > 0)
            {
                if (jpeg_read_raw_data_encap(sp.m_subsampling_ver * 8) == 0)
                    return (0);
                sp.m_subsampling_convert_state = m;
            }
            return 1;
        }

        private int OJPEGPreDecodeSkipScanlines()
        {
            uint m;
            if (sp.m_skip_buffer == null)
                sp.m_skip_buffer = new byte[sp.m_bytes_per_line];

            for (m = 0; m < sp.m_lines_per_strile; m++)
            {
                if (jpeg_read_scanlines_encap(sp, sp.m_libjpeg_jpeg_decompress_struct, sp.m_skip_buffer, 1) == 0)
                    return 0;
            }
            return 1;
        }

        private int OJPEGDecodeRaw(byte[] buf, int cc)
        {
            const string module = "OJPEGDecodeRaw";

            if (cc % sp.m_bytes_per_line != 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Fractional scanline not read");
                return (0);
            }

            Debug.Assert(cc > 0);
            int m = 0; // offset
            int n = cc;
            do
            {
                if (sp.m_subsampling_convert_state == 0)
                {
                    if (jpeg_read_raw_data_encap(sp.m_subsampling_ver * 8) == 0)
                        return (0);
                }

                uint oy = sp.m_subsampling_convert_state * sp.m_subsampling_ver * sp.m_subsampling_convert_ylinelen;
                uint ocb = sp.m_subsampling_convert_state * sp.m_subsampling_convert_clinelen;
                uint ocr = sp.m_subsampling_convert_state * sp.m_subsampling_convert_clinelen;

                int i = 0;
                int ii = 0;
                int p = m;
                for (uint q = 0; q < sp.m_subsampling_convert_clinelenout; q++)
                {
                    uint r = oy;
                    for (byte sy = 0; sy < sp.m_subsampling_ver; sy++)
                    {
                        for (byte sx = 0; sx < sp.m_subsampling_hor; sx++)
                        {
                            i = (int)(r / sp.m_subsampling_convert_ylinelen);
                            ii = (int)(r % sp.m_subsampling_convert_ylinelen);
                            r++;
                            buf[p++] = sp.m_subsampling_convert_ybuf[i][ii];
                        }

                        r += sp.m_subsampling_convert_ylinelen - sp.m_subsampling_hor;
                    }
                    oy += sp.m_subsampling_hor;

                    i = (int)(ocb / sp.m_subsampling_convert_clinelen);
                    ii = (int)(ocb % sp.m_subsampling_convert_clinelen);
                    ocb++;
                    buf[p++] = sp.m_subsampling_convert_cbbuf[i][ii];

                    i = (int)(ocr / sp.m_subsampling_convert_clinelen);
                    ii = (int)(ocr % sp.m_subsampling_convert_clinelen);
                    ocr++;
                    buf[p++] = sp.m_subsampling_convert_crbuf[i][ii];
                }
                sp.m_subsampling_convert_state++;
                if (sp.m_subsampling_convert_state == sp.m_subsampling_convert_clines)
                    sp.m_subsampling_convert_state = 0;
                m += (int)sp.m_bytes_per_line;
                n -= (int)sp.m_bytes_per_line;
            } while (n > 0);
            return 1;
        }

        private int OJPEGDecodeScanlines(byte[] buf, int cc)
        {
            const string module = "OJPEGDecodeScanlines";

            if (cc % sp.m_bytes_per_line != 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Fractional scanline not read");
                return (0);
            }

            Debug.Assert(cc > 0);

            int m = 0;
            byte[] temp = new byte[sp.m_bytes_per_line];
            int n = cc;
            do
            {
                if (jpeg_read_scanlines_encap(sp, sp.m_libjpeg_jpeg_decompress_struct, temp, 1) == 0)
                    return (0);

                Buffer.BlockCopy(temp, 0, buf, m, temp.Length);
                m += (int)sp.m_bytes_per_line;
                n -= (int)sp.m_bytes_per_line;
            } while (n > 0);

            return 1;
        }

        public void OJPEGSubsamplingCorrect()
        {
            const string module = "OJPEGSubsamplingCorrect";
            byte mh;
            byte mv;
            Debug.Assert(sp.m_subsamplingcorrect_done == 0);

            if ((m_tif.m_dir.td_samplesperpixel != 3) || ((m_tif.m_dir.td_photometric != Photometric.YCBCR) &&
                (m_tif.m_dir.td_photometric != Photometric.ITULAB)))
            {
                if (sp.m_subsampling_tag != 0)
                {
                    Tiff.WarningExt(m_tif, m_tif.m_clientdata, module,
                        "Subsampling tag not appropriate for this Photometric and/or SamplesPerPixel");
                }

                sp.m_subsampling_hor = 1;
                sp.m_subsampling_ver = 1;
                sp.m_subsampling_force_desubsampling_inside_decompression = 0;
            }
            else
            {
                sp.m_subsamplingcorrect_done = 1;
                mh = sp.m_subsampling_hor;
                mv = sp.m_subsampling_ver;
                sp.m_subsamplingcorrect = 1;
                OJPEGReadHeaderInfoSec();
                if (sp.m_subsampling_force_desubsampling_inside_decompression != 0)
                {
                    sp.m_subsampling_hor = 1;
                    sp.m_subsampling_ver = 1;
                }
                sp.m_subsamplingcorrect = 0;

                if (((sp.m_subsampling_hor != mh) || (sp.m_subsampling_ver != mv)) && (sp.m_subsampling_force_desubsampling_inside_decompression == 0))
                {
                    if (sp.m_subsampling_tag == 0)
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module,
                            "Subsampling tag is not set, yet subsampling inside JPEG data [{0},{1}] does not match default values [2,2]; assuming subsampling inside JPEG data is correct",
                            sp.m_subsampling_hor, sp.m_subsampling_ver);
                    }
                    else
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module,
                            "Subsampling inside JPEG data [{0},{1}] does not match subsampling tag values [{2},{3}]; assuming subsampling inside JPEG data is correct",
                            sp.m_subsampling_hor, sp.m_subsampling_ver, mh, mv);
                    }
                }

                if (sp.m_subsampling_force_desubsampling_inside_decompression != 0)
                {
                    if (sp.m_subsampling_tag == 0)
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module,
                            "Subsampling tag is not set, yet subsampling inside JPEG data does not match default values [2,2] (nor any other values allowed in TIFF); assuming subsampling inside JPEG data is correct and desubsampling inside JPEG decompression");
                    }
                    else
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module,
                            "Subsampling inside JPEG data does not match subsampling tag values [{0},{1}] (nor any other values allowed in TIFF); assuming subsampling inside JPEG data is correct and desubsampling inside JPEG decompression",
                            mh, mv);
                    }
                }

                if (sp.m_subsampling_force_desubsampling_inside_decompression == 0)
                {
                    if (sp.m_subsampling_hor < sp.m_subsampling_ver)
                    {
                        Tiff.WarningExt(m_tif, m_tif.m_clientdata, module,
                            "Subsampling values [{0},{1}] are not allowed in TIFF",
                            sp.m_subsampling_hor, sp.m_subsampling_ver);
                    }
                }
            }

            sp.m_subsamplingcorrect_done = 1;
        }

        private int OJPEGReadHeaderInfo()
        {
            const string module = "OJPEGReadHeaderInfo";
            Debug.Assert(sp.m_readheader_done == 0);
            sp.m_image_width = (uint)m_tif.m_dir.td_imagewidth;
            sp.m_image_length = (uint)m_tif.m_dir.td_imagelength;
            if (m_tif.IsTiled())
            {
                sp.m_strile_width = (uint)m_tif.m_dir.td_tilewidth;
                sp.m_strile_length = (uint)m_tif.m_dir.td_tilelength;
                sp.m_strile_length_total = ((sp.m_image_length + sp.m_strile_length - 1) / sp.m_strile_length) * sp.m_strile_length;
            }
            else
            {
                sp.m_strile_width = sp.m_image_width;
                sp.m_strile_length = (uint)m_tif.m_dir.td_rowsperstrip;
                sp.m_strile_length_total = sp.m_image_length;
            }
            sp.m_samples_per_pixel = (byte)m_tif.m_dir.td_samplesperpixel;
            if (sp.m_samples_per_pixel == 1)
            {
                sp.m_plane_sample_offset = 0;
                sp.m_samples_per_pixel_per_plane = sp.m_samples_per_pixel;
                sp.m_subsampling_hor = 1;
                sp.m_subsampling_ver = 1;
            }
            else
            {
                if (sp.m_samples_per_pixel != 3)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module,
                        "SamplesPerPixel {0} not supported for this compression scheme",
                        sp.m_samples_per_pixel);
                    return 0;
                }

                sp.m_plane_sample_offset = 0;
                if (m_tif.m_dir.td_planarconfig == PlanarConfig.CONTIG)
                    sp.m_samples_per_pixel_per_plane = 3;
                else
                    sp.m_samples_per_pixel_per_plane = 1;
            }
            if (sp.m_strile_length < sp.m_image_length)
            {
                if (sp.m_strile_length % (sp.m_subsampling_ver * 8) != 0)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module,
                        "Incompatible vertical subsampling and image strip/tile length");
                    return 0;
                }
                sp.m_restart_interval = (ushort)(((sp.m_strile_width + sp.m_subsampling_hor * 8 - 1) / (sp.m_subsampling_hor * 8)) * (sp.m_strile_length / (sp.m_subsampling_ver * 8)));
            }

            if (OJPEGReadHeaderInfoSec() == 0)
                return 0;

            sp.m_sos_end[0].m_log = 1;
            sp.m_sos_end[0].m_in_buffer_source = sp.m_in_buffer_source;
            sp.m_sos_end[0].m_in_buffer_next_strile = sp.m_in_buffer_next_strile;
            sp.m_sos_end[0].m_in_buffer_file_pos = sp.m_in_buffer_file_pos - sp.m_in_buffer_togo;
            sp.m_sos_end[0].m_in_buffer_file_togo = sp.m_in_buffer_file_togo + sp.m_in_buffer_togo;
            sp.m_readheader_done = 1;
            return 1;
        }

        private int OJPEGReadSecondarySos(short s)
        {
            Debug.Assert(s > 0);
            Debug.Assert(s < 3);
            Debug.Assert(sp.m_sos_end[0].m_log != 0);
            Debug.Assert(sp.m_sos_end[s].m_log == 0);

            sp.m_plane_sample_offset = (byte)(s - 1);
            while (sp.m_sos_end[sp.m_plane_sample_offset].m_log == 0)
                sp.m_plane_sample_offset--;

            sp.m_in_buffer_source = sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_source;
            sp.m_in_buffer_next_strile = sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_next_strile;
            sp.m_in_buffer_file_pos = sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_file_pos;
            sp.m_in_buffer_file_pos_log = 0;
            sp.m_in_buffer_file_togo = sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_file_togo;
            sp.m_in_buffer_togo = 0;
            sp.m_in_buffer_cur = 0;

            while (sp.m_plane_sample_offset < s)
            {
                do
                {
                    byte m;
                    if (OJPEGReadByte(out m) == 0)
                        return 0;

                    if (m == 255)
                    {
                        do
                        {
                            if (OJPEGReadByte(out m) == 0)
                                return 0;

                            if (m != 255)
                                break;
                        } while (true);

                        if (m == (byte)JPEG_MARKER.SOS)
                            break;
                    }
                } while (true);

                sp.m_plane_sample_offset++;
                if (OJPEGReadHeaderInfoSecStreamSos() == 0)
                    return 0;

                sp.m_sos_end[sp.m_plane_sample_offset].m_log = 1;
                sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_source = sp.m_in_buffer_source;
                sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_next_strile = sp.m_in_buffer_next_strile;
                sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_file_pos = sp.m_in_buffer_file_pos - sp.m_in_buffer_togo;
                sp.m_sos_end[sp.m_plane_sample_offset].m_in_buffer_file_togo = sp.m_in_buffer_file_togo + sp.m_in_buffer_togo;
            }

            return 1;
        }

        private int OJPEGWriteHeaderInfo()
        {
            Debug.Assert(sp.m_libjpeg_session_active == 0);

            sp.m_out_state = OJPEGStateOutState.ososSoi;
            sp.m_restart_index = 0;

            sp.m_libjpeg_jpeg_error_mgr = new OJpegErrorManager(this);
            if (!jpeg_create_decompress_encap(sp))
                return 0;

            sp.m_libjpeg_session_active = 1;
            sp.m_libjpeg_jpeg_source_mgr = new OJpegSrcManager(this);
            sp.m_libjpeg_jpeg_decompress_struct.Src = sp.m_libjpeg_jpeg_source_mgr;

            if (jpeg_read_header_encap(sp, true) == ReadResult.JPEG_SUSPENDED)
                return 0;

            if ((sp.m_subsampling_force_desubsampling_inside_decompression == 0) && (sp.m_samples_per_pixel_per_plane > 1))
            {
                sp.m_libjpeg_jpeg_decompress_struct.Raw_data_out = true;
                //#if JPEG_LIB_VERSION >= 70
                //    sp.libjpeg_jpeg_decompress_struct.do_fancy_upsampling=FALSE;
                //#endif
                sp.m_libjpeg_jpeg_query_style = 0;
                if (sp.m_subsampling_convert_log == 0)
                {
                    //Debug.Assert(sp.subsampling_convert_ycbcrbuf == null);
                    Debug.Assert(sp.m_subsampling_convert_ybuf == null);
                    Debug.Assert(sp.m_subsampling_convert_cbbuf == null);
                    Debug.Assert(sp.m_subsampling_convert_crbuf == null);
                    Debug.Assert(sp.m_subsampling_convert_ycbcrimage == null);

                    sp.m_subsampling_convert_ylinelen = (uint)((sp.m_strile_width + sp.m_subsampling_hor * 8 - 1) / (sp.m_subsampling_hor * 8) * sp.m_subsampling_hor * 8);
                    sp.m_subsampling_convert_ylines = (uint)(sp.m_subsampling_ver * 8);
                    sp.m_subsampling_convert_clinelen = sp.m_subsampling_convert_ylinelen / sp.m_subsampling_hor;
                    sp.m_subsampling_convert_clines = 8;

                    sp.m_subsampling_convert_ybuf = new byte[sp.m_subsampling_convert_ylines][];
                    for (int i = 0; i < sp.m_subsampling_convert_ylines; i++)
                        sp.m_subsampling_convert_ybuf[i] = new byte[sp.m_subsampling_convert_ylinelen];

                    sp.m_subsampling_convert_cbbuf = new byte[sp.m_subsampling_convert_clines][];
                    sp.m_subsampling_convert_crbuf = new byte[sp.m_subsampling_convert_clines][];
                    for (int i = 0; i < sp.m_subsampling_convert_clines; i++)
                    {
                        sp.m_subsampling_convert_cbbuf[i] = new byte[sp.m_subsampling_convert_clinelen];
                        sp.m_subsampling_convert_crbuf[i] = new byte[sp.m_subsampling_convert_clinelen];
                    }

                    //sp.subsampling_convert_ybuflen = sp.subsampling_convert_ylinelen * sp.subsampling_convert_ylines;
                    //sp.subsampling_convert_cbuflen = sp.subsampling_convert_clinelen * sp.subsampling_convert_clines;
                    //sp.subsampling_convert_ycbcrbuflen = sp.subsampling_convert_ybuflen + 2 * sp.subsampling_convert_cbuflen;
                    //sp.subsampling_convert_ycbcrbuf = new byte[sp.subsampling_convert_ycbcrbuflen];

                    //sp.subsampling_convert_ybuf = sp.subsampling_convert_ycbcrbuf;
                    //sp.subsampling_convert_cbbuf = sp.subsampling_convert_ybuf + sp.subsampling_convert_ybuflen;
                    //sp.subsampling_convert_crbuf = sp.subsampling_convert_cbbuf + sp.subsampling_convert_cbuflen;
                    
                    //sp.subsampling_convert_ycbcrimagelen = 3 + sp.subsampling_convert_ylines + 2 * sp.subsampling_convert_clines;
                    //sp.subsampling_convert_ycbcrimage = new byte[sp.subsampling_convert_ycbcrimagelen][];
                    sp.m_subsampling_convert_ycbcrimage = new byte[3][][];

                    sp.m_subsampling_convert_ycbcrimage[0] = new byte[sp.m_subsampling_convert_ylines][];

                    //byte[][] m = sp.subsampling_convert_ycbcrimage;
                    //int mIndex = 3;

                    for (uint n = 0; n < sp.m_subsampling_convert_ylines; n++)
                        sp.m_subsampling_convert_ycbcrimage[0][n] = sp.m_subsampling_convert_ybuf[n];

                    sp.m_subsampling_convert_ycbcrimage[1] = new byte[sp.m_subsampling_convert_clines][];
                    for (uint n = 0; n < sp.m_subsampling_convert_clines; n++)
                        sp.m_subsampling_convert_ycbcrimage[1][n] = sp.m_subsampling_convert_cbbuf[n];

                    sp.m_subsampling_convert_ycbcrimage[2] = new byte[sp.m_subsampling_convert_clines][];
                    for (uint n = 0; n < sp.m_subsampling_convert_clines; n++)
                        sp.m_subsampling_convert_ycbcrimage[2][n] = sp.m_subsampling_convert_crbuf[n];

                    //m[0] = sp.subsampling_convert_ycbcrimage[3];
                    //m[1] = sp.subsampling_convert_ycbcrimage[3 + sp.subsampling_convert_ylines];
                    //m[2] = sp.subsampling_convert_ycbcrimage[3 + sp.subsampling_convert_ylines + sp.subsampling_convert_clines];

                    sp.m_subsampling_convert_clinelenout = ((sp.m_strile_width + sp.m_subsampling_hor - 1) / sp.m_subsampling_hor);
                    sp.m_subsampling_convert_state = 0;
                    sp.m_bytes_per_line = (uint)(sp.m_subsampling_convert_clinelenout * (sp.m_subsampling_ver * sp.m_subsampling_hor + 2));
                    sp.m_lines_per_strile = ((sp.m_strile_length + sp.m_subsampling_ver - 1) / sp.m_subsampling_ver);
                    sp.m_subsampling_convert_log = 1;
                }
            }
            else
            {
                sp.m_libjpeg_jpeg_decompress_struct.Jpeg_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                sp.m_libjpeg_jpeg_decompress_struct.Out_color_space = J_COLOR_SPACE.JCS_UNKNOWN;
                sp.m_libjpeg_jpeg_query_style = 1;
                sp.m_bytes_per_line = sp.m_samples_per_pixel_per_plane * sp.m_strile_width;
                sp.m_lines_per_strile = sp.m_strile_length;
            }

            if (!jpeg_start_decompress_encap(sp))
                return 0;

            sp.m_writeheader_done = 1;
            return 1;
        }

        private void OJPEGLibjpegSessionAbort()
        {
            Debug.Assert(sp.m_libjpeg_session_active != 0);
            sp.m_libjpeg_jpeg_decompress_struct.jpeg_destroy();
            sp.m_libjpeg_session_active = 0;
        }

        private int OJPEGReadHeaderInfoSec()
        {
            const string module = "OJPEGReadHeaderInfoSec";
            byte m;
            ushort n;
            byte o;
            if (sp.m_file_size == 0)
                sp.m_file_size = (uint)m_tif.GetStream().Size(m_tif.m_clientdata);

            if (sp.m_jpeg_interchange_format != 0)
            {
                if (sp.m_jpeg_interchange_format >= sp.m_file_size)
                {
                    sp.m_jpeg_interchange_format = 0;
                    sp.m_jpeg_interchange_format_length = 0;
                }
                else
                {
                    if ((sp.m_jpeg_interchange_format_length == 0) || (sp.m_jpeg_interchange_format + sp.m_jpeg_interchange_format_length > sp.m_file_size))
                        sp.m_jpeg_interchange_format_length = sp.m_file_size - sp.m_jpeg_interchange_format;
                }
            }

            sp.m_in_buffer_source = OJPEGStateInBufferSource.osibsNotSetYet;
            sp.m_in_buffer_next_strile = 0;
            sp.m_in_buffer_strile_count = (uint)m_tif.m_dir.td_nstrips;
            sp.m_in_buffer_file_togo = 0;
            sp.m_in_buffer_togo = 0;

            do
            {
                if (OJPEGReadBytePeek(out m) == 0)
                    return 0;

                if (m != 255)
                    break;

                OJPEGReadByteAdvance();
                do
                {
                    if (OJPEGReadByte(out m) == 0)
                        return 0;
                } while (m == 255);

                switch ((JPEG_MARKER)m)
                {
                    case JPEG_MARKER.SOI:
                        /* this type of marker has no data, and should be skipped */
                        break;
                    case JPEG_MARKER.COM:
                    case JPEG_MARKER.APP0:
                    case JPEG_MARKER.APP1:
                    case JPEG_MARKER.APP2:
                    case JPEG_MARKER.APP3:
                    case JPEG_MARKER.APP4:
                    case JPEG_MARKER.APP5:
                    case JPEG_MARKER.APP6:
                    case JPEG_MARKER.APP7:
                    case JPEG_MARKER.APP8:
                    case JPEG_MARKER.APP9:
                    case JPEG_MARKER.APP10:
                    case JPEG_MARKER.APP11:
                    case JPEG_MARKER.APP12:
                    case JPEG_MARKER.APP13:
                    case JPEG_MARKER.APP14:
                    case JPEG_MARKER.APP15:
                        /* this type of marker has data, but it has no use to us (and no place here) and should be skipped */
                        if (OJPEGReadWord(out n) == 0)
                            return (0);
                        if (n < 2)
                        {
                            if (sp.m_subsamplingcorrect == 0)
                                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt JPEG data");
                            return (0);
                        }
                        if (n > 2)
                            OJPEGReadSkip((ushort)(n - 2));
                        break;
                    case JPEG_MARKER.DRI:
                        if (OJPEGReadHeaderInfoSecStreamDri() == 0)
                            return (0);
                        break;
                    case JPEG_MARKER.DQT:
                        if (OJPEGReadHeaderInfoSecStreamDqt() == 0)
                            return (0);
                        break;
                    case JPEG_MARKER.DHT:
                        if (OJPEGReadHeaderInfoSecStreamDht() == 0)
                            return (0);
                        break;
                    case JPEG_MARKER.SOF0:
                    case JPEG_MARKER.SOF1:
                    case JPEG_MARKER.SOF3:
                        if (OJPEGReadHeaderInfoSecStreamSof(m) == 0)
                            return (0);
                        if (sp.m_subsamplingcorrect != 0)
                            return (1);
                        break;
                    case JPEG_MARKER.SOS:
                        if (sp.m_subsamplingcorrect != 0)
                            return (1);
                        Debug.Assert(sp.m_plane_sample_offset == 0);
                        if (OJPEGReadHeaderInfoSecStreamSos() == 0)
                            return (0);
                        break;
                    default:
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Unknown marker type {0} in JPEG data", m);
                        return (0);
                }
            } while (m != (byte)JPEG_MARKER.SOS);

            if (sp.m_subsamplingcorrect != 0)
                return 1;

            if (sp.m_sof_log == 0)
            {
                if (OJPEGReadHeaderInfoSecTablesQTable() == 0)
                    return (0);

                sp.m_sof_marker_id = (byte)JPEG_MARKER.SOF0;
                for (o = 0; o < sp.m_samples_per_pixel; o++)
                    sp.m_sof_c[o] = o;

                sp.m_sof_hv[0] = (byte)((sp.m_subsampling_hor << 4) | sp.m_subsampling_ver);
                for (o = 1; o < sp.m_samples_per_pixel; o++)
                    sp.m_sof_hv[o] = 17;

                sp.m_sof_x = sp.m_strile_width;
                sp.m_sof_y = sp.m_strile_length_total;
                sp.m_sof_log = 1;

                if (OJPEGReadHeaderInfoSecTablesDcTable() == 0)
                    return (0);

                if (OJPEGReadHeaderInfoSecTablesAcTable() == 0)
                    return (0);

                for (o = 1; o < sp.m_samples_per_pixel; o++)
                    sp.m_sos_cs[o] = o;
            }

            return 1;
        }

        private int OJPEGReadHeaderInfoSecStreamDri()
        {
            // this could easilly cause trouble in some cases...
            // but no such cases have occured so far
            const string module = "OJPEGReadHeaderInfoSecStreamDri";
            ushort m;
            if (OJPEGReadWord(out m) == 0)
                return 0;

            if (m != 4)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DRI marker in JPEG data");
                return (0);
            }

            if (OJPEGReadWord(out m) == 0)
                return 0;

            sp.m_restart_interval = m;
            return 1;
        }

        private int OJPEGReadHeaderInfoSecStreamDqt()
        {
            // this is a table marker, and it is to be saved as a whole for
            // exact pushing on the jpeg stream later on
            const string module = "OJPEGReadHeaderInfoSecStreamDqt";
            ushort m;
            uint na;
            byte[] nb;
            byte o;
            if (OJPEGReadWord(out m) == 0)
                return (0);

            if (m <= 2)
            {
                if (sp.m_subsamplingcorrect == 0)
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DQT marker in JPEG data");
                return (0);
            }

            if (sp.m_subsamplingcorrect != 0)
            {
                OJPEGReadSkip((ushort)(m - 2));
            }
            else
            {
                m -= 2;
                do
                {
                    if (m < 65)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DQT marker in JPEG data");
                        return (0);
                    }

                    na = 69;
                    nb = new byte[na];
                    nb[0] = 255;
                    nb[1] = (byte)JPEG_MARKER.DQT;
                    nb[2] = 0;
                    nb[3] = 67;
                    if (OJPEGReadBlock(65, nb, 4) == 0)
                        return (0);

                    o = (byte)(nb[4] & 15);
                    if (3 < o)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DQT marker in JPEG data");
                        return (0);
                    }

                    sp.m_qtable[o] = nb;
                    m -= 65;
                } while (m > 0);
            }
            return (1);
        }

        private int OJPEGReadHeaderInfoSecStreamDht()
        {
            // this is a table marker, and it is to be saved as a whole for
            // exact pushing on the jpeg stream later on
            // TODO: the following assumes there is only one table in
            // this marker... but i'm not quite sure that assumption is
            // guaranteed correct
            const string module = "OJPEGReadHeaderInfoSecStreamDht";
            ushort m;
            uint na;
            byte[] nb;
            byte o;
            if (OJPEGReadWord(out m) == 0)
                return (0);
            if (m <= 2)
            {
                if (sp.m_subsamplingcorrect == 0)
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DHT marker in JPEG data");
                return (0);
            }
            if (sp.m_subsamplingcorrect != 0)
            {
                OJPEGReadSkip((ushort)(m - 2));
            }
            else
            {
                na = (uint)(2 + m);
                nb = new byte[na];
                nb[0] = 255;
                nb[1] = (byte)JPEG_MARKER.DHT;
                nb[2] = (byte)(m >> 8);
                nb[3] = (byte)(m & 255);
                if (OJPEGReadBlock((ushort)(m - 2), nb, 4) == 0)
                    return (0);
                o = nb[4];
                if ((o & 240) == 0)
                {
                    if (3 < o)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DHT marker in JPEG data");
                        return (0);
                    }
                    sp.m_dctable[o] = nb;
                }
                else
                {
                    if ((o & 240) != 16)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DHT marker in JPEG data");
                        return (0);
                    }
                    o &= 15;
                    if (3 < o)
                    {
                        Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt DHT marker in JPEG data");
                        return (0);
                    }
                    sp.m_actable[o] = nb;
                }
            }
            return (1);
        }

        private int OJPEGReadHeaderInfoSecStreamSof(byte marker_id)
        {
            /* this marker needs to be checked, and part of its data needs to be saved for regeneration later on */
            const string module = "OJPEGReadHeaderInfoSecStreamSof";
            ushort m;
            ushort n;
            byte o;
            ushort p;
            ushort q;
            if (sp.m_sof_log != 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt JPEG data");
                return (0);
            }
            if (sp.m_subsamplingcorrect == 0)
                sp.m_sof_marker_id = marker_id;
            /* Lf: data length */
            if (OJPEGReadWord(out m) == 0)
                return (0);
            if (m < 11)
            {
                if (sp.m_subsamplingcorrect == 0)
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt SOF marker in JPEG data");
                return (0);
            }
            m -= 8;
            if (m % 3 != 0)
            {
                if (sp.m_subsamplingcorrect == 0)
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt SOF marker in JPEG data");
                return (0);
            }
            n = (ushort)(m / 3);
            if (sp.m_subsamplingcorrect == 0)
            {
                if (n != sp.m_samples_per_pixel)
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG compressed data indicates unexpected number of samples");
                    return (0);
                }
            }
            /* P: Sample precision */
            if (OJPEGReadByte(out o) == 0)
                return (0);
            if (o != 8)
            {
                if (sp.m_subsamplingcorrect == 0)
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG compressed data indicates unexpected number of bits per sample");
                return (0);
            }
            /* Y: Number of lines, X: Number of samples per line */
            if (sp.m_subsamplingcorrect != 0)
                OJPEGReadSkip(4);
            else
            {
                /* TODO: probably best to also add check on allowed upper bound, especially x, may cause buffer overflow otherwise i think */
                /* Y: Number of lines */
                if (OJPEGReadWord(out p) == 0)
                    return (0);
                if ((p < sp.m_image_length) && (p < sp.m_strile_length_total))
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG compressed data indicates unexpected height");
                    return (0);
                }
                sp.m_sof_y = p;
                /* X: Number of samples per line */
                if (OJPEGReadWord(out p) == 0)
                    return (0);
                if ((p < sp.m_image_width) && (p < sp.m_strile_width))
                {
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG compressed data indicates unexpected width");
                    return (0);
                }
                sp.m_sof_x = p;
            }
            /* Nf: Number of image components in frame */
            if (OJPEGReadByte(out o) == 0)
                return (0);
            if (o != n)
            {
                if (sp.m_subsamplingcorrect == 0)
                    Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt SOF marker in JPEG data");
                return (0);
            }
            /* per component stuff */
            /* TODO: double-check that flow implies that n cannot be as big as to make us overflow sof_c, sof_hv and sof_tq arrays */
            for (q = 0; q < n; q++)
            {
                /* C: Component identifier */
                if (OJPEGReadByte(out o) == 0)
                    return (0);
                if (sp.m_subsamplingcorrect == 0)
                    sp.m_sof_c[q] = o;
                /* H: Horizontal sampling factor, and V: Vertical sampling factor */
                if (OJPEGReadByte(out o) == 0)
                    return (0);
                if (sp.m_subsamplingcorrect != 0)
                {
                    if (q == 0)
                    {
                        sp.m_subsampling_hor = (byte)(o >> 4);
                        sp.m_subsampling_ver = (byte)(o & 15);
                        if (((sp.m_subsampling_hor != 1) && (sp.m_subsampling_hor != 2) && (sp.m_subsampling_hor != 4)) ||
                            ((sp.m_subsampling_ver != 1) && (sp.m_subsampling_ver != 2) && (sp.m_subsampling_ver != 4)))
                            sp.m_subsampling_force_desubsampling_inside_decompression = 1;
                    }
                    else
                    {
                        if (o != 17)
                            sp.m_subsampling_force_desubsampling_inside_decompression = 1;
                    }
                }
                else
                {
                    sp.m_sof_hv[q] = o;
                    if (sp.m_subsampling_force_desubsampling_inside_decompression == 0)
                    {
                        if (q == 0)
                        {
                            if (o != ((sp.m_subsampling_hor << 4) | sp.m_subsampling_ver))
                            {
                                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG compressed data indicates unexpected subsampling values");
                                return (0);
                            }
                        }
                        else
                        {
                            if (o != 17)
                            {
                                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "JPEG compressed data indicates unexpected subsampling values");
                                return (0);
                            }
                        }
                    }
                }
                /* Tq: Quantization table destination selector */
                if (OJPEGReadByte(out o) == 0)
                    return (0);
                if (sp.m_subsamplingcorrect == 0)
                    sp.m_sof_tq[q] = o;
            }
            if (sp.m_subsamplingcorrect == 0)
                sp.m_sof_log = 1;
            return (1);
        }

        private int OJPEGReadHeaderInfoSecStreamSos()
        {
            /* this marker needs to be checked, and part of its data needs to be saved for regeneration later on */
            const string module = "OJPEGReadHeaderInfoSecStreamSos";
            ushort m;
            byte n;
            byte o;
            Debug.Assert(sp.m_subsamplingcorrect == 0);
            if (sp.m_sof_log == 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt SOS marker in JPEG data");
                return (0);
            }
            /* Ls */
            if (OJPEGReadWord(out m) == 0)
                return (0);
            if (m != 6 + sp.m_samples_per_pixel_per_plane * 2)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt SOS marker in JPEG data");
                return (0);
            }
            /* Ns */
            if (OJPEGReadByte(out n) == 0)
                return (0);
            if (n != sp.m_samples_per_pixel_per_plane)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt SOS marker in JPEG data");
                return (0);
            }
            /* Cs, Td, and Ta */
            for (o = 0; o < sp.m_samples_per_pixel_per_plane; o++)
            {
                /* Cs */
                if (OJPEGReadByte(out n) == 0)
                    return (0);
                sp.m_sos_cs[sp.m_plane_sample_offset + o] = n;
                /* Td and Ta */
                if (OJPEGReadByte(out n) == 0)
                    return (0);
                sp.m_sos_tda[sp.m_plane_sample_offset + o] = n;
            }
            /* skip Ss, Se, Ah, en Al -> no check, as per Tom Lane recommendation, as per LibJpeg source */
            OJPEGReadSkip(3);
            return 1;
        }

        private int OJPEGReadHeaderInfoSecTablesQTable()
        {
            const string module = "OJPEGReadHeaderInfoSecTablesQTable";
            byte m;
            byte n;
            uint oa;
            byte[] ob;
            uint p;
            if (sp.m_qtable_offset[0] == 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Missing JPEG tables");
                return (0);
            }
            sp.m_in_buffer_file_pos_log = 0;
            for (m = 0; m < sp.m_samples_per_pixel; m++)
            {
                if ((sp.m_qtable_offset[m] != 0) && ((m == 0) || (sp.m_qtable_offset[m] != sp.m_qtable_offset[m - 1])))
                {
                    for (n = 0; n < m - 1; n++)
                    {
                        if (sp.m_qtable_offset[m] == sp.m_qtable_offset[n])
                        {
                            Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt JpegQTables tag value");
                            return (0);
                        }
                    }
                    oa = 69;
                    ob = new byte[oa];
                    ob[0] = 255;
                    ob[1] = (byte)JPEG_MARKER.DQT;
                    ob[2] = 0;
                    ob[3] = 67;
                    ob[4] = m;
                    TiffStream stream = m_tif.GetStream();
                    stream.Seek(m_tif.m_clientdata, sp.m_qtable_offset[m], SeekOrigin.Begin);
                    p = (uint)stream.Read(m_tif.m_clientdata, ob, 5, 64);
                    if (p != 64)
                        return (0);
                    sp.m_qtable[m] = ob;
                    sp.m_sof_tq[m] = m;
                }
                else
                    sp.m_sof_tq[m] = sp.m_sof_tq[m - 1];
            }
            return (1);
        }

        private int OJPEGReadHeaderInfoSecTablesDcTable()
        {
            const string module = "OJPEGReadHeaderInfoSecTablesDcTable";
            byte m;
            byte n;
            byte[] o = new byte[16];
            uint p;
            uint q;
            uint ra;
            byte[] rb;
            if (sp.m_dctable_offset[0] == 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Missing JPEG tables");
                return (0);
            }
            sp.m_in_buffer_file_pos_log = 0;
            for (m = 0; m < sp.m_samples_per_pixel; m++)
            {
                if ((sp.m_dctable_offset[m] != 0) && ((m == 0) || (sp.m_dctable_offset[m] != sp.m_dctable_offset[m - 1])))
                {
                    for (n = 0; n < m - 1; n++)
                    {
                        if (sp.m_dctable_offset[m] == sp.m_dctable_offset[n])
                        {
                            Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt JpegDcTables tag value");
                            return (0);
                        }
                    }

                    TiffStream stream = m_tif.GetStream();
                    stream.Seek(m_tif.m_clientdata, sp.m_dctable_offset[m], SeekOrigin.Begin);
                    p = (uint)stream.Read(m_tif.m_clientdata, o, 0, 16);
                    if (p != 16)
                        return (0);
                    q = 0;
                    for (n = 0; n < 16; n++)
                        q += o[n];
                    ra = 21 + q;
                    rb = new byte[ra];
                    rb[0] = 255;
                    rb[1] = (byte)JPEG_MARKER.DHT;
                    rb[2] = (byte)((19 + q) >> 8);
                    rb[3] = (byte)((19 + q) & 255);
                    rb[4] = m;
                    for (n = 0; n < 16; n++)
                        rb[5 + n] = o[n];

                    p = (uint)stream.Read(m_tif.m_clientdata, rb, 21, (int)q);
                    if (p != q)
                        return (0);
                    sp.m_dctable[m] = rb;
                    sp.m_sos_tda[m] = (byte)(m << 4);
                }
                else
                    sp.m_sos_tda[m] = sp.m_sos_tda[m - 1];
            }
            return (1);
        }

        private int OJPEGReadHeaderInfoSecTablesAcTable()
        {
            const string module = "OJPEGReadHeaderInfoSecTablesAcTable";
            byte m;
            byte n;
            byte[] o = new byte[16];
            uint p;
            uint q;
            uint ra;
            byte[] rb;
            if (sp.m_actable_offset[0] == 0)
            {
                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Missing JPEG tables");
                return (0);
            }
            sp.m_in_buffer_file_pos_log = 0;
            for (m = 0; m < sp.m_samples_per_pixel; m++)
            {
                if ((sp.m_actable_offset[m] != 0) && ((m == 0) || (sp.m_actable_offset[m] != sp.m_actable_offset[m - 1])))
                {
                    for (n = 0; n < m - 1; n++)
                    {
                        if (sp.m_actable_offset[m] == sp.m_actable_offset[n])
                        {
                            Tiff.ErrorExt(m_tif, m_tif.m_clientdata, module, "Corrupt JpegAcTables tag value");
                            return (0);
                        }
                    }
                    TiffStream stream = m_tif.GetStream();
                    stream.Seek(m_tif.m_clientdata, sp.m_actable_offset[m], SeekOrigin.Begin);
                    p = (uint)stream.Read(m_tif.m_clientdata, o, 0, 16);
                    if (p != 16)
                        return (0);
                    q = 0;
                    for (n = 0; n < 16; n++)
                        q += o[n];
                    ra = 21 + q;
                    rb = new byte[ra];
                    rb[0] = 255;
                    rb[1] = (byte)JPEG_MARKER.DHT;
                    rb[2] = (byte)((19 + q) >> 8);
                    rb[3] = (byte)((19 + q) & 255);
                    rb[4] = (byte)(16 | m);
                    for (n = 0; n < 16; n++)
                        rb[5 + n] = o[n];

                    p = (uint)stream.Read(m_tif.m_clientdata, rb, 21, (int)q);
                    if (p != q)
                        return (0);
                    sp.m_actable[m] = rb;
                    sp.m_sos_tda[m] = (byte)(sp.m_sos_tda[m] | m);
                }
                else
                    sp.m_sos_tda[m] = (byte)(sp.m_sos_tda[m] | (sp.m_sos_tda[m - 1] & 15));
            }
            return (1);
        }

        private int OJPEGReadBufferFill()
        {
            ushort m;
            int n;
            /* TODO: double-check: when subsamplingcorrect is set, no call to TIFFErrorExt or TIFFWarningExt should be made
             * in any other case, seek or read errors should be passed through */
            do
            {
                if (sp.m_in_buffer_file_togo != 0)
                {
                    TiffStream stream = m_tif.GetStream();
                    if (sp.m_in_buffer_file_pos_log == 0)
                    {
                        stream.Seek(m_tif.m_clientdata, sp.m_in_buffer_file_pos, SeekOrigin.Begin);
                        sp.m_in_buffer_file_pos_log = 1;
                    }
                    m = OJPEGState.OJPEG_BUFFER;
                    if (m > sp.m_in_buffer_file_togo)
                        m = (ushort)sp.m_in_buffer_file_togo;

                    n = stream.Read(m_tif.m_clientdata, sp.m_in_buffer, 0, (int)m);
                    if (n == 0)
                        return (0);
                    Debug.Assert(n > 0);
                    Debug.Assert(n <= OJPEGState.OJPEG_BUFFER);
                    Debug.Assert(n < 65536);
                    Debug.Assert((ushort)n <= sp.m_in_buffer_file_togo);
                    m = (ushort)n;
                    sp.m_in_buffer_togo = m;
                    sp.m_in_buffer_cur = 0;
                    sp.m_in_buffer_file_togo -= m;
                    sp.m_in_buffer_file_pos += m;
                    break;
                }
                sp.m_in_buffer_file_pos_log = 0;
                switch (sp.m_in_buffer_source)
                {
                    case OJPEGStateInBufferSource.osibsNotSetYet:
                        if (sp.m_jpeg_interchange_format != 0)
                        {
                            sp.m_in_buffer_file_pos = sp.m_jpeg_interchange_format;
                            sp.m_in_buffer_file_togo = sp.m_jpeg_interchange_format_length;
                        }
                        sp.m_in_buffer_source = OJPEGStateInBufferSource.osibsJpegInterchangeFormat;
                        break;
                    case OJPEGStateInBufferSource.osibsJpegInterchangeFormat:
                        sp.m_in_buffer_source = OJPEGStateInBufferSource.osibsStrile;
                        goto case OJPEGStateInBufferSource.osibsStrile;
                    case OJPEGStateInBufferSource.osibsStrile:
                        if (sp.m_in_buffer_next_strile == sp.m_in_buffer_strile_count)
                            sp.m_in_buffer_source = OJPEGStateInBufferSource.osibsEof;
                        else
                        {
                            if (m_tif.m_dir.td_stripoffset == null)
                            {
                                Tiff.ErrorExt(m_tif, m_tif.m_clientdata, m_tif.m_name, "Strip offsets are missing");
                                return (0);
                            }
                            sp.m_in_buffer_file_pos = m_tif.m_dir.td_stripoffset[sp.m_in_buffer_next_strile];
                            if (sp.m_in_buffer_file_pos != 0)
                            {
                                if (sp.m_in_buffer_file_pos >= sp.m_file_size)
                                    sp.m_in_buffer_file_pos = 0;
                                else
                                {
                                    sp.m_in_buffer_file_togo = m_tif.m_dir.td_stripbytecount[sp.m_in_buffer_next_strile];
                                    if (sp.m_in_buffer_file_togo == 0)
                                        sp.m_in_buffer_file_pos = 0;
                                    else if (sp.m_in_buffer_file_pos + sp.m_in_buffer_file_togo > sp.m_file_size)
                                        sp.m_in_buffer_file_togo = sp.m_file_size - sp.m_in_buffer_file_pos;
                                }
                            }
                            sp.m_in_buffer_next_strile++;
                        }
                        break;
                    default:
                        return (0);
                }
            } while (true);
            return 1;
        }

        private int OJPEGReadByte(out byte b)
        {
            if (sp.m_in_buffer_togo == 0)
            {
                if (OJPEGReadBufferFill() == 0)
                {
                    b = 0;
                    return 0;
                }

                Debug.Assert(sp.m_in_buffer_togo > 0);
            }

            b = sp.m_in_buffer[sp.m_in_buffer_cur];
            sp.m_in_buffer_cur++;
            sp.m_in_buffer_togo--;
            return 1;
        }

        public int OJPEGReadBytePeek(out byte b)
        {
            if (sp.m_in_buffer_togo == 0)
            {
                if (OJPEGReadBufferFill() == 0)
                {
                    b = 0;
                    return 0;
                }

                Debug.Assert(sp.m_in_buffer_togo > 0);
            }

            b = sp.m_in_buffer[sp.m_in_buffer_cur];
            return 1;
        }

        private void OJPEGReadByteAdvance()
        {
            Debug.Assert(sp.m_in_buffer_togo > 0);
            sp.m_in_buffer_cur++;
            sp.m_in_buffer_togo--;
        }

        private int OJPEGReadWord(out ushort word)
        {
            word = 0;
            byte m;
            if (OJPEGReadByte(out m) == 0)
                return 0;

            word = (ushort)(m << 8);
            if (OJPEGReadByte(out m) == 0)
                return 0;

            word |= m;
            return 1;
        }

        public int OJPEGReadBlock(ushort len, byte[] mem, int offset)
        {
            ushort mlen;
            ushort n;
            Debug.Assert(len > 0);
            mlen = len;
            int mmem = offset;
            do
            {
                if (sp.m_in_buffer_togo == 0)
                {
                    if (OJPEGReadBufferFill() == 0)
                        return (0);
                    Debug.Assert(sp.m_in_buffer_togo > 0);
                }
                n = mlen;
                if (n > sp.m_in_buffer_togo)
                    n = sp.m_in_buffer_togo;

                Buffer.BlockCopy(sp.m_in_buffer, sp.m_in_buffer_cur, mem, mmem, n);
                sp.m_in_buffer_cur += n;
                sp.m_in_buffer_togo -= n;
                mlen -= n;
                mmem += n;
            } while (mlen > 0);
            return (1);
        }

        private void OJPEGReadSkip(ushort len)
        {
            ushort m;
            ushort n;
            m = len;
            n = m;
            if (n > sp.m_in_buffer_togo)
                n = sp.m_in_buffer_togo;
            sp.m_in_buffer_cur += n;
            sp.m_in_buffer_togo -= n;
            m -= n;
            if (m > 0)
            {
                Debug.Assert(sp.m_in_buffer_togo == 0);
                n = m;
                if (n > sp.m_in_buffer_file_togo)
                    n = (ushort)sp.m_in_buffer_file_togo;
                sp.m_in_buffer_file_pos += n;
                sp.m_in_buffer_file_togo -= n;
                sp.m_in_buffer_file_pos_log = 0;
                /* we don't skip past jpeginterchangeformat/strile block...
                 * if that is asked from us, we're dealing with totally bazurk
                 * data anyway, and we've not seen this happening on any
                 * testfile, so we might as well likely cause some other
                 * meaningless error to be passed at some later time
                 */
            }
        }

        internal int OJPEGWriteStream(out byte[] mem, out uint len)
        {
            mem = null;
            len = 0;
            do
            {
                Debug.Assert(sp.m_out_state <= OJPEGStateOutState.ososEoi);
                switch (sp.m_out_state)
                {
                    case OJPEGStateOutState.ososSoi:
                        OJPEGWriteStreamSoi(out mem, out len);
                        break;
                    case OJPEGStateOutState.ososQTable0:
                        OJPEGWriteStreamQTable(0, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososQTable1:
                        OJPEGWriteStreamQTable(1, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososQTable2:
                        OJPEGWriteStreamQTable(2, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososQTable3:
                        OJPEGWriteStreamQTable(3, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososDcTable0:
                        OJPEGWriteStreamDcTable(0, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososDcTable1:
                        OJPEGWriteStreamDcTable(1, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososDcTable2:
                        OJPEGWriteStreamDcTable(2, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososDcTable3:
                        OJPEGWriteStreamDcTable(3, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososAcTable0:
                        OJPEGWriteStreamAcTable(0, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososAcTable1:
                        OJPEGWriteStreamAcTable(1, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososAcTable2:
                        OJPEGWriteStreamAcTable(2, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososAcTable3:
                        OJPEGWriteStreamAcTable(3, out mem, out len);
                        break;
                    case OJPEGStateOutState.ososDri:
                        OJPEGWriteStreamDri(out mem, out len);
                        break;
                    case OJPEGStateOutState.ososSof:
                        OJPEGWriteStreamSof(out mem, out len);
                        break;
                    case OJPEGStateOutState.ososSos:
                        OJPEGWriteStreamSos(out mem, out len);
                        break;
                    case OJPEGStateOutState.ososCompressed:
                        if (OJPEGWriteStreamCompressed(out mem, out len) == 0)
                            return (0);
                        break;
                    case OJPEGStateOutState.ososRst:
                        OJPEGWriteStreamRst(out mem, out len);
                        break;
                    case OJPEGStateOutState.ososEoi:
                        OJPEGWriteStreamEoi(out mem, out len);
                        break;
                }
            } while (len == 0);
            return (1);
        }

        private void OJPEGWriteStreamSoi(out byte[] mem, out uint len)
        {
            Debug.Assert(OJPEGState.OJPEG_BUFFER >= 2);
            sp.m_out_buffer[0] = 255;
            sp.m_out_buffer[1] = (byte)JPEG_MARKER.SOI;
            len = 2;
            mem = sp.m_out_buffer;
            sp.m_out_state++;
        }

        private void OJPEGWriteStreamQTable(byte table_index, out byte[] mem, out uint len)
        {
            mem = null;
            len = 0;

            if (sp.m_qtable[table_index] != null)
            {
                mem = sp.m_qtable[table_index];
                len = (uint)sp.m_qtable[table_index].Length;
            }
            sp.m_out_state++;
        }

        private void OJPEGWriteStreamDcTable(byte table_index, out byte[] mem, out uint len)
        {
            mem = null;
            len = 0;

            if (sp.m_dctable[table_index] != null)
            {
                mem = sp.m_dctable[table_index];
                len = (uint)sp.m_dctable[table_index].Length;
            }
            sp.m_out_state++;
        }

        private void OJPEGWriteStreamAcTable(byte table_index, out byte[] mem, out uint len)
        {
            mem = null;
            len = 0;

            if (sp.m_actable[table_index] != null)
            {
                mem = sp.m_actable[table_index];
                len = (uint)sp.m_actable[table_index].Length;
            }
            sp.m_out_state++;
        }

        private void OJPEGWriteStreamDri(out byte[] mem, out uint len)
        {
            Debug.Assert(OJPEGState.OJPEG_BUFFER >= 6);
            mem = null;
            len = 0;

            if (sp.m_restart_interval != 0)
            {
                sp.m_out_buffer[0] = 255;
                sp.m_out_buffer[1] = (byte)JPEG_MARKER.DRI;
                sp.m_out_buffer[2] = 0;
                sp.m_out_buffer[3] = 4;
                sp.m_out_buffer[4] = (byte)(sp.m_restart_interval >> 8);
                sp.m_out_buffer[5] = (byte)(sp.m_restart_interval & 255);
                len = 6;
                mem = sp.m_out_buffer;
            }
            sp.m_out_state++;
        }

        private void OJPEGWriteStreamSof(out byte[] mem, out uint len)
        {
            byte m;
            Debug.Assert(OJPEGState.OJPEG_BUFFER >= 2 + 8 + sp.m_samples_per_pixel_per_plane * 3);
            Debug.Assert(255 >= 8 + sp.m_samples_per_pixel_per_plane * 3);
            sp.m_out_buffer[0] = 255;
            sp.m_out_buffer[1] = sp.m_sof_marker_id;
            /* Lf */
            sp.m_out_buffer[2] = 0;
            sp.m_out_buffer[3] = (byte)(8 + sp.m_samples_per_pixel_per_plane * 3);
            /* P */
            sp.m_out_buffer[4] = 8;
            /* Y */
            sp.m_out_buffer[5] = (byte)(sp.m_sof_y >> 8);
            sp.m_out_buffer[6] = (byte)(sp.m_sof_y & 255);
            /* X */
            sp.m_out_buffer[7] = (byte)(sp.m_sof_x >> 8);
            sp.m_out_buffer[8] = (byte)(sp.m_sof_x & 255);
            /* Nf */
            sp.m_out_buffer[9] = sp.m_samples_per_pixel_per_plane;
            for (m = 0; m < sp.m_samples_per_pixel_per_plane; m++)
            {
                /* C */
                sp.m_out_buffer[10 + m * 3] = sp.m_sof_c[sp.m_plane_sample_offset + m];
                /* H and V */
                sp.m_out_buffer[10 + m * 3 + 1] = sp.m_sof_hv[sp.m_plane_sample_offset + m];
                /* Tq */
                sp.m_out_buffer[10 + m * 3 + 2] = sp.m_sof_tq[sp.m_plane_sample_offset + m];
            }
            len = (uint)(10 + sp.m_samples_per_pixel_per_plane * 3);
            mem = sp.m_out_buffer;
            sp.m_out_state++;
        }

        private void OJPEGWriteStreamSos(out byte[] mem, out uint len)
        {
            byte m;
            Debug.Assert(OJPEGState.OJPEG_BUFFER >= 2 + 6 + sp.m_samples_per_pixel_per_plane * 2);
            Debug.Assert(255 >= 6 + sp.m_samples_per_pixel_per_plane * 2);
            sp.m_out_buffer[0] = 255;
            sp.m_out_buffer[1] = (byte)JPEG_MARKER.SOS;
            /* Ls */
            sp.m_out_buffer[2] = 0;
            sp.m_out_buffer[3] = (byte)(6 + sp.m_samples_per_pixel_per_plane * 2);
            /* Ns */
            sp.m_out_buffer[4] = sp.m_samples_per_pixel_per_plane;
            for (m = 0; m < sp.m_samples_per_pixel_per_plane; m++)
            {
                /* Cs */
                sp.m_out_buffer[5 + m * 2] = sp.m_sos_cs[sp.m_plane_sample_offset + m];
                /* Td and Ta */
                sp.m_out_buffer[5 + m * 2 + 1] = sp.m_sos_tda[sp.m_plane_sample_offset + m];
            }
            /* Ss */
            sp.m_out_buffer[5 + sp.m_samples_per_pixel_per_plane * 2] = 0;
            /* Se */
            sp.m_out_buffer[5 + sp.m_samples_per_pixel_per_plane * 2 + 1] = 63;
            /* Ah and Al */
            sp.m_out_buffer[5 + sp.m_samples_per_pixel_per_plane * 2 + 2] = 0;
            len = (uint)(8 + sp.m_samples_per_pixel_per_plane * 2);
            mem = sp.m_out_buffer;
            sp.m_out_state++;
        }

        private int OJPEGWriteStreamCompressed(out byte[] mem, out uint len)
        {
            mem = null;
            len = 0;

            if (sp.m_in_buffer_togo == 0)
            {
                if (OJPEGReadBufferFill() == 0)
                    return (0);
                Debug.Assert(sp.m_in_buffer_togo > 0);
            }
            len = sp.m_in_buffer_togo;

            if (sp.m_in_buffer_cur == 0)
            {
                mem = sp.m_in_buffer;
            }
            else
            {
                mem = new byte[len];
                Buffer.BlockCopy(sp.m_in_buffer, sp.m_in_buffer_cur, mem, 0, (int)len);
            }

            sp.m_in_buffer_togo = 0;
            if (sp.m_in_buffer_file_togo == 0)
            {
                switch (sp.m_in_buffer_source)
                {
                    case OJPEGStateInBufferSource.osibsStrile:
                        if (sp.m_in_buffer_next_strile < sp.m_in_buffer_strile_count)
                            sp.m_out_state = OJPEGStateOutState.ososRst;
                        else
                            sp.m_out_state = OJPEGStateOutState.ososEoi;
                        break;
                    case OJPEGStateInBufferSource.osibsEof:
                        sp.m_out_state = OJPEGStateOutState.ososEoi;
                        break;
                    default:
                        break;
                }
            }
            return (1);
        }

        private void OJPEGWriteStreamRst(out byte[] mem, out uint len)
        {
            Debug.Assert(OJPEGState.OJPEG_BUFFER >= 2);
            sp.m_out_buffer[0] = 255;
            sp.m_out_buffer[1] = (byte)((byte)JPEG_MARKER.RST0 + sp.m_restart_index);
            sp.m_restart_index++;
            if (sp.m_restart_index == 8)
                sp.m_restart_index = 0;
            len = 2;
            mem = sp.m_out_buffer;
            sp.m_out_state = OJPEGStateOutState.ososCompressed;
        }

        private void OJPEGWriteStreamEoi(out byte[] mem, out uint len)
        {
            Debug.Assert(OJPEGState.OJPEG_BUFFER >= 2);
            sp.m_out_buffer[0] = 255;
            sp.m_out_buffer[1] = (byte)JPEG_MARKER.EOI;
            len = 2;
            mem = sp.m_out_buffer;
        }

        private bool jpeg_create_decompress_encap(OJPEGState sp)
        {
            try
            {
                sp.m_libjpeg_jpeg_decompress_struct = new jpeg_decompress_struct(sp.m_libjpeg_jpeg_error_mgr);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private ReadResult jpeg_read_header_encap(OJPEGState sp, bool require_image)
        {
            ReadResult res = ReadResult.JPEG_SUSPENDED;
            try
            {
                res = sp.m_libjpeg_jpeg_decompress_struct.jpeg_read_header(require_image);
            }
            catch (Exception)
            {
                return ReadResult.JPEG_SUSPENDED;
            }

            return res;
        }

        private bool jpeg_start_decompress_encap(OJPEGState sp)
        {
            try
            {
                sp.m_libjpeg_jpeg_decompress_struct.jpeg_start_decompress();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private int jpeg_read_scanlines_encap(OJPEGState sp, jpeg_decompress_struct cinfo, byte[] scanlines, int max_lines)
        {
            int n = 0;
            try
            {
                byte[][] temp = new byte[1][];
                temp[0] = scanlines;
                n = cinfo.jpeg_read_scanlines(temp, max_lines);
            }
            catch (Exception)
            {
                return 0;
            }

            return n;
        }

        private int jpeg_read_raw_data_encap(int max_lines)
        {
            jpeg_decompress_struct cinfo = sp.m_libjpeg_jpeg_decompress_struct;
            //byte[][] data = sp.subsampling_convert_ycbcrimage;

            int n = 0;
            try
            {
                //byte[][][] temp = new byte[3][][];
                //temp[0] = data;
                //n = cinfo.jpeg_read_raw_data(temp, max_lines);
                n = cinfo.jpeg_read_raw_data(sp.m_subsampling_convert_ycbcrimage, max_lines);
            }
            catch (Exception)
            {
                return 0;
            }

            return n;
        }

        //#ifndef LIBJPEG_ENCAP_EXTERNAL
        //static void
        //jpeg_encap_unwind(TIFF* tif)
        //{
        //    OJPEGState* sp=(OJPEGState*)tif.tif_data;
        //    LONGJMP(sp.exit_jmpbuf,1);
        //}
        //#endif
    }
}
