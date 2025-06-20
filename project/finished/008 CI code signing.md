- [x] Add code signing to our GitHub Actions release process
    - [x] It used to be integrated into New-Release.ps1, which ran in GitHub Actions, but now code signing requires a physical HSM that we can't use in the cloud. We will split New-Release.ps1 into two phases: 1.) Everything leading up to "Generate portable ZIP", and 2.) Generating the portable zip, and the MSI with WiX. This will allow us to run phase 1 in GitHub Actions, then I will download the resulting release files, sign the EXE myself, and then run phase 2 locally.
    - [x] Add a mandatory parameter to New-Release.ps1 for `-Phase 1` or `-Phase 2`.
    - [x] Update `.github\workflows\sqlnotebook.yml` to run phase 1 only, then to upload an artifact containing `src/SqlNotebook/bin/*`, this will contain the release files needed for the portable zip and WiX.
    - [x] Update `scripts/publish.sh` to run phase 1 only; we use this for local development and testing of the phase 1 process.
    - [x] Update the release instructions in `CONTRIBUTING.md`. Remember that phase 1 runs in GitHub Actions and phase 2 runs locally where the HSM is located.
    - [x] Here are the instructions for signing locally, please format it nicely in `CONTRIBUTING.md`:
        - Install Windows SDK in order to get signtool. The only necessary features are "Windows SDK Signing Tools for Desktop Apps" and "MSI Tools". https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
        - Search for signtool in C:\Program Files (x86). Set $signtool to its path.
        - Find the HSM entry in 1Password. Set $sha1 to the SHA1 hash. Keep the entry up so you can copy the password out.
        - Download the phase 1 zip from GitHub Actions.
        - Set $sha1 to the hash of the code signing certificate, then: `& $signtool sign /v /tr http://timestamp.sectigo.com /fd SHA256 /td SHA256 /sha1 $sha1 SqlNotebook.exe`. Paste the password when prompted.
        - Verify digital signature in the file properties.
        - Run `New-Release.ps1` phase 2.
    - [x] Refactor `New-Release.ps1` by splitting the phases into separate scripts.
        - [x] Read `CONTRIBUTING.md` to understand the release process.
        - [x] Move phase 1 into `Start-Release.ps1`.
        - [x] Move phase 2 into `Finish-Release.ps1`, which now won't need to have `-MsbuildPath`.
        - [x] Delete `New-Release.ps1`.
        - [x] Update `.github\workflows\sqlnotebook.yml` to point to `Start-Release.ps1`.
        - [x] Update `CONTRIBUTING.md` accordingly.
    - [x] The code signing is currently manual. Automate it in `Finish-Release.ps1`.
        - [x] Read `CONTRIBUTING.md` to understand the release process.
        - [x] Add required `-SigntoolPath` parameter, path to `signtool.exe`.
        - [x] Add required `-SigntoolSha1` parameter, the value for signtool's `/sha1` parameter.
        - [x] You have to call `signtool.exe` four times:
            - [x] 2x SqlNotebook.exe (which is then included in both the portable .zip and in the .msi): x64 and arm64
            - [x] 2x SqlNotebook.msi (the output from WiX): x64 and arm64
        - [x] Produce an output folder containing the exact files to attach to the release:
            - [x] SqlNotebook-arm64.msi (WiX installer)
            - [x] SqlNotebook-arm64.zip (portable zip)
            - [x] SqlNotebook-x64.msi (WiX installer)
            - [x] SqlNotebook-x64.zip (portable zip)
        - [x] Update `CONTRIBUTING.md` accordingly. The developer will have to enter their password interactively when prompted by `signtool`, but your script will take care of the rest. Then the developer will attach those four files to the GitHub release and proceed with the rest of the release process.
    - [x] I want the final release files to be named like `SqlNotebook-arm64-2.0.0.msi` where 2.0.0 is the version number. Add `-Version` as a required parameter to `Finish-Release.ps1` and use it to rename the final files.
        - [x] Update `CONTRIBUTING.md` accordingly.
