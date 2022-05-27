
using System.Text;
using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive
{
    /// <summary>
    /// Used to build Google Drive queries
    /// </summary>
    public class QueryBuilder
    {
        private bool _includeTrashed;
        private readonly StringBuilder _content = new();

        /// <summary>
        /// Used to build Google Drive queries
        /// </summary>
        /// <param name="other">Other query used as base for this one</param>
        public QueryBuilder(QueryBuilder? other = default)
        {
            if (other != null)
            {
                _content = new StringBuilder(other._content.ToString());
                _includeTrashed = other._includeTrashed;
            }
        }

        /// <summary>
        /// It will include only the resources with a specific key-value pair in its Public properties
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public QueryBuilder HasPropertyValue(string propertyName, string propertyValue)
        {
            _content.Append($" properties has {{ key='{propertyName.Replace("'","\'")}' and value='{propertyValue.Replace("'","\'")}' }}");
            return this;

        }

        /// <summary>
        /// It will not include the resources with a specific key-value pair in its Public properties
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public QueryBuilder HasNotPropertyValue(string propertyName, string propertyValue)
        {
            _content.Append(" not ");
            return HasPropertyValue(propertyName, propertyValue);
        }

        /// <summary>
        /// It will include only files of a specific type in the query results
        /// </summary>
        /// <param name="type">mime type of the file</param>
        public QueryBuilder IsType(DriveResource.MimeType type)
        {
            return IsType(type.GetString());
        }

        /// <summary>
        /// It will include only files of a specific type in the query results
        /// </summary>
        /// <param name="type">mime type of the file</param>
        /// <returns></returns>
        public QueryBuilder IsType(string type)
        {
            _content.Append($" mimeType = '{type}' ");
            return this;
        }

        /// <summary>
        /// It adds a specific string that has be included in the type of the resources queried.
        /// For example 'video' will bring only video files
        /// </summary>
        /// <param name="type">part of the type</param>
        /// <returns></returns>
        public QueryBuilder TypeContains(string type)
        {
            _content.Append($" mimeType contains '{type}' ");
            return this;
        }

        /// <summary>
        /// It will not include files of a specific type in the query results
        /// </summary>
        /// <param name="type">type of the file</param>
        /// <returns></returns>
        public QueryBuilder IsNotType(DriveResource.MimeType type)
        {
            return IsNotType(type.GetString());
        }

        /// <summary>
        /// It will not include files of a specific type in the query results
        /// </summary>
        /// <param name="type">mime type of the file</param>
        /// <returns></returns>
        public QueryBuilder IsNotType(string type)
        {
            _content.Append($" mimeType != '{type}' ");
            return this;
        }

        /// <summary>
        /// It adds a specific string that cannot be included in the type of the resources queried.
        /// For example 'video' will bring all resources except for video files
        /// </summary>
        /// <param name="type">part of the type</param>
        /// <returns></returns>
        public QueryBuilder TypeNotContains(string type)
        {
            _content.Append($" not ");
            return TypeContains(type);
        }

        /// <summary>
        /// Sets the owner of the query result
        /// </summary>
        /// <param name="email">email of the user that owns the resources</param>
        /// <returns></returns>
        public QueryBuilder IsOwner(string email)
        {
            _content.Append($" '{email}' in owners ");
            return this;
        }

        /// <summary>
        /// Adds a specific name that the result of our query must have
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <returns></returns>
        public QueryBuilder IsName(string name)
        {
            _content.Append($" name = '{name.Replace("'",@"\'")}' ");
            return this;
        }
        
        
        /// <summary>
        /// Adds a specific string that has to be in the name of our query result
        /// </summary>
        /// <param name="text">text that has to be in the name of the resources</param>
        /// <returns></returns>
        public QueryBuilder NameContains(string text)
        {
            _content.Append($" name contains '{text.Replace("'","\'")}' ");
            return this;
        }

        /// <summary>
        /// Adds a specific string that cannot be in the name of our query result
        /// </summary>
        /// <param name="text">text that cannot be in the name of the resources</param>
        /// <returns></returns>
        public QueryBuilder NameNotContains(string text)
        {
            _content.Append(" not ");
            return NameContains(text);
        }

        /// <summary>
        /// Adds a specific folder as a parent for our query result
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public QueryBuilder IsParent(string parentId)
        {
            _content.Append($" '{parentId}' in parents ");
            return this;
        }

        /// <summary>
        /// Adds a specific folder as not a parent for our query result
        /// </summary>
        /// <param name="parentId">Id of a folder</param>
        /// <returns></returns>
        public QueryBuilder IsNotParent(string parentId)
        {
            _content.Append(" not ");
            return IsParent(parentId);
        }

        /// <summary>
        /// Includes files that are in the recycling bin
        /// </summary>
        /// <param name="trashed"></param>
        /// <returns></returns>
        public QueryBuilder IncludeTrashed(bool trashed = true)
        {
            _includeTrashed = trashed;
            return this;
        }

        /// <summary>
        /// It creates the query " ... AND ... "
        /// </summary>
        public QueryBuilder And()
        {
            _content.Append(" and ");
            return this;
        }

        /// <summary>
        /// It creates the query: " (a) AND (b) "
        /// </summary>
        /// <param name="a">Left side of the query</param>
        /// <param name="b">Right side of the query</param>
        public QueryBuilder And(QueryBuilder a, QueryBuilder b)
        {
            _content.Append(" ( ");
            _content.Append(a._content);
            _content.Append(" ) ");
            And();
            _content.Append(" ( ");
            _content.Append(b._content);
            _content.Append(" ) ");

            return this;
        }

        /// <summary>
        /// It creates the query: " ... AND (right) "
        /// </summary>
        /// <param name="right">Right side of the query</param>
        public QueryBuilder And(QueryBuilder? right)
        {
            if (right == null)
                return this;

            And();
            _content.Append(" ( ");
            _content.Append(right._content);
            _content.Append(" ) ");

            return this;
        }

        /// <summary>
        /// It creates the query " ... OR ... "
        /// </summary>
        public QueryBuilder Or()
        {
            _content.Append(" or ");
            return this;
        }

        /// <summary>
        /// It creates the query: " (a) OR (b) "
        /// </summary>
        /// <param name="a">Left side of the query</param>
        /// <param name="b">Right side of the query</param>
        public QueryBuilder Or(QueryBuilder a, QueryBuilder b)
        {
            _content.Append(" ( ");
            _content.Append(a._content);
            _content.Append(" ) ");
            Or();
            _content.Append(" ( ");
            _content.Append(b._content);
            _content.Append(" ) ");

            return this;
        }
        /// <summary>
        /// It creates the query: " ... OR (right) "
        /// </summary>
        /// <param name="right">Right side of the query</param>
        public QueryBuilder Or(QueryBuilder? right)
        {
            if (right == null)
                return this;
            Or();
            _content.Append(" ( ");
            _content.Append(right._content);
            _content.Append(" ) ");

            return this;
        }



        /// <summary>
        /// It creates the query for the Google Drive API
        /// </summary>
        /// <returns>A query</returns>
        public string Build()
        {
            if (_content.Length > 0)
            {
                And();
            }
            _content.Append($" trashed = {_includeTrashed} ");
            return _content.ToString();
        }
    }
}
