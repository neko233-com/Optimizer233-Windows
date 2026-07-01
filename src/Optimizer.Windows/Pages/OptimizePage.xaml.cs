using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Helpers;
using Optimizer_Windows.Services;
using System.Diagnostics;

namespace Optimizer_Windows.Pages;

public sealed partial class OptimizePage : Page
{
    private readonly FileSystemStatsService _fileSystemStatsService = FileSystemStatsService.Instance;

    private readonly string _tempPath = Path.GetTempPath();

    public OptimizePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavOptimize");
        PageSubtitleText.Text = AppText.Get("OptimizeSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");
        TempSnapshotTitleText.Text = AppText.Get("OptimizeTempTitle");
        TempFilesLabelText.Text = AppText.Get("OptimizeFiles");
        TempSizeLabelText.Text = AppText.Get("OptimizeSize");
        OpenTempFolderButton.Content = AppText.Get("OptimizeOpenTemp");
        StorageSenseTitleText.Text = AppText.Get("OptimizeStorageSenseTitle");
        StorageSenseBodyText.Text = AppText.Get("OptimizeStorageSenseBody");
        OpenStorageSenseButton.Content = AppText.Get("OptimizeStorageSenseAction");
        StartupAppsTitleText.Text = AppText.Get("OptimizeStartupTitle");
        StartupAppsBodyText.Text = AppText.Get("OptimizeStartupBody");
        OpenStartupAppsButton.Content = AppText.Get("OptimizeStartupAction");
        TaskManagerTitleText.Text = AppText.Get("OptimizeTaskManagerTitle");
        TaskManagerBodyText.Text = AppText.Get("OptimizeTaskManagerBody");
        OpenTaskManagerButton.Content = AppText.Get("OptimizeTaskManagerAction");
        DiskCleanupTitleText.Text = AppText.Get("OptimizeDiskCleanupTitle");
        DiskCleanupBodyText.Text = AppText.Get("OptimizeDiskCleanupBody");
        OpenDiskCleanupButton.Content = AppText.Get("OptimizeDiskCleanupAction");
        WindowsUpdateTitleText.Text = AppText.Get("OptimizeWindowsUpdateTitle");
        WindowsUpdateBodyText.Text = AppText.Get("OptimizeWindowsUpdateBody");
        OpenWindowsUpdateButton.Content = AppText.Get("OptimizeWindowsUpdateAction");
        PowerSettingsTitleText.Text = AppText.Get("OptimizePowerTitle");
        PowerSettingsBodyText.Text = AppText.Get("OptimizePowerBody");
        OpenPowerSettingsButton.Content = AppText.Get("OptimizePowerAction");
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshTempStatsAsync();
    }

    private async void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshTempStatsAsync();
    }

    private async Task RefreshTempStatsAsync()
    {
        try
        {
            LoadingRing.IsActive = true;
            RefreshButton.IsEnabled = false;
            StatusText.Text = AppText.Get("StatusScanningTemp");
            TempPathText.Text = _tempPath;

            var stats = await _fileSystemStatsService.ScanDirectoryAsync(_tempPath);

            TempFileCountText.Text = $"{stats.FileCount:N0}";
            TempFolderSizeText.Text = FileSizeFormatter.Format(stats.TotalSizeBytes);
            StatusText.Text = AppText.Format("StatusUpdated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusScanFailed", ex.Message);
        }
        finally
        {
            LoadingRing.IsActive = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private void OpenTempFolderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch(_tempPath);
    }

    private void OpenStorageSenseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch("ms-settings:storagepolicies");
    }

    private void OpenStartupAppsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch("ms-settings:startupapps");
    }

    private void OpenTaskManagerButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch("taskmgr.exe");
    }

    private void OpenDiskCleanupButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch("cleanmgr.exe");
    }

    private void OpenWindowsUpdateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch("ms-settings:windowsupdate");
    }

    private void OpenPowerSettingsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch("ms-settings:powersleep");
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

            StatusText.Text = AppText.Format("StatusOpened", target);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }
}
