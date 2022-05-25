using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive
{

    internal class MimeTypeAttribute : Attribute
    {
        public string value;
        public DriveResource.MimeType? defaultExportTo;

        public MimeTypeAttribute(string value)
        {
            this.value = value;
        }
        
        public MimeTypeAttribute(string value,DriveResource.MimeType defaultExportTo)
        {
            this.value = value;
            this.defaultExportTo = defaultExportTo;
        }
    }

}
