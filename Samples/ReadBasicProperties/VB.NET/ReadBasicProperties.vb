Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ReadBasicProperties
        Public Shared Sub Main()
            Using image As Tiff = Tiff.Open("Sample Data\pc260001.tif", "r")
                Dim value As FieldValue() = image.GetField(TiffTag.IMAGEWIDTH)
                Dim width As Integer = value(0).ToInt()

                value = image.GetField(TiffTag.IMAGELENGTH)
                Dim height As Integer = value(0).ToInt()

                value = image.GetField(TiffTag.XRESOLUTION)
                Dim dpiX As Single = value(0).ToFloat()

                value = image.GetField(TiffTag.YRESOLUTION)
                Dim dpiY As Single = value(0).ToInt()

                MessageBox.Show(String.Format("Width = {0}, Height = {1}, DPI = {2}x{3}", width, height, dpiX, dpiY), "TIFF properties")
            End Using
        End Sub
    End Class
End Namespace
