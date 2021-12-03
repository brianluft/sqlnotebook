Set-StrictMode -Version 3
$ErrorActionPreference = "Stop"

& "$PSScriptRoot\Update-Docs.ps1"

$ghDir = (Resolve-Path "$PSScriptRoot\..\..\sqlnotebook-gh-pages").Path
Get-ChildItem $ghDir | % { Remove-Item $_ }

$siteDir = (Resolve-Path "$PSScriptRoot\..\web\site").Path
Push-Location $siteDir
Copy-Item -Recurse ".\*" "$ghDir\" -Exclude ".git"
Pop-Location
