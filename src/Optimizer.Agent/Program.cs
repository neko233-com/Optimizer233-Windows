using Microsoft.Win32;
using Optimizer.Shared;
using Optimizer.Shared.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Optimizer233-Windows");
Directory.CreateDirectory(appDataRoot);

var argsList = args.ToList();
var jsonOutput = argsList.Remove("--json");

if (argsList.Count == 0 || argsList[0] is "help" or "--help" or "-h")
{
    WriteResult(new
    {
        ok = true,
        usage = new[]
        {
            "Optimizer233.Agent list [--json]",
            "Optimizer233.Agent get <operation-id> [--json]",
            "Optimizer233.Agent exec <operation-id> [options] [--json]"
        }
    });
    return;
}

var command = argsList[0].ToLowerInvariant();

switch (command)
{
    case "list":
        WriteResult(new
        {
            ok = true,
            operations = OperationCatalog.All
        });
        return;

    case "get":
        if (argsList.Count < 2)
        {
            Fail("Missing operation id.");
            return;
        }

        var descriptor = OperationCatalog.Find(argsList[1]);
        if (descriptor is null)
        {
            Fail($"Unknown operation id: {argsList[1]}");
            return;
        }

        WriteResult(new { ok = true, operation = descriptor });
        return;

    case "exec":
        if (argsList.Count < 2)
        {
            Fail("Missing operation id.");
            return;
        }

        var operationId = argsList[1];
        var operation = OperationCatalog.Find(operationId);
        if (operation is null)
        {
            Fail($"Unknown operation id: {operationId}");
            return;
        }

        var options = ParseOptions(argsList.Skip(2).ToArray());

        try
        {
            var result = await ExecuteAsync(operationId, options);
            WriteResult(new { ok = true, operation, result });
        }
        catch (Exception ex)
        {
            Fail(ex.Message, operation);
        }
        return;
}

Fail($"Unknown command: {command}");
return;

async Task<object?> ExecuteAsync(string operationId, Dictionary<string, string> options)
{
    switch (operationId.ToLowerInvariant())
    {
        case "storage-sense":
            Launch("ms-settings:storagepolicies");
            return new { launched = "ms-settings:storagepolicies" };
        case "disk-cleanup":
            Launch("cleanmgr.exe");
            return new { launched = "cleanmgr.exe" };
        case "temp-folder":
            Launch(Path.GetTempPath());
            return new { launched = Path.GetTempPath() };
        case "startup-apps":
            Launch("ms-settings:startupapps");
            return new { launched = "ms-settings:startupapps" };
        case "task-manager":
            Launch("taskmgr.exe");
            return new { launched = "taskmgr.exe" };
        case "windows-update":
            Launch("ms-settings:windowsupdate");
            return new { launched = "ms-settings:windowsupdate" };
        case "power-settings":
            Launch("ms-settings:powersleep");
            return new { launched = "ms-settings:powersleep" };
        case "cleanup-scan":
            return ScanCleanupTargets();
        case "cleanup-run":
            return CleanCleanupTargets();
        case "startup-toggle":
            SetStartupEnabled(ParseBool(GetRequiredOption(options, "enabled")));
            return new { enabled = GetStartupEnabled() };
        case "config-export":
        {
            var path = options.TryGetValue("path", out var exportPath)
                ? exportPath
                : Path.Combine(appDataRoot, "optimizer233-config.json");
            await ExportConfigAsync(path);
            return new { path };
        }
        case "config-import":
        {
            var path = GetRequiredOption(options, "path");
            await ImportConfigAsync(path);
            return new { path };
        }
        case "config-reset":
            await ResetConfigAsync();
            return new { reset = true };
        case "appx-list":
            return await RunJsonPowerShellAsync("""
$ProgressPreference='SilentlyContinue'
Get-AppxPackage |
  Sort-Object Name |
  Select-Object Name, Publisher, @{Name='Version';Expression={$_.Version.ToString()}}, PackageFullName |
  ConvertTo-Json -Depth 3
""");
        case "appx-remove":
            await RunPowerShellAsync($"Remove-AppxPackage -Package '{EscapeSingleQuotes(GetRequiredOption(options, "package"))}'");
            return new { removed = true };
        case "security-status":
            return await RunJsonPowerShellAsync("""
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
} | ConvertTo-Json -Depth 3
""");
        case "office-list":
            return GetInstalledOfficeProducts();
        case "service-list":
            return await RunJsonPowerShellAsync("""
$ProgressPreference='SilentlyContinue'
Get-CimInstance Win32_Service |
  Sort-Object DisplayName |
  Select-Object Name, DisplayName, StartMode, State |
  ConvertTo-Json -Depth 3
""");
        case "service-set-startup":
            await RunPowerShellAsync($"Set-Service -Name '{EscapeSingleQuotes(GetRequiredOption(options, "name"))}' -StartupType {GetRequiredOption(options, "mode")}");
            return new { updated = true };
        case "task-list":
            return await RunJsonPowerShellAsync("""
$ProgressPreference='SilentlyContinue'
Get-ScheduledTask |
  Sort-Object TaskPath, TaskName |
  Select-Object TaskName, TaskPath, State, Author |
  ConvertTo-Json -Depth 3
""");
        case "task-set-enabled":
        {
            var enabled = ParseBool(GetRequiredOption(options, "enabled"));
            var verb = enabled ? "Enable-ScheduledTask" : "Disable-ScheduledTask";
            await RunPowerShellAsync($"{verb} -TaskName '{EscapeSingleQuotes(GetRequiredOption(options, "name"))}' -TaskPath '{EscapeSingleQuotes(GetRequiredOption(options, "path"))}'");
            return new { updated = true, enabled };
        }
        case "update-check":
            return new
            {
                manifest = "https://github.com/neko233-com/Optimizer233-Windows/releases/latest/download/latest.json",
                releasePage = "https://github.com/neko233-com/Optimizer233-Windows/releases"
            };
        default:
            throw new InvalidOperationException($"Unsupported operation: {operationId}");
    }
}

