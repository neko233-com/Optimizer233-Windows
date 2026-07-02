namespace Optimizer_Windows.Models;

public sealed class ScheduledTaskInfo
{
    public string TaskName { get; set; } = string.Empty;

    public string TaskPath { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
