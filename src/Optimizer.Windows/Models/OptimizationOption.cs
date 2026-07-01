namespace Optimizer_Windows.Models;

public sealed class OptimizationOption
{
    public string ActionId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Badge { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
