using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    /*---------------------------------------------------------------------------*/
    /*------ field names for pdf dictionaries -----------------------------------*/
    class Field
    {
        //Common
        public static string Type = "Type";
        public static string Subtype = "Subtype";

        //Fonts
        public static string Font = "Font";
        public static string BaseFont = "BaseFont";
        public static string FontDescriptor = "FontDescriptor";
        public static string ToUnicode = "ToUnicode";

        //Simple fonts
        public static string FirstChar = "FirstChar";
        public static string LastChar = "LastChar";
        public static string Widths = "Widths";

        //CID Fonts
        public static string CIDSystemInfo = "CIDSystemInfo";
        public static string DW = "DW";
        public static string W = "W";
        public static string CIDToGIDMap = "CIDToGIDMap";

        //Encoding
        public static string Encoding = "Encoding";
        public static string BaseEncoding = "BaseEncoding";
        public static string Differences = "Differences";

        //ColorSpaces
        public static string ColorSpace = "ColorSpace";
        public static string DeviceGray = "DeviceGray";
        public static string DeviceRGB = "DeviceRGB";
        public static string DeviceCMYK = "DeviceCMYK";
        public static string ICCBased = "ICCBased";

        //XObjects
        public static string XObject = "XObject";

        //Document Info
        public static string Author = "Author";
        public static string Creator = "Creator";
        public static string Keywords = "Keywords";
        public static string Producer = "Producer";
        public static string Subject = "Subject";
        public static string Title = "Title";
        public static string CreationDate = "CreationDate";
        public static string BitsPerComponent = "BitsPerComponent";
        public static string Predictor = "Predictor";
        public static string Columns = "Columns";
        public static string Colors = "Colors";
        public static string Intent = "Intent";
    }
}
