using Optimizer_Windows.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Optimizer_Windows.Services;

public sealed class SecurityService
{
    public static SecurityService Instance { get; } = new();

    private SecurityService()
    {
    }

    public async Task<SecurityStatusInfo> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        const string script = """
$ProgressPreference='SilentlyContinue'
$mp = Get-MpComputerStatus
$fw = Get-NetFirewallProfile | Sort-Object Name
[PSCustomObject]@{
  AntivirusEnabled = [bool]$mp.AntivirusEnabled
  RealTimeProtectionEnabled = [bool]$mp.RealTimeProtectionEnabled
  FirewallDomainEnabled = [bool](($fw | Where-Object Name -eq 'Domain').Enabled)
  FirewallPrivateEnabled = [bool](($fw | Where-Object Name -eq 'Private').Enabled)
  FirewallPublicEnabled = [bool](($fw | Where-Object Name -eq 'Public').Enabled)
  AntivirusEngineVersion = [string]$mp.AMEngineVersion
  LastQuickScanTime = if ($mp.QuickScanStartTime) { $mp.QuickScanStartTime.ToString('yyyy-MM-dd HH:mm:ss') } else { '' }
  TamperProtected = [bool]$mp.IsTamperProtected
} | ConvertTo-Json -Depth 3
""";

        var json = await RunPowerShellAsync(script, cancellationToken);
        return JsonSerializer.Deserialize<SecurityStatusInfo>(json) ?? new SecurityStatusInfo();
    }

    public Task SetRealtimeProtectionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var script = $"Set-MpPreference -DisableRealtimeMonitoring ${(!enabled).ToString().ToLowerInvariant()}";
        return RunPowerShellAsync(script, cancellationToken);
    }

    public Task SetFirewallProfileAsync(string profileName, bool enabled, CancellationToken cancellationToken = default)
    {
        var state = enabled ? "True" : "False";
        var script = $"Set-NetFirewallProfile -Profile {profileName} -Enabled {state}";
        return RunPowerShellAsync(script, cancellationToken);
    }

    private static async Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken = default)
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
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }
        });
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "Security query failed." : stderr.Trim());
        }

        return stdout.Trim();
    }
}
