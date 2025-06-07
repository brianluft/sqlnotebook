//Copyright (c) Microsoft Corporation.  All rights reserved.

using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>Defines properties for known folders that identify the path of standard known folders.</summary>
    public static class KnownFolders
    {
        /// <summary>Gets a strongly-typed read-only collection of all the registered known folders.</summary>
        public static ICollection<IKnownFolder> All => GetAllFolders();

        /// <summary>Gets the metadata for the <b>CommonOEMLinks</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CommonOemLinks => GetKnownFolder(FolderIdentifiers.CommonOEMLinks);

        /// <summary>Gets the metadata for the <b>DeviceMetadataStore</b> folder.</summary>
        public static IKnownFolder DeviceMetadataStore
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.DeviceMetadataStore);
            }
        }

        /// <summary>Gets the metadata for the <b>DocumentsLibrary</b> folder.</summary>
        public static IKnownFolder DocumentsLibrary
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.DocumentsLibrary);
            }
        }

        /// <summary>Gets the metadata for the <b>ImplicitAppShortcuts</b> folder.</summary>
        public static IKnownFolder ImplicitAppShortcuts
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.ImplicitAppShortcuts);
            }
        }

        /// <summary>Gets the metadata for the <b>Libraries</b> folder.</summary>
        public static IKnownFolder Libraries
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.Libraries);
            }
        }

        /// <summary>Gets the metadata for the <b>MusicLibrary</b> folder.</summary>
        public static IKnownFolder MusicLibrary
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.MusicLibrary);
            }
        }

        /// <summary>Gets the metadata for the <b>OtherUsers</b> folder.</summary>
        public static IKnownFolder OtherUsers
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.OtherUsers);
            }
        }

        /// <summary>Gets the metadata for the <b>PicturesLibrary</b> folder.</summary>
        public static IKnownFolder PicturesLibrary
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.PicturesLibrary);
            }
        }

        /// <summary>Gets the metadata for the <b>PublicRingtones</b> folder.</summary>
        public static IKnownFolder PublicRingtones
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.PublicRingtones);
            }
        }

        /// <summary>Gets the metadata for the <b>RecordedTVLibrary</b> folder.</summary>
        public static IKnownFolder RecordedTVLibrary
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.RecordedTVLibrary);
            }
        }

        /// <summary>Gets the metadata for the <b>Ringtones</b> folder.</summary>
        public static IKnownFolder Ringtones
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.Ringtones);
            }
        }

        /// <summary>
        ///Gets the metadata for the <b>UserPinned</b> folder.
        /// </summary>
        public static IKnownFolder UserPinned
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.UserPinned);
            }
        }

        /// <summary>Gets the metadata for the <b>UserProgramFiles</b> folder.</summary>
        public static IKnownFolder UserProgramFiles
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.UserProgramFiles);
            }
        }

        /// <summary>Gets the metadata for the <b>UserProgramFilesCommon</b> folder.</summary>
        public static IKnownFolder UserProgramFilesCommon
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.UserProgramFilesCommon);
            }
        }

        /// <summary>Gets the metadata for the <b>UsersLibraries</b> folder.</summary>
        public static IKnownFolder UsersLibraries
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.UsersLibraries);
            }
        }

        /// <summary>Gets the metadata for the <b>VideosLibrary</b> folder.</summary>
        public static IKnownFolder VideosLibrary
        {
            get
            {
                CoreHelpers.ThrowIfNotWin7();
                return GetKnownFolder(FolderIdentifiers.VideosLibrary);
            }
        }

        /// <summary>Gets the metadata for the <b>AddNewPrograms</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder AddNewPrograms => GetKnownFolder(FolderIdentifiers.AddNewPrograms);

        /// <summary>Gets the metadata for the <b>AdminTools</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder AdminTools => GetKnownFolder(FolderIdentifiers.AdminTools);

        /// <summary>Gets the metadata for the <b>AppUpdates</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder AppUpdates => GetKnownFolder(FolderIdentifiers.AppUpdates);

        /// <summary>Gets the metadata for the <b>CDBurning</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CDBurning => GetKnownFolder(FolderIdentifiers.CDBurning);

        /// <summary>Gets the metadata for the <b>ChangeRemovePrograms</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ChangeRemovePrograms => GetKnownFolder(FolderIdentifiers.ChangeRemovePrograms);

        /// <summary>Gets the metadata for the <b>CommonAdminTools</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CommonAdminTools => GetKnownFolder(FolderIdentifiers.CommonAdminTools);

        /// <summary>Gets the metadata for the <b>CommonPrograms</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CommonPrograms => GetKnownFolder(FolderIdentifiers.CommonPrograms);

        /// <summary>Gets the metadata for the <b>CommonStartMenu</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CommonStartMenu => GetKnownFolder(FolderIdentifiers.CommonStartMenu);

        /// <summary>Gets the metadata for the <b>CommonStartup</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CommonStartup => GetKnownFolder(FolderIdentifiers.CommonStartup);

        /// <summary>Gets the metadata for the <b>CommonTemplates</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder CommonTemplates => GetKnownFolder(FolderIdentifiers.CommonTemplates);

        /// <summary>Gets the metadata for the <b>Computer</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Computer => GetKnownFolder(
                    FolderIdentifiers.Computer);

        /// <summary>Gets the metadata for the <b>Conflict</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Conflict => GetKnownFolder(
                    FolderIdentifiers.Conflict);

        /// <summary>Gets the metadata for the <b>Connections</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Connections => GetKnownFolder(
                    FolderIdentifiers.Connections);

        /// <summary>Gets the metadata for the <b>Contacts</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Contacts => GetKnownFolder(FolderIdentifiers.Contacts);

        /// <summary>Gets the metadata for the <b>ControlPanel</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ControlPanel => GetKnownFolder(
                    FolderIdentifiers.ControlPanel);

        /// <summary>Gets the metadata for the <b>Cookies</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Cookies => GetKnownFolder(FolderIdentifiers.Cookies);

        /// <summary>Gets the metadata for the <b>Desktop</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Desktop => GetKnownFolder(
                    FolderIdentifiers.Desktop);

        /// <summary>Gets the metadata for the per-user <b>Documents</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Documents => GetKnownFolder(FolderIdentifiers.Documents);

        /// <summary>Gets the metadata for the per-user <b>Downloads</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Downloads => GetKnownFolder(FolderIdentifiers.Downloads);

        /// <summary>Gets the metadata for the per-user <b>Favorites</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Favorites => GetKnownFolder(FolderIdentifiers.Favorites);

        /// <summary>Gets the metadata for the <b>Fonts</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Fonts => GetKnownFolder(FolderIdentifiers.Fonts);

        /// <summary>Gets the metadata for the <b>Games</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Games => GetKnownFolder(FolderIdentifiers.Games);

        /// <summary>Gets the metadata for the <b>GameTasks</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder GameTasks => GetKnownFolder(FolderIdentifiers.GameTasks);

        /// <summary>Gets the metadata for the <b>History</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder History => GetKnownFolder(FolderIdentifiers.History);

        /// <summary>Gets the metadata for the <b>Internet</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Internet => GetKnownFolder(
                    FolderIdentifiers.Internet);

        /// <summary>Gets the metadata for the <b>InternetCache</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder InternetCache => GetKnownFolder(FolderIdentifiers.InternetCache);

        /// <summary>Gets the metadata for the per-user <b>Links</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Links => GetKnownFolder(FolderIdentifiers.Links);

        /// <summary>Gets the metadata for the per-user <b>LocalAppData</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder LocalAppData => GetKnownFolder(FolderIdentifiers.LocalAppData);

        /// <summary>Gets the metadata for the <b>LocalAppDataLow</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder LocalAppDataLow => GetKnownFolder(FolderIdentifiers.LocalAppDataLow);

        /// <summary>Gets the metadata for the <b>LocalizedResourcesDir</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder LocalizedResourcesDir => GetKnownFolder(FolderIdentifiers.LocalizedResourcesDir);

        /// <summary>Gets the metadata for the per-user <b>Music</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Music => GetKnownFolder(FolderIdentifiers.Music);

        /// <summary>Gets the metadata for the <b>NetHood</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder NetHood => GetKnownFolder(FolderIdentifiers.NetHood);

        /// <summary>Gets the metadata for the <b>Network</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Network => GetKnownFolder(
                    FolderIdentifiers.Network);

        /// <summary>Gets the metadata for the <b>OriginalImages</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder OriginalImages => GetKnownFolder(FolderIdentifiers.OriginalImages);

        /// <summary>Gets the metadata for the <b>PhotoAlbums</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PhotoAlbums => GetKnownFolder(FolderIdentifiers.PhotoAlbums);

        /// <summary>Gets the metadata for the per-user <b>Pictures</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Pictures => GetKnownFolder(FolderIdentifiers.Pictures);

        /// <summary>Gets the metadata for the <b>Playlists</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Playlists => GetKnownFolder(FolderIdentifiers.Playlists);

        /// <summary>Gets the metadata for the <b>Printers</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Printers => GetKnownFolder(
                    FolderIdentifiers.Printers);

        /// <summary>Gets the metadata for the <b>PrintHood</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PrintHood => GetKnownFolder(FolderIdentifiers.PrintHood);

        /// <summary>Gets the metadata for the <b>Profile</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Profile => GetKnownFolder(FolderIdentifiers.Profile);

        /// <summary>Gets the metadata for the <b>ProgramData</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramData => GetKnownFolder(FolderIdentifiers.ProgramData);

        /// <summary>Gets the metadata for the <b>ProgramFiles</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramFiles => GetKnownFolder(FolderIdentifiers.ProgramFiles);

        /// <summary>Gets the metadata for the <b>ProgramFilesCommon</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramFilesCommon => GetKnownFolder(FolderIdentifiers.ProgramFilesCommon);

        /// <summary>Gets the metadata for the <b>ProgramFilesCommonX64</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramFilesCommonX64 => GetKnownFolder(FolderIdentifiers.ProgramFilesCommonX64);

        /// <summary>Gets the metadata for the <b>ProgramFilesCommonX86</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramFilesCommonX86 => GetKnownFolder(FolderIdentifiers.ProgramFilesCommonX86);

        /// <summary>Gets the metadata for the <b>ProgramsFilesX64</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramFilesX64 => GetKnownFolder(FolderIdentifiers.ProgramFilesX64);

        /// <summary>Gets the metadata for the <b>ProgramFilesX86</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ProgramFilesX86 => GetKnownFolder(FolderIdentifiers.ProgramFilesX86);

        /// <summary>Gets the metadata for the <b>Programs</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Programs => GetKnownFolder(FolderIdentifiers.Programs);

        /// <summary>Gets the metadata for the <b>Public</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Public => GetKnownFolder(FolderIdentifiers.Public);

        /// <summary>Gets the metadata for the <b>PublicDesktop</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicDesktop => GetKnownFolder(FolderIdentifiers.PublicDesktop);

        /// <summary>Gets the metadata for the <b>PublicDocuments</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicDocuments => GetKnownFolder(FolderIdentifiers.PublicDocuments);

        /// <summary>Gets the metadata for the <b>PublicDownloads</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicDownloads => GetKnownFolder(FolderIdentifiers.PublicDownloads);

        /// <summary>Gets the metadata for the <b>PublicGameTasks</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicGameTasks => GetKnownFolder(FolderIdentifiers.PublicGameTasks);

        /// <summary>Gets the metadata for the <b>PublicMusic</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicMusic => GetKnownFolder(FolderIdentifiers.PublicMusic);

        /// <summary>Gets the metadata for the <b>PublicPictures</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicPictures => GetKnownFolder(FolderIdentifiers.PublicPictures);

        /// <summary>Gets the metadata for the <b>PublicVideos</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder PublicVideos => GetKnownFolder(FolderIdentifiers.PublicVideos);

        /// <summary>Gets the metadata for the per-user <b>QuickLaunch</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder QuickLaunch => GetKnownFolder(FolderIdentifiers.QuickLaunch);

        /// <summary>Gets the metadata for the per-user <b>Recent</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Recent => GetKnownFolder(FolderIdentifiers.Recent);

        /// <summary>Gets the metadata for the <b>RecordedTV</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        /// <remarks>This folder is not used.</remarks>
        public static IKnownFolder RecordedTV => GetKnownFolder(FolderIdentifiers.RecordedTV);

        /// <summary>Gets the metadata for the <b>RecycleBin</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder RecycleBin => GetKnownFolder(
                    FolderIdentifiers.RecycleBin);

        /// <summary>Gets the metadata for the <b>ResourceDir</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder ResourceDir => GetKnownFolder(FolderIdentifiers.ResourceDir);

        /// <summary>Gets the metadata for the <b>RoamingAppData</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder RoamingAppData => GetKnownFolder(FolderIdentifiers.RoamingAppData);

        /// <summary>Gets the metadata for the <b>SampleMusic</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SampleMusic => GetKnownFolder(FolderIdentifiers.SampleMusic);

        /// <summary>Gets the metadata for the <b>SamplePictures</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SamplePictures => GetKnownFolder(FolderIdentifiers.SamplePictures);

        /// <summary>Gets the metadata for the <b>SamplePlaylists</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SamplePlaylists => GetKnownFolder(FolderIdentifiers.SamplePlaylists);

        /// <summary>Gets the metadata for the <b>SampleVideos</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SampleVideos => GetKnownFolder(FolderIdentifiers.SampleVideos);

        /// <summary>Gets the metadata for the per-user <b>SavedGames</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SavedGames => GetKnownFolder(FolderIdentifiers.SavedGames);

        /// <summary>Gets the metadata for the per-user <b>SavedSearches</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SavedSearches => GetKnownFolder(FolderIdentifiers.SavedSearches);

        /// <summary>Gets the metadata for the <b>SearchCsc</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SearchCsc => GetKnownFolder(FolderIdentifiers.SearchCsc);

        /// <summary>Gets the metadata for the <b>SearchHome</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SearchHome => GetKnownFolder(FolderIdentifiers.SearchHome);

        /// <summary>Gets the metadata for the <b>SearchMapi</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SearchMapi => GetKnownFolder(FolderIdentifiers.SearchMapi);

        /// <summary>Gets the metadata for the per-user <b>SendTo</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SendTo => GetKnownFolder(FolderIdentifiers.SendTo);

        /// <summary>Gets the metadata for the <b>SidebarDefaultParts</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SidebarDefaultParts => GetKnownFolder(FolderIdentifiers.SidebarDefaultParts);

        /// <summary>Gets the metadata for the <b>SidebarParts</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SidebarParts => GetKnownFolder(FolderIdentifiers.SidebarParts);

        /// <summary>Gets the metadata for the per-user <b>StartMenu</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder StartMenu => GetKnownFolder(FolderIdentifiers.StartMenu);

        /// <summary>Gets the metadata for the <b>Startup</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Startup => GetKnownFolder(FolderIdentifiers.Startup);

        /// <summary>Gets the metadata for the <b>SyncManager</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SyncManager => GetKnownFolder(
                    FolderIdentifiers.SyncManager);

        /// <summary>Gets the metadata for the <b>SyncResults</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SyncResults => GetKnownFolder(
                    FolderIdentifiers.SyncResults);

        /// <summary>Gets the metadata for the <b>SyncSetup</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SyncSetup => GetKnownFolder(
                    FolderIdentifiers.SyncSetup);

        /// <summary>Gets the metadata for the <b>System</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder System => GetKnownFolder(FolderIdentifiers.System);

        /// <summary>Gets the metadata for the <b>SystemX86</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder SystemX86 => GetKnownFolder(FolderIdentifiers.SystemX86);

        /// <summary>Gets the metadata for the <b>Templates</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Templates => GetKnownFolder(FolderIdentifiers.Templates);

        /// <summary>Gets the metadata for the <b>TreeProperties</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder TreeProperties => GetKnownFolder(FolderIdentifiers.TreeProperties);

        /// <summary>Gets the metadata for the <b>UserProfiles</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder UserProfiles => GetKnownFolder(FolderIdentifiers.UserProfiles);

        /// <summary>Gets the metadata for the <b>UsersFiles</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder UsersFiles => GetKnownFolder(FolderIdentifiers.UsersFiles);

        /// <summary>Gets the metadata for the <b>Videos</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Videos => GetKnownFolder(FolderIdentifiers.Videos);

        /// <summary>Gets the metadata for the <b>Windows</b> folder.</summary>
        /// <value>An <see cref="IKnownFolder"/> object.</value>
        public static IKnownFolder Windows => GetKnownFolder(FolderIdentifiers.Windows);

        private static ReadOnlyCollection<IKnownFolder> GetAllFolders()
        {
            // Should this method be thread-safe?? (It'll take a while to get a list of all the known folders, create the managed wrapper and
            // return the read-only collection.

            IList<IKnownFolder> foldersList = new List<IKnownFolder>();
            var folders = IntPtr.Zero;

            try
            {
                var knownFolderManager = new KnownFolderManagerClass();
                knownFolderManager.GetFolderIds(out folders, out var count);

                if (count > 0 && folders != IntPtr.Zero)
                {
                    // Loop through all the KnownFolderID elements
                    for (var i = 0; i < count; i++)
                    {
                        // Read the current pointer
                        var current = new IntPtr(folders.ToInt64() + (Marshal.SizeOf(typeof(Guid)) * i));

                        // Convert to Guid
                        var knownFolderID = (Guid)Marshal.PtrToStructure(current, typeof(Guid));

                        var kf = KnownFolderHelper.FromKnownFolderIdInternal(knownFolderID);

                        // Add to our collection if it's not null (some folders might not exist on the system or we could have an exception
                        // that resulted in the null return from above method call
                        if (kf != null) { foldersList.Add(kf); }
                    }
                }
            }
            finally
            {
                if (folders != IntPtr.Zero) { Marshal.FreeCoTaskMem(folders); }
            }

            return new ReadOnlyCollection<IKnownFolder>(foldersList);
        }

        private static IKnownFolder GetKnownFolder(Guid guid) => KnownFolderHelper.FromKnownFolderId(guid);
    }
}