Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class AddPageToTiff
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            File.Copy("Sample Data\16bit.tif", "Sample Data\ToBeAppended.tif", True)

            Using image As Tiff = Tiff.Open("Sample Data\ToBeAppended.tif", "a")
                Dim newPageNumber As Integer = image.NumberOfDirectories() + 1
                Const width As Integer = 100
                Const height As Integer = 100

                image.SetField(TiffTag.IMAGEWIDTH, width)
                image.SetField(TiffTag.IMAGELENGTH, height)
                image.SetField(TiffTag.BITSPERSAMPLE, 8)
                image.SetField(TiffTag.SAMPLESPERPIXEL, 3)
                image.SetField(TiffTag.ROWSPERSTRIP, height)

                image.SetField(TiffTag.COMPRESSION, Compression.LZW)
                image.SetField(TiffTag.PHOTOMETRIC, Photometric.RGB)
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB)
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)

                Dim buffer As Byte() = Nothing
                Using bmp As New Bitmap(width, height, PixelFormat.Format24bppRgb)
                    Using g As Graphics = Graphics.FromImage(bmp)
                        g.FillRectangle(Brushes.White, g.VisibleClipBounds)
                        Dim s As String = newPageNumber.ToString()
                        Dim f As Font = SystemFonts.DefaultFont

                        Dim size As SizeF = g.MeasureString(s, f)
                        Dim loc As New PointF(Math.Max((bmp.Width - size.Width) / 2, 0), Math.Max((bmp.Height - size.Height) / 2, 0))
                        g.DrawString(s, f, Brushes.Black, loc)

                        buffer = getImageRasterBytes(bmp, PixelFormat.Format24bppRgb)
                    End Using
                End Using

                Dim stride As Integer = buffer.Length \ height
                convertRGBSamples(buffer, width, height)

                Dim i As Integer = 0, offset As Integer = 0
                While i < height
                    image.WriteScanline(buffer, offset, i, 0)
                    offset += stride
                    i += 1
                End While
            End Using

            Process.Start("Sample Data\ToBeAppended.tif")
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
        ''' Converts BGR samples into RGB samples
        ''' </summary>
        Private Shared Sub convertRGBSamples(ByVal data As Byte(), ByVal width As Integer, ByVal height As Integer)
            Dim stride As Integer = data.Length \ height
            Const samplesPerPixel As Integer = 3

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
