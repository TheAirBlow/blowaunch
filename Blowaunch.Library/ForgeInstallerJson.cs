using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    public class ForgeInstallerJson
    {
        public class JsonServerClientPair
        {
            [JsonProperty("client")] public string Client;
            [JsonProperty("server")] public string Server;
        }

        public class JsonProcessor
        {
            [JsonProperty("sides")] public string[] Sides;
            [JsonProperty("jar")] public string Jar;
            [JsonProperty("classpath")] public string[] Classpath;
            [JsonProperty("args")] public string[] Arguments;
            [JsonProperty("outputs")] public Dictionary<string, string> Output;
        }

        [JsonProperty("Data")] public Dictionary<string, JsonServerClientPair> Data;
        [JsonProperty("libraries")] public MojangMainJson.JsonLibrary[] Libraries;
        [JsonProperty("processors")] public JsonProcessor[] Processors;
    }
}