using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing;
using System.IO;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// This interface exposes the <see cref="ConstructBitmap"/> function for initializing the Thumbnail Provider with file information. This
    /// interface can be used in conjunction with the other intialization interfaces, but only 1 will be accessed according to the priorities
    /// preset by the Windows Shell: <see cref="IThumbnailFromStream"/><see cref="IThumbnailFromShellObject"/><see cref="IThumbnailFromFile"/>
    /// </summary>
    public interface IThumbnailFromFile
    {
        /// <summary>
        /// Provides the <see cref="FileInfo"/> to the item from which a thumbnail should be created. <remarks>Only 32bpp bitmaps support
        /// adornments. While 24bpp bitmaps will be displayed they will not display adornments. Additional guidelines for developing
        /// thumbnails can be found at http://msdn.microsoft.com/en-us/library/cc144115(v=VS.85).aspx </remarks>
        /// </summary>
        /// <param name="info">FileInfo to initialize the thumbnail</param>
        /// <param name="sideSize">Square side dimension in which the thumbnail should fit; the thumbnail will be scaled otherwise.</param>
        /// <returns>Generated thumbnail</returns>
        Bitmap ConstructBitmap(FileInfo info, int sideSize);
    }

    /// <summary>
    /// This interface exposes the <see cref="ConstructBitmap"/> function for initializing the Thumbnail Provider with a
    /// <see cref="ShellObject"/>. This interface can be used in conjunction with the other intialization interfaces, but only 1 will be
    /// accessed according to the priorities preset by the Windows Shell: <see cref="IThumbnailFromStream"/><see cref="IThumbnailFromShellObject"/><see cref="IThumbnailFromFile"/>
    /// </summary>
    public interface IThumbnailFromShellObject
    {
        /// <summary>
        /// Provides the <see cref="ShellObject"/> to the item from which a thumbnail should be created. <remarks>Only 32bpp bitmaps support
        /// adornments. While 24bpp bitmaps will be displayed they will not display adornments. Additional guidelines for developing
        /// thumbnails can be found at http://msdn.microsoft.com/en-us/library/cc144115(v=VS.85).aspx </remarks>
        /// </summary>
        /// <param name="shellObject">ShellObject to initialize the thumbnail</param>
        /// <param name="sideSize">Square side dimension in which the thumbnail should fit; the thumbnail will be scaled otherwise.</param>
        /// <returns>Generated thumbnail</returns>
        Bitmap ConstructBitmap(ShellObject shellObject, int sideSize);
    }

    /// <summary>
    /// This interface exposes the <see cref="ConstructBitmap"/> function for initializing the Thumbnail Provider with a
    /// <see cref="Stream"/>. If this interfaces is not used, then the handler must opt out of process isolation. This interface can be used
    /// in conjunction with the other intialization interfaces, but only 1 will be accessed according to the priorities preset by the Windows
    /// Shell: <see cref="IThumbnailFromStream"/><see cref="IThumbnailFromShellObject"/><see cref="IThumbnailFromFile"/>
    /// </summary>
    public interface IThumbnailFromStream
    {
        /// <summary>
        /// Provides the <see cref="Stream"/> to the item from which a thumbnail should be created. <remarks>Only 32bpp bitmaps support
        /// adornments. While 24bpp bitmaps will be displayed they will not display adornments. Additional guidelines for developing
        /// thumbnails can be found at http://msdn.microsoft.com/en-us/library/cc144115(v=VS.85).aspx </remarks>
        /// </summary>
        /// <param name="stream">Stream to initialize the thumbnail</param>
        /// <param name="sideSize">Square side dimension in which the thumbnail should fit; the thumbnail will be scaled otherwise.</param>
        /// <returns>Generated thumbnail</returns>
        Bitmap ConstructBitmap(Stream stream, int sideSize);
    }
}