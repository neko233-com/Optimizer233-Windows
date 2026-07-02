using Microsoft.Win32;

namespace Optimizer_Windows.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Optimizer233-Windows";

    public static StartupService Instance { get; } = new();

    private StartupService()
    {
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? throw new InvalidOperationException("Executable path unavailable.");
            key.SetValue(ValueName, $"\"{exePath}\"");
            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
