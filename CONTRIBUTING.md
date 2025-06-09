# Contributors Guide

Use a Windows machine with at least 8GB RAM and 100GB disk.
In AWS, a `c5a.xlarge` instance running Windows Server 2022 will do.

- Install [7-Zip](https://www.7-zip.org/).
- Install [Visual Studio Community 2022](https://visualstudio.microsoft.com/vs/).
    - Include ".NET desktop development" workload.
    - Include "Desktop development with C++" workload.
    - Include individual component: Windows Universal CRT SDK
    - Include individual component: Windows Universal C Runtime
    - Include individual component: MSVC v143 - VS 2022 C++ ARM64/ARM64EC build tools (Latest)
- Install [tidy-html5](https://github.com/htacg/tidy-html5) to your `PATH`.
- Install [SeaMonkey](https://www.seamonkey-project.org/releases/). Open it, Edit > Preferences > Appearance. Set "When SeaMonkey starts up, open..." to Composer.
- Install [WiX Toolset 3.14](https://github.com/wixtoolset/wix3/releases).
- Install [Chocolatey](https://chocolatey.org/install).
- Install [NuGet](https://www.nuget.org/downloads) and put it in your `PATH`.

## How to build from source

- In PowerShell on Windows, run `ps1/Update-Deps.ps1` to download non-NuGet deps and generate the doc files.
- Open `src\SqlNotebook.sln` in Visual Studio and build.

## How to edit documentation

- Use SeaMonkey Composer to edit the files in `doc/`.
- In WSL, run `ps1/Update-DocFormatting.ps1` to reformat the HTML.
- In Windows, run `ps1/Update-Docs.ps1` to rebuild the website and integrated help.

## How to generate railroad diagram files

- https://pikchr.org/home/pikchrshow
- Save the Pikchr source to a .pikchr file in `doc/art`.
- Click the copy SVG button on the website, paste into a .svg file.
- Get dimensions from the first line of the SVG file.
- Use SeaMonkey Composer to delete the image in the corresponding HTML page, then re-add it.
    - Set the image dimensions.
    - Set its `class="railroad"` attribute.
- Run `ps1/Update-Docs.ps1` to rebuild the website and integrated help.

## How to update SQLite

- Download the amalgamation, doc, and src zips from the SQLite website.
- Update `ps1/Update-Deps.ps1` with the URLs. Use `Get-FileHash` to produce the SHA-256 hashes.
- Close Visual Studio.
- Run `ps1/Update-Deps.ps1`.
- Build the app and fix any errors.
- Read the release notes. The grammar may have changed, which needs to be reflected in `SqliteGrammar.cs`.

## How to release a new version

- Check for new updates to NuGet packages.
- Bump `AssemblyFileVersion` and `AssemblyCopyright` in `src\SqlNotebook\Properties\AssemblyInfo.cs`.
- Bump `ProductVersion` in `src\SqlNotebook.wxs`.
- Add a news entry in `web\index.html`.
- Close Visual Studio.
- Commit changes using commit message "Version X.X.X", and push.

### Phase 1: Build and prepare files (runs in GitHub Actions)
- Wait for GitHub Actions to complete Phase 1 build.
- Download the Phase 1 artifacts from GitHub Actions: `SqlNotebook-x64-release-files` and `SqlNotebook-arm64-release-files`.
- Extract each artifact to get the build outputs in `src/SqlNotebook/bin/publish/`.

### Phase 2: Code signing and final packaging (runs locally)
- **Prerequisites for local signing:**
  1. Install Windows SDK to get signtool. The only necessary features are "Windows SDK Signing Tools for Desktop Apps" and "MSI Tools". Download from: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
  2. Search for signtool in `C:\Program Files (x86)`. Set `$signtool` to its path.
  3. Find the HSM entry in 1Password. Set `$sha1` to the SHA1 hash. Keep the entry open so you can copy the password out.
  4. Download the Phase 1 zip from GitHub Actions.

- **For each platform (x64, arm64):**
  1. Extract the corresponding Phase 1 artifact to your local workspace.
  2. Sign the executable using the following command:
     ```powershell
     & $signtool sign /v /tr http://timestamp.sectigo.com /fd SHA256 /td SHA256 /sha1 $sha1 SqlNotebook.exe
     ```
     Paste the password when prompted.
  3. Verify the digital signature in the file properties.
  4. Run Phase 2: `powershell.exe ps1/New-Release.ps1 -Platform <platform> -MsbuildPath <path> -Phase "2"`
  5. This will generate `SQLNotebook.zip` and `SQLNotebook.msi` in `src/SqlNotebook/bin/`.
  6. Rename them to `SQLNotebook-X.X.X-<platform>.*`.

- Test the zip and MSI files.
- Create release on GitHub, upload zip and msi.
    - Let GitHub create a new tag, name it `vX.X.X`.
    - Set release title to `vX.X.X`.
    - Copy the release verbiage from the previous release, and edit in the new release notes.
    - Edit the previous release and remove the first two lines, the download links. We don't want to confuse users who visit the releases page.
- Update `web\appversion.txt` with new version and MSI URL.
- Run `ps1\Update-GitHubPages.ps1` and force push the `sqlnotebook-gh-pages` repo.
- Update `src\chocolatey\sqlnotebook.nuspec` with copyright and version.
- Update `src\chocolatey\tools\chocolateyInstall.ps1` with MSI URL.
- Put a copy of the code signing certificate into `C:\Tools\Brian Luft.pfx`.
- Get your [Chocolatey API key](https://community.chocolatey.org/account).
- In PowerShell from `src\chocolatey`:
    ```
    choco pack
    nuget sign sqlnotebook.X.X.X.nupkg -CertificatePath "C:\Tools\Brian Luft.pfx" -Timestamper http://timestamp.sectigo.com
    choco install sqlnotebook -s .
    (test that it worked)
    $api = '<chocolatey api key>'
    choco apikey -k "$api" -source https://chocolatey.org/
    choco push .\sqlnotebook.X.X.X.nupkg -s https://chocolatey.org/
    ```
- Commit changes using commit message "Update website and Chocolatey to version X.X.X", and push.
