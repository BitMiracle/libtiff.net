Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ReadExifMetadata
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using image As Tiff = Tiff.Open("Sample data\dscf0013.tif", "r")
                If image Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                Dim exifIFDTag As FieldValue() = image.GetField(TiffTag.EXIFIFD)
                If exifIFDTag Is Nothing Then
                    MessageBox.Show("Exif metadata not found")
                    Return
                End If

                Dim exifIFDOffset As Integer = exifIFDTag(0).ToInt()
                If Not image.ReadEXIFDirectory(exifIFDOffset) Then
                    MessageBox.Show("Could not read EXIF IFD")
                    Return
                End If

                Using writer As New StreamWriter("ReadExifMetadata.txt")
                    For tag As TiffTag = TiffTag.EXIF_EXPOSURETIME To TiffTag.EXIF_IMAGEUNIQUEID
                        Dim value As FieldValue() = image.GetField(tag)
                        If value IsNot Nothing Then
                            For i As Integer = 0 To value.Length - 1
                                writer.WriteLine("{0}", tag.ToString())
                                writer.WriteLine("{0} : {1}", value(i).Value.[GetType]().ToString(), value(i).ToString())
                            Next

                            writer.WriteLine()
                        End If
                    Next
                End Using
            End Using

            Process.Start("ReadExifMetadata.txt")
        End Sub
    End Class
End Namespace