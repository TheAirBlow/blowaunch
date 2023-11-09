using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Blowaunch.Library;
using Newtonsoft.Json;
using Spectre.Console;

namespace Blowaunch.ConsoleApp
{
    /// <summary>
    /// Parse the website and get the installer link.
    /// After that we download it, extract and get
    /// everything we need.
    ///
    /// Sorry LexManos :)
    /// </summary>
    public static class ForgeThingy
    {
        /// <summary>
        /// Get the installer link
        /// </summary>
        /// <param name="version">Version</param>
        /// <returns>Installer link</returns>
        public static string GetLink(string version)
        {
            try {
                var forgeLink = string.Format(Fetcher.ForgeEndpoints.ForgeWebsite, version);
                var content = Fetcher.Fetch(forgeLink);
                var recommended = content.IndexOf("Download Recommended", StringComparison.Ordinal);
                var latest = content.IndexOf("Download Latest", StringComparison.Ordinal);
                var subst = content.Substring(recommended == -1 ? latest : recommended);
                var download = subst.IndexOf("<a href=\"", StringComparison.Ordinal);
                var subst2 = subst.Substring(download);
                var url = subst2.IndexOf("url=", StringComparison.Ordinal) + "url=".Length;
                var subst3 = subst2.Substring(url);
                var end = subst3.IndexOf("\"", StringComparison.Ordinal);
                return subst3.Substring(0, end);
            } catch (Exception e) {
                AnsiConsole.MarkupLine("[red]Unable to parse the website: An exception occured.[/]");
                AnsiConsole.WriteException(e);
                Environment.Exit(-1);
                return null;
            }
        }

        /// <summary>
        /// Get artifact's path
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="original">Return the original data if it is not in brackets</param>
        /// <returns>Path</returns>
        public static string ArtifactPath(string data, bool original)
        {
            string path;
            if (data.StartsWith("[") && data.EndsWith("]")) {
                data = data.Substring(1, data.Length - 2);
                var ext = ".jar";
                var split = data.Split(':');
                var index = split[^1].IndexOf("@", StringComparison.Ordinal);
                if (index != -1) {
                    ext = $".{split[^1].Substring(index + 1)}";
                    split[^1] = split[^1].Substring(0, index);
                }
                var package = split[0];
                var name = split[1];
                var version = split[2];
                var classifier = split.Length > 3 ? $"-{split[3]}" : "";
                path = Path.Combine(FilesManager.Directories.LibrariesRoot,
                    package.Replace('.', Path.DirectorySeparatorChar),
                    name, version, $"{name}-{version}{classifier}{ext}");
            } else if (original) return data;
            else if (data.StartsWith('\'') && data.EndsWith("\'")) path = data;
            else path = Path.Combine(FilesManager.Directories.LibrariesRoot, data);

            if (Path.IsPathFullyQualified(path) && !Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        /// <summary>
        /// Get the addon JSON
        /// </summary>
        /// <param name="link">Link to the installer</param>
        /// <param name="main">Main JSON</param>
        /// <returns>The addon JSON</returns>
        public static BlowaunchAddonJson GetAddonJson(string link, BlowaunchMainJson main)
        {
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn());
            return progress.Start(ctx => {
                var task = ctx.AddTask("Downloading installer").IsIndeterminate();
                task.StartTask();
                var jar = Path.Combine(Path.GetTempPath(), ".blowaunch-forge", "installer.jar");
                var dir = Path.Combine(Path.GetTempPath(), ".blowaunch-forge");
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir); Fetcher.Download(link, jar);
                task.Description = "Extracting";
                ZipFile.ExtractToDirectory(jar, dir);
                task.Description = "Parsing";
                var content = File.ReadAllText(Path.Combine(dir, "version.json"));
                var data = MojangLegacyMainJson.IsLegacyJson(content) 
                    ? BlowaunchMainJson.MojangToBlowaunchPartial(JsonConvert.DeserializeObject<MojangLegacyMainJson>(content)) 
                    : BlowaunchMainJson.MojangToBlowaunchPartial(JsonConvert.DeserializeObject<MojangMainJson>(content));
                task.Description = "Processing addon";
                var addon = new BlowaunchAddonJson {
                    Legacy = MojangLegacyMainJson.IsLegacyJson(content),
                    Author = "LexManos and contributors",
                    Information = "Black magic performed on the installer JAR",
                    BaseVersion = data.Version.Split('-')[0],
                    Arguments = new BlowaunchMainJson.JsonArguments(),
                    MainClass = data.MainClass
                };
                var libraries = new List<BlowaunchMainJson.JsonLibrary>();
                foreach (var lib in data.Libraries) {
                    if (string.IsNullOrEmpty(lib.Url)) {
                        var dest = Path.Combine(FilesManager.Directories.Root, "forge", $"{lib.Name}-{lib.Version}.jar");
                        if (!Directory.Exists(Path.GetDirectoryName(dest))) Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        if (!File.Exists(dest)) File.Copy(Path.Combine(dir, "maven", lib.Path.Replace('/', 
                            Path.DirectorySeparatorChar)), dest);
                        lib.Url = $"file://{dest}"; // :bigbrain:
                    }
                    libraries.Add(lib);
                }

                addon.Arguments.Game = data.Arguments.Game;
                addon.Arguments.Java = data.Arguments.Java;
                addon.Libraries = libraries.ToArray();
                task.StopTask();
                return addon;
            });
        }

