This sample shows how to extract EXIF metadata from a TIFF file.

EXIF tags are stored in EXIF IFD (information directory). This sample shows how to get offset to that directory and read it. When EXIF IFD is read all EXIF tags can be retrieved using Tiff.GetField method with appropriate tag identifiers. EXIF tag identifiers start with EXIF_ (i.e. TiffTag.EXIF_*).
