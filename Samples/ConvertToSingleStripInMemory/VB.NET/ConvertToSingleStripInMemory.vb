Imports System.Diagnostics
Imports System.Windows.Forms
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ConvertToSingleStripInMemory
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Dim inputBytes As Byte() = File.ReadAllBytes("Sample Data\multipage.tif")
            Dim byteStream As New TiffStreamForBytes(inputBytes)

            Using input As Tiff = Tiff.ClientOpen("bytes", "r", Nothing, byteStream)
                If input Is Nothing Then
                    MessageBox.Show("Could not open incoming image")
                    Return
                End If

                If input.IsTiled() Then
                    MessageBox.Show("Could not process tiled image")
                    Return
                End If

                Using ms As New MemoryStream()
                    Using output As Tiff = Tiff.ClientOpen("InMemory", "w", ms, New TiffStream())
                        Dim numberOfDirectories As Integer = input.NumberOfDirectories()
                        For i As Short = 0 To numberOfDirectories - 1
                            input.SetDirectory(i)

                            copyTags(input, output)
                            copyStrips(input, output)

                            output.WriteDirectory()
                        Next
                    End Using

                    ' retrieve bytes from memory stream and write them in a file
                    Dim bytes As Byte() = ms.ToArray()
                    File.WriteAllBytes("SavedBytes.tif", bytes)
                End Using
            End Using

            Using result As Tiff = Tiff.Open("SavedBytes.tif", "rc")
                MessageBox.Show("Number of strips in result file: " & result.NumberOfStrips())
            End Using

            Process.Start("SavedBytes.tif")
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

    ''' <summary>
    ''' Custom read-only stream for byte buffer that can be used
    ''' with Tiff.ClientOpen method.
    ''' </summary>
    Class TiffStreamForBytes
        Inherits TiffStream
        Private m_bytes As Byte()
        Private m_position As Integer

        Public Sub New(ByVal bytes As Byte())
            m_bytes = bytes
            m_position = 0
        End Sub

        Public Overrides Function Read(ByVal clientData As Object, ByVal buffer__1 As Byte(), ByVal offset As Integer, ByVal count As Integer) As Integer
            If (m_position + count) > m_bytes.Length Then
                Return -1
            End If

            Buffer.BlockCopy(m_bytes, m_position, buffer__1, offset, count)
            m_position += count
            Return count
        End Function

        Public Overrides Sub Write(ByVal clientData As Object, ByVal buffer As Byte(), ByVal offset As Integer, ByVal count As Integer)
            Throw New InvalidOperationException("This stream is read-only")
        End Sub

        Public Overrides Function Seek(ByVal clientData As Object, ByVal offset As Long, ByVal origin As SeekOrigin) As Long
            Select Case origin
                Case SeekOrigin.Begin
                    If offset > m_bytes.Length Then
                        Return -1
                    End If

                    m_position = CInt(offset)
                    Return m_position

                Case SeekOrigin.Current
                    If (offset + m_position) > m_bytes.Length Then
                        Return -1
                    End If

                    m_position += CInt(offset)
                    Return m_position

                Case SeekOrigin.[End]
                    If (m_bytes.Length - offset) < 0 Then
                        Return -1
                    End If

                    m_position = CInt(m_bytes.Length - offset)
                    Return m_position
            End Select

            Return -1
        End Function

        Public Overrides Sub Close(ByVal clientData As Object)
            ' nothing to do
        End Sub

        Public Overrides Function Size(ByVal clientData As Object) As Long
            Return m_bytes.Length
        End Function
    End Class
End Namespace
