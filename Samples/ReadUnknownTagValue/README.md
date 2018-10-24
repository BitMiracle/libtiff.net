This sample shows how to read value of a TIFF tag that is not well-known to the library.

LibTiff.Net designed to read and write all well-known TIFF tags but once in a while you may face a TIFF file
that has some data tagged with a tag unknown to the library. Don't worry, the library won't lose any data,
the tag will be auto-registered and you'll be able to retrieve the data using GetField method.
