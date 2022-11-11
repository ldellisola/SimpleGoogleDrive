using System.Reflection.Emit;
using System.Text.Json;
using SimpleGoogleDrive.Models;

namespace SimpleGoogleDrive
{
    internal class PathStorage
    {
        private static PathStorage? Reference { get; set; }
        private static string _serializedStorage = @"./PathStorage.json";
        private static readonly bool SaveFile = true;

        /// <summary>
        /// Gets the instance of the Path Storage.
        /// </summary>
        /// <param name="usePersistentStorage">If true, it will store a serialized version of the data to reload later</param>
        /// <param name="persistentStoragePath">If usePersistentStorage is true, then it will store the data in this path</param>
        public static PathStorage GetInstance(bool usePersistentStorage = false, string? persistentStoragePath = default)
        {
            if (Reference != null)
                return Reference;

            if (persistentStoragePath != null)
                _serializedStorage = persistentStoragePath;

            if (usePersistentStorage && File.Exists(_serializedStorage))
            {
                Reference = new PathStorage();
                var serializedDic = File.ReadAllText(_serializedStorage);
                var dictionaries = JsonSerializer.Deserialize<Dictionary<string, string>[]>(serializedDic);

                Reference._idToPath = dictionaries?[0] ?? new Dictionary<string, string>();
                Reference._pathToId = dictionaries?[1] ?? new Dictionary<string, string>();
                
                 
                // Reference._storage = JsonSerializer.Deserialize<Dictionary<string, string?>>(serializedDic) ?? new Dictionary<string, string?>();
            }
            else
            {
                Reference = new PathStorage();
            }

            return Reference;
        }

        /// <summary>
        /// It saves the know data to a file
        /// </summary>
        public static void Store()
        {
            if (SaveFile)
            {
                var buffer = JsonSerializer.Serialize(new [] {Reference?._idToPath,Reference?._pathToId });
                File.WriteAllText(_serializedStorage, buffer);
            }
        }


        private Dictionary<string, string> _idToPath = new();
        private Dictionary<string, string> _pathToId = new();

        public string? GetId(string path)
        {
            return _pathToId.TryGetValue(path, out var id) switch
            {
                true => id,
                false => null
            };
        }

        public string? GetPath(string id)
        {
            return _idToPath.TryGetValue(id, out string? path) switch
            {
                true => path,
                false => null
            };
        }
        

        public void Add(string id, string path)
        {
            path = path.FormatPath();
            _pathToId[path] = id;
            _idToPath[id] = path;
        }

        public void DeleteId(string id)
        {
            var path = _idToPath[id];
            _idToPath.Remove(id);
            _pathToId.Remove(path);
        }

        public void DeletePath(string path)
        {
            path = path.FormatPath();
            var id = _pathToId[path];
            _pathToId.Remove(path);
            _idToPath.Remove(id);
        }


        // private Dictionary<string, string?> _parents = new();
        // private Dictionary<string, string> _names = new();
        //
        // public void Add(DriveResource? resource)
        // {
        //     if (resource is null)
        //         return;
        //     
        //     _parents.Add(resource.Id, resource.Parent);
        //     _names.Add(resource.Id,resource.Name);
        // }
        //
        // public string? GetFullPath(string id)
        // {
        //     var ids = new List<string?>();
        //     do
        //     {
        //         if (_parents.TryGetValue(id, out string? parentId))
        //             ids.Add(parentId);
        //         else
        //             break;
        //     } while (true);
        //
        //     ids.RemoveAll(t => t is null);
        //
        //     return ids.Any() ? ids.Select(t => _names[t!]).Aggregate("", (a, b) => $"{a}/{b}") : null;
        // }

        // public void DeleteRoot(string id)
        // {
        //     _parents.Remove(id);
        //     _names.Remove(id);
        // }
        
        

        // private Dictionary<string, string?> _storage = new();
        //
        // public string? this[string? id]
        // {
        //     get => _storage!.GetValueOrDefault(id?.FormatPath(), null);
        //     set { if (id != null) _storage[id] = value.FormatPath(); }
        // }
        //
        // /// <summary>
        // /// It retrieves a path from the id
        // /// </summary>
        // /// <param name="id"></param>
        // /// <returns></returns>
        // public string? Key(string? id)
        // {
        //     if (id is null || !_storage.TryGetValue(id, out var ret)) 
        //         return null;
        //     
        //     return ret;
        // }
    }
}
