Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ReadSamples
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            ' Open the TIFF image
            Using image As Tiff = Tiff.Open("Sample Data\marbles.tif", "r")
                If image Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                ' Find the width and height of the image
                Dim value As FieldValue() = image.GetField(TiffTag.IMAGEWIDTH)
                Dim width As Integer = value(0).ToInt()

                value = image.GetField(TiffTag.IMAGELENGTH)
                Dim height As Integer = value(0).ToInt()

                Dim imageSize As Integer = height * width
                Dim raster As Integer() = New Integer(imageSize - 1) {}

                ' Read the image into the memory buffer
                If Not image.ReadRGBAImage(width, height, raster) Then
                    MessageBox.Show("Could not read image")
                    Return
                End If

                Using bmp As New Bitmap(200, 200)
                    For i As Integer = 0 To bmp.Width - 1
                        For j As Integer = 0 To bmp.Height - 1
                            bmp.SetPixel(i, j, getSample(i + 330, j + 30, raster, width, height))
                        Next
                    Next

                    bmp.Save("ReadSamples.bmp")

                End Using
            End Using

            Process.Start("ReadSamples.bmp")
        End Sub

        Private Shared Function getSample(ByVal x As Integer, ByVal y As Integer, ByVal raster As Integer(), ByVal width As Integer, ByVal height As Integer) As Color
            Dim offset As Integer = (height - y - 1) * width + x
            Dim red As Integer = Tiff.GetR(raster(offset))
            Dim green As Integer = Tiff.GetG(raster(offset))
            Dim blue As Integer = Tiff.GetB(raster(offset))
            Return Color.FromArgb(red, green, blue)
        End Function
    End Class
End Namespace