using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibJpeg.Classic
{
#if EXPOSE_LIBJPEG
    public
#endif
    enum DensityUnit
    {
        Unknown = 0, /* Unknown */
        DotsInch = 1, /* dots/inch */
        DotsCm = 2 /* dots/cm */
    }
}
