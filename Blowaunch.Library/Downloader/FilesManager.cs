using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Serilog.Core;

namespace Blowaunch.Library.Downloader
{
    /// <summary>
    /// Blowaunch Files Manager
    /// </summary>
    public static class FilesManager
    {
        /// <summary>
        /// Minecraft Directories
        /// </summary>
        public static class Directories
        {
            public static readonly string Root =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".minecraft");
            public static readonly string AssetsRoot =
                Path.Combine(Root, "assets");
            public static readonly string AssetsObjects =
                Path.Combine(AssetsRoot, "objects");
            public static readonly string AssetsObject =
                Path.Combine(AssetsRoot, "objects", "{0,-38}", "{0}");
            public static readonly string AssetsIndexes =
                Path.Combine(AssetsRoot, "indexes");
            public static readonly string LibrariesRoot =
                Path.Combine(Root, "libraries");
            public static readonly string VersionsRoot =
                Path.Combine(Root, "versions");
        }

        /// <summary>
        /// Initialize directories
        /// </summary>
        public static void InitializeDirectories()
        {
            Directory.CreateDirectory(Directories.Root);
            Directory.CreateDirectory(Directories.AssetsRoot);
            Directory.CreateDirectory(Directories.AssetsObjects);
            Directory.CreateDirectory(Directories.AssetsIndexes);
            Directory.CreateDirectory(Directories.LibrariesRoot);
            Directory.CreateDirectory(Directories.VersionsRoot);
        }

        /// <summary>
        /// Downloads an asset
        /// </summary>
        /// <param name="asset">Blowaunch Asset JSON</param>
        /// /// <param name="logger">Serilog Logger</param>
        public static void DownloadAsset(BlowaunchAssetsJson.JsonAsset asset, Logger logger)
        {
            string path = new StringBuilder().AppendFormat(Directories.AssetsObject, asset.ShaHash).ToString();
            Fetcher.Download(asset.Url, path);
            string hash = HashHelper.Hash(File.ReadAllBytes(path));
            if (hash != asset.ShaHash) {
                logger.Warning($"[Assets] {path} hash mismatch: {hash} and {asset.ShaHash}, redownloading...");
                DownloadAsset(asset, logger);
            }
            logger.Information($"[Assets] {path} successfully downloaded!");
        }

        /// <summary>
        /// Get library path
        /// </summary>
        /// <param name="library">Blowaunch Library JSON</param>
        /// <returns>Path</returns>
        public static string GetLibraryPath(BlowaunchMainJson.JsonLibrary library)
        {
            string path;
            if (library.Platform == "any")
                path = Path.Combine(Directories.LibrariesRoot,
                    string.Join(Path.PathSeparator, library.Package.Split('.')),
                    library.Name, library.Version, $"{library.Name}-{library.Version}.jar");
            else path = Path.Combine(Directories.LibrariesRoot, 
                string.Join(Path.PathSeparator, library.Package.Split('.')), 
                library.Name, library.Version, $"{library.Name}-{library.Version}-natives-{library.Platform}.jar");
            return path;
        }
        
        /// <summary>
        /// Downloads a library
        /// </summary>
        /// <param name="library">Blowaunch Library JSON</param>
        /// <param name="logger">Serilog Logger</param>
        [SuppressMessage("ReSharper", "ConvertIfStatementToConditionalTernaryExpression")]
        public static void DownloadLibrary(BlowaunchMainJson.JsonLibrary library, Logger logger)
        {
            string path = GetLibraryPath(library);
            Directory.CreateDirectory(path);
            Fetcher.Download(library.Url, path);
            string hash = HashHelper.Hash(File.ReadAllBytes(path));
            if (hash != library.ShaHash) {
                logger.Warning($"[Libraries] {path} hash mismatch: {hash} and {library.ShaHash}, redownloading...");
                DownloadLibrary(library, logger);
            }
            logger.Information($"[Libraries] {path} successfully downloaded!");
        }

        /// <summary>
        /// Downloads client and all required files
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="logger">Serilog Logger</param>
        public static void DownloadClient(BlowaunchMainJson main, Logger logger)
        {
            string path = Path.Combine(Directories.VersionsRoot, main.Version);
            string version = Path.Combine(path, $"{main.Version}.jar");
            string json = Path.Combine(path, $"{main.Version}.json");
            string logging = Path.Combine(path, "logging.xml");
            Directory.CreateDirectory(path);
            Fetcher.Download(main.Downloads.Client.Url, version);
            Fetcher.Download(main.Logging.Download.Url, logging);
            string hash1 = HashHelper.Hash(File.ReadAllBytes(version));
            if (hash1 != main.Downloads.Client.ShaHash) {
                logger.Warning($"[Client] {version} hash mismatch: {hash1} and {main.Downloads.Client.ShaHash}, redownloading...");
                DownloadClient(main, logger);
            }
            string hash2 = HashHelper.Hash(File.ReadAllBytes(logging));
            if (hash2 != main.Logging.Download.ShaHash) {
                logger.Warning($"[Client] {logging} hash mismatch: {hash2} and {main.Logging.Download.ShaHash}, redownloading...");
                DownloadClient(main, logger);
            }
            
            File.WriteAllText(JsonConvert.SerializeObject(main), json);
            logger.Information($"[Client] {main.Version} successfully downloaded!");
        }
    }
}