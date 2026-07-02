using Optimizer_Windows.Models;
using System.Text.Json;

namespace Optimizer_Windows.Services;

public sealed class AppConfigurationService
{
    public static AppConfigurationService Instance { get; } = new();

    private readonly UserPreferencesService _preferencesService = UserPreferencesService.Instance;
    private readonly OptimizationStateService _optimizationStateService = OptimizationStateService.Instance;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    private AppConfigurationService()
    {
    }

    public AppConfigurationSnapshot CreateSnapshot()
    {
        return new AppConfigurationSnapshot
        {
            Preferences = _preferencesService.Load(),
            SelectedOptimizationActionIds = _optimizationStateService.GetSelectedActionIds().ToList()
        };
    }

    public async Task ExportAsync(string path)
    {
        var snapshot = CreateSnapshot();
        var json = JsonSerializer.Serialize(snapshot, _serializerOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task ImportAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var snapshot = JsonSerializer.Deserialize<AppConfigurationSnapshot>(json) ?? new AppConfigurationSnapshot();
        await _preferencesService.SaveAsync(snapshot.Preferences);
        _optimizationStateService.SetSelectedActionIds(snapshot.SelectedOptimizationActionIds);
    }

    public async Task ResetAsync()
    {
        await _preferencesService.SaveAsync(new UserPreferences());
        _optimizationStateService.Reset();
    }
}
