using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGoogleDrive.Exceptions
{
    public class ResourceDoesNotExistException : Exception
    {
        public ResourceDoesNotExistException(string? message) : base(message)
        {
        }
    }
}
