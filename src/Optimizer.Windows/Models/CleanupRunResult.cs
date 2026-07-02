namespace Optimizer_Windows.Models;

public sealed class CleanupRunResult
{
    public required long DeletedFileCount { get; init; }

    public required long FreedBytes { get; init; }

    public required long FailedItemCount { get; init; }
}
