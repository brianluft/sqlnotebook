Set-StrictMode -Version 3
$ErrorActionPreference = "Stop"

$ps1Dir = $PSScriptRoot
$rootDir = (Resolve-Path (Join-Path $ps1Dir "..\")).Path
$docDir = (Resolve-Path "$rootDir\doc").Path

foreach ($svgFilePath in ([System.IO.Directory]::GetFiles("$docDir/art", "*.svg"))) {
    $svg = [System.IO.File]::ReadAllText($svgFilePath)
    $svg = $svg.Replace("background-color: #f5f5f5;", "")
    [System.IO.File]::WriteAllText($svgFilePath, $svg)
}

function Format($htmlFilePath) {
    $tempFilePath = "$htmlFilePath.tmp"
    Remove-Item -Force $tempFilePath -ErrorAction SilentlyContinue

    # Write reformatted HTML to $tempFilePath
    tidy -q -utf8 -i -w 120 -o "$tempFilePath" "$htmlFilePath"
    if (-not (Test-Path $tempFilePath)) {
        throw "Tidy failed on `"$htmlFilePath`"!"
    }

    # Remove the <meta name="generator" ...> tag that HTML Tidy inserts.
    $html = [System.IO.File]::ReadAllText($tempFilePath)
    $indexStart = $html.IndexOf('<meta name="generator"')
    $indexStart = $html.LastIndexOf("`n", $indexStart)
    $indexEnd = $html.IndexOf('>', $indexStart)
    $html = $html.Substring(0, $indexStart) + $html.Substring($indexEnd + 1)
    [System.IO.File]::WriteAllText($htmlFilePath, $html)

    # Fix line endings
    $content = [System.IO.File]::ReadAllText($htmlFilePath)
    $content = $content.Replace("`r`n", "`n")
    $content = $content.Replace("`n", "`r`n")
    [System.IO.File]::WriteAllText($htmlFilePath, $content)
    Remove-Item -Force $tempFilePath
}

$htmlFilePaths = [System.IO.Directory]::GetFiles($docDir, "*.html")
foreach ($filePath in $htmlFilePaths) {
    echo $filePath
    Format $filePath
}

Format "$rootDir/web/index.html"
Format "$rootDir/src/SqlNotebook/ThirdPartyLicenses.html"
