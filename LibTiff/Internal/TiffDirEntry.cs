using System.Globalization;

namespace BitMiracle.LibTiff.Classic.Internal
{
    /// <summary>
    /// TIFF Image File Directories are comprised of a table of field
    /// descriptors of the form shown below.  The table is sorted in
    /// ascending order by tag.  The values associated with each entry are
    /// disjoint and may appear anywhere in the file (so long as they are
    /// placed on a word boundary).
    /// 
    /// If the value is 4 bytes or less, then it is placed in the offset
    /// field to save space.  If the value is less than 4 bytes, it is
    /// left-justified in the offset field.
    /// </summary>
    class TiffDirEntry
    {
        public TiffTag tdir_tag;
        public TiffType tdir_type;

        /// <summary>
        /// number of items; length in spec
        /// </summary>
        public int tdir_count;

        /// <summary>
        /// byte offset to field data
        /// </summary>
        public ulong tdir_offset;

        public new string ToString()
        {
            return tdir_tag.ToString() + ", " + tdir_type.ToString() + " " +
                tdir_offset.ToString(CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// size in bytes of the entry depending on the current format
        /// </summary>
        /// <param name="isBigTiff">if set to <c>true</c> then the bigtiff size will be returned.</param>
        /// <returns></returns>
        public static int SizeInBytes(bool isBigTiff)
        {
            if (isBigTiff) return 20;
            else return 12;
        }
    }
}
