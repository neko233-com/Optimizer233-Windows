using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Diagnostics;
using Windows.UI;

namespace Optimizer_Windows.Pages;

public sealed partial class SecurityPage : Page
{
    private readonly SecurityService _securityService = SecurityService.Instance;
    private bool _isInitializing;

    public SecurityPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = AppText.Get("NavSecurity");
        PageSubtitleText.Text = AppText.Get("SecuritySubtitle");
        StatusCardTitleText.Text = AppText.Get("SecurityProtectionCard");
        SwitchesCardTitleText.Text = AppText.Get("SecuritySwitchesCard");
        SwitchHintText.Text = AppText.Get("SecuritySwitchHint");
        ToggleSearchBox.PlaceholderText = AppText.Get("SecurityToggleSearchPlaceholder");
        DetailsCardTitleText.Text = AppText.Get("SecurityDetailsCard");
        ActionsCardTitleText.Text = AppText.Get("SecurityActionsCard");

        RefreshButton.Content = AppText.Get("Refresh");
        AntivirusLabelText.Text = AppText.Get("SecurityAntivirus");
        RealtimeLabelText.Text = AppText.Get("SecurityRealtime");
        FirewallLabelText.Text = AppText.Get("SecurityFirewall");

        RealtimeToggleLabelText.Text = AppText.Get("SecurityRealtimeSwitch");
        DomainToggleLabelText.Text = AppText.Get("SecurityDomain");
        PrivateToggleLabelText.Text = AppText.Get("SecurityPrivate");
        PublicToggleLabelText.Text = AppText.Get("SecurityPublic");

        OpenSecurityButton.Content = AppText.Get("SecurityOpenApp");
        OpenFirewallButton.Content = AppText.Get("SecurityOpenFirewall");
        OpenUpdateButton.Content = AppText.Get("SecurityOpenUpdate");
        StatusText.Text = AppText.Get("SecurityReady");
    }

    private async void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void ToggleSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ApplyToggleFilter(sender.Text);
    }

    private async Task RefreshAsync()
    {
        try
        {
            StatusText.Text = AppText.Get("SecurityLoading");
            RefreshButton.IsEnabled = false;
            _isInitializing = true;

            var status = await _securityService.GetStatusAsync();
            ApplyStatusCard(AntivirusValueText, AntivirusHintText, status.AntivirusEnabled, AppText.Get("SecurityAntivirus"));
            ApplyStatusCard(RealtimeValueText, RealtimeHintText, status.RealTimeProtectionEnabled, AppText.Get("SecurityRealtime"));

            var firewallEnabledCount = new[] { status.FirewallDomainEnabled, status.FirewallPrivateEnabled, status.FirewallPublicEnabled }.Count(value => value);
            FirewallValueText.Text = $"{firewallEnabledCount}/3";
            FirewallHintText.Text = $"{AppText.Get("SecurityDomain")} {BoolText(status.FirewallDomainEnabled)} · {AppText.Get("SecurityPrivate")} {BoolText(status.FirewallPrivateEnabled)} · {AppText.Get("SecurityPublic")} {BoolText(status.FirewallPublicEnabled)}";
            FirewallValueText.Foreground = CreateStateBrush(firewallEnabledCount > 0);

            RealtimeToggle.IsOn = status.RealTimeProtectionEnabled;
            DomainToggle.IsOn = status.FirewallDomainEnabled;
            PrivateToggle.IsOn = status.FirewallPrivateEnabled;
            PublicToggle.IsOn = status.FirewallPublicEnabled;

            RealtimeToggle.IsEnabled = !status.TamperProtected;
            SwitchHintText.Text = status.TamperProtected
                ? $"{AppText.Get("SecuritySwitchHint")} Tamper Protection=On."
                : AppText.Get("SecuritySwitchHint");

            EngineText.Text = $"{AppText.Get("SecurityEngine")}: {Fallback(status.AntivirusEngineVersion)}";
            LastScanText.Text = $"{AppText.Get("SecurityLastScan")}: {Fallback(status.LastQuickScanTime)}";
            StatusText.Text = AppText.Get("SecurityLoaded");
            ApplyToggleFilter(ToggleSearchBox.Text);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusLoadFailed", ex.Message);
        }
        finally
        {
            _isInitializing = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private async void RealtimeToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        await ApplyToggleAsync(() => _securityService.SetRealtimeProtectionAsync(RealtimeToggle.IsOn), "SecurityRealtimeUpdated");
    }

    private async void DomainToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        await ApplyToggleAsync(() => _securityService.SetFirewallProfileAsync("Domain", DomainToggle.IsOn), "SecurityFirewallUpdated");
    }

    private async void PrivateToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        await ApplyToggleAsync(() => _securityService.SetFirewallProfileAsync("Private", PrivateToggle.IsOn), "SecurityFirewallUpdated");
    }

    private async void PublicToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        await ApplyToggleAsync(() => _securityService.SetFirewallProfileAsync("Public", PublicToggle.IsOn), "SecurityFirewallUpdated");
    }

    private async Task ApplyToggleAsync(Func<Task> action, string successKey)
    {
        try
        {
            ToggleSwitchesEnabled(false);
            await action();
            StatusText.Text = AppText.Get(successKey);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
        finally
        {
            await RefreshAsync();
        }
    }

    private void ToggleSwitchesEnabled(bool enabled)
    {
        RealtimeToggle.IsEnabled = enabled;
        DomainToggle.IsEnabled = enabled;
        PrivateToggle.IsEnabled = enabled;
        PublicToggle.IsEnabled = enabled;
    }

    private void ApplyToggleFilter(string? rawQuery)
    {
        var query = rawQuery?.Trim() ?? string.Empty;
        ApplyCardVisibility(RealtimeToggleCard, query, RealtimeToggleLabelText.Text, "realtime defender protection");
        ApplyCardVisibility(DomainToggleCard, query, DomainToggleLabelText.Text, "domain firewall network");
        ApplyCardVisibility(PrivateToggleCard, query, PrivateToggleLabelText.Text, "private firewall network");
        ApplyCardVisibility(PublicToggleCard, query, PublicToggleLabelText.Text, "public firewall network");
    }

    private static void ApplyCardVisibility(Border border, string query, params string[] haystacks)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            border.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        var matched = haystacks.Any(text => text.Contains(query, StringComparison.OrdinalIgnoreCase));
        border.Visibility = matched ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void ApplyStatusCard(TextBlock valueText, TextBlock hintText, bool enabled, string label)
    {
        valueText.Text = BoolText(enabled);
        valueText.Foreground = CreateStateBrush(enabled);
        hintText.Text = enabled
            ? $"{label} {AppText.Get("SecurityOn")}"
            : $"{label} {AppText.Get("SecurityOff")}";
    }

    private string BoolText(bool value) => value ? AppText.Get("SecurityOn") : AppText.Get("SecurityOff");

    private static string Fallback(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? AppText.Get("CommonUnavailable") : value;
    }

    private static Brush CreateStateBrush(bool enabled)
    {
        return new SolidColorBrush(enabled
            ? Color.FromArgb(255, 176, 255, 198)
            : Color.FromArgb(255, 255, 180, 188));
    }

    private void OpenSecurityButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("windowsdefender:");

    private void OpenFirewallButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("ms-settings:windowsdefender-firewall");

    private void OpenUpdateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Launch("ms-settings:windowsupdate");

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
