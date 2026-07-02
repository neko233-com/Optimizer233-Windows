using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Diagnostics;

namespace Optimizer_Windows.Pages;

public sealed partial class HomePage : Page
{
    private readonly WindowsFeatureService _featureService = WindowsFeatureService.Instance;
    private readonly SecurityService _securityService = SecurityService.Instance;
    private readonly StartupService _startupService = StartupService.Instance;
    private readonly SystemSnapshotService _snapshotService = SystemSnapshotService.Instance;
    private CancellationTokenSource? _refreshCts;

    public HomePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        HeroEyebrowText.Text = AppText.Get("OverviewHeroEyebrow");
        WelcomeTitleText.Text = AppText.Get("OverviewTitle");
        WelcomeSubtitleText.Text = AppText.Get("OverviewSubtitle");
        RecommendationTitleText.Text = AppText.Get("OverviewRecommendationsTitle");
        RefreshButton.Content = AppText.Get("OverviewStatusRefresh");
        DashboardHintText.Text = AppText.Get("OverviewDashboardHint");

        DeviceLabelText.Text = AppText.Get("OverviewDevice");
        WindowsLabelText.Text = AppText.Get("OverviewWindows");
        MemoryLabelText.Text = AppText.Get("OverviewMemory");
        StorageLabelText.Text = AppText.Get("OverviewStorage");
        SecurityLabelText.Text = AppText.Get("OverviewSecurity");

        HealthTitleText.Text = AppText.Get("OverviewHealthTitle");
        CpuHealthLabelText.Text = AppText.Get("OverviewProcessor");
        MemoryHealthLabelText.Text = AppText.Get("OverviewHealthMemory");
        StorageHealthLabelText.Text = AppText.Get("OverviewHealthStorage");
        ConfigScoreLabelText.Text = AppText.Get("OverviewConfigScore");

