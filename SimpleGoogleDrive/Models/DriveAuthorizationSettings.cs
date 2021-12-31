using Google.Apis.Drive.v3;

namespace SimpleGoogleDrive.Models
{
    public class DriveAuthorizationSettings
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName">Name of your application</param>
        /// <param name="credentials">Credentials file from Google Cloud Console</param>
        /// <param name="userStore">Name of the data store where user authentication data will be stored</param>
        /// <param name="user">User name, tipically just "user"</param>
        public DriveAuthorizationSettings(string applicationName, FileInfo credentials, string userStore, string user = "user")
        {
            User = user;
            ApplicationName = applicationName;
            Credentials = credentials;
            UserStore = userStore;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationName">Name of your application</param>
        /// <param name="clientSecret">Client secret from Google Cloud Console</param>
        /// <param name="userStore">Name of the data store where user authentication data will be stored</param>
        /// <param name="user">User name, tipically just "user"</param>
        public DriveAuthorizationSettings(string applicationName, string clientSecret, string userStore, string user = "user")
        {
            User = user;
            ApplicationName = applicationName;
            ClientSecret = clientSecret;
            UserStore = userStore;
        }

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
        /// It checks if the data is valid
        /// </summary>
        /// <returns>true if the data is valid</returns>
        public bool IsValid()
        {
            return Scope.Length > 0 && (ClientSecret != null || (Credentials?.Exists ?? false));
        }

    }
}
