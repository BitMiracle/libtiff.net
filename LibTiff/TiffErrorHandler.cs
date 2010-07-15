/* Copyright (C) 2008-2010, Bit Miracle
 * http://www.bitmiracle.com
 * 
 * This software is based in part on the work of the Sam Leffler, Silicon 
 * Graphics, Inc. and contributors.
 *
 * Copyright (c) 1988-1997 Sam Leffler
 * Copyright (c) 1991-1997 Silicon Graphics, Inc.
 * For conditions of distribution and use, see the accompanying README file.
 */

using System;
using System.IO;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Error handler.
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffErrorHandler
    {
        /// <summary>
        /// Handles errors.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="module">The module.</param>
        /// <param name="format">The message format.</param>
        /// <param name="args">The optional format arguments.</param>
        public virtual void ErrorHandler(Tiff tif, string module, string format, params object[] args)
        {
            using (TextWriter stderr = Console.Error)
            {
                if (module != null)
                    stderr.Write("{0}: ", module);

                stderr.Write(format, args);
                stderr.Write(".\n");
            }
        }

        /// <summary>
        /// Handles errors.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="fd">The fd.</param>
        /// <param name="module">The module.</param>
        /// <param name="format">The message format.</param>
        /// <param name="args">The optional format arguments.</param>
        public virtual void ErrorHandlerExt(Tiff tif, object fd, string module, string format, params object[] args)
        {
        }

        /// <summary>
        /// Handles warnings.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="module">The module.</param>
        /// <param name="format">The message format.</param>
        /// <param name="args">The optional format arguments.</param>
        public virtual void WarningHandler(Tiff tif, string module, string format, params object[] args)
        {
            using (TextWriter stderr = Console.Error)
            {
                if (module != null)
                    stderr.Write("{0}: ", module);

                stderr.Write("Warning, ");
                stderr.Write(format, args);
                stderr.Write(".\n");
            }
        }

        /// <summary>
        /// Handles warnings.
        /// </summary>
        /// <param name="tif">The tif.</param>
        /// <param name="fd">The fd.</param>
        /// <param name="module">The module.</param>
        /// <param name="format">The message format.</param>
        /// <param name="args">The optional format arguments.</param>
        public virtual void WarningHandlerExt(Tiff tif, object fd, string module, string format, params object[] args)
        {
        }
    }
}
