using System.Collections;

namespace BitMiracle.LibTiff.Internal
{
    public class TagCompare : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            TiffFieldInfo ta = x as TiffFieldInfo;
            TiffFieldInfo tb = y as TiffFieldInfo;

            /* NB: be careful of return values for 16-bit platforms */
            if (ta.field_tag != tb.field_tag)
                return ((int)ta.field_tag - (int)tb.field_tag);

            return (ta.field_type == TiffDataType.TIFF_ANY) ? 0 : ((int)tb.field_type - (int)ta.field_type);
        }
    }
}
