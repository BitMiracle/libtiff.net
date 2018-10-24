Imports System
Imports System.Diagnostics
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ConvertToSingleStrip
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using input As Tiff = Tiff.Open("Sample Data\multipage.tif", "r")
                If input Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                If input.IsTiled() Then
                    MessageBox.Show("Could not process tiled image")
                    Return
                End If

                Using output As Tiff = Tiff.Open("ConvertToSingleStrip.tif", "w")
                    Dim numberOfDirectories As Integer = input.NumberOfDirectories()
                    For i As Short = 0 To numberOfDirectories - 1
                        input.SetDirectory(i)

                        copyTags(input, output)
                        copyStrips(input, output)

                        output.WriteDirectory()
                    Next
                End Using
            End Using

            Using result As Tiff = Tiff.Open("ConvertToSingleStrip.tif", "rc")
                MessageBox.Show("Number of strips in result file: " + result.NumberOfStrips().ToString())
            End Using

            Process.Start("ConvertToSingleStrip.tif")
        End Sub

        Private Shared Sub copyTags(ByVal input As Tiff, ByVal output As Tiff)
            For t As Integer = 0 To 65535
                Dim tag As TiffTag = DirectCast(t, TiffTag)
                Dim tagValue As FieldValue() = input.GetField(tag)
                If tagValue IsNot Nothing Then
                    output.GetTagMethods().SetField(output, tag, tagValue)
                End If
            Next

            Dim height As Integer = input.GetField(TiffTag.IMAGELENGTH)(0).ToInt()
            output.SetField(TiffTag.ROWSPERSTRIP, height)
        End Sub

        Private Shared Sub copyStrips(ByVal input As Tiff, ByVal output As Tiff)
            Dim encoded As Boolean = False
            Dim compressionTagValue As FieldValue() = input.GetField(TiffTag.COMPRESSION)
            If compressionTagValue IsNot Nothing Then
                encoded = (compressionTagValue(0).ToInt() <> CInt(Compression.NONE))
            End If

            Dim numberOfStrips As Integer = input.NumberOfStrips()

            Dim offset As Integer = 0
            Dim stripsData As Byte() = New Byte(numberOfStrips * input.StripSize() - 1) {}
            For i As Integer = 0 To numberOfStrips - 1
                Dim bytesRead As Integer = readStrip(input, i, stripsData, offset, encoded)
                offset += bytesRead
            Next

            writeStrip(output, stripsData, offset, encoded)
        End Sub

        Private Shared Function readStrip(ByVal image As Tiff, ByVal stripNumber As Integer, ByVal buffer As Byte(), ByVal offset As Integer, ByVal encoded As Boolean) As Integer
            If encoded Then
                Return image.ReadEncodedStrip(stripNumber, buffer, offset, buffer.Length - offset)
            Else
                Return image.ReadRawStrip(stripNumber, buffer, offset, buffer.Length - offset)
            End If
        End Function

        Private Shared Sub writeStrip(ByVal image As Tiff, ByVal stripsData As Byte(), ByVal count As Integer, ByVal encoded As Boolean)
            If encoded Then
                image.WriteEncodedStrip(0, stripsData, count)
            Else
                image.WriteRawStrip(0, stripsData, count)
            End If
        End Sub
    End Class
End Namespace