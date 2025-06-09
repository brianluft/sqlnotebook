Set-StrictMode -Version 3
$ErrorActionPreference = "Stop"

& "$PSScriptRoot\Update-Docs.ps1"

$ghDir = (Resolve-Path "$PSScriptRoot\..\..\website-sqlnotebook.com").Path
Get-ChildItem $ghDir | % { Remove-Item -Force -Recurse "$ghDir\$_" }

$siteDir = (Resolve-Path "$PSScriptRoot\..\web\site").Path
Push-Location $siteDir
Copy-Item -Recurse ".\*" "$ghDir\" -Exclude ".git"
Pop-Location

Push-Location $ghDir
git add -A
git commit --amend
Pop-Location
