#!/bin/bash
set -euo pipefail
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..
powershell.exe -NoProfile ps1/Clear-TempFiles.ps1
