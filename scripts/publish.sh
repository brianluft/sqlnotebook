#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
PLATFORM=$(scripts/get-native-arch.sh)
MSBUILD_PLATFORM="${PLATFORM,,}"

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

echo "Running Start-Release.ps1 for platform $MSBUILD_PLATFORM"
powershell.exe -NoProfile ps1/Start-Release.ps1 -Platform "$MSBUILD_PLATFORM" -MsbuildPath "'$MSBUILD'"
