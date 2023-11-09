using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Blowaunch.Library;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using Newtonsoft.Json;
using Spectre.Console;
using static System.Int32;
using Panel = Avalonia.Controls.Panel;

namespace Blowaunch.AvaloniaApp.Views
{
    public class MainWindow : Window
    {
        private class Version {
            public string Name { get; set; }
            public string Id { get; set; }
        }
        
        private Panel _authPanel;
        private Button _runGameButton;
        private ComboBox _moddingCombo;
        private ComboBox _versionsCombo;
        public static volatile Runner.Configuration Configuration = null!;
        public static volatile bool Online;
        
        // Progress Bar
        private Panel _progressContainer;
        private ProgressBar _progressBar;
        private TextBlock _files;
        private TextBlock _info;
        
        // Settings
        private TextBox _resolution;
        private TextBox _javaArgs;
        private TextBox _gameArgs;
        private TextBox _ramMax;
        private ToggleSwitch _forceOffline;
        private ToggleSwitch _fullScreen;
        private ToggleSwitch _snaphots;
        private ToggleSwitch _alphas;
        private ToggleSwitch _betas;
        private ToggleSwitch _demo;
        
        private static bool CheckForInternet(int timeoutMs = 5000)
        {
            try {
                var request = (HttpWebRequest)WebRequest.Create("https://google.com");
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using var response = (HttpWebResponse)request.GetResponse();
                return true;
            } catch { return false; }
        }
        
        public MainWindow()
        {
            try {
                Configuration = JsonConvert.DeserializeObject<Runner.Configuration>(
                    File.ReadAllText("config.json"))!;
            } catch {
                File.WriteAllText("config.json", JsonConvert
                    .SerializeObject(new Runner.Configuration(), Formatting.Indented));
                Configuration = JsonConvert.DeserializeObject<Runner.Configuration>(
                    File.ReadAllText("config.json"))!;
            }

            Online = CheckForInternet();
            if (Configuration.ForceOffline)
                Online = false;
            FilesManager.InitializeDirectories();
            InitializeComponent();
        }

        public void BeginDownload(object? sender, RoutedEventArgs e)
        {
            _progressContainer.IsVisible = true;
            _runGameButton.IsEnabled = false;
            _moddingCombo.IsEnabled = false;
            _versionsCombo.IsEnabled = false;
            new Thread(DownloadThread).Start();
        }

        public async void DownloadThread()
        {
            BlowaunchMainJson mainJson = null!;
            BlowaunchAddonJson? addonJson = null!;
            var main = Path.Combine(FilesManager.Directories.VersionsRoot, Configuration.Version);
            if (!Directory.Exists(main)) Directory.CreateDirectory(main);
            var mainJsonPath = Path.Combine(main, $"version.json");
            var addonJsonPath = Path.Combine(main, $"addon.json");
            var addonFabricJsonPath = Path.Combine(main, $"fabric.json");
            var addonForgeJsonPath = Path.Combine(main, $"forge.json");
            switch (Configuration.Type) {
                case Runner.Configuration.VersionType.OfficialMojang:
                    if (Online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        try { mainJson = MojangFetcher.GetMain(Configuration.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version Configuration![/]");
                            return;
                        }
                    } else {
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the Configuration in offline mode");
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(await File.ReadAllTextAsync(mainJsonPath))!;
                    }
                    break;
                case Runner.Configuration.VersionType.CustomVersionFromDir:
                    AnsiConsole.WriteLine($"[Unofficial] The version is a custom one");
                    mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(
                        await File.ReadAllTextAsync(Path.Combine(FilesManager.Directories.VersionsRoot, Configuration.Version,
                            $"{Configuration.Version}.Configuration")))!;
                    break;
                case Runner.Configuration.VersionType.CustomWithAddonConfig:
                    AnsiConsole.WriteLine($"[Unofficial] The version is a custom one");
                    AnsiConsole.WriteLine($"[Unofficial] The addon is a custom one");
                    mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(
                        await File.ReadAllTextAsync(Path.Combine(FilesManager.Directories.VersionsRoot, Configuration.Version,
                            $"{Configuration.Version}.Configuration")))!;
                    addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(
                        await File.ReadAllTextAsync(Path.Combine(FilesManager.Directories.VersionsRoot, Configuration.Version, 
                            $"addon.Configuration")))!;
                    break;
                case Runner.Configuration.VersionType.OfficialWithAddonConfig:
                    if (Online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Unofficial] The addon is a custom one");
                        try { mainJson = MojangFetcher.GetMain(Configuration.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version Configuration![/]");
                            return;
                        }
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(
                            await File.ReadAllTextAsync(Path.Combine(FilesManager.Directories.VersionsRoot, Configuration.Version, 
                                $"addon.Configuration")))!;
                    } else {
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Version Configuration does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Addon Configuration does not exist![/]");
                            return;
                        }
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the Configuration in offline mode");
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(await File.ReadAllTextAsync(mainJsonPath))!;
                    }
                    
                    break;
                case Runner.Configuration.VersionType.OfficialWithFabricModLoader:
                    if (Online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Official] The addon is downloaded from Fabric's Maven repo");
                        try { mainJson = MojangFetcher.GetMain(Configuration.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version Configuration![/]");
                            return;
                        }
                        AnsiConsole.WriteLine("[Fabric] Fetching profile Configuration");
                        addonJson = FabricFetcher.GetAddon(Configuration.Version);
                        AnsiConsole.WriteLine("[Fabric] Done!");
                    } else {
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the Configuration in offline mode");
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Version Configuration does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonFabricJsonPath)) {
                            AnsiConsole.MarkupLine("[red]Fabric addon Configuration does not exist![/]");
                            return;
                        }
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(await File.ReadAllTextAsync(mainJsonPath))!;
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(await File.ReadAllTextAsync(addonFabricJsonPath))!;
                    }
                    
