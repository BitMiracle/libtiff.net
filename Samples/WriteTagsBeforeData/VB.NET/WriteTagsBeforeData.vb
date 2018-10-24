Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class WriteTagsBeforeData
        Public Shared Sub Main()
            Dim fileName As String = "random.tif"
            Dim totalPages As Integer = 3
            Dim width As Integer = 100
            Dim height As Integer = 150

            Using output As Tiff = Tiff.Open(fileName, "w")

                For page As Integer = 0 To totalPages - 1
                    If page <> 0 Then
                        ' save previous directory data
                        output.WriteDirectory()

                        ' create new directory and make it current
                        output.CreateDirectory()
                        output.SetDirectory(page)
                    End If

                    ' setup image properties
                    output.SetField(TiffTag.IMAGEWIDTH, width)
                    output.SetField(TiffTag.IMAGELENGTH, height)
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1)
                    output.SetField(TiffTag.BITSPERSAMPLE, 16)
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT)
                    output.SetField(TiffTag.ROWSPERSTRIP, height)
                    output.SetField(TiffTag.XRESOLUTION, 88.0)
                    output.SetField(TiffTag.YRESOLUTION, 88.0)
                    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.CENTIMETER)
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK)
                    output.SetField(TiffTag.COMPRESSION, Compression.NONE)
                    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB)

                    ' cause tags data to be put in an image
                    output.CheckpointDirectory()

                    ' create image data
                    Dim random As New Random()
                    For i As Integer = 0 To height - 1
                        Dim samples As Short() = New Short(width - 1) {}
                        For j As Integer = 0 To width - 1
                            samples(j) = CShort(random.[Next](0, Short.MaxValue))
                        Next

                        Dim buf As Byte() = New Byte(samples.Length * 2 - 1) {}
                        Buffer.BlockCopy(samples, 0, buf, 0, buf.Length)
                        output.WriteScanline(buf, i)
                    Next
                Next
            End Using

            System.Diagnostics.Process.Start(fileName)
        End Sub
    End Class
End Namespace
