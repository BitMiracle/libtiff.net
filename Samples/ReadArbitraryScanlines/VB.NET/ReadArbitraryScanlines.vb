Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ReadArbitraryScanlines
        Public Shared Sub Main()
            Dim startScanline As Integer = 10
            Dim stopScanline As Integer = 20

            Using image As Tiff = Tiff.Open("Sample Data\f-lzw.tif", "r")
                Dim stride As Integer = image.ScanlineSize()
                Dim scanline As Byte() = New Byte(stride - 1) {}

                Dim compression As Compression = DirectCast(image.GetField(TiffTag.COMPRESSION)(0).ToInt(), Compression)
                If compression = compression.LZW OrElse compression = compression.PACKBITS Then
                    ' LZW and PackBits compression schemes do not allow
                    ' scanlines to be read in a random fashion.
                    ' So, we need to read all scanlines from start of the image.

                    For i As Integer = 0 To startScanline - 1
                        ' of course, the data won't be used.
                        image.ReadScanline(scanline, i)
                    Next
                End If

                For i As Integer = startScanline To stopScanline - 1

                    ' do what ever you need with the data
                    image.ReadScanline(scanline, i)
                Next
            End Using
        End Sub
    End Class
End Namespace
