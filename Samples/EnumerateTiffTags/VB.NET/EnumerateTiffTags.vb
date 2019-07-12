Imports Microsoft.VisualBasic
Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class EnumerateTiffTags
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Const fileName As String = "Sample data\multipage.tif"

            Using image As Tiff = Tiff.Open(fileName, "r")
                If image Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                Using writer As New StreamWriter("EnumerateTiffTags.txt")
                    Dim numberOfDirectories As Short = image.NumberOfDirectories()
                    For d As Short = 0 To numberOfDirectories - 1
                        If d <> 0 Then
                            writer.WriteLine("---------------------------------")
                        End If

                        image.SetDirectory(CShort(d))

                        writer.WriteLine("Image {0}, page {1} has following tags set:" & vbCrLf, fileName, d)
                        For t As Integer = 0 To 65535
                            Dim tag As TiffTag = DirectCast(t, TiffTag)
                            Dim value As FieldValue() = image.GetField(tag)
                            If value IsNot Nothing Then
                                For j As Integer = 0 To value.Length - 1
                                    writer.WriteLine("{0}", tag.ToString())
                                    writer.WriteLine("{0} : {1}", value(j).Value.[GetType]().ToString(), value(j).ToString())
                                Next

                                writer.WriteLine()
                            End If
                        Next
                    Next
                End Using
            End Using

            Process.Start("EnumerateTiffTags.txt")
        End Sub
    End Class
End Namespace