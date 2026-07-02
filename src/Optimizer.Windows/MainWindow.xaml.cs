using Microsoft.UI.Windowing;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Optimizer.Shared;
using Optimizer.Shared.Models;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Pages;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Numerics;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Optimizer_Windows;

public sealed partial class MainWindow : Window
{
    private readonly string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
    private readonly List<SearchCommand> _searchCommands = [];
    private bool _initialNavigationApplied;
    private ContainerVisual? _backdropRoot;
    private SpriteVisual? _primaryGlowVisual;
    private SpriteVisual? _secondaryGlowVisual;
    private SpriteVisual? _tertiaryGlowVisual;
    private SpriteVisual? _lineVisualA;
    private SpriteVisual? _lineVisualB;
    private SpriteVisual? _lineVisualC;
    private bool _backdropInitialized;

    public ObservableCollection<SearchCommand> PaletteCommands { get; } = [];

    public MainWindow()
    {
        InitializeComponent();

        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = ElementTheme.Dark;
        }

        ConfigureWindowChrome();
        ResizeAndCenterWindow();
        ApplyLocalization();
        BackdropHost.Loaded += BackdropHost_Loaded;
        BackdropHost.SizeChanged += BackdropHost_SizeChanged;
        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_initialNavigationApplied)
        {
            return;
        }

        _initialNavigationApplied = true;
        NavView.SelectedItem = OverviewNavItem;
        NavigateTo("home");
    }

    private void ConfigureWindowChrome()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppHeaderBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = Color.FromArgb(255, 9, 10, 16);
        titleBar.InactiveBackgroundColor = Color.FromArgb(255, 9, 10, 16);
        titleBar.ForegroundColor = Color.FromArgb(255, 255, 255, 255);
        titleBar.InactiveForegroundColor = Color.FromArgb(255, 169, 175, 188);
        titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonForegroundColor = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 169, 175, 188);
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 26, 11, 16);
        titleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 59, 17, 27);
        titleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 255, 255, 255);
    }

    private void ResizeAndCenterWindow()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
        }

        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = area.WorkArea;
        var width = Math.Min(1540, Math.Max(1220, workArea.Width - 96));
        var height = Math.Min(960, Math.Max(820, workArea.Height - 96));
        var size = new SizeInt32(width, height);
        AppWindow.Resize(size);

        var x = workArea.X + Math.Max(0, (workArea.Width - size.Width) / 2);
        var y = workArea.Y + Math.Max(0, (workArea.Height - size.Height) / 2);
        AppWindow.Move(new PointInt32(x, y));
    }

    private void BackdropHost_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureBackdropEffects();
    }

    private void BackdropHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBackdropLayout();
    }

    private void ApplyLocalization()
    {
        Title = AppText.Get("AppTitle");
        AppHeaderTitleText.Text = AppText.Get("AppTitle");
        AppHeaderVersionText.Text = $"v{_appVersion}";
        OverviewNavItem.Content = AppText.Get("NavOverview");
        OptimizeNavItem.Content = AppText.Get("NavOptimize");
        RestoreNavItem.Content = AppText.Get("NavRestore");
        SecurityNavItem.Content = AppText.Get("NavSecurity");
        CleanupNavItem.Content = AppText.Get("NavCleanup");
        HardwareNavItem.Content = AppText.Get("NavHardware");
        FeaturesNavItem.Content = AppText.Get("NavFeatures");
        WslNavItem.Content = AppText.Get("NavWsl");
        UpdatesNavItem.Content = AppText.Get("NavUpdates");
        OfficeNavItem.Content = AppText.Get("NavOffice");
        AppxNavItem.Content = AppText.Get("NavAppx");
        TroubleshootNavItem.Content = AppText.Get("NavTroubleshoot");
        AboutNavItem.Content = AppText.Get("NavAbout");
        CurrentSectionText.Text = AppText.Get("NavOverview");
        OpenCommandPaletteLabelText.Text = AppText.Get("SearchPaletteOpen");
        SearchShortcutText.Text = AppText.Get("SearchShortcut");
        CommandPaletteTitleText.Text = AppText.Get("SearchPaletteTitle");
        CommandPaletteHintText.Text = AppText.Get("SearchPaletteHint");
        CommandPaletteBox.PlaceholderText = AppText.Get("SearchPlaceholder");
        RebuildSearchCommands();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            NavigateTo(item.Tag?.ToString() ?? "home");
        }
    }

    private void NavigateTo(string tag)
    {
        switch (tag)
        {
            case "home":
                CurrentSectionText.Text = AppText.Get("NavOverview");
                NavFrame.Navigate(typeof(HomePage));
                break;
            case "optimize":
                CurrentSectionText.Text = AppText.Get("NavOptimize");
                NavFrame.Navigate(typeof(OptimizePage));
                break;
            case "cleanup":
                CurrentSectionText.Text = AppText.Get("NavCleanup");
                NavFrame.Navigate(typeof(CleanupPage));
                break;
            case "restore":
                CurrentSectionText.Text = AppText.Get("NavRestore");
                NavFrame.Navigate(typeof(RestorePage));
                break;
            case "security":
                CurrentSectionText.Text = AppText.Get("NavSecurity");
                NavFrame.Navigate(typeof(SecurityPage));
                break;
            case "hardware":
                CurrentSectionText.Text = AppText.Get("NavHardware");
                NavFrame.Navigate(typeof(HardwarePage));
                break;
            case "features":
                CurrentSectionText.Text = AppText.Get("NavFeatures");
                NavFrame.Navigate(typeof(FeaturesPage));
                break;
            case "wsl":
                CurrentSectionText.Text = AppText.Get("NavWsl");
                NavFrame.Navigate(typeof(WslPage));
                break;
            case "updates":
                CurrentSectionText.Text = AppText.Get("NavUpdates");
                NavFrame.Navigate(typeof(SettingsPage));
                break;
            case "office":
                CurrentSectionText.Text = AppText.Get("NavOffice");
                NavFrame.Navigate(typeof(OfficePage));
                break;
            case "appx":
                CurrentSectionText.Text = AppText.Get("NavAppx");
                NavFrame.Navigate(typeof(AppxPage));
                break;
            case "troubleshoot":
                CurrentSectionText.Text = AppText.Get("NavTroubleshoot");
                NavFrame.Navigate(typeof(TroubleshootingPage));
                break;
            case "about":
                CurrentSectionText.Text = AppText.Get("NavAbout");
                NavFrame.Navigate(typeof(AboutPage));
                break;
            default:
                throw new InvalidOperationException($"Unknown navigation item tag: {tag}");
        }
    }

    private void RebuildSearchCommands()
    {
        _searchCommands.Clear();

        AddPageSearchCommand("home", AppText.Get("NavOverview"), "overview home dashboard status");
        AddPageSearchCommand("optimize", AppText.Get("NavOptimize"), "optimize tuning startup storage cleanup");
        AddPageSearchCommand("restore", AppText.Get("NavRestore"), "restore config backup import export");
        AddPageSearchCommand("security", AppText.Get("NavSecurity"), "security defender firewall protection");
        AddPageSearchCommand("cleanup", AppText.Get("NavCleanup"), "cleanup cache junk temp");
        AddPageSearchCommand("hardware", AppText.Get("NavHardware"), "hardware cpu gpu memory disk network");
        AddPageSearchCommand("features", AppText.Get("NavFeatures"), "windows features optional feature wsl hyper-v sandbox smb telnet dotnet");
        AddPageSearchCommand("wsl", AppText.Get("NavWsl"), "wsl linux ubuntu docker distro shell terminal backend frontend gamedev install update shutdown");
        AddPageSearchCommand("updates", AppText.Get("NavUpdates"), "updates update startup release mirror");
        AddPageSearchCommand("office", AppText.Get("NavOffice"), "office microsoft 365 visio project");
        AddPageSearchCommand("appx", AppText.Get("NavAppx"), "appx package uninstall store");
        AddPageSearchCommand("troubleshoot", AppText.Get("NavTroubleshoot"), "troubleshoot services scheduled tasks");
        AddPageSearchCommand("about", AppText.Get("NavAbout"), "about docs roadmap version");

        AddActionSearchCommand("storage-sense", "optimize", "ms-settings:storagepolicies", "storage cleanup auto sense");
        AddActionSearchCommand("disk-cleanup", "optimize", "cleanmgr.exe", "disk cleanup wizard cleanmgr");
        AddActionSearchCommand("temp-folder", "optimize", Path.GetTempPath(), "temp folder cache files");
        AddActionSearchCommand("startup-apps", "optimize", "ms-settings:startupapps", "startup boot autorun apps");
        AddActionSearchCommand("task-manager", "optimize", "taskmgr.exe", "task manager processes cpu memory");
        AddActionSearchCommand("windows-update", "optimize", "ms-settings:windowsupdate", "windows update patch");
        AddActionSearchCommand("power-settings", "optimize", "ms-settings:powersleep", "power sleep thermal battery");
        AddNavigationSearchCommand("security-realtime", "security", AppText.Get("SecurityRealtimeSwitch"), $"{AppText.Get("SecurityRealtime")} defender realtime toggle switch");
        AddNavigationSearchCommand("security-domain", "security", AppText.Get("SecurityDomain"), $"{AppText.Get("SecurityDomain")} firewall toggle switch");
        AddNavigationSearchCommand("security-private", "security", AppText.Get("SecurityPrivate"), $"{AppText.Get("SecurityPrivate")} firewall toggle switch");
        AddNavigationSearchCommand("security-public", "security", AppText.Get("SecurityPublic"), $"{AppText.Get("SecurityPublic")} firewall toggle switch");
        AddNavigationSearchCommand("feature-developer-mode", "features", AppText.Get("FeatureDeveloperModeTitle"), "developer mode frontend backend game development toggle search");
        AddNavigationSearchCommand("feature-long-paths", "features", AppText.Get("FeatureLongPathsTitle"), "long paths npm node dotnet unreal unity frontend backend game development toggle search");
        AddNavigationSearchCommand("feature-game-mode", "features", AppText.Get("FeatureGameModeTitle"), "game mode player fps latency performance gamer toggle search");
        AddNavigationSearchCommand("feature-wsl", "features", AppText.Get("FeatureWslTitle"), "wsl linux frontend backend game development toggle search");
        AddNavigationSearchCommand("feature-hyperv", "features", AppText.Get("FeatureHyperVTitle"), "hyper-v backend virtual machine game development toggle search");
        AddNavigationSearchCommand("feature-sandbox", "features", AppText.Get("FeatureSandboxTitle"), "sandbox unknown app isolated frontend backend gamer toggle search");
        AddNavigationSearchCommand("feature-directplay", "features", AppText.Get("FeatureDirectPlayTitle"), "directplay legacy game player toggle search");
        AddNavigationSearchCommand("feature-smb1", "features", AppText.Get("FeatureSmb1Title"), "smb1 cifs backend legacy nas gamer toggle search");
        AddNavigationSearchCommand("cleanup-scan", "cleanup", AppText.Get("CleanupStartScan"), "cleanup scan analyze cache junk temp async");
        AddNavigationSearchCommand("wsl-install", "wsl", AppText.Get("WslInstall"), "wsl install setup linux ubuntu debian docker");
        AddNavigationSearchCommand("wsl-update", "wsl", AppText.Get("WslUpdate"), "wsl update kernel refresh linux");
        AddNavigationSearchCommand("wsl-shutdown", "wsl", AppText.Get("WslShutdown"), "wsl shutdown stop distros virtual machine");
    }

    private void AddPageSearchCommand(string targetTag, string title, string keywords)
    {
        _searchCommands.Add(new SearchCommand
        {
            Id = $"nav-{targetTag}",
            Title = title,
            Description = AppText.Get("SearchSectionDescription"),
            TargetTag = targetTag,
            Keywords = keywords
        });
    }

    private void AddActionSearchCommand(string operationId, string targetTag, string launchTarget, string keywords)
    {
        var operation = OperationCatalog.Find(operationId);
        if (operation is null)
        {
            return;
        }

        _searchCommands.Add(new SearchCommand
        {
            Id = operation.Id,
            Title = GetLocalizedOperationTitle(operation.Id),
            Description = $"{GetRiskText(operation.RiskLevel)} · {AppText.Get("SearchActionDescription")}",
            TargetTag = targetTag,
            LaunchTarget = launchTarget,
            Keywords = $"{keywords} {operation.Id} {operation.Category}"
        });
    }

    private void AddNavigationSearchCommand(string id, string targetTag, string title, string keywords)
    {
        _searchCommands.Add(new SearchCommand
        {
            Id = id,
            Title = title,
            Description = AppText.Get("SearchSectionDescription"),
            TargetTag = targetTag,
            Keywords = keywords
        });
    }

    private string GetLocalizedOperationTitle(string operationId)
    {
        return operationId switch
        {
            "storage-sense" => AppText.Get("OptimizeStorageSenseTitle"),
            "disk-cleanup" => AppText.Get("OptimizeDiskCleanupTitle"),
            "temp-folder" => AppText.Get("OptimizeOptionTempTitle"),
            "startup-apps" => AppText.Get("OptimizeStartupTitle"),
            "task-manager" => AppText.Get("OptimizeTaskManagerTitle"),
            "windows-update" => AppText.Get("OptimizeWindowsUpdateTitle"),
            "power-settings" => AppText.Get("OptimizePowerTitle"),
            _ => operationId
        };
    }

    private string GetRiskText(OperationRiskLevel riskLevel)
    {
        return riskLevel switch
        {
            OperationRiskLevel.Low => AppText.Get("SearchRiskLow"),
            OperationRiskLevel.Medium => AppText.Get("SearchRiskMedium"),
            OperationRiskLevel.High => AppText.Get("SearchRiskHigh"),
            _ => AppText.Get("CommonUnknown")
        };
    }

    private IEnumerable<SearchCommand> GetMatchingCommands(string rawQuery)
    {
        var query = rawQuery.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return _searchCommands.Take(12);
        }

        return _searchCommands
            .Where(command =>
                command.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                command.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                command.Keywords.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(12);
    }

    private void UpdatePaletteResults(string rawQuery)
    {
        var matches = GetMatchingCommands(rawQuery).ToArray();
        PaletteCommands.Clear();

        if (matches.Length == 0)
        {
            PaletteCommands.Add(new SearchCommand
            {
                Id = "no-results",
                Title = AppText.Get("SearchNoResults"),
                Description = string.Empty
            });
            CommandPaletteList.SelectedIndex = 0;
            return;
        }

        foreach (var match in matches)
        {
            PaletteCommands.Add(match);
        }

        CommandPaletteList.SelectedIndex = 0;
    }

    private void ExecuteSearchCommand(SearchCommand command)
    {
        if (command.Id == "no-results")
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(command.TargetTag))
        {
            SelectNavigationItem(command.TargetTag);
        }

        if (!string.IsNullOrWhiteSpace(command.LaunchTarget))
        {
            Launch(command.LaunchTarget);
        }

        HideCommandPalette();
    }

    private void SelectNavigationItem(string tag)
    {
        var item = GetNavigationItems().FirstOrDefault(candidate =>
            string.Equals(candidate.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));

        if (item is not null)
        {
            NavView.SelectedItem = item;
            return;
        }

        NavigateTo(tag);
    }

    private IReadOnlyList<NavigationViewItem> GetNavigationItems()
    {
        return
        [
            OverviewNavItem,
            OptimizeNavItem,
            RestoreNavItem,
            SecurityNavItem,
            CleanupNavItem,
            HardwareNavItem,
            FeaturesNavItem,
            WslNavItem,
            UpdatesNavItem,
            OfficeNavItem,
            AppxNavItem,
            TroubleshootNavItem,
            AboutNavItem
        ];
    }

    private void OpenCommandPaletteButton_Click(object sender, RoutedEventArgs e)
    {
        ShowCommandPalette();
    }

    private void OpenCommandPaletteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ShowCommandPalette();
        args.Handled = true;
    }

    private void CloseCommandPaletteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (CommandPaletteOverlay.Visibility == Visibility.Visible)
        {
            HideCommandPalette();
            args.Handled = true;
        }
    }

    private void ShowCommandPalette()
    {
        CommandPaletteOverlay.Visibility = Visibility.Visible;
        CommandPaletteBox.Text = string.Empty;
        UpdatePaletteResults(string.Empty);

        DispatcherQueue.TryEnqueue(() =>
        {
            CommandPaletteBox.Focus(FocusState.Programmatic);
            CommandPaletteBox.SelectAll();
        });
    }

    private void HideCommandPalette()
    {
        CommandPaletteOverlay.Visibility = Visibility.Collapsed;
        CommandPaletteBox.Text = string.Empty;
        PaletteCommands.Clear();
    }

    private void CommandPaletteBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePaletteResults(CommandPaletteBox.Text);
    }

    private void CommandPaletteBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Down)
        {
            MovePaletteSelection(1);
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Up)
        {
            MovePaletteSelection(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Enter)
        {
            if (CommandPaletteList.SelectedItem is SearchCommand command)
            {
                ExecuteSearchCommand(command);
            }
            else if (PaletteCommands.FirstOrDefault() is SearchCommand fallback)
            {
                ExecuteSearchCommand(fallback);
            }

            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Escape)
        {
            HideCommandPalette();
            e.Handled = true;
        }
    }

    private void CommandPaletteList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && CommandPaletteList.SelectedItem is SearchCommand command)
        {
            ExecuteSearchCommand(command);
            e.Handled = true;
            return;
        }

        if (e.Key == VirtualKey.Escape)
        {
            HideCommandPalette();
            e.Handled = true;
        }
    }

    private void CommandPaletteList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SearchCommand command)
        {
            ExecuteSearchCommand(command);
        }
    }

    private void MovePaletteSelection(int delta)
    {
        if (PaletteCommands.Count == 0)
        {
            return;
        }

        var nextIndex = CommandPaletteList.SelectedIndex + delta;
        if (nextIndex < 0)
        {
            nextIndex = PaletteCommands.Count - 1;
        }
        else if (nextIndex >= PaletteCommands.Count)
        {
            nextIndex = 0;
        }

        CommandPaletteList.SelectedIndex = nextIndex;
        if (CommandPaletteList.SelectedItem is not null)
        {
            CommandPaletteList.ScrollIntoView(CommandPaletteList.SelectedItem);
        }
    }

    private void Launch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void EnsureBackdropEffects()
    {
        if (_backdropInitialized || BackdropHost.ActualWidth <= 0 || BackdropHost.ActualHeight <= 0)
        {
            return;
        }

        var compositor = ElementCompositionPreview.GetElementVisual(BackdropHost).Compositor;
        _backdropRoot = compositor.CreateContainerVisual();
        ElementCompositionPreview.SetElementChildVisual(BackdropHost, _backdropRoot);

        _primaryGlowVisual = CreateGlowVisual(compositor,
            Color.FromArgb(28, 255, 58, 95),
            Color.FromArgb(0, 255, 58, 95));
        _secondaryGlowVisual = CreateGlowVisual(compositor,
            Color.FromArgb(20, 82, 150, 255),
            Color.FromArgb(0, 82, 150, 255));
        _tertiaryGlowVisual = CreateGlowVisual(compositor,
            Color.FromArgb(18, 137, 42, 255),
            Color.FromArgb(0, 137, 42, 255));
        _lineVisualA = CreateBeamVisual(compositor,
            Color.FromArgb(0, 255, 100, 125),
            Color.FromArgb(150, 255, 102, 142),
            Color.FromArgb(0, 255, 100, 125));
        _lineVisualB = CreateBeamVisual(compositor,
            Color.FromArgb(0, 84, 168, 255),
            Color.FromArgb(110, 112, 182, 255),
            Color.FromArgb(0, 84, 168, 255));
        _lineVisualC = CreateBeamVisual(compositor,
            Color.FromArgb(0, 118, 58, 255),
            Color.FromArgb(86, 131, 77, 255),
            Color.FromArgb(0, 118, 58, 255));

        _backdropRoot.Children.InsertAtBottom(_primaryGlowVisual);
        _backdropRoot.Children.InsertAtBottom(_secondaryGlowVisual);
        _backdropRoot.Children.InsertAtBottom(_tertiaryGlowVisual);
        _backdropRoot.Children.InsertAtTop(_lineVisualA);
        _backdropRoot.Children.InsertAtTop(_lineVisualB);
        _backdropRoot.Children.InsertAtTop(_lineVisualC);

        StartBackdropAnimations(compositor);
        _backdropInitialized = true;
        UpdateBackdropLayout();
    }

    private static SpriteVisual CreateGlowVisual(Compositor compositor, Color innerColor, Color outerColor)
    {
        var brush = compositor.CreateRadialGradientBrush();
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, innerColor));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.45f, Color.FromArgb((byte)(innerColor.A / 2), innerColor.R, innerColor.G, innerColor.B)));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, outerColor));
        brush.EllipseCenter = new Vector2(0.5f, 0.5f);
        brush.EllipseRadius = new Vector2(0.5f, 0.5f);

        var visual = compositor.CreateSpriteVisual();
        visual.Brush = brush;
        visual.Opacity = 1f;
        return visual;
    }

    private static SpriteVisual CreateBeamVisual(Compositor compositor, Color startColor, Color centerColor, Color endColor)
    {
        var brush = compositor.CreateLinearGradientBrush();
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, startColor));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(0.5f, centerColor));
        brush.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, endColor));
        brush.StartPoint = new Vector2(0, 0.5f);
        brush.EndPoint = new Vector2(1, 0.5f);

        var visual = compositor.CreateSpriteVisual();
        visual.Brush = brush;
        visual.Opacity = 0.26f;
        return visual;
    }

    private void StartBackdropAnimations(Compositor compositor)
    {
        if (_primaryGlowVisual is not null)
        {
            var primaryAnimation = compositor.CreateVector3KeyFrameAnimation();
            primaryAnimation.InsertKeyFrame(0.0f, new Vector3(0.92f, 0.92f, 1f));
            primaryAnimation.InsertKeyFrame(0.5f, new Vector3(1.08f, 1.08f, 1f));
            primaryAnimation.InsertKeyFrame(1.0f, new Vector3(0.92f, 0.92f, 1f));
            primaryAnimation.Duration = TimeSpan.FromSeconds(9);
            primaryAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            _primaryGlowVisual.CenterPoint = new Vector3(320, 320, 0);
            _primaryGlowVisual.StartAnimation(nameof(SpriteVisual.Scale), primaryAnimation);
        }

        if (_secondaryGlowVisual is not null)
        {
            var secondaryAnimation = compositor.CreateVector3KeyFrameAnimation();
            secondaryAnimation.InsertKeyFrame(0.0f, new Vector3(1.04f, 1.04f, 1f));
            secondaryAnimation.InsertKeyFrame(0.5f, new Vector3(0.95f, 0.95f, 1f));
            secondaryAnimation.InsertKeyFrame(1.0f, new Vector3(1.04f, 1.04f, 1f));
            secondaryAnimation.Duration = TimeSpan.FromSeconds(11);
            secondaryAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            _secondaryGlowVisual.CenterPoint = new Vector3(360, 360, 0);
            _secondaryGlowVisual.StartAnimation(nameof(SpriteVisual.Scale), secondaryAnimation);
        }

        if (_tertiaryGlowVisual is not null)
        {
            var tertiaryAnimation = compositor.CreateVector3KeyFrameAnimation();
            tertiaryAnimation.InsertKeyFrame(0.0f, new Vector3(1f, 1f, 1f));
            tertiaryAnimation.InsertKeyFrame(0.5f, new Vector3(1.12f, 1.08f, 1f));
            tertiaryAnimation.InsertKeyFrame(1.0f, new Vector3(1f, 1f, 1f));
            tertiaryAnimation.Duration = TimeSpan.FromSeconds(13);
            tertiaryAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            _tertiaryGlowVisual.CenterPoint = new Vector3(240, 240, 0);
            _tertiaryGlowVisual.StartAnimation(nameof(SpriteVisual.Scale), tertiaryAnimation);

            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.InsertKeyFrame(0.0f, 0.22f);
            opacityAnimation.InsertKeyFrame(0.5f, 0.35f);
            opacityAnimation.InsertKeyFrame(1.0f, 0.22f);
            opacityAnimation.Duration = TimeSpan.FromSeconds(8.5);
            opacityAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
            _tertiaryGlowVisual.StartAnimation(nameof(SpriteVisual.Opacity), opacityAnimation);
        }

        if (_lineVisualA is not null)
        {
            var driftA = compositor.CreateScalarKeyFrameAnimation();
            driftA.InsertKeyFrame(0.0f, -12f);
            driftA.InsertKeyFrame(0.5f, 10f);
            driftA.InsertKeyFrame(1.0f, -12f);
            driftA.Duration = TimeSpan.FromSeconds(7.5);
            driftA.IterationBehavior = AnimationIterationBehavior.Forever;
            _lineVisualA.StartAnimation("RotationAngleInDegrees", driftA);
        }

        if (_lineVisualB is not null)
        {
            var driftB = compositor.CreateScalarKeyFrameAnimation();
            driftB.InsertKeyFrame(0.0f, -16f);
            driftB.InsertKeyFrame(0.5f, -8f);
            driftB.InsertKeyFrame(1.0f, -16f);
            driftB.Duration = TimeSpan.FromSeconds(10);
            driftB.IterationBehavior = AnimationIterationBehavior.Forever;
            _lineVisualB.StartAnimation("RotationAngleInDegrees", driftB);
        }

        if (_lineVisualC is not null)
        {
            var driftC = compositor.CreateScalarKeyFrameAnimation();
            driftC.InsertKeyFrame(0.0f, 18f);
            driftC.InsertKeyFrame(0.5f, 10f);
            driftC.InsertKeyFrame(1.0f, 18f);
            driftC.Duration = TimeSpan.FromSeconds(12);
            driftC.IterationBehavior = AnimationIterationBehavior.Forever;
            _lineVisualC.StartAnimation("RotationAngleInDegrees", driftC);
        }
    }

    private void UpdateBackdropLayout()
    {
        if (_backdropRoot is null || BackdropHost.ActualWidth <= 0 || BackdropHost.ActualHeight <= 0)
        {
            return;
        }

        var width = (float)BackdropHost.ActualWidth;
        var height = (float)BackdropHost.ActualHeight;

        _backdropRoot.Size = new Vector2(width, height);

        if (_primaryGlowVisual is not null)
        {
            _primaryGlowVisual.Size = new Vector2(width * 0.52f, height * 0.72f);
            _primaryGlowVisual.Offset = new Vector3(-width * 0.12f, height * 0.18f, 0);
            _primaryGlowVisual.CenterPoint = new Vector3(_primaryGlowVisual.Size.X / 2, _primaryGlowVisual.Size.Y / 2, 0);
        }

        if (_secondaryGlowVisual is not null)
        {
            _secondaryGlowVisual.Size = new Vector2(width * 0.6f, height * 0.74f);
            _secondaryGlowVisual.Offset = new Vector3(width * 0.62f, -height * 0.14f, 0);
            _secondaryGlowVisual.CenterPoint = new Vector3(_secondaryGlowVisual.Size.X / 2, _secondaryGlowVisual.Size.Y / 2, 0);
        }

        if (_tertiaryGlowVisual is not null)
        {
            _tertiaryGlowVisual.Size = new Vector2(width * 0.38f, height * 0.44f);
            _tertiaryGlowVisual.Offset = new Vector3(width * 0.34f, height * 0.54f, 0);
            _tertiaryGlowVisual.CenterPoint = new Vector3(_tertiaryGlowVisual.Size.X / 2, _tertiaryGlowVisual.Size.Y / 2, 0);
        }

        if (_lineVisualA is not null)
        {
            _lineVisualA.Size = new Vector2(width * 0.82f, 2f);
            _lineVisualA.Offset = new Vector3(width * 0.18f, 92f, 0);
            _lineVisualA.CenterPoint = new Vector3(_lineVisualA.Size.X / 2, 1f, 0);
        }

        if (_lineVisualB is not null)
        {
            _lineVisualB.Size = new Vector2(width * 0.66f, 2f);
            _lineVisualB.Offset = new Vector3(width * 0.44f, height * 0.68f, 0);
            _lineVisualB.CenterPoint = new Vector3(_lineVisualB.Size.X / 2, 1f, 0);
        }

        if (_lineVisualC is not null)
        {
            _lineVisualC.Size = new Vector2(width * 0.48f, 2f);
            _lineVisualC.Offset = new Vector3(width * 0.06f, height * 0.82f, 0);
            _lineVisualC.CenterPoint = new Vector3(_lineVisualC.Size.X / 2, 1f, 0);
        }
    }
}
