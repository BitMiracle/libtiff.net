using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class AddCustomTagsToExistingTiff
    {
        private const TiffTag TIFFTAG_GDAL_METADATA = (TiffTag)42112;

        private static Tiff.TiffExtendProc m_parentExtender;

        public static void TagExtender(Tiff tif)
        {
            TiffFieldInfo[] tiffFieldInfo = 
            {
                new TiffFieldInfo(TIFFTAG_GDAL_METADATA, -1, -1, TiffType.ASCII,
                    FieldBit.Custom, true, false, "GDALMetadata"),
            };

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);

            if (m_parentExtender != null)
                m_parentExtender(tif);
        }

        public static void Main()
        {
            // Register the extender callback
            // It's a good idea to keep track of the previous tag extender (if any) so that we can call it
            // from our extender allowing a chain of customizations to take effect.
            m_parentExtender = Tiff.SetTagExtender(TagExtender);

            File.Copy(@"Sample Data\dummy.tif", @"Sample Data\ToBeModifed.tif", true);

            string existingTiffName = @"Sample Data\ToBeModifed.tif";
            using (Tiff image = Tiff.Open(existingTiffName, "a"))
            {
                // we should rewind to first directory (first image) because of append mode
                image.SetDirectory(0);

                // set the custom tag 
                string value = "<GDALMetadata>\n<Item name=\"IMG_GUID\">" + 
                    "817C0168-0688-45CD-B799-CF8C4DE9AB2B</Item>\n<Item" + 
                    " name=\"LAYER_TYPE\" sample=\"0\">athematic</Item>\n</GDALMetadata>";
                image.SetField(TIFFTAG_GDAL_METADATA, value);

                // rewrites directory saving new tag
                image.CheckpointDirectory();
            }

            // restore previous tag extender
            Tiff.SetTagExtender(m_parentExtender);
        }
    }
}
