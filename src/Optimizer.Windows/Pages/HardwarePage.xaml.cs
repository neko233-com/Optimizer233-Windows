using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;

namespace Optimizer_Windows.Pages;

public sealed partial class HardwarePage : Page
{
    private readonly SystemSnapshotService _snapshotService = SystemSnapshotService.Instance;
    private CancellationTokenSource? _refreshCts;

    public ObservableCollection<DetailItem> CpuItems { get; } = [];

    public ObservableCollection<DetailItem> GpuItems { get; } = [];

    public ObservableCollection<DetailItem> MemoryItems { get; } = [];

    public ObservableCollection<DetailItem> StorageItems { get; } = [];

    public ObservableCollection<DetailItem> NetworkItems { get; } = [];

    public ObservableCollection<DetailItem> InputItems { get; } = [];

    public HardwarePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        HeroEyebrowText.Text = AppText.Get("HardwareHeroEyebrow");
        PanelTitleText.Text = AppText.Get("HardwarePanelTitle");
        PageTitleText.Text = AppText.Get("NavHardware");
        PageSubtitleText.Text = AppText.Get("HardwareSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");

        CpuSummaryLabelText.Text = AppText.Get("HardwareSummaryCpu");
        GpuSummaryLabelText.Text = AppText.Get("HardwareSummaryGpu");
        MemorySummaryLabelText.Text = AppText.Get("HardwareSummaryMemory");
        StorageSummaryLabelText.Text = AppText.Get("HardwareSummaryStorage");
        NetworkSummaryLabelText.Text = AppText.Get("HardwareSummaryNetwork");
        InputSummaryLabelText.Text = AppText.Get("HardwareSummaryInput");

        CpuCardTitleText.Text = AppText.Get("HardwareCpu");
        GpuCardTitleText.Text = AppText.Get("HardwareGpu");
        MemoryCardTitleText.Text = AppText.Get("HardwareMemory");
        StorageCardTitleText.Text = AppText.Get("HardwareStorage");
        NetworkCardTitleText.Text = AppText.Get("HardwareNetwork");
        InputCardTitleText.Text = AppText.Get("HardwareInput");
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
            LoadingRing.IsActive = true;
            RefreshButton.IsEnabled = false;
            StatusText.Text = AppText.Get("StatusCollectingHardware");

            var snapshot = await _snapshotService.CaptureAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            ReplaceItems(CpuItems, snapshot.CpuDetails);
            ReplaceItems(GpuItems, snapshot.GpuDetails);
            ReplaceItems(MemoryItems, snapshot.MemoryDetails);
            ReplaceItems(StorageItems, snapshot.StorageDetails);
            ReplaceItems(NetworkItems, snapshot.NetworkDetails);
            ReplaceItems(InputItems, snapshot.InputDetails);

            CpuSummaryValueText.Text = snapshot.ProcessorName;
            CpuSummaryDetailText.Text = $"{GetValue(snapshot.CpuDetails, "Cores")} C / {GetValue(snapshot.CpuDetails, "Threads")} T";

            var primaryGpu = GetPrimaryGpu(snapshot.GpuDetails);
            GpuSummaryValueText.Text = primaryGpu;
            GpuSummaryDetailText.Text = $"{snapshot.GpuDetails.Count} {AppText.Get("HardwareItems")}";

            MemorySummaryValueText.Text = GetValue(snapshot.MemoryDetails, "Installed");
            MemorySummaryDetailText.Text = snapshot.MemorySummary;

            StorageSummaryValueText.Text = $"{snapshot.Drives.Count} {AppText.Get("HardwareDetected")}";
            StorageSummaryDetailText.Text = snapshot.StorageSummary;

            NetworkSummaryValueText.Text = snapshot.NetworkSummary;
            NetworkSummaryDetailText.Text = $"{snapshot.NetworkDetails.Count} {AppText.Get("HardwareItems")}";

            InputSummaryValueText.Text = snapshot.InputSummary;
            InputSummaryDetailText.Text = $"{snapshot.InputDetails.Count} {AppText.Get("HardwareItems")}";

            StatusText.Text = AppText.Format("HardwareUpdatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusLoadFailed", ex.Message);
        }
        finally
        {
            LoadingRing.IsActive = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private static string GetValue(IReadOnlyList<DetailItem> items, string label)
    {
        return items.FirstOrDefault(item => string.Equals(item.Label, label, StringComparison.OrdinalIgnoreCase))?.Value
            ?? AppText.Get("CommonUnavailable");
    }

    private static string GetPrimaryGpu(IReadOnlyList<DetailItem> items)
    {
        if (items.Count == 0)
        {
            return AppText.Get("CommonUnavailable");
        }

        var preferred = items.FirstOrDefault(item =>
            item.Label.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
            item.Label.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
            item.Label.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
            item.Label.Contains("Intel", StringComparison.OrdinalIgnoreCase));

        return preferred?.Label ?? items[0].Label;
    }

    private static void ReplaceItems(ObservableCollection<DetailItem> target, IReadOnlyList<DetailItem> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
