param (
    [string]$Platform
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Verify that $Platform is either 'x64' or 'arm64'
if ($Platform -ne 'x64' -and $Platform -ne 'arm64') {
    throw "Platform must be either 'x64' or 'arm64'."
}

Write-Output "=== PHASE 2: Generate portable ZIP and MSI ==="

$wixDir = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
if (-not (Test-Path $wixDir)) {
    throw "WiX not found!"
}

$ps1Dir = $PSScriptRoot
$rootDir = (Resolve-Path (Join-Path $ps1Dir "..\")).Path
$srcDir = Join-Path $rootDir "src"
$binDir = Join-Path $srcDir "SqlNotebook\bin"
$relDir = Join-Path $srcDir "SqlNotebook\bin\publish"
$tempWxsFilePath = "$srcDir\SqlNotebook\bin\temp.wxs"

# Verify that Phase 1 was completed
if (-not (Test-Path "$relDir\SqlNotebook.exe")) {
    throw "Phase 1 must be completed first. SqlNotebook.exe not found in $relDir"
}

Push-Location $relDir

$msiFilename = "SQLNotebook.msi"
$msiFilePath = "$binDir\$msiFilename"
$zipFilePath = "$binDir\SQLNotebook.zip"

rm "$relDir\$msiFilename" -ErrorAction SilentlyContinue
rm $zipFilePath -ErrorAction SilentlyContinue
rm "$relDir\SqlNotebook.wixobj" -ErrorAction SilentlyContinue
rm "$relDir\SqlNotebook.wxs" -ErrorAction SilentlyContinue

Copy-Item -Force "$srcdir\SqlNotebook\SqlNotebookIcon.ico" "$relDir\SqlNotebookIcon.ico"

#
# Generate portable ZIP
#

Write-Output "Generating portable ZIP..."
[System.IO.Compression.ZipFile]::CreateFromDirectory($relDir, $zipFilePath)

#
# Generate MSI
#

Write-Output "Generating MSI..."

# Replace <!--FILES--> in the wxs file with <File> entries
$filesXml = ""
$refsXml = ""

& "$wixDir\heat.exe" dir . -o "$tempWxsFilePath" -cg SqlNotebookComponentGroup -sfrag -gg -g1 -sreg -svb6 -scom -suid
if ($LastExitCode -ne 0) {
    throw "heat failed."
}

$heatLines = [System.IO.File]::ReadAllLines($tempWxsFilePath)
Remove-Item $tempWxsFilePath
$doneWithFilesXml = $false
for ($i = 5; $i -lt $heatLines.Length; $i++) {
    if ($heatLines[$i].Contains("</DirectoryRef>")) {
        $doneWithFilesXml = $true

        # heat misses this one
        $filesXml += '<Component Win64="yes" Id="System.IO.Compression.Native.dll" Guid="D1B5046E-FA58-4D54-AE9D-DF56895DFB5C">' + "`r`n"
        $filesXml += '<File Id="System.IO.Compression.Native.dll" KeyPath="yes" Source="SourceDir\System.IO.Compression.Native.dll" />' + "`r`n"
        $filesXml += '</Component>' + "`r`n"
    }
    if (-not $doneWithFilesXml) {
        $filesXml += $heatLines[$i] + "`r`n"
    }
    if ($heatLines[$i].Contains("<ComponentRef")) {
        $refsXml += $heatLines[$i] + "`r`n"
    }
}
$filesXml = $filesXml.Substring(0, $filesXml.LastIndexOf('</Directory>')).Replace("<Component ", '<Component Win64="yes" ')

$wxs = (Get-Content "$srcdir\SqlNotebook.wxs").Replace("<!--FILES-->", $filesXml).Replace("<!--REFS-->", $refsXml).Replace("<!--PLATFORM-->", $Platform)

Set-Content "$relDir\SqlNotebook.wxs" $wxs

& "$wixDir\candle.exe" -nologo -pedantic "$relDir\SqlNotebook.wxs" | Write-Output
if ($LastExitCode -ne 0) {
    throw "candle failed."
}
if (-not (Test-Path "$relDir\SqlNotebook.wixobj")) {
    throw "candle failed to produce SqlNotebook.wixobj"
}

& "$wixDir\light.exe" -nologo -pedantic -ext WixUIExtension -cultures:en-us "$relDir\SqlNotebook.wixobj" | Write-Output
if ($LastExitCode -ne 0) {
    throw "light failed."
}
if (-not (Test-Path "$relDir\SqlNotebook.msi")) {
    throw "light failed to produce SqlNotebook.msi"
}

Move-Item -Force "$relDir\SqlNotebook.msi" $msiFilePath

Pop-Location

Write-Output "Phase 2 completed."
Write-Output "ZIP: $zipFilePath"
Write-Output "MSI: $msiFilePath" 