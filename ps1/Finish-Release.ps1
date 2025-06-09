param (
    [Parameter(Mandatory=$true)]
    [string]$Platform,
    [Parameter(Mandatory=$true)]
    [string]$SigntoolPath,
    [Parameter(Mandatory=$true)]
    [string]$SigntoolSha1,
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Verify that $Platform is either 'x64' or 'arm64'
if ($Platform -ne 'x64' -and $Platform -ne 'arm64') {
    throw "Platform must be either 'x64' or 'arm64'."
}

# Verify that signtool exists
if (-not (Test-Path $SigntoolPath)) {
    throw "Signtool not found at: $SigntoolPath"
}

Write-Output "=== PHASE 2: Code signing, generate portable ZIP and MSI ==="

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
$outputDir = Join-Path $rootDir "release-output"

# Verify that Phase 1 was completed
if (-not (Test-Path "$relDir\SqlNotebook.exe")) {
    throw "Phase 1 must be completed first. SqlNotebook.exe not found in $relDir"
}

# Create output directory
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

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
# Code sign the executable
#

Write-Output "Code signing SqlNotebook.exe..."
& $SigntoolPath sign /v /tr http://timestamp.sectigo.com /fd SHA256 /td SHA256 /sha1 $SigntoolSha1 "SqlNotebook.exe"
if ($LastExitCode -ne 0) {
    throw "Code signing of SqlNotebook.exe failed."
}
Write-Output "SqlNotebook.exe signed successfully."

Write-Output "Code signing SqlNotebookCmd.exe..."
& $SigntoolPath sign /v /tr http://timestamp.sectigo.com /fd SHA256 /td SHA256 /sha1 $SigntoolSha1 "SqlNotebookCmd.exe"
if ($LastExitCode -ne 0) {
    throw "Code signing of SqlNotebookCmd.exe failed."
}
Write-Output "SqlNotebookCmd.exe signed successfully."

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

#
# Code sign the MSI
#

Write-Output "Code signing SqlNotebook.msi..."
& $SigntoolPath sign /v /tr http://timestamp.sectigo.com /fd SHA256 /td SHA256 /sha1 $SigntoolSha1 $msiFilePath
if ($LastExitCode -ne 0) {
    throw "Code signing of SqlNotebook.msi failed."
}
Write-Output "SqlNotebook.msi signed successfully."

#
# Copy final files to output directory with proper naming
#

$finalZipName = "SqlNotebook-$Platform-$Version.zip"
$finalMsiName = "SqlNotebook-$Platform-$Version.msi"

Copy-Item $zipFilePath "$outputDir\$finalZipName"
Copy-Item $msiFilePath "$outputDir\$finalMsiName"

Pop-Location

Write-Output "Phase 2 completed successfully."
Write-Output "Release files created in: $outputDir"
Write-Output "  - $finalZipName"
Write-Output "  - $finalMsiName"
Write-Output ""
Write-Output "These files are ready to be attached to the GitHub release." 