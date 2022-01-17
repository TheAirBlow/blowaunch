using System;
using System.Text;
using Blowaunch.Library.FetcherJson;
using Newtonsoft.Json;

namespace Blowaunch.Library
{
    /// <summary>
    /// Fabric Fetcher
    /// </summary>
    public static class FabricFetcher
    {
        /// <summary>
        /// Fetch Fabric Addon JSON
        /// </summary>
        /// <param name="version">Mojang Version</param>
        /// <returns>Addon JSON</returns>
        public static BlowaunchAddonJson GetAddon(string version)
        {
            var loaders = JsonConvert.DeserializeObject<FabricLoadersJson>
            (Fetcher.Fetch(new StringBuilder().AppendFormat(Fetcher.FabricEndpoints.VersionLoaders, version)
                .ToString()));
            if (loaders == null)
                throw new Exception($"Unable to find Fabric Loader JSON for {version}!");
            return BlowaunchAddonJson.MojangToBlowaunch(JsonConvert.DeserializeObject<MojangMainJson>
                (Fetcher.Fetch(new StringBuilder().AppendFormat(Fetcher.FabricEndpoints.LoaderJson, version, loaders.Data[0].Loader).ToString())));
        }
    }
}