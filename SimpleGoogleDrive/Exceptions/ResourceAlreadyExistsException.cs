using SimpleGoogleDrive.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGoogleDrive.Exceptions
{
    public class ResourceAlreadyExistsException : Exception
    {
        private DriveResource resource;

        public ResourceAlreadyExistsException(DriveResource resource)
        {
            this.resource = resource;
        }
    }
}