void WriteResult(object payload)
{
    var options = CreateJsonOptions();
    if (jsonOutput)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload, options));
        return;
    }

    Console.WriteLine(JsonSerializer.Serialize(payload, options));
}

void Fail(string message, OperationDescriptor? operation = null)
{
    var options = CreateJsonOptions();
    var payload = new
    {
        ok = false,
        error = message,
        operation
    };

    Console.Error.WriteLine(JsonSerializer.Serialize(payload, options));
    Environment.ExitCode = 1;
}

JsonSerializerOptions CreateJsonOptions()
{
    var options = new JsonSerializerOptions { WriteIndented = true };
    options.Converters.Add(new JsonStringEnumConverter());
    return options;
}

Dictionary<string, string> ParseOptions(string[] values)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < values.Length; i++)
    {
        var token = values[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = token[2..];
        var value = i + 1 < values.Length && !values[i + 1].StartsWith("--", StringComparison.Ordinal) ? values[++i] : "true";
        result[key] = value;
    }

    return result;
}

string GetRequiredOption(Dictionary<string, string> options, string key)
{
    return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new InvalidOperationException($"Missing required option --{key}");
}

bool ParseBool(string value)
{
    return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("yes", StringComparison.OrdinalIgnoreCase);
}

void Launch(string target)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = target,
        UseShellExecute = true
    });
}

bool GetStartupEnabled()
{
    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: false);
    return key?.GetValue("Optimizer233-Windows") is string value && !string.IsNullOrWhiteSpace(value);
}

void SetStartupEnabled(bool enabled)
{
    using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
    if (enabled)
    {
        var exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Optimizer233-Windows", "Optimizer233.Windows.exe");
        key.SetValue("Optimizer233-Windows", $"\"{exePath}\"");
        return;
    }

    key.DeleteValue("Optimizer233-Windows", throwOnMissingValue: false);
}

IReadOnlyList<object> ScanCleanupTargets()
{
    var targets = GetCleanupTargets();
    return targets.Select(target =>
    {
        var stats = ScanDirectory(target.Path);
        return new
        {
            target.Id,
            target.Title,
            target.Description,
            target.Path,
            stats.FileCount,
            stats.TotalSizeBytes,
            risk = OperationRiskLevel.Low.ToString()
        };
    }).ToArray();
}

