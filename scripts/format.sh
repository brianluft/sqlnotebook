#!/bin/bash
cd "$( dirname "${BASH_SOURCE[0]}" )"
cd ..

LOGFILE=format.log
dotnet tool run csharpier format src/ 2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Error: Formatting failed"
    exit 1
fi

powershell.exe -NoProfile ps1/Update-DocFormatting.ps1 2>&1 | grep -v "Warning" 2>&1 >"$LOGFILE"
if [ $? -ne 0 ]; then
    cat "$LOGFILE"
    rm "$LOGFILE"
    echo "Error: Formatting failed"
    exit 1
fi

rm "$LOGFILE"
echo "Formatting complete."