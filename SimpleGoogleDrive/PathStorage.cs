using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleGoogleDrive
{
    internal class PathStorage
    {
        private static PathStorage? Reference { get; set; } = null;
        private static string SerializedStorage = @"./PathStorage.json";
        private static bool SaveFile = true; 

        /// <summary>
        /// Gets the instance of the Path Storage.
        /// </summary>
        /// <param name="usePersistantStorage">If true, it will store a serialized version of the data to reload later</param>
        /// <param name="persistanceStoragePath">If usePersistantStorage is true, then it will store the data in this path</param>
        public static PathStorage GetInstance( bool usePersistantStorage = false, string? persistanceStoragePath = default)
        {
            if (Reference != null)
                return Reference;

            if (persistanceStoragePath != null)
                PathStorage.SerializedStorage = persistanceStoragePath;

            if (usePersistantStorage && File.Exists(SerializedStorage))
            {
                Reference = new PathStorage();
                Reference.storage = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(SerializedStorage));
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
                var buffer = JsonSerializer.Serialize(Reference?.storage);
                File.WriteAllText(SerializedStorage, buffer);
            }
        }


        private Dictionary<string, string?> storage = new Dictionary<string, string?>();

        public string? this[string? key]
        {
            get => storage!.GetValueOrDefault(key?.FormatPath(), null); 
            set { if (key != null) storage[key.FormatPath()] = value; }
        }

        /// <summary>
        /// It retrieves a path from the id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string? Key(string id)
        {
            if (id == null) return null;

            storage.TryGetValue(id, out string? ret);
            return ret;
        }
    }
}
