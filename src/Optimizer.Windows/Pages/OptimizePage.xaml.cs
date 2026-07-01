using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Helpers;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace Optimizer_Windows.Pages;

public sealed partial class OptimizePage : Page
{
    private readonly FileSystemStatsService _fileSystemStatsService = FileSystemStatsService.Instance;

    private readonly string _tempPath = Path.GetTempPath();

    public ObservableCollection<OptimizationOption> Options { get; } = [];

    public OptimizePage()
    {
        InitializeComponent();
        InitializeOptions();
        ApplyLocalization();
    }

    private void InitializeOptions()
    {
        if (Options.Count > 0)
        {
            return;
        }

        var safeBadge = AppText.Get("OptimizeBadgeSafe");

        Options.Add(new OptimizationOption { ActionId = "storage-sense", Badge = safeBadge });
        Options.Add(new OptimizationOption { ActionId = "disk-cleanup", Badge = safeBadge });
        Options.Add(new OptimizationOption { ActionId = "temp-folder", Badge = safeBadge });
        Options.Add(new OptimizationOption { ActionId = "startup-apps", Badge = safeBadge });
        Options.Add(new OptimizationOption { ActionId = "task-manager", Badge = safeBadge });
        Options.Add(new OptimizationOption { ActionId = "windows-update", Badge = safeBadge });
        Options.Add(new OptimizationOption { ActionId = "power-settings", Badge = safeBadge });

        ApplyOptionLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavOptimize");
        PageSubtitleText.Text = AppText.Get("OptimizeSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");
        HeroEyebrowText.Text = AppText.Get("OptimizeHeroEyebrow");
        HeroTitleText.Text = AppText.Get("OptimizeHeroTitle");
        HeroBodyText.Text = AppText.Get("OptimizeHeroBody");
        OpenSelectedButton.Content = AppText.Get("OptimizeOpenSelected");
        ExportPlanButton.Content = AppText.Get("OptimizeExportPlan");
        TempSnapshotTitleText.Text = AppText.Get("OptimizeTempTitle");
        TempFilesLabelText.Text = AppText.Get("OptimizeFiles");
        TempSizeLabelText.Text = AppText.Get("OptimizeSize");
        OpenTempFolderButton.Content = AppText.Get("OptimizeOpenTemp");
        ProfilesTitleText.Text = AppText.Get("OptimizeProfilesTitle");
        ProfilesSubtitleText.Text = AppText.Get("OptimizeProfilesSubtitle");
        BalancedPresetTitleText.Text = AppText.Get("OptimizePresetBalancedTitle");
        BalancedPresetBodyText.Text = AppText.Get("OptimizePresetBalancedBody");
        BalancedPresetButton.Content = AppText.Get("OptimizePresetBalancedAction");
        StoragePresetTitleText.Text = AppText.Get("OptimizePresetStorageTitle");
        StoragePresetBodyText.Text = AppText.Get("OptimizePresetStorageBody");
        StoragePresetButton.Content = AppText.Get("OptimizePresetStorageAction");
        StartupPresetTitleText.Text = AppText.Get("OptimizePresetStartupTitle");
        StartupPresetBodyText.Text = AppText.Get("OptimizePresetStartupBody");
        StartupPresetButton.Content = AppText.Get("OptimizePresetStartupAction");
        ChecklistTitleText.Text = AppText.Get("OptimizeChecklistTitle");
        ChecklistSubtitleText.Text = AppText.Get("OptimizeChecklistSubtitle");
        SelectAllButton.Content = AppText.Get("OptimizeSelectAll");
        ClearSelectionButton.Content = AppText.Get("OptimizeClearSelection");
        ApplyOptionLocalization();
    }

