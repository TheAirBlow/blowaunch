using Newtonsoft.Json;

namespace Blowaunch.Library.FetcherJson
{
    /// <summary>
    /// Fabric - Loaders JSON
    /// </summary>
    public class FabricLoadersJson
    {
        /// <summary>
        /// Fabric Loaders JSON - Loader
        /// </summary>
        public class JsonFabricLoader
        {
            [JsonProperty("version")] public string Version;
        }
        
        /// <summary>
        /// Fabric Loaders JSON - Data
        /// </summary>
        public class JsonFabricData
        {
            [JsonProperty("loader")] public JsonFabricLoader Loader;
        }

        public JsonFabricData[] Data;
    }
}