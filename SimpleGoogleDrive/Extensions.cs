using System.Globalization;
using System.Reflection;
using SimpleGoogleDrive.Models;
using File = Google.Apis.Drive.v3.Data.File;

namespace SimpleGoogleDrive
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// It gets the expected Google Mime Type for a GoogleDriveResource
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static DriveResource.MimeType MimeType(this FileInfo file) =>
            // TODO: Add all GoogleDriveResource types
            file.Extension.ToLowerInvariant() switch
            {
                ".mkv" => DriveResource.MimeType.Mkv,
                ".flv" => DriveResource.MimeType.Flv,
                ".mp4" => DriveResource.MimeType.Mp4,
                ".mov" => DriveResource.MimeType.Mov,
                ".avi" => DriveResource.MimeType.Avi,
                ".wmv" => DriveResource.MimeType.Wmv,
                ".txt" => DriveResource.MimeType.Txt,
                ".zip" => DriveResource.MimeType.Zip,
                ".pdf" => DriveResource.MimeType.Pdf,
                _ => DriveResource.MimeType.Unknown
            };

        /// <summary>
        /// It gets the Mimetype of a Google Drive Resource
        /// </summary>
        /// <param name="googleDriveResource">Google Drive Resource</param>
        /// <returns></returns>
        public static DriveResource.MimeType MimeType(this File googleDriveResource)
        {
            var comp = StringComparer.Create(CultureInfo.InvariantCulture, true);
            return Enum.GetValues<DriveResource.MimeType>()
                .FirstOrDefault(t => comp.Compare(t.GetString(), googleDriveResource.MimeType) == 0, DriveResource.MimeType.Unknown);
        }

        public static DriveResource.MimeType MimeType(this string mimeType)
        {
            var comp = StringComparer.Create(CultureInfo.InvariantCulture, true);
            return Enum.GetValues<DriveResource.MimeType>()
                .FirstOrDefault(t => comp.Compare(t.GetString(), mimeType) == 0, DriveResource.MimeType.Unknown);

        }

        /// <summary>
        /// It Gets the string form of a Mime type
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static string GetString(this DriveResource.MimeType mimeType)
        {
            // Get the type
            Type type = mimeType.GetType();

            // Get fieldinfo for this type
            var fieldInfo = type.GetField(mimeType.ToString());

            if (fieldInfo is null)
                return DriveResource.MimeType.Unknown.GetString();
            
            // Return the first if there was a match.
            return fieldInfo.GetCustomAttributes<MimeTypeAttribute>(false).FirstOrDefault()?.Value ?? DriveResource.MimeType.Unknown.GetString();
        }

        /// <summary>
        /// It gets the default mime type to export a file.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static DriveResource.MimeType? GetDefaultExportType(this DriveResource.MimeType mimeType)
        {
            // Get the type
            Type type = mimeType.GetType();

            // Get fieldinfo for this type
            var fieldInfo = type.GetField(mimeType.ToString());

            if (fieldInfo is null)
                return null;

            // Return the first if there was a match.
            return fieldInfo.GetCustomAttributes<MimeTypeAttribute>(false).FirstOrDefault()?.DefaultExportTo;
        }

        /// <summary>
        /// It splits a full path into a path and a resource
        /// </summary>
        /// <param name="pathToResource"></param>
        /// <returns></returns>
        public static (string?, string) SplitPathFromResource(this string pathToResource)
        {
            var path = pathToResource.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            string? parentPath = null;
            var resource = path.Last();

            if (path.Length >= 2)
            {
                parentPath = path.SkipLast(1).Aggregate("", (a, b) => a + $"{b}/");
            }

            return (parentPath, resource);
        }

        /// <summary>
        /// It removes any platform dependent paths and converts it to one Google Drive can understand
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string FormatPath(this string path)
        {
            return path
                .Trim()
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Aggregate("", (a, b) => a + $"{b}/");

        }
    }
}
