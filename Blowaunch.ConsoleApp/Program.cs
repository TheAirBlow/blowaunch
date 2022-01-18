using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Blowaunch.Library;
using Blowaunch.Library.Downloader;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;

namespace Blowaunch.ConsoleApp
{
    public static class Program
    {
        public static bool CheckForInternet(int timeoutMs = 2000)
        {
            try {
                var request = (HttpWebRequest)WebRequest.Create("8.8.8.8");
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using var response = (HttpWebResponse)request.GetResponse();
                return true;
            } catch { return false; }
        }
        
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static void Main(string[] args)
        {
            /* Just to convert forge JSON to Blowaunch one
            var bebra = JsonConvert.DeserializeObject<MojangMainJson>(
                File.ReadAllText("C:\\Users\\TheAirBlow\\AppData\\Roaming\\.blowaunch\\versions\\1.12.2\\addon.json"));
            var libraries = new List<BlowaunchMainJson.JsonLibrary>();
            foreach (var lib in bebra.Libraries) {
                var split = lib.Name.Split(':');
                var main = new BlowaunchMainJson.JsonLibrary {
                    Allow = Array.Empty<string>(),
                    Disallow = Array.Empty<string>(),
                    Package = split[0],
                    Name = split[1],
                    Version = split[2],
                    Platform = "any",
                    Size = lib.Downloads.Artifact.Size,
                    ShaHash = lib.Downloads.Artifact.ShaHash,
                    Url = lib.Downloads.Artifact.Url,
                    Exclude = Array.Empty<string>(),
                    Extract = false
                };
                
                libraries.Add(main);
            }
            Console.WriteLine(JsonConvert.SerializeObject(libraries, Formatting.Indented));
            return;
            */

            AnsiConsole.MarkupLine($"[yellow]Welcome to Blowaunch v{Assembly.GetExecutingAssembly().GetName().Version!}![/]");
            AnsiConsole.MarkupLine($"[yellow]Written completely from scratch, but is based on deprecated Node.js version[/]");
            AnsiConsole.MarkupLine($"[yellow]Official GitHub: https://github.com/theairblow/blowaunch[/]");

            if (!File.Exists("config.json")) {
                AnsiConsole.MarkupLine("[red]config.json does not exist![/]");
                AnsiConsole.MarkupLine("[red]An empty one was created.[/]");
                File.WriteAllText("config.json", JsonConvert
                    .SerializeObject(new Runner.Configuration()));
                return;
            }

            var json = JsonConvert.DeserializeObject<Runner.Configuration>(
                File.ReadAllText("config.json"));
            BlowaunchMainJson mainJson = null;
            BlowaunchAddonJson addonJson = null;
            
            var online = CheckForInternet();
            if (json.ForceOffline) online = false;
            AnsiConsole.WriteLine(json.ForceOffline 
                ? $"[Internet] Offline mode forced by configuration"
                : online 
                    ? $"[Internet] No internet, offline mode"
                    : $"[Internet] Internet connection present, online mode");

            var mainJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"{json.Version}.json");
            var addonJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"addon.json");
            var addonFabricJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"fabric.json");
            var addonForgeJsonPath =
                Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, $"forge.json");
            var command = "";
            switch (json.Type) {
                case Runner.Configuration.VersionType.OfficialMojang:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        mainJson = MojangFetcher.GetMain(json.Version);
                    } else {
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(File.ReadAllText(mainJsonPath));
                    }
                    MainDownloader.DownloadAll(mainJson, online);
                    command = Runner.GenerateCommand(mainJson, json);
                    break;
                case Runner.Configuration.VersionType.CustomVersionFromDir:
                    AnsiConsole.WriteLine($"[Unofficial] The version is a custom one");
                    mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(
                        File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version,
                            $"{json.Version}.json")));
                    MainDownloader.DownloadAll(mainJson, online);
                    command = Runner.GenerateCommand(mainJson, json);
                    break;
                case Runner.Configuration.VersionType.CustomWithAddonConfig:
                    AnsiConsole.WriteLine($"[Unofficial] The version is a custom one");
                    AnsiConsole.WriteLine($"[Unofficial] The addon is a custom one");
                    mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(
                        File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version,
                            $"{json.Version}.json")));
                    addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(
                        File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, 
                            $"addon.json")));
                    MainDownloader.DownloadAll(mainJson, addonJson, online);
                    command = Runner.GenerateCommand(mainJson, addonJson, json);
                    break;
                case Runner.Configuration.VersionType.OfficialWithAddonConfig:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Unofficial] The addon is a custom one");
                        mainJson = MojangFetcher.GetMain(json.Version);
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(
                            File.ReadAllText(Path.Combine(FilesManager.Directories.VersionsRoot, json.Version, 
                                $"addon.json")));
                    } else {
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Version JSON does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Addon JSON does not exist![/]");
                            return;
                        }
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(File.ReadAllText(mainJsonPath));
                    }

                    MainDownloader.DownloadAll(mainJson, addonJson, online);
                    command = Runner.GenerateCommand(mainJson, addonJson, json);
                    break;
                case Runner.Configuration.VersionType.OfficialWithFabricModLoader:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Official] The addon is downloaded from Fabric's Maven repo");
                        mainJson = MojangFetcher.GetMain(json.Version);
                        AnsiConsole.WriteLine("[Fabric] Fetching profile JSON");
                        addonJson = FabricFetcher.GetAddon(json.Version);
                        AnsiConsole.WriteLine("[Fabric] Done!");
                    } else {
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Version JSON does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonFabricJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Fabric addon JSON does not exist![/]");
                            return;
                        }
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(File.ReadAllText(mainJsonPath));
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(File.ReadAllText(addonFabricJsonPath));
                    }
                    
                    MainDownloader.DownloadAll(mainJson, addonJson, online);
                    command = Runner.GenerateCommand(mainJson, addonJson, json);
                    File.WriteAllText(addonFabricJsonPath, JsonConvert.SerializeObject(addonJson));
                    break;
                case Runner.Configuration.VersionType.OfficialWithForgeModLoader:
                    if (online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Official] The addon is downloaded from Blowaunch's repo");
                        AnsiConsole.WriteLine("[Forge] Fetching forge.json");
                        var forge = JsonConvert.DeserializeObject<ForgeJson>(Fetcher.Fetch(Fetcher.BlowaunchEndpoints.Forge));
                        if (!forge.Versions.ContainsKey(json.Version)) {
                            AnsiConsole.MarkupLine($"[red]There is no Forge for {json.Version} in the repo yet![/]");
                            return;
                        }

                        var link = forge.Versions[json.Version];
                        AnsiConsole.WriteLine($"[Forge] Fetching {Path.GetFileName(link)}");
                        mainJson = MojangFetcher.GetMain(json.Version);
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(Fetcher.Fetch(link));
                        AnsiConsole.WriteLine($"[Forge] Done!");
                    } else {
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine($"[red]Version JSON does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonForgeJsonPath)) {
                            AnsiConsole.MarkupLine($"[red]Forge addon JSON does not exist![/]");
                            return;
                        }
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the JSON in offline mode");
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(File.ReadAllText(mainJsonPath));
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(File.ReadAllText(addonForgeJsonPath));
                    }
                    
                    MainDownloader.DownloadAll(mainJson, addonJson, online);
                    command = Runner.GenerateCommand(mainJson, addonJson, json);
                    File.WriteAllText(addonForgeJsonPath, JsonConvert.SerializeObject(addonJson));
                    break;
            }

            if (json.Type == Runner.Configuration.VersionType.CustomVersionFromDir 
                || json.Type == Runner.Configuration.VersionType.CustomWithAddonConfig)
                AnsiConsole.WriteLine("[JSON] Be aware that the author/info may be a lie!");
            AnsiConsole.WriteLine($"[JSON] Version author: {mainJson.Author}");
            AnsiConsole.WriteLine($"[JSON] Version information: {mainJson.Information}");
            if (addonJson != null) {
                AnsiConsole.WriteLine($"[JSON] Addon author: {addonJson.Author}");
                AnsiConsole.WriteLine($"[JSON] Addon information: {addonJson.Information}");
            }
            File.WriteAllText(mainJsonPath, JsonConvert.SerializeObject(mainJson));

            var dir = Path.Combine(FilesManager.Directories.JavaRoot, mainJson.JavaMajor.ToString());
            var extract = Path.Combine(FilesManager.Directories.JavaRoot);
            if (online) {
                var progress = AnsiConsole.Progress()
                    .HideCompleted(true)
                    .Columns(new TaskDescriptionColumn(), 
                        new ProgressBarColumn(), 
                        new PercentageColumn(), 
                        new ElapsedTimeColumn());
                progress.Start(i => {
                    var main = i.AddTask("OpenJDK").IsIndeterminate();
                    if (!Directory.Exists(dir)) {
                        main.Description = "Fetching";
                        var openjdk = JsonConvert.DeserializeObject<OpenJdkJson>(Fetcher.Fetch(Fetcher.BlowaunchEndpoints.OpenJdk));
                        if (!openjdk.Versions.ContainsKey(mainJson.JavaMajor)) {
                            AnsiConsole.MarkupLine($"[red]Unable to find OpenJDK version {mainJson.JavaMajor}![/]");
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
                            var link = openjdk.Versions[mainJson.JavaMajor].Windows;
                            var path = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(link)!);
                            main.Description = "Downloading";
                            Fetcher.Download(link, Path.Combine(Path.GetTempPath(), 
                                Path.GetFileName(link)!));
                            main.Description = "Extracting";
                            ZipFile.ExtractToDirectory(path, 
                                extract, true);
                            main.Description = "Renaming";
                            Directory.Move(Path.Combine(extract, openjdk.Versions[mainJson
                                .JavaMajor].Directory), dir);
                        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                            AnsiConsole.WriteLine("[OpenJDK] Detected Linux!");
                            var link = openjdk.Versions[mainJson.JavaMajor].Linux;
                            var path = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(link)!);
                            main.Description = "Downloading";
                            Fetcher.Download(link, path);
                            main.Description = "Extracting";
                            ExtractTar(path, extract);
                            main.Description = "Renaming";
                            Directory.Move(Path.Combine(extract, openjdk.Versions[mainJson
                                .JavaMajor].Directory), dir);
                        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                            AnsiConsole.WriteLine("[OpenJDK] Detected MacOS!");
                            var link = openjdk.Versions[mainJson.JavaMajor].MacOs;
                            var path = Path.Combine(Path.GetTempPath(),
                                Path.GetFileName(link)!);
                            main.Description = "Downloading";
                            Fetcher.Download(link, Path.Combine(Path.GetTempPath(), 
                                Path.GetFileName(link)!));
                            main.Description = "Extracting";
                            ExtractTar(path, extract);
                            main.Description = "Renaming";
                            Directory.Move(Path.Combine(extract, openjdk.Versions[mainJson
                                .JavaMajor].Directory), dir);
                        } else {
                            AnsiConsole.MarkupLine($"[red]Your OS is not supported![/]");
                            return;
                        }
                    } else AnsiConsole.WriteLine("[OpenJDK] Skipping, already downloaded!");
                });
            } else AnsiConsole.WriteLine("[OpenJDK] Skipping, we are in offline mode");

            AnsiConsole.WriteLine($"[Runner] Starting minecraft...");
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                WorkingDirectory = FilesManager.Directories.Root,
                FileName = Path.Combine(dir, "bin", "java"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = command
            };
            proc.OutputDataReceived += (_, e) => {
                Console.WriteLine(e.Data);
            };
            proc.ErrorDataReceived += (_, e) => {
                Console.WriteLine(e.Data);
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            Console.WriteLine("\nPress any key to close...");
            Console.ReadKey();
        }
    }
}