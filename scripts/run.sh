#!/bin/bash
set -euo pipefail

cd "$( dirname "${BASH_SOURCE[0]}" )"
PLATFORM=$(./get-native-arch.sh)
RID="win-${PLATFORM,,}"
"../src/SqlNotebook/bin/$PLATFORM/Debug/net7.0-windows/$RID/SqlNotebook.exe"
