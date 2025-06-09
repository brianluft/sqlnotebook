- [ ] Create `ps1\Get-SqliteDepLines.ps1`.
    - [ ] Required parameter: version number string like "3460000"
    - [ ] Required parameter: release year like "2025"
    - [ ] In a temporary folder, download these for the specified version/year:
        - [ ] `https://sqlite.org/<year>/sqlite-src-<version>.zip`
        - [ ] `https://sqlite.org/<year>/sqlite-amalgamation-<version>.zip`
        - [ ] `https://sqlite.org/<year>/sqlite-doc-<version>.zip`
    - [ ] Use `Get-FileHash` to get the SHA-256 hash of each one.
    - [ ] Print out this PowerShell script for the user to copy-paste:
        ```
        $sqliteCodeUrl = '<url>'
        $sqliteCodeHash = '<hash>'
        $sqliteDocUrl = '<url>'
        $sqliteDocHash = '<hash>'
        $sqliteSrcUrl = '<url>'
        $sqliteSrcHash = '<hash>'
        ```
