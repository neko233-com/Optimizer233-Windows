using Microsoft.UI.Xaml;

namespace Optimizer_Windows.Models;

public sealed class WslDistroInfo
{
    public string Name { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public int Version { get; set; }

    public bool IsDefault { get; set; }

    public string StateLabel => string.IsNullOrWhiteSpace(State) ? "Unknown" : State;

    public string VersionLabel => $"WSL {Version}";

    public Visibility DefaultBadgeVisibility => IsDefault ? Visibility.Visible : Visibility.Collapsed;
}
