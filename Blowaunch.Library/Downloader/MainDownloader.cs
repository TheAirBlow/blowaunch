using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog.Core;
using Spectre.Console;

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
        /// <param name="online">Is in online mode</param>
        public static void DownloadAll(BlowaunchMainJson main, bool online)
        {
            FilesManager.InitializeDirectories();
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(), 
                    new ProgressBarColumn(), 
                    new PercentageColumn(), 
                    new ElapsedTimeColumn());
            var assetsMojang = JsonConvert.DeserializeObject<MojangAssetsJson>(Fetcher.Fetch(main.Assets.Url));
            var assetsBlowaunch = BlowaunchAssetsJson.MojangToBlowaunch(assetsMojang);
            progress.Start((i) => {
                var libs = i.AddTask("Libraries");
                AnsiConsole.WriteLine("[Downloader] Downloading libraries...");
                libs.MaxValue = main.Libraries.Length;
                foreach (var lib in main.Libraries) {
                    libs.Description = $"{lib.Name} v{lib.Version} {lib.Platform}";
                    FilesManager.DownloadLibrary(lib, main.Version, online);
                    libs.Increment(1);
                }

                libs.StopTask();
                AnsiConsole.WriteLine("[Downloader] Saving assets index...");
                File.WriteAllText(Path.Combine(FilesManager.Directories.AssetsIndexes, $"{main.Assets.Id}.json"),
                    JsonConvert.SerializeObject(assetsMojang));
                var assets = i.AddTask("Assets");
                assets.MaxValue = assetsBlowaunch.Assets.Length;
                AnsiConsole.WriteLine("[Downloader] Downloading assets...");
                foreach (var asset in assetsBlowaunch.Assets) {
                    assets.Description = $"{asset.Name}";
                    FilesManager.DownloadAsset(asset, online);
                    assets.Increment(1);
                }

                assets.StopTask();
                var client = i.AddTask("Client").IsIndeterminate();
                AnsiConsole.WriteLine("[Downloader] Downloading client...");
                FilesManager.DownloadClient(main, online);
                client.StopTask();
                
                AnsiConsole.WriteLine("[Downloader] Done!");
            });
        }

        /// <summary>
        /// Downloads a version
        /// </summary>
        /// <param name="main">Blowaunch Main JSON</param>
        /// <param name="addon">Blowaunch Addon JSON</param>
        /// <param name="online">Is in online mode</param>
        public static void DownloadAll(BlowaunchMainJson main, BlowaunchAddonJson addon, bool online)
        {
            AnsiConsole.WriteLine("[Downloader] Blowaunch Addon JSON is used");
            if (main.Version != addon.BaseVersion) {
                AnsiConsole.MarkupLine($"[red]Incompatible addon and main JSON files![/]");
                AnsiConsole.MarkupLine($"[red]Addon is for {addon.BaseVersion}, not for {main.Version}.[/]");
                Environment.Exit(-1);
            }
            var newlibs = main.Libraries.ToList();
            newlibs.AddRange(addon.Libraries);
            main.Libraries = newlibs.ToArray();
            main.MainClass = addon.MainClass;
            var gamelist = main.Arguments.Game.ToList();
            gamelist.AddRange(addon.Arguments.Game);
            main.Arguments.Game = gamelist.ToArray();
            var javalist = main.Arguments.Java.ToList();
            gamelist.AddRange(addon.Arguments.Java);
            main.Arguments.Java = javalist.ToArray();
            DownloadAll(main, online);
        }
    }
}