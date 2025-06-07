//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Collection of all the standard system stock icons
    /// </summary>
    public class StockIcons
    {
        #region Private Members

        private readonly IDictionary<StockIconIdentifier, StockIcon> stockIconCache;
        private readonly StockIconSize defaultSize = StockIconSize.Large;
        private readonly bool isSelected;
        private readonly bool isLinkOverlay;

        #endregion

        #region Public Constructors

        /// <summary>
        /// Creates a stock icon collection using the default options for 
        /// size, link overlay and selection state.
        /// </summary>
        public StockIcons()
        {
            // Create an empty dictionary. Stock icons will be created when requested
            // or when they are enumerated on this collection
            stockIconCache = new Dictionary<StockIconIdentifier, StockIcon>();

            var allIdentifiers = Enum.GetValues(typeof(StockIconIdentifier));

            foreach (StockIconIdentifier id in allIdentifiers)
            {
                stockIconCache.Add(id, null);
            }
        }

        /// <summary>
        /// Overloaded constructor that takes in size and Boolean values for 
        /// link overlay and selected icon state. The settings are applied to 
        /// all the stock icons in the collection.
        /// </summary>
        /// <param name="size">StockIcon size for all the icons in the collection.</param>
        /// <param name="linkOverlay">Link Overlay state for all the icons in the collection.</param>
        /// <param name="selected">Selection state for all the icons in the collection.</param>
        public StockIcons(StockIconSize size, bool linkOverlay, bool selected)
        {
            defaultSize = size;
            isLinkOverlay = linkOverlay;
            isSelected = selected;

            // Create an empty dictionary. Stock icons will be created when requested
            // or when they are enumerated on this collection
            stockIconCache = new Dictionary<StockIconIdentifier, StockIcon>();

            var allIdentifiers = Enum.GetValues(typeof(StockIconIdentifier));

            foreach (StockIconIdentifier id in allIdentifiers)
            {
                stockIconCache.Add(id, null);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the default stock icon size in one of the StockIconSize values.
        /// This size applies to all the stock icons in the collection.
        /// </summary>
        public StockIconSize DefaultSize => defaultSize;

        /// <summary>
        /// Gets the default link overlay state for the icon. This property 
        /// applies to all the stock icons in the collection.
        /// </summary>
        public bool DefaultLinkOverlay => isLinkOverlay;

        /// <summary>
        /// Gets the default selected state for the icon. This property 
        /// applies to all the stock icons in the collection.
        /// </summary>
        public bool DefaultSelectedState => isSelected;

        /// <summary>
        /// Gets a collection of all the system stock icons
        /// </summary>
        public ICollection<StockIcon> AllStockIcons => GetAllStockIcons();

        /// <summary>
        /// Icon for a document (blank page), no associated program.
        /// </summary>
        public StockIcon DocumentNotAssociated => GetStockIcon(StockIconIdentifier.DocumentNotAssociated);

        /// <summary>
        /// Icon for a document with an associated program.
        /// </summary>
        public StockIcon DocumentAssociated => GetStockIcon(StockIconIdentifier.DocumentAssociated);

        /// <summary>
        ///  Icon for a generic application with no custom icon.
        /// </summary>
        public StockIcon Application => GetStockIcon(StockIconIdentifier.Application);

        /// <summary>
        ///  Icon for a closed folder.
        /// </summary>
        public StockIcon Folder => GetStockIcon(StockIconIdentifier.Folder);

        /// <summary>
        /// Icon for an open folder. 
        /// </summary>
        public StockIcon FolderOpen => GetStockIcon(StockIconIdentifier.FolderOpen);

        /// <summary>
        /// Icon for a 5.25" floppy disk drive.
        /// </summary>
        public StockIcon Drive525 => GetStockIcon(StockIconIdentifier.Drive525);

        /// <summary>
        ///  Icon for a 3.5" floppy disk drive. 
        /// </summary>
        public StockIcon Drive35 => GetStockIcon(StockIconIdentifier.Drive35);

        /// <summary>
        ///  Icon for a removable drive.
        /// </summary>
        public StockIcon DriveRemove => GetStockIcon(StockIconIdentifier.DriveRemove);

        /// <summary>
        ///  Icon for a fixed (hard disk) drive.
        /// </summary>
        public StockIcon DriveFixed => GetStockIcon(StockIconIdentifier.DriveFixed);

        /// <summary>
        ///  Icon for a network drive.
        /// </summary>
        public StockIcon DriveNetwork => GetStockIcon(StockIconIdentifier.DriveNetwork);

        /// <summary>
        ///  Icon for a disconnected network drive.
        /// </summary>
        public StockIcon DriveNetworkDisabled => GetStockIcon(StockIconIdentifier.DriveNetworkDisabled);

        /// <summary>
        ///  Icon for a CD drive.
        /// </summary>
        public StockIcon DriveCD => GetStockIcon(StockIconIdentifier.DriveCD);

        /// <summary>
        ///  Icon for a RAM disk drive. 
        /// </summary>
        public StockIcon DriveRam => GetStockIcon(StockIconIdentifier.DriveRam);

        /// <summary>
        ///  Icon for an entire network. 
        /// </summary>
        public StockIcon World => GetStockIcon(StockIconIdentifier.World);

        /// <summary>
        ///  Icon for a computer on the network.
        /// </summary>
        public StockIcon Server => GetStockIcon(StockIconIdentifier.Server);

        /// <summary>
        ///  Icon for a printer. 
        /// </summary>
        public StockIcon Printer => GetStockIcon(StockIconIdentifier.Printer);

        /// <summary>
        /// Icon for My Network places.
        /// </summary>
        public StockIcon MyNetwork => GetStockIcon(StockIconIdentifier.MyNetwork);

        /// <summary>
        /// Icon for search (magnifying glass).
        /// </summary>
        public StockIcon Find => GetStockIcon(StockIconIdentifier.Find);

        /// <summary>
        ///  Icon for help.     
        /// </summary>
        public StockIcon Help => GetStockIcon(StockIconIdentifier.Help);

        /// <summary>
        ///  Icon for an overlay indicating shared items.        
        /// </summary>
        public StockIcon Share => GetStockIcon(StockIconIdentifier.Share);

        /// <summary>
        ///  Icon for an overlay indicating shortcuts to items.
        /// </summary>
        public StockIcon Link => GetStockIcon(StockIconIdentifier.Link);

        /// <summary>
        /// Icon for an overlay for slow items.
        /// </summary>
        public StockIcon SlowFile => GetStockIcon(StockIconIdentifier.SlowFile);

        /// <summary>
        ///  Icon for a empty recycle bin.
        /// </summary>
        public StockIcon Recycler => GetStockIcon(StockIconIdentifier.Recycler);

        /// <summary>
        ///  Icon for a full recycle bin.
        /// </summary>
        public StockIcon RecyclerFull => GetStockIcon(StockIconIdentifier.RecyclerFull);

        /// <summary>
        ///  Icon for audio CD media.
        /// </summary>
        public StockIcon MediaCDAudio => GetStockIcon(StockIconIdentifier.MediaCDAudio);

        /// <summary>
        ///  Icon for a security lock.
        /// </summary>
        public StockIcon Lock => GetStockIcon(StockIconIdentifier.Lock);

        /// <summary>
        ///  Icon for a auto list.
        /// </summary>
        public StockIcon AutoList => GetStockIcon(StockIconIdentifier.AutoList);

        /// <summary>
        /// Icon for a network printer.
        /// </summary>
        public StockIcon PrinterNet => GetStockIcon(StockIconIdentifier.PrinterNet);

        /// <summary>
        ///  Icon for a server share.
        /// </summary>
        public StockIcon ServerShare => GetStockIcon(StockIconIdentifier.ServerShare);

        /// <summary>
        ///  Icon for a Fax printer.
        /// </summary>
        public StockIcon PrinterFax => GetStockIcon(StockIconIdentifier.PrinterFax);

        /// <summary>
        /// Icon for a networked Fax printer.
        /// </summary>
        public StockIcon PrinterFaxNet => GetStockIcon(StockIconIdentifier.PrinterFaxNet);

        /// <summary>
        ///  Icon for print to file.
        /// </summary>
        public StockIcon PrinterFile => GetStockIcon(StockIconIdentifier.PrinterFile);

        /// <summary>
        /// Icon for a stack.
        /// </summary>
        public StockIcon Stack => GetStockIcon(StockIconIdentifier.Stack);

        /// <summary>
        ///  Icon for a SVCD media.
        /// </summary>
        public StockIcon MediaSvcd => GetStockIcon(StockIconIdentifier.MediaSvcd);

        /// <summary>
        ///  Icon for a folder containing other items.
        /// </summary>
        public StockIcon StuffedFolder => GetStockIcon(StockIconIdentifier.StuffedFolder);

        /// <summary>
        ///  Icon for an unknown drive.
        /// </summary>
        public StockIcon DriveUnknown => GetStockIcon(StockIconIdentifier.DriveUnknown);

        /// <summary>
        ///  Icon for a DVD drive. 
        /// </summary>
        public StockIcon DriveDvd => GetStockIcon(StockIconIdentifier.DriveDvd);

        /// <summary>
        /// Icon for DVD media.
        /// </summary>
        public StockIcon MediaDvd => GetStockIcon(StockIconIdentifier.MediaDvd);

        /// <summary>
        ///  Icon for DVD-RAM media.   
        /// </summary>
        public StockIcon MediaDvdRam => GetStockIcon(StockIconIdentifier.MediaDvdRam);

        /// <summary>
        /// Icon for DVD-RW media.
        /// </summary>
        public StockIcon MediaDvdRW => GetStockIcon(StockIconIdentifier.MediaDvdRW);

        /// <summary>
        ///  Icon for DVD-R media.
        /// </summary>
        public StockIcon MediaDvdR => GetStockIcon(StockIconIdentifier.MediaDvdR);

        /// <summary>
        ///  Icon for a DVD-ROM media.
        /// </summary>
        public StockIcon MediaDvdRom => GetStockIcon(StockIconIdentifier.MediaDvdRom);

        /// <summary>
        ///  Icon for CD+ (Enhanced CD) media.
        /// </summary>
        public StockIcon MediaCDAudioPlus => GetStockIcon(StockIconIdentifier.MediaCDAudioPlus);

        /// <summary>
        ///  Icon for CD-RW media.
        /// </summary>
        public StockIcon MediaCDRW => GetStockIcon(StockIconIdentifier.MediaCDRW);

        /// <summary>
        ///  Icon for a CD-R media.
        /// </summary>
        public StockIcon MediaCDR => GetStockIcon(StockIconIdentifier.MediaCDR);

        /// <summary>
        ///  Icon burning a CD.
        /// </summary>
        public StockIcon MediaCDBurn => GetStockIcon(StockIconIdentifier.MediaCDBurn);

        /// <summary>
        ///  Icon for blank CD media.
        /// </summary>
        public StockIcon MediaBlankCD => GetStockIcon(StockIconIdentifier.MediaBlankCD);

        /// <summary>
        ///  Icon for CD-ROM media.
        /// </summary>
        public StockIcon MediaCDRom => GetStockIcon(StockIconIdentifier.MediaCDRom);

        /// <summary>
        ///  Icon for audio files.
        /// </summary>
        public StockIcon AudioFiles => GetStockIcon(StockIconIdentifier.AudioFiles);

        /// <summary>
        ///  Icon for image files.
        /// </summary>
        public StockIcon ImageFiles => GetStockIcon(StockIconIdentifier.ImageFiles);

        /// <summary>
        ///  Icon for video files.
        /// </summary>
        public StockIcon VideoFiles => GetStockIcon(StockIconIdentifier.VideoFiles);

        /// <summary>
        ///  Icon for mixed Files.
        /// </summary>
        public StockIcon MixedFiles => GetStockIcon(StockIconIdentifier.MixedFiles);

        /// <summary>
        /// Icon for a folder back.
        /// </summary>
        public StockIcon FolderBack => GetStockIcon(StockIconIdentifier.FolderBack);

        /// <summary>
        ///  Icon for a folder front.
        /// </summary>
        public StockIcon FolderFront => GetStockIcon(StockIconIdentifier.FolderFront);

        /// <summary>
        ///  Icon for a security shield. Use for UAC prompts only.
        /// </summary>
        public StockIcon Shield => GetStockIcon(StockIconIdentifier.Shield);

        /// <summary>
        ///  Icon for a warning.
        /// </summary>
        public StockIcon Warning => GetStockIcon(StockIconIdentifier.Warning);

        /// <summary>
        ///  Icon for an informational message.
        /// </summary>
        public StockIcon Info => GetStockIcon(StockIconIdentifier.Info);

        /// <summary>
        ///  Icon for an error message.
        /// </summary>
        public StockIcon Error => GetStockIcon(StockIconIdentifier.Error);

        /// <summary>
        ///  Icon for a key.
        /// </summary>
        public StockIcon Key => GetStockIcon(StockIconIdentifier.Key);

        /// <summary>
        ///  Icon for software.
        /// </summary>
        public StockIcon Software => GetStockIcon(StockIconIdentifier.Software);

        /// <summary>
        ///  Icon for a rename.
        /// </summary>
        public StockIcon Rename => GetStockIcon(StockIconIdentifier.Rename);

        /// <summary>
        ///  Icon for delete.
        /// </summary>
        public StockIcon Delete => GetStockIcon(StockIconIdentifier.Delete);

        /// <summary>
        ///  Icon for audio DVD media.
        /// </summary>
        public StockIcon MediaAudioDvd => GetStockIcon(StockIconIdentifier.MediaAudioDvd);

        /// <summary>
        ///  Icon for movie DVD media.
        /// </summary>
        public StockIcon MediaMovieDvd => GetStockIcon(StockIconIdentifier.MediaMovieDvd);

        /// <summary>
        ///  Icon for enhanced CD media.
        /// </summary>
        public StockIcon MediaEnhancedCD => GetStockIcon(StockIconIdentifier.MediaEnhancedCD);

        /// <summary>
        ///  Icon for enhanced DVD media.
        /// </summary>
        public StockIcon MediaEnhancedDvd => GetStockIcon(StockIconIdentifier.MediaEnhancedDvd);

        /// <summary>
        ///  Icon for HD-DVD media.
        /// </summary>
        public StockIcon MediaHDDvd => GetStockIcon(StockIconIdentifier.MediaHDDvd);

        /// <summary>
        ///  Icon for BluRay media.
        /// </summary>
        public StockIcon MediaBluRay => GetStockIcon(StockIconIdentifier.MediaBluRay);

        /// <summary>
        ///  Icon for VCD media.
        /// </summary>
        public StockIcon MediaVcd => GetStockIcon(StockIconIdentifier.MediaVcd);

        /// <summary>
        ///  Icon for DVD+R media.
        /// </summary>
        public StockIcon MediaDvdPlusR => GetStockIcon(StockIconIdentifier.MediaDvdPlusR);

        /// <summary>
        ///  Icon for DVD+RW media.
        /// </summary>
        public StockIcon MediaDvdPlusRW => GetStockIcon(StockIconIdentifier.MediaDvdPlusRW);

        /// <summary>
        ///  Icon for desktop computer.
        /// </summary>
        public StockIcon DesktopPC => GetStockIcon(StockIconIdentifier.DesktopPC);

        /// <summary>
        ///  Icon for mobile computer (laptop/notebook).
        /// </summary>
        public StockIcon MobilePC => GetStockIcon(StockIconIdentifier.MobilePC);

        /// <summary>
        ///  Icon for users.
        /// </summary>
        public StockIcon Users => GetStockIcon(StockIconIdentifier.Users);

        /// <summary>
        ///  Icon for smart media.
        /// </summary>
        public StockIcon MediaSmartMedia => GetStockIcon(StockIconIdentifier.MediaSmartMedia);

        /// <summary>
        ///  Icon for compact flash.
        /// </summary>
        public StockIcon MediaCompactFlash => GetStockIcon(StockIconIdentifier.MediaCompactFlash);

        /// <summary>
        ///  Icon for a cell phone.
        /// </summary>
        public StockIcon DeviceCellPhone => GetStockIcon(StockIconIdentifier.DeviceCellPhone);

        /// <summary>
        ///  Icon for a camera.
        /// </summary>
        public StockIcon DeviceCamera => GetStockIcon(StockIconIdentifier.DeviceCamera);

        /// <summary>
        ///  Icon for video camera.
        /// </summary>
        public StockIcon DeviceVideoCamera => GetStockIcon(StockIconIdentifier.DeviceVideoCamera);

        /// <summary>
        ///  Icon for audio player.
        /// </summary>
        public StockIcon DeviceAudioPlayer => GetStockIcon(StockIconIdentifier.DeviceAudioPlayer);

        /// <summary>
        ///  Icon for connecting to network.
        /// </summary>
        public StockIcon NetworkConnect => GetStockIcon(StockIconIdentifier.NetworkConnect);

        /// <summary>
        ///  Icon for the Internet.
        /// </summary>
        public StockIcon Internet => GetStockIcon(StockIconIdentifier.Internet);

        /// <summary>
        ///  Icon for a ZIP file.
        /// </summary>
        public StockIcon ZipFile => GetStockIcon(StockIconIdentifier.ZipFile);

        /// <summary>
        /// Icon for settings.
        /// </summary>
        public StockIcon Settings => GetStockIcon(StockIconIdentifier.Settings);

        /// <summary>
        /// HDDVD Drive (all types)
        /// </summary>
        public StockIcon DriveHDDVD => GetStockIcon(StockIconIdentifier.DriveHDDVD);

        /// <summary>
        /// Icon for BluRay Drive (all types)
        /// </summary>
        public StockIcon DriveBluRay => GetStockIcon(StockIconIdentifier.DriveBluRay);

        /// <summary>
        /// Icon for HDDVD-ROM Media
        /// </summary>
        public StockIcon MediaHDDVDROM => GetStockIcon(StockIconIdentifier.MediaHDDVDROM);

        /// <summary>
        /// Icon for HDDVD-R Media
        /// </summary>
        public StockIcon MediaHDDVDR => GetStockIcon(StockIconIdentifier.MediaHDDVDR);

        /// <summary>
        /// Icon for HDDVD-RAM Media
        /// </summary>
        public StockIcon MediaHDDVDRAM => GetStockIcon(StockIconIdentifier.MediaHDDVDRAM);

        /// <summary>
        /// Icon for BluRay ROM Media
        /// </summary>
        public StockIcon MediaBluRayROM => GetStockIcon(StockIconIdentifier.MediaBluRayROM);

        /// <summary>
        /// Icon for BluRay R Media
        /// </summary>
        public StockIcon MediaBluRayR => GetStockIcon(StockIconIdentifier.MediaBluRayR);

        /// <summary>
        /// Icon for BluRay RE Media (Rewriable and RAM)
        /// </summary>
        public StockIcon MediaBluRayRE => GetStockIcon(StockIconIdentifier.MediaBluRayRE);

        /// <summary>
        /// Icon for Clustered disk
        /// </summary>
        public StockIcon ClusteredDisk => GetStockIcon(StockIconIdentifier.ClusteredDisk);

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the existing stock icon from the internal cache, or creates a new one
        /// based on the current settings if it's not in the cache.
        /// </summary>
        /// <param name="stockIconIdentifier">Unique identifier for the requested stock icon</param>
        /// <returns>Stock Icon based on the identifier given (either from the cache or created new)</returns>
        private StockIcon GetStockIcon(StockIconIdentifier stockIconIdentifier)
        {
            // Check the cache first
            if (stockIconCache[stockIconIdentifier] != null)
                return stockIconCache[stockIconIdentifier];
            else
            {
                // Create a new icon based on our default settings
                var icon = new StockIcon(stockIconIdentifier, defaultSize, isLinkOverlay, isSelected);

                try
                {
                    // Add it to the cache
                    stockIconCache[stockIconIdentifier] = icon;
                }
                catch
                {
                    icon.Dispose();
                    throw;
                }

                // Return 
                return icon;
            }
        }

        private ICollection<StockIcon> GetAllStockIcons()
        {
            // Create a list of stock Identifiers
            var ids = new StockIconIdentifier[stockIconCache.Count];
            stockIconCache.Keys.CopyTo(ids, 0);

            // For each identifier, if our cache is null, create a new stock icon
            foreach (var id in ids)
            {
                if (stockIconCache[id] == null)
                    GetStockIcon(id);
            }

            // return the list of stock icons
            return stockIconCache.Values;
        }


        #endregion

    }


}
