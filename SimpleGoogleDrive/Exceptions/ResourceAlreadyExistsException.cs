using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive.Exceptions
{
    public class ResourceAlreadyExistsException : Exception
    {

        public ResourceAlreadyExistsException(DriveResource resource) :base($"The resource ${resource.Name} already exists")
        {
        }
    }
}
