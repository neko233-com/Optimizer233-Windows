using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Services;
using System.Diagnostics;

namespace Optimizer_Windows.Pages;

public sealed partial class RestorePage : Page
{
    private readonly AppConfigurationService _configurationService = AppConfigurationService.Instance;

    public RestorePage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavRestore");
        PageSubtitleText.Text = AppText.Get("RestoreSubtitle");
        ExportButton.Content = AppText.Get("RestoreExport");
        ImportButton.Content = AppText.Get("RestoreImport");
        ResetButton.Content = AppText.Get("RestoreReset");
        OpenSystemRestoreButton.Content = AppText.Get("RestoreOpenSystem");
        StatusText.Text = AppText.Get("RestoreReady");
    }

    private async void ExportButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var directory = Path.Combine(documents, "Optimizer Windows");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "optimizer233-config.json");
            await _configurationService.ExportAsync(path);
            StatusText.Text = AppText.Format("StatusPlanExported", path);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private async void ImportButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Path.Combine(documents, "Optimizer Windows", "optimizer233-config.json");
            await _configurationService.ImportAsync(path);
            StatusText.Text = AppText.Format("RestoreImported", path);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private async void ResetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            await _configurationService.ResetAsync();
            StatusText.Text = AppText.Get("RestoreResetDone");
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private void OpenSystemRestoreButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "SystemPropertiesProtection.exe",
                UseShellExecute = true
            });
            StatusText.Text = AppText.Get("RestoreOpenedSystem");
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }
}
