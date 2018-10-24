Imports System.Diagnostics

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class TiffWithColorMap
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Const numberOfColors As Integer = 256
            Const width As Integer = 32
            Const height As Integer = 100
            Const samplesPerPixel As Integer = 1
            Const bitsPerSample As Integer = 8
            Const fileName As String = "TiffWithColorMap.tif"

            Using output As Tiff = Tiff.Open(fileName, "w")
                output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)
                output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT)
                output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                output.SetField(TiffTag.PHOTOMETRIC, Photometric.PALETTE)
                output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0))

                ' it is good idea to specify resolution too (but it is not necessary)
                output.SetField(TiffTag.XRESOLUTION, 100.0)
                output.SetField(TiffTag.YRESOLUTION, 100.0)
                output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH)

                ' compression is optional
                output.SetField(TiffTag.COMPRESSION, Compression.ADOBE_DEFLATE)

                ' fill color tables
                Dim redTable As UShort() = New UShort((1 << bitsPerSample) - 1) {}
                Dim greenTable As UShort() = New UShort((1 << bitsPerSample) - 1) {}
                Dim blueTable As UShort() = New UShort((1 << bitsPerSample) - 1) {}
                For i As Integer = 0 To numberOfColors - 1
                    redTable(i) = CUShort(100 * i)
                    greenTable(i) = CUShort(150 * i)
                    blueTable(i) = CUShort(200 * i)
                Next
                output.SetField(TiffTag.COLORMAP, redTable, greenTable, blueTable)

                ' fill samples array
                Dim buffer As Byte()() = New Byte(height - 1)() {}
                For j As Integer = 0 To height - 1
                    buffer(j) = New Byte(width - 1) {}
                    For i As Integer = 0 To width - 1
                        buffer(j)(i) = (j * width + i) Mod 256
                    Next
                Next

                For j As Integer = 0 To height - 1
                    output.WriteScanline(buffer(j), j)
                Next
            End Using

            Process.Start(fileName)
        End Sub
    End Class
End Namespace