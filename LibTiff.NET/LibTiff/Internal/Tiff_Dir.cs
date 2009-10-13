/* Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

/*
 * Directory Tag Get & Set Routines.
 * (and also some miscellaneous stuff)
 */

using System;
using System.Collections.Generic;
using System.Text;

using BitMiracle.LibTiff.Internal;

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        /* is tag value normal or pseudo */
        private static bool isPseudoTag(TIFFTAG t)
        {
            return ((int)t > 0xffff);
        }

        private bool isFillOrder(UInt16 o)
        {
            return ((m_flags & o) != 0);
        }

        private static uint BITn(int n)
        {
            return (((uint)1L) << (n & 0x1f));
        }

        /*
        * Return true / false according to whether or not
        * it is permissible to set the tag's value.
        * Note that we allow ImageLength to be changed
        * so that we can append and extend to images.
        * Any other tag may not be altered once writing
        * has commenced, unless its value has no effect
        * on the format of the data that is written.
        */
        private bool okToChangeTag(TIFFTAG tag)
        {
            TiffFieldInfo fip = FindFieldInfo(tag, TiffDataType.TIFF_ANY);
            if (fip == null)
            {
                /* unknown tag */
                ErrorExt(this, m_clientdata, "SetField", "%s: Unknown %stag %u", m_name, isPseudoTag(tag) ? "pseudo-" : "", tag);
                return false;
            }

            if (tag != TIFFTAG.TIFFTAG_IMAGELENGTH && (m_flags & TIFF_BEENWRITING) != 0 && !fip.field_oktochange)
            {
                /*
                 * Consult info table to see if tag can be changed
                 * after we've started writing.  We only allow changes
                 * to those tags that don't/shouldn't affect the
                 * compression and/or format of the data.
                 */
                ErrorExt(this, m_clientdata, "SetField", "%s: Cannot modify tag \"%s\" while writing", m_name, fip.field_name);
                return false;
            }

            return true;
        }

        /*
        * Setup a default directory structure.
        */
        private void setupDefaultDirectory()
        {
            int tiffFieldInfoCount;
            TiffFieldInfo[] tiffFieldInfo = getFieldInfo(out tiffFieldInfoCount);
            setupFieldInfo(tiffFieldInfo, tiffFieldInfoCount);

            m_dir = new TiffDirectory();
            m_postDecodeMethod = PostDecodeMethodType.pdmNone;
            m_foundfield = null;
            
            m_tagmethods = m_defaultTagMethods;

            /*
             *  Give client code a chance to install their own
             *  tag extensions & methods, prior to compression overloads.
             */
            //if (m_extender != null)
            //    (*m_extender)(this);
            
            SetField(TIFFTAG.TIFFTAG_COMPRESSION, COMPRESSION.COMPRESSION_NONE);
            
            /*
             * NB: The directory is marked dirty as a result of setting
             * up the default compression scheme.  However, this really
             * isn't correct -- we want TIFF_DIRTYDIRECT to be set only
             * if the user does something.  We could just do the setup
             * by hand, but it seems better to use the normal mechanism
             * (i.e. SetField).
             */
            m_flags &= ~TIFF_DIRTYDIRECT;

            /*
             * we clear the ISTILED flag when setting up a new directory.
             * Should we also be clearing stuff like INSUBIFD?
             */
            m_flags &= ~TIFF_ISTILED;
        }

        private bool advanceDirectory(ref int nextdir, out uint off)
        {
            off = 0;

            const string module = "advanceDirectory";
            UInt16 dircount;
            
            if (!seekOK(nextdir) || !readUInt16OK(out dircount))
            {
                ErrorExt(this, m_clientdata, module, "%s: Error fetching directory count", m_name);
                return false;
            }

            if ((m_flags & TIFF_SWAB) != 0)
                SwabShort(ref dircount);

            off = seekFile(dircount * TiffDirEntry.SizeInBytes, SEEK_CUR);

            if (!readIntOK(out nextdir))
            {
                ErrorExt(this, m_clientdata, module, "%s: Error fetching directory link", m_name);
                return false;
            }

            if ((m_flags & TIFF_SWAB) != 0)
                SwabLong(ref nextdir);
            
            return true;
        }

        internal static void setString(out string cpp, string cp)
        {
            cpp = cp.Clone() as string;
        }

        internal static void setShortArray(out UInt16[] wpp, UInt16[] wp, uint n)
        {
            wpp = new UInt16[n];
            for (uint i = 0; i < n; i++)
                wpp[i] = wp[i];
        }

        internal static void setLongArray(out uint[] lpp, uint[] lp, uint n)
        {
            lpp = new uint[n];
            for (uint i = 0; i < n; i++)
                lpp[i] = lp[i];
        }

        internal bool fieldSet(int field)
        {
            return ((m_dir.td_fieldsset[field / 32] & BITn(field)) != 0);
        }

        internal void setFieldBit(int field)
        {
            m_dir.td_fieldsset[field / 32] |= BITn(field);
        }

        internal void clearFieldBit(int field)
        {
            m_dir.td_fieldsset[field / 32] &= ~BITn(field);
        }
    }
}
