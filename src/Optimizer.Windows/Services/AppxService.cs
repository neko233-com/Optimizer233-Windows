using Optimizer_Windows.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Optimizer_Windows.Services;

public sealed class AppxService
{
    public static AppxService Instance { get; } = new();

    private AppxService()
    {
    }

    public async Task<IReadOnlyList<AppxPackageInfo>> GetInstalledPackagesAsync()
    {
        const string script = """
$ProgressPreference='SilentlyContinue'
Get-AppxPackage |
    Sort-Object Name |
    Select-Object Name,
                  @{Name='DisplayName';Expression={$_.Name}},
                  Publisher,
                  @{Name='Version';Expression={$_.Version.ToString()}},
                  PackageFullName |
    ConvertTo-Json -Depth 3
""";

        var json = await RunPowerShellAsync(script);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        if (json.TrimStart().StartsWith('['))
        {
            return JsonSerializer.Deserialize<List<AppxPackageInfo>>(json) ?? [];
        }

        var single = JsonSerializer.Deserialize<AppxPackageInfo>(json);
        return single is null ? [] : [single];
    }

    public async Task RemovePackageAsync(string packageFullName)
    {
        var escaped = packageFullName.Replace("'", "''", StringComparison.Ordinal);
        var script = $"Remove-AppxPackage -Package '{escaped}'";
        await RunPowerShellAsync(script);
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
}
