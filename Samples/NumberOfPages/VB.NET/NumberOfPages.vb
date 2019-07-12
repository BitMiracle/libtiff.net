Imports Microsoft.VisualBasic
Imports System
Imports System.Text
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class NumberOfPages
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Const fileName As String = "Sample Data/multipage.tif"

            Using image As Tiff = Tiff.Open(fileName, "r")
                If image Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                Dim message As New StringBuilder()
                message.AppendFormat("Tiff.NumberOfDirectories() returns {0} pages" & vbCrLf, image.NumberOfDirectories())
                message.AppendFormat("Enumerated {0} pages", CalculatePageNumber(image))

                MessageBox.Show(message.ToString())
            End Using
        End Sub

        Private Shared Function CalculatePageNumber(ByVal image As Tiff) As Integer
            Dim pageCount As Integer = 0
            Do
                pageCount += 1
            Loop While image.ReadDirectory()

            Return pageCount
        End Function
    End Class
End Namespace