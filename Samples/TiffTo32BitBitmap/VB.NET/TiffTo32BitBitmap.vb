Imports System.Drawing
Imports System.Drawing.Imaging

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class TiffTo32BitBitmap
        Private Sub New()
        End Sub
        Public Shared Sub Main()
            Using tif As Tiff = Tiff.Open("Sample data\dscf0013.tif", "r")
                ' Find the width and height of the image
                Dim value As FieldValue() = tif.GetField(TiffTag.IMAGEWIDTH)
                Dim width As Integer = value(0).ToInt()

                value = tif.GetField(TiffTag.IMAGELENGTH)
                Dim height As Integer = value(0).ToInt()

                ' Read the image into the memory buffer
                Dim raster As Integer() = New Integer(height * width - 1) {}
                If Not tif.ReadRGBAImage(width, height, raster) Then
                    System.Windows.Forms.MessageBox.Show("Could not read image")
                    Return
                End If

                Using bmp As New Bitmap(width, height, PixelFormat.Format32bppRgb)
                    Dim rect As New Rectangle(0, 0, bmp.Width, bmp.Height)

                    Dim bmpdata As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb)
                    Dim bits As Byte() = New Byte(bmpdata.Stride * bmpdata.Height - 1) {}

                    For y As Integer = 0 To bmp.Height - 1
                        Dim rasterOffset As Integer = y * bmp.Width
                        Dim bitsOffset As Integer = (bmp.Height - y - 1) * bmpdata.Stride

                        For x As Integer = 0 To bmp.Width - 1
                            Dim rgba As Integer = raster(rasterOffset)
                            rasterOffset = rasterOffset + 1
                            bits(bitsOffset) = CByte((rgba >> 16) And &HFF)
                            bits(bitsOffset + 1) = CByte((rgba >> 8) And &HFF)
                            bits(bitsOffset + 2) = CByte(rgba And &HFF)
                            bits(bitsOffset + 3) = CByte((rgba >> 24) And &HFF)
                            bitsOffset = bitsOffset + 4
                        Next
                    Next

                    System.Runtime.InteropServices.Marshal.Copy(bits, 0, bmpdata.Scan0, bits.Length)
                    bmp.UnlockBits(bmpdata)

                    bmp.Save("TiffTo32BitBitmap.bmp")
                    System.Diagnostics.Process.Start("TiffTo32BitBitmap.bmp")
                End Using
            End Using
        End Sub
    End Class
End Namespace
