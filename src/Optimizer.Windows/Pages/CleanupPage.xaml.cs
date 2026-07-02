using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Helpers;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;

namespace Optimizer_Windows.Pages;

public sealed partial class CleanupPage : Page
{
    private readonly CleanupService _cleanupService = CleanupService.Instance;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _cleanCts;
    private bool _isScanning;
    private bool _isCleaning;
    private bool _hasScanResults;

    public ObservableCollection<CleanupTargetResult> CleanupTargets { get; } = [];

    public CleanupPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavCleanup");
        PageSubtitleText.Text = AppText.Get("OptimizeCleanupSubtitle");
        SummaryCardTitleText.Text = AppText.Get("OptimizeCleanupSummaryTitle");
        TargetsLabelText.Text = AppText.Get("OptimizeCleanupTargets");
        FilesLabelText.Text = AppText.Get("OptimizeFiles");
        SizeLabelText.Text = AppText.Get("OptimizeSize");
        TargetsListTitleText.Text = AppText.Get("OptimizeCleanupTitle");
        LegendTargetText.Text = AppText.Get("CleanupLegendTarget");
        LegendFilesText.Text = AppText.Get("CleanupLegendFiles");
        LegendSizeText.Text = AppText.Get("CleanupLegendSize");
        LegendActionText.Text = AppText.Get("CleanupLegendAction");
        AnalyzeButton.Content = AppText.Get("CleanupStartScan");
        CancelScanButton.Content = AppText.Get("CleanupCancelScan");
        CleanButton.Content = AppText.Get("OptimizeCleanupAction");
        ActionsTitleText.Text = AppText.Get("OverviewActionsTitle");
        ActionsHintText.Text = AppText.Get("CleanupScanHint");
        EmptyStateText.Text = AppText.Get("CleanupNoScanYet");
        TargetsValueText.Text = "0";
        FilesValueText.Text = "0";
        SizeValueText.Text = "0 B";
        SummaryText.Text = AppText.Get("CleanupReadyToScan");
        UpdateActionState();
        UpdateEmptyStateVisibility();
    }

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SummaryText.Text = AppText.Get("CleanupReadyToScan");
        UpdateActionState();
    }

    private void Page_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _scanCts?.Cancel();
        _cleanCts?.Cancel();
    }

    private async void AnalyzeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshCleanupTargetsAsync();
    }

    private void CancelScanButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _scanCts?.Cancel();
        _cleanCts?.Cancel();
    }

    private async void CleanButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await CleanTargetsAsync(_cleanupService.GetTargets().Select(item => item.Id));
    }

    private async void CleanTargetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not CleanupTargetResult target)
        {
            return;
        }

        await CleanTargetsAsync([target.Id], target.Title);
    }

    private async Task CleanTargetsAsync(IEnumerable<string> targetIds, string? targetTitle = null)
    {
        try
        {
            _cleanCts?.Cancel();
            _cleanCts = new CancellationTokenSource();
            _isCleaning = true;

            AnalyzeButton.IsEnabled = false;
            CancelScanButton.IsEnabled = true;
            CleanButton.IsEnabled = false;
            ScanProgressRing.IsActive = true;
            ScanProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            SummaryText.Text = AppText.Get("StatusCleaningJunk");

            var result = await _cleanupService.CleanAsync(targetIds, _cleanCts.Token);

            SummaryText.Text = string.IsNullOrWhiteSpace(targetTitle)
                ? AppText.Format(
                    "StatusCleanupDone",
                    result.DeletedFileCount.ToString("N0"),
                    FileSizeFormatter.Format(result.FreedBytes),
                    result.FailedItemCount.ToString("N0"))
                : AppText.Format(
                    "CleanupLocalDone",
                    targetTitle,
                    result.DeletedFileCount.ToString("N0"),
                    FileSizeFormatter.Format(result.FreedBytes),
                    result.FailedItemCount.ToString("N0"));

            await RefreshCleanupTargetsAsync();
        }
        catch (OperationCanceledException)
        {
            SummaryText.Text = AppText.Get("CleanupScanCanceled");
        }
        catch (Exception ex)
        {
            SummaryText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
        finally
        {
            _isCleaning = false;
            _cleanCts = null;
            ScanProgressRing.IsActive = false;
            ScanProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            UpdateActionState();
        }
    }

    private async Task RefreshCleanupTargetsAsync()
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var cancellationToken = _scanCts.Token;

        try
        {
            _isScanning = true;
            UpdateActionState();
            ScanProgressRing.IsActive = true;
            ScanProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            SummaryText.Text = AppText.Get("StatusScanningCleanup");

            var results = await _cleanupService.ScanAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            CleanupTargets.Clear();
            foreach (var result in results)
            {
                CleanupTargets.Add(result);
            }

            var totalFiles = results.Sum(item => item.FileCount);
            var totalBytes = results.Sum(item => item.TotalSizeBytes);
            _hasScanResults = results.Count > 0;
            TargetsValueText.Text = results.Count.ToString("N0");
            FilesValueText.Text = totalFiles.ToString("N0");
            SizeValueText.Text = FileSizeFormatter.Format(totalBytes);
            SummaryText.Text = AppText.Format(
                "StatusCleanupSummary",
                totalFiles.ToString("N0"),
                results.Count.ToString("N0"),
                FileSizeFormatter.Format(totalBytes));
            UpdateEmptyStateVisibility();
        }
        catch (OperationCanceledException)
        {
            SummaryText.Text = AppText.Get("CleanupScanCanceled");
        }
        catch (Exception ex)
        {
            SummaryText.Text = AppText.Format("StatusScanFailed", ex.Message);
        }
        finally
        {
            _isScanning = false;
            _scanCts = null;
            ScanProgressRing.IsActive = false;
            ScanProgressRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            UpdateActionState();
        }
    }

    private void UpdateActionState()
    {
        AnalyzeButton.IsEnabled = !_isScanning && !_isCleaning;
        CancelScanButton.IsEnabled = _isScanning || _isCleaning;
        CleanButton.IsEnabled = !_isScanning && !_isCleaning && _hasScanResults;
        TargetsListView.IsEnabled = !_isScanning && !_isCleaning;
    }

    private void UpdateEmptyStateVisibility()
    {
        EmptyStateText.Visibility = CleanupTargets.Count == 0
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }
}
