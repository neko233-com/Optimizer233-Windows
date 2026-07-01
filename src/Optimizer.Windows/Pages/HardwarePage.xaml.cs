using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;

namespace Optimizer_Windows.Pages;

public sealed partial class HardwarePage : Page
{
    private readonly SystemSnapshotService _snapshotService = SystemSnapshotService.Instance;

    public ObservableCollection<DetailItem> CpuItems { get; } = [];

    public ObservableCollection<DetailItem> GpuItems { get; } = [];

    public ObservableCollection<DetailItem> MemoryItems { get; } = [];

    public ObservableCollection<DetailItem> StorageItems { get; } = [];

    public ObservableCollection<DetailItem> NetworkItems { get; } = [];

    public HardwarePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavHardware");
        PageSubtitleText.Text = AppText.Get("HardwareSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");
        CpuExpander.Header = AppText.Get("HardwareCpu");
        GpuExpander.Header = AppText.Get("HardwareGpu");
        MemoryExpander.Header = AppText.Get("HardwareMemory");
        StorageExpander.Header = AppText.Get("HardwareStorage");
        NetworkExpander.Header = AppText.Get("HardwareNetwork");
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
        try
        {
            LoadingRing.IsActive = true;
            RefreshButton.IsEnabled = false;
            StatusText.Text = AppText.Get("StatusCollectingHardware");

            var snapshot = await _snapshotService.CaptureAsync();

            ReplaceItems(CpuItems, snapshot.CpuDetails);
            ReplaceItems(GpuItems, snapshot.GpuDetails);
            ReplaceItems(MemoryItems, snapshot.MemoryDetails);
            ReplaceItems(StorageItems, snapshot.StorageDetails);
            ReplaceItems(NetworkItems, snapshot.NetworkDetails);

            StatusText.Text = AppText.Format("StatusUpdated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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

    private static void ReplaceItems(ObservableCollection<DetailItem> target, IReadOnlyList<DetailItem> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
