using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class PDFRect
    {
        public float left;
	    public float top;
	    public float right;
	    public float bottom;

        public PDFRect()
        {
            left = 0;
	        bottom = 0;
	        right = 0;
	        top = 0;
        }

        public PDFRect(float left, float top, float right, float bottom)
        {
            this.left = left;
	        this.bottom = bottom;
	        this.right = right;
	        this.top = top;
        }

        public float Height()
        {
            return Math.Abs(bottom - top);
        }

	    public float Width()
        {
            return Math.Abs(right - left);
        }

        public bool IsRectEmpty()
        {
            if (Width() < 1 || Height() < 1)
                return true;

            return false;
        }
    }
}
