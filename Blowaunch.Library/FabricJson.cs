using Newtonsoft.Json;

namespace Blowaunch.Library
{
    public class FabricJson
    {
        public class JsonLibrary
        {
            [JsonProperty("name")] public string Name;
            [JsonProperty("url")] public string Url;
        }

        public class JsonArguments
        {
            [JsonProperty("jvm")] public string[] Java;
            [JsonProperty("game")] public string[] Game;
        }

        [JsonProperty("arguments")] public JsonArguments Arguments;
        [JsonProperty("libraries")] public JsonLibrary[] Libraries;
        [JsonProperty("inheritsFrom")] public string BaseVersion;
        [JsonProperty("mainClass")] public string MainClass;
    }
}