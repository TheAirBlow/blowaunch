using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blowaunch.Library;
using Blowaunch.Library.FetcherJson;
using Newtonsoft.Json;
using Spectre.Console;
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
        private ComboBox _versionsCombo;
        public static Runner.Configuration Configuration = null!;

        public MainWindow()
        {
            if (!File.Exists("config.json"))
                File.WriteAllText("config.json", JsonConvert
                    .SerializeObject(new Runner.Configuration(), Formatting.Indented));
            else {
                try {
                    Configuration = JsonConvert.DeserializeObject<Runner.Configuration>(
                        File.ReadAllText("config.json"))!;
                } catch {
                    File.WriteAllText("config.json", JsonConvert
                        .SerializeObject(new Runner.Configuration(), Formatting.Indented));
                    Configuration = JsonConvert.DeserializeObject<Runner.Configuration>(
                        File.ReadAllText("config.json"))!;
                }
            }
            
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _authPanel = this.FindControl<Panel>("Authentication");
            _versionsCombo = this.FindControl<ComboBox>("Versions");
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

            // In case a new version is released
            // Won't work if we would enable/disable
            // "Show snaphots" and similar, in case of it
            // VersionIndex should be updated manually.
            Configuration.VersionIndex = json.Versions.Length
                                         - Configuration.LastVersionsCount 
                                         + Configuration.VersionIndex;
            _versionsCombo.Items = versions;
            _versionsCombo.SelectedIndex = Configuration.VersionIndex;
            _versionsCombo.SelectionChanged += ComboOnSelectionChanged;
        }
        
        private void SaveConfiguration()
            => File.WriteAllText("config.json", JsonConvert.SerializeObject(
                Configuration, Formatting.Indented));

        private void ComboOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var version = e.AddedItems[0] as Version;
            Configuration.Version = version!.Id;
            Configuration.VersionIndex = _versionsCombo.SelectedIndex;
            SaveConfiguration();
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