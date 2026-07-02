namespace Optimizer_Windows.Models;

public sealed class SearchCommand
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TargetTag { get; set; } = string.Empty;

    public string LaunchTarget { get; set; } = string.Empty;

    public string Keywords { get; set; } = string.Empty;

    public string Subtitle => string.IsNullOrWhiteSpace(Description) ? Title : $"{Title} · {Description}";

    public override string ToString()
    {
        return Subtitle;
    }
}
