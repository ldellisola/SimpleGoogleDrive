using System.Runtime.CompilerServices;
using SimpleGoogleDrive.Exceptions;

namespace SimpleGoogleDrive.Models;

/// <summary>
/// 
/// </summary>
public class DriveResource
{
    private readonly GoogleDriveService? _service;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    public DriveResource(GoogleDriveService service)
    {
        _service = service;
    }

    /// <summary>
    /// Name of the resource
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Full path of the resource
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Google Drive Id of the resource
    /// </summary> 
    public string Id { get; set; } = null!;

    /// <summary>
    /// List of the parent folders' ids
    /// </summary>
    public string? Parent { get; set; } = null;

    /// <summary>
    /// Type of the resource
    /// </summary>
    public MimeType Type { get; set; }

    /// <summary>
    /// Size in bytes. If the resource is a folder it may not exist.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// It says if the file is on the recycle bin
    /// </summary>
    public bool IsTrashed { get; set; }

    /// <summary>
    /// Public file properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// It downloads the resource. If it is a folder it will not download the resources within
    /// </summary>
    /// <param name="destination">Local path where to store the resource. If it exists, it will overwrite it</param>
    /// <param name="onProgress">Function to run on download progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    public Task Download(string destination, Action<long, long?>? onProgress = default,
        Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return _service.DownloadResource(this, destination, onProgress, onFailure, token);
    }

    /// <summary>
    /// It exports the resource. If it is a folder it will not download the resources within
    /// </summary>
    /// <param name="destination">Local path where to store the resource. If it exists, it will overwrite it</param>
    /// <param name="exportType"></param>
    /// <param name="onProgress">Function to run on download progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    public Task Export(string destination, MimeType exportType = default, Action<long, long?>? onProgress = default,
        Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return _service.ExportResource(this, destination, exportType, onProgress, onFailure, token);
    }

    /// <summary>
    /// It exports the resource
    /// </summary>
    /// <param name="stream">Stream to store the data in</param>
    /// <param name="exportType"></param>
    /// <param name="onProgress">Function to run on download progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    public Task Export(MemoryStream stream, MimeType exportType = default, Action<long, long?>? onProgress = default,
        Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return _service.ExportResource(this, stream, exportType, onProgress, onFailure, token);
    }

    /// <summary>
    /// It downloads the resource
    /// </summary>
    /// <param name="stream">Stream to store the data in</param>
    /// <param name="onProgress">Function to run on download progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    public Task Download(MemoryStream stream, Action<long, long?>? onProgress = default,
        Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return _service.DownloadResource(this, stream, onProgress, onFailure, token);
    }

    /// <summary>
    /// It deletes the resource
    /// </summary>
    /// <param name="token">Cancellation token</param>
    public Task Delete(CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return _service.DeleteResource(this, token);
    }

    /// <summary>
    /// It retrieves all the resources of a folder.
    /// </summary>
    /// <param name="parameters">Extra parameters for the query</param>
    /// <param name="deepSearch">If true it will bring all the child resources</param> 
    /// <param name="token">Cancellation token</param>
    public async IAsyncEnumerable<DriveResource> GetInnerResources(QueryBuilder? parameters = default,
        bool deepSearch = false, [EnumeratorCancellation] CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);

        if (Type is not MimeType.Folder)
            yield break;
        
        var folders = new Queue<DriveResource>(new[] {this});
        Task searchParentTask = Task.CompletedTask;
        while (folders.Any())
        {
            var folder = folders.Dequeue();

            if (deepSearch)
            {
                // TODO: See if I can implement the query mechanism so I only have to do the query once.
                // I will be bringing more data but it may be worth it.
                var getChildrenQuery = new QueryBuilder().IsType(MimeType.Folder).And().IsParent(folder.Id);
                searchParentTask = _service.QueryResources(getChildrenQuery, token).ForEachAsync(resource => folders.Enqueue(resource));
            }
            
            var query = new QueryBuilder().IsParent(folder.Id).And(parameters);
            await foreach (var resource in _service.QueryResources(query, token)) 
                yield return resource;

            await searchParentTask;
        }
    }

    /// <summary>
    /// It retrieves the full name of the file, including the path
    /// </summary>
    /// <param name="token">Cancellation token</param>
    public async Task<string> GetFullName(CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        FullName ??= await _service.GetResourceFullName(this, token);
        return FullName;
    }


    /// <summary>
    /// It returns the parent folder, if it exists.
    /// </summary>
    /// <param name="token">Cancellation token</param>
    public Task<DriveResource?> GetParent(CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return Parent switch
        {
            null => Task.FromResult<DriveResource?>(null),
            _ => _service.GetResource(Parent, token)
        };
    }

    /// <summary>
    /// It copies the resource to another location in Google Drive. It does not work for folders
    /// </summary>
    /// <param name="destination">Path on Google Drive to store the copy of the resource</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The new resource created on the destination path</returns>
    /// <exception cref="FoldersCannotBeCopiedException">It's thrown if the resource to be copied is a folder</exception>
    public Task<DriveResource?> Copy(string destination, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        
        if (Type is MimeType.Folder)
            throw new FoldersCannotBeCopiedException();

        return _service.CopyResource(this, destination, token);
    }

    /// <summary>
    /// It copies the resource to another location in Google Drive. It does not work for folders
    /// </summary>
    /// <param name="parentFolder">Folder where to copy the resource</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The new resource created on the destination path</returns>
    /// <exception cref="FoldersCannotBeCopiedException">It's thrown if the resource to be copied is a folder</exception>
    public Task<DriveResource?> Copy(DriveResource parentFolder, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        if (Type is MimeType.Folder)
            throw new FoldersCannotBeCopiedException();

        return _service.CopyResource(this, parentFolder, token);
    }

    /// <summary>
    /// It updates a resource's name and public properties on Google Drive with the ones on this object
    /// </summary>
    /// <param name="token">Cancellation token</param>
    public Task Update(CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        return _service.UpdateResource(this, token);
    }

    /// <summary>
    /// It updates a resource's name, public properties and contents on Google Drive with the ones on this object
    /// </summary>
    /// <param name="file">File with the content to be uploaded</param>
    /// <param name="onProgress">Function to run on each progress update. The parameters are the total amount of bytes uploaded and the size of the file</param>
    /// <param name="onFailure">Function to run if the upload fails</param>
    /// <param name="token">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">It's thrown if the file does not exists</exception>
    public Task Update(FileInfo file, Action<long, long>? onProgress = default, Action<Exception>? onFailure = default,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(_service);
        if (!file.Exists)
            throw new FileNotFoundException();

        return _service.UpdateResource(this, file.OpenRead(), file.MimeType(), onProgress, onFailure, token);
    }


    /// <summary>
    /// Type of the resource
    /// </summary>
    public enum MimeType
    {
        /// <summary>
        /// The Mime type could not be detected or is not considered in this library
        /// </summary>
        [MimeType("unknown/unknown")] Unknown = -1,

        /// <summary>
        /// A Google Drive Folder 
        /// </summary>
        [MimeType("application/vnd.google-apps.folder")]
        Folder,
        
        /// <summary>
        /// A video with a .mkv extension
        /// </summary>
        [MimeType("video/x-matroska")] Mkv,
        
        /// <summary>
        /// A video with the .flv extension
        /// </summary>
        [MimeType("video/x-flv")] Flv,
        
        /// <summary>
        /// A video with the .mp4 extension
        /// </summary>
        [MimeType("video/mp4")] Mp4,
        
        /// <summary>
        /// A video with the .mov extension
        /// </summary>
        [MimeType("video/quicktime")] Mov,
        
        /// <summary>
        /// A video with the .avi extension
        /// </summary>
        [MimeType("video/x-msvideo")] Avi,
        
        /// <summary>
        /// A video with the .wmv extension
        /// </summary>
        [MimeType("video/x-ms-wmv")] Wmv,
        
        /// <summary>
        /// A text file
        /// </summary>
        [MimeType("text/plain")] Txt,
        
        /// <summary>
        /// An HTML file
        /// </summary>
        [MimeType("text/html")] Html,
        
        /// <summary>
        /// A zip file
        /// </summary>
        [MimeType("application/zip")] Zip,
        
        /// <summary>
        /// A rich text file
        /// </summary>
        [MimeType("application/rtf")] Rtf,

        /// <summary>
        /// An Open Office word document (Word equivalent)
        /// </summary>
        [MimeType("application/vnd.oasis.opendocument.text")]
        OpenOfficeDoc,
        
        /// <summary>
        /// A PDF file
        /// </summary>
        [MimeType("application/pdf")] Pdf,

        /// <summary>
        /// A Microsoft Word file
        /// </summary>
        [MimeType("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        MsWord,
        
        /// <summary>
        /// A digital book
        /// </summary>
        [MimeType("application/epub+zip")] Epub,

        /// <summary>
        /// An Excel spreadsheet
        /// </summary>
        [MimeType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        MsExcel,

        /// <summary>
        /// An Open Office spreadsheet document (Excel equivalent)
        /// </summary>
        [MimeType("application/x-vnd.oasis.opendocument.spreadsheet")]
        OpenOfficeSheet,
        
        /// <summary>
        /// A comma separated value file
        /// </summary>
        [MimeType("text/csv")] Csv,

        /// <summary>
        /// A tab separated value file
        /// </summary>
        [MimeType("text/tab-separated-values")]
        Tsv,
        
        /// <summary>
        /// A Jpeg image
        /// </summary>
        [MimeType("image/jpeg")] Jpeg,
        
        /// <summary>
        /// A Png image
        /// </summary>
        [MimeType("image/png")] Png,
        
        /// <summary>
        /// A Svg image
        /// </summary>
        [MimeType("image/svg+xml")] Svg,
        
        /// <summary>
        /// A PowerPoint presentation file
        /// </summary>
        [MimeType("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        MsPowerPoint,
        
        /// <summary>
        /// An Open Office presentation file (PowerPoint equivalent)
        /// </summary>
        [MimeType("application/vnd.oasis.opendocument.presentation")]
        OpenOfficePresentation,

        /// <summary>
        /// A Json file
        /// </summary>
        [MimeType("application/vnd.google-apps.script+json")]
        Json,

        /// <summary>
        /// A Google Docs file (Word equivalent)
        /// </summary>
        [MimeType("application/vnd.google-apps.document", MsWord)]
        GoogleDocs,

        /// <summary>
        /// A third party shortcut
        /// </summary>
        [MimeType("application/vnd.google-apps.drive-sdk")]
        ThirdPartyShortcut,
        
        /// <summary>
        /// A Google Drawing file
        /// </summary>
        [MimeType("application/vnd.google-apps.drawing", Png)]
        GoogleDrawing,

        /// <summary>
        /// A Google Slides file (PowerPoint equivalent)
        /// </summary>
        [MimeType("application/vnd.google-apps.presentation", MsPowerPoint)]
        GoogleSlides,

        /// <summary>
        /// A Google App Script File
        /// </summary>
        [MimeType("application/vnd.google-apps.script", Json)]
        GoogleAppScript,

        /// <summary>
        /// A Google Spreadsheet file (Excel equivalent)
        /// </summary>
        [MimeType("application/vnd.google-apps.spreadsheet", MsExcel)]
        GoogleSpreadsheet,

        /// <summary>
        /// A generic video file
        /// </summary>
        [MimeType("application/vnd.google-apps.video")]
        GenericVideo,

        /// <summary>
        /// A generic photo file
        /// </summary>
        [MimeType("application/vnd.google-apps.photo")]
        GenericPhoto,

        /// <summary>
        /// A Draw.IO diagram
        /// </summary>
        [MimeType("application/vnd.jgraph.mxfile")]
        DrawIoDiagram
    }
}