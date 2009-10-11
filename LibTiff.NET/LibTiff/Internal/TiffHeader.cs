using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Internal
{
    struct TiffHeader
    {
        public const int TIFF_MAGIC_SIZE = 2;
        public const int TIFF_VERSION_SIZE = 2;
        public const int TIFF_DIROFFSET_SIZE = 4;

        public UInt16 tiff_magic; /* magic number (defines byte order) */
        public UInt16 tiff_version; /* TIFF version number */
        public uint tiff_diroff; /* byte offset to first directory */
    }
}
