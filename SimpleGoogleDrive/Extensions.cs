using System.Globalization;
using SimpleGoogleDrive.Models;
using File = Google.Apis.Drive.v3.Data.File;

namespace SimpleGoogleDrive
{
    public static class Extensions
    {
        public static DriveResource.MimeType MimeType(this FileInfo f) =>
            // TODO: Add all file types
            f.Extension.ToLowerInvariant() switch
            {
                ".mkv" => DriveResource.MimeType.MKV,
                ".flv" => DriveResource.MimeType.FLV,
                ".mp4" => DriveResource.MimeType.MP4,
                ".mov" => DriveResource.MimeType.MOV,
                ".avi" => DriveResource.MimeType.AVI,
                ".wmv" => DriveResource.MimeType.WMV,
                ".txt" => DriveResource.MimeType.TXT,
                ".zip" => DriveResource.MimeType.ZIP,
                ".pdf" => DriveResource.MimeType.PDF,
                _ => DriveResource.MimeType.Unknown
            };

        public static DriveResource.MimeType MimeType(this File f)
        {
            var comp = StringComparer.Create(CultureInfo.InvariantCulture, true);
            return Enum.GetValues<DriveResource.MimeType>().FirstOrDefault(t => comp.Compare(t.GetString(), f.MimeType) == 0, DriveResource.MimeType.Unknown);
        }

        public static string GetString(this DriveResource.MimeType value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            var fieldInfo = type.GetField(value.ToString());

            if (fieldInfo is null)
            {
                return "";
            }
            // Get the stringvalue attributes
            MimeTypeAttribute[]? attribs = fieldInfo.GetCustomAttributes(
                typeof(MimeTypeAttribute), false) as MimeTypeAttribute[];

            // Return the first if there was a match.
            return attribs?[0].value ?? DriveResource.MimeType.Unknown.GetString();
        }

        public static DriveResource.MimeType? GetDefaultExportType(this DriveResource.MimeType value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            var fieldInfo = type.GetField(value.ToString());

            if (fieldInfo is null)
            {
                return null;
            }
            
            MimeTypeAttribute[]? attribs = fieldInfo.GetCustomAttributes(
                typeof(MimeTypeAttribute), false) as MimeTypeAttribute[];

            // Return the first if there was a match.
            return attribs?.FirstOrDefault()?.defaultExportTo;
        }

        public static (string?, string) SplitPathFromResource(this string pathToResource)
        {
            var path = pathToResource.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            string? parentPath = null;
            var resource = path.Last();

            if (path.Length >= 2)
            {
                parentPath = path.SkipLast(1).Aggregate("", (a, b) => a += $"{b}/");
            }

            return (parentPath, resource);
        }

        public static string FormatPath(this string path)
        {
            return path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Aggregate("", (a, b) => a += $"{b}/")
                ;

        }
    }
}
