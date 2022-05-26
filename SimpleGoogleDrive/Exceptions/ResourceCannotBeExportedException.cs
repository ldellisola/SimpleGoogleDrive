using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive.Exceptions;

/// <summary>
/// Only some resources like Google docs, sheets, etc can be exported.
/// See https://developers.google.com/drive/api/guides/ref-export-formats
/// </summary>
public class ResourceCannotBeExportedException : Exception
{
    /// <summary>
    /// Only some resources like Google docs, sheets, etc can be exported.
    /// See https://developers.google.com/drive/api/guides/ref-export-formats
    /// </summary>
    /// <param name="resource">Resource that was intended to be exported</param>
    public ResourceCannotBeExportedException(DriveResource resource) :base($"The resource ${resource.Name} cannot be exported")
    {
    }
}