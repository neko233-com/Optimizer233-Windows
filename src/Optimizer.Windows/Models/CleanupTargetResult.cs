namespace Optimizer_Windows.Models;

public sealed class CleanupTargetResult
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public long FileCount { get; set; }

    public long TotalSizeBytes { get; set; }

    public string FileCountLabel => $"{FileCount:N0}";

    public string SizeLabel => Helpers.FileSizeFormatter.Format(TotalSizeBytes);

    public string FileCountCaption => Localization.AppText.Get("CleanupLegendFiles");

    public string SizeCaption => Localization.AppText.Get("CleanupLegendSize");

    public string ActionLabel => Localization.AppText.Get("CleanupActionThis");
}
