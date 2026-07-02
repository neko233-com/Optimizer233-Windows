using Optimizer_Windows.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Optimizer_Windows.Services;

public sealed class TroubleshootingService
{
    public static TroubleshootingService Instance { get; } = new();

    private TroubleshootingService()
    {
    }

    public async Task<IReadOnlyList<ServiceItemInfo>> GetServicesAsync()
    {
        const string script = """
$ProgressPreference='SilentlyContinue'
Get-CimInstance Win32_Service |
  Sort-Object DisplayName |
  Select-Object Name, DisplayName, StartMode, State |
  ConvertTo-Json -Depth 3
""";

        var json = await RunPowerShellAsync(script);
        return DeserializeList<ServiceItemInfo>(json);
    }

    public async Task<IReadOnlyList<ScheduledTaskInfo>> GetScheduledTasksAsync()
    {
        const string script = """
$ProgressPreference='SilentlyContinue'
Get-ScheduledTask |
  Sort-Object TaskPath, TaskName |
  Select-Object TaskName, TaskPath, State, Author |
  ConvertTo-Json -Depth 3
""";

        var json = await RunPowerShellAsync(script);
        return DeserializeList<ScheduledTaskInfo>(json);
    }

    public Task SetServiceStartModeAsync(string serviceName, string startupType)
    {
        var script = $"Set-Service -Name '{EscapeSingleQuotes(serviceName)}' -StartupType {startupType}";
        return RunPowerShellAsync(script);
    }

    public Task SetScheduledTaskEnabledAsync(string taskName, string taskPath, bool enabled)
    {
        var verb = enabled ? "Enable-ScheduledTask" : "Disable-ScheduledTask";
        var script = $"{verb} -TaskName '{EscapeSingleQuotes(taskName)}' -TaskPath '{EscapeSingleQuotes(taskPath)}'";
        return RunPowerShellAsync(script);
    }

    private static async Task<string> RunPowerShellAsync(string script)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PowerShell.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "PowerShell command failed." : stderr.Trim());
        }

        return stdout.Trim();
    }

    private static IReadOnlyList<T> DeserializeList<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        if (json.TrimStart().StartsWith('['))
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? [];
        }

        var single = JsonSerializer.Deserialize<T>(json);
        return single is null ? [] : [single];
    }

    private static string EscapeSingleQuotes(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
