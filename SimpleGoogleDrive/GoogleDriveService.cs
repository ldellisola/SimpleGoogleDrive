using System.Runtime.CompilerServices;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using SimpleGoogleDrive.Exceptions;
using SimpleGoogleDrive.Models;
using File = Google.Apis.Drive.v3.Data.File;

namespace SimpleGoogleDrive
{
    /// <summary>
    /// Google Drive API
    /// </summary>
    public class GoogleDriveService : IDisposable
    {
        private readonly DriveAuthSettings _settings;
        private DriveService? _service;
        private readonly PathStorage _storage;
        private bool _disposedValue;
        private readonly string _fields = "id, name, parents, mimeType, properties, size, trashed";

        public int Calls = 0;

        /// <summary>
        /// Google Drive API
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="usePersistentStorage"></param>
        /// <param name="persistentStoragePath"></param>
        /// <exception cref="ArgumentException"></exception>
        public GoogleDriveService(DriveAuthSettings settings, bool usePersistentStorage = false, string? persistentStoragePath = default)
        {
            _storage = PathStorage.GetInstance(usePersistentStorage, persistentStoragePath);

            if (!settings.IsValid())
                throw new ArgumentException("Invalid DriveAuthorizationSettings");
                
            _settings = settings;
        }

        /// <summary>
        /// It authenticates the services with Google's server
        /// </summary>
        /// <param name="token">Cancellation Token</param>
        public async Task<GoogleDriveService> Authenticate(CancellationToken token = default)
        {
            ClientSecrets clientSecrets;

            if (_settings.Credentials is not null)
                clientSecrets = (await GoogleClientSecrets.FromFileAsync(_settings.Credentials?.FullName, token))
                    .Secrets;
            else
                clientSecrets = new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret,
                };

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                _settings.Scope,
                _settings.User,
                token,
                new FileDataStore(_settings.UserStore),
                _settings.Mode switch
                {
                    DriveAuthSettings.AuthMode.Web => new LocalServerCodeReceiver(),
                    DriveAuthSettings.AuthMode.Console => new PromptCodeReceiver(),
                    _ => throw new ArgumentOutOfRangeException()
                }
            );

