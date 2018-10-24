This sample shows how to read image scanlines in a random fashion.

To read scanlines of an image you may use one of Tiff.ReadScanline methods, but there is a catch. Images
compressed using LZW or PackBits compression scheme don't allow accessing scanlines in a random fashion. 

This sample shows how to avoid this limitation and access any scanline you want.