using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Optimizer_Windows.Pages;

public sealed partial class TroubleshootingPage : Page
{
    private readonly TroubleshootingService _troubleshootingService = TroubleshootingService.Instance;

    public ObservableCollection<ServiceItemInfo> Services { get; } = [];
    public ObservableCollection<ScheduledTaskInfo> Tasks { get; } = [];

    public TroubleshootingPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        ServicesTitleText.Text = AppText.Get("TroubleshootServices");
        TasksTitleText.Text = AppText.Get("TroubleshootTasks");
        RefreshButton.Content = AppText.Get("Refresh");
        ServicesManualButton.Content = AppText.Get("TroubleshootServicesManual");
        ServicesDisabledButton.Content = AppText.Get("TroubleshootServicesDisabled");
        TasksEnableButton.Content = AppText.Get("TroubleshootTasksEnable");
        TasksDisableButton.Content = AppText.Get("TroubleshootTasksDisable");
        OpenToolsButton.Content = AppText.Get("TroubleshootOpenTools");
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void ServicesManualButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ChangeServicesAsync("Manual");
    }

    private async void ServicesDisabledButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ChangeServicesAsync("Disabled");
    }

    private async void TasksEnableButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ChangeTasksAsync(true);
    }

    private async void TasksDisableButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ChangeTasksAsync(false);
    }

    private void OpenToolsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = "services.msc", UseShellExecute = true });
        Process.Start(new ProcessStartInfo { FileName = "taskschd.msc", UseShellExecute = true });
        Process.Start(new ProcessStartInfo { FileName = "eventvwr.msc", UseShellExecute = true });
    }

    private async Task RefreshAsync()
    {
        Services.Clear();
        Tasks.Clear();

        foreach (var item in (await _troubleshootingService.GetServicesAsync()).Take(60))
        {
            Services.Add(item);
        }

        foreach (var item in (await _troubleshootingService.GetScheduledTasksAsync()).Take(60))
        {
            Tasks.Add(item);
        }
    }

    private async Task ChangeServicesAsync(string mode)
    {
        foreach (var item in Services.Where(static item => item.IsSelected).ToArray())
        {
            await _troubleshootingService.SetServiceStartModeAsync(item.Name, mode);
        }

        await RefreshAsync();
    }

    private async Task ChangeTasksAsync(bool enabled)
    {
        foreach (var item in Tasks.Where(static item => item.IsSelected).ToArray())
        {
            await _troubleshootingService.SetScheduledTaskEnabledAsync(item.TaskName, item.TaskPath, enabled);
        }

        await RefreshAsync();
    }
}
