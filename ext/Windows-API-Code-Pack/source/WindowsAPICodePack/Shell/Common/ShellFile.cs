//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Shell.Resources;
using System.IO;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>A file in the Shell Namespace</summary>
    public class ShellFile : ShellObject
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal ShellFile(string path)
        {
            // Get the absolute path
            var absPath = ShellHelper.GetAbsolutePath(path);

            // Make sure this is valid
            if (!File.Exists(absPath))
            {
                throw new FileNotFoundException(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LocalizedMessages.FilePathNotExist, path));
            }

            ParsingName = absPath;
        }

        internal ShellFile(IShellItem2 shellItem) => nativeShellItem = shellItem;

        /// <summary>The path for this file</summary>
        public virtual string Path => ParsingName;

        /// <summary>Constructs a new ShellFile object given a file path</summary>
        /// <param name="path">The file or folder path</param>
        /// <returns>ShellFile object created using given file path.</returns>
        public static ShellFile FromFilePath(string path) => new ShellFile(path);
    }
}