    private void ApplyOptionLocalization()
    {
        SetOptionText("storage-sense", "OptimizeStorageSenseTitle", "OptimizeStorageSenseBody");
        SetOptionText("disk-cleanup", "OptimizeDiskCleanupTitle", "OptimizeDiskCleanupBody");
        SetOptionText("temp-folder", "OptimizeOptionTempTitle", "OptimizeOptionTempBody");
        SetOptionText("startup-apps", "OptimizeStartupTitle", "OptimizeStartupBody");
        SetOptionText("task-manager", "OptimizeTaskManagerTitle", "OptimizeTaskManagerBody");
        SetOptionText("windows-update", "OptimizeWindowsUpdateTitle", "OptimizeWindowsUpdateBody");
        SetOptionText("power-settings", "OptimizePowerTitle", "OptimizePowerBody");
    }

    private void SetOptionText(string actionId, string titleKey, string descriptionKey)
    {
        var option = Options.FirstOrDefault(item => item.ActionId == actionId);
        if (option is null)
        {
            return;
        }

        option.Title = AppText.Get(titleKey);
        option.Description = AppText.Get(descriptionKey);
        option.Badge = AppText.Get("OptimizeBadgeSafe");
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

    private void BalancedPresetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyPreset("balanced", ["storage-sense", "startup-apps", "windows-update"]);
    }

    private void StoragePresetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyPreset("storage", ["storage-sense", "disk-cleanup", "temp-folder"]);
    }

    private void StartupPresetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyPreset("startup", ["startup-apps", "task-manager", "power-settings"]);
    }

    private void ApplyPreset(string presetName, IReadOnlyCollection<string> actionIds)
    {
        foreach (var option in Options)
        {
            option.IsSelected = actionIds.Contains(option.ActionId);
        }

        StatusText.Text = AppText.Format("StatusPresetApplied", AppText.Get($"OptimizePresetName{presetName[..1].ToUpperInvariant()}{presetName[1..]}"));
        RebindOptions();
    }

    private void SelectAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        foreach (var option in Options)
        {
            option.IsSelected = true;
        }

        RebindOptions();
    }

    private void ClearSelectionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        foreach (var option in Options)
        {
            option.IsSelected = false;
        }

        RebindOptions();
    }

    private void OpenSelectedButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var selected = Options.Where(option => option.IsSelected).ToArray();
        if (selected.Length == 0)
        {
            StatusText.Text = AppText.Get("StatusNoSelection");
            return;
        }

        foreach (var option in selected)
        {
            OpenAction(option.ActionId);
        }

        StatusText.Text = AppText.Format("StatusOpenedSelection", selected.Length);
    }

    private async void ExportPlanButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var selected = Options
                .Where(option => option.IsSelected)
                .Select(option => new
                {
                    option.ActionId,
                    option.Title,
                    option.Description
                })
                .ToArray();

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var exportDirectory = Path.Combine(documents, "Optimizer Windows");
            Directory.CreateDirectory(exportDirectory);

            var exportPath = Path.Combine(exportDirectory, "optimization-plan.json");
            var json = JsonSerializer.Serialize(selected, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(exportPath, json);

            StatusText.Text = AppText.Format("StatusPlanExported", exportPath);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private void OpenTempFolderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch(_tempPath);
    }

    private void OpenAction(string actionId)
    {
        switch (actionId)
        {
            case "storage-sense":
                Launch("ms-settings:storagepolicies");
                break;
            case "disk-cleanup":
                Launch("cleanmgr.exe");
                break;
            case "temp-folder":
                Launch(_tempPath);
                break;
            case "startup-apps":
                Launch("ms-settings:startupapps");
                break;
            case "task-manager":
                Launch("taskmgr.exe");
                break;
            case "windows-update":
                Launch("ms-settings:windowsupdate");
                break;
            case "power-settings":
                Launch("ms-settings:powersleep");
                break;
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
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private void RebindOptions()
    {
        var ordered = Options.ToArray();
        Options.Clear();
        foreach (var option in ordered)
        {
            Options.Add(option);
        }
    }
}
