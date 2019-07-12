Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO

Imports BitMiracle.LibTiff.Classic

Namespace BitMiracle.LibTiff.Samples
    Public NotInheritable Class AddCustomTagsToExistingTiff
        Private Sub New()
        End Sub
        Private Const TIFFTAG_GDAL_METADATA As TiffTag = DirectCast(42112, TiffTag)

        Private Shared m_parentExtender As Tiff.TiffExtendProc

        Public Shared Sub TagExtender(tif As Tiff)
            Dim tiffFieldInfo As TiffFieldInfo() = {New TiffFieldInfo(TIFFTAG_GDAL_METADATA, -1, -1, TiffType.ASCII, FieldBit.[Custom], True, _
             False, "GDALMetadata")}

            tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length)

            If m_parentExtender IsNot Nothing Then
                m_parentExtender.Invoke(tif)
            End If
        End Sub

        Public Shared Sub Main()
            ' Register the extender callback
            ' It's a good idea to keep track of the previous tag extender (if any) so that we can call it
            ' from our extender allowing a chain of customizations to take effect.
            m_parentExtender = Tiff.SetTagExtender(AddressOf TagExtender)

            File.Copy("Sample Data\dummy.tif", "Sample Data\ToBeModifed.tif", True)

            Dim existingTiffName As String = "Sample Data\ToBeModifed.tif"
            Using image As Tiff = Tiff.Open(existingTiffName, "a")
                ' we should rewind to first directory (first image) because of append mode
                image.SetDirectory(0)

                ' set the custom tag 
                Dim value As String = "<GDALMetadata>" & vbLf & "<Item name=""IMG_GUID"">" & "817C0168-0688-45CD-B799-CF8C4DE9AB2B</Item>" & vbLf & "<Item" & " name=""LAYER_TYPE"" sample=""0"">athematic</Item>" & vbLf & "</GDALMetadata>"
                image.SetField(TIFFTAG_GDAL_METADATA, value)

                ' rewrites directory saving new tag
                image.CheckpointDirectory()
            End Using

            ' restore previous tag extender
            Tiff.SetTagExtender(m_parentExtender)
        End Sub
    End Class
End Namespace
