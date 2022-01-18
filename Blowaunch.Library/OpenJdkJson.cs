using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    public class OpenJdkJson
    {
        public class JsonVersion
        {
            [JsonProperty("directory")] public string Directory;
            [JsonProperty("windows")] public string Windows;
            [JsonProperty("linux")] public string Linux;
            [JsonProperty("macos")] public string MacOs;
        }

        [JsonProperty("versions")] public Dictionary<int, JsonVersion> Versions;
    }
}