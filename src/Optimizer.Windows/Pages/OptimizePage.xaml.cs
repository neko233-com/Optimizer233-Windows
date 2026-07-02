using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Optimizer.Shared;
using Optimizer.Shared.Models;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Windows.UI;

namespace Optimizer_Windows.Pages;

public sealed partial class OptimizePage : Page
{
    private readonly FileSystemStatsService _fileSystemStatsService = FileSystemStatsService.Instance;
    private readonly OptimizationStateService _optimizationStateService = OptimizationStateService.Instance;
    private readonly string _tempPath = Path.GetTempPath();

    public ObservableCollection<OptimizationOption> Options { get; } = [];

    public OptimizePage()
    {
        InitializeComponent();
        InitializeOptions();
        ApplyLocalization();
        RefreshSelectedSummary();
    }

    private void InitializeOptions()
    {
        if (Options.Count > 0)
        {
            return;
        }

        Options.Add(new OptimizationOption { ActionId = "storage-sense" });
        Options.Add(new OptimizationOption { ActionId = "disk-cleanup" });
        Options.Add(new OptimizationOption { ActionId = "temp-folder" });
        Options.Add(new OptimizationOption { ActionId = "startup-apps" });
        Options.Add(new OptimizationOption { ActionId = "task-manager" });
        Options.Add(new OptimizationOption { ActionId = "windows-update" });
        Options.Add(new OptimizationOption { ActionId = "power-settings" });

        ApplyOptionLocalization();
        ApplySavedSelection();
    }

    private void ApplySavedSelection()
    {
        var selectedIds = _optimizationStateService.GetSelectedActionIds();
        foreach (var option in Options)
        {
            option.IsSelected = selectedIds.Contains(option.ActionId, StringComparer.OrdinalIgnoreCase);
        }
    }

    private void ApplyLocalization()
    {
        HeroEyebrowText.Text = AppText.Get("OptimizeHeroEyebrow");
        ChecklistTitleText.Text = AppText.Get("OptimizeChecklistTitle");
        ChecklistSubtitleText.Text = AppText.Get("OptimizeChecklistSubtitle");
        SelectedTitleText.Text = AppText.Get("OptimizeSelectedTitle");
        OutputTitleText.Text = AppText.Get("CommonOutput");
        ActionsPanelTitleText.Text = AppText.Get("OptimizeActionDeckTitle");
        ActionsPanelBodyText.Text = AppText.Get("OptimizeActionDeckBody");
        BalancedPresetButton.Content = AppText.Get("OptimizePresetBalancedAction");
        StoragePresetButton.Content = AppText.Get("OptimizePresetStorageAction");
        StartupPresetButton.Content = AppText.Get("OptimizePresetStartupAction");
        SelectAllButton.Content = AppText.Get("OptimizeSelectAll");
        ClearSelectionButton.Content = AppText.Get("OptimizeClearSelection");
        ExportPlanButton.Content = AppText.Get("OptimizeExportPlan");
        OpenSelectedButton.Content = AppText.Get("OptimizeOpenSelected");
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
        ApplyRiskMetadata(option);
    }

    private void ApplyRiskMetadata(OptimizationOption option)
    {
        var operation = OperationCatalog.Find(option.ActionId);
        if (operation is null)
        {
            option.Badge = AppText.Get("CommonUnknown");
            return;
        }

        option.RiskLevel = operation.RiskLevel;
        option.Badge = GetRiskLabel(operation.RiskLevel);

        var palette = operation.RiskLevel switch
        {
            OperationRiskLevel.Low => (bg: Color.FromArgb(255, 13, 29, 20), border: Color.FromArgb(255, 42, 82, 58), fg: Color.FromArgb(255, 176, 255, 198)),
            OperationRiskLevel.Medium => (bg: Color.FromArgb(255, 38, 25, 10), border: Color.FromArgb(255, 120, 78, 19), fg: Color.FromArgb(255, 255, 211, 140)),
            OperationRiskLevel.High => (bg: Color.FromArgb(255, 42, 12, 16), border: Color.FromArgb(255, 138, 34, 47), fg: Color.FromArgb(255, 255, 180, 188)),
            _ => (bg: Color.FromArgb(255, 24, 24, 24), border: Color.FromArgb(255, 64, 64, 64), fg: Color.FromArgb(255, 230, 230, 230))
        };

        option.BadgeBackground = new SolidColorBrush(palette.bg);
        option.BadgeBorder = new SolidColorBrush(palette.border);
        option.BadgeForeground = new SolidColorBrush(palette.fg);
    }

