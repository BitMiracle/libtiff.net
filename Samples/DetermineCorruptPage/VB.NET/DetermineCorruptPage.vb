Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class DetermineCorruptPage
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using image As Tiff = Tiff.Open("Sample Data\127.tif", "r")
                If image Is Nothing Then
                    MessageBox.Show("Could not load incoming image")
                    Return
                End If

                Dim numberOfDirectories As Integer = image.NumberOfDirectories()
                For i As Integer = 0 To numberOfDirectories - 1
                    image.SetDirectory(CShort(i))

                    Dim width As Integer = image.GetField(TiffTag.IMAGEWIDTH)(0).ToInt()
                    Dim height As Integer = image.GetField(TiffTag.IMAGELENGTH)(0).ToInt()

                    Dim imageSize As Integer = height * width
                    Dim raster As Integer() = New Integer(imageSize) {}

                    If Not image.ReadRGBAImage(width, height, raster, True) Then
                        MessageBox.Show("Page " + i.ToString() + " is corrupted")
                        Return
                    End If
                Next
            End Using
        End Sub
    End Class
End Namespace