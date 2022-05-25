using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive.Exceptions;

public class ResourceTooBigForExportException: Exception
{
    public ResourceTooBigForExportException(DriveResource resource) :
        base(
            $"The file {resource.Name} is too big to be exported ({resource.Size/10 * 1e9} MB). Max size is 10 MB, see https://developers.google.com/drive/api/v3/reference/files/export")
    {
    }
}