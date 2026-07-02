namespace Optimizer_Windows.Models;

public sealed class AppxPackageInfo
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string PackageFullName { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
