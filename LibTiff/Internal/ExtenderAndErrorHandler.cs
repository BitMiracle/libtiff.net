using System;
using System.IO;
using BitMiracle.LibTiff.Classic.Internal;

namespace BitMiracle.LibTiff.Classic
{
    partial class Tiff
    {
#if THREAD_SAFE_LIBTIFF
        private TiffErrorHandler m_errorHandler;
#else
        private static TiffErrorHandler m_errorHandler;
#endif

        /// <summary>
        /// Client Tag extension support (from Niles Ritter).
        /// </summary>
#if THREAD_SAFE_LIBTIFF
        private TiffExtendProc m_extender;
#else
        private static TiffExtendProc m_extender;
#endif

#if THREAD_SAFE_LIBTIFF
        public
#else
        private
#endif
 static Tiff Open(string fileName, string mode, TiffErrorHandler errorHandler)
        {
            return Tiff.Open(fileName, mode, errorHandler, null);
        }

#if THREAD_SAFE_LIBTIFF
        public
#else
        private
#endif
 static Tiff Open(string fileName, string mode, TiffErrorHandler errorHandler, TiffExtendProc extender)
        {
            const string module = "Open";

            FileMode fileMode;
            FileAccess fileAccess;
            getMode(mode, module, out fileMode, out fileAccess);

            FileStream stream = null;
            try
            {
                if (fileAccess == FileAccess.Read)
                    stream = File.Open(fileName, fileMode, fileAccess, FileShare.Read);
                else
                    stream = File.Open(fileName, fileMode, fileAccess);
            }
#if THREAD_SAFE_LIBTIFF
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return null;
            }
#else
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Error(module, "Failed to open '{0}'. {1}", fileName, e.Message);
                return null;
            }
#endif

            Tiff tif = ClientOpen(fileName, mode, stream, new TiffStream(), errorHandler, extender);
            if (tif == null)
                stream.Dispose();
            else
                tif.m_fileStream = stream;

            return tif;
        }

#if THREAD_SAFE_LIBTIFF
        public
#else
        private
#endif
 static Tiff ClientOpen(string name, string mode, object clientData, TiffStream stream, TiffErrorHandler errorHandler)
        {
            return ClientOpen(name, mode, clientData, stream, errorHandler, null);
        }

#if THREAD_SAFE_LIBTIFF
        public
#else
        private
