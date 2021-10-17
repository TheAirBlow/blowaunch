using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    /// <summary>
    /// Mojang - Assets JSON
    /// </summary>
    public class MojangAssetsJson
    {
        /// <summary>
        /// Mojang Assets JSON - Asset
        /// </summary>
        public class JsonAsset
        {
            [JsonProperty("hash")] public string ShaHash;
            [JsonProperty("size")] public int Size;
        }
        
        [JsonProperty("objects")] public Dictionary<string, JsonAsset> Assets;
    }
}