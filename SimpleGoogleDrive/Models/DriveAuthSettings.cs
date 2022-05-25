using Google.Apis.Drive.v3;

namespace SimpleGoogleDrive.Models
{
    public class DriveAuthSettings
    {
        public enum AuthMode
        {
            Console,
            Web
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName">Name of your application</param>
        /// <param name="credentials">Credentials file from Google Cloud Console</param>
        /// <param name="userStore">Name of the data store where user authentication data will be stored</param>
        /// <param name="user">User name, tipically just "user"</param>
        /// <param name="mode">How the Authentication will be done, either in a web server or in the console</param>
        public DriveAuthSettings(string applicationName, FileInfo credentials, string userStore, string user = "user", AuthMode mode = AuthMode.Web)
        {
            User = user;
            ApplicationName = applicationName;
            Credentials = credentials;
            UserStore = userStore;
            Mode = mode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName">Name of your application</param>
        /// <param name="clientId">Client Id from Google Cloud Console</param>
        /// <param name="clientSecret">Client secret from Google Cloud Console</param>
        /// <param name="userStore">Name of the data store where user authentication data will be stored</param>
        /// <param name="user">User name, tipically just "user"</param>
        /// <param name="mode">How the Authentication will be done, either in a web server or in the console</param>
        public DriveAuthSettings(string applicationName,string clientId, string clientSecret, string userStore, string user = "user", AuthMode mode = AuthMode.Web)
        {
            User = user;
            ClientId = clientId;
            ApplicationName = applicationName;
            ClientSecret = clientSecret;
            UserStore = userStore;
            Mode = mode;
        }

        /// <summary>
        /// Client Id from Google Cloud Console
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Name of your application
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// Credentials file from Google Cloud Console
        /// </summary>
        public FileInfo? Credentials { get; set; }

        /// <summary>
        /// Client secret from Google Cloud Console
        /// </summary>
        public string? ClientSecret { get; set; }
        /// <summary>
        /// Name of the data store where user authentication data will be stored
        /// </summary>
        public string UserStore { get; set; }
        /// <summary>
        /// User name, tipically just "user"
        /// </summary>
        public string User { get; set; } = "user";

        /// <summary>
        /// Requested scope of Google APIs, usually just the Drive API
        /// </summary>
        public string[] Scope = { DriveService.Scope.Drive };
        
        /// <summary>
        /// Authentication mode, wither through a local webserver or via console
        /// </summary>
        public AuthMode Mode { get; set; }

        /// <summary>
        /// It checks if the data is valid
        /// </summary>
        /// <returns>true if the data is valid</returns>
        public bool IsValid()
        {
            return Scope.Length > 0 && (ClientSecret != null || (Credentials?.Exists ?? false));
        }

    }
}