#endif
 static Tiff ClientOpen(string name, string mode, object clientData, TiffStream stream, TiffErrorHandler errorHandler, TiffExtendProc extender)
        {
            const string module = "ClientOpen";

            if (mode == null || mode.Length == 0)
            {
                ErrorExt(null, clientData, module, "{0}: mode string should contain at least one char", name);
                return null;
            }

            FileMode fileMode;
            FileAccess fileAccess;
            int m = getMode(mode, module, out fileMode, out fileAccess);

            Tiff tif = new Tiff();
#if THREAD_SAFE_LIBTIFF
            if (errorHandler != null)
                tif.m_errorHandler = errorHandler;
            if (extender != null)
                tif.m_extender = extender;
#endif
            tif.m_name = name;

            tif.m_mode = m & ~(O_CREAT | O_TRUNC);
            tif.m_curdir = -1; // non-existent directory
            tif.m_curoff = 0;
            tif.m_curstrip = -1; // invalid strip
            tif.m_row = -1; // read/write pre-increment
            tif.m_clientdata = clientData;

            if (stream == null)
            {
                ErrorExt(tif, clientData, module, "TiffStream is null pointer.");
                return null;
            }

            tif.m_stream = stream;

            // setup default state
            tif.m_currentCodec = tif.m_builtInCodecs[0];

            // Default is to return data MSB2LSB and enable the use of
            // strip chopping when a file is opened read-only.
            tif.m_flags = TiffFlags.MSB2LSB;

            if (m == O_RDONLY || m == O_RDWR)
                tif.m_flags |= STRIPCHOP_DEFAULT;

            // Process library-specific flags in the open mode string.
            // See remarks for Open method for the list of supported flags.
            int modelength = mode.Length;
            for (int i = 0; i < modelength; i++)
            {
                switch (mode[i])
                {
                    case 'b':
                        if ((m & O_CREAT) != 0)
                            tif.m_flags |= TiffFlags.SWAB;
                        break;
                    case 'l':
                        break;
                    case 'B':
                        tif.m_flags = (tif.m_flags & ~TiffFlags.FILLORDER) | TiffFlags.MSB2LSB;
                        break;
                    case 'L':
                        tif.m_flags = (tif.m_flags & ~TiffFlags.FILLORDER) | TiffFlags.LSB2MSB;
                        break;
                    case 'H':
                        tif.m_flags = (tif.m_flags & ~TiffFlags.FILLORDER) | TiffFlags.LSB2MSB;
                        break;
                    case 'C':
                        if (m == O_RDONLY)
                            tif.m_flags |= TiffFlags.STRIPCHOP;
                        break;
                    case 'c':
                        if (m == O_RDONLY)
                            tif.m_flags &= ~TiffFlags.STRIPCHOP;
                        break;
                    case 'h':
                        tif.m_flags |= TiffFlags.HEADERONLY;
                        break;
                    case '4':
                        tif.m_flags |= TiffFlags.NOBIGTIFF;
                        break;
                    case '8':
                        tif.m_flags |= TiffFlags.ISBIGTIFF;
                        break;
                }
            }

            // Read in TIFF header.

            if ((tif.m_mode & O_TRUNC) != 0 || !tif.readHeaderOkWithoutExceptions(ref tif.m_header))
            {
                if (tif.m_mode == O_RDONLY)
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Cannot read TIFF header");
                    return null;
                }

                // Setup header and write.

                if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                    tif.m_header.tiff_magic = TIFF_BIGENDIAN;
                else
                    tif.m_header.tiff_magic = TIFF_LITTLEENDIAN;


                if ((tif.m_flags & TiffFlags.ISBIGTIFF) == TiffFlags.ISBIGTIFF)
                {
                    tif.m_header.tiff_version = TIFF_BIGTIFF_VERSION;
                    tif.m_header.tiff_diroff = 0; //filled in later
                    tif.m_header.tiff_fill = 0;
                    tif.m_header.tiff_offsize = sizeof(long);
                    if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                    {
                        SwabShort(ref tif.m_header.tiff_version);
                        SwabShort(ref tif.m_header.tiff_offsize);
                    }

                }
                else
                {
                    tif.m_header.tiff_version = TIFF_VERSION;
                    tif.m_header.tiff_diroff = 0; //filled in later
                    tif.m_header.tiff_fill = sizeof(long);
                    if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
                    {
                        SwabShort(ref tif.m_header.tiff_version);
                    }
                }

                tif.seekFile(0, SeekOrigin.Begin);

                if (!tif.writeHeaderOK(tif.m_header))
                {
                    ErrorExt(tif, tif.m_clientdata, name, "Error writing TIFF header");
                    tif.m_mode = O_RDONLY;
                    return null;
                }

                // Setup the byte order handling.
                tif.initOrder(tif.m_header.tiff_magic);

                // Setup default directory.
                tif.setupDefaultDirectory();
                tif.m_diroff = 0;
                tif.m_dirlist = null;
                tif.m_dirlistsize = 0;
                tif.m_dirnumber = 0;
                return tif;
            }

            // Setup the byte order handling.
            if (tif.m_header.tiff_magic != TIFF_BIGENDIAN &&
                tif.m_header.tiff_magic != TIFF_LITTLEENDIAN &&
                tif.m_header.tiff_magic != MDI_LITTLEENDIAN)
            {
                ErrorExt(tif, tif.m_clientdata, name,
                    "Not a TIFF or MDI file, bad magic number {0} (0x{1:x})",
                    tif.m_header.tiff_magic, tif.m_header.tiff_magic);
                tif.m_mode = O_RDONLY;
                return null;
            }

            tif.initOrder(tif.m_header.tiff_magic);

            // Swap header if required.
            if ((tif.m_flags & TiffFlags.SWAB) == TiffFlags.SWAB)
            {
                SwabShort(ref tif.m_header.tiff_version);
                SwabBigTiffValue(ref tif.m_header.tiff_diroff, tif.m_header.tiff_version == TIFF_BIGTIFF_VERSION, false);
            }

            // Now check version (if needed, it's been byte-swapped).
            // Note that this isn't actually a version number, it's a
            // magic number that doesn't change (stupid).
            if (tif.m_header.tiff_version == TIFF_BIGTIFF_VERSION)
            {
                if ((tif.m_flags & TiffFlags.NOBIGTIFF) == TiffFlags.NOBIGTIFF)
                {
                    ErrorExt(tif, tif.m_clientdata, name,
                    "This is a BigTIFF file. Non-BigTIFF mode '32' is forced");
                    tif.m_mode = O_RDONLY;
                    return null;
                }
            }
            if (tif.m_header.tiff_version == TIFF_VERSION)
            {
                if ((tif.m_flags & TiffFlags.ISBIGTIFF) == TiffFlags.ISBIGTIFF)
                {
                    ErrorExt(tif, tif.m_clientdata, name,
                    "This is a non-BigTIFF file. BigTIFF mode '64' is forced");
                    tif.m_mode = O_RDONLY;
                    return null;
                }
            }
            if (tif.m_header.tiff_version != TIFF_VERSION && tif.m_header.tiff_version != TIFF_BIGTIFF_VERSION)
            {
                ErrorExt(tif, tif.m_clientdata, name,
                    "Not a TIFF file, bad version number {0} (0x{1:x})",
                    tif.m_header.tiff_version, tif.m_header.tiff_version);
                tif.m_mode = O_RDONLY;
                return null;
            }

            tif.m_flags |= TiffFlags.MYBUFFER;
            tif.m_rawcp = 0;
            tif.m_rawdata = null;
            tif.m_rawdatasize = 0;

            // Sometimes we do not want to read the first directory (for example,
            // it may be broken) and want to proceed to other directories. I this
            // case we use the HEADERONLY flag to open file and return
            // immediately after reading TIFF header.
            if ((tif.m_flags & TiffFlags.HEADERONLY) == TiffFlags.HEADERONLY)
                return tif;

            // Setup initial directory.
            switch (mode[0])
            {
                case 'r':
                    tif.m_nextdiroff = tif.m_header.tiff_diroff;

                    if (tif.ReadDirectory())
                    {
                        tif.m_rawcc = -1;
                        tif.m_flags |= TiffFlags.BUFFERSETUP;
                        return tif;
                    }
                    break;
                case 'a':
                    // New directories are automatically append to the end of
                    // the directory chain when they are written out (see WriteDirectory).
                    tif.setupDefaultDirectory();
                    return tif;
            }

            tif.m_mode = O_RDONLY;
            return null;
        }

        private static TiffErrorHandler setErrorHandlerImpl(TiffErrorHandler errorHandler)
        {
#if THREAD_SAFE_LIBTIFF
            throw new InvalidOperationException("Do not use SetErrorHandler method (it's not thread-safe).\n" +
                "Use overloads for Open and ClientOpen methods to achieve the same.");
#else
            TiffErrorHandler prev = m_errorHandler;
            m_errorHandler = errorHandler;
            return prev;
#endif
        }

        private static TiffExtendProc setTagExtenderImpl(TiffExtendProc extender)
        {
#if THREAD_SAFE_LIBTIFF
            throw new InvalidOperationException("Do not use SetTagExtender method (it's not thread-safe).\n" +
                "Use overloads for Open and ClientOpen methods to achieve the same.");
#else
            TiffExtendProc prev = m_extender;
            m_extender = extender;
            return prev;
#endif
        }

        private static TiffErrorHandler getErrorHandler(Tiff tif)
        {
            TiffErrorHandler errorHandler = null;
#if THREAD_SAFE_LIBTIFF
            if (tif != null)
            {
                errorHandler = tif.m_errorHandler;
            }
#else
            errorHandler = m_errorHandler;
#endif
            return errorHandler;
        }
    }
}
