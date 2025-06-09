param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $true)]
    [string]$Year
)

$global:ProgressPreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3

# Create temporary directory
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
Write-Host "Using temporary directory: $tempDir"
mkdir $tempDir | Out-Null

try {
    # Define the URLs
    $sqliteCodeUrl = "https://sqlite.org/$Year/sqlite-amalgamation-$Version.zip"
    $sqliteDocUrl = "https://sqlite.org/$Year/sqlite-doc-$Version.zip" 
    $sqliteSrcUrl = "https://sqlite.org/$Year/sqlite-src-$Version.zip"
    
    # Download and hash each file
    Write-Host "Downloading and hashing SQLite files..."
    
    # SQLite amalgamation
    $codeFile = Join-Path $tempDir "sqlite-amalgamation-$Version.zip"
    Write-Host "Downloading: $sqliteCodeUrl"
    Invoke-WebRequest -UseBasicParsing -Uri $sqliteCodeUrl -OutFile $codeFile
    $sqliteCodeHash = (Get-FileHash $codeFile).Hash
    Write-Host "Hash: $sqliteCodeHash"
    
    # SQLite documentation
    $docFile = Join-Path $tempDir "sqlite-doc-$Version.zip"
    Write-Host "Downloading: $sqliteDocUrl"
    Invoke-WebRequest -UseBasicParsing -Uri $sqliteDocUrl -OutFile $docFile
    $sqliteDocHash = (Get-FileHash $docFile).Hash
    Write-Host "Hash: $sqliteDocHash"
    
    # SQLite source
    $srcFile = Join-Path $tempDir "sqlite-src-$Version.zip"
    Write-Host "Downloading: $sqliteSrcUrl"
    Invoke-WebRequest -UseBasicParsing -Uri $sqliteSrcUrl -OutFile $srcFile
    $sqliteSrcHash = (Get-FileHash $srcFile).Hash
    Write-Host "Hash: $sqliteSrcHash"
    
    Write-Host ""
    Write-Host "Copy and paste the following PowerShell script:"
    Write-Host ""
    Write-Host "`$sqliteCodeUrl = '$sqliteCodeUrl'"
    Write-Host "`$sqliteCodeHash = '$sqliteCodeHash'"
    Write-Host "`$sqliteDocUrl = '$sqliteDocUrl'"
    Write-Host "`$sqliteDocHash = '$sqliteDocHash'"
    Write-Host "`$sqliteSrcUrl = '$sqliteSrcUrl'"
    Write-Host "`$sqliteSrcHash = '$sqliteSrcHash'"
    
} finally {
    # Clean up temporary directory
    Write-Host ""
    Write-Host "Cleaning up temporary directory..."
    Remove-Item -Force -Recurse $tempDir -ErrorAction SilentlyContinue
} 