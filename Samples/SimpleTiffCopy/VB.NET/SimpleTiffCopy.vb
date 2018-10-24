Imports System.Diagnostics

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class SimpleTiffCopy
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using input As Tiff = Tiff.Open("Sample Data\flag_t24.tif", "r")
                Dim width As Integer = input.GetField(TiffTag.IMAGEWIDTH)(0).ToInt()
                Dim height As Integer = input.GetField(TiffTag.IMAGELENGTH)(0).ToInt()
                Dim samplesPerPixel As Integer = input.GetField(TiffTag.SAMPLESPERPIXEL)(0).ToInt()
                Dim bitsPerSample As Integer = input.GetField(TiffTag.BITSPERSAMPLE)(0).ToInt()
                Dim photo As Integer = input.GetField(TiffTag.PHOTOMETRIC)(0).ToInt()

                Dim scanlineSize As Integer = input.ScanlineSize()
                Dim buffer As Byte()() = New Byte(height - 1)() {}
                For i As Integer = 0 To height - 1
                    buffer(i) = New Byte(scanlineSize - 1) {}
                    input.ReadScanline(buffer(i), i)
                Next

                Using output As Tiff = Tiff.Open("SimpleTiffCopy.tif", "w")
                    output.SetField(TiffTag.IMAGEWIDTH, width)
                    output.SetField(TiffTag.IMAGELENGTH, height)
                    output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel)
                    output.SetField(TiffTag.BITSPERSAMPLE, bitsPerSample)
                    output.SetField(TiffTag.ROWSPERSTRIP, output.DefaultStripSize(0))
                    output.SetField(TiffTag.PHOTOMETRIC, photo)
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)

                    ' change orientation of the image
                    output.SetField(TiffTag.ORIENTATION, Orientation.RIGHTBOT)

                    For i As Integer = 0 To height - 1
                        output.WriteScanline(buffer(i), i)
                    Next
                End Using
            End Using

            Process.Start("SimpleTiffCopy.tif")
        End Sub
    End Class
End Namespace