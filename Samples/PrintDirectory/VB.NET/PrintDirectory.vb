Imports Microsoft
Imports System.Diagnostics
Imports System.IO
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class PrintDirectory
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using image As Tiff = Tiff.Open("Sample Data\multipage.tif", "r")
                If image Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                Dim endOfLine As Byte() = {VisualBasic.AscW(VisualBasic.ControlChars.Cr), VisualBasic.AscW(VisualBasic.ControlChars.Lf)}
                Using stream As New FileStream("PrintDirectory.txt", FileMode.Create)
                    Do
                        image.PrintDirectory(stream)

                        stream.Write(endOfLine, 0, endOfLine.Length)
                    Loop While image.ReadDirectory()
                End Using
            End Using

            Process.Start("PrintDirectory.txt")
        End Sub
    End Class
End Namespace