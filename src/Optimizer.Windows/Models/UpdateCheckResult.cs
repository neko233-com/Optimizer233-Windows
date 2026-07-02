namespace Optimizer_Windows.Models;

public sealed class UpdateCheckResult
{
    public required UpdateManifest Manifest { get; init; }

    public required UpdateChannel Source { get; init; }

    public required bool IsUpdateAvailable { get; init; }

    public required string PreferredDownloadUrl { get; init; }
}
