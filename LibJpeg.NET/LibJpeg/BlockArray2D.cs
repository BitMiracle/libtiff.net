using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using LibJpeg.Classic;

namespace LibJpeg
{
    //public class Block
    //{
    //    private JBLOCK m_block;

    //    internal Block(JBLOCK block)
    //    {
    //        Debug.Assert(block != null);
    //        m_block = block;
    //    }

    //    public short this[int i]
    //    {
    //        get
    //        {
    //            return m_block[i];
    //        }
    //        set
    //        {
    //            m_block[i] = value;
    //        }
    //    }
    //}

    public class BlockArray2D
    {
        private Classic.jvirt_barray_control m_array;

        internal BlockArray2D(Classic.jvirt_barray_control arr)
        {
            Debug.Assert(arr != null);
            m_array = arr;
        }

        /// <summary>
        /// Access the part of a virtual block array starting at startRow
        /// and extending for numRows rows.
        /// </summary>
        public JBLOCK[][] this[int startRow, int numRows]
        {
            get
            {
                return m_array.access_virt_barray((uint)startRow, (uint)numRows);
            }
            
        }
    }
}
