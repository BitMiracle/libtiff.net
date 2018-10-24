Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ImageToBitonalTiff
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using bmp As New Bitmap("Sample data\rgb.jpg")
                ' convert using WriteEncodedStrip
                Dim tiffBytes As Byte() = GetTiffImageBytes(bmp, False)
                File.WriteAllBytes("ImageToBitonalTiff.tif", tiffBytes)

                ' make another conversion using WriteScanline
                tiffBytes = GetTiffImageBytes(bmp, True)
                File.WriteAllBytes("ImageToTiff_ByScanlines.tif", tiffBytes)

                Process.Start("ImageToBitonalTiff.tif")
            End Using
        End Sub

        Public Shared Function GetTiffImageBytes(ByVal img As Bitmap, ByVal byScanlines As Boolean) As Byte()
            Try
                Dim raster As Byte() = GetImageRasterBytes(img)

                Using ms As New MemoryStream()
                    Using tif As Tiff = Tiff.ClientOpen("InMemory", "w", ms, New TiffStream())
                        If tif Is Nothing Then
                            Return Nothing
                        End If

                        tif.SetField(TiffTag.IMAGEWIDTH, img.Width)
                        tif.SetField(TiffTag.IMAGELENGTH, img.Height)
                        tif.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4)
                        tif.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK)

                        tif.SetField(TiffTag.ROWSPERSTRIP, img.Height)

                        tif.SetField(TiffTag.XRESOLUTION, img.HorizontalResolution)
                        tif.SetField(TiffTag.YRESOLUTION, img.VerticalResolution)

                        tif.SetField(TiffTag.SUBFILETYPE, 0)
                        tif.SetField(TiffTag.BITSPERSAMPLE, 1)
                        tif.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB)
                        tif.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT)

                        tif.SetField(TiffTag.SAMPLESPERPIXEL, 1)
                        tif.SetField(TiffTag.T6OPTIONS, 0)
                        tif.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH)

                        tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)

                        Dim tiffStride As Integer = tif.ScanlineSize()
                        Dim stride As Integer = raster.Length / img.Height

                        If byScanlines Then
                            ' raster stride MAY be bigger than TIFF stride (due to padding in raster raster)
                            Dim i As Integer = 0, offset As Integer = 0
							While i < img.Height
								Dim res As Boolean = tif.WriteScanline(raster, offset, i, 0)
								If Not res Then
									Return Nothing
								End If

								offset += stride
								i += 1
							End While
                        Else
                            If tiffStride < stride Then
                                ' raster stride is bigger than TIFF stride
                                ' this is due to padding in raster bits
                                ' we need to create correct TIFF strip and write it into TIFF

                                Dim stripBits As Byte() = New Byte(tiffStride * img.Height - 1) {}
                                Dim i As Integer = 0, rasterPos As Integer = 0, stripPos As Integer = 0
                                While i < img.Height
                                    System.Buffer.BlockCopy(raster, rasterPos, stripBits, stripPos, tiffStride)
                                    rasterPos += stride
                                    stripPos += tiffStride
                                    i += 1
                                End While

                                ' Write the information to the file
                                Dim n As Integer = tif.WriteEncodedStrip(0, stripBits, stripBits.Length)
                                If n <= 0 Then
                                    Return Nothing
                                End If
                            Else
                                ' Write the information to the file
                                Dim n As Integer = tif.WriteEncodedStrip(0, raster, raster.Length)
                                If n <= 0 Then
                                    Return Nothing
                                End If
                            End If
                        End If
                    End Using

                    Return ms.GetBuffer()
                End Using
            Catch generatedExceptionName As Exception
                Return Nothing
            End Try
        End Function

        Public Shared Function GetImageRasterBytes(ByVal img As Bitmap) As Byte()
            ' Specify full image
            Dim rect As New Rectangle(0, 0, img.Width, img.Height)

            Dim bmp As Bitmap = img
            Dim bits As Byte() = Nothing

            Try
                ' Lock the managed memory
                If img.PixelFormat <> PixelFormat.Format1bppIndexed Then
                    bmp = convertToBitonal(img)
                End If

                Dim bmpdata As BitmapData = bmp.LockBits(rect, ImageLockMode.[ReadOnly], PixelFormat.Format1bppIndexed)

                ' Declare an array to hold the bytes of the bitmap.
                bits = New Byte(bmpdata.Stride * bmpdata.Height - 1) {}

                ' Copy the sample values into the array.
                Marshal.Copy(bmpdata.Scan0, bits, 0, bits.Length)

                ' Release managed memory
                bmp.UnlockBits(bmpdata)
            Finally
                If Not Object.ReferenceEquals(bmp, img) Then
                    bmp.Dispose()
                End If
            End Try

            Return bits
        End Function

        Private Shared Function convertToBitonal(ByVal original As Bitmap) As Bitmap
            Dim sourceStride As Integer
            Dim sourceBuffer As Byte() = extractBytes(original, sourceStride)

            ' Create destination bitmap
            Dim destination As New Bitmap(original.Width, original.Height, PixelFormat.Format1bppIndexed)

            destination.SetResolution(original.HorizontalResolution, original.VerticalResolution)

            ' Lock destination bitmap in memory
            Dim destinationData As BitmapData = destination.LockBits(New Rectangle(0, 0, destination.Width, destination.Height), ImageLockMode.[WriteOnly], PixelFormat.Format1bppIndexed)

            ' Create buffer for destination bitmap bits
            Dim imageSize As Integer = destinationData.Stride * destinationData.Height
            Dim destinationBuffer As Byte() = New Byte(imageSize - 1) {}

            Dim sourceIndex As Integer = 0
            Dim destinationIndex As Integer = 0
            Dim pixelTotal As Integer = 0
            Dim destinationValue As Byte = 0
            Dim pixelValue As Integer = 128
            Dim height As Integer = destination.Height
            Dim width As Integer = destination.Width
            Dim threshold As Integer = 500

            For y As Integer = 0 To height - 1
                sourceIndex = y * sourceStride
                destinationIndex = y * destinationData.Stride
                destinationValue = 0
                pixelValue = 128

                For x As Integer = 0 To width - 1
                    ' Compute pixel brightness (i.e. total of Red, Green, and Blue values)
                    pixelTotal = CType(sourceBuffer(sourceIndex + 1), Integer) + CType(sourceBuffer(sourceIndex + 2), Integer) + CType(sourceBuffer(sourceIndex + 3), Integer)

                    If pixelTotal > threshold Then
                        destinationValue += CByte(pixelValue)
                    End If

                    If pixelValue = 1 Then
                        destinationBuffer(destinationIndex) = destinationValue
                        destinationIndex += 1
                        destinationValue = 0
                        pixelValue = 128
                    Else
                        pixelValue >>= 1
                    End If

                    sourceIndex += 4
                Next

                If pixelValue <> 128 Then
                    destinationBuffer(destinationIndex) = destinationValue
                End If
            Next

            Marshal.Copy(destinationBuffer, 0, destinationData.Scan0, imageSize)
            destination.UnlockBits(destinationData)
            Return destination
        End Function

        Private Shared Function extractBytes(ByVal original As Bitmap, ByRef stride As Integer) As Byte()
            Dim source As Bitmap = Nothing

            Try
                ' If original bitmap is not already in 32 BPP, ARGB format, then convert
                If original.PixelFormat <> PixelFormat.Format32bppArgb Then
                    source = New Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb)
                    source.SetResolution(original.HorizontalResolution, original.VerticalResolution)
                    Using g As Graphics = Graphics.FromImage(source)
                        g.DrawImageUnscaled(original, 0, 0)
                    End Using
                Else
                    source = original
                End If

                ' Lock source bitmap in memory
                Dim sourceData As BitmapData = source.LockBits(New Rectangle(0, 0, source.Width, source.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)

                ' Copy image data to binary array
                Dim imageSize As Integer = sourceData.Stride * sourceData.Height
                Dim sourceBuffer As Byte() = New Byte(imageSize - 1) {}
                Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, imageSize)

                ' Unlock source bitmap
                source.UnlockBits(sourceData)

                stride = sourceData.Stride
                Return sourceBuffer
            Finally
                If Not Object.ReferenceEquals(source, original) Then
                    source.Dispose()
                End If
            End Try

        End Function
    End Class
End Namespace