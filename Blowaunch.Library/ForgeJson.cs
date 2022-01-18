using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    public class ForgeJson
    {
        [JsonProperty("versions")] public Dictionary<string, string> Versions;
    }
}