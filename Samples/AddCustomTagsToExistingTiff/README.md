This sample shows how to add custom TIFF tag to an existing TIFF image.

Custom tags are tags unknown to the library. However, they may be added to a file without problems. Special "extender callback" method should be used to instruct the library about format, names and other properties of such tags.

This sample shows how to create "extender callback" method, register that method with the library and use Tiff.MergeFieldInfo to merge description of GDAL Metadata tag with other tags description. GDAL Metadata tag is used as an example, you may add any other tags the same way.
