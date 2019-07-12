The library is capable of dealing with images that are written to follow the 5.0 or 6.0 TIFF spec. There is also considerable support for some of the more esoteric portions of the 6.0 TIFF spec. 

Core requirements
-----------------

Both "MM" and "II" (big-endian and little-endian) byte orders are handled. Both packed and separated planar configuration of samples. Any number of samples per pixel (memory permitting). Any image width and height (memory permitting). Multiple subfiles can be read and written. Editing is not supported in that related subfiles (i.e. a reduced resolution version of an image) and they are not automatically updated. 

Tags handled: 

* <xref:BitMiracle.LibTiff.Classic.TiffTag>.EXTRASAMPLES 
* TiffTag.IMAGEWIDTH
* TiffTag.IMAGELENGTH
* TiffTag.SUBFILETYPE
* TiffTag.RESOLUTIONUNIT
* TiffTag.ROWSPERSTRIP
* TiffTag.STRIPOFFSETS
* TiffTag.STRIPBYTECOUNTS
* TiffTag.XRESOLUTION
* TiffTag.YRESOLUTION

Tiled Images
------------

* TiffTag.TILEWIDTH
* TiffTag.TILELENGTH
* TiffTag.TILEOFFSETS
* TiffTag.TILEBYTECOUNTS

Image Colorimetry Information
-----------------------------

* TiffTag.WHITEPOINT
* TiffTag.PRIMARYCHROMATICITIES
* TiffTag.TRANSFERFUNCTION
* TiffTag.REFERENCEBLACKWHITE

Class B for bilevel images
--------------------------

* TiffTag.SAMPLESPERPIXEL = 1
* TiffTag.BITSPERSAMPLE = 1
* TiffTag.COMPRESSION = <xref:BitMiracle.LibTiff.Classic.Compression>.NONE, Compression.CCITTRLE, or Compression.PACKBITS 
* TiffTag.PHOTOMETRIC = <xref:BitMiracle.LibTiff.Classic.Photometric>.MINISWHITE, Photometric.MINISBLACK 

Class G for grayscale images
----------------------------

* TiffTag.SAMPLESPERPIXEL = 1
* TiffTag.BITSPERSAMPLE = 4, 8
* TiffTag.COMPRESSION = Compression.NONE, Compression.LZW
* TiffTag.PHOTOMETRIC = Photometric.MINISWHITE, Photometric.MINISBLACK

Class P for palette color images
--------------------------------

* TiffTag.SAMPLESPERPIXEL = 1
* TiffTag.BITSPERSAMPLE = 1-8
* TiffTag.COMPRESSION = Compression.NONE, Compression.LZW
* TiffTag.PHOTOMETRIC = Photometric.PALETTE
* TiffTag.COLORMAP

Class R for RGB full color images
---------------------------------

* TiffTag.SAMPLESPERPIXEL = 3
* TiffTag.BITSPERSAMPLE = <8, 8, 8>
* TiffTag.PLANARCONFIG = <xref:BitMiracle.LibTiff.Classic.PlanarConfig>.CONTIG, PlanarConfig.SEPARATE 
* TiffTag.COMPRESSION = Compression.NONE, Compression.LZW
* TiffTag.PHOTOMETRIC = Photometric.RGB

Class F for facsimile
---------------------

* TiffTag.SAMPLESPERPIXEL = 1
* TiffTag.BITSPERSAMPLE = 1
* TiffTag.COMPRESSION = Compression.NONE, Compression.CCITTRLE, Compression.PACKBITS, Compression.CCITTFAX3 (Compression.CCITT_T4), Compression.CCITTFAX4 (Compression.CCITT_T6)
* TiffTag.PHOTOMETRIC = Photometric.MINISWHITE, Photometric.MINISBLACK
* TiffTag.GROUP3OPTIONS (TiffTag.T4OPTIONS) = <xref:BitMiracle.LibTiff.Classic.Group3Opt>.* 
* TiffTag.GROUP4OPTIONS (TiffTag.T6OPTIONS)
* TiffTag.FAXFILLFUNC = instance of <xref:BitMiracle.LibTiff.Classic.Tiff.FaxFillFunc>
* TiffTag.PAGENUMBER
* TiffTag.XRESOLUTION
* TiffTag.YRESOLUTION
* TiffTag.SOFTWARE
* TiffTag.BADFAXLINES
* TiffTag.CLEANFAXDATA = <xref:BitMiracle.LibTiff.Classic.CleanFaxData>.* 
* TiffTag.CONSECUTIVEBADFAXLINES
* TiffTag.DATETIME
* TiffTag.DOCUMENTNAME
* TiffTag.IMAGEDESCRIPTION
* TiffTag.ORIENTATION = <xref:BitMiracle.LibTiff.Classic.Orientation>.* 

