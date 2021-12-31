
using System.Text;

namespace SimpleGoogleDrive.Models
{
    public class QueryBuilder
    {
        private bool includeTrashed = false;
        private readonly StringBuilder content = new StringBuilder();

        public QueryBuilder(QueryBuilder? other = default)
        {
            if (other != null)
            {
                content = new StringBuilder(other.content.ToString());
                includeTrashed = other.includeTrashed;
            }
        }

        /// <summary>
        /// It will include only the resources with a specific key-value pair in its Public properties
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public QueryBuilder HasPropertyValue(string propertyName, string propertyValue)
        {
            content.Append($" properties has {{ key='{propertyName}' and value='{propertyValue}' }}");
            return this;

        }

        /// <summary>
        /// It will not include the resources with a specific key-value pair in its Public properties
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public QueryBuilder HasNotPropertyValue(string propertyName, string propertyValue)
        {
            content.Append(" not ");
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
            content.Append($" mimeType = '{type}' ");
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
            content.Append($" mimeType contains '{type}' ");
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
            content.Append($" mimeType != '{type}' ");
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
            content.Append($" not ");
            return TypeContains(type);
        }

        /// <summary>
        /// Sets the owner of the query result
        /// </summary>
        /// <param name="email">email of the user that owns the resources</param>
        /// <returns></returns>
        public QueryBuilder IsOwner(string email)
        {
            content.Append($" '{email}' in owners ");
            return this;
        }

        /// <summary>
        /// Adds a specific name that the result of our query must have
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <returns></returns>
        public QueryBuilder IsName(string name)
        {
            content.Append($" name = '{name}' ");
            return this;
        }





        /// <summary>
        /// Adds a specific string that has to be in the name of our query result
        /// </summary>
        /// <param name="text">text that has to be in the name of the resources</param>
        /// <returns></returns>
        public QueryBuilder NameContains(string text)
        {
            content.Append($" name contains '{text}' ");
            return this;
        }

        /// <summary>
        /// Adds a specific string that cannot be in the name of our query result
        /// </summary>
        /// <param name="text">text that cannot be in the name of the resources</param>
        /// <returns></returns>
        public QueryBuilder NameNotContains(string text)
        {
            content.Append(" not ");
            return NameContains(text);
        }

        /// <summary>
        /// Adds a specific folder as a parent for our query result
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public QueryBuilder IsParent(string parentId)
        {
            content.Append($" '{parentId}' in parents ");
            return this;
        }

        /// <summary>
        /// Adds a specific folder as not a parent for our query result
        /// </summary>
        /// <param name="parentId">Id of a folder</param>
        /// <returns></returns>
        public QueryBuilder IsNotParent(string parentId)
        {
            content.Append(" not ");
            return IsParent(parentId);
        }

        /// <summary>
        /// Includes files that are in the recycling bin
        /// </summary>
        /// <param name="trashed"></param>
        /// <returns></returns>
        public QueryBuilder IncludeTrashed(bool trashed = true)
        {
            includeTrashed = trashed;
            return this;
        }

        /// <summary>
        /// It creates the query " ... AND ... "
        /// </summary>
        public QueryBuilder And()
        {
            content.Append(" and ");
            return this;
        }

        /// <summary>
        /// It creates the query: " (a) AND (b) "
        /// </summary>
        /// <param name="a">Left side of the query</param>
        /// <param name="b">Right side of the query</param>
        public QueryBuilder And(QueryBuilder a, QueryBuilder b)
        {
            content.Append(" ( ");
            content.Append(a.content);
            content.Append(" ) ");
            And();
            content.Append(" ( ");
            content.Append(b.content);
            content.Append(" ) ");

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
            content.Append(" ( ");
            content.Append(right.content);
            content.Append(" ) ");

            return this;
        }

        /// <summary>
        /// It creates the query " ... OR ... "
        /// </summary>
        public QueryBuilder Or()
        {
            content.Append(" or ");
            return this;
        }

        /// <summary>
        /// It creates the query: " (a) OR (b) "
        /// </summary>
        /// <param name="a">Left side of the query</param>
        /// <param name="b">Right side of the query</param>
        public QueryBuilder Or(QueryBuilder a, QueryBuilder b)
        {
            content.Append(" ( ");
            content.Append(a.content);
            content.Append(" ) ");
            Or();
            content.Append(" ( ");
            content.Append(b.content);
            content.Append(" ) ");

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
            content.Append(" ( ");
            content.Append(right.content);
            content.Append(" ) ");

            return this;
        }



        /// <summary>
        /// It creates the query for the Google Drive API
        /// </summary>
        /// <returns>A query</returns>
        public string Build()
        {
            if (content.Length > 0)
            {
                And();
            }
            content.Append($" trashed = {includeTrashed} ");
            return content.ToString();
        }
    }
}
