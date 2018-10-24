This sample shows how to read basic properties of a TIFF image like dimensions and resolution.

TIFF image properties are the data that is stored in a TIFF file along with pixel's data. Each property is
tagged with a numeric value.

LibTiff.Net defines mnemonic names for well-known tags (see TiffTag enumeration).
You can read properties using GetField method. The method returns array of FieldValue structures. Most of
the properties are represented by exactly one value but there are some properties that are represented by
two, three or more values. FieldValue structure has methods to convert values to integers, strings and
other data types.
