using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using Blowaunch.Library;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using Newtonsoft.Json;
using Spectre.Console;

namespace Blowaunch.ConsoleApp
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
            progress.Start(i => {
                var libs = i.AddTask("Libraries");
                AnsiConsole.WriteLine("[Downloader] Downloading libraries...");
                libs.MaxValue = main.Libraries.Length;
                foreach (var lib in main.Libraries) {
                    libs.Description = $"Downloading {lib.Name} v{lib.Version} {lib.Platform}";
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
                
                var dir = Path.Combine(FilesManager.Directories.JavaRoot, main.JavaMajor.ToString());
                var extract = Path.Combine(FilesManager.Directories.JavaRoot);
                if (online) {
                    if (!Directory.Exists(dir)) {
                        var task = i.AddTask("OpenJDK").IsIndeterminate();
                        task.Description = "Fetching";
                        var openjdk = JsonConvert.DeserializeObject<OpenJdkJson>(Fetcher.Fetch(Fetcher.BlowaunchEndpoints.OpenJdk));
                        if (!openjdk.Versions.ContainsKey(main.JavaMajor)) {
                            AnsiConsole.MarkupLine($"[red]Unable to find OpenJDK version {main.JavaMajor}![/]");
                            AnsiConsole.MarkupLine($"[red]Please report it to us on the GitHub issues page.[/]");
                            return;
                        }

                        void ExtractTar(string path, string directory) { 
                            var dataBuffer = new byte[4096];
                            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                            using var gzipStream = new GZipInputStream(fs);
                            using var fsOut = File.OpenWrite(directory);
                            fsOut.Seek(0, SeekOrigin.Begin);
                            StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                        }
            
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                            AnsiConsole.WriteLine("[OpenJDK] Detected Windows!");
                            var link = openjdk.Versions[main.JavaMajor].Windows;
                            var path = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(link)!);
                            task.Description = "Downloading";
                            Fetcher.Download(link, Path.Combine(Path.GetTempPath(), 
                                Path.GetFileName(link)!));
                            task.Description = "Extracting";
                            ZipFile.ExtractToDirectory(path, 
                                extract, true);
                            task.Description = "Renaming";
                            Directory.Move(Path.Combine(extract, openjdk.Versions[main
                                .JavaMajor].Directory), dir);
                        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                            AnsiConsole.WriteLine("[OpenJDK] Detected Linux!");
                            var link = openjdk.Versions[main.JavaMajor].Linux;
                            var path = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(link)!);
                            task.Description = "Downloading";
                            Fetcher.Download(link, path);
                            task.Description = "Extracting";
                            ExtractTar(path, extract);
                            task.Description = "Renaming";
                            Directory.Move(Path.Combine(extract, openjdk.Versions[main
                                .JavaMajor].Directory), dir);
                        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                            AnsiConsole.WriteLine("[OpenJDK] Detected MacOS!");
                            var link = openjdk.Versions[main.JavaMajor].MacOs;
                            var path = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(link)!);
                            task.Description = "Downloading";
                            Fetcher.Download(link, Path.Combine(Path.GetTempPath(), 
                                Path.GetFileName(link)!));
                            task.Description = "Extracting";
                            ExtractTar(path, extract);
                            task.Description = "Renaming";
                            Directory.Move(Path.Combine(extract, openjdk.Versions[main
                                .JavaMajor].Directory), dir);
                        } else {
                            AnsiConsole.MarkupLine($"[red]Your OS is not supported![/]");
                            return;
                        }
                        task.StopTask();
                    } else AnsiConsole.WriteLine("[OpenJDK] Skipping, already downloaded!");
                } else AnsiConsole.WriteLine("[OpenJDK] Skipping, we are in offline mode");

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
            /* Is not required and dublicates the arguments
               It is done in the Runner when it's needed
            var gamelist = main.Arguments.Game.ToList();
            gamelist.AddRange(addon.Arguments.Game);
            main.Arguments.Game = gamelist.ToArray();
            var javalist = main.Arguments.Java.ToList();
            javalist.AddRange(addon.Arguments.Java);
            main.Arguments.Java = javalist.ToArray();
            */
            DownloadAll(main, online);
        }
    }
}