namespace Optimizer.Shared.Models;

public sealed class OperationDescriptor
{
    public string Id { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public OperationRiskLevel RiskLevel { get; init; }

    public string CliExample { get; init; } = string.Empty;
}