                    await File.WriteAllTextAsync(addonFabricJsonPath, JsonConvert.SerializeObject(addonJson, Formatting.Indented));
                    break;
                case Runner.Configuration.VersionType.OfficialWithForgeModLoader:
                    if (Online) {
                        AnsiConsole.WriteLine($"[Official] The version is downloaded from Mojang's servers");
                        AnsiConsole.WriteLine($"[Official] The addon is downloaded from Forge's maven repo");
                        try { mainJson = MojangFetcher.GetMain(Configuration.Version); } 
                        catch {
                            AnsiConsole.MarkupLine("[red]Unable to fetch the version Configuration![/]");
                            return;
                        }

                        // TODO: Get Forge JSON
                    } else {
                        if (!File.Exists(mainJsonPath)) {
                            AnsiConsole.MarkupLine($"[red]Version Configuration does not exist![/]");
                            return;
                        }
                        if (!File.Exists(addonForgeJsonPath)) {
                            AnsiConsole.MarkupLine($"[red]Forge addon Configuration does not exist![/]");
                            return;
                        }
                        AnsiConsole.WriteLine($"[Unverifiable] We can't redownload the Configuration in offline mode");
                        mainJson = JsonConvert.DeserializeObject<BlowaunchMainJson>(await File.ReadAllTextAsync(mainJsonPath))!;
                        addonJson = JsonConvert.DeserializeObject<BlowaunchAddonJson>(await File.ReadAllTextAsync(addonForgeJsonPath))!;
                    }
                    
                    await File.WriteAllTextAsync(addonForgeJsonPath, JsonConvert.SerializeObject(addonJson, Formatting.Indented));
                    break;
            }
            
            if (Configuration.Type == Runner.Configuration.VersionType.CustomVersionFromDir 
                || Configuration.Type == Runner.Configuration.VersionType.CustomWithAddonConfig)
                AnsiConsole.WriteLine("[Configuration] Be aware that the author/info may be a lie!");
            AnsiConsole.WriteLine($"[Configuration] Version author: {mainJson.Author}");
            AnsiConsole.WriteLine($"[Configuration] Version information: {mainJson.Information}");
            if (addonJson != null) {
                AnsiConsole.WriteLine($"[Configuration] Addon author: {addonJson.Author}");
                AnsiConsole.WriteLine($"[Configuration] Addon information: {addonJson.Information}");
            }
            await File.WriteAllTextAsync(mainJsonPath, JsonConvert.SerializeObject(mainJson, Formatting.Indented));
            
