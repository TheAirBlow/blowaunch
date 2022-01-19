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
                var task = i.AddTask("Libraries");
                task.MaxValue = main.Libraries.Length;
                foreach (var lib in main.Libraries) {
                    task.Description = $"Downloading library {lib.Name} v{lib.Version} {lib.Platform}";
                    FilesManager.DownloadLibrary(lib, main.Version, online);
                    task.Increment(1);
                }
                
                task.IsIndeterminate = true;
                task.Description = $"Saving asset index";
                File.WriteAllText(Path.Combine(FilesManager.Directories.AssetsIndexes, $"{main.Assets.Id}.json"),
                    JsonConvert.SerializeObject(assetsMojang));
                task.IsIndeterminate = false;
                task.MaxValue = assetsBlowaunch.Assets.Length;
                foreach (var asset in assetsBlowaunch.Assets) {
                    task.Description = $"Downloading asset {Path.GetFileName(asset.Name)}";
                    FilesManager.DownloadAsset(asset, online);
                    task.Increment(1);
                }
                
                task.IsIndeterminate = true;
                task.Description = $"Downloading client";
                FilesManager.DownloadClient(main, online);

                var dir = Path.Combine(FilesManager.Directories.JavaRoot, main.JavaMajor.ToString());
                var extract = Path.Combine(FilesManager.Directories.JavaRoot);
                if (online) {
                    if (!Directory.Exists(dir)) {
                        task.Description = "Fetching";
                        var openjdk = JsonConvert.DeserializeObject<OpenJdkJson>(Fetcher.Fetch(Fetcher.BlowaunchEndpoints.OpenJdk));
                        if (!openjdk!.Versions.ContainsKey(main.JavaMajor)) {
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
                    } else AnsiConsole.WriteLine("[OpenJDK] Skipping, already downloaded!");
                } else AnsiConsole.WriteLine("[OpenJDK] Skipping, we are in offline mode");
                task.StopTask();
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
            DownloadAll(main, online);
        }
    }
}