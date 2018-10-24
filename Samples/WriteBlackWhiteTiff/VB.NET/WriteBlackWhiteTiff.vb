Imports System
Imports System.Diagnostics

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class WriteBlackWhiteTiff
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Const width As Integer = 100
            Const height As Integer = 150
            Const fileName As String = "WriteBlackWhiteTiff.tif"

            Using output As Tiff = Tiff.Open(fileName, "w")
                output.SetField(TiffTag.IMAGEWIDTH, width)
                output.SetField(TiffTag.IMAGELENGTH, height)
                output.SetField(TiffTag.SAMPLESPERPIXEL, 1)
                output.SetField(TiffTag.BITSPERSAMPLE, 8)
                output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT)
                output.SetField(TiffTag.ROWSPERSTRIP, height)
                output.SetField(TiffTag.XRESOLUTION, 88.0)
                output.SetField(TiffTag.YRESOLUTION, 88.0)
                output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH)
                output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK)
                output.SetField(TiffTag.COMPRESSION, Compression.NONE)
                output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB)

                Dim random As New Random()
                For i As Integer = 0 To height - 1
                    Dim buf As Byte() = New Byte(width - 1) {}
                    For j As Integer = 0 To width - 1
                        buf(j) = CByte(random.[Next](255))
                    Next

                    output.WriteScanline(buf, i)
                Next
            End Using

            Process.Start(fileName)
        End Sub
    End Class
End Namespace