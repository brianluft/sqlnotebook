$ErrorActionPreference = "Stop"

$packageName = 'SQL Notebook'
$installerType = 'msi'
$silentArgs = '/quiet'
$validExitCodes = @(0)

$platform = [System.Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE', 'Machine')

if ($platform -eq 'ARM64') {
    $url = 'https://github.com/electroly/sqlnotebook/releases/download/v2.0.0/SqlNotebook-arm64-2.0.0.msi'
    $checksum = '90C86C8D7EFEA1A6CB2D6BBBC8F357BCAF794529DB6F50C9DF871ABABAC43C12'
} else {
    $url = 'https://github.com/electroly/sqlnotebook/releases/download/v2.0.0/SqlNotebook-64bit-2.0.0.msi'
    $checksum = 'C5F3B26D7D2BBBB2F6541A57A17FBC587B0E2B0B3098A1C376FD8BA526216BDC'
}

Install-ChocolateyPackage "$packageName" "$installerType" "$silentArgs" "$url" -validExitCodes $validExitCodes -checksumType 'sha256' -checksum $checksum
