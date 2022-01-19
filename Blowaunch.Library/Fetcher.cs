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
            public const string Asset = "http://resources.download.minecraft.net/{0}/{1}";
        }

        public static class FabricEndpoints
        {
            public const string VersionLoaders = "https://meta.fabricmc.net/v2/versions/loader/{0}";
            public const string LoaderJson = "https://meta.fabricmc.net/v2/versions/loader/{0}/{1}/profile/json";
        }

        public static class BlowaunchEndpoints
        {
            public const string OpenJdk = "https://github.com/TheAirBlow/blowaunch/raw/main/openjdk.json";
        }

        public static class ForgeEndpoints
        {
            public const string ForgeWebsite = "https://files.minecraftforge.net/net/minecraftforge/forge/index_{0}.html";
        }
        
        public static string Fetch(string url)
        {
            using var wc = new WebClient();
            return wc.DownloadString(url);
        }

        public static void Download(string url, string path)
        {
            using var client = new WebClient();
            client.DownloadFile(url, path);
        }
    }
}