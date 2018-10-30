Tiff2Rgba converts a TIFF image to RGBA color space. 

Usage
-----

`Tiff2Rgba [ options ] input.tif output.tif`

Description
-----------

Tiff2Rgba converts a wide variety of TIFF images into an RGBA TIFF image. This includes the ability to translate different color spaces and photometric interpretation into RGBA, support for alpha blending, and translation of many different bit depths into a 32bit RGBA image. 

Internally this program is implemented using the <xref:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage*> method, and it suffers any limitations of that method. This includes limited support for > 8 BitsPerSample images, and flaws with some esoteric combinations of BitsPerSample, photometric interpretation, block organization and planar configuration. 

The generated images are stripped images with four samples per pixel (red, green, blue and alpha) or if the `−n` flag is used, three samples per pixel (red, green, and blue). The resulting images are always planar configuration contiguous. For this reason, this program is a useful utility for transform exotic TIFF files into a form ingestible by almost any TIFF supporting software. 

Options
-------

|Option|Description|
|---|---|
|−c compression_name|Specify a compression scheme to use when writing image data: `−c none` for no compression (the default), `−c packbits` for the PackBits compression algorithm, `−c zip` for the Deflate compression algorithm, `−c jpeg` for the JPEG compression algorithm, and `−c lzw` for Lempel-Ziv & Welch.|
|−r number_of_rows|Write data with a specified number of rows per strip; by default the number of rows/strip is selected so that each strip is approximately 8 kilobytes.|
|−b|Process the image one block (strip/tile) at a time instead of by reading the whole image into memory at once. This may be necessary for very large images on systems with limited RAM.|
|−n|Drop the alpha component from the output file, producing a pure RGB file. Currently this does not work if the `−b` flag is also in effect.|
