namespace SimpleGoogleDrive.Exceptions
{
    public class ServiceNotAuthenticatedException : Exception
    {
        public ServiceNotAuthenticatedException() : base("The service has not been authenticated") { }
    }
}
