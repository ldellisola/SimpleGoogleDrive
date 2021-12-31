namespace SimpleGoogleDrive.Exceptions
{
    public class FolderCannotBeCopiedException : Exception
    {
        public FolderCannotBeCopiedException() : base("Folders cannot be copied in Google Drive") { }
    }
}
