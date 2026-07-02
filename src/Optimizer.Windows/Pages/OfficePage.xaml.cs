using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Optimizer_Windows.Pages;

public sealed partial class OfficePage : Page
{
    private readonly OfficeService _officeService = OfficeService.Instance;

    public ObservableCollection<OfficeInstallInfo> OfficeProducts { get; } = [];

    public OfficePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavOffice");
        PageSubtitleText.Text = AppText.Get("OfficeSubtitle");
        RefreshButton.Content = AppText.Get("Refresh");
        OpenDownloadButton.Content = AppText.Get("OfficeOpenDownload");
        OpenDeploymentToolButton.Content = AppText.Get("OfficeOpenOdt");
        OpenAppsFeaturesButton.Content = AppText.Get("OfficeOpenApps");
        StatusText.Text = AppText.Get("OfficeReady");
    }

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RefreshProducts();
    }

    private void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RefreshProducts();
    }

    private void RefreshProducts()
    {
        OfficeProducts.Clear();
        foreach (var product in _officeService.GetInstalledProducts())
        {
            OfficeProducts.Add(product);
        }

        StatusText.Text = AppText.Format("OfficeLoaded", OfficeProducts.Count);
    }

    private void OpenDownloadButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch(_officeService.GetDownloadUrl());

    private void OpenDeploymentToolButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch(_officeService.GetDeploymentToolUrl());

    private void OpenAppsFeaturesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("ms-settings:appsfeatures");

    private void Launch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }
}
