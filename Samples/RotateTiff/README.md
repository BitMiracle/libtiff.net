This sample shows how to rotate a TIFF image by 90, 180 and 270 degrees. Rotated images are to be properly displayed by any TIFF viewer.

To rotate an image one might just set the value of TiffTag.ORIENTATION tag. Unfortunately, not all of TIFF viewers respect the value of
this tag when displaying an image. In fact, most of the viewers don't respect the value of orientation tag.

This sample shows how to rotate image data in a viewer-agnostic way.