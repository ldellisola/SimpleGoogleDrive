namespace SimpleGoogleDrive.Exceptions
{
    /// <summary>
    /// Folders cannot be copied in Google Drive
    /// </summary>
    public class FoldersCannotBeCopiedException : Exception
    {
        /// <summary>
        /// Folders cannot be copied in Google Drive
        /// </summary>
        public FoldersCannotBeCopiedException() : base("Folders cannot be copied in Google Drive") { }
    }
}
