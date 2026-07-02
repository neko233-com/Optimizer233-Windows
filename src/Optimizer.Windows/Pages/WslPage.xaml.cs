using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;

namespace Optimizer_Windows.Pages;

public sealed partial class WslPage : Page
{
    private readonly WslService _wslService = WslService.Instance;
    private readonly List<WslDistroInfo> _allDistros = [];
    private CancellationTokenSource? _refreshCts;
    private bool _isBusy;

    public ObservableCollection<WslDistroInfo> VisibleDistros { get; } = [];

    public WslPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        HeroEyebrowText.Text = AppText.Get("WslHeroEyebrow");
        PageTitleText.Text = AppText.Get("NavWsl");
        PageSubtitleText.Text = AppText.Get("WslSubtitle");
        StatusCardTitleText.Text = AppText.Get("WslStatusTitle");
        RefreshButton.Content = AppText.Get("Refresh");
        DistroMetricLabelText.Text = AppText.Get("WslDistrosMetric");
        RunningMetricLabelText.Text = AppText.Get("WslRunningMetric");
        VersionMetricLabelText.Text = AppText.Get("WslDefaultVersionMetric");
        DefaultMetricLabelText.Text = AppText.Get("WslDefaultDistroMetric");
        SearchBox.PlaceholderText = AppText.Get("WslSearchPlaceholder");
        InstallButton.Content = AppText.Get("WslInstall");
        UpdateButton.Content = AppText.Get("WslUpdate");
        ShutdownButton.Content = AppText.Get("WslShutdown");
        DistrosTitleText.Text = AppText.Get("WslDistrosTitle");
        RawStatusTitleText.Text = AppText.Get("WslRawStatusTitle");
        ActionHintText.Text = AppText.Get("WslActionHint");
        OpenShellButton.Content = AppText.Get("WslOpenShell");
        OpenDocsButton.Content = AppText.Get("WslOpenDocs");
        EmptyStateText.Text = AppText.Get("WslEmpty");
        StatusText.Text = AppText.Get("WslReady");
        DistroMetricValueText.Text = "0";
        RunningMetricValueText.Text = "0";
        VersionMetricValueText.Text = AppText.Get("WslDefaultVersionNone");
        DefaultMetricValueText.Text = AppText.Get("WslDefaultDistroNone");
        RawStatusText.Text = AppText.Get("CommonNotChecked");
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _refreshCts?.Cancel();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var cancellationToken = _refreshCts.Token;

        try
        {
            _isBusy = true;
            UpdateActionState();
            StatusText.Text = AppText.Get("WslLoading");

            var snapshot = await _wslService.GetStatusAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _allDistros.Clear();
            _allDistros.AddRange(snapshot.Distros);
            ApplyFilter(SearchBox.Text);

            DistroMetricValueText.Text = snapshot.DistroCount.ToString();
            RunningMetricValueText.Text = snapshot.RunningCount.ToString();
            VersionMetricValueText.Text = snapshot.DefaultVersion;
            DefaultMetricValueText.Text = snapshot.DefaultDistribution;
            RawStatusText.Text = snapshot.RawStatus;
            StatusText.Text = snapshot.StatusSummary;
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
            _isBusy = false;
            UpdateActionState();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(SearchBox.Text);
    }

    private void ApplyFilter(string? rawQuery)
    {
        var query = rawQuery?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allDistros
            : _allDistros.Where(distro =>
                distro.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                distro.State.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                distro.VersionLabel.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        VisibleDistros.Clear();
        foreach (var distro in filtered)
        {
            VisibleDistros.Add(distro);
        }

        EmptyStateText.Visibility = VisibleDistros.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(
            cancellationToken => _wslService.InstallAsync(cancellationToken),
            AppText.Get("WslInstallStarted"));
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(
            cancellationToken => _wslService.UpdateAsync(cancellationToken),
            AppText.Get("WslUpdated"));
    }

    private async void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(
            cancellationToken => _wslService.ShutdownAsync(cancellationToken),
            AppText.Get("WslShutdownDone"));
    }

    private async Task RunActionAsync(Func<CancellationToken, Task> action, string successText)
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var cancellationToken = _refreshCts.Token;

        try
        {
            _isBusy = true;
            UpdateActionState();
            StatusText.Text = AppText.Get("StatusCollectingTelemetry");
            await action(cancellationToken);
            StatusText.Text = successText;
            await RefreshAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
        finally
        {
            _isBusy = false;
            UpdateActionState();
        }
    }

    private void OpenShellButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _wslService.OpenShell();
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private void OpenDocsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _wslService.OpenDocs();
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private void UpdateActionState()
    {
        RefreshButton.IsEnabled = !_isBusy;
        SearchBox.IsEnabled = !_isBusy;
        InstallButton.IsEnabled = !_isBusy;
        UpdateButton.IsEnabled = !_isBusy;
        ShutdownButton.IsEnabled = !_isBusy;
        OpenShellButton.IsEnabled = !_isBusy;
        OpenDocsButton.IsEnabled = !_isBusy;
    }
}
