using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive.Exceptions
{
    /// <summary>
    /// The resource already exists
    /// </summary>
    public class ResourceAlreadyExistsException : Exception
    {

        /// <summary>
        /// The resource already exists
        /// </summary>
        /// <param name="resource">Resource that was intended to be created</param>
        public ResourceAlreadyExistsException(DriveResource resource) :base($"The resource ${resource.Name} already exists")
        {
        }
    }
}
