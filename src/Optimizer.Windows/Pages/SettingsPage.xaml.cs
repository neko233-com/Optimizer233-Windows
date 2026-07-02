using Microsoft.UI.Xaml.Controls;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Diagnostics;
using System.Globalization;

namespace Optimizer_Windows.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly UserPreferencesService _preferencesService = UserPreferencesService.Instance;
    private readonly UpdateService _updateService = UpdateService.Instance;
    private readonly StartupService _startupService = StartupService.Instance;

    private UserPreferences _preferences = new();
    private UpdateCheckResult? _lastUpdateResult;
    private bool _isInitializing;

    public SettingsPage()
    {
        InitializeComponent();
        ApplyLocalization();
        LoadPreferences();
    }

    private void ApplyLocalization()
    {
        PageEyebrowText.Text = AppText.Get("NavSettings").ToUpperInvariant();
        PageTitleText.Text = AppText.Get("SettingsTitle");
        PageSubtitleText.Text = AppText.Get("SettingsSubtitle");
        BehaviorTitleText.Text = AppText.Get("SettingsBehaviorTitle");
        StartupTitleText.Text = AppText.Get("SettingsStartupTitle");
        StartupBodyText.Text = AppText.Get("SettingsStartupBody");
        LanguageTitleText.Text = AppText.Get("SettingsLanguageTitle");
        LanguageBodyText.Text = AppText.Get("SettingsLanguageBody");
        UpdateTitleText.Text = AppText.Get("SettingsUpdateTitle");
        UpdateBodyText.Text = AppText.Get("SettingsUpdateBody");
        CurrentVersionLabelText.Text = AppText.Get("SettingsCurrentVersion");
        LatestVersionLabelText.Text = AppText.Get("SettingsLatestVersion");
        UpdateSourceLabelText.Text = AppText.Get("SettingsUpdateSource");
        CustomMirrorLabelText.Text = AppText.Get("SettingsCustomMirror");
        CustomMirrorPrefixBox.PlaceholderText = AppText.Get("SettingsCustomMirrorPlaceholder");
        CustomMirrorHintText.Text = AppText.Get("SettingsCustomMirrorHint");
        SaveSourceButton.Content = AppText.Get("SettingsApplySource");
        CheckUpdatesButton.Content = AppText.Get("SettingsCheckUpdates");
        DownloadUpdateButton.Content = AppText.Get("SettingsDownloadUpdate");
        OpenReleasesButton.Content = AppText.Get("SettingsOpenReleases");
        OpenNotesButton.Content = AppText.Get("SettingsOpenNotes");
        LatestVersionValueText.Text = AppText.Get("CommonNotChecked");
        CurrentVersionValueText.Text = _updateService.CurrentVersion;
        LanguageValueText.Text = $"{AppText.Get("SettingsCurrentLanguage")}: {CultureInfo.CurrentUICulture.NativeName}";
        UpdateStatusText.Text = AppText.Get("CommonNotChecked");
    }

    private void LoadPreferences()
    {
        _isInitializing = true;

        _preferences = _preferencesService.Load();
        _preferences.LaunchAtStartup = _startupService.IsEnabled();

        StartupToggle.IsOn = _preferences.LaunchAtStartup;
        CustomMirrorPrefixBox.Text = _preferences.CustomUpdatePrefix;

        var channels = _updateService.GetChannels(_preferences);
        UpdateSourceComboBox.ItemsSource = channels;
        UpdateSourceComboBox.SelectedValue = channels.Any(channel => channel.Id == _preferences.PreferredUpdateChannelId)
            ? _preferences.PreferredUpdateChannelId
            : "github";
        UpdateCustomMirrorVisibility();

        _isInitializing = false;
    }

    private async void StartupToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        try
        {
            _preferences.LaunchAtStartup = StartupToggle.IsOn;
            _startupService.SetEnabled(_preferences.LaunchAtStartup);
            await _preferencesService.SaveAsync(_preferences);
            UpdateStatusText.Text = AppText.Get(_preferences.LaunchAtStartup ? "StatusStartupEnabled" : "StatusStartupDisabled");
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private async void SaveSourceButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            _preferences.CustomUpdatePrefix = CustomMirrorPrefixBox.Text.Trim();
            _preferences.PreferredUpdateChannelId = UpdateSourceComboBox.SelectedValue as string ?? "github";

            if (_preferences.PreferredUpdateChannelId == "custom" && string.IsNullOrWhiteSpace(_preferences.CustomUpdatePrefix))
            {
                UpdateStatusText.Text = AppText.Get("SettingsCustomMirrorHint");
                return;
            }

            await _preferencesService.SaveAsync(_preferences);
            LoadPreferences();

            var selectedChannel = UpdateSourceComboBox.SelectedItem as UpdateChannel;
            UpdateStatusText.Text = AppText.Format("StatusSourceSaved", selectedChannel?.DisplayName ?? AppText.Get("SettingsSourceGithub"));
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }

    private void UpdateSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateCustomMirrorVisibility();
    }

    private void UpdateCustomMirrorVisibility()
    {
        var selectedId = UpdateSourceComboBox.SelectedValue as string ?? "github";
        CustomMirrorPanel.Visibility = selectedId == "custom"
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private async void CheckUpdatesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            UpdateStatusText.Text = AppText.Get("StatusCheckingUpdates");
            CheckUpdatesButton.IsEnabled = false;
            DownloadUpdateButton.IsEnabled = false;
            OpenNotesButton.IsEnabled = false;

            _lastUpdateResult = await _updateService.CheckForUpdatesAsync(_preferences);
            LatestVersionValueText.Text = _lastUpdateResult.Manifest.Version;
            DownloadUpdateButton.IsEnabled = _lastUpdateResult.IsUpdateAvailable;
            OpenNotesButton.IsEnabled = true;

            UpdateStatusText.Text = _lastUpdateResult.IsUpdateAvailable
                ? AppText.Format("StatusUpdateReady", _lastUpdateResult.Manifest.Version, _lastUpdateResult.Source.DisplayName)
                : AppText.Get("StatusAlreadyLatest");
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = AppText.Format("StatusLoadFailed", ex.Message);
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    private void DownloadUpdateButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_lastUpdateResult is null)
        {
            return;
        }

        Launch(_lastUpdateResult.PreferredDownloadUrl);
    }

    private void OpenReleasesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Launch(_updateService.GetReleasesUrl());
    }

    private void OpenNotesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_lastUpdateResult is null)
        {
            return;
        }

        Launch(_lastUpdateResult.Manifest.ReleaseNotesUrl);
    }

    private void Launch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }
}
