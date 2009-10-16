using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    /// <summary>
    /// Alternate source manager for reading from JPEGTables.
    /// We can share all the code except for the init routine.
    /// </summary>
    class JpegTablesSource : JpegStdSource
    {
        public JpegTablesSource(JpegCodec sp)
            : base(sp)
        {

        }

        public override void init_source()
        {
            initInternalBuffer(m_sp.m_jpegtables, m_sp.m_jpegtables_length);
        }
    }
}
