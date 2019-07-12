Imports System.Drawing
Imports System.Drawing.Imaging

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class BitmapTo32BitColorTiff
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using bmp As New Bitmap("Sample Data\rgb.jpg")
                Using tif As Tiff = Tiff.Open("BitmapTo32BitColorTiff.tif", "w")
                    Dim raster As Byte() = getImageRasterBytes(bmp, PixelFormat.Format32bppArgb)
                    tif.SetField(TiffTag.IMAGEWIDTH, bmp.Width)
                    tif.SetField(TiffTag.IMAGELENGTH, bmp.Height)
                    tif.SetField(TiffTag.COMPRESSION, Compression.LZW)
                    tif.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB)

                    tif.SetField(TiffTag.ROWSPERSTRIP, bmp.Height)

                    tif.SetField(TiffTag.XRESOLUTION, bmp.HorizontalResolution)
                    tif.SetField(TiffTag.YRESOLUTION, bmp.VerticalResolution)

                    tif.SetField(TiffTag.BITSPERSAMPLE, 8)
                    tif.SetField(TiffTag.SAMPLESPERPIXEL, 4)

                    tif.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                    tif.SetField(TiffTag.EXTRASAMPLES, 1, New Short() {CType(ExtraSample.UNASSALPHA, Short)})

                    Dim stride As Integer = raster.Length \ bmp.Height
                    convertSamples(raster, bmp.Width, bmp.Height)

                    Dim i As Integer = 0, offset As Integer = 0
                    While i < bmp.Height
                        tif.WriteScanline(raster, offset, i, 0)
                        offset += stride
                        i += 1
                    End While
                End Using

                System.Diagnostics.Process.Start("BitmapTo32BitColorTiff.tif")
            End Using
        End Sub

        Private Shared Function getImageRasterBytes(ByVal bmp As Bitmap, ByVal format As PixelFormat) As Byte()
            Dim rect As New Rectangle(0, 0, bmp.Width, bmp.Height)
            Dim bits As Byte() = Nothing

            Try
                ' Lock the managed memory
                Dim bmpdata As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, format)

                ' Declare an array to hold the bytes of the bitmap.
                bits = New Byte(bmpdata.Stride * bmpdata.Height - 1) {}

                ' Copy the values into the array.
                System.Runtime.InteropServices.Marshal.Copy(bmpdata.Scan0, bits, 0, bits.Length)

                ' Release managed memory
                bmp.UnlockBits(bmpdata)
            Catch
                Return Nothing
            End Try

            Return bits
        End Function

        ''' <summary>
        ''' Converts BGRA samples into RGBA samples
        ''' </summary>
        Private Shared Sub convertSamples(ByVal data As Byte(), ByVal width As Integer, ByVal height As Integer)
            Dim stride As Integer = data.Length \ height
            Const samplesPerPixel As Integer = 4

            For y As Integer = 0 To height - 1
                Dim offset As Integer = stride * y
                Dim strideEnd As Integer = offset + width * samplesPerPixel

                Dim i As Integer = offset
                While i < strideEnd
                    Dim temp As Byte = data(i + 2)
                    data(i + 2) = data(i)
                    data(i) = temp
                    i += samplesPerPixel
                End While
            Next
        End Sub
    End Class
End Namespace
