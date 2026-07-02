namespace Optimizer_Windows.Models;

public sealed class WslStatusSnapshot
{
    public bool IsAvailable { get; init; }

    public string DefaultDistribution { get; init; } = string.Empty;

    public string DefaultVersion { get; init; } = string.Empty;

    public int DistroCount { get; init; }

    public int RunningCount { get; init; }

    public string StatusSummary { get; init; } = string.Empty;

    public string RawStatus { get; init; } = string.Empty;

    public IReadOnlyList<WslDistroInfo> Distros { get; init; } = [];
}
