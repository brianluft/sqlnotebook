param (
    [Parameter(Mandatory=$true)]
    [string]$MsbuildPath,

    [Parameter(Mandatory=$true)]
    [string]$Platform
)

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
dotnet clean src/Tests/Tests.csproj
if ($LastExitCode -ne 0) {
    throw "Failed to clean tests."
}

dotnet build src/Tests/Tests.csproj /p:Configuration=Debug /p:Platform=$Platform
if ($LastExitCode -ne 0) {
    throw "Failed to build tests."
}

dotnet run --project src/Tests/Tests.csproj --no-build /p:Configuration=Debug /p:Platform=$Platform
if ($LastExitCode -ne 0) {
    throw "Failed to run tests."
}
