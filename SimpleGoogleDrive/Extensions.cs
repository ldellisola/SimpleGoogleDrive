using SimpleGoogleDrive.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGoogleDrive
{
    internal static class Extensions
    {
        public static Models.DriveResource.MimeType MimeType(this FileInfo f)
        {
            switch (f.Extension.ToLower())
            {
                case ".mkv":
                    return Models.DriveResource.MimeType.MKV;
                case ".flv":
                    return Models.DriveResource.MimeType.FLV;
                case ".mp4":
                    return Models.DriveResource.MimeType.MP4;
                case ".mov":
                    return Models.DriveResource.MimeType.MOV;
                case ".avi":
                    return Models.DriveResource.MimeType.AVI;
                case ".wmv":
                    return Models.DriveResource.MimeType.WMV;
                case ".txt":
                    return Models.DriveResource.MimeType.TXT;
                default:
                    return Models.DriveResource.MimeType.Unknown;
            }
        }

        public static DriveResource.MimeType MimeType(this Google.Apis.Drive.v3.Data.File f)
        {
            var comp = StringComparer.Create(CultureInfo.InvariantCulture, true);
            return Enum.GetValues<DriveResource.MimeType>().FirstOrDefault(t => comp.Compare(t.GetString(),f.MimeType) == 0, DriveResource.MimeType.Unknown);
        }

        public static string GetString(this DriveResource.MimeType value)
        {
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            var fieldInfo = type.GetField(value.ToString());

            if (fieldInfo == null)
            {
                return "";
            }
            // Get the stringvalue attributes
            StringValueAttribute[]? attribs = fieldInfo.GetCustomAttributes(
                typeof(StringValueAttribute), false) as StringValueAttribute[];

            // Return the first if there was a match.
            return attribs?[0].value ?? DriveResource.MimeType.Unknown.GetString();
        }

        public static (string?, string) SplitPathFromResource(this string pathToResource)
        {
            var path = pathToResource.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            string? parentPath = null;
            var resource = path.Last();

            if (path.Length >= 2)
            {
                parentPath = path.SkipLast(1).Aggregate("",(a, b) => a += $"{b}/");
            }

            return (parentPath, resource);
        }

        public static string FormatPath(this string path)
        {
            return path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Aggregate("",(a,b) => a+=$"{b}/")
                ;

        }
    }
}
