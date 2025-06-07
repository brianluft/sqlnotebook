# Generate the tests in src\Tests\ScriptTest.cs

Set-StrictMode -Version 3
$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\src\Tests\scripts")).Path
$sqlFilePaths = [System.IO.Directory]::GetFiles($root, "*.sql", [System.IO.SearchOption]::AllDirectories)
$cs += "namespace Tests;`r`n`r`n"
$cs += "// Update this with ps1/Update-Tests.ps1. Do not edit manually.`r`n"
$cs += "public sealed partial class ScriptTest {`r`n"
foreach ($sqlFilePath in $sqlFilePaths) {
    $relativeFilePath = $sqlFilePath.Substring($root.Length + 1)
    $method = ''
    for ($i = 0; $i -lt $relativeFilePath.Length; $i++) {
        $ch = $relativeFilePath[$i];
        if ("$ch" -match '[A-Z0-9]') {
            $method += $ch
        } else {
            $method += '_'
        }
    }
    $cs += "    [TestMethod] public void Test_$method() => TestScript(@`"$relativeFilePath`");`r`n"
}
$cs += "}`r`n"

$filePath = (Join-Path $PSScriptRoot "..\src\Tests\ScriptTest.g.cs")
[System.IO.File]::WriteAllText($filePath, $cs)
