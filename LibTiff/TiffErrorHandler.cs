using System;
using System.Diagnostics;
using System.IO;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Default error handler implementation.
    /// </summary>
    /// <remarks>
    /// <para><b>TiffErrorHandler</b> provides error and warning handling methods that write an
    /// error or a warning messages to the <see cref="Console.Error"/>.
    /// </para><para>
    /// Applications that desire to capture control in the event of an error or a warning should
    /// set their custom error and warning handler using <see cref="Tiff.SetErrorHandler"/> method.
    /// </para>
    /// </remarks>
#if EXPOSE_LIBTIFF
    public
#endif
    class TiffErrorHandler
    {
        /// <summary>
        /// Handles an error by writing it text to the <see cref="Console.Error"/>.
        /// </summary>
        /// <param name="tif">An instance of the <see cref="Tiff"/> class. Can be <c>null</c>.</param>
        /// <param name="method">The method where an error is detected.</param>
        /// <param name="format">A composite format string (see Remarks).</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <remarks>
        /// The <paramref name="format"/> is a composite format string that uses the same format as
        /// <see cref="O:System.String.Format"/> method. The <paramref name="method"/> parameter, if
        /// not <c>null</c>, is printed before the message; it typically is used to identify the
        /// method in which an error is detected.
        /// </remarks>
        public virtual void ErrorHandler(Tiff tif, string method, string format, params object[] args)
        {
#if NETFX_CORE
            Debug.Write("ERROR ");
            if (method != null)
                Debug.Write("{0}: ", method);

            Debug.Write(string.Format(format, args));
            Debug.Write("\n");
#else
            TextWriter stderr = Console.Error;
            if (method != null)
                stderr.Write("{0}: ", method);

            stderr.Write(format, args);
            stderr.Write("\n");
#endif


        }

        /// <summary>
        /// Handles an error by writing it text to the <see cref="Console.Error"/>.
        /// </summary>
        /// <param name="tif">An instance of the <see cref="Tiff"/> class. Can be <c>null</c>.</param>
        /// <param name="clientData">A client data.</param>
        /// <param name="method">The method where an error is detected.</param>
        /// <param name="format">A composite format string (see Remarks).</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <remarks><para>
        /// The <paramref name="format"/> is a composite format string that uses the same format as
        /// <see cref="O:System.String.Format"/> method. The <paramref name="method"/> parameter, if
        /// not <c>null</c>, is printed before the message; it typically is used to identify the
        /// method in which an error is detected.
        /// </para><para>
        /// The <paramref name="clientData"/> parameter can be anything. Its value and meaning is
        /// defined by an application and not the library.
        /// </para></remarks>
        public virtual void ErrorHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
        {
        }

        /// <summary>
        /// Handles a warning by writing it text to the <see cref="Console.Error"/>.
        /// </summary>
        /// <param name="tif">An instance of the <see cref="Tiff"/> class. Can be <c>null</c>.</param>
        /// <param name="method">The method where a warning is detected.</param>
        /// <param name="format">A composite format string (see Remarks).</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <remarks>
        /// The <paramref name="format"/> is a composite format string that uses the same format as
        /// <see cref="O:System.String.Format"/> method. The <paramref name="method"/> parameter, if
        /// not <c>null</c>, is printed before the message; it typically is used to identify the
        /// method in which a warning is detected.
        /// </remarks>
        public virtual void WarningHandler(Tiff tif, string method, string format, params object[] args)
        {
#if NETFX_CORE
            Debug.Write("WARNING ");
            if (method != null)
                Debug.Write("{0}: ", method);

            Debug.Write(string.Format(format, args));
            Debug.Write("\n");
#else

            TextWriter stderr = Console.Error;
            if (method != null)
                stderr.Write("{0}: ", method);

            stderr.Write("Warning, ");
            stderr.Write(format, args);
            stderr.Write("\n");
#endif
        }

        /// <summary>
        /// Handles a warning by writing it text to the <see cref="Console.Error"/>.
        /// </summary>
        /// <param name="tif">An instance of the <see cref="Tiff"/> class. Can be <c>null</c>.</param>
        /// <param name="clientData">A client data.</param>
        /// <param name="method">The method where a warning is detected.</param>
        /// <param name="format">A composite format string (see Remarks).</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <remarks><para>
        /// The <paramref name="format"/> is a composite format string that uses the same format as
        /// <see cref="O:System.String.Format"/> method. The <paramref name="method"/> parameter, if
        /// not <c>null</c>, is printed before the message; it typically is used to identify the
        /// method in which a warning is detected.
        /// </para><para>
        /// The <paramref name="clientData"/> parameter can be anything. Its value and meaning is
        /// defined by an application and not the library.
        /// </para></remarks>
        public virtual void WarningHandlerExt(Tiff tif, object clientData, string method, string format, params object[] args)
        {
        }
    }
}
