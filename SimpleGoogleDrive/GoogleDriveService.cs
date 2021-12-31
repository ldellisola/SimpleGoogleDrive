using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using SimpleGoogleDrive.Exceptions;
using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive
{
    public class GoogleDriveService : IDisposable
    {
        private readonly DriveAuthorizationSettings settings;
        private DriveService? service;
        private readonly PathStorage storage;
        private bool disposedValue;

        public GoogleDriveService(DriveAuthorizationSettings settings_, bool usePersistantStorage = false, string? persistanceStoragePath = default)
        {
            storage = PathStorage.GetInstance(usePersistantStorage, persistanceStoragePath);

            if (settings_.IsValid())
            {
                settings = settings_;
            }
            else
            {
                throw new ArgumentException("Invalid DriveAuthorizationSettings");
            }
        }

        /// <summary>
        /// It authenticates the services with Google's server
        /// </summary>
        /// <param name="token">Cancellation Token</param>
        public async Task Authenticate(CancellationToken token = default)
        {
            var clientSecrets = new ClientSecrets();

            if (settings.Credentials != null)
                clientSecrets = (await GoogleClientSecrets.FromFileAsync(settings.Credentials?.FullName, token)).Secrets;
            else
                clientSecrets.ClientSecret = settings.ClientSecret;

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                settings.Scope,
                settings.User,
                token,
                new FileDataStore(settings.UserStore)
            );

            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = settings.ApplicationName
            });


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
        /// <param name="folderPath">It's the folder path to be created. For example "folder/newFolder/otherFolder"</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>It returns the created resource</returns>
        /// <exception cref="ServiceNotAuthenticatedException">It is thrown when the service is used but not authenticated</exception>
        /// <exception cref="ResourceAlreadyExistsException">It is thrown if the folder already exists</exception>
        public async Task<DriveResource?> CreateFolder(string pathToFolder, CancellationToken token = default)
        {
            var resource = await FindFolder(pathToFolder, token: token);

            if (resource != null)
                throw new ResourceAlreadyExistsException(resource);

            var (path, folder) = pathToFolder.SplitPathFromResource();
            string? parentId = null;

            if (path != null)
            {
                parentId = (await FindFolder(path, token: token))?.Id ?? (await CreateFolder(path, token))?.Id;
            }

            var metadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folder,
                MimeType = DriveResource.MimeType.Folder.GetString(),
                Parents = parentId != null ? new List<string?>() { parentId } : default
            };

            var request = (service?.Files.Create(metadata) ?? throw new ServiceNotAuthenticatedException());
            request.Fields = "*";
            var response = await request.ExecuteAsync(token);

            storage[pathToFolder] = response.Id;

            return MapToDriveResource(response);

        }

        /// <summary>
        /// It gets the full name, including the path and extension of a given resource
        /// </summary>
        /// <param name="fileId">Id of the Google Drive resource</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>It returns the full name of the resource</returns>
        private async Task<string> GetResourceFullName(string fileId, CancellationToken token = default)
        {
            var resource = await GetResource(fileId, token);
            return await GetResourceFullName(resource);
        }

        /// <summary>
        /// It gets the full name, including the path and extension of a given resource
        /// </summary>
        /// <param name="fileId">Google Drive resource</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>It returns the full name of the resource</returns>
        public async Task<string> GetResourceFullName(DriveResource resource, CancellationToken token = default)
        {
            if (storage.Key(resource.Id) != null)
            {
                return storage.Key(resource.Id);
            }

            var notInRoot = (await QueryResources(new QueryBuilder().IsNotParent("root").And().IsParent(resource.Parent).And().IsName(resource.Name))).Count() != 0;

            if (resource.Parent != null && notInRoot)
            {
                return (storage.Key(resource.Parent) ?? await GetResourceFullName(resource.Parent, token)) + "/" + resource.Name;
            }
            else
            {
                return resource.Name;
            }
        }

        /// <summary>
        /// It retrieves a resource from Google Drive
        /// </summary>
        /// <param name="id">Id of the file</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The desired resource</returns>
        /// <exception cref="ServiceNotAuthenticatedException">Throws if the service was not authenticated</exception>
        /// <exception cref="ResourceDoesNotExistException">Throws if the id does not match with any resource</exception>
        public async Task<DriveResource> GetResource(string id, CancellationToken token = default)
        {
            var request = service?.Files.Get(id) ?? throw new ServiceNotAuthenticatedException();
            request.Fields = "*";

            var resource = await request.ExecuteAsync(token) ?? throw new ResourceDoesNotExistException(id);

            return MapToDriveResource(resource);
        }

        /// <summary>
        /// It maps a GoogleDrive File to a DriveResource
        /// </summary>
        /// <param name="resource">Google Drive resource</param>
        /// <returns>A DriveResource object</returns>
        private DriveResource MapToDriveResource(Google.Apis.Drive.v3.Data.File resource)
        {
            return new DriveResource(this)
            {
                Id = resource.Id,
                Name = resource.Name,
                Parent = resource.Parents?.FirstOrDefault(),
                Type = resource.MimeType(),
                Properties = resource.Properties?.ToDictionary(t => t.Key, t => t.Value) ?? new Dictionary<string, string>(),
                Size = resource.Size,
                IsTrashed = resource.Trashed ?? false,
            };
        }

        /// <summary>
        /// It queries Google Drive 
        /// </summary>
        /// <param name="queryBuilder">Query</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>A list of Drive Resources</returns>
        /// <exception cref="ServiceNotAuthenticatedException">Throws if the service was not authenticated</exception>
        public async Task<IEnumerable<DriveResource>> QueryResources(QueryBuilder queryBuilder, CancellationToken token = default)
        {
            var resources = new List<Google.Apis.Drive.v3.Data.File>();
            string? pageToken = null;

            do
            {
                var request = service?.Files.List() ?? throw new ServiceNotAuthenticatedException();
                request.Q = queryBuilder.Build();
                request.PageToken = pageToken;
                request.Fields = "*";
                request.PageSize = 1000;

                var result = await request.ExecuteAsync(token);
                pageToken = result.NextPageToken;
                resources.AddRange(result.Files);

            } while (pageToken != null);

            return resources.ConvertAll(t => MapToDriveResource(t));
        }

        /// <summary>
        /// It finds a folder within Google Drive
        /// </summary>
        /// <param name="pathToFolder">Path on Google Drive to the folder</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>The resource, if the folder exists</returns>
        public async Task<DriveResource?> FindFolder(string? pathToFolder, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            if (storage[pathToFolder] != null)
            {
                var folder = await GetResource(storage[pathToFolder], token);

                if (folder != null && !folder.IsTrashed)
                    return folder;
            }

            var query = new QueryBuilder().IsType(DriveResource.MimeType.Folder).And(parameters);

            var resource = await FindResource(pathToFolder, query, token);

            if (resource != null && !resource.IsTrashed)
                storage[pathToFolder] = resource.Id;

            return resource;
        }

        /// <summary>
        /// It finds a File within Google Drive
        /// </summary>
        /// <param name="pathToFile">Path in GoogleDrive to the file</param>
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
        /// <param name="token">Cancellation token</param>
        /// <returns>The requested Resource</returns>
        /// <exception cref="ServiceNotAuthenticatedException">Throws if the service was not authenticated</exception>
        public async Task<DriveResource?> FindResource(string? pathToResource, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            if (pathToResource == null)
                return default;

            var (parentFolder, resource) = pathToResource.SplitPathFromResource();

            var parentFolderId = "root";

            if (parentFolder != null)
            {
                parentFolderId = (await FindFolder(parentFolder, null, token))?.Id;

                if (parentFolderId == null)
                    throw new ResourceDoesNotExistException(parentFolderId);
            }


            var query = new QueryBuilder().IsName(resource).And().IsParent(parentFolderId).And(parameters);


            var result = await QueryResources(query, token);

            return result.FirstOrDefault();
        }

        /// <summary>
        /// It deletes a resource from Google Drive
        /// </summary>
        /// <param name="resource">Resource to be deleted</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ServiceNotAuthenticatedException">It will be thrown if the service wasn't authenticated</exception>
        public async Task DeleteResource(DriveResource resource, CancellationToken token = default)
        {
            await (service?.Files.Delete(resource.Id).ExecuteAsync(token) ?? throw new ServiceNotAuthenticatedException());
            if (resource.Type == DriveResource.MimeType.Folder)
            {
                storage[await resource.GetFullName()] = null;
            }
        }

        /// <summary>
        /// It deletes a resource from Google Drive
        /// </summary>
        /// <param name="pathToResource">Path on Google Drive to the resource</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public async Task DeleteResource(string pathToResource, QueryBuilder? parameters = default, CancellationToken token = default)
        {
            var (path, resourceName) = pathToResource.SplitPathFromResource();

            var query = new QueryBuilder().IsName(resourceName).And(parameters);

            if (path != null)
            {
                var folderResource = await FindFolder(path, null, token);
                if (folderResource != null)
                    query.IsParent(folderResource.Id);

            }

            var resource = (await QueryResources(query, token)).FirstOrDefault();

            if (resource != null)
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
        /// <returns>Returns the Drive resource if the upload was successul. Null otherwise</returns>
        /// <exception cref="ServiceNotAuthenticatedException"></exception>
        public async Task<DriveResource?> CreateFile(FileStream data, string mimeType, string destination, Dictionary<string, string>? properties = default,
            Action<long, long>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var (destinationPath, destinationName) = destination.SplitPathFromResource();

            string? parentId = null;

            if (destinationPath != null)
                parentId = ((await FindFolder(destinationPath, token: token)) ?? (await CreateFolder(destinationPath, token)))?.Id;

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = destinationName,
                Parents = parentId != null ? new List<string>() { parentId } : default,
                Properties = properties
            };


            var request = service?.Files.Create(fileMetadata, data, mimeType) ?? throw new ServiceNotAuthenticatedException();

            DriveResource? resource = null;
            request.Fields = "*";

            request.ResponseReceived += rest => resource = MapToDriveResource(rest);

            request.ProgressChanged += prog =>
            {
                if (prog.Status == Google.Apis.Upload.UploadStatus.Uploading && onProgress != null)
                {
                    onProgress(prog.BytesSent, request.ContentStream.Length);
                }
                else if (prog.Status == Google.Apis.Upload.UploadStatus.Failed && onFailure != null)
                {
                    onFailure(prog.Exception);
                }
            };

            var result = await request.UploadAsync(token);

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
        /// <exception cref="ServiceNotAuthenticatedException"></exception>
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
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ServiceNotAuthenticatedException"></exception>
        public async Task DownloadResource(DriveResource resource, Stream stream, Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var request = (service?.Files.Get(resource.Id) ?? throw new ServiceNotAuthenticatedException());


            request.MediaDownloader.ProgressChanged += prog =>
            {
                if (prog.Status == Google.Apis.Download.DownloadStatus.Downloading && onProgress != default)
                {
                    onProgress(prog.BytesDownloaded, resource.Size);
                }
            };

            var response = await request.DownloadAsync(stream, token);

            if (response.Status == Google.Apis.Download.DownloadStatus.Failed && onFailure != default)
            {
                onFailure(response.Exception);
            }

        }

        /// <summary>
        /// It downloads a resource to a specific file
        /// </summary>
        /// <param name="resource">Resource to download</param>
        /// <param name="destination">Local path where to store the resource</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task DownloadResource(DriveResource resource, string destination, Action<long, long?>? onProgress = default,
            Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            var file = new FileInfo(destination);

            MemoryStream stream = new MemoryStream();
            await DownloadResource(resource, stream, onProgress, onFailure, token);

            if (!file.Exists)
                Directory.CreateDirectory(file.Directory.FullName);



            using (var fs = new FileStream(file.FullName, FileMode.Create))
            {
                stream.WriteTo(fs);
            }
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
        /// <exception cref="ServiceNotAuthenticatedException"></exception>
        public async Task<DriveResource?> CopyResource(DriveResource resource, DriveResource? destination, CancellationToken token = default)
        {

            if (destination != null && destination.Type != DriveResource.MimeType.Folder)
                return null;


            var metadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = resource?.Name,
                Parents = new List<string> { destination?.Id ?? "root" }
            };

            var request = service?.Files.Copy(metadata, resource.Id) ?? throw new ServiceNotAuthenticatedException();
            request.Fields = "*";

            return MapToDriveResource(await request.ExecuteAsync(token));

        }

        /// <summary>
        /// It will update the name or the properties of the resource
        /// </summary>
        /// <param name="resource">resource with the new data</param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ServiceNotAuthenticatedException"></exception>
        public async Task<DriveResource?> UpdateResource(DriveResource? resource, CancellationToken token = default)
        {
            if (resource == null)
                return null;

            var metadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = resource.Name,
                Properties = resource.Properties
            };

            var request = service?.Files.Update(metadata, resource.Id) ?? throw new ServiceNotAuthenticatedException();
            request.Fields = "*";

            return MapToDriveResource(await request.ExecuteAsync(token));
        }

        /// <summary>
        /// It will update the name or the properties of the resource
        /// </summary>
        /// <param name="resource">resource with the new data</param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ServiceNotAuthenticatedException"></exception>
        public async Task<DriveResource?> UpdateResource(DriveResource? resource, Stream content, string contentType,
            Action<long, long>? onProgress = default, Action<Exception>? onFailure = default, CancellationToken token = default)
        {
            if (resource == null)
                return null;

            var metadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = resource.Name,
                Properties = resource.Properties
            };

            var request = service?.Files.Update(metadata, resource.Id, content, contentType) ?? throw new ServiceNotAuthenticatedException();
            request.Fields = "*";
            request.ResponseReceived += res => resource = MapToDriveResource(res);

            request.ProgressChanged += prog =>
            {
                if (prog.Status == Google.Apis.Upload.UploadStatus.Uploading && onProgress != null)
                {
                    onProgress(prog.BytesSent, request.ContentStream.Length);
                }
                else if (prog.Status == Google.Apis.Upload.UploadStatus.Failed && onFailure != null)
                {
                    onFailure(prog.Exception);
                }
            };

            await request.UploadAsync(token);

            return resource;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    service?.Dispose();
                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
