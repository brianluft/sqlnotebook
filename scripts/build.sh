#!/bin/bash
set -euo pipefail

cd "$( dirname "${BASH_SOURCE[0]}" )"
PLATFORM=$(./get-native-arch.sh)
cd ..

# Convert "x64"/"ARM64" to "win-x64"/"win-arm64" (lowercase) in $RID
RID="win-${PLATFORM,,}"

# Use vswhere to locate msbuild.exe
VSWHERE="/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
if [ ! -f "$VSWHERE" ]; then
    echo "Could not find vswhere.exe!"
    exit 1
fi

MSBUILD=$("$VSWHERE" -latest -requires Microsoft.Component.MSBuild -find "MSBuild/**/Bin/MSBuild.exe" | head -n 1)
if [ ! -f "$MSBUILD" ]; then
    echo "Could not find msbuild.exe!"
    exit 1
fi

set +e
LOGFILE="build.log"

powershell.exe -NoProfile "ps1/Update-Tests.ps1"
if [ $? -ne 0 ]; then
    echo "Update-Tests.ps1 failed."
    exit 1
fi

cd src/SqlNotebook

echo "--- Restore ---"
"$MSBUILD" --nologo --verbosity:quiet --t:restore --p:Configuration=Debug --p:Platform=$PLATFORM --p:RuntimeIdentifier=$RID --p:PublishReadyToRun=true SqlNotebook.csproj 2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Failed to restore NuGet dependencies."
    exit 1
fi

echo "--- Dependencies ---"
"$MSBUILD" --nologo --verbosity:quiet --t:build --p:Configuration=Debug --p:Platform=$PLATFORM ../SqlNotebookDb/SqlNotebookDb.vcxproj 2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Failed to build sqlite3."
    exit 1
fi

"$MSBUILD" --nologo --verbosity:quiet --t:build --p:Configuration=Debug --p:Platform=$PLATFORM ../crypto/crypto.vcxproj  2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Failed to build crypto."
    exit 1
fi

"$MSBUILD" --nologo --verbosity:quiet --t:build --p:Configuration=Debug --p:Platform=$PLATFORM ../fuzzy/fuzzy.vcxproj 2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Failed to build fuzzy."
    exit 1
fi

"$MSBUILD" --nologo --verbosity:quiet --t:build --p:Configuration=Debug --p:Platform=$PLATFORM ../stats/stats.vcxproj 2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Failed to build stats."
    exit 1
fi

rm "$LOGFILE"

echo "--- SqlNotebook ---"
"$MSBUILD" --nologo --verbosity:quiet --t:build --p:Configuration=Debug --p:Platform=$PLATFORM --p:RuntimeIdentifier=$RID --p:SelfContained=true SqlNotebook.csproj
if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo "--- Tests ---"
cd ../Tests
"$MSBUILD" --nologo --verbosity:quiet --t:build --p:Configuration=Debug --p:Platform=$PLATFORM Tests.csproj
if [ $? -ne 0 ]; then
    echo "Failed to build tests."
    exit 1
fi

"bin/$PLATFORM/Debug/net7.0-windows/Tests.exe"
if [ $? -ne 0 ]; then
    echo "Failed to run tests."
    exit 1
fi

echo "Build and tests succeeded."
