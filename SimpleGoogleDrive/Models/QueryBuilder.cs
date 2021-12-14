using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGoogleDrive.Models
{
    public class QueryBuilder
    {
        private List<string> isType = new List<string>();
        private List<string> isNotType = new List<string>();
        
        private List<string> isOwner = new List<string>();
        private List<string> isName = new List<string>();

        private List<string> nameContains = new List<string>();
        private List<string> nameNotContains = new List<string>();

        private List<string> typeContains  = new List<string>();
        private List<string> typeNotContains  = new List<string>();

        private List<string> isParent = new List<string>();
        private List<string> isNotParent = new List<string>();

        private List<(string,string)> hasPropertyValue = new List<(string,string)>();
        private List<(string,string)> hasNotPropertyValue = new List<(string,string)>();

        private bool includeTrashed = false;


        public  QueryBuilder(QueryBuilder? other = default)
        {
            if (other != null)
            {
                isType = new List<string>(other.isType);
                isNotType = new List<string>(other.isNotType);   
                isOwner = new List<string>(other.isOwner);
                isName = new List<string>(other.isName);
                nameContains = new List<string>(other.nameContains);
                nameNotContains = new List<string>(other.nameNotContains);
                typeContains = new List<string>(other.typeContains);
                typeNotContains = new List<string>(other.typeNotContains);
                isParent = new List<string>(other.isParent);
                isNotParent = new List<string>(other.isNotParent);
                hasPropertyValue = new List<(string,string)>(other.hasPropertyValue);
                hasNotPropertyValue = new List<(string, string)>(other.hasNotPropertyValue);
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
            var ret = new QueryBuilder(this);
            ret.hasPropertyValue.Add((propertyName,propertyValue));
            return ret;
        }

        /// <summary>
        /// It will not include the resources with a specific key-value pair in its Public properties
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="propertyValue">Property value</param>
        public QueryBuilder HasNotPropertyValue(string propertyName, string propertyValue)
        {
            var ret = new QueryBuilder(this);
            ret.hasNotPropertyValue.Add((propertyName,propertyValue));
            return ret;
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
            var ret = new QueryBuilder(this);
            ret.isType.Add(type);
            return ret;
        }

        /// <summary>
        /// It adds a specific string that has be included in the type of the resources queried.
        /// For example 'video' will bring only video files
        /// </summary>
        /// <param name="type">part of the type</param>
        /// <returns></returns>
        public QueryBuilder TypeContains(string type)
        {
            var ret = new QueryBuilder(this);
            ret.typeContains.Add(type);
            return ret;
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
            var ret = new QueryBuilder(this);
            ret.isNotType.Add(type);
            return ret;
        }

        /// <summary>
        /// It adds a specific string that cannot be included in the type of the resources queried.
        /// For example 'video' will bring all resources except for video files
        /// </summary>
        /// <param name="type">part of the type</param>
        /// <returns></returns>
        public QueryBuilder TypeNotContains(string type)
        {
            var ret = new QueryBuilder(this);
            ret.typeNotContains.Add(type);
            return ret;
        }

        /// <summary>
        /// Sets the owner of the query result
        /// </summary>
        /// <param name="email">email of the user that owns the resources</param>
        /// <returns></returns>
        public QueryBuilder IsOwner(string email)
        {
            var ret = new QueryBuilder(this);
            ret.isOwner.Add(email);
            return ret;
        }

        /// <summary>
        /// Adds a specific name that the result of our query must have
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <returns></returns>
        public QueryBuilder IsName(string name)
        {
            var ret = new QueryBuilder(this);
            ret.isName.Add(name);
            return ret;
        }

        /// <summary>
        /// Adds a specific string that has to be in the name of our query result
        /// </summary>
        /// <param name="text">text that has to be in the name of the resources</param>
        /// <returns></returns>
        public QueryBuilder NameContains(string text)
        {
            var ret = new QueryBuilder(this);
            ret.nameContains.Add(text);
            return ret;
        }

        /// <summary>
        /// Adds a specific string that cannot be in the name of our query result
        /// </summary>
        /// <param name="text">text that cannot be in the name of the resources</param>
        /// <returns></returns>
        public QueryBuilder NameNotContains(string text)
        {            
            var ret = new QueryBuilder(this);
            ret.nameNotContains.Add(text);
            return ret;
        }

        /// <summary>
        /// Adds a specific folder as a parent for our query result
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public QueryBuilder IsParent(string parentId)
        {
            var ret = new QueryBuilder(this);
            ret.isParent.Add(parentId);
            return ret;
        }

        /// <summary>
        /// Adds a specific folder as not a parent for our query result
        /// </summary>
        /// <param name="parentId">Id of a folder</param>
        /// <returns></returns>
        public QueryBuilder IsNotParent(string parentId)
        {
            var ret = new QueryBuilder(this);
            ret.isNotParent.Add(parentId);
            return ret;
        }

        /// <summary>
        /// Includes files that are in the recycling bin
        /// </summary>
        /// <param name="trashed"></param>
        /// <returns></returns>
        public QueryBuilder IncludeTrashed(bool trashed = true)
        {
            var ret = new QueryBuilder(this);
            ret.includeTrashed = trashed;
            return ret;
        }

        /// <summary>
        /// It creates a query that follows 'parameter operator element or ...'
        /// </summary>
        /// <param name="param">Name of the paramenter. For example 'mimeType'</param>
        /// <param name="op">Operator user. For example '='</param>
        /// <param name="elements">Elements to add to que query</param>
        /// <returns>A query</returns>
        private string BuildInnerQuery(string param, string op, List<string> elements)
        {
            return elements.Aggregate("",(a, b) => a += $"or {param} {op} '{b}' ")
                           .Skip(2)
                           .Aggregate("",(a, b) => a += b);
        }

        /// <summary>
        /// It creates the query for the Google Drive API
        /// </summary>
        /// <returns>A query</returns>
        public string Build()
        {
            string query = $" trashed = {includeTrashed} ";



            if (isType.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("mimeType","=", isType) }) ";
            }

            if (isNotType.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("mimeType","!=", isNotType) }) ";
            }

            if (isOwner.Count > 0)
            {
                query += $"and ({ isOwner.Aggregate("",(a,b)=> a+= $"or '{b}' in owners ").Skip(2).Aggregate("",(a,b)=> a+=b) }) ";
            }

            if (isName.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("name","=",isName) }) ";
            }

            if (nameContains.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("name","contains",nameContains) }) ";
            }

            if (nameNotContains.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("not name","contains", nameNotContains) }) ";
            }

            if (typeContains.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("mimeType", "contains", typeContains) }) ";
            }

            if (typeNotContains.Count > 0)
            {
                query += $"and ({ BuildInnerQuery("not mimeType", "contains", typeNotContains) }) ";
            }

            if (isParent.Count > 0)
            {
                query += $"and ({ isParent.Aggregate("",(a, b) => a += $"or '{b}' in parents ").Skip(2).Aggregate("", (a, b) => a += b) }) ";
            }

            if (isNotParent.Count > 0)
            {
                query += $"and ({ isNotParent.Aggregate("",(a, b) => a += $"or not '{b}' in parents ").Skip(2).Aggregate("", (a, b) => a += b) }) ";
            }

            if (hasPropertyValue.Count > 0)
            {
                query += $"and ( {hasPropertyValue.Aggregate("", (a, b) => a += $"or properties has {{ key='{b.Item1}' and value='{b.Item2}' }}").Skip(2).Aggregate("", (a, b) => a += b)})";
            }

            if (hasNotPropertyValue.Count > 0)
            {
                query += $"and ( {hasNotPropertyValue.Aggregate("", (a, b) => a += $"or not properties has {{ key='{b.Item1}' and value='{b.Item2}' }}").Skip(2).Aggregate("", (a, b) => a += b)})";
            }

            return query;
        }
    }
}
