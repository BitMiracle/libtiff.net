Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class RotateTiff
        Public Shared Sub Main()
            Dim rotateAngles As Integer() = New Integer() {90, 180, 270}

            For angleIndex As Integer = 0 To rotateAngles.Length - 1
                Dim outputFileName As String = String.Format("Rotated-{0}-degrees.tif", rotateAngles(angleIndex))

                Using input As Tiff = Tiff.Open("Sample Data\flag_t24.tif", "r")
                    Using output As Tiff = Tiff.Open(outputFileName, "w")
                        For page As Short = 0 To input.NumberOfDirectories() - 1
                            input.SetDirectory(page)
                            output.SetDirectory(page)

                            Dim width As Integer = input.GetField(TiffTag.IMAGEWIDTH)(0).ToInt()
                            Dim height As Integer = input.GetField(TiffTag.IMAGELENGTH)(0).ToInt()
                            Dim samplesPerPixel As Integer = input.GetField(TiffTag.SAMPLESPERPIXEL)(0).ToInt()
                            Dim bitsPerSample As Integer = input.GetField(TiffTag.BITSPERSAMPLE)(0).ToInt()
                            Dim photo As Integer = input.GetField(TiffTag.PHOTOMETRIC)(0).ToInt()

                            Dim raster As Integer() = New Integer(width * height - 1) {}
                            input.ReadRGBAImageOriented(width, height, raster, Orientation.TOPLEFT)

                            raster = rotate(raster, rotateAngles(angleIndex), width, height)

                            output.SetField(TiffTag.IMAGEWIDTH, width)
                            output.SetField(TiffTag.IMAGELENGTH, height)
                            output.SetField(TiffTag.SAMPLESPERPIXEL, 3)
                            output.SetField(TiffTag.BITSPERSAMPLE, 8)
                            output.SetField(TiffTag.ROWSPERSTRIP, height)
                            output.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB)
                            output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                            output.SetField(TiffTag.COMPRESSION, Compression.DEFLATE)

                            Dim strip As Byte() = rasterToRgbBuffer(raster)
                            output.WriteEncodedStrip(0, strip, strip.Length)

                            output.WriteDirectory()
                        Next
                    End Using
                End Using

                Process.Start(outputFileName)
            Next
        End Sub

        Private Shared Function rasterToRgbBuffer(raster As Integer()) As Byte()
            Dim buf As Byte() = New Byte(raster.Length * 3 - 1) {}
            For i As Integer = 0 To raster.Length - 1
                Buffer.BlockCopy(raster, i * 4, buf, i * 3, 3)
            Next

            Return buf
        End Function

        Private Shared Function rotate(buffer As Integer(), angle As Integer, ByRef width As Integer, ByRef height As Integer) As Integer()
            Dim rotatedWidth As Integer = width
            Dim rotatedHeight As Integer = height
            Dim numberOf90s As Integer = angle \ 90
            If numberOf90s Mod 2 <> 0 Then
                Dim tmp As Integer = rotatedWidth
                rotatedWidth = rotatedHeight
                rotatedHeight = tmp
            End If

            Dim rotated As Integer() = New Integer(rotatedWidth * rotatedHeight - 1) {}

            For h As Integer = 0 To height - 1
                For w As Integer = 0 To width - 1
                    Dim item As Integer = buffer(h * width + w)
                    Dim x As Integer = 0
                    Dim y As Integer = 0
                    Select Case numberOf90s Mod 4
                        Case 0
                            x = w
                            y = h
                            Exit Select

                        Case 1
                            x = (height - h - 1)
                            y = (rotatedHeight - 1) - (width - w - 1)
                            Exit Select

                        Case 2
                            x = (width - w - 1)
                            y = (height - h - 1)

                            Exit Select

                        Case 3
                            x = (rotatedWidth - 1) - (height - h - 1)
                            y = (width - w - 1)
                            Exit Select
                    End Select

                    rotated(y * rotatedWidth + x) = item
                Next
            Next

            width = rotatedWidth
            height = rotatedHeight
            Return rotated
        End Function
    End Class
End Namespace
