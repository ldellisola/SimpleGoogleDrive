using SimpleGoogleDrive.Exceptions;

namespace SimpleGoogleDrive.Models
{
    public class DriveResource
    {
        private readonly GoogleDriveService? service = null;

        public DriveResource(GoogleDriveService service)
        {
            this.service = service;
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
            return service?.DownloadResource(this, destination, onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();
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
            return service?.DownloadResource(this, stream, onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();
        }

        /// <summary>
        /// It deletes the resource
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
        public Task Delete(CancellationToken token = default)
        {
            return service?.DeleteResource(this, token) ?? throw new ServiceNotAuthenticatedException();
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

            foreach (var resource in await (service?.QueryResources(query, token) ?? throw new ServiceNotAuthenticatedException()))
            {
                resources.Add(resource);
            }

            if (deepSearch)
            {
                var folders = await (service?.QueryResources(new QueryBuilder().IsType(MimeType.Folder).And().IsParent(Id), token) ?? throw new ServiceNotAuthenticatedException());

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
            return await (service?.GetResourceFullName(this, token) ?? throw new ServiceNotAuthenticatedException());
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
                return await service?.GetResource(Parent, token) ?? throw new ServiceNotAuthenticatedException();
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

            return service?.CopyResource(this, destination, token) ?? throw new ServiceNotAuthenticatedException();
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

            return service?.CopyResource(this, parentFolder, token) ?? throw new ServiceNotAuthenticatedException();
        }

        /// <summary>
        /// It updates a resource's name and public properties on Google Drive with the ones on this object
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ServiceNotAuthenticatedException">It's thrown if the service is not authenticated</exception>
        public Task Update(CancellationToken token = default)
        {
            return service?.UpdateResource(this, token) ?? throw new ServiceNotAuthenticatedException();
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

            return service?.UpdateResource(this, file.OpenRead(), file.MimeType().GetString(), onProgress, onFailure, token) ?? throw new ServiceNotAuthenticatedException();

        }

















        public enum MimeType
        {
            [StringValue("unknown/unkown")]
            Unknown = -1,
            [StringValue("application/vnd.google-apps.folder")]
            Folder,
            [StringValue("video/x-matroska")]
            MKV,
            [StringValue("video/x-flv")]
            FLV,
            [StringValue("video/mp4")]
            MP4,
            [StringValue("video/quicktime")]
            MOV,
            [StringValue("video/x-msvideo")]
            AVI,
            [StringValue("video/x-ms-wmv")]
            WMV,
            [StringValue("text/plain")]
            TXT
        }



    }
}
