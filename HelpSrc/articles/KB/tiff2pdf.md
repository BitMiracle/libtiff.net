Tiff2Pdf converts a TIFF image to a PDF document. 

Usage
-----

`Tiff2Pdf [ options ] input.tiff`

Description
-----------

Tiff2Pdf opens a TIFF image and writes a PDF document to standard output. 

The program converts one TIFF file to one PDF file, including multiple page TIFF files, tiled TIFF files, black and white, grayscale, and color TIFF files that contain data of TIFF photometric interpretations of bilevel, grayscale, RGB, YCbCr, CMYK separation, and ICC L\*a\*b\* as supported by LibTiff and PDF. 

If you have multiple TIFF files to convert into one PDF file then use Tiffcp or other program to concatenate the files into a multiple page TIFF file. If the input TIFF file is of huge dimensions (greater than 10000 pixels height or width) convert the input image to a tiled TIFF if it is not already. 

The standard output is standard output. Set the output file name with the `−o output.pdf` option. 

All black and white files are compressed into a single strip CCITT G4 Fax compressed PDF, unless tiled, where tiled black and white images are compressed into tiled CCITT G4 Fax compressed PDF. 

Color and grayscale data can be compressed using either JPEG compression, ITU-T T.81, or Zip/Deflate LZ77 compression. Set the compression type using the `−j` or `−z` options. Use only one or the other of `−j` and `−z`. 

If the input TIFF contains single strip CCITT G4 Fax compressed information, then that is written to the PDF file without transcoding, unless the options of no compression and no passthrough are set, `−d` and `−n`. 

If the input TIFF contains JPEG or single strip Zip/Deflate compressed information then that is written to the PDF file without transcoding, unless the options of no compression and no passthrough are set. 

The default page size upon which the TIFF image is placed is determined by the resolution and extent of the image data. Default values for the TIFF image resolution can be set using the `−x` and `−y` options. The page size can be set using the `−p` option for paper size, or `−w` and `−l` for paper width and length, then each page of the TIFF image is centered on its page. The distance unit for default resolution and page width and length can be set by the `−u` option, the default unit is inch. 

Various items of the output document information can be set with the `−e`, `−c`, `−a`, `−t`, `−s`, and `−k` options. Setting the argument of the option to "" for these tags causes the relevant document information field to be not written. Some of the document information values otherwise get their information from the input TIFF image, the software, author, document name, and image description. 

The Portable Document Format (PDF) specification is copyrighted by Adobe Systems, Incorporated. 

Options
-------

|Option|Description|
|---|---|
|−o output-file|Set the output to go to file output-file.|
|−j|Compress with JPEG.|
|−z|Compress with Zip/Deflate.|
|−q quality|Set the compression quality, 1-100 for JPEG.|
|−n|Do not allow data to be converted without uncompressing, no compressed data passthrough.|
|−b|Set PDF "Interpolate" user preference.|
|−d|Do not compress (decompress).|
|−i|Invert colors.|
|−p paper-size|Set paper size, e.g. `letter`, `legal`, `A4`.|
|−u [i\|m]|Set distance unit, `i` for inch, `m` for centimeter.|
|−w width|Set width in units.|
|−l length|Set length in units.|
|−x xres|Set x/width resolution default.|
|−y yres|Set y/length resolution default.|
|−r [d\|o]|Set `d` for resolution default for images without resolution, `o` for resolution override for all images.|
|−f|Set PDF "Fit Window" user preference.|
|−e YYYYMMDDHHMMSS|Set document information date, overrides image or current date/time default, YYYYMMDDHHMMSS.|
|−c creator|Set document information creator, overrides image software default.|
|−a author|Set document information author, overrides image artist default.|
|−t title|Set document information title, overrides image document name default.|
|−s subject|Set document information subject, overrides image description default.|
|−k keywords|Set document information keywords.|
|−h|List usage reminder to standard error and exit.|

Examples
--------

The following example would generate the file output.pdf from input.tiff. 

`Tiff2Pdf −o output.pdf input.tiff`

The following example would generate PDF output from input.tiff and write it to standard output. 

`Tiff2Pdf input.tiff`

The following example would generate the file output.pdf from input.tiff, putting the image pages on a letter sized page, compressing the output with JPEG, with JPEG quality 75, setting the title to "Document", and setting the "Fit Window" option. 

`Tiff2Pdf −p letter −j −q 75 −t "Document" −f −o output.pdf input.tiff`
