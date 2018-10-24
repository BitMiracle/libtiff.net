Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class Convert16BitTo8Bit
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using tiff8bit As Bitmap = getBitmap8Bit("Sample Data\16bit.tif")
                If tiff8bit Is Nothing Then
                    Console.WriteLine("Failed to convert image. Maybe input image does not exist or is not 16 bit.")
                    Return
                End If

                tiff8bit.Save("Convert16BitTo8Bit.bmp")
                Process.Start("Convert16BitTo8Bit.bmp")
            End Using

        End Sub

        Private Shared Function getBitmap8Bit(ByVal inputName As String) As Bitmap
            Dim result As Bitmap

            Using tif As Tiff = Tiff.Open(inputName, "r")
                Dim res As FieldValue() = tif.GetField(TiffTag.IMAGELENGTH)
                Dim height As Integer = res(0).ToInt()

                res = tif.GetField(TiffTag.IMAGEWIDTH)
                Dim width As Integer = res(0).ToInt()

                res = tif.GetField(TiffTag.BITSPERSAMPLE)
                Dim bpp As Short = res(0).ToShort()
                If bpp <> 16 Then
                    Return Nothing
                End If

                res = tif.GetField(TiffTag.SAMPLESPERPIXEL)
                Dim spp As Short = res(0).ToShort()
                If spp <> 1 Then
                    Return Nothing
                End If

                res = tif.GetField(TiffTag.PHOTOMETRIC)
                Dim photo As Photometric = DirectCast(res(0).ToInt(), Photometric)
                If photo <> Photometric.MINISBLACK AndAlso photo <> Photometric.MINISWHITE Then
                    Return Nothing
                End If

                Dim stride As Integer = tif.ScanlineSize()
                Dim buffer As Byte() = New Byte(stride - 1) {}

                result = New Bitmap(width, height, PixelFormat.Format8bppIndexed)
                Dim buffer8Bit As Byte() = Nothing

                For i As Integer = 0 To height - 1
                    Dim imgRect As New Rectangle(0, i, width, 1)
                    Dim imgData As BitmapData = result.LockBits(imgRect, ImageLockMode.[WriteOnly], PixelFormat.Format8bppIndexed)

                    If buffer8Bit Is Nothing Then
                        buffer8Bit = New Byte(imgData.Stride - 1) {}
                    Else
                        Array.Clear(buffer8Bit, 0, buffer8Bit.Length)
                    End If

                    tif.ReadScanline(buffer, i)
                    convertBuffer(buffer, buffer8Bit)

                    Marshal.Copy(buffer8Bit, 0, imgData.Scan0, buffer8Bit.Length)
                    result.UnlockBits(imgData)
                Next
            End Using

            Return result
        End Function

        Private Shared Sub convertBuffer(ByVal buffer As Byte(), ByVal buffer8Bit As Byte())
            Dim src As Integer = 0, dst As Integer = 0
            While src < buffer.Length
                Dim value16 As Integer = buffer(src)
                src += 1
                value16 = value16 + (CType(buffer(src), Integer) << 8)
                src += 1
                buffer8Bit(dst) = Math.Floor(value16 / 257.0 + 0.5) Mod 256
                dst += 1
            End While
        End Sub
    End Class
End Namespace