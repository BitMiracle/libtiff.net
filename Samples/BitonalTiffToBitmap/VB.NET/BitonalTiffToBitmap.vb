Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class BitonalTiffToBitmap
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using bitmap As Bitmap = tiffToBitmap("Sample Data\bitonal.tif")
                If bitmap Is Nothing Then
                    MessageBox.Show("Failed to convert image. Maybe input image does not exist or is not 1 bit per pixel.")
                    Return
                End If

                bitmap.Save("BitonalTiffToBitmap.bmp")
                Process.Start("BitonalTiffToBitmap.bmp")
            End Using
        End Sub

        Private Shared Function tiffToBitmap(ByVal fileName As String) As Bitmap
            Using tif As Tiff = Tiff.Open(fileName, "r")
                If tif Is Nothing Then
                    Return Nothing
                End If

                Dim imageHeight As FieldValue() = tif.GetField(TiffTag.IMAGELENGTH)
                Dim height As Integer = imageHeight(0).ToInt()

                Dim imageWidth As FieldValue() = tif.GetField(TiffTag.IMAGEWIDTH)
                Dim width As Integer = imageWidth(0).ToInt()

                Dim bitsPerSample As FieldValue() = tif.GetField(TiffTag.BITSPERSAMPLE)
                Dim bpp As Short = bitsPerSample(0).ToShort()
                If bpp <> 1 Then
                    Return Nothing
                End If

                Dim samplesPerPixel As FieldValue() = tif.GetField(TiffTag.SAMPLESPERPIXEL)
                Dim spp As Short = samplesPerPixel(0).ToShort()
                If spp <> 1 Then
                    Return Nothing
                End If

                Dim photoMetricField As FieldValue() = tif.GetField(TiffTag.PHOTOMETRIC)
                Dim photo As Photometric = DirectCast(photoMetricField(0).ToInt(), Photometric)
                If photo <> Photometric.MINISBLACK AndAlso photo <> Photometric.MINISWHITE Then
                    Return Nothing
                End If

                Dim stride As Integer = tif.ScanlineSize()
                Dim result As New Bitmap(width, height, PixelFormat.Format1bppIndexed)

                ' update bitmap palette according to Photometric value
                Dim minIsWhite As Boolean = (photo = Photometric.MINISWHITE)
                Dim palette As ColorPalette = result.Palette
                If minIsWhite Then
                    palette.Entries(0) = Color.White
                    palette.Entries(1) = Color.Black
                Else
                    palette.Entries(0) = Color.Black
                    palette.Entries(1) = Color.White
                End If
                result.Palette = palette

                For i As Integer = 0 To height - 1
                    Dim imgRect As New Rectangle(0, i, width, 1)
                    Dim imgData As BitmapData = result.LockBits(imgRect, ImageLockMode.[WriteOnly], PixelFormat.Format1bppIndexed)

                    Dim buffer As Byte() = New Byte(stride - 1) {}
                    tif.ReadScanline(buffer, i)

                    Marshal.Copy(buffer, 0, imgData.Scan0, buffer.Length)
                    result.UnlockBits(imgData)
                Next

                Return result
            End Using
        End Function
    End Class
End Namespace