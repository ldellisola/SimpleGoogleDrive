using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive.Exceptions;

public class ResourceCannotBeExportedException : Exception
{
    public ResourceCannotBeExportedException(DriveResource resource) :base($"The resource ${resource.Name} cannot be exported")
    {
    }
}