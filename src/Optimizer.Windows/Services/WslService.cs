using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Optimizer_Windows.Services;

public sealed class WslService
{
    public static WslService Instance { get; } = new();

    private WslService()
    {
    }

    public async Task<WslStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var statusTask = RunWslCommandAsync("--status", requiresElevation: false, cancellationToken, tolerateFailure: true);
        var listTask = RunWslCommandAsync("--list --verbose", requiresElevation: false, cancellationToken, tolerateFailure: true);
        await Task.WhenAll(statusTask, listTask);

        var statusOutput = await statusTask;
        var listOutput = await listTask;
        var distros = ParseDistros(listOutput).ToArray();
        var defaultDistribution = ExtractStatusValue(statusOutput, "默认分发", "default distribution");
        var defaultVersion = ExtractStatusValue(statusOutput, "默认版本", "default version");

        return new WslStatusSnapshot
        {
            IsAvailable = !string.IsNullOrWhiteSpace(statusOutput) || distros.Length > 0,
            DefaultDistribution = string.IsNullOrWhiteSpace(defaultDistribution) ? AppText.Get("WslDefaultDistroNone") : defaultDistribution,
            DefaultVersion = string.IsNullOrWhiteSpace(defaultVersion) ? AppText.Get("WslDefaultVersionNone") : defaultVersion,
            DistroCount = distros.Length,
            RunningCount = distros.Count(distro => distro.State.Contains("running", StringComparison.OrdinalIgnoreCase) || distro.State.Contains("运行", StringComparison.OrdinalIgnoreCase)),
            StatusSummary = BuildSummary(statusOutput, distros.Length),
            RawStatus = string.IsNullOrWhiteSpace(statusOutput) ? AppText.Get("CommonUnavailable") : statusOutput,
            Distros = distros
        };
    }

    public Task InstallAsync(CancellationToken cancellationToken = default)
    {
        return RunWslCommandAsync("--install --no-distribution", requiresElevation: true, cancellationToken);
    }

    public Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        return RunWslCommandAsync("--update", requiresElevation: false, cancellationToken);
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        return RunWslCommandAsync("--shutdown", requiresElevation: false, cancellationToken);
    }

    public void OpenShell()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "wsl.exe",
            UseShellExecute = true
        });
    }

    public void OpenDocs()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://learn.microsoft.com/windows/wsl/",
            UseShellExecute = true
        });
    }

    private static string BuildSummary(string statusOutput, int distroCount)
    {
        if (string.IsNullOrWhiteSpace(statusOutput) && distroCount == 0)
        {
            return AppText.Get("CommonUnavailable");
        }

        return AppText.Format("WslSummary", distroCount.ToString());
    }

    private static IEnumerable<WslDistroInfo> ParseDistros(string rawOutput)
    {
        foreach (var line in NormalizeOutput(rawOutput)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.Contains("NAME", StringComparison.OrdinalIgnoreCase) || line.Contains("名称", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isDefault = line.TrimStart().StartsWith('*');
            var normalized = line.Replace("*", string.Empty, StringComparison.Ordinal).Trim();
            var match = Regex.Match(normalized, @"^(?<name>.+?)\s{2,}(?<state>.+?)\s{2,}(?<version>\d+)$");
            if (!match.Success)
            {
                continue;
            }

            yield return new WslDistroInfo
            {
                Name = match.Groups["name"].Value.Trim(),
                State = match.Groups["state"].Value.Trim(),
                Version = int.TryParse(match.Groups["version"].Value, out var version) ? version : 0,
                IsDefault = isDefault
            };
        }
    }

    private static string ExtractStatusValue(string output, string zhKey, string enKey)
    {
        foreach (var line in NormalizeOutput(output)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.Contains(zhKey, StringComparison.OrdinalIgnoreCase) || line.Contains(enKey, StringComparison.OrdinalIgnoreCase))
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex >= 0 && separatorIndex < line.Length - 1)
                {
                    return line[(separatorIndex + 1)..].Trim();
                }
            }
        }

        return string.Empty;
    }

    private static async Task<string> RunWslCommandAsync(string arguments, bool requiresElevation, CancellationToken cancellationToken, bool tolerateFailure = false)
    {
        using var process = StartProcess(arguments, requiresElevation);
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

        if (requiresElevation)
        {
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 && !tolerateFailure)
            {
                throw new InvalidOperationException($"WSL command failed with exit code {process.ExitCode}.");
            }

            return string.Empty;
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 && !tolerateFailure)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "WSL command failed." : NormalizeOutput(stderr));
        }

        return NormalizeOutput(string.IsNullOrWhiteSpace(stdout) ? stderr : stdout);
    }

    private static Process StartProcess(string arguments, bool requiresElevation)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = arguments,
            UseShellExecute = requiresElevation,
            Verb = requiresElevation ? "runas" : string.Empty,
            RedirectStandardOutput = !requiresElevation,
            RedirectStandardError = !requiresElevation,
            CreateNoWindow = !requiresElevation
        };

        if (!requiresElevation)
        {
            startInfo.StandardOutputEncoding = Encoding.Unicode;
            startInfo.StandardErrorEncoding = Encoding.Unicode;
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start wsl.exe.");
    }

    private static string NormalizeOutput(string raw)
    {
        return raw
            .Replace("\u0000", string.Empty, StringComparison.Ordinal)
            .Replace("\uFEFF", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Trim();
    }
}
