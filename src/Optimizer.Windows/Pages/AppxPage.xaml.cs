using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;

namespace Optimizer_Windows.Pages;

public sealed partial class AppxPage : Page
{
    private readonly AppxService _appxService = AppxService.Instance;

    public ObservableCollection<AppxPackageInfo> Packages { get; } = [];

    public AppxPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavAppx");
        PageSubtitleText.Text = AppText.Get("AppxSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");
        RemoveButton.Content = AppText.Get("AppxRemove");
        StatusText.Text = AppText.Get("AppxReady");
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshPackagesAsync();
    }

    private async void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshPackagesAsync();
    }

    private async void RemoveButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var selected = Packages.Where(item => item.IsSelected).ToArray();
        if (selected.Length == 0)
        {
            StatusText.Text = AppText.Get("StatusNoSelection");
            return;
        }

        try
        {
            RemoveButton.IsEnabled = false;
            RefreshButton.IsEnabled = false;

            foreach (var package in selected)
            {
                await _appxService.RemovePackageAsync(package.PackageFullName);
            }

            StatusText.Text = AppText.Format("AppxRemoved", selected.Length);
            await RefreshPackagesAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
        finally
        {
            RemoveButton.IsEnabled = true;
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task RefreshPackagesAsync()
    {
        try
        {
            StatusText.Text = AppText.Get("AppxLoading");
            Packages.Clear();
            foreach (var package in await _appxService.GetInstalledPackagesAsync())
            {
                Packages.Add(package);
            }

            StatusText.Text = AppText.Format("AppxLoaded", Packages.Count);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusLoadFailed", ex.Message);
        }
    }
}
