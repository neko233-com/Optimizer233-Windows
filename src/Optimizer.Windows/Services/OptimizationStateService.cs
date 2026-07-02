namespace Optimizer_Windows.Services;

public sealed class OptimizationStateService
{
    public static OptimizationStateService Instance { get; } = new();

    private readonly HashSet<string> _selectedActionIds = new(StringComparer.OrdinalIgnoreCase);

    private OptimizationStateService()
    {
    }

    public IReadOnlyCollection<string> GetSelectedActionIds()
    {
        return _selectedActionIds.ToArray();
    }

    public void SetSelectedActionIds(IEnumerable<string> actionIds)
    {
        _selectedActionIds.Clear();
        foreach (var actionId in actionIds.Where(static id => !string.IsNullOrWhiteSpace(id)))
        {
            _selectedActionIds.Add(actionId);
        }
    }

    public void Reset()
    {
        _selectedActionIds.Clear();
    }
}
