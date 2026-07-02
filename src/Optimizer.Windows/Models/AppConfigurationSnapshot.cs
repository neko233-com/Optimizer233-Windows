namespace Optimizer_Windows.Models;

public sealed class AppConfigurationSnapshot
{
    public UserPreferences Preferences { get; set; } = new();

    public List<string> SelectedOptimizationActionIds { get; set; } = [];
}
