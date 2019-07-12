Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class Process16BitSamples
        Public Shared Sub Main()
            Using tif As Tiff = Tiff.Open("Sample Data\16bit-lzw.tif", "r")
                Dim width As Integer = tif.GetField(TiffTag.IMAGEWIDTH)(0).ToInt()
                Dim height As Integer = tif.GetField(TiffTag.IMAGELENGTH)(0).ToInt()
                Dim dpiX As Double = tif.GetField(TiffTag.XRESOLUTION)(0).ToDouble()
                Dim dpiY As Double = tif.GetField(TiffTag.YRESOLUTION)(0).ToDouble()

                Dim scanline As Byte() = New Byte(tif.ScanlineSize() - 1) {}
                Dim scanline16Bit As UShort() = New UShort(tif.ScanlineSize() / 2 - 1) {}

                Using output As Tiff = Tiff.Open("processed.tif", "w")
                    If output Is Nothing Then
                        Return
                    End If

                    output.SetField(TiffTag.IMAGEWIDTH, width)
                    output.SetField(TiffTag.IMAGELENGTH, height)
                    output.SetField(TiffTag.BITSPERSAMPLE, 16)
                    output.SetField(TiffTag.SAMPLESPERPIXEL, 1)
                    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK)
                    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)
                    output.SetField(TiffTag.ROWSPERSTRIP, 1)
                    output.SetField(TiffTag.COMPRESSION, Compression.LZW)

                    For i As Integer = 0 To height - 1
                        tif.ReadScanline(scanline, i)
                        MultiplyScanLineAs16BitSamples(scanline, scanline16Bit, 16)
                        output.WriteScanline(scanline, i)
                    Next
                End Using

                Process.Start("processed.tif")
            End Using
        End Sub

        Private Shared Sub MultiplyScanLineAs16BitSamples(scanline As Byte(), temp As UShort(), factor As UShort)
            If scanline.Length Mod 2 <> 0 Then
                ' each two bytes define one sample so there should be even number of bytes
                Throw New ArgumentException()
            End If

            ' pack all bytes to ushorts
            Buffer.BlockCopy(scanline, 0, temp, 0, scanline.Length)

            For i As Integer = 0 To temp.Length - 1
                temp(i) *= factor
            Next

            ' unpack all ushorts to bytes
            Buffer.BlockCopy(temp, 0, scanline, 0, scanline.Length)
        End Sub
    End Class
End Namespace
