namespace Optimizer_Windows.Models;

public sealed class ServiceItemInfo
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string StartMode { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