            _service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _settings.ApplicationName
            });

            return this;
        }

        /// <summary>
        /// It stops the service's execution
        /// </summary>
        public void Stop()
        {
            PathStorage.Store();
        }

        /// <summary>
        /// It creates all the folders in the path. If the folders exists it does not do anything
        /// </summary>
        /// <param name="pathToFolder">It's the folder path to be created. For example "folder/newFolder/otherFolder"</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>It returns the created resource</returns>
        /// <exception cref="ResourceAlreadyExistsException">It is thrown if the folder already exists</exception>
        public async Task<DriveResource?> CreateFolder(string pathToFolder, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            var resource = await FindFolder(pathToFolder, token: token);

            if (resource is not null)
                throw new ResourceAlreadyExistsException(resource);

            var (path, folder) = pathToFolder.SplitPathFromResource();
            string? parentId = null;

            if (path is not null)
                parentId = (await FindFolder(path, token: token))?.Id ?? (await CreateFolder(path, token))?.Id;

            var metadata = new File()
            {
                Name = folder,
                MimeType = DriveResource.MimeType.Folder.GetString(),
                Parents = parentId != null ? new List<string?>() { parentId } : default
            };

            var request = _service.Files.Create(metadata);
            request.Fields = _fields;
            var response = await request.ExecuteAsync(token);

            _storage.Add(response.Id, pathToFolder);
            return MapToDriveResource(response);

        }

        /// <summary>
        /// It gets the full name, including the path and extension of a given resource
        /// </summary>
        /// <param name="fileId">Id of the Google Drive resource</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>It returns the full name of the resource</returns>
        private async Task<string?> GetResourceFullName(string fileId, CancellationToken token = default)
        {
            var resource = await GetResource(fileId, token);
            return resource switch
            {
                null => null,
                _ => await GetResourceFullName(resource)
            };
        }

        /// <summary>
        /// It gets the full name, including the path and extension of a given resource
        /// </summary>
        /// <param name="resource">Google Drive resource</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>It returns the full name of the resource</returns>
        public async Task<string> GetResourceFullName(DriveResource? resource, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(resource);
            var cachedResult = _storage.GetPath(resource.Id);
            if (cachedResult is not null)
                return cachedResult;

            var inRootQuery = new QueryBuilder()
                .IsParent("root")
                .And()
                .IsName(resource.Name);

            var inRoot = await QueryResources(inRootQuery).AnyAsync(token);

            if (inRoot)
                return resource.Name;
            
            var parentFullName = _storage.GetPath(resource.Parent!) ?? await GetResourceFullName(resource.Parent!, token);
            var fullName = new StringBuilder(parentFullName!).Append(resource.Name).ToString();
            _storage.Add(resource.Id,fullName);
            return fullName;
        }

        /// <summary>
        /// It retrieves a resource from Google Drive
        /// </summary>
        /// <param name="id">Id of the file</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The desired resource</returns>
        public async Task<DriveResource?> GetResource(string? id, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            ArgumentNullException.ThrowIfNull(id);
            Calls++;
            var request = _service.Files.Get(id);
            request.Fields = _fields;

            var resource = await request.ExecuteAsync(token);

            return MapToDriveResource(resource);
        }

        /// <summary>
        /// It maps a GoogleDrive File to a DriveResource
        /// </summary>
        /// <param name="resource">Google Drive resource</param>
        /// <returns>A DriveResource object</returns>
        private DriveResource? MapToDriveResource(File? resource)
        {
            return resource switch
            {
                null => null,
                _ => new DriveResource(this)
                {
                    Id = resource.Id,
                    Name = resource.Name,
                    Parent = resource.Parents?.FirstOrDefault(),
                    Type = resource.MimeType(),
                    Properties = resource.Properties?.ToDictionary(t => t.Key, t => t.Value) ??
                                 new Dictionary<string, string>(),
                    Size = resource.Size,
                    IsTrashed = resource.Trashed ?? false,
                }
            };
        }

        /// <summary>
        /// It queries Google Drive 
        /// </summary>
        /// <param name="queryBuilder">Query</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A list of Drive Resources</returns>
        public async IAsyncEnumerable<DriveResource> QueryResources(QueryBuilder queryBuilder, [EnumeratorCancellation] CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            string? pageToken = null;
            do
            {
                Calls++;
                FilesResource.ListRequest request = _service.Files.List();
                request.Q = queryBuilder.Build();
                request.PageToken = pageToken;
                request.Fields = $"nextPageToken,files({_fields})";
                request.PageSize = 1000;

                FileList result = await request.ExecuteAsync(token);
                pageToken = result.NextPageToken;

                foreach (File resource in result.Files) 
                    yield return MapToDriveResource(resource)!;

            } while (pageToken != null);
        }

        /// <summary>
        /// It finds a folder within Google Drive
        /// </summary>
        /// <param name="pathToFolder">Path on Google Drive to the folder</param>
        /// <param name="parameters"></param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>The resource, if the folder exists</returns>
        public async Task<DriveResource?> FindFolder(string pathToFolder, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            var folderId = _storage.GetId(pathToFolder);
            if (folderId is not null)
            {
                var folder = await GetResource(folderId, token);

                if (folder is not null && !folder.IsTrashed)
                    return folder;
            }

            var query = new QueryBuilder().IsType(DriveResource.MimeType.Folder).And(parameters);

            var resource = await FindResource(pathToFolder, query, token);

            if (resource is not null && !resource.IsTrashed)
                _storage.Add(resource.Id,pathToFolder);

            return resource;
        }

        /// <summary>
        /// It finds a File within Google Drive
        /// </summary>
        /// <param name="pathToFile">Path in GoogleDrive to the file</param>
        /// <param name="parameters">Extra query parameters</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>the resource, if it exists</returns>
        public Task<DriveResource?> FindFile(string? pathToFile, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            var query = new QueryBuilder().IsNotType(DriveResource.MimeType.Folder).And(parameters);
            return FindResource(pathToFile, query, token);

        }

        /// <summary>
        /// It finds a resource form Google drive using its path
        /// </summary>
        /// <param name="pathToResource">Path on google drive to the resource</param>
        /// <param name="parameters"></param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The requested Resource</returns>
        public async Task<DriveResource?> FindResource(string? pathToResource, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            if (pathToResource is null)
                return default;

            var (parentFolder, resource) = pathToResource.SplitPathFromResource();

            var parentFolderId = "root";

            if (parentFolder is not null)
            {
                parentFolderId = (await FindFolder(parentFolder, null, token))?.Id;

                if (parentFolderId is null)
                    return null;
            }
            
            var query = new QueryBuilder().IsName(resource).And().IsParent(parentFolderId).And(parameters);
            
            return await QueryResources(query, token).FirstOrDefaultAsync(token);
        }

        /// <summary>
        /// It deletes a resource from Google Drive
        /// </summary>
        /// <param name="resource">Resource to be deleted</param>
        /// <param name="token">Cancellation token</param>
        public async Task DeleteResource(DriveResource resource, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            await _service.Files.Delete(resource.Id).ExecuteAsync(token);
            
            if (resource.Type is DriveResource.MimeType.Folder)
                _storage.DeleteId(resource.Id);

        }

        /// <summary>
        /// It deletes a resource from Google Drive
        /// </summary>
        /// <param name="pathToResource">Path on Google Drive to the resource</param>
        /// <param name="parameters">Extra query parameters</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public async Task DeleteResource(string pathToResource, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            var (path, resourceName) = pathToResource.SplitPathFromResource();

            var query = new QueryBuilder().IsName(resourceName).And(parameters);

            if (path is not null)
            {
                var folderResource = await FindFolder(path, null, token);
                if (folderResource is not null)
                    query.IsParent(folderResource.Id);
            }

            var resource = await QueryResources(query, token).FirstOrDefaultAsync(token);

            if (resource is not null)
                await DeleteResource(resource, token);
        }

        /// <summary>
        /// It creates and uploads a file into google drive
        /// </summary>
        /// <param name="data">Data to upload</param>
        /// <param name="mimeType">Type of the file</param>
        /// <param name="destination">Where the file will be saved</param>
        /// <param name="properties">Public properties of the file</param>
        /// <param name="onProgress">Function that will run during upload. The parameters are the bytes sent and the total file size</param>
        /// <param name="onFailure">Function that will run if the upload fails. The parameter is the exception thrown</param>
        /// <param name="token"></param>
        /// <returns>Returns the Drive resource if the upload was successful. Null otherwise</returns>
        public async Task<DriveResource?> CreateFile(FileStream data, string mimeType, string destination, Dictionary<string, string>? properties = default,
            Action<long, long>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            
            var (destinationPath, destinationName) = destination.SplitPathFromResource();

            string? parentId = null;

            if (destinationPath is not null)
                parentId = (await FindFolder(destinationPath, token: token) ?? await CreateFolder(destinationPath, token))?.Id;

            var fileMetadata = new File()
            {
                Name = destinationName,
                Parents = parentId != null ? new List<string>() { parentId } : default,
                Properties = properties
            };


            var request = _service.Files.Create(fileMetadata, data, mimeType);

            DriveResource? resource = null;
            request.Fields = _fields;

            request.ResponseReceived += rest => resource = MapToDriveResource(rest);

            request.ProgressChanged += progress =>
            {
                if (progress.Status == Google.Apis.Upload.UploadStatus.Uploading && onProgress != null)
                {
                    onProgress(progress.BytesSent, request.ContentStream.Length);
                }
                else if (progress.Status == Google.Apis.Upload.UploadStatus.Failed && onFailure != null)
                {
                    onFailure(progress.Exception);
                }
            };

            await request.UploadAsync(token);
            
            return resource;
        }

        /// <summary>
        /// It creates and uploads a file into google drive
        /// </summary>
        /// <param name="file">File to be uploaded</param>
        /// <param name="destination">Where the file will be saved</param>
        /// <param name="properties">Public properties of the file</param>
        /// <param name="onProgress">Function that will run during upload. The parameters are the bytes sent and the total file size</param>
        /// <param name="onFailure">Function that will run if the upload fails. The parameter is the exception thrown</param>
        /// <param name="token"></param>
        /// <returns>Returns the Drive resource if the upload was successul. Null otherwise</returns>
        public async Task<DriveResource?> CreateFile(FileInfo file, string destination, Dictionary<string, string>? properties = default,
            Action<long, long>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            if (!file.Exists)
                return null;

            using (FileStream stream = file.OpenRead())
            {
                string mimeType = file.MimeType().GetString();
                return await CreateFile(stream, mimeType, destination, properties, onProgress, onFailure, token);
            }
        }

        /// <summary>
        /// It downloads a resource to a specific file
        /// </summary>
        /// <param name="pathToResource">Path to the resource on google drive</param>
        /// <param name="stream">Stream to store the data in</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        /// <returns>true if the pathToResource is valid</returns>
        public async Task<bool> DownloadResource(string pathToResource, MemoryStream stream, Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var resource = await FindResource(pathToResource, token: token);
            if (resource == null)
                return false;

            await DownloadResource(resource, stream, onProgress, onFailure, token);
            return true;
        }

        /// <summary>
        /// It downloads a resource to a specific file
        /// </summary>
        /// <param name="pathToResource">Path to the resource on google drive</param>
        /// <param name="destination">Local path where to store the resource</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        /// <returns>true if the pathToResource is valid</returns>
        public async Task<bool> DownloadResource(string pathToResource, string destination, Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var resource = await FindResource(pathToResource, token: token);
            if (resource == null)
                return false;

            await DownloadResource(resource, destination, onProgress, onFailure, token);
            return true;
        }

        /// <summary>
        /// It downloads a resource to a specific file
        /// </summary>
        /// <param name="resource">Resource to download</param>
        /// <param name="stream">Stream to store the data in</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        public async Task DownloadResource(DriveResource resource, Stream stream, Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            
            var request = _service.Files.Get(resource.Id);
            
            request.MediaDownloader.ProgressChanged += progress =>
            {
                if (progress.Status == Google.Apis.Download.DownloadStatus.Downloading && onProgress != default)
                    onProgress(progress.BytesDownloaded, resource.Size);
            };
            
            var response = await request.DownloadAsync(stream, token);

            if (response.Status == Google.Apis.Download.DownloadStatus.Failed && onFailure != default)
                onFailure(response.Exception);

        }

        


        /// <summary>
        /// It exports a resource to a specific file
        /// </summary>
        /// <param name="pathToResource">Path to the resource on google drive</param>
        /// <param name="destination">Local path where to store the resource</param>
        /// <param name="exportType">Mime type to export the resource</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        /// <returns>true if the pathToResource is valid</returns>
        public async Task<bool> ExportResource(
            string pathToResource, string destination, DriveResource.MimeType exportType = default,
            Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var resource = await FindResource(pathToResource, token: token);
            if (resource is null)
                return false;

            await ExportResource(resource, destination,exportType, onProgress, onFailure, token);
            return true;
        }
        /// <summary>
        /// It exports a resource to a specific file
        /// </summary>
        /// <param name="resource">Resource on google drive</param>
        /// <param name="stream">Stream to store the data</param>
        /// <param name="exportType">Mime type to export the resource</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        public async Task ExportResource(DriveResource resource, Stream stream, DriveResource.MimeType exportType = default,
            Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            if (exportType is default(DriveResource.MimeType))
                exportType = resource.Type.GetDefaultExportType() ??
                             throw new ResourceCannotBeExportedException(resource);

            if (resource.Size > 10 * 1e9)
                throw new ResourceTooBigForExportException(resource);

            var request = _service.Files.Export(resource.Id, exportType.GetString());
            
            request.MediaDownloader.ProgressChanged += progress =>
            {
                if (progress.Status is Google.Apis.Download.DownloadStatus.Downloading && onProgress != default)
                {
                    onProgress(progress.BytesDownloaded, resource.Size);
                }
            };
            
            var response = await request.DownloadAsync(stream, token);

            if (response.Status is Google.Apis.Download.DownloadStatus.Failed && onFailure != default)
            {
                onFailure(response.Exception);
            }

        }

        /// <summary>
        /// It downloads a resource to a specific file
        /// </summary>
        /// <param name="resource">Resource to download</param>
        /// <param name="destination">Local path where to store the resource</param>
        /// <param name="onFailure"></param>
        /// <param name="token"></param>
        /// <param name="exportType"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public async Task ExportResource(
            DriveResource resource, string destination,DriveResource.MimeType exportType = default,
            Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var file = new FileInfo(destination);

            MemoryStream stream = new MemoryStream();
            await ExportResource(resource, stream, exportType, onProgress, onFailure, token);

            if (!file.Exists && file.Directory is not null)
                Directory.CreateDirectory(file.Directory.FullName);
            
            using (var fs = new FileStream(file.FullName, FileMode.Create))
            {
                stream.WriteTo(fs);
            }
        }

        /// <summary>
        /// It downloads a resource to a specific file
        /// </summary>
        /// <param name="resource">Resource to download</param>
        /// <param name="destination">Local path where to store the resource</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task DownloadResource(DriveResource resource, string destination, Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var file = new FileInfo(destination);

            MemoryStream stream = new MemoryStream();
            await DownloadResource(resource, stream, onProgress, onFailure, token);

            if (!file.Exists && file.Directory is not null)
                Directory.CreateDirectory(file.Directory.FullName);
            
            using (var fs = new FileStream(file.FullName, FileMode.Create)) stream.WriteTo(fs);
        }

        /// <summary>
        /// It will copy a resource to another location. It does nothing for folders
        /// </summary>
        /// <param name="resource"> resource to copy</param>
        /// <param name="destination"> location where to copy the files</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<DriveResource?> CopyResource(DriveResource resource, string destination, CancellationToken token = default)
        {
            var folder = await FindFolder(destination, token: token);

            return await CopyResource(resource, folder, token);

        }

        /// <summary>
        /// It will copy a resource to another location. It does nothing for folders
        /// </summary>
        /// <param name="resource"> resource to copy</param>
        /// <param name="destination">parent folder to copy to</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<DriveResource?> CopyResource(DriveResource resource, DriveResource? destination, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            if (destination is not null && destination.Type is not DriveResource.MimeType.Folder)
                return null;
            
            var metadata = new File
            {
                Name = resource.Name,
                Parents = new List<string> { destination?.Id ?? "root" }
            };

            var request = _service.Files.Copy(metadata, resource.Id);
            request.Fields = _fields;

            return MapToDriveResource(await request.ExecuteAsync(token));

        }

        /// <summary>
        /// It will update the name or the properties of the resource
        /// </summary>
        /// <param name="resource">resource with the new data</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<DriveResource?> UpdateResource(DriveResource? resource, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            
            if (resource is null)
                return null;

            var metadata = new File()
            {
                Name = resource.Name,
                Properties = resource.Properties
            };

            var request = _service.Files.Update(metadata, resource.Id);
            request.Fields = _fields;

            return MapToDriveResource(await request.ExecuteAsync(token));
        }

        /// <summary>
        /// It updates a resource
        /// </summary>
        /// <param name="resource">Resource with the new data</param>
        /// <param name="content">New content for the resource</param>
        /// <param name="contentType">New content type</param>
        /// <param name="onProgress">Function to run on progress updates. The parameters are the downloaded bytes and total bytes</param>
        /// <param name="onFailure">Function to run on Failure</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<DriveResource?> UpdateResource(DriveResource? resource, Stream content, DriveResource.MimeType contentType,
            Action<long, long>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(_service);
            if (resource is null)
                return null;

            var metadata = new File()
            {
                Name = resource.Name,
                Properties = resource.Properties
            };

            var request = _service.Files.Update(metadata, resource.Id, content, contentType.GetString());
            request.Fields = _fields;
            request.ResponseReceived += res => resource = MapToDriveResource(res);

            request.ProgressChanged += progress =>
            {
                if (progress.Status is Google.Apis.Upload.UploadStatus.Uploading && onProgress is not null)
                {
                    onProgress(progress.BytesSent, request.ContentStream.Length);
                }
                else if (progress.Status is Google.Apis.Upload.UploadStatus.Failed && onFailure is not null)
                {
                    onFailure(progress.Exception);
                }
            };

            await request.UploadAsync(token);

            return resource;
        }

        /// <summary>
        /// Dispose service and store PathStorage
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _service?.Dispose();
                    Stop();
                }
                
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose service and store PathStorage
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
