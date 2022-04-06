using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Blowaunch.Library;
using Blowaunch.Library.Authentication;
using Hardware.Info;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using Panel = Avalonia.Controls.Panel;

namespace Blowaunch.AvaloniaApp.Views;
#pragma warning disable CS8618
#pragma warning disable CS0618
#pragma warning disable CS1998
public class MainWindow : Window
{
    #region UI Elements
    // All fields used
    private Panel _authPanel;
    private Panel _loadingPanel;
    private Panel _progressPanel;
    private ComboBox _versionsCombo;
    private TextBlock _accountName;
    private TextBlock _accountType;
    private ComboBox _accountsCombo;
    private TextBox _usernameMojang;
    private TextBox _passwordMojang;
    private TextBox _usernameCracked;
    private TextBlock _progressInfo;
    private TextBlock _progressFiles;
    private ProgressBar _progressBar;
    private Button _mojangLoginButton;
    private Button _microsoftLoginButton;
    
    // Settings
    private ToggleSwitch _customWindowSize;
    private NumericUpDown _windowWidth;
    private NumericUpDown _windowHeight;
    private TextBox _javaArguments;
    private TextBox _gameArguments;
    private NumericUpDown _ramManual;
    private Slider _ramSlider;
    private ToggleSwitch _showSnaphots;
    private ToggleSwitch _showAlpha;
    private ToggleSwitch _showBeta;
    private ToggleSwitch _forceOffline;
    private ToggleSwitch _minecraftDemo;
    private Button _saveChanges;
    private Button _revertChanges;
    #endregion
    #region Other stuff
    /// <summary>
    /// Launcher Configuration
    /// </summary>
    public static LauncherConfig Config = new();
    
    /// <summary>
    /// Serilog Logger
    /// </summary>
    public static Logger Logger = new LoggerConfiguration()
        .WriteTo.File("blowaunch.log")
        .WriteTo.Console()
        .CreateLogger();
    
    /// <summary>
    /// Is in offline mode?
    /// </summary>
    public static bool OfflineMode;
    
    /// <summary>
    /// Hardware information
    /// </summary>
    private HardwareInfo _info = new();

    /// <summary>
    /// Did the SelectionChanged event was set?
    /// </summary>
    private bool _selectionChanged;
    #endregion
    #region Initialization
    /// <summary>
    /// Initialize everything
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        InitializeFields();
        _loadingPanel!.IsVisible = true;
        _progressPanel!.IsVisible = true;
        _progressBar!.IsIndeterminate = true;

