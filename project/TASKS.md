- [x] Bug: `scripts/publish.sh` (which calls `ps1/New-Release.ps1` internally) produces the error below. We do not want self-contained executables anywhere and I've attempted to disable it, but I'm still getting this error. Find out how to actually disable self contained executables to fix this error.
    ```
    Publishing.
    MSBuild version 17.14.10+8b8e13593 for .NET Framework
    C:\Program Files\dotnet\sdk\9.0.300\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.targets(1379,5): error NETSDK1150: The referenced proj
    ect '..\SqlNotebookCmd\SqlNotebookCmd.csproj' is a non self-contained executable.  A non self-contained executable cannot be referenced by 
    a self-contained executable.  For more information, see https://aka.ms/netsdk1150 [C:\Projects\sqlnotebook\src\SqlNotebook\SqlNotebook.cspr 
    oj]
    Failed to publish.
    At C:\Projects\sqlnotebook\ps1\New-Release.ps1:97 char:5
    +     throw "Failed to publish."
    +     ~~~~~~~~~~~~~~~~~~~~~~~~~~
        + CategoryInfo          : OperationStopped: (Failed to publish.:String) [], RuntimeException
        + FullyQualifiedErrorId : Failed to publish.
    ```
