namespace Optimizer_Windows.Models;

public sealed class CleanupTarget
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Path { get; init; }
}
