TiffCP copies (and possibly converts) a TIFF file. 

Usage
-----

`TiffCP [ options ] src1.tif ... srcN.tif dst.tif`

Description
-----------

TiffCP combines one or more files created according to the Tag Image File Format, Revision 6.0 into a single TIFF file. Because the output file may be compressed using a different algorithm than the input files, TiffCP is most often used to convert between different compression schemes. 

By default, TiffCP will copy all the understood tags in a TIFF directory of an input file to the associated directory in the output file. 

TiffCP can be used to reorganize the storage characteristics of data in a file, but it is explicitly intended to not alter or convert the image data content in any way. 

Options
-------

|Option|Description|
|---|---|
|−b image|Subtract the following monochrome image from all others processed. This can be used to remove a noise bias from a set of images. This bias image is typically an image of noise the camera saw with its shutter closed.|
|−B|Force output to be written with Big-Endian byte order. This option only has an effect when the output file is created or overwritten and not when it is appended to.|
|−C|Suppress the use of "strip chopping" when reading images that have a single strip/tile of uncompressed data.|
|−c|Specify the compression to use for data written to the output file: `none` for no compression, `packbits` for PackBits compression, `lzw` for Lempel-Ziv & Welch compression, `jpeg` for baseline JPEG compression, `zip` for Deflate compression, `g3` for CCITT Group 3 (T.4) compression, and `g4` for CCITT Group 4 (T.6) compression. By default TiffCP will compress data according to the value of the Compression tag found in the source file.<br><br>The CCITT Group 3 and Group 4 compression algorithms can only be used with bilevel data.<br><br>Group 3 compression can be specified together with several T.4-specific options: `1d` for 1-dimensional encoding, `2d` for 2-dimensional encoding, and `fill` to force each encoded scanline to be zero-filled so that the terminating EOL code lies on a byte boundary. Group 3-specific options are specified by appending a ":"-separated list to the "g3" option; e.g. `−c g3:2d:fill` to get 2D-encoded data with byte-aligned EOL codes.<br><br>LZW compression can be specified together with a predictor value. A predictor value of `2` causes each scanline of the output image to undergo horizontal differencing before it is encoded; a value of `1` forces each scanline to be encoded without differencing. LZW-specific options are specified by appending a ":"-separated list to the "lzw" option; e.g. `−c lzw:2` for LZW compression with horizontal differencing.|
|−f|Specify the bit fill order to use in writing output data. By default, TiffCP will create a new file with the same fill order as the original. Specifying `−f lsb2msb` will force data to be written with the FillOrder tag set to LSB2MSB, while `−f msb2lsb` will force data to be written with the FillOrder tag set to MSB2LSB.|
|−i|Ignore non-fatal read errors and continue processing of the input file.|
|−l|Specify the length of a tile (in pixels). TiffCP attempts to set the tile dimensions so that no more than 8 kilobytes of data appear in a tile.|
|−L|Force output to be written with Little-Endian byte order. This option only has an effect when the output file is created or overwritten and not when it is appended to.|
|−p|Specify the planar configuration to use in writing image data that has one 8-bit sample per pixel. By default, TiffCP will create a new file with the same planar configuration as the original. Specifying `−p contig` will force data to be written with multi-sample data packed together, while `−p separate` will force samples to be written in separate planes.|
|−r|Specify the number of rows (scanlines) in each strip of data written to the output file. By default (or when value 0 is specified), TiffCP attempts to set the rows/strip that no more than 8 kilobytes of data appear in a strip. If you specify special value `−1` it will results in infinite number of the rows per strip. The entire image will be the one strip in that case.|
|−s|Force the output file to be written with data organized in strips (rather than tiles).|
|−t|Force the output file to be written with data organized in tiles (rather than strips).|
|−w|Specify the width of a tile (in pixels). TiffCP attempts to set the tile dimensions so that no more than 8 kilobytes of data appear in a tile.|
|−x|Force the output file to be written with PAGENUMBER value in sequence.|
|−,=character|Substitute character for "," in parsing image directory indices in files. This is necessary if filenames contain commas. Note that −,= with whitespace immediately following will disable the special meaning of the "," entirely.|

Examples
--------

The following concatenates two files and writes the result using LZW encoding: 

`tiffcp −c lzw a.tif b.tif result.tif`

To convert a G3 1d-encoded TIFF to a single strip of G4-encoded data the following might be used (1000 is just a number that is larger than the number of rows in the source file): 

`tiffcp −c g4 −r 10000 g3.tif g4.tif`

To extract a selected set of images from a multi-image TIFF file, the file name may be immediately followed by a "," separated list of image directory indices. The first image is always in directory 0. Thus, to copy the 1st and 3rd images of image file "album.tif" to "result.tif": 

`tiffcp album.tif,0,2 result.tif`

A trailing comma denotes remaining images in sequence. The following command will copy all image with except the first one: 

`tiffcp album.tif,1, result.tif`

Given file "CCD.tif" whose first image is a noise bias followed by images which include that bias, subtract the noise from all those images following it (while decompressing) with the command: 

`tiffcp −c none −b CCD.tif CCD.tif,1, result.tif`

If the file above were named "CCD,X.tif", the −,= option would be required to correctly parse this filename with image numbers, as follows: 

`tiffcp −c none −,=% −b CCD,X.tif CCD,X%1%.tif result.tif`
