Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class UsingCustomTiffStream
        Public Shared Sub Main()
            Dim stream As New MyStream()

            ' Open the TIFF image for reading
            Using image As Tiff = Tiff.ClientOpen("custom", "r", Nothing, stream)
                If image Is Nothing Then
                    Return
                End If

                ' Read image data here the same way
                ' as if LibTiff.Net was using regular image file
                image.Close()
            End Using
        End Sub

        ''' <summary>
        ''' Custom stream for LibTiff.Net.
        ''' Please consult documentation for TiffStream class for method parameters meaning.
        ''' </summary>
        Private Class MyStream
            Inherits TiffStream
            ' You may implement any constructor you want here.

            Public Overrides Function Read(clientData As Object, buffer As Byte(), offset As Integer, count As Integer) As Integer
                ' stub implementation
                Return -1
            End Function

            Public Overrides Sub Write(clientData As Object, buffer As Byte(), offset As Integer, count As Integer)
                ' stub implementation
            End Sub

            Public Overrides Function Seek(clientData As Object, offset As Long, whence As System.IO.SeekOrigin) As Long
                ' stub implementation
                Return -1
            End Function

            Public Overrides Sub Close(clientData As Object)
                ' stub implementation
            End Sub

            Public Overrides Function Size(clientData As Object) As Long
                ' stub implementation
                Return -1
            End Function
        End Class
    End Class
End Namespace
