using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Optimizer.Shared.Models;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using Optimizer_Windows.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Optimizer_Windows.Pages;

public sealed partial class FeaturesPage : Page
{
    private readonly WindowsFeatureService _featureService = WindowsFeatureService.Instance;
    private readonly List<WindowsFeatureItem> _allFeatures = [];
    private CancellationTokenSource? _refreshCts;
    private bool _isRefreshing;

    public ObservableCollection<WindowsFeatureItem> VisibleFeatures { get; } = [];

    public FeaturesPage()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        HeroEyebrowText.Text = AppText.Get("FeaturesHeroEyebrow");
        PageTitleText.Text = AppText.Get("NavFeatures");
        PageSubtitleText.Text = AppText.Get("FeaturesSubtitle");
        StatusCardTitleText.Text = AppText.Get("FeaturesStatusTitle");
        RefreshButton.Content = AppText.Get("Refresh");
        OpenWindowsFeaturesButton.Content = AppText.Get("FeaturesOpenNative");
        OpenSystemRestoreButton.Content = AppText.Get("FeaturesOpenRestore");
        FeatureSearchBox.PlaceholderText = AppText.Get("FeaturesSearchPlaceholder");
        ShowAllButton.Content = AppText.Get("FeaturesFilterAll");
        FrontendFilterButton.Content = AppText.Get("FeaturesFilterFrontend");
        BackendFilterButton.Content = AppText.Get("FeaturesFilterBackend");
        GameDevFilterButton.Content = AppText.Get("FeaturesFilterGameDev");
        GamerFilterButton.Content = AppText.Get("FeaturesFilterGamer");

        EnabledMetricLabelText.Text = AppText.Get("FeaturesEnabledMetric");
        RestartMetricLabelText.Text = AppText.Get("FeaturesRestartMetric");
        HighRiskMetricLabelText.Text = AppText.Get("FeaturesHighRiskMetric");
        EnabledMetricHintText.Text = AppText.Get("FeaturesEnabledHint");
        RestartMetricHintText.Text = AppText.Get("FeaturesRestartHint");
        HighRiskMetricHintText.Text = AppText.Get("FeaturesHighRiskHint");
        ColumnNameText.Text = AppText.Get("FeaturesColumnName");
        ColumnAudienceText.Text = AppText.Get("FeaturesColumnAudience");
        ColumnStateText.Text = AppText.Get("FeaturesColumnState");
        ColumnRiskText.Text = AppText.Get("FeaturesColumnRisk");
        ColumnToggleText.Text = AppText.Get("FeaturesColumnToggle");
        EmptyStateText.Text = AppText.Get("SearchNoResults");
        StatusText.Text = AppText.Get("FeaturesReady");
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
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
            _isRefreshing = true;
            RefreshButton.IsEnabled = false;
            FeatureSearchBox.IsEnabled = false;
            StatusText.Text = AppText.Get("StatusScanningFeatures");

            var features = await _featureService.GetFeaturesAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _allFeatures.Clear();
            _allFeatures.AddRange(features);

            UpdateMetrics(features);
            ApplyFilter(FeatureSearchBox.Text);
            StatusText.Text = AppText.Format("StatusFeaturesLoaded", features.Count(feature => feature.IsAvailable));
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
            _isRefreshing = false;
            RefreshButton.IsEnabled = true;
            FeatureSearchBox.IsEnabled = true;
        }
    }

    private void UpdateMetrics(IReadOnlyList<WindowsFeatureItem> features)
    {
        EnabledMetricValueText.Text = features.Count(feature => feature.IsEnabled).ToString();
        RestartMetricValueText.Text = features.Count(feature => feature.RequiresRestart).ToString();
        HighRiskMetricValueText.Text = features.Count(feature => feature.RiskLevel == OperationRiskLevel.High).ToString();
    }

    private void FeatureSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        ApplyFilter(sender.Text);
    }

    private void ShowAllButton_Click(object sender, RoutedEventArgs e)
    {
        FeatureSearchBox.Text = string.Empty;
        ApplyFilter(string.Empty);
    }

    private void FrontendFilterButton_Click(object sender, RoutedEventArgs e) => ApplyQuickFilter(AppText.Get("FeaturesFilterFrontend"));

    private void BackendFilterButton_Click(object sender, RoutedEventArgs e) => ApplyQuickFilter(AppText.Get("FeaturesFilterBackend"));

    private void GameDevFilterButton_Click(object sender, RoutedEventArgs e) => ApplyQuickFilter(AppText.Get("FeaturesFilterGameDev"));

    private void GamerFilterButton_Click(object sender, RoutedEventArgs e) => ApplyQuickFilter(AppText.Get("FeaturesFilterGamer"));

    private void ApplyQuickFilter(string query)
    {
        FeatureSearchBox.Text = query;
        ApplyFilter(query);
    }

    private void ApplyFilter(string? rawQuery)
    {
        var query = rawQuery?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allFeatures
                .OrderByDescending(feature => feature.IsEnabled)
                .ThenBy(feature => feature.DisplayName)
                .ToList()
            : _allFeatures
                .Select(feature => new { Feature = feature, Score = Score(feature, query) })
                .Where(item => item.Score >= 0)
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.Feature.IsEnabled)
                .ThenBy(item => item.Feature.DisplayName)
                .Select(item => item.Feature)
                .ToList();

        VisibleFeatures.Clear();
        foreach (var feature in filtered)
        {
            VisibleFeatures.Add(feature);
        }

        EmptyStateText.Visibility = VisibleFeatures.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static int Score(WindowsFeatureItem feature, string query)
    {
        var haystack = $"{feature.DisplayName} {feature.Description} {feature.AudienceText} {feature.SearchKeywords}";

        if (haystack.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1000 - haystack.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        }

        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length > 1)
        {
            var matched = tokens.Count(token => haystack.Contains(token, StringComparison.OrdinalIgnoreCase));
            if (matched > 0)
            {
                return matched * 100;
            }
        }

        return IsSubsequence(query, haystack) ? 25 : -1;
    }

    private static bool IsSubsequence(string query, string haystack)
    {
        var index = 0;
        foreach (var character in haystack)
        {
            if (index < query.Length && char.ToLowerInvariant(character) == char.ToLowerInvariant(query[index]))
            {
                index++;
            }
        }

        return index == query.Length;
    }

    private async void FeatureToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing || sender is not ToggleSwitch toggle || toggle.Tag is not WindowsFeatureItem feature)
        {
            return;
        }

        try
        {
            feature.IsBusy = true;
            StatusText.Text = AppText.Format("StatusApplyingFeature", feature.DisplayName);
            await _featureService.SetFeatureStateAsync(feature.Id, feature.IsEnabled);
            StatusText.Text = AppText.Format("StatusFeatureApplied", feature.DisplayName);
        }
        catch (Exception ex)
        {
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
        finally
        {
            feature.IsBusy = false;
            await RefreshAsync();
        }
    }

    private void OpenWindowsFeaturesButton_Click(object sender, RoutedEventArgs e) => Launch("optionalfeatures.exe");

    private void OpenSystemRestoreButton_Click(object sender, RoutedEventArgs e) => Launch("SystemPropertiesProtection.exe");

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
            StatusText.Text = AppText.Format("StatusOpenFailed", ex.Message);
        }
    }
}
