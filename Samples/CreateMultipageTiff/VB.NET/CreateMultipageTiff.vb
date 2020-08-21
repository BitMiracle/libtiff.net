Imports System
Imports System.Diagnostics

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class CreateMultipageTiff
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Const numberOfPages As Integer = 4

            Const width As Integer = 256
            Const height As Integer = 256
            Const samplesPerPixel As Integer = 1
            Const bitsPerSample As Integer = 8

            Const fileName As String = "CreateMultipageTiff.tif"

            Dim firstPageBuffer As Byte()() = New Byte(height - 1)() {}
            For j As Integer = 0 To height - 1
                firstPageBuffer(j) = New Byte(width - 1) {}
                For i As Integer = 0 To width - 1
                    firstPageBuffer(j)(i) = j * i Mod 256
                Next
            Next

            Using output As Tiff = Tiff.Open(fileName, "w")
                For page As Integer = 0 To numberOfPages - 1
                    output.SetField(TiffTag.IMAGEWIDTH, width / samplesPerPixel)
                    output.SetField(TiffTag.IMAGELENGTH, height)
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                    output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)
                    output.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT)
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)

                    If (page Mod 2 = 0) Then
                        output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK)
                    Else
                        output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISWHITE)
                    End If

                    output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0))
                    output.SetField(TiffTag.XRESOLUTION, 100.0)
                    output.SetField(TiffTag.YRESOLUTION, 100.0)
                    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH)

                    ' specify that it's a page within the multipage file
                    output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE)
                    ' specify the page number
                    output.SetField(TiffTag.PAGENUMBER, page, numberOfPages)

                    For j As Integer = 0 To height - 1
                        output.WriteScanline(firstPageBuffer(j), j)
                    Next

                    output.WriteDirectory()
                Next
            End Using

            Process.Start(fileName)
        End Sub
    End Class
End Namespace