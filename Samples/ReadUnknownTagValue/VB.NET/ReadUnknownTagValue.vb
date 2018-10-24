Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ReadUnknownTagValue
        Public Shared Sub Main()
            Using image As Tiff = Tiff.Open("Sample Data\pc260001.tif", "r")
                ' read auto-registered tag 50341
                Dim value As FieldValue() = image.GetField(DirectCast(50341, TiffTag))
                System.Console.Out.WriteLine("Tag value(s) are as follows:")
                For i As Integer = 0 To value.Length - 1
                    System.Console.Out.WriteLine("{0} : {1}", i, value(i).ToString())
                Next
            End Using
        End Sub
    End Class
End Namespace
