$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3

$p = "ext\dockpanelsuite\WinFormsUI\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\dockpanelsuite\WinFormsUI\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\dockpanelsuite\WinFormsUI\ThemeVS2012Light\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\dockpanelsuite\WinFormsUI\ThemeVS2012Light\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\Windows-API-Code-Pack\source\WindowsAPICodePack\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\Windows-API-Code-Pack\source\WindowsAPICodePack\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\Windows-API-Code-Pack\source\WindowsAPICodePack\Core\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "ext\Windows-API-Code-Pack\source\WindowsAPICodePack\Core\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebook\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebook\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookScript\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookScript\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookDb\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookDb\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookCmd\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookCmd\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\Tests\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\Tests\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\crypto\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\crypto\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\fuzzy\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\fuzzy\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\stats\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\stats\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\packages"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "web\site"; if (Test-Path $p) { rm -Force -Recurse $p }
