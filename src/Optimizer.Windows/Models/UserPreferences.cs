namespace Optimizer_Windows.Models;

public sealed class UserPreferences
{
    public bool LaunchAtStartup { get; set; }

    public string PreferredUpdateChannelId { get; set; } = "github";

    public string CustomUpdatePrefix { get; set; } = string.Empty;
}
