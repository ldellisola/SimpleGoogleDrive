using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive
{

    internal class MimeTypeAttribute : Attribute
    {
        public string Value;
        public DriveResource.MimeType? DefaultExportTo;

        public MimeTypeAttribute(string value)
        {
            Value = value;
        }
        
        public MimeTypeAttribute(string value,DriveResource.MimeType defaultExportTo)
        {
            Value = value;
            DefaultExportTo = defaultExportTo;
        }
    }

}
