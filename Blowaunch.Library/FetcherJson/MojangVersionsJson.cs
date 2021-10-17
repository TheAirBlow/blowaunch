using Newtonsoft.Json;

namespace Blowaunch.Library.FetcherJson
{
    /// <summary>
    /// Mojang - Versions JSON
    /// </summary>
    public class MojangVersionsJson
    {
        /// <summary>
        /// Mojang Versions JSON - Latest
        /// </summary>
        public class JsonLatest
        {
            [JsonProperty("release")] public string Release;
            [JsonProperty("snapshot")] public string Snapshot;
        }

        /// <summary>
        /// Mojang Versions JSON - Version
        /// </summary>
        public class JsonVersion
        {
            [JsonProperty("type")] public BlowaunchMainJson.JsonType Type;
            [JsonProperty("url")] public string Url;
            [JsonProperty("id")] public string Id;
        }

        [JsonProperty("latest")] public JsonLatest Latest;
        [JsonProperty("versions")] public JsonVersion[] Versions;
    }
}