using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;

namespace Optimizer_Windows.Pages;

public sealed partial class HomePage : Page
{
    private readonly SystemSnapshotService _snapshotService = SystemSnapshotService.Instance;

    public ObservableCollection<DiskUsage> Drives { get; } = [];

    public HomePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("OverviewTitle");
        PageSubtitleText.Text = AppText.Get("OverviewSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");
        DeviceLabelText.Text = AppText.Get("OverviewDevice");
        WindowsLabelText.Text = AppText.Get("OverviewWindows");
        ProcessorLabelText.Text = AppText.Get("OverviewProcessor");
        MemoryLabelText.Text = AppText.Get("OverviewMemory");
        StorageLabelText.Text = AppText.Get("OverviewStorage");
        FocusLabelText.Text = AppText.Get("OverviewFocus");
        FocusTitleText.Text = AppText.Get("OverviewFocusTitle");
        FocusBodyText.Text = AppText.Get("OverviewFocusBody");
        DriveHealthTitleText.Text = AppText.Get("OverviewDriveHealth");
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
            StatusText.Text = AppText.Get("StatusCollectingTelemetry");

            var snapshot = await _snapshotService.CaptureAsync();

            HostNameText.Text = snapshot.DeviceName;
            DeviceText.Text = snapshot.DeviceModel;
            WindowsText.Text = snapshot.WindowsVersion;
            BootText.Text = snapshot.BootTime;
            ProcessorText.Text = snapshot.ProcessorName;
            GraphicsText.Text = snapshot.GraphicsName;
            MemoryText.Text = snapshot.MemorySummary;
            MemoryUsageBar.Value = snapshot.MemoryUsagePercent;
            StorageText.Text = snapshot.StorageSummary;
            NetworkText.Text = snapshot.NetworkSummary;

            Drives.Clear();
            foreach (var drive in snapshot.Drives)
            {
                Drives.Add(drive);
            }

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
}