object CleanCleanupTargets()
{
    long deletedFiles = 0;
    long freedBytes = 0;
    long failedItems = 0;

    foreach (var target in GetCleanupTargets())
    {
        if (!Directory.Exists(target.Path))
        {
            continue;
        }

        var pending = new Stack<string>();
        var discovered = new List<string>();
        pending.Push(target.Path);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            discovered.Add(current);

            try
            {
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    pending.Push(directory);
                }
            }
            catch
            {
                failedItems++;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var length = info.Exists ? info.Length : 0;
                        info.IsReadOnly = false;
                        info.Delete();
                        deletedFiles++;
                        freedBytes += length;
                    }
                    catch
                    {
                        failedItems++;
                    }
                }
            }
            catch
            {
                failedItems++;
            }
        }

        foreach (var directory in discovered.OrderByDescending(item => item.Length))
        {
            if (string.Equals(directory.TrimEnd('\\'), target.Path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory, recursive: false);
                }
            }
            catch
            {
                failedItems++;
            }
        }
    }

    return new { deletedFiles, freedBytes, failedItems };
}

(long FileCount, long TotalSizeBytes) ScanDirectory(string path)
{
    if (!Directory.Exists(path))
    {
        return (0, 0);
    }

    long fileCount = 0;
    long totalSizeBytes = 0;
    var pending = new Stack<string>();
    pending.Push(path);

    while (pending.Count > 0)
    {
        var current = pending.Pop();
        try
        {
            foreach (var file in Directory.EnumerateFiles(current))
            {
                try
                {
                    var info = new FileInfo(file);
                    fileCount++;
                    totalSizeBytes += info.Length;
                }
                catch
                {
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(current))
            {
                pending.Push(directory);
            }
        }
        catch
        {
        }
    }

    return (fileCount, totalSizeBytes);
}

IReadOnlyList<(string Id, string Title, string Description, string Path)> GetCleanupTargets()
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    return
    [
        ("user-temp", "User Temp", "Current user temporary files.", Path.GetTempPath()),
        ("windows-temp", "Windows Temp", "Shared temporary folder.", Path.Combine(windowsDirectory, "Temp")),
        ("directx-shader", "DirectX Shader Cache", "Rebuildable DirectX shader cache.", Path.Combine(localAppData, "D3DSCache")),
        ("edge-cache", "Edge Cache", "Microsoft Edge cache.", Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data")),
        ("chrome-cache", "Chrome Cache", "Google Chrome cache.", Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data"))
    ];
}

async Task ExportConfigAsync(string path)
{
    var payload = new
    {
        PreferredUpdateChannelId = "github",
        CustomUpdatePrefix = "",
        LaunchAtStartup = GetStartupEnabled(),
        SelectedOptimizationActionIds = Array.Empty<string>()
    };

    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
}

async Task ImportConfigAsync(string path)
{
    _ = await File.ReadAllTextAsync(path);
}

Task ResetConfigAsync()
{
    SetStartupEnabled(false);
    return Task.CompletedTask;
}

async Task<object?> RunJsonPowerShellAsync(string script)
{
    var json = await RunPowerShellAsync(script);
    if (string.IsNullOrWhiteSpace(json))
    {
        return null;
    }

    return JsonSerializer.Deserialize<object>(json);
}

async Task<string> RunPowerShellAsync(string script)
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

IReadOnlyList<object> GetInstalledOfficeProducts()
{
    var results = new List<object>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                var displayName = subKey?.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                if (!displayName.Contains("Office", StringComparison.OrdinalIgnoreCase) &&
                    !displayName.Contains("Microsoft 365", StringComparison.OrdinalIgnoreCase) &&
                    !displayName.Contains("Visio", StringComparison.OrdinalIgnoreCase) &&
                    !displayName.Contains("Project", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seen.Add(displayName))
                {
                    continue;
                }

                results.Add(new
                {
                    DisplayName = displayName,
                    DisplayVersion = subKey?.GetValue("DisplayVersion") as string ?? string.Empty,
                    Publisher = subKey?.GetValue("Publisher") as string ?? string.Empty
                });
            }
        }
    }

    return results;
}

string EscapeSingleQuotes(string value) => value.Replace("'", "''", StringComparison.Ordinal);
