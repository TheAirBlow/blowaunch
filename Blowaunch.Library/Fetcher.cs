using System.Net;

namespace Blowaunch.Library
{
    public static class Fetcher
    {
        public static class MojangEndpoints
        {
            public const string Versions = "http://launchermeta.mojang.com/mc/game/version_manifest.json";
            public const string Library =
                "https://libraries.minecraft.net/{0}/{1}/{2}/{1}-{2}{3}.jar";
            public const string LibraryHash =
                "https://libraries.minecraft.net/{0}/{1}/{2}/{1}-{2}{3}.sha1";
            public const string Asset = "http://resources.download.minecraft.net/{0}/{1}";
        }

        public static class FabricEndpoints
        {
            public const string VersionLoaders = "https://meta.fabricmc.net/v2/versions/loader/{0}";
            public const string LoaderJson = "https://meta.fabricmc.net/v2/versions/loader/{0}/{1}/profile/json";
        }
        
        public static string Fetch(string url)
        {
            using WebClient wc = new WebClient();
            return wc.DownloadString(url);
        }

        public static void Download(string url, string path)
        {
            using var client = new WebClient();
            client.DownloadFile(url, path);
        }
    }
}