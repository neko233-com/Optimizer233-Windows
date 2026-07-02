using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Optimizer_Windows.Services;

public sealed class UpdateService
{
    private const string RepositoryFullName = "neko233-com/Optimizer233-Windows";
    private const string OfficialManifestUrl = "https://github.com/neko233-com/Optimizer233-Windows/releases/latest/download/latest.json";

    public static UpdateService Instance { get; } = new();

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private UpdateService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Optimizer233-Windows");
    }

    public string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public IReadOnlyList<UpdateChannel> GetChannels(UserPreferences preferences)
    {
        var channels = new List<UpdateChannel>
        {
            new()
            {
                Id = "github",
                DisplayName = AppText.Get("SettingsSourceGithub"),
                Prefix = string.Empty
            },
            new()
            {
                Id = "ghfast",
                DisplayName = AppText.Get("SettingsSourceGhfast"),
                Prefix = "https://ghfast.top/"
            },
            new()
            {
                Id = "custom",
                DisplayName = AppText.Get("SettingsSourceCustom"),
                Prefix = preferences.CustomUpdatePrefix.Trim(),
                IsCustom = true
            }
        };

        return channels;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        var channels = GetChannels(preferences);
        var orderedChannels = channels
            .OrderByDescending(channel => string.Equals(channel.Id, preferences.PreferredUpdateChannelId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Exception? lastException = null;

        foreach (var channel in orderedChannels)
        {
            try
            {
                var manifestUrl = BuildMirroredUrl(channel.Prefix, OfficialManifestUrl);
                var manifest = await FetchManifestAsync(manifestUrl, cancellationToken);
                var current = ParseVersion(CurrentVersion);
                var latest = ParseVersion(manifest.Version);

                return new UpdateCheckResult
                {
                    Manifest = manifest,
                    Source = channel,
                    IsUpdateAvailable = latest > current,
                    PreferredDownloadUrl = BuildMirroredUrl(channel.Prefix, manifest.LatestDownloadUrl)
                };
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException("No update source available.", lastException);
    }

    public string GetReleasesUrl()
    {
        return $"https://github.com/{RepositoryFullName}/releases";
    }

    private async Task<UpdateManifest> FetchManifestAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, cancellationToken: cancellationToken);
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("Invalid update manifest.");
        }

        return manifest;
    }

    private static Version ParseVersion(string value)
    {
        var normalized = value.Trim().TrimStart('v', 'V');
        return Version.TryParse(normalized, out var version) ? version : new Version(0, 0, 0);
    }

    private static string BuildMirroredUrl(string prefix, string url)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return url;
        }

        var normalizedPrefix = prefix.Trim();
        if (!normalizedPrefix.EndsWith('/'))
        {
            normalizedPrefix += "/";
        }

        return normalizedPrefix + url;
    }
}
