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

namespace BitMiracle.LibTiff
{
    public partial class Tiff
    {
        /* is tag value normal or pseudo */
        private static bool isPseudoTag(uint t)
        {
            return (t > 0xffff);
        }

        private bool isFillOrder(UInt16 o)
        {
            return ((m_flags & o) != 0);
        }

        private static uint BITn(int n)
        {
            return (((unsigned int)1L) << (n & 0x1f));
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
        private bool okToChangeTag(uint tag)
        {
            const TiffFieldInfo* fip = FindFieldInfo(tag, TIFF_ANY);
            if (fip == null)
            {
                /* unknown tag */
                Tiff::ErrorExt(this, m_clientdata, "SetField", "%s: Unknown %stag %u", m_name, isPseudoTag(tag) ? "pseudo-" : "", tag);
                return false;
            }

            if (tag != TIFFTAG_IMAGELENGTH && (m_flags & TIFF_BEENWRITING) != 0 && !fip.field_oktochange)
            {
                /*
                 * Consult info table to see if tag can be changed
                 * after we've started writing.  We only allow changes
                 * to those tags that don't/shouldn't affect the
                 * compression and/or format of the data.
                 */
                Tiff::ErrorExt(this, m_clientdata, "SetField", "%s: Cannot modify tag \"%s\" while writing", m_name, fip.field_name);
                return false;
            }

            return true;
        }

        /*
        * Setup a default directory structure.
        */
        private void setupDefaultDirectory()
        {
            size_t tiffFieldInfoCount;
            const TiffFieldInfo* tiffFieldInfo = getFieldInfo(tiffFieldInfoCount);
            setupFieldInfo(tiffFieldInfo, tiffFieldInfoCount);

            m_dir = new TiffDirectory();
            m_postDecodeMethod = Tiff::pdmNone;
            m_foundfield = null;
            
            m_tagmethods = m_defaultTagMethods;

            /*
             *  Give client code a chance to install their own
             *  tag extensions & methods, prior to compression overloads.
             */
            if (m_extender)
                (*m_extender)(this);
            
            SetField(TIFFTAG_COMPRESSION, COMPRESSION_NONE);
            
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

        private bool advanceDirectory(ref uint nextdir, out uint off)
        {
            static const char module[] = "advanceDirectory";
            UInt16 dircount;
            
            if (!seekOK(nextdir) || !readUInt16OK(dircount))
            {
                Tiff::ErrorExt(this, m_clientdata, module, "%s: Error fetching directory count", m_name);
                return false;
            }

            if ((m_flags & TIFF_SWAB) != 0)
                Tiff::SwabShort(dircount);

            if (off != null)
                off = seekFile(dircount * sizeof(TiffDirEntry), SEEK_CUR);
            else
                seekFile(dircount * sizeof(TiffDirEntry), SEEK_CUR);

            if (!readUInt32OK(nextdir))
            {
                Tiff::ErrorExt(this, m_clientdata, module, "%s: Error fetching directory link", m_name);
                return false;
            }

            if ((m_flags & TIFF_SWAB) != 0)
                Tiff::SwabLong(nextdir);
            
            return true;
        }

        internal static void setString(out string cpp, string cp)
        {
            delete cpp;

            size_t sz = strlen(cp) + 1;
            cpp = new char[sz];

            strcpy(cpp, cp);
            cpp[sz - 1] = 0;
        }

        internal static void setShortArray(out UInt16[] wpp, UInt16[] wp, uint n)
        {
            delete wpp;
            wpp = new UInt16[n];

            for (uint i = 0; i < n; i++)
                wpp[i] = wp[i];
        }

        internal static void setLongArray(out uint[] lpp, uint[] lp, uint n)
        {
            delete lpp;
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
