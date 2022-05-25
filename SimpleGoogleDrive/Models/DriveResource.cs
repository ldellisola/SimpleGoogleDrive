using SimpleGoogleDrive.Exceptions;

namespace SimpleGoogleDrive.Models;

public class DriveResource
{
    private readonly GoogleDriveService? _service;

    public DriveResource(GoogleDriveService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Name of the resource
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Google Drive Id of the resource
    /// </summary> 
    public string Id { get; set; }

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
    public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// It downloads the resource. If it is a folder it will not download the resources within
    /// </summary>
    /// <param name="destination">Local path where to store the resource. If it exists, it will overwrite it</param>
    /// <param name="onProgress">Function to run on downdload progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public Task Download(string destination, Action<long, long?>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        return _service?.DownloadResource(this, destination, onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It exports the resource. If it is a folder it will not download the resources within
    /// </summary>
    /// <param name="destination">Local path where to store the resource. If it exists, it will overwrite it</param>
    /// <param name="exportType"></param>
    /// <param name="onProgress">Function to run on downdload progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public Task Export(string destination, MimeType exportType = default, Action<long, long?>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        return _service?.ExportResource(this, destination, exportType, onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It exports the resource
    /// </summary>
    /// <param name="stream">Stream to store the data in</param>
    /// <param name="exportType"></param>
    /// <param name="onProgress">Function to run on downdload progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public Task Export(MemoryStream stream, MimeType exportType = default, Action<long, long?>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        return _service?.ExportResource(this, stream,exportType, onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It downloads the resource
    /// </summary>
    /// <param name="stream">Stream to store the data in</param>
    /// <param name="onProgress">Function to run on downdload progress. The first parameter is the total uploaded data and the second one is the total file size, bot in bytes</param>
    /// <param name="onFailure">Function to run if the download fails</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public Task Download(MemoryStream stream, Action<long, long?>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        return _service?.DownloadResource(this, stream, onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It deletes the resource
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public Task Delete(CancellationToken token = default)
    {
        return _service?.DeleteResource(this, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It retrieves all the resources of a folder.
    /// </summary>
    /// <param name="parameters">Extra parameters for the query</param>
    /// <param name="deepSearch">If true it will bring all the child resources</param> 
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public async Task<IEnumerable<DriveResource>> GetInnerResources(QueryBuilder? parameters = default, bool deepSearch = false, CancellationToken token = default)
    {
        if (Type != MimeType.Folder)
            return Enumerable.Empty<DriveResource>();

        var query = new QueryBuilder().IsParent(Id).And(parameters);

        var resources = new List<DriveResource>();

        foreach (var resource in await (_service?.QueryResources(query, token) ?? throw new ServiceNotAuthenticatedException()))
        {
            resources.Add(resource);
        }

        if (deepSearch)
        {
            var folders = await (_service?.QueryResources(new QueryBuilder().IsType(MimeType.Folder).And().IsParent(Id), token) ?? throw new ServiceNotAuthenticatedException());

            foreach (var folder in folders)
            {
                resources.AddRange(await folder.GetInnerResources(parameters, deepSearch, token));
            }
        }

        return resources;

    }

    /// <summary>
    /// It retrieves the full name of the file, including the path
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public async Task<string> GetFullName(CancellationToken token = default)
    {
        return await (_service?.GetResourceFullName(this, token) ?? throw new ServiceNotAuthenticatedException());
    }

    /// <summary>
    /// It returns the parent folder, if it exists.
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public async Task<DriveResource?> GetParent(CancellationToken token = default)
    {
        if (Parent == null)
            return null;
        else
            return await _service?.GetResource(Parent, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It copies the resource to another location in Google Drive. It does not work for folders
    /// </summary>
    /// <param name="destination">Path on Google Drive to store the copy of the resource</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The new resource created on the destination path</returns>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    /// <exception cref="FolderCannotBeCopiedException">It's thrown if the resource to be copied is a folder</exception>
    public Task<DriveResource?> Copy(string destination, CancellationToken token = default)
    {
        if (Type == MimeType.Folder)
            throw new FolderCannotBeCopiedException();

        return _service?.CopyResource(this, destination, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It copies the resource to another location in Google Drive. It does not work for folders
    /// </summary>
    /// <param name="parentFolder">Folder where to copy the resource</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The new resource created on the destination path</returns>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    /// <exception cref="FolderCannotBeCopiedException">It's thrown if the resource to be copied is a folder</exception>
    public Task<DriveResource?> Copy(DriveResource parentFolder, CancellationToken token = default)
    {
        if (Type == MimeType.Folder)
            throw new FolderCannotBeCopiedException();

        return _service?.CopyResource(this, parentFolder, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It updates a resource's name and public properties on Google Drive with the ones on this object
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    public Task Update(CancellationToken token = default)
    {
        return _service?.UpdateResource(this, token) ?? throw new ServiceNotAuthenticatedException();
    }

    /// <summary>
    /// It updates a resource's name, public properties and contents on Google Drive with the ones on this object
    /// </summary>
    /// <param name="file">File with the content to be uploaded</param>
    /// <param name="onProgress">Function to run on each progress update. The parameters are the total amount of bytes uplodaded and the size of the file</param>
    /// <param name="onFailure">Function to run if the upload fails</param>
    /// <param name="token">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
    /// <exception cref="FileNotFoundException">It's thrown if the file does not exists</exception>
    public Task Update(FileInfo file, Action<long, long>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
    {
        if (!file.Exists)
            throw new FileNotFoundException();

        return _service?.UpdateResource(this, file.OpenRead(), file.MimeType().GetString(), onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();

    }
        

    public enum MimeType
    {
        [MimeType("unknown/unkown")]
        Unknown = -1,
        [MimeType("application/vnd.google-apps.folder")]
        Folder,
        [MimeType("video/x-matroska")]
        MKV,
        [MimeType("video/x-flv")]
        FLV,
        [MimeType("video/mp4")]
        MP4,
        [MimeType("video/quicktime")]
        MOV,
        [MimeType("video/x-msvideo")]
        AVI,
        [MimeType("video/x-ms-wmv")]
        WMV,
        [MimeType("text/plain")]
        TXT,
        [MimeType("text/html")]
        HTML,
        [MimeType("application/zip")]
        ZIP,
        [MimeType("application/rtf")]
        RTF,
        [MimeType("application/vnd.oasis.opendocument.text")]
        OpenOfficeDoc,
        [MimeType("application/pdf")]
        PDF,
        [MimeType("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        MSWord,
        [MimeType("application/epub+zip")]
        EPUB,
        [MimeType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        MSExcel,
        [MimeType("application/x-vnd.oasis.opendocument.spreadsheet")]
        OpenOfficeSheet,
        [MimeType("text/csv")]
        CSV,
        [MimeType("text/tab-separated-values")]
        TSV,
        [MimeType("image/jpeg")]
        JPEG,
        [MimeType("image/png")]
        PNG,
        [MimeType("image/svg+xml")]
        SVG,
        [MimeType("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        MsPowerPoint,
        [MimeType("application/vnd.oasis.opendocument.presentation")]
        OpenOfficePresentation,
        [MimeType("application/vnd.google-apps.script+json")]
        JSON,
        [MimeType("application/vnd.google-apps.document", MSWord)]
        GoogleDocs,
        [MimeType("application/vnd.google-apps.drive-sdk")]
        ThirdPartyShortcut,
        [MimeType("application/vnd.google-apps.drawing",PNG)]
        GoogleDrawing,
        [MimeType("application/vnd.google-apps.presentation",MsPowerPoint)]
        GoogleSlides,
        [MimeType("application/vnd.google-apps.script",JSON)]
        GoogleAppScript,
        [MimeType("application/vnd.google-apps.spreadsheet",MSExcel)]
        GoogleSpreadsheet,
        [MimeType("application/vnd.google-apps.video")]
        GenericVideo,
        [MimeType("application/vnd.google-apps.photo")]
        GenericPhoto,
        [MimeType("application/vnd.jgraph.mxfile")]
        DrawIODiagram,
    }
}