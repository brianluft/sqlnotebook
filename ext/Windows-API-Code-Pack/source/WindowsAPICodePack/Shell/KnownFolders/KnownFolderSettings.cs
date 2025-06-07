//Copyright (c) Microsoft Corporation.  All rights reserved.

using MS.WindowsAPICodePack.Internal;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>Internal class to represent the KnownFolder settings/properties</summary>
    internal class KnownFolderSettings
    {
        private FolderProperties knownFolderProperties;

        internal KnownFolderSettings(IKnownFolderNative knownFolderNative) => GetFolderProperties(knownFolderNative);

        /// <summary>Gets this known folder's canonical name.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string CanonicalName => knownFolderProperties.canonicalName;

        /// <summary>Gets the category designation for this known folder.</summary>
        /// <value>A <see cref="FolderCategory"/> value.</value>
        public FolderCategory Category => knownFolderProperties.category;

        /// <summary>Gets an value that describes this known folder's behaviors.</summary>
        /// <value>A <see cref="DefinitionOptions"/> value.</value>
        public DefinitionOptions DefinitionOptions => knownFolderProperties.definitionOptions;

        /// <summary>Gets this known folder's description.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string Description => knownFolderProperties.description;

        /// <summary>Gets the unique identifier for this known folder.</summary>
        /// <value>A <see cref="System.Guid"/> value.</value>
        public Guid FolderId => knownFolderProperties.folderId;

        /// <summary>Gets a string representation of this known folder's type.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string FolderType => knownFolderProperties.folderType;

        /// <summary>Gets the unique identifier for this known folder's type.</summary>
        /// <value>A <see cref="System.Guid"/> value.</value>
        public Guid FolderTypeId => knownFolderProperties.folderTypeId;

        /// <summary>Gets this known folder's localized name.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string LocalizedName => knownFolderProperties.localizedName;

        /// <summary>Gets the resource identifier for this known folder's localized name.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string LocalizedNameResourceId => knownFolderProperties.localizedNameResourceId;

        /// <summary>Gets the unique identifier for this known folder's parent folder.</summary>
        /// <value>A <see cref="System.Guid"/> value.</value>
        public Guid ParentId => knownFolderProperties.parentId;

        /// <summary>Gets the path for this known folder.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string Path => knownFolderProperties.path;

        /// <summary>Gets a value that indicates whether this known folder's path exists on the computer.</summary>
        /// <value>A bool <see cref="System.Boolean"/> value.</value>
        /// <remarks>
        /// If this property value is <b>false</b>, the folder might be a virtual folder ( <see cref="Category"/> property will be
        /// <see cref="FolderCategory.Virtual"/> for virtual folders)
        /// </remarks>
        public bool PathExists => knownFolderProperties.pathExists;

        /// <summary>
        /// Gets a value that states whether this known folder can have its path set to a new value, including any restrictions on the redirection.
        /// </summary>
        /// <value>A <see cref="RedirectionCapability"/> value.</value>
        public RedirectionCapability Redirection => knownFolderProperties.redirection;

        /// <summary>Gets this known folder's relative path.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string RelativePath => knownFolderProperties.relativePath;

        /// <summary>Gets this known folder's security attributes.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string Security => knownFolderProperties.security;

        /// <summary>Gets this known folder's tool tip text.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string Tooltip => knownFolderProperties.tooltip;

        /// <summary>Gets the resource identifier for this known folder's tool tip text.</summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string TooltipResourceId => knownFolderProperties.tooltipResourceId;

        /// <summary>Gets this known folder's file attributes, such as "read-only".</summary>
        /// <value>A <see cref="System.IO.FileAttributes"/> value.</value>
        public System.IO.FileAttributes FileAttributes => knownFolderProperties.fileAttributes;

        /// <summary>Populates a structure that contains this known folder's properties.</summary>
        private void GetFolderProperties(IKnownFolderNative knownFolderNative)
        {
            Debug.Assert(knownFolderNative != null);

            knownFolderNative.GetFolderDefinition(out var nativeFolderDefinition);

            try
            {
                knownFolderProperties.category = nativeFolderDefinition.category;
                knownFolderProperties.canonicalName = Marshal.PtrToStringUni(nativeFolderDefinition.name);
                knownFolderProperties.description = Marshal.PtrToStringUni(nativeFolderDefinition.description);
                knownFolderProperties.parentId = nativeFolderDefinition.parentId;
                knownFolderProperties.relativePath = Marshal.PtrToStringUni(nativeFolderDefinition.relativePath);
                knownFolderProperties.parsingName = Marshal.PtrToStringUni(nativeFolderDefinition.parsingName);
                knownFolderProperties.tooltipResourceId = Marshal.PtrToStringUni(nativeFolderDefinition.tooltip);
                knownFolderProperties.localizedNameResourceId = Marshal.PtrToStringUni(nativeFolderDefinition.localizedName);
                knownFolderProperties.iconResourceId = Marshal.PtrToStringUni(nativeFolderDefinition.icon);
                knownFolderProperties.security = Marshal.PtrToStringUni(nativeFolderDefinition.security);
                knownFolderProperties.fileAttributes = (System.IO.FileAttributes)nativeFolderDefinition.attributes;
                knownFolderProperties.definitionOptions = nativeFolderDefinition.definitionOptions;
                knownFolderProperties.folderTypeId = nativeFolderDefinition.folderTypeId;
                knownFolderProperties.folderType = FolderTypes.GetFolderType(knownFolderProperties.folderTypeId);

                knownFolderProperties.path = GetPath(out var pathExists, knownFolderNative);
                knownFolderProperties.pathExists = pathExists;

                knownFolderProperties.redirection = knownFolderNative.GetRedirectionCapabilities();

                // Turn tooltip, localized name and icon resource IDs into the actual resources.
                knownFolderProperties.tooltip = CoreHelpers.GetStringResource(knownFolderProperties.tooltipResourceId);
                knownFolderProperties.localizedName = CoreHelpers.GetStringResource(knownFolderProperties.localizedNameResourceId);

                knownFolderProperties.folderId = knownFolderNative.GetId();
            }
            finally
            {
                // Clean up memory.
                Marshal.FreeCoTaskMem(nativeFolderDefinition.name);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.description);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.relativePath);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.parsingName);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.tooltip);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.localizedName);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.icon);
                Marshal.FreeCoTaskMem(nativeFolderDefinition.security);
            }
        }

        /// <summary>Gets the path of this this known folder.</summary>
        /// <param name="fileExists">
        /// Returns false if the folder is virtual, or a boolean value that indicates whether this known folder exists.
        /// </param>
        /// <param name="knownFolderNative">Native IKnownFolder reference</param>
        /// <returns>A <see cref="System.String"/> containing the path, or <see cref="string.Empty"/> if this known folder does not exist.</returns>
        private string GetPath(out bool fileExists, IKnownFolderNative knownFolderNative)
        {
            Debug.Assert(knownFolderNative != null);

            var kfPath = string.Empty;
            fileExists = true;

            // Virtual folders do not have path.
            if (knownFolderProperties.category == FolderCategory.Virtual)
            {
                fileExists = false;
                return kfPath;
            }

            try
            {
                kfPath = knownFolderNative.GetPath(0);
            }
            catch (System.IO.FileNotFoundException)
            {
                fileExists = false;
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                fileExists = false;
            }

            return kfPath;
        }
    }
}