Class S for separated images
----------------------------

* TiffTag.SAMPLESPERPIXEL = 4
* TiffTag.PLANARCONFIG = PlanarConfig.CONTIG, PlanarConfig.SEPARATE
* TiffTag.COMPRESSION = Compression.NONE, Compression.LZW
* TiffTag.PHOTOMETRIC = Photometric.SEPARATED
* TiffTag.INKSET = <xref:BitMiracle.LibTiff.Classic.InkSet>.CMYK 
* TiffTag.DOTRANGE
* TiffTag.INKNAMES
* TiffTag.NUMBEROFINKS
* TiffTag.TARGETPRINTER

Class Y for YCbCr images
------------------------

* TiffTag.SAMPLESPERPIXEL = 3
* TiffTag.BITSPERSAMPLE = <8, 8, 8>
* TiffTag.PLANARCONFIG = PlanarConfig.CONTIG, PlanarConfig.SEPARATE
* TiffTag.COMPRESSION = Compression.NONE, Compression.LZW, Compression.JPEG
* TiffTag.PHOTOMETRIC = Photometric.YCBCR
* TiffTag.YCBCRCOEFFICIENTS
* TiffTag.YCBCRSUBSAMPLING
* TiffTag.YCBCRPOSITIONING = <xref:BitMiracle.LibTiff.Classic.YCbCrPosition>.* 
* colorimetry info from TIFF Specification, revision 6.0 Appendix H (see [Links to Resources](~/articles/links.html)) 