    private string GetRiskLabel(OperationRiskLevel riskLevel)
    {
        return riskLevel switch
        {
            OperationRiskLevel.Low => AppText.Get("SearchRiskLow"),
            OperationRiskLevel.Medium => AppText.Get("SearchRiskMedium"),
            OperationRiskLevel.High => AppText.Get("SearchRiskHigh"),
            _ => AppText.Get("CommonUnknown")
        };
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await UpdateOutputAsync();
    }

    private async Task UpdateOutputAsync()
    {
        try
        {
            var stats = await _fileSystemStatsService.ScanDirectoryAsync(_tempPath);
            OutputText.Text = $"{AppText.Get("OptimizeTempTitle")}: {stats.FileCount:N0} {AppText.Get("OptimizeFiles")} / {Helpers.FileSizeFormatter.Format(stats.TotalSizeBytes)}";
        }
        catch (Exception ex)
        {
            OutputText.Text = AppText.Format("StatusScanFailed", ex.Message);
        }
    }

    private void OptionSelectionChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is OptimizationOption option)
        {
            option.IsSelected = checkBox.IsChecked == true;
        }

        RefreshSelectedSummary();
    }

    private void RefreshSelectedSummary()
    {
        var selected = Options.Where(item => item.IsSelected).ToArray();
        _optimizationStateService.SetSelectedActionIds(selected.Select(item => item.ActionId));
        SelectedItemsText.Text = selected.Length == 0
            ? AppText.Get("StatusNoSelection")
            : $"{AppText.Format("OptimizeSelectionCount", selected.Length)}{Environment.NewLine}{string.Join(Environment.NewLine, selected.Select(item => $"• {item.Title} · {item.Badge}"))}";
    }

    private void BalancedPresetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyPreset(["storage-sense", "startup-apps", "windows-update"]);
    }

    private void StoragePresetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyPreset(["storage-sense", "disk-cleanup", "temp-folder"]);
    }

    private void StartupPresetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyPreset(["startup-apps", "task-manager", "power-settings"]);
    }

    private void ApplyPreset(IReadOnlyCollection<string> actionIds)
    {
        foreach (var option in Options)
        {
            option.IsSelected = actionIds.Contains(option.ActionId);
        }

        RebindOptions();
        RefreshSelectedSummary();
    }

    private void SelectAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        foreach (var option in Options)
        {
            option.IsSelected = true;
        }

        RebindOptions();
        RefreshSelectedSummary();
    }

    private void ClearSelectionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        foreach (var option in Options)
        {
            option.IsSelected = false;
        }

        RebindOptions();
        RefreshSelectedSummary();
    }

    private void OpenSelectedButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var selected = Options.Where(option => option.IsSelected).ToArray();
        if (selected.Length == 0)
        {
            OutputText.Text = AppText.Get("StatusNoSelection");
            return;
        }

        foreach (var option in selected)
        {
            OpenAction(option.ActionId);
        }

        OutputText.Text = AppText.Format("StatusOpenedSelection", selected.Length);
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
                    option.Description,
                    RiskLevel = option.Badge
                })
                .ToArray();

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var exportDirectory = Path.Combine(documents, "Optimizer Windows");
            Directory.CreateDirectory(exportDirectory);

            var exportPath = Path.Combine(exportDirectory, "optimization-plan.json");
            var json = JsonSerializer.Serialize(selected, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(exportPath, json);

            OutputText.Text = AppText.Format("StatusPlanExported", exportPath);
        }
        catch (Exception ex)
        {
            OutputText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
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
            OutputText.Text = AppText.Format("StatusOpenFailed", ex.Message);
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
