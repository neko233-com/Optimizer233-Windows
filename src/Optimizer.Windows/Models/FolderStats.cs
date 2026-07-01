namespace Optimizer_Windows.Models;

public sealed class FolderStats
{
    public required string Path { get; init; }

    public required long FileCount { get; init; }

    public required long TotalSizeBytes { get; init; }
}
