#!/bin/bash
set -euo pipefail

# Detect the native architecture: x64 or ARM64?
# 
# Don't use $PROCESSOR_ARCHITECTURE directly; that reports the architecture of bash.exe which is always x64 even in the
# supposedly arm64 build of Git for Windows.
#
# Instead, invoke PowerShell and check it there, because powershell.exe is always the correct architecture.
ARCH=$(powershell.exe -NoProfile -Command "[System.Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE', 'Machine')")
case "$ARCH" in
    "ARM64") PLATFORM="ARM64" ;;
    *) PLATFORM="x64" ;;  # Default to x64 for all other values
esac

echo -n "$PLATFORM"