        /// <summary>
        /// Run processors
        /// </summary>
        /// <param name="main">Main JSON</param>
        /// <param name="online">Is in online mode</param>
        /// <returns>The addon JSON</returns>
        public static void RunProcessors(BlowaunchMainJson main, bool online)
        {
            var progress = AnsiConsole.Progress()
                .HideCompleted(true)
                .Columns(new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ElapsedTimeColumn());
            progress.Start(ctx => {
                var task = ctx.AddTask("Downloading installer").IsIndeterminate();
                task.StartTask();
                var jar = Path.Combine(Path.GetTempPath(), ".blowaunch-forge", "installer.jar");
                var dir = Path.Combine(Path.GetTempPath(), ".blowaunch-forge");
                var contentInstaller = File.ReadAllText(Path.Combine(dir, "install_profile.json"));
                var dataInstaller = JsonConvert.DeserializeObject<ForgeInstallerJson>(contentInstaller);
                if (File.Exists(Path.Combine(FilesManager.Directories.VersionsRoot, main.Version, $"forge.json"))) {
                    AnsiConsole.WriteLine("[Forge] Skipping processors, already done!");
                    task.StopTask();
                }
                task.Description = "Processing artifact paths";
                var descriptors = new Dictionary<string, string>();
                foreach (var i in dataInstaller?.Data!)
                    descriptors.Add(i.Key, ArtifactPath(i.Value.Client, false));
                task.IsIndeterminate = false;
                task.MaxValue = dataInstaller!.Libraries.Length;
                foreach (var lib in dataInstaller.Libraries) {
                    var split = lib.Name.Split(":");
                    split[2] = split[2].Split('@')[0];
                    task.Description = $"Downloading {split[1]} v{split[2]}";
                    if (string.IsNullOrEmpty(lib.Downloads.Artifact.Url)) {
                        var dest = Path.Combine(FilesManager.Directories.Root, "forge", 
                            $"{split[1]}-{split[2]}{Path.GetExtension(lib.Downloads.Artifact.Path)}");
                        if (!Directory.Exists(Path.GetDirectoryName(dest))) Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        if (!File.Exists(dest)) File.Copy(Path.Combine(dir, "maven", lib.Downloads.Artifact.Path!.Replace('/', 
                            Path.DirectorySeparatorChar)), dest);
                        lib.Downloads.Artifact.Url = $"file://{dest}"; // :bigbrain:
                    }
                    
                    FilesManager.DownloadLibrary(new BlowaunchMainJson.JsonLibrary {
                        Allow = Array.Empty<string>(),
                        Disallow = Array.Empty<string>(),
                        Exclude = Array.Empty<string>(),
                        Extract = false,
                        Name = split[1],
                        Package = split[0],
                        Version = split[2],
                        Platform = "any",
                        ShaHash = lib.Downloads.Artifact.ShaHash,
                        Path = lib.Downloads.Artifact.Path,
                        Size = lib.Downloads.Artifact.Size,
                        Url = lib.Downloads.Artifact.Url
                    }, main.Version, online);
                    task.Increment(1);
                }

                task.Value = 0;
                task.Description = $"Running processors";
                task.MaxValue = dataInstaller.Processors.Length;
                for (var index = 0; index < dataInstaller.Processors.Length; index++) {
                    var proc = dataInstaller.Processors[index];
                    if (proc.Sides != null && !proc.Sides.Contains("client")) {
                        AnsiConsole.WriteLine($"[Forge] Skipping processor {index + 1} - not for client!");
                        task.Increment(1);
                        continue;
                    }
                    var file = FilesManager.GetLibraryPath(new BlowaunchMainJson.JsonLibrary { Path = dataInstaller.Libraries
                        .FirstOrDefault(x => x.Name == proc.Jar)!.Downloads.Artifact.Path });
                    var mainClass = "<couldn't find>";
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    using (var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs)) {
                        var ze = zf.GetEntry($"META-INF/MANIFEST.MF");
                        using (var s = zf.GetInputStream(ze))
                        using (var r = new StreamReader(s)) {
                            while (!r.EndOfStream) {
                                var line = r.ReadLine();
                                if (line.StartsWith("Main-Class: "))
                                    mainClass = line.Substring("Main-Class: ".Length,
                                        line.Length - "Main-Class: ".Length);
                            }
                        }
                    }

                    var classpath = new StringBuilder();
                    var separator = Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";";
                    foreach (var str in proc.Classpath) {
                        var file2 = FilesManager.GetLibraryPath(new BlowaunchMainJson.JsonLibrary { Path = dataInstaller.Libraries
                            .FirstOrDefault(x => x.Name == str)!.Downloads.Artifact.Path });
                        classpath.Append($"{file2}{separator}");
                    }

                    classpath.Remove(classpath.Length - 1, 1);
                    var args = new StringBuilder();
                    foreach (var arg in proc.Arguments) {
                        var replaced = arg.Replace("{ROOT}", FilesManager.Directories.Root)
                            .Replace("{INSTALLER}", jar)
                            .Replace("{MINECRAFT_JAR}", Path.Combine(FilesManager.Directories.VersionsRoot,
                                main.Version,
                                $"{main.Version}.jar")).Replace("{SIDE}", "client")
                            .Replace("{MINECRAFT_VERSION}", main.Version.Split('-')[0]);
                        foreach (var desc in descriptors)
                            replaced = replaced.Replace("{" + desc.Key + "}", desc.Value);
                        replaced = ArtifactPath(replaced, true);
                        if (replaced.StartsWith("/") || replaced.StartsWith("\\")) replaced = 
                            Path.Combine(Path.GetTempPath(), ".blowaunch-forge", replaced
                                .Substring(1, replaced.Length - 1)
                                .Replace('/', '\\'));
                        args.Append($"{replaced} ");
                    }

                    args.Remove(args.Length - 1, 1);
                    var command = $"-cp {file}{separator}{classpath} {mainClass} {args}";
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo {
                        WorkingDirectory = FilesManager.Directories.Root,
                        FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot, 
                            main.JavaMajor.ToString()), "bin", "java"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Arguments = command
                    };
                    process.OutputDataReceived += (_, e) => {
                        if (e.Data == null) return;
                        AnsiConsole.WriteLine(e.Data);
                    };
                    process.ErrorDataReceived += (_, e) => {
                        if (e.Data == null) return;
                        AnsiConsole.WriteLine(e.Data);
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    if (proc.Output != null) {
                        AnsiConsole.WriteLine($"[Forge] Verifying hashes...");
                        foreach (var n in proc.Output) {
                            var path = n.Key;
                            var sha1 = n.Value;
                            foreach (var desc in descriptors)
                                path = path.Replace("{" + desc.Key + "}", desc.Value);
                            foreach (var desc in descriptors)
                                sha1 = sha1.Replace("{" + desc.Key + "}", desc.Value);
                            var hash = HashHelper.Hash(path);
                            sha1 = sha1.Substring(1, sha1.Length - 2);
                            if (hash != sha1) {
                                AnsiConsole.MarkupLine($"[red]Hash mismatch for {path}![/]");
                                AnsiConsole.MarkupLine($"[red]Expected: {sha1}[/]");
                                AnsiConsole.MarkupLine($"[red]Actual: {hash}[/]");
                                Environment.Exit(0);
                            }
                        }
                    }
                    task.Increment(1);
                }
                task.StopTask();
            });
        } 
    }
}