param (
    [string]$MsbuildPath,
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

# Verify MsbuildPath
if (-not (Test-Path $MsbuildPath)) {
    throw "MsbuildPath not found: $MsbuildPath"
}

Write-Output "=== PHASE 1: Build and prepare release files ==="

# Windows SDK 10.0.*.*
$windowsSdkBaseDir = "C:\Program Files (x86)\Windows Kits\10\Redist"
$windowsSdkVersion = `
    Get-ChildItem -Path $windowsSdkBaseDir | 
    Where-Object { $_.Name -match '^10\.0\.\d+\.\d+$' } | 
    Sort-Object Name -Descending | 
    Select-Object -First 1 -ExpandProperty Name
Write-Output "Windows SDK version: $windowsSdkVersion"
$windowsSdkDir = Join-Path -Path $windowsSdkBaseDir -ChildPath "$windowsSdkVersion\ucrt\DLLs\$Platform"
if (-not (Test-Path $windowsSdkDir)) {
    throw "Windows 10 SDK $windowsSdkVersion not found!"
}

# Visual C++ Redistributable 14.*.*
$vsBaseDir = "C:\Program Files\Microsoft Visual Studio\2022"
$vsEditionDir = Get-ChildItem -Path $vsBaseDir | Select-Object -First 1
$vsRuntimeBaseDir = Join-Path -Path $vsEditionDir.FullName -ChildPath "VC\Redist\MSVC"
$vsRuntimeVersion = `
    Get-ChildItem -Path $vsRuntimeBaseDir | 
    Where-Object { $_.Name -match '^14\.\d+\.\d+$' } | 
    Sort-Object Name -Descending | 
    Select-Object -First 1 -ExpandProperty Name
Write-Output "Visual C++ Redistributable version: $vsRuntimeVersion"
$vsRuntimeDir = Join-Path -Path $vsRuntimeBaseDir -ChildPath "$vsRuntimeVersion\$Platform\Microsoft.VC143.CRT"
if (-not (Test-Path $vsRuntimeDir)) {
    throw "Visual C++ Redistributable $vsRuntimeVersion not found!"
}

# Do the build
Write-Output "Restoring source dependencies."
& ps1\Update-Deps.ps1

$msbuildPlatform = $Platform
if ($Platform -eq 'arm64') {
    $msbuildPlatform = 'ARM64'
}

Write-Output "Restoring NuGet dependencies."
Push-Location src\SqlNotebook
& "$MsbuildPath" /verbosity:quiet /t:restore /p:Configuration=Release /p:Platform=$msbuildPlatform /p:RuntimeIdentifier=win-$Platform /p:PublishReadyToRun=true SqlNotebook.csproj
if ($LastExitCode -ne 0) {
    throw "Failed to restore NuGet dependencies."
}

Write-Output "Building sqlite3."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Release /p:Platform=$msbuildPlatform ..\SqlNotebookDb\SqlNotebookDb.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build sqlite3."
}

Write-Output "Building crypto."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Release /p:Platform=$msbuildPlatform ..\crypto\crypto.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build crypto."
}

Write-Output "Building fuzzy."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Release /p:Platform=$msbuildPlatform ..\fuzzy\fuzzy.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build fuzzy."
}

Write-Output "Building stats."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Release /p:Platform=$msbuildPlatform ..\stats\stats.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build stats."
}

Write-Output "Publishing."
& "$MsbuildPath" /verbosity:quiet /t:publish /p:Configuration=Release /p:Platform=$msbuildPlatform /p:RuntimeIdentifier=win-$Platform /p:PublishProfile=FolderProfile SqlNotebook.csproj
if ($LastExitCode -ne 0) {
    throw "Failed to publish."
}

Write-Output "Preparing release files."
$ps1Dir = $PSScriptRoot
$rootDir = (Resolve-Path (Join-Path $ps1Dir "..\")).Path
$srcDir = Join-Path $rootDir "src"
$binDir = Join-Path $srcDir "SqlNotebook\bin"
$relDir = Join-Path $srcDir "SqlNotebook\bin\publish"

Remove-Item "$relDir\portable" -Recurse -ErrorAction SilentlyContinue
Remove-Item "$relDir\*.pdb" -ErrorAction SilentlyContinue
Remove-Item "$relDir\*.xml" -ErrorAction SilentlyContinue
Remove-Item "$relDir\*.wixpdb" -ErrorAction SilentlyContinue
Remove-Item "$relDir\*.wixobj" -ErrorAction SilentlyContinue
Remove-Item "$relDir\*.wxs" -ErrorAction SilentlyContinue
Copy-Item -Force "$rootDir\src\SqlNotebookDb\bin\$Platform\Release\sqlite3.dll" "$relDir\sqlite3.dll"
Copy-Item -Force "$rootDir\src\crypto\bin\$Platform\Release\crypto.dll" "$relDir\crypto.dll"
Copy-Item -Force "$rootDir\src\fuzzy\bin\$Platform\Release\fuzzy.dll" "$relDir\fuzzy.dll"
Copy-Item -Force "$rootDir\src\stats\bin\$Platform\Release\stats.dll" "$relDir\stats.dll"
Copy-Item -Force "$windowsSdkDir\*.dll" "$relDir\"
Copy-Item -Force "$vsRuntimeDir\*.dll" "$relDir\"

# Delete all the localized folders
foreach ($dir in [System.IO.Directory]::GetDirectories($relDir)) {
    if ([System.IO.Path]::GetFileName($dir) -ne 'doc') {
        [System.IO.Directory]::Delete($dir, $true)
    }
}

Pop-Location

Write-Output "Phase 1 completed. Release files are prepared in $relDir"
Write-Output "Next steps:"
Write-Output "1. Sign SqlNotebook.exe in $relDir"
Write-Output "2. Run Finish-Release.ps1 to generate ZIP and MSI" 