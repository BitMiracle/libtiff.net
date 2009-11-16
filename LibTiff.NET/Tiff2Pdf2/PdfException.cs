using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.Docotic.PDFLib
{
    class PdfException : Exception
    {
        private PdfExceptionType m_type;

        public PdfException()
        {
            m_type = PdfExceptionType.UnknownPdfException;
        }

        public PdfException(PdfExceptionType type) : base(PdfException.getErrorMessage(type))
        {
            m_type = type;
        }

        public PdfExceptionType GetExceptionType()
        {
            return m_type;
        }

        public string GetErrorMessage()
        {
            return getErrorMessage(m_type);
        }

        private static string getErrorMessage(PdfExceptionType type)
        {
            switch (type)
            {
                case PdfExceptionType.MemoryAllocationFailed:
                    return "Cannot allocate memory";

                case PdfExceptionType.InvalidObject:
                    return "Object is invalid";

                case PdfExceptionType.InvalidIndex:
                    return "Wrong collection index";

                case PdfExceptionType.InvalidDestination:
                    return "Destination object is invalid";

                case PdfExceptionType.InvalidParameter:
                    return "Wrong parameter";

                case PdfExceptionType.InvalidJpegImage:
                    return "JPEG image has wrong format";

                case PdfExceptionType.InvalidPngImage:
                    return "PNG image has wrong format";

                case PdfExceptionType.InvalidGifImage:
                    return "GIF image has wrong format";

                case PdfExceptionType.InvalidTiffImage:
                    return "TIFF image has wrong format";

                case PdfExceptionType.InvalidXObject:
                    return "Wrong XObject";

                case PdfExceptionType.InvalidDocument:
                    return "Document is invalid";

                case PdfExceptionType.InvalidItem:
                    return "Wrong item type";

                case PdfExceptionType.ArrayLengthExceeded:
                    return "Exceeded array length limit";

                case PdfExceptionType.DictionaryLengthExceeded:
                    return "Exceeded dictionary length limit";

                case PdfExceptionType.XrefLengthExceeded:
                    return "Exceeded XRef length limit";

                case PdfExceptionType.RealValueIsOutOfRange:
                    return "Real value is out of range";

                case PdfExceptionType.NameIsTooLong:
                    return "Exceeded name length limit";

                case PdfExceptionType.BinaryIsTooLong:
                    return "Exceeded binary object length limit";

                case PdfExceptionType.StringIsTooLong:
                    return "Exceeded string length limit";

                case PdfExceptionType.GStateStackDepthExceeded:
                    return "Exceeded graphics state stack depth limit";

                case PdfExceptionType.ZLibError:
                    return "Error in deflate compressor";

                case PdfExceptionType.LibPngError:
                    return "Error in LibPng";

                case PdfExceptionType.FileOpenFailed:
                    return "Cannot open the file";

                case PdfExceptionType.FileIOError:
                    return "I/O error";

                case PdfExceptionType.CannotGetEncryptionDictionary:
                    return "Cannot get encryption dictionary";

                case PdfExceptionType.CannotGetDictionaryItem:
                    return "Item is not found";

                case PdfExceptionType.CannotGetPNGPallete:
                    return "Cannot get PNG pallete";

                case PdfExceptionType.CannotSetParent:
                    return "Page already has parent";

                case PdfExceptionType.CannotRestoreState:
                    return "Cannot restore graphics state";

                case PdfExceptionType.CannotShowText:
                    return "Cannot show text with selected font";

                case PdfExceptionType.JWWCodeLimitExceeded:
                    return "Exceeded JWW code limit";

                case PdfExceptionType.StreamError:
                    return "Stream error";

                case PdfExceptionType.IncorrectLexeme:
                    return "Lexeme is incorrect";

                case PdfExceptionType.WrongDocumentHeader:
                    return "PDF document header not found or has wrong format";

                case PdfExceptionType.UnsupportedDocumentFormat:
                    return "Unsupported PDF format version";

                case PdfExceptionType.WrongStartxref:
                    return "startxref not found or has wrong format";

                case PdfExceptionType.WrongTrailer:
                    return "Trailer not found or has wrong format";

                case PdfExceptionType.WrongXref:
                    return "Xref not found or has wrong format";

                case PdfExceptionType.WrongDictionary:
                    return "Dictionary has wrong format";

                case PdfExceptionType.WrongCatalog:
                    return "Document catalog is invalid";

                case PdfExceptionType.InvalidPageOperator:
                    return "Page operator is invalid";

                case PdfExceptionType.WrongPages:
                    return "Pages dictionary is invalid";

                case PdfExceptionType.WrongPage:
                    return "Page dictionary is invalid";

                case PdfExceptionType.WrongFont:
                    return "Font object is invalid";

                case PdfExceptionType.WrongEncoding:
                    return "Encoding object is invalid";

                case PdfExceptionType.WrongResources:
                    return "Resources dictionary is invalid";

                case PdfExceptionType.WrongArray:
                    return "Array size is invalid";

                case PdfExceptionType.WrongObject:
                    return "Object is invalid";

                case PdfExceptionType.WrongString:
                    return "String object is invalid";

                case PdfExceptionType.WrongControlName:
                    return "Control with such name already exists";

                case PdfExceptionType.WrongEncryptDictionary:
                    return "Encrypt dictionary has wrong format";

                case PdfExceptionType.WrongPassword:
                    return "Password is incorrect";

                case PdfExceptionType.UnsupportedFont:
                    return "Unsupported font dictionary subtype";

                case PdfExceptionType.WrongViewerPreferences:
                    return "Value in viewer preferences dictionary is invalid";

                case PdfExceptionType.AlreadyAttached:
                    return "Object already attached to a xref";

                case PdfExceptionType.NotAttached:
                    return "Object is not attached to a xref";

                case PdfExceptionType.UnsupportedActionType:
                    return "Specified action type is unsupported";

                case PdfExceptionType.CannotDeleteAlonePage:
                    return "Cannot delete alone page";

                case PdfExceptionType.CannotDeleteCurrentPage:
                    return "Cannot delete current page";

                case PdfExceptionType.LZWTableOverflow:
                    return "LZW table contains more than 4095 entries";

                case PdfExceptionType.UnsupportedEncryptAlgorithm:
                    return "Unsupported encrypt algorithm version";

                case PdfExceptionType.UnsupportedEncryptor:
                    return "Unsupported encryptor";

                case PdfExceptionType.UnsupportedColorSpace:
                    return "Unsupported color space";

                case PdfExceptionType.InvalidBMPImage:
                    return "Bitmap has wrong format";
                    
                case PdfExceptionType.InvalidImage:
                    return "Image has wrong format";

                case PdfExceptionType.CannotUseColorWithColoredPattern:
                    return "Can not use color with colored pattern";

                case PdfExceptionType.UnsupportedJpegImage:
                    return "Unsupported Jpeg image type";

                case PdfExceptionType.UnsupportedBMPImage:
                    return "Unsupported BMP image type";

                case PdfExceptionType.UnknownPdfException:
                default:
                    return "Unknown exception";
            }
        }
    }
}