        new Thread(async () => {
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading hardware info...";
                _progressFiles!.Text = "Step 1 out of 5";
            });
            _info.RefreshMemoryList();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading configuration...";
                _progressFiles!.Text = "Step 2 out of 5";
            });
            await LoadConfig();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading versions...";
                _progressFiles!.Text = "Step 3 out of 5";
            });
            await LoadVersions();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Validating accounts...";
                _progressFiles!.Text = "Step 4 out of 5";
            });
            ValidateAccounts();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo!.Text = "Loading settings and accounts...";
                _progressFiles!.Text = "Step 5 out of 5";
            });
            await Dispatcher.UIThread.InvokeAsync(() => {
                LoadSettings();
                ReloadAccounts();
                _loadingPanel.IsVisible = false;
                _progressPanel.IsVisible = false;
                _progressBar.IsIndeterminate = false;
            });
        }).Start();
    }
    
    /// <summary>
    /// Initialize components
    /// </summary>
    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);
    #endregion
    #region CheckForInternet()
    /// <summary>
    /// Check for internet connection
    /// </summary>
    /// <param name="timeoutMs">Timeout (in millis)</param>
    /// <returns>Boolean value</returns>
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
    #endregion
    #region InitializeFields()
    /// <summary>
    /// Initialize fields used
    /// </summary>
    private void InitializeFields()
    {
        _loadingPanel = this.FindControl<Panel>("Loading");
        _authPanel = this.FindControl<Panel>("Authentication");
        _versionsCombo = this.FindControl<ComboBox>("Versions");
        _accountsCombo = this.FindControl<ComboBox>("Accounts");
        _progressPanel = this.FindControl<Panel>("ProgressPanel");
        _accountName = this.FindControl<TextBlock>("AccountName");
        _accountType = this.FindControl<TextBlock>("AccountType");
        _usernameMojang = this.FindControl<TextBox>("UsernameMojang");
        _passwordMojang = this.FindControl<TextBox>("PasswordMojang");
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _progressInfo = this.FindControl<TextBlock>("ProgressInfo");
        _progressFiles = this.FindControl<TextBlock>("ProgressFiles");
        _mojangLoginButton = this.FindControl<Button>("MojangButton");
        _usernameCracked = this.FindControl<TextBox>("UsernameCracked");
        _microsoftLoginButton = this.FindControl<Button>("LoginButton");
        _customWindowSize = this.FindControl<ToggleSwitch>("CustomWindowSize");
        _showSnaphots = this.FindControl<ToggleSwitch>("ShowSnapshots");
        _showAlpha = this.FindControl<ToggleSwitch>("ShowAlpha");
        _showBeta = this.FindControl<ToggleSwitch>("ShowBeta");
        _forceOffline = this.FindControl<ToggleSwitch>("ForceOffline");
        _minecraftDemo = this.FindControl<ToggleSwitch>("MinecraftDemo");
        _windowWidth = this.FindControl<NumericUpDown>("WindowWidth");
        _windowHeight = this.FindControl<NumericUpDown>("WindowHeight");
        _ramManual = this.FindControl<NumericUpDown>("RamManual");
        _javaArguments = this.FindControl<TextBox>("JavaArguments");
        _gameArguments = this.FindControl<TextBox>("GameArguments");
        _saveChanges = this.FindControl<Button>("SaveChanges");
        _revertChanges = this.FindControl<Button>("RevertChanges");
        _ramSlider = this.FindControl<Slider>("RamSlider");
        
        _ramManual.ValueChanged += (_, e) => {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_ramSlider.Value == _ramManual.Value)
                return;
            if (e.NewValue > _ramSlider.Maximum)
                _ramManual.Value = _ramSlider.Maximum;
            else if (e.NewValue < 0)
                _ramManual.Value = 0;
            
            _ramSlider.Value = e.NewValue;
        };
        
        _windowWidth.ValueChanged += (_, e) => {
            if (e.NewValue < 0)
                _windowWidth.Value = 0;
        };
        
        _windowHeight.ValueChanged += (_, e) => {
            if (e.NewValue < 0)
                _windowHeight.Value = 0;
        };

        _ramSlider.PropertyChanged += (_, _) => {
            if (_ramSlider.Value % 1 != 0)
                _ramSlider.Value = Math.Floor(_ramSlider.Value);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_ramSlider.Value == _ramManual.Value)
                return;
            _ramManual.Value = _ramSlider.Value;
        };
    }
    #endregion
    #region LoadVersions()
    /// <summary>
    /// Load Minecraft versions
    /// </summary>
    private async Task LoadVersions()
    {
        Logger.Information("Loading versions...");
        var versions = new List<LauncherConfig.VersionClass>();
        if (CheckForInternet()) {
            Logger.Information("Internet available, fetching");
            var json = MojangFetcher.GetVersions();
            foreach (var i in json.Versions) {
                try {
                    var sb = new StringBuilder();
                    switch (i.Type) {
                        case BlowaunchMainJson.JsonType.release:
                            sb.Append("Release ");
                            break;
                        case BlowaunchMainJson.JsonType.snapshot:
                            if (!Config.ShowSnapshots) continue;
                            sb.Append("Snapshot ");
                            break;
                        case BlowaunchMainJson.JsonType.old_beta:
                            if (!Config.ShowBeta) continue;
                            sb.Append("Beta ");
                            break;
                        case BlowaunchMainJson.JsonType.old_alpha:
                            if (!Config.ShowAlpha) continue;
                            sb.Append("Alpha ");
                            break;
                    }

                    sb.Append(i.Id);
                    var item = new LauncherConfig.VersionClass 
                        { Name = sb.ToString(), Id = i.Id };
                    versions.Add(item);
                } catch (Exception e) {
                    Logger.Error($"Unable to load {i.Id}! {0}", e);
                }
            }
        } else {
            Logger.Warning("Internet unavailable!");
            await Dispatcher.UIThread.InvokeAsync(() => {
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                        Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = "Offline mode enabled - " +
                                         "integrity checks would be " +
                                         "skipped.",
                        ContentTitle = "No internet connection!"
                    });
                msBoxStandardWindow.Show();
            });
            OfflineMode = true;
        }

        Logger.Information("Loading custom versions...");
        foreach (var i in Directory.GetDirectories(
                     FilesManager.Directories.VersionsRoot)) {
            var name = Path.GetFileName(i);
            if (versions.Any(x => x.Id == name))
                continue;
            
            Logger.Information($"Processing {name}");
            try {
                var jsonPath = Path.Combine(i + "\\", $"{name}.json");
                var json = File.ReadAllText(jsonPath);
                dynamic d = JObject.Parse(json);
                if (MojangMainJson.IsMojangJson(d))
                    json = JsonConvert.SerializeObject(
                        BlowaunchMainJson.MojangToBlowaunch(
                            JsonConvert.DeserializeObject
                                <MojangMainJson>(json)));
                var actualJson = JsonConvert.DeserializeObject
                    <BlowaunchMainJson>(json);
                var sb = new StringBuilder();
                switch (actualJson!.Type) {
                    case BlowaunchMainJson.JsonType.release:
                        sb.Append("Release ");
                        break;
                    case BlowaunchMainJson.JsonType.snapshot:
                        if (!Config.ShowSnapshots) continue;
                        sb.Append("Snapshot ");
                        break;
                    case BlowaunchMainJson.JsonType.old_beta:
                        if (!Config.ShowBeta) continue;
                        sb.Append("Beta ");
                        break;
                    case BlowaunchMainJson.JsonType.old_alpha:
                        if (!Config.ShowAlpha) continue;
                        sb.Append("Alpha ");
                        break;
                }

                sb.Append(actualJson.Version);
                var item = new LauncherConfig.VersionClass
                    { Name = sb.ToString(), Id = name };
                versions.Add(item);
            } catch (Exception e) {
                Logger.Error("Unable to load the version! {0}", e);
            }
        }
        
        var index = versions.FindIndex(
            x => x.Id == Config.Version.Id 
                 && x.Name == Config.Version.Name);
        await Dispatcher.UIThread.InvokeAsync(() => {
            _versionsCombo.Items = versions;
            _versionsCombo.SelectedIndex = index;
            if (_selectionChanged) return;
            _versionsCombo.SelectionChanged += (_, e) => {
                if (e.AddedItems.Count == 0) return;
                Config.Version = (e.AddedItems[0] 
                    as LauncherConfig.VersionClass)!;
                SaveConfig();
            };
            _selectionChanged = true;
        });
    }
    #endregion
    #region Configuration
    /// <summary>
    /// Load configuration file
    /// </summary>
    private async Task LoadConfig()
    {
        Logger.Information("Loading configuration...");
        if (!File.Exists("config.json")) {
            Logger.Information("Not found, creating new one");
            /*
            await Dispatcher.UIThread.InvokeAsync(() => {
                var msBoxStandardWindow = MessageBoxManager
                    .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                        Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = "Blowaunch uses it's own " +
                                         "JSON file format, so " +
                                         "you cannot use the Blowaunch " +
                                         "directory in other launcher!",
                        ContentTitle = "Warning!"
                    });
                msBoxStandardWindow.Show();
            });
            */
            SaveConfig();
        } else {
            try {
                Config = JsonConvert.DeserializeObject
                    <LauncherConfig>(File.ReadAllText(
                        "config.json"))!;
            } catch (Exception e) {
                Logger.Error("Unable to load config! {0}", e);
                /*
                await Dispatcher.UIThread.InvokeAsync(() => {
                    var msBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                            Icon = MessageBox.Avalonia.Enums.Icon.Error,
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentMessage = "Blowaunch is unable to load " +
                                             "the configuration file.\n" +
                                             "Settings were not loaded.",
                            ContentTitle = "An error occured!"
                        });
                    msBoxStandardWindow.Show();
                });
                */
            }
        }
    }

    /// <summary>
    /// Save configuration file
    /// </summary>
    private void SaveConfig()
    {
        try { File.WriteAllText("config.json",
            JsonConvert.SerializeObject(
                Config)); }
        catch (Exception e) {
            Logger.Error("Unable to save config! {0}", e);
            /*
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = "Blowaunch is unable to save " +
                                     "the configuration file.\n" +
                                     "Settings would be reset " +
                                     "when you close and open " +
                                     "Blowaunch again.",
                    ContentTitle = "An error occured!"
                });
            msBoxStandardWindow.Show();
            */
        }
    }
    #endregion
    #region Accounts
    private void ValidateAccounts()
    {
        var accounts = new List<Account>();
        foreach (var acc in Config.Accounts) {
            switch (acc.Type) {
                case Account.AuthType.Microsoft:
                    if (DateTime.Now <= acc.ValidUntil) {
                        var clone = acc;
                        try {
                            Library.Authentication.Microsoft.Refresh(ref clone);
                            accounts.Add(clone);
                        } catch (Exception e) {
                            Logger.Warning($"Microsoft account {acc.Name} " +
                                           $"cannot be refreshed, deleting...", e);
                        }
                    } else accounts.Add(acc);
                    break;
                case Account.AuthType.Mojang:
                    if (!Mojang.Validate(acc)) {
                        var clone = acc;
                        try {
                            Mojang.Refresh(ref clone);
                            accounts.Add(clone);
                        } catch (Exception e) {
                            Logger.Warning($"Microsoft account {acc.Name} " +
                                           $"cannot be refreshed, deleting...", e);
                        }
                    } else accounts.Add(acc);
                    break;
                case Account.AuthType.None:
                    accounts.Add(acc);
                    break;
            }
        }

        Config.Accounts = accounts;
        SaveConfig();
    }
    
    /// <summary>
    /// Reload accounts combo
    /// and textblocks
    /// </summary>
    private void ReloadAccounts()
    {
        Logger.Information("Loading accounts...");
        _accountsCombo.Items = Config.Accounts.ToList();
        var account = Config.Accounts.Where(x => 
                x.Id == Config.SelectedAccountId)
            .ToList();
        if (account.Count == 0) {
            _accountName.Text = "[No Account]";
            _accountType.Text = "No authentication";
            return;
        }
        
        _accountsCombo.SelectedItem = account;
        _accountName.Text = account[0].Name;
        switch (account[0].Type) {
            case Account.AuthType.Microsoft:
                _accountType.Text = "Microsoft Account";
                break;
            case Account.AuthType.Mojang:
                _accountType.Text = "Mojang Account";
                break;
            case Account.AuthType.None:
                _accountType.Text = "No authentication";
                break;
        }
    }

    /// <summary>
    /// Delete selected account
    /// </summary>
    public void DeleteAccount(object? sender, RoutedEventArgs e)
    {
        var item = _accountsCombo.SelectedItem as Account;
        if (Config.SelectedAccountId == item!.Id)
            Config.SelectedAccountId = "";
        _accountsCombo.SelectedIndex = 0;
        Config.Accounts.Remove(item);
        SaveConfig(); ReloadAccounts();
    }

    /// <summary>
    /// Select account
    /// </summary>
    public void SelectAccount(object? sender, RoutedEventArgs e)
    {
        if (_accountsCombo.SelectedItem is not Account item) {
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = "Select an account first!",
                    ContentTitle = "Error"
                });
            msBoxStandardWindow.Show();
            return;
        }
        Config.SelectedAccountId = item.Id;
        SaveConfig(); ReloadAccounts();
        _authPanel.IsVisible = false;
    }
    
    /// <summary>
    /// Login into Cracked account
    /// </summary>
    public void CrackedLogin(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Adding cracked account...");
        Config.Accounts.Add(new Account {
           Type = Account.AuthType.None,
           Name = _usernameCracked.Text,
           Id = Guid.NewGuid().ToString()
        });
        Logger.Information("Successfully logged in!");
        SaveConfig(); ReloadAccounts();
        
        var msBoxStandardWindow = MessageBoxManager
            .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                Icon = MessageBox.Avalonia.Enums.Icon.Success,
                ButtonDefinitions = ButtonEnum.Ok,
                ContentMessage = "Successfully created a Cracked account!",
                ContentTitle = "Success"
            });
        msBoxStandardWindow.Show();
    }
    
    /// <summary>
    /// Login into Microsoft account
    /// </summary>
    public void MicrosoftLogin(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Starting Microsoft OAuth2 listener...");
        try {
            Library.Authentication.Microsoft.OpenLoginPage();
            Library.Authentication.Microsoft.StartListener(
                async acc => await Dispatcher.UIThread
                    .InvokeAsync(() => {
                        _microsoftLoginButton.IsVisible = true;
                        _progressPanel.IsVisible = false;
                        _progressInfo.Text = "";
                        acc.Id = Guid.NewGuid().ToString();
                        Config.Accounts.Add(acc);
        
                        Logger.Information("Successfully logged in!");
                        SaveConfig(); ReloadAccounts();
        
                        var msBoxStandardWindow1 = MessageBoxManager
                            .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                                Icon = MessageBox.Avalonia.Enums.Icon.Success,
                                ButtonDefinitions = ButtonEnum.Ok,
                                ContentMessage = "Successfully logged into your Minecraft account!",
                                ContentTitle = "Success"
                            });
                        msBoxStandardWindow1.Show();
                    }), async ex => await Dispatcher.UIThread
                    .InvokeAsync(() => {
                        _microsoftLoginButton.IsVisible = true;
                        _progressPanel.IsVisible = false;
                        _progressInfo.Text = "";
                        var msBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                                Icon = MessageBox.Avalonia.Enums.Icon.Error,
                                ButtonDefinitions = ButtonEnum.Ok,
                                ContentMessage = ex.Message,
                            ContentTitle = ex.GetType().Name
                        });
                        msBoxStandardWindow.Show();
                }), async (str, c) => await Dispatcher.UIThread
                    .InvokeAsync(() => {
                        if (c == -1)
                            _progressBar.IsIndeterminate = true;
                        else {
                            _progressFiles.Text = $"Step {c + 1} out of 5";
                            _progressBar.IsIndeterminate = false;
                            _progressBar.Value = c;
                        }
                        _progressInfo.Text = str;
                    }));
            _microsoftLoginButton.IsVisible = false;
            _progressFiles.Text = "Step 1 out of 5";
            _progressPanel.IsVisible = true;
            _progressBar.Maximum = 4;
        } catch (Exception ex) {
            Logger.Error("An error occured: {0}", ex);
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = ex.Message,
                    ContentTitle = ex.GetType().Name
                });
            msBoxStandardWindow.Show();
        }
    }

    /// <summary>
    /// Login into Mojang account
    /// </summary>
    public void MojangLogin(object? sender, RoutedEventArgs e)
    {
        _mojangLoginButton.IsVisible = false;
        _progressBar.IsIndeterminate = true;
        _progressInfo.Text = "Logging in...";
        _progressFiles.Text = "Step 1 out of 1";
        _progressPanel.IsVisible = true;
        new Thread(async () => {
            try {
                Logger.Information("Adding Mojang account...");
                var account = Mojang.Login(_usernameMojang.Text,
                    _passwordMojang.Text);
                account.Id = Guid.NewGuid().ToString();
                Config.Accounts.Add(account);
            
                Logger.Information("Successfully logged in!");
                await Dispatcher.UIThread.InvokeAsync(() => {
                    SaveConfig(); ReloadAccounts();
                    _progressBar.IsIndeterminate = false;
                    _mojangLoginButton.IsVisible = true;
                    _progressPanel.IsVisible = false;
                    _progressInfo.Text = "";
                    var msBoxStandardWindow1 = MessageBoxManager
                        .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                            Icon = MessageBox.Avalonia.Enums.Icon.Success,
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentMessage = "Successfully logged into your Minecraft account!",
                            ContentTitle = "Success"
                        });
                    msBoxStandardWindow1.Show();
                });
            } catch (Exception ex) {
                Logger.Error("An error occured: {0}", ex);
                await Dispatcher.UIThread.InvokeAsync(() => {
                    _progressBar.IsIndeterminate = false;
                    _mojangLoginButton.IsVisible = true;
                    _progressPanel.IsVisible = false;
                    _progressInfo.Text = "";
                    var msBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandardWindow(new MessageBoxStandardParams {
                            Icon = MessageBox.Avalonia.Enums.Icon.Error,
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentMessage = ex.Message,
                            ContentTitle = ex.GetType().Name
                        });
                    msBoxStandardWindow.Show();
                });
            }
        }).Start();
    }
    #endregion
    #region Settings
    /// <summary>
    /// Show settings in the UI
    /// </summary>
    private void LoadSettings()
    {
        Logger.Information("Loading settings...");
        _customWindowSize.IsChecked = Config.CustomWindowSize;
        _windowWidth.Value = Config.WindowSize.X;
        _windowHeight.Value = Config.WindowSize.Y;
        _javaArguments.Text = Config.JvmArgs;
        _gameArguments.Text = Config.GameArgs;
        _ramManual.Value = int.Parse(Config.RamMax);
        _ramSlider.Value = int.Parse(Config.RamMax);
        _ramSlider.Maximum = _info.MemoryList.Sum(
            x => (long)x.Capacity) / 1000000;
        _showSnaphots.IsChecked = Config.ShowSnapshots;
        _showAlpha.IsChecked = Config.ShowAlpha;
        _showBeta.IsChecked = Config.ShowBeta;
        _forceOffline.IsChecked = Config.ForceOffline;
        _minecraftDemo.IsChecked = Config.DemoUser;

        if (Config.ForceOffline)
            OfflineMode = true;
    }
    
    /// <summary>
    /// Reloads settings
    /// </summary>
    public void RevertChanges(object? sender, RoutedEventArgs e)
        => LoadSettings();

    /// <summary>
    /// Save settings
    /// </summary>
    public void SaveChanges(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Saving settings...");
        Config.CustomWindowSize = _customWindowSize.IsChecked!.Value;
        Config.WindowSize = new Vector2(
            (int)_windowWidth.Value,
            (int)_windowHeight.Value);
        Config.JvmArgs = _javaArguments.Text;
        Config.GameArgs = _gameArguments.Text;
        Config.RamMax = _ramManual.Value.ToString(
            CultureInfo.InvariantCulture);
        Config.ShowSnapshots = _showSnaphots.IsChecked!.Value;
        Config.ShowAlpha = _showAlpha.IsChecked!.Value;
        Config.ShowBeta = _showBeta.IsChecked!.Value;
        Config.ForceOffline = _forceOffline.IsChecked!.Value;
        Config.DemoUser = _minecraftDemo.IsChecked!.Value;
        SaveConfig();
        
        _loadingPanel.IsVisible = true;
        _progressPanel.IsVisible = true;
        _progressBar.IsIndeterminate = true;

        new Thread(async () => {
            await Dispatcher.UIThread.InvokeAsync(() => {
                _progressInfo.Text = "Loading versions...";
                _progressFiles.Text = "Step 1 out of 1";
            });
            await LoadVersions();
            await Dispatcher.UIThread.InvokeAsync(() => {
                _loadingPanel.IsVisible = false;
                _progressPanel.IsVisible = false;
                _progressBar.IsIndeterminate = false;
            });
        }).Start();
    }

    /// <summary>
    /// Reset settings
    /// </summary>
    public void ResetSettings(object? sender, RoutedEventArgs e)
    {
        Logger.Information("Resetting settings...");
        var conf = new LauncherConfig();
        Config.CustomWindowSize = conf.CustomWindowSize;
        Config.WindowSize = conf.WindowSize;
        Config.JvmArgs = conf.JvmArgs;
        Config.GameArgs = conf.GameArgs;
        Config.RamMax = conf.RamMax;
        Config.ShowSnapshots = conf.ShowSnapshots;
        Config.ShowAlpha = conf.ShowAlpha;
        Config.ShowBeta = conf.ShowBeta;
        Config.ForceOffline = conf.ForceOffline;
        Config.DemoUser = conf.DemoUser;
        SaveConfig(); LoadSettings();
    }
    #endregion
    #region Events
    /// <summary>
    /// Close authentication panel
    /// </summary>
    public void CloseAuthPanel(object? sender, RoutedEventArgs e)
        => _authPanel.IsVisible = false;
        
    /// <summary>
    /// Open authentication panel
    /// </summary>
    public void OpenAuthPanel(object? sender, RoutedEventArgs e)
        => _authPanel.IsVisible = true;
    
    /// <summary>
    /// Open root directory
    /// </summary>
    public void OpenDirectory(object? sender, RoutedEventArgs e)
        => Process.Start(new ProcessStartInfo {
            FileName = FilesManager.Directories.Root,
            UseShellExecute = true,
            Verb = "open"
        });
    #endregion
    #region Runner
    /*
    public void RunMinecraft(object? sender, RoutedEventArgs e)
    {
        if (_progressPanel.IsVisible) {
            var msBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandardWindow(new MessageBoxStandardParams{
                    Icon = MessageBox.Avalonia.Enums.Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentMessage = "An operation is active at the time!",
                    ContentTitle = "Error"
                });
            msBoxStandardWindow.Show();
            return;
        }

        _progressPanel.IsVisible = true;
    }
    */
    #endregion
}