Class "JPEG" for JPEG images (per TIFF Technical Note #2)
---------------------------------------------------------

* TiffTag.PHOTOMETRIC = Photometric.MINISBLACK, Photometric.RGB, Photometric.SEPARATED, or Photometric.YCBCR
* Class Y tags if TiffTag.PHOTOMETRIC = Photometric.YCBCR
* Class S tags if TiffTag.PHOTOMETRIC = Photometric.SEPARATED
* TiffTag.COMPRESSION = Compression.JPEG

JPEG support is based on the post-6.0 proposal given in TIFF Technical Note #2 (see [Links to Resources](~/articles/links.html)) which defines a revised JPEG-in-TIFF scheme (revised over that appendix that was part of the TIFF 6.0 specification). In addition, the library supports PKZIP-style Deflate encoding (Compression.DEFLATE) and Old-style JPEG encoding (as defined in the 6.0 specification, Compression.OJPEG) in read-only mode. 

The following table shows the tags that are recognized and how they are used by the library. If no use is indicated, then the library reads and writes the tag, but does not use it internally. 

|Tag|R/W|Library's Use|
|---|---|---|
|TiffTag.SUBFILETYPE|R/W|none|
|TiffTag.OSUBFILETYPE|R/W|none|
|TiffTag.IMAGEWIDTH|R/W|lots|
|TiffTag.IMAGELENGTH|R/W|lots|
|TiffTag.BITSPERSAMPLE|R/W|lots|
|TiffTag.COMPRESSION|R/W|to select appropriate codec|
|TiffTag.PHOTOMETRIC|R/W|lots|
|TiffTag.THRESHHOLDING|R/W||
|TiffTag.CELLWIDTH||parsed but ignored|
|TiffTag.CELLLENGTH||parsed but ignored|
|TiffTag.FILLORDER|R/W|control bit order|
|TiffTag.DOCUMENTNAME|R/W||
|TiffTag.IMAGEDESCRIPTION|R/W  
|TiffTag.MAKE|R/W||
|TiffTag.MODEL|R/W||
|TiffTag.STRIPOFFSETS|R/W|data i/o|
|TiffTag.ORIENTATION|R/W||
|TiffTag.SAMPLESPERPIXEL|R/W|lots|
|TiffTag.ROWSPERSTRIP|R/W|data i/o|
|TiffTag.STRIPBYTECOUNTS|R/W|data i/o|
|TiffTag.MINSAMPLEVALUE|R/W||
|TiffTag.MAXSAMPLEVALUE|R/W||
|TiffTag.XRESOLUTION|R/W||
|TiffTag.YRESOLUTION|R/W|used by Group 3 2d encoder|
|TiffTag.PLANARCONFIG|R/W|data i/o|
|TiffTag.PAGENAME|R/W||
|TiffTag.XPOSITION|R/W||
|TiffTag.YPOSITION|R/W||
|TiffTag.FREEOFFSETS|parsed but ignored|
|TiffTag.FREEBYTECOUNTS|parsed but ignored|
|TiffTag.GRAYRESPONSEUNIT|parsed but ignored|
|TiffTag.GRAYRESPONSECURVE|parsed but ignored|
|TiffTag.GROUP3OPTIONS|R/W|used by Group 3 codec|
|TiffTag.GROUP4OPTIONS|R/W||
|TiffTag.RESOLUTIONUNIT|R/W|used by Group 3 2d encoder|
|TiffTag.PAGENUMBER|R/W||
|TiffTag.COLORRESPONSEUNIT|parsed but ignored|
|TiffTag.TRANSFERFUNCTION|R/W||
|TiffTag.SOFTWARE|R/W||
|TiffTag.DATETIME|R/W||
|TiffTag.ARTIST|R/W||
|TiffTag.HOSTCOMPUTER|R/W||
|TiffTag.PREDICTOR|R/W|used by LZW codec|
|TiffTag.WHITEPOINT|R/W||
|TiffTag.PRIMARYCHROMATICITIES|R/W||
|TiffTag.COLORMAP|R/W||
|TiffTag.TILEWIDTH|R/W|data i/o|
|TiffTag.TILELENGTH|R/W|data i/o|
|TiffTag.TILEOFFSETS|R/W|data i/o|
|TiffTag.TILEBYTECOUNTS|R/W|data i/o|
|TiffTag.BADFAXLINES|R/W||
|TiffTag.CLEANFAXDATA|R/W||
|TiffTag.CONSECUTIVEBADFAXLINES|R/W||
|TiffTag.SUBIFD|R/W|subimage descriptor support|
|TiffTag.INKSET|R/W||
|TiffTag.INKNAMES|R/W||
|TiffTag.DOTRANGE|R/W||
|TiffTag.TARGETPRINTER|R/W||
|TiffTag.EXTRASAMPLES|R/W|lots|
|TiffTag.SAMPLEFORMAT|R/W||
|TiffTag.SMINSAMPLEVALUE|R/W||
|TiffTag.SMAXSAMPLEVALUE|R/W||
|TiffTag.JPEGTABLES|R/W|used by JPEG codec|
|TiffTag.YCBCRCOEFFICIENTS|R/W|used by <xref:BitMiracle.LibTiff.Classic.Tiff.ReadRGBAImage*> support|
|TiffTag.YCBCRSUBSAMPLING|R/W|tile/strip size calculations|
|TiffTag.YCBCRPOSITIONING|R/W||
|TiffTag.REFERENCEBLACKWHITE|R/W||
|TiffTag.MATTEING|R|none (obsoleted by TiffTag.EXTRASAMPLES)|
|TiffTag.DATATYPE|R|none (obsoleted by TiffTag.SAMPLEFORMAT)|
|TiffTag.IMAGEDEPTH|R/W|tile/strip calculations|
|TiffTag.TILEDEPTH|R/W|tile/strip calculations|
|TiffTag.STONITS|R/W||

The TiffTag.MATTEING and TiffTag.DATATYPE have been obsoleted by the 6.0 TiffTag.EXTRASAMPLES and TiffTag.SAMPLEFORMAT tags. Consult the documentation on the ExtraSample.ASSOCALPHA for elaboration. Note, however, that if you use ExtraSample.ASSOCALPHA, you are expected to save data that is pre-multipled by Alpha. If this means nothing to you, check out Porter and Duff's paper in the '84 SIGGRAPH proceedings: "Compositing Digital Images". 

The TiffTag.IMAGEDEPTH is a non-standard, but registered tag that specifies the Z-dimension of volumetric data. The combination of TiffTag.IMAGEWIDTH, TiffTag.IMAGELENGTH, and TiffTag.IMAGEDEPTH, defines a 3D volume of pixels that are further specified by TiffTag.BITSPERSAMPLE and TiffTag.SAMPLESPERPIXEL. The TiffTag.TILEDEPTH (also non-standard, but registered) can be used to specified a subvolume "tiling" of a volume of data. 

The Colorimetry, and CMYK tags are additions that appear in TIFF 6.0. Consult the TIFF 6.0 specification referenced in [Links to Resources](~/articles/links.html). 
