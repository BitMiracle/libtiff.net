This sample shows how to create simple black and white TIFF image.

To create a TIFF file you should create Tiff object using Tiff.Open method, then set necessary properties like width, height, resolution, etc. and then fill image contents using one of the Tiff.Write* methods.

This sample shows how to create bitonal image with randomly generated samples (pixels) using Tiff.WriteScanline method.