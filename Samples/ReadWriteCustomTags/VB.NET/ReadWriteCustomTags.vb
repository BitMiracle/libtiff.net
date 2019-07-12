Imports Microsoft.VisualBasic
Imports System.Diagnostics
Imports System.Text
Imports System.Windows.Forms

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class ReadWriteCustomTags
        Private Sub New()
        End Sub
        Private Const TIFFTAG_ASCIITAG As TiffTag = DirectCast(666, TiffTag)
        Private Const TIFFTAG_LONGTAG As TiffTag = DirectCast(667, TiffTag)
        Private Const TIFFTAG_SHORTTAG As TiffTag = DirectCast(668, TiffTag)
        Private Const TIFFTAG_RATIONALTAG As TiffTag = DirectCast(669, TiffTag)
        Private Const TIFFTAG_FLOATTAG As TiffTag = DirectCast(670, TiffTag)
        Private Const TIFFTAG_DOUBLETAG As TiffTag = DirectCast(671, TiffTag)
        Private Const TIFFTAG_BYTETAG As TiffTag = DirectCast(672, TiffTag)

        Private Shared m_parentExtender As Tiff.TiffExtendProc

        Public Shared Sub TagExtender(ByVal tif As Tiff)
            Dim tiffFieldInfo As TiffFieldInfo() = {New TiffFieldInfo(TIFFTAG_ASCIITAG, -1, -1, TiffType.ASCII, FieldBit.[Custom], True, _
             False, "MyTag"), New TiffFieldInfo(TIFFTAG_SHORTTAG, 2, 2, TiffType.[SHORT], FieldBit.[Custom], False, _
             True, "ShortTag"), New TiffFieldInfo(TIFFTAG_LONGTAG, 2, 2, TiffType.[LONG], FieldBit.[Custom], False, _
             True, "LongTag"), New TiffFieldInfo(TIFFTAG_RATIONALTAG, 2, 2, TiffType.RATIONAL, FieldBit.[Custom], False, _
             True, "RationalTag"), New TiffFieldInfo(TIFFTAG_FLOATTAG, 2, 2, TiffType.FLOAT, FieldBit.[Custom], False, _
             True, "FloatTag"), New TiffFieldInfo(TIFFTAG_DOUBLETAG, 2, 2, TiffType.[DOUBLE], FieldBit.[Custom], False, _
             True, "DoubleTag"), _
             New TiffFieldInfo(TIFFTAG_BYTETAG, 2, 2, TiffType.[BYTE], FieldBit.[Custom], False, _
             True, "ByteTag")}

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length)

            If m_parentExtender IsNot Nothing Then
                m_parentExtender.Invoke(tif)
            End If

        End Sub

        Public Shared Sub Main()
            ' Define an image
            Dim buffer As Byte() = New Byte(25 * 144 - 1) {}

            ' Register the extender callback
            ' It's a good idea to keep track of the previous tag extender (if any) so that we can call it
            ' from our extender allowing a chain of customizations to take effect.
            m_parentExtender = Tiff.SetTagExtender(AddressOf TagExtender)

            Dim outputFileName As String = writeTiffWithCustomTags(buffer)
            readCustomTags(outputFileName)
            
            ' restore previous tag extender
            Tiff.SetTagExtender(m_parentExtender)
        End Sub

        Private Shared Function writeTiffWithCustomTags(ByVal buffer As Byte()) As String
            Dim outputFileName As String = "output.tif"
            Using image As Tiff = Tiff.Open(outputFileName, "w")
                ' set up some basic tags before adding data
                image.SetField(TiffTag.IMAGEWIDTH, 25 * 8)
                image.SetField(TiffTag.IMAGELENGTH, 144)
                image.SetField(TiffTag.BITSPERSAMPLE, 1)
                image.SetField(TiffTag.SAMPLESPERPIXEL, 1)
                image.SetField(TiffTag.ROWSPERSTRIP, 144)

                image.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4)
                image.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISWHITE)
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB)
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG)

                image.SetField(TiffTag.XRESOLUTION, 150.0)
                image.SetField(TiffTag.YRESOLUTION, 150.0)
                image.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH)

                ' set custom tags

                Dim value As String = "Tag contents"
                image.SetField(TIFFTAG_ASCIITAG, value)

                Dim shorts As Short() = {263, 264}
                image.SetField(TIFFTAG_SHORTTAG, 2, shorts)

                Dim longs As Integer() = {117, 118}
                image.SetField(TIFFTAG_LONGTAG, 2, longs)

                Dim rationals As Single() = {0.333333F, 0.444444F}
                image.SetField(TIFFTAG_RATIONALTAG, 2, rationals)

                Dim floats As Single() = {0.666666F, 0.777777F}
                image.SetField(TIFFTAG_FLOATTAG, 2, floats)

                Dim doubles As Double() = {0.1234567, 0.7654321}
                image.SetField(TIFFTAG_DOUBLETAG, 2, doubles)

                Dim bytes As Byte() = {89, 90}
                image.SetField(TIFFTAG_BYTETAG, 2, bytes)

                ' Write the information to the file
                image.WriteEncodedStrip(0, buffer, 25 * 144)
            End Using
            Return outputFileName
        End Function

        Private Shared Sub readCustomTags(ByVal outputFileName As String)
            Const messageFormat As String = "{0} is read ok: {1}" & vbCrLf
            Dim result As New StringBuilder()

            ' Now open that TIFF back and read new tags
            Using image As Tiff = Tiff.Open(outputFileName, "r")
                Dim res As FieldValue() = image.GetField(TIFFTAG_ASCIITAG)
                Dim tagOk As Boolean = (res IsNot Nothing AndAlso res.Length = 1 AndAlso res(0).ToString() = "Tag contents")
                result.AppendFormat(messageFormat, "MyTag", tagOk)

                res = image.GetField(TIFFTAG_SHORTTAG)
                tagOk = (res IsNot Nothing AndAlso res.Length = 2 AndAlso res(0).ToInt() = 2 AndAlso res(1).ToShortArray() IsNot Nothing)
                result.AppendFormat(messageFormat, "ShortTag", tagOk)

                res = image.GetField(TIFFTAG_LONGTAG)
                tagOk = (res IsNot Nothing AndAlso res.Length = 2 AndAlso res(0).ToInt() = 2 AndAlso res(1).ToIntArray() IsNot Nothing)
                result.AppendFormat(messageFormat, "LongTag", tagOk)

                res = image.GetField(TIFFTAG_RATIONALTAG)
                tagOk = (res IsNot Nothing AndAlso res.Length = 2 AndAlso res(0).ToInt() = 2 AndAlso res(1).ToFloatArray() IsNot Nothing)
                result.AppendFormat(messageFormat, "RationalTag", tagOk)

                res = image.GetField(TIFFTAG_FLOATTAG)
                tagOk = (res IsNot Nothing AndAlso res.Length = 2 AndAlso res(0).ToInt() = 2 AndAlso res(1).ToFloatArray() IsNot Nothing)
                result.AppendFormat(messageFormat, "FloatTag", tagOk)

                res = image.GetField(TIFFTAG_DOUBLETAG)
                tagOk = (res IsNot Nothing AndAlso res.Length = 2 AndAlso res(0).ToInt() = 2 AndAlso res(1).ToFloatArray() IsNot Nothing)
                result.AppendFormat(messageFormat, "DoubleTag", tagOk)

                res = image.GetField(TIFFTAG_BYTETAG)
                tagOk = (res IsNot Nothing AndAlso res.Length = 2 AndAlso res(0).ToInt() = 2 AndAlso res(1).ToByteArray() IsNot Nothing)
                result.AppendFormat(messageFormat, "ByteTag", tagOk)
            End Using

            MessageBox.Show(result.ToString())
        End Sub
    End Class
End Namespace