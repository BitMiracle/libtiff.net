Imports System
Imports System.Diagnostics

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class CreateGradientTiff
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using tif As Tiff = Tiff.Open("CreateGradientTiff.tif", "w")
                If tif Is Nothing Then
                    Return
                End If

                tif.SetField(TiffTag.IMAGEWIDTH, 256)
                tif.SetField(TiffTag.IMAGELENGTH, 256)
                tif.SetField(TiffTag.BITSPERSAMPLE, 8)
                tif.SetField(TiffTag.SAMPLESPERPIXEL, 3)
                tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB)
                tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                tif.SetField(TiffTag.ROWSPERSTRIP, 1)

                Dim color_ptr As Byte() = New Byte(256 * 3 - 1) {}
                For i As Integer = 0 To 255
                    For j As Integer = 0 To 255
                        color_ptr(j * 3 + 0) = CByte(i)
                        color_ptr(j * 3 + 1) = CByte(i)
                        color_ptr(j * 3 + 2) = CByte(i)
                    Next
                    tif.WriteScanline(color_ptr, i)
                Next

                tif.FlushData()
                tif.Close()
            End Using

            Process.Start("CreateGradientTiff.tif")
        End Sub
    End Class
End Namespace