        ActionsTitleText.Text = AppText.Get("OverviewActionsTitle");
        ActionsHintText.Text = AppText.Get("OverviewDashboardHint");
        OpenCleanupButton.Content = AppText.Get("OverviewActionCleanup");
        OpenSecurityButton.Content = AppText.Get("OverviewActionSecurity");
        OpenUpdatesButton.Content = AppText.Get("OverviewActionUpdates");
        OpenHardwareButton.Content = AppText.Get("OverviewActionHardware");
        OpenStorageSenseButton.Content = AppText.Get("OverviewActionStorageSense");
        OpenTaskManagerButton.Content = AppText.Get("OverviewActionTaskManager");
        OpenWindowsUpdateButton.Content = AppText.Get("OverviewActionWindowsUpdate");
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshSnapshotAsync();
    }

    private async void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshSnapshotAsync();
    }

    private async Task RefreshSnapshotAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var cancellationToken = _refreshCts.Token;

        try
        {
            RefreshButton.IsEnabled = false;
            SnapshotText.Text = AppText.Get("StatusCollectingTelemetry");
            RecommendationBodyText.Text = AppText.Get("StatusCollectingTelemetry");

            var snapshotTask = _snapshotService.CaptureAsync(cancellationToken);
            var securityTask = _securityService.GetStatusAsync(cancellationToken);
            var featureTask = TryGetFeaturesAsync(cancellationToken);
            await Task.WhenAll(snapshotTask, securityTask, featureTask);

            var snapshot = await snapshotTask;
            var security = await securityTask;
            var features = await featureTask;
            cancellationToken.ThrowIfCancellationRequested();

            DeviceValueText.Text = snapshot.DeviceName;
            DeviceHintText.Text = snapshot.DeviceModel;

            WindowsValueText.Text = snapshot.WindowsVersion;
            WindowsHintText.Text = snapshot.BootTime;

            MemoryValueText.Text = $"{snapshot.MemoryUsagePercent:0.#}%";
            MemoryPressureBar.Value = snapshot.MemoryUsagePercent;
            MemoryHintText.Text = snapshot.MemorySummary;

            var storagePressure = GetMaxStorageUsage(snapshot);
            StorageValueText.Text = $"{storagePressure}%";
            StoragePressureBar.Value = storagePressure;
            StorageHintText.Text = snapshot.StorageSummary;

            SecurityValueText.Text = security.RealTimeProtectionEnabled ? AppText.Get("SecurityOn") : AppText.Get("SecurityOff");
            SecurityHintText.Text = $"{AppText.Get("SecurityFirewall")}: {GetFirewallSummary(security)}";

            BootText.Text = snapshot.BootTime;
            NetworkText.Text = $"{AppText.Get("OverviewNetwork")}: {snapshot.NetworkSummary}";

            CpuHealthValueText.Text = snapshot.ProcessorName;
            MemoryHealthValueText.Text = snapshot.MemorySummary;
            StorageHealthValueText.Text = snapshot.StorageSummary;
            MemoryHealthBar.Value = snapshot.MemoryUsagePercent;
            StorageHealthBar.Value = storagePressure;

            var configScore = BuildConfigurationScore(features, snapshot, security, storagePressure);
            ConfigScoreValueText.Text = configScore.OverallText;
            ConfigScoreHintText.Text = configScore.Summary;

            RecommendationBodyText.Text = $"{BuildRecommendation(snapshot, security)} {configScore.CompactHint}";
            SnapshotText.Text = $"{snapshot.DeviceName} | {snapshot.ProcessorName} | {snapshot.InputSummary} | {AppText.Get("OverviewConfigScore")} {configScore.Overall}/100 | Startup {(_startupService.IsEnabled() ? AppText.Get("SecurityOn") : AppText.Get("SecurityOff"))}";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SnapshotText.Text = AppText.Format("StatusLoadFailed", ex.Message);
            RecommendationBodyText.Text = AppText.Format("StatusLoadFailed", ex.Message);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task<IReadOnlyList<WindowsFeatureItem>> TryGetFeaturesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _featureService.GetFeaturesAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }

    private string BuildRecommendation(SystemSnapshot snapshot, SecurityStatusInfo security)
    {
        if (!security.RealTimeProtectionEnabled || (!security.FirewallDomainEnabled && !security.FirewallPrivateEnabled && !security.FirewallPublicEnabled))
        {
            return AppText.Get("OverviewRecommendationsSecurity");
        }

        if (snapshot.MemoryUsagePercent >= 75)
        {
            return AppText.Get("OverviewRecommendationsMemory");
        }

        if (GetMaxStorageUsage(snapshot) >= 80)
        {
            return AppText.Get("OverviewRecommendationsStorage");
        }

        return AppText.Get("OverviewRecommendationsUpdate");
    }

    private static int GetMaxStorageUsage(SystemSnapshot snapshot)
    {
        var max = 0;
        foreach (var detail in snapshot.StorageDetails)
        {
            var start = detail.Value.LastIndexOf('(');
            var end = detail.Value.LastIndexOf("%)");
            if (start < 0 || end <= start)
            {
                continue;
            }

            var raw = detail.Value[(start + 1)..end];
            if (int.TryParse(raw, out var value))
            {
                max = Math.Max(max, value);
            }
        }

        return max;
    }

    private string GetFirewallSummary(SecurityStatusInfo security)
    {
        var enabled = new[]
        {
            security.FirewallDomainEnabled,
            security.FirewallPrivateEnabled,
            security.FirewallPublicEnabled
        }.Count(value => value);

        return $"{enabled}/3";
    }

    private ConfigurationScoreSnapshot BuildConfigurationScore(IReadOnlyList<WindowsFeatureItem> features, SystemSnapshot snapshot, SecurityStatusInfo security, int storagePressure)
    {
        var frontend = 48;
        frontend += ScoreFeature(features, "DeveloperMode", 18);
        frontend += ScoreFeature(features, "LongPaths", 18);
        frontend += ScoreFeature(features, "Microsoft-Windows-Subsystem-Linux", 10);
        frontend += snapshot.MemoryUsagePercent < 85 ? 4 : 0;
        frontend += security.RealTimeProtectionEnabled ? 2 : 0;

        var backend = 45;
        backend += ScoreFeature(features, "DeveloperMode", 14);
        backend += ScoreFeature(features, "LongPaths", 14);
        backend += ScoreFeature(features, "Microsoft-Windows-Subsystem-Linux", 12);
        backend += ScoreFeature(features, "VirtualMachinePlatform", 10);
        backend += ScoreFeature(features, "Microsoft-Hyper-V-All", 8);
        backend += security.RealTimeProtectionEnabled ? 4 : 0;

        var gameDev = 44;
        gameDev += ScoreFeature(features, "DeveloperMode", 16);
        gameDev += ScoreFeature(features, "LongPaths", 14);
        gameDev += ScoreFeature(features, "GameMode", 10);
        gameDev += ScoreFeature(features, "VirtualMachinePlatform", 8);
        gameDev += ScoreFeature(features, "NetFx3", 4);
        gameDev += storagePressure < 85 ? 4 : 0;

        var gamer = 50;
        gamer += ScoreFeature(features, "GameMode", 18);
        gamer += ScoreFeature(features, "DirectPlay", 6);
        gamer += ScoreFeature(features, "NetFx3", 6);
        gamer += snapshot.MemoryUsagePercent < 85 ? 8 : 0;
        gamer += storagePressure < 85 ? 6 : 0;
        gamer += security.RealTimeProtectionEnabled ? 6 : 0;

        frontend = Math.Min(100, frontend);
        backend = Math.Min(100, backend);
        gameDev = Math.Min(100, gameDev);
        gamer = Math.Min(100, gamer);

        var overall = (int)Math.Round((frontend + backend + gameDev + gamer) / 4d);
        var summary = AppText.Format("OverviewConfigScoreSummary",
            AppText.Get("OverviewPersonaFrontend"), frontend,
            AppText.Get("OverviewPersonaBackend"), backend,
            AppText.Get("OverviewPersonaGameDev"), gameDev,
            AppText.Get("OverviewPersonaGamer"), gamer);

        return new ConfigurationScoreSnapshot(
            overall,
            $"{overall}/100",
            summary,
            AppText.Format("OverviewConfigScoreCompact", overall));
    }

    private static int ScoreFeature(IReadOnlyList<WindowsFeatureItem> features, string id, int score)
    {
        return features.Any(feature => string.Equals(feature.Id, id, StringComparison.OrdinalIgnoreCase) && feature.IsEnabled)
            ? score
            : 0;
    }

    private sealed record ConfigurationScoreSnapshot(int Overall, string OverallText, string Summary, string CompactHint);

    private void OpenCleanupButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Frame.Navigate(typeof(CleanupPage));

    private void OpenSecurityButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Frame.Navigate(typeof(SecurityPage));

    private void OpenUpdatesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Frame.Navigate(typeof(SettingsPage));

    private void OpenHardwareButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Frame.Navigate(typeof(HardwarePage));

    private void OpenStorageSenseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("ms-settings:storagepolicies");

    private void OpenTaskManagerButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("taskmgr.exe");

    private void OpenWindowsUpdateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("ms-settings:windowsupdate");

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
        catch (Exception ex)
        {
            SnapshotText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }
}
