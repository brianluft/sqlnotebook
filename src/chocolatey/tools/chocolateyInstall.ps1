$ErrorActionPreference = "Stop"

$packageName = 'SQL Notebook'
$installerType = 'msi'
$silentArgs = '/quiet'
$validExitCodes = @(0)

$platform = [System.Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE', 'Machine')

if ($platform -eq 'ARM64') {
    $url = 'https://github.com/electroly/sqlnotebook/releases/download/v2.0.0/SqlNotebook-arm64-2.0.0.msi'
    $checksum = 'E42EBE99220DD484A80360F62CA0C520A5CAADEF67B0EA04BAC009BEF59303C4'
} else {
    $url = 'https://github.com/electroly/sqlnotebook/releases/download/v2.0.0/SqlNotebook-64bit-2.0.0.msi'
    $checksum = '4B6A8B59B8D6BADE46B07E46229C3EDB229FB8E7EBFE7610C0837711CF47A441'
}

Install-ChocolateyPackage "$packageName" "$installerType" "$silentArgs" "$url" -validExitCodes $validExitCodes -checksumType 'sha256' -checksum $checksum
