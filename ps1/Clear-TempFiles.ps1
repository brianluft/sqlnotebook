$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3

$p = "src\SqlNotebook\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebook\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookScript\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookScript\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookDb\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookDb\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookCmd\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\SqlNotebookCmd\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\crypto\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\crypto\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\fuzzy\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\fuzzy\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\stats\bin"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\stats\obj"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "src\packages"; if (Test-Path $p) { rm -Force -Recurse $p }
$p = "web\site"; if (Test-Path $p) { rm -Force -Recurse $p }
