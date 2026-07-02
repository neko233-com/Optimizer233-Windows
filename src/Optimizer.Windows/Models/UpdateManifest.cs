namespace Optimizer_Windows.Models;

public sealed class UpdateManifest
{
    public string AppId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string ReleaseTag { get; set; } = string.Empty;

    public string PublishedAt { get; set; } = string.Empty;

    public string ReleaseNotesUrl { get; set; } = string.Empty;

    public string VersionedDownloadUrl { get; set; } = string.Empty;

    public string LatestDownloadUrl { get; set; } = string.Empty;
}
