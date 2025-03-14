# Set platform to x64 or ARM64 based on the actual build system platform
$Platform = $env:PROCESSOR_ARCHITECTURE

Write-Output "Building sqlite3."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Debug /p:Platform=$Platform src\SqlNotebookDb\SqlNotebookDb.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build sqlite3."
}

Write-Output "Building crypto."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Debug /p:Platform=$Platform src\crypto\crypto.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build crypto."
}

Write-Output "Building fuzzy."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Debug /p:Platform=$Platform src\fuzzy\fuzzy.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build fuzzy."
}

Write-Output "Building stats."
& "$MsbuildPath" /verbosity:quiet /t:build /p:Configuration=Debug /p:Platform=$Platform src\stats\stats.vcxproj
if ($LastExitCode -ne 0) {
    throw "Failed to build stats."
}

Write-Output "Running tests."
dotnet test src/Tests/ /p:Configuration=Debug /p:Platform=$Platform
