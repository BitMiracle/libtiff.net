Function getCommandOutput(theCommand)

    Dim objShell, objCmdExec
    Set objShell = CreateObject("WScript.Shell")
    Set objCmdExec = objshell.exec(thecommand)
    getCommandOutput = objCmdExec.StdOut.ReadAll
end Function

'Read text file
function GetFile(FileName)
  If FileName<>"" Then
    Dim FS, FileStream
    Set FS = CreateObject("Scripting.FileSystemObject")
      on error resume Next
      Set FileStream = FS.OpenTextFile(FileName)
      GetFile = FileStream.ReadAll
  End If
End Function

'Write string As a text file.
function WriteFile(FileName, Contents)
  Dim OutStream, FS

  on error resume Next
  Set FS = CreateObject("Scripting.FileSystemObject")
    Set OutStream = FS.OpenTextFile(FileName, 2, True)
    OutStream.Write Contents
End Function

InputFile = WScript.Arguments(0)
OutputFile = WScript.Arguments(1)

revCount = getCommandOutput("git rev-list HEAD --count")
IF (IsNumeric(revCount)) THEN 
	revCount = Replace(revCount, vbCr, "")
	revCount = Replace(revCount, vbLf, "")

	'Wscript.Echo revCount

	'Read source text file
	FileContents = GetFile(InputFile)

	ReplacedContents = Replace(FileContents, "$REVNUM$", revCount, 1, -1, 1)

	WriteFile OutputFile, ReplacedContents
ELSE
	WriteFile OutputFile, ""
END IF

	