            // Here we download shit
            await Download(mainJson, addonJson);
            // TODO: Here we run Forge processors if needed
            await Dispatcher.UIThread.InvokeAsync(() => {
                _info.Text = "Running Minecraft!";
            });
            var command = addonJson != null 
                ? Runner.GenerateCommand(mainJson, addonJson, Configuration) 
                : Runner.GenerateCommand(mainJson, Configuration);
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo {
                WorkingDirectory = FilesManager.Directories.Root,
                FileName = Path.Combine(Path.Combine(FilesManager.Directories.JavaRoot, 
                    mainJson.JavaMajor.ToString()), "bin", "java"),
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
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressContainer.IsVisible = false;
                _runGameButton.IsEnabled = true;
                _moddingCombo.IsEnabled = true;
                _versionsCombo.IsEnabled = true;
            });
        }

        private async Task Download(BlowaunchMainJson main, BlowaunchAddonJson? addon)
        {
            if (addon != null) {
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
            }
            
            // Libraries
            AnsiConsole.WriteLine("[Downloader] Downloading libraries");
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressBar.Value = 0;
                _progressBar.Maximum = main.Libraries.Length;
                _files.Text = $"{_progressBar.Value}/{_progressBar.Maximum} files";
            });
            foreach (var lib in main.Libraries) {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    _info.Text = $"Downloading library {lib.Name} v{lib.Version} {lib.Platform}";
                });
                FilesManager.DownloadLibrary(lib, main.Version, Online);
                await Dispatcher.UIThread.InvokeAsync(() => {
                    _progressBar.Value++;
                    _files.Text = $"{_progressBar.Value}/{_progressBar.Maximum} files";
                });
            }
            
            // Assets
            AnsiConsole.WriteLine("[Downloader] Downloading assets");
            var assetsMojang = JsonConvert.DeserializeObject<MojangAssetsJson>(Fetcher.Fetch(main.Assets.Url));
            var assetsBlowaunch = BlowaunchAssetsJson.MojangToBlowaunch(assetsMojang);
            await File.WriteAllTextAsync(Path.Combine(FilesManager.Directories.AssetsIndexes, $"{main.Assets.Id}.json"),
                JsonConvert.SerializeObject(assetsMojang));
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressBar.Value = 0;
                _progressBar.Maximum = assetsBlowaunch.Assets.Length;
                _files.Text = $"{_progressBar.Value}/{_progressBar.Maximum} files";
            });
            foreach (var asset in assetsBlowaunch.Assets) {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    _info.Text = $"Downloading asset {Path.GetFileName(asset.Name)}";
                });
                FilesManager.DownloadAsset(asset, Online);
                await Dispatcher.UIThread.InvokeAsync(() => {
                    _progressBar.Value++;
                    _files.Text = $"{_progressBar.Value}/{_progressBar.Maximum} files";
                });
            }
            
            // Client
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressBar.Maximum = 2;
                _progressBar.IsIndeterminate = true;
                _info.Text = "Downloading client & log4j config";
                _files.Text = $"{_progressBar.Value}/{_progressBar.Maximum} files";
            });
            
            FilesManager.DownloadClient(main, Online);
            
            // OpenJDK
            var dir = Path.Combine(FilesManager.Directories.JavaRoot, main.JavaMajor.ToString());
            var extract = Path.Combine(FilesManager.Directories.JavaRoot);
            if (Online) {
                if (!Directory.Exists(dir)) {
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        _progressBar.Maximum = 1;
                        _progressBar.IsIndeterminate = true;
                        _info.Text = "Fetching OpenJDK JSON";
                        _files.Text = $"{_progressBar.Value}/{_progressBar.Maximum} files";
                    });
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
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Downloading OpenJDK v{main.JavaMajor}";
                        });
                        Fetcher.Download(link, Path.Combine(Path.GetTempPath(), 
                            Path.GetFileName(link)));
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Extracting OpenJDK v{main.JavaMajor}";
                        });
                        ZipFile.ExtractToDirectory(path, 
                            extract, true);
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Renaming directory";
                        });
                        Directory.Move(Path.Combine(extract, openjdk.Versions[main
                            .JavaMajor].Directory), dir);
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                        AnsiConsole.WriteLine("[OpenJDK] Detected Linux!");
                        var link = openjdk.Versions[main.JavaMajor].Linux;
                        var path = Path.Combine(Path.GetTempPath(),
                            Path.GetFileName(link)!);
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Downloading OpenJDK v{main.JavaMajor}";
                        });
                        Fetcher.Download(link, path);
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Extracting OpenJDK v{main.JavaMajor}";
                        });
                        ExtractTar(path, extract);
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Renaming directory";
                        });
                        Directory.Move(Path.Combine(extract, openjdk.Versions[main
                            .JavaMajor].Directory), dir);
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                        AnsiConsole.WriteLine("[OpenJDK] Detected MacOS!");
                        var link = openjdk.Versions[main.JavaMajor].MacOs;
                        var path = Path.Combine(Path.GetTempPath(),
                            Path.GetFileName(link)!);
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Downloading OpenJDK v{main.JavaMajor}";
                        });
                        Fetcher.Download(link, Path.Combine(Path.GetTempPath(), 
                            Path.GetFileName(link)!));
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Extracting OpenJDK v{main.JavaMajor}";
                        });
                        ExtractTar(path, extract);
                        await Dispatcher.UIThread.InvokeAsync(() => {
                            _info.Text = $"Renaming directory";
                        });
                        Directory.Move(Path.Combine(extract, openjdk.Versions[main
                            .JavaMajor].Directory), dir);
                    } else AnsiConsole.MarkupLine($"[red]Your OS is not supported![/]");
                } else AnsiConsole.WriteLine("[OpenJDK] Skipping, already downloaded!");
            } else AnsiConsole.WriteLine("[OpenJDK] Skipping, we are in offline mode");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Progress Bar
            _progressContainer = this.FindControl<Panel>("ProgressPanel");
            _progressBar = this.FindControl<ProgressBar>("ProgressBar");
            _files = this.FindControl<TextBlock>("ProgressFiles");
            _info = this.FindControl<TextBlock>("ProgressInfo");
            
            // Settings
            _resolution = this.FindControl<TextBox>("WindowSize");
            _javaArgs = this.FindControl<TextBox>("JavaArgs");
            _gameArgs = this.FindControl<TextBox>("GameArgs");
            _ramMax = this.FindControl<TextBox>("MaxRam");
            _forceOffline = this.FindControl<ToggleSwitch>("OfflineMode");
            _fullScreen = this.FindControl<ToggleSwitch>("FullScreen");
            _snaphots = this.FindControl<ToggleSwitch>("ShowSnapshot");
            _alphas = this.FindControl<ToggleSwitch>("ShowAlpha");
            _betas = this.FindControl<ToggleSwitch>("ShowBeta");
            _demo = this.FindControl<ToggleSwitch>("DemoMode");
            LoadSettings(null, null!);
            
            // Versions
            _authPanel = this.FindControl<Panel>("Authentication");
            _versionsCombo = this.FindControl<ComboBox>("Versions");
            _moddingCombo = this.FindControl<ComboBox>("Modding");
            _runGameButton = this.FindControl<Button>("RunGame");
            _runGameButton.Click += BeginDownload;

            _moddingCombo.SelectedIndex = (int)Configuration.Type;
            _versionsCombo.SelectionChanged += ComboOnSelectionChanged;
            _moddingCombo.SelectionChanged += ComboModdingSelectionChanged;
            
            LoadVersions();
        }

        private void SaveConfiguration()
            => File.WriteAllText("config.Configuration", JsonConvert.SerializeObject(
                Configuration, Formatting.Indented));

        private void ComboOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try {
                var version = e.AddedItems[0] as Version;
                Configuration.Version = version!.Id;
                Configuration.VersionIndex = _versionsCombo.SelectedIndex;
                SaveConfiguration();
            } catch { /* Ignore */ }
        }

        private void ComboModdingSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Configuration.Type = (Runner.Configuration.VersionType) 
                _moddingCombo.SelectedIndex;
            SaveConfiguration();
        }
        
        private void LoadVersions()
        {
            var json = MojangFetcher.GetVersions();
            var versions = new List<Version>();
            if (Configuration.LastVersionsCount == 0)
                Configuration.LastVersionsCount =
                    json.Versions.Length;
            foreach (var i in json.Versions) {
                var sb = new StringBuilder();
                if (i.Id == json.Latest.Release
                    || i.Id == json.Latest.Snapshot)
                    sb.Append("Latest ");
                switch (i.Type) {
                    case BlowaunchMainJson.JsonType.release:
                        sb.Append("Release ");
                        break;
                    case BlowaunchMainJson.JsonType.snapshot:
                        if (!Configuration.ShowSnapshots) continue;
                        sb.Append("Snapshot ");
                        break;
                    case BlowaunchMainJson.JsonType.old_beta:
                        if (!Configuration.ShowOldBeta) continue;
                        sb.Append("Beta ");
                        break;
                    case BlowaunchMainJson.JsonType.old_alpha:
                        if (!Configuration.ShowOldAlpha) continue;
                        sb.Append("Alpha ");
                        break;
                }

                sb.Append(i.Id);
                var item = new Version { Name = sb.ToString(), Id = i.Id };
                versions.Add(item);
            }
            
            Configuration.VersionIndex = json.Versions.Length
                                         - Configuration.LastVersionsCount 
                                         + Configuration.VersionIndex;
            _versionsCombo.Items = versions;
            _versionsCombo.SelectedIndex = Configuration.VersionIndex;
        }

        public void SaveSettings(object? sender, RoutedEventArgs e)
        {
            var split = _resolution.Text.Split('x');
            if (split.Length != 2 || !TryParse(split[0], out _)
                                  || !TryParse(split[1], out _)) {
                _resolution.Text = $"{Configuration.WindowSize.X}x{Configuration.WindowSize.Y}";
            } else {
                Configuration.WindowSize.X = Parse(split[0]);
                Configuration.WindowSize.Y = Parse(split[1]);
            }

            Configuration.JvmArgs = _javaArgs.Text;
            Configuration.GameArgs = _gameArgs.Text;
            if (_ramMax.Text.Split(' ').Length != 1
                || _ramMax.Text == "") {
                _ramMax.Text = Configuration.RamMax;
            } else Configuration.RamMax = _ramMax.Text;
            
            Configuration.ForceOffline = (bool)_forceOffline.IsChecked!;
            Configuration.CustomWindowSize = !(bool)_fullScreen.IsChecked!;
            Configuration.ShowSnapshots = (bool)_snaphots.IsChecked!;
            Configuration.ShowOldAlpha = (bool)_betas.IsChecked!;
            Configuration.ShowOldBeta = (bool)_alphas.IsChecked!;
            Configuration.DemoUser = (bool)_demo.IsChecked!;
            SaveConfiguration(); LoadVersions();
        }

        public void DisableResolution(object? sender, RoutedEventArgs e)
            => _resolution.IsReadOnly = true;
        
        public void EnableResolution(object? sender, RoutedEventArgs e)
            => _resolution.IsReadOnly = false;

        public void LoadSettings(object? sender, RoutedEventArgs e)
        {
            _resolution.IsReadOnly = !Configuration.CustomWindowSize;
            _resolution.Text = $"{Configuration.WindowSize.X}x{Configuration.WindowSize.Y}";
            _javaArgs.Text = Configuration.JvmArgs;
            _gameArgs.Text = Configuration.GameArgs;
            _ramMax.Text = Configuration.RamMax;
            _forceOffline.IsChecked = Configuration.ForceOffline;
            _fullScreen.IsChecked = !Configuration.CustomWindowSize;
            _snaphots.IsChecked = Configuration.ShowSnapshots;
            _alphas.IsChecked = Configuration.ShowOldAlpha;
            _betas.IsChecked = Configuration.ShowOldBeta;
            _demo.IsChecked = Configuration.DemoUser;
        }
        
        public void ResetSettings(object? sender, RoutedEventArgs e)
        {
            File.WriteAllText("config.Configuration", JsonConvert
                .SerializeObject(new Runner.Configuration(), Formatting.Indented));
            Configuration = JsonConvert.DeserializeObject<Runner.Configuration>(
                File.ReadAllText("config.Configuration"))!;
            LoadSettings(sender, e); LoadVersions();
        }

        public void OpenDirectory(object? sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo {
                FileName = FilesManager.Directories.Root,
                UseShellExecute = true,
                Verb = "open"
            });

        public void CloseAuthPanel(object? sender, RoutedEventArgs e)
            => _authPanel.IsVisible = false;
        
        public void OpenAuthPanel(object? sender, RoutedEventArgs e)
            => _authPanel.IsVisible = true;
    }
}