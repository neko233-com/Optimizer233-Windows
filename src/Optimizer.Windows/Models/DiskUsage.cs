namespace Optimizer_Windows.Models;

public sealed class DiskUsage
{
    public string Name { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public int UsagePercent { get; set; }

    public string UsageLabel { get; set; } = string.Empty;
}
