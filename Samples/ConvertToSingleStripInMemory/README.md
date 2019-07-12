This sample shows how to convert any non-tiled TIFF image to the TIFF image which have all data written in a single strip using custom TiffStream and System.IO.MemoryStream.

Input image is read from byte buffer (via custom TiffStream) and output image is written in System.IO.MemoryStream.