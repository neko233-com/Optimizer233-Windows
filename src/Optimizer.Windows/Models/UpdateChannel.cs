namespace Optimizer_Windows.Models;

public sealed class UpdateChannel
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Prefix { get; init; } = string.Empty;

    public bool IsCustom { get; init; }
}
