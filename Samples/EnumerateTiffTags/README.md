This sample shows how to enumerate all tags that are set in a TIFF image.

Basic idea is to use Tiff.GetField method with all possible input values in a loop and check for non-null return value.