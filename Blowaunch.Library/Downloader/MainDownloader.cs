using System.IO;
using Newtonsoft.Json;
using Serilog.Core;

namespace Blowaunch.Library.Downloader
{
    /// <summary>
    /// Main downloader
    /// </summary>
    public class MainDownloader
    {
        /// <summary>
        /// Downloads a version
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="logger">Serilog Logger</param>
        public static void DownloadAll(BlowaunchMainJson main, Logger logger)
        {
            FilesManager.InitializeDirectories();
            logger.Information("[Downloader] Downloading libraries...");
            foreach (BlowaunchMainJson.JsonLibrary lib in main.Libraries)
                FilesManager.DownloadLibrary(lib, logger);
            var assetsMojang = JsonConvert.DeserializeObject<MojangAssetsJson>(Fetcher.Fetch(main.Assets.Url));
            var assetsBlowaunch = BlowaunchAssetsJson.MojangToBlowaunch(assetsMojang);
            logger.Information("[Downloader] Saving assets index...");
            File.WriteAllText(JsonConvert.SerializeObject(assetsMojang), 
                Path.Combine(FilesManager.Directories.AssetsIndexes, $"{main.Assets.Id}.json"));
            logger.Information("[Downloader] Downloading assets...");
            foreach (BlowaunchAssetsJson.JsonAsset asset in assetsBlowaunch.Assets)
                FilesManager.DownloadAsset(asset, logger);
            logger.Information("[Downloader] Downloading client...");
            FilesManager.DownloadClient(main, logger);
            logger.Information("[Downloader] Done!");
        } 
    }
}