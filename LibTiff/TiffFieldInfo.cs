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

using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Field info.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffFieldInfo
    {
        /// <summary>
        /// marker for variable length tags
        /// </summary>
        public const short Variable = -1;

        /// <summary>
        /// marker for SamplesPerPixel tags
        /// </summary>
        public const short Spp = -2;

        /// <summary>
        /// marker for int var-length tags
        /// </summary>
        public const short Variable2 = -3;

        private TiffTag m_tag;
        private short m_readCount;
        private short m_writeCount;
        private TiffType m_type;
        private short m_bit;
        private bool m_okToChange;
        private bool m_passCount;
        private string m_name;

        /// <summary>
        /// Initializes a new instance of the <see cref="TiffFieldInfo"/> class.
        /// </summary>
        /// <param name="tag">The tag to describe.</param>
        /// <param name="readCount">The number of value elements to read when
        /// reading field information or one of TiffFieldInfo.Variable,
        /// TiffFieldInfo.Spp and TiffFieldInfo.Variable2</param>
        /// <param name="writeCount">The number of value elements to write when
        /// writing field information or one of TiffFieldInfo.Variable,
        /// TiffFieldInfo.Spp and TiffFieldInfo.Variable2</param>
        /// <param name="type">The type of the value elements.</param>
        /// <param name="bit">Index of the bit to use in "Set Fields Vector"
        /// when this instance is merged into field info collection. Take a look
        /// at <see cref="FieldBit"/> class.</param>
        /// <param name="okToChange">If true, then it is permissible to set the
        /// tag's value even after writing has commenced.</param>
        /// <param name="passCount">If true, then number of value elements
        /// should be passed to SetField method as second parameter (right
        /// after tag type AND before value itself).</param>
        /// <param name="name">The name (description) of the tag this
        /// instance describes.</param>
        public TiffFieldInfo(TiffTag tag, short readCount, short writeCount,
            TiffType type, short bit, bool okToChange, bool passCount, string name)
        {
            m_tag = tag;
            m_readCount = readCount;
            m_writeCount = writeCount;
            m_type = type;
            m_bit = bit;
            m_okToChange = okToChange;
            m_passCount = passCount;
            m_name = name;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (m_bit != FieldBit.Custom || m_name.Length == 0)
                return m_tag.ToString();

            return m_name;
        }

        /// <summary>
        /// The tag described by this instance.
        /// </summary>
        public TiffTag Tag
        {
            get { return m_tag; }
        }

        /// <summary>
        /// Number of value elements to read when reading field information or
        /// one of TiffFieldInfo.Variable, TiffFieldInfo.Spp and
        /// TiffFieldInfo.Variable2
        /// </summary>
        public short ReadCount
        {
            get { return m_readCount; }
        }

        /// <summary>
        /// Number of value elements to write when writing field information or
        /// one of TiffFieldInfo.Variable, TiffFieldInfo.Spp and
        /// TiffFieldInfo.Variable2
        /// </summary>
        public short WriteCount
        {
            get { return m_writeCount; }
        }

        /// <summary>
        /// Type of the value elements.
        /// </summary>
        public TiffType Type
        {
            get { return m_type; }
        }

        /// <summary>
        /// Index of the bit to use in "Set Fields Vector" when this instance
        /// is merged into field info collection. Take a look at FieldBit class.
        /// </summary>
        public short Bit
        {
            get { return m_bit; }
        }

        /// <summary>
        /// If true, then it is permissible to set the tag's value even after
        /// writing has commenced.
        /// </summary>
        public bool OkToChange
        {
            get { return m_okToChange; }
        }

        /// <summary>
        /// If true, then number of value elements should be passed to
        /// SetField method as second parameter (right after tag type AND before
        /// value itself).
        /// </summary>
        public bool PassCount
        {
            get { return m_passCount; }
        }

        /// <summary>
        /// The name (or description) of the tag this instance describes.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            internal set { m_name = value; }
        }
    }
}
