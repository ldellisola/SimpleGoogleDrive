namespace SimpleGoogleDrive.Exceptions
{
    public class ResourceDoesNotExistException : Exception
    {
        public ResourceDoesNotExistException(string? message) : base(message)
        {
        }
    }
}
