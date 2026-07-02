using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Optimizer.Shared.Models;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using System.Diagnostics;
using System.Text.Json;
using Windows.UI;

namespace Optimizer_Windows.Services;

public sealed class WindowsFeatureService
{
    private static readonly IReadOnlyList<FeatureDefinition> Catalog =
    [
        new(WindowsFeatureKind.RegistryDword, "DeveloperMode", "FeatureDeveloperModeTitle", "FeatureDeveloperModeBody", "FeatureAudienceDevAll", "developer sideload debug frontend backend gamedev 游戏开发 前端 后端", OperationRiskLevel.Low, RegistryPath: @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock", RegistryName: "AllowDevelopmentWithoutDevLicense", EnabledValue: 1, DisabledValue: 0, RequiresElevation: true),
        new(WindowsFeatureKind.RegistryDword, "LongPaths", "FeatureLongPathsTitle", "FeatureLongPathsBody", "FeatureAudienceFrontendBackendGameDev", "long paths npm node dotnet unreal unity build frontend backend gamedev 前端 后端 游戏开发", OperationRiskLevel.Low, RegistryPath: @"HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem", RegistryName: "LongPathsEnabled", EnabledValue: 1, DisabledValue: 0, RequiresElevation: true),
        new(WindowsFeatureKind.RegistryDword, "GameMode", "FeatureGameModeTitle", "FeatureGameModeBody", "FeatureAudienceGameDevGamer", "game mode gamer performance latency fps 游戏 玩家 游戏开发", OperationRiskLevel.Low, RegistryPath: @"HKCU:\Software\Microsoft\GameBar", RegistryName: "AutoGameModeEnabled", EnabledValue: 1, DisabledValue: 0, RequiresElevation: false),
        new(WindowsFeatureKind.OptionalFeature, "Containers-DisposableClientVM", "FeatureSandboxTitle", "FeatureSandboxBody", "FeatureAudienceAll", "virtual sandbox isolation disposable vm frontend backend gamedev gamer 前端 后端 游戏开发 玩家", OperationRiskLevel.Medium),
        new(WindowsFeatureKind.OptionalFeature, "Microsoft-Windows-Subsystem-Linux", "FeatureWslTitle", "FeatureWslBody", "FeatureAudienceFrontendBackendGameDev", "wsl linux terminal dev container subsystem frontend backend gamedev 前端 后端 游戏开发", OperationRiskLevel.Medium),
        new(WindowsFeatureKind.OptionalFeature, "VirtualMachinePlatform", "FeatureVmPlatformTitle", "FeatureVmPlatformBody", "FeatureAudienceBackendGameDev", "virtual machine platform wsl2 hypervisor backend gamedev 后端 游戏开发", OperationRiskLevel.Medium),
        new(WindowsFeatureKind.OptionalFeature, "Microsoft-Hyper-V-All", "FeatureHyperVTitle", "FeatureHyperVBody", "FeatureAudienceBackendGameDev", "hyper-v virtualization virtual machines vms backend gamedev 后端 游戏开发", OperationRiskLevel.High),
        new(WindowsFeatureKind.OptionalFeature, "NetFx3", "FeatureNetFx3Title", "FeatureNetFx3Body", "FeatureAudienceGameDevGamer", "dotnet .net framework legacy runtime gamer gamedev 游戏 玩家 游戏开发", OperationRiskLevel.Low),
        new(WindowsFeatureKind.OptionalFeature, "Printing-PrintToPDFServices-Features", "FeaturePrintPdfTitle", "FeaturePrintPdfBody", "FeatureAudienceAll", "print pdf export microsoft print to pdf frontend backend gamedev gamer 前端 后端 游戏开发 玩家", OperationRiskLevel.Low),
        new(WindowsFeatureKind.OptionalFeature, "TelnetClient", "FeatureTelnetTitle", "FeatureTelnetBody", "FeatureAudienceBackend", "telnet client legacy insecure terminal backend network 后端", OperationRiskLevel.High),
        new(WindowsFeatureKind.OptionalFeature, "TFTP", "FeatureTftpTitle", "FeatureTftpBody", "FeatureAudienceBackend", "tftp client legacy transfer network backend router switch 后端", OperationRiskLevel.Medium),
        new(WindowsFeatureKind.OptionalFeature, "SMB1Protocol", "FeatureSmb1Title", "FeatureSmb1Body", "FeatureAudienceBackendGamer", "smb1 cifs legacy file sharing insecure backend retro gamer 后端 玩家", OperationRiskLevel.High),
        new(WindowsFeatureKind.OptionalFeature, "DirectPlay", "FeatureDirectPlayTitle", "FeatureDirectPlayBody", "FeatureAudienceGamer", "directplay legacy games compatibility gamer 玩家", OperationRiskLevel.Low)
    ];

    public static WindowsFeatureService Instance { get; } = new();

    private WindowsFeatureService()
    {
    }

    public async Task<IReadOnlyList<WindowsFeatureItem>> GetFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var optionalDefinitions = Catalog.Where(definition => definition.Kind == WindowsFeatureKind.OptionalFeature).ToArray();
        var registryDefinitions = Catalog.Where(definition => definition.Kind == WindowsFeatureKind.RegistryDword).ToArray();

        var script = $$"""
$ProgressPreference='SilentlyContinue'
$optionalNames = @({{string.Join(", ", optionalDefinitions.Select(definition => $"'{definition.Id}'"))}})
$registryItems = @(
{{string.Join(",\n", registryDefinitions.Select(definition => $"  [PSCustomObject]@{{ Id = '{definition.Id}'; Path = '{definition.RegistryPath}'; Name = '{definition.RegistryName}' }}"))}}
)
$all = Get-CimInstance -ClassName Win32_OptionalFeature
$result = @()
foreach ($name in $optionalNames) {
  $feature = $all | Where-Object Name -eq $name | Select-Object -First 1
  if ($feature) {
    $result += [PSCustomObject]@{
      Id = [string]$name
      FeatureName = [string]$name
      InstallState = [int]$feature.InstallState
      Available = $true
    }
  }
  else {
    $result += [PSCustomObject]@{
      Id = [string]$name
      FeatureName = [string]$name
      InstallState = 0
      Available = $false
    }
  }
}
foreach ($item in $registryItems) {
  try {
    $value = (Get-ItemProperty -Path $item.Path -Name $item.Name -ErrorAction Stop).$($item.Name)
    $result += [PSCustomObject]@{
      Id = [string]$item.Id
      FeatureName = [string]$item.Id
      IntValue = [int]$value
      Available = $true
    }
  }
  catch {
    $result += [PSCustomObject]@{
      Id = [string]$item.Id
      FeatureName = [string]$item.Id
      IntValue = 0
      Available = $false
    }
  }
}
@($result) | ConvertTo-Json -Depth 5
""";

        var json = await RunPowerShellAsync(script, cancellationToken);
        var states = JsonSerializer.Deserialize<List<FeatureStateDto>>(json) ?? [];
        var stateMap = states.ToDictionary(state => state.FeatureName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return Catalog.Select(definition =>
        {
            stateMap.TryGetValue(definition.Id, out var definitionState);
            return CreateItem(definition, definitionState);
        }).ToArray();
    }

    public Task SetFeatureStateAsync(string featureName, bool enabled, CancellationToken cancellationToken = default)
    {
        var definition = Catalog.FirstOrDefault(item => string.Equals(item.Id, featureName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown feature: {featureName}");

        var script = definition.Kind switch
        {
            WindowsFeatureKind.OptionalFeature => enabled
                ? $"Enable-WindowsOptionalFeature -Online -FeatureName \"{definition.Id}\" -All -NoRestart | Out-Null"
                : $"Disable-WindowsOptionalFeature -Online -FeatureName \"{definition.Id}\" -NoRestart | Out-Null",
            WindowsFeatureKind.RegistryDword => $"New-Item -Path \"{definition.RegistryPath}\" -Force | Out-Null; Set-ItemProperty -Path \"{definition.RegistryPath}\" -Name \"{definition.RegistryName}\" -Type DWord -Value {(enabled ? definition.EnabledValue : definition.DisabledValue)}",
            _ => throw new InvalidOperationException($"Unsupported feature kind: {definition.Kind}")
        };

        return definition.RequiresElevation
            ? RunPowerShellElevatedAsync(script, cancellationToken)
            : RunPowerShellAsync(script, cancellationToken);
    }

    private static WindowsFeatureItem CreateItem(FeatureDefinition definition, FeatureStateDto? state)
    {
        var normalizedState = definition.Kind switch
        {
            WindowsFeatureKind.OptionalFeature => GetStateFromInstallState(state?.InstallState ?? 0, state?.Available ?? false),
            WindowsFeatureKind.RegistryDword => GetStateFromRegistryValue(state?.IntValue ?? 0, state?.Available ?? false, definition.EnabledValue),
            _ => "Unavailable"
        };
        var enabled = normalizedState.Equals("Enabled", StringComparison.OrdinalIgnoreCase);

        return new WindowsFeatureItem
        {
            Id = definition.Id,
            DisplayName = AppText.Get(definition.TitleKey),
            Description = AppText.Get(definition.DescriptionKey),
            Category = AppText.Get("NavFeatures"),
            AudienceText = AppText.Get(definition.AudienceKey),
            SearchKeywords = definition.SearchKeywords,
            RiskLevel = definition.RiskLevel,
            RiskLabel = GetRiskLabel(definition.RiskLevel),
            RiskBrush = CreateRiskBrush(definition.RiskLevel),
            RiskBackground = CreateRiskBackground(definition.RiskLevel),
            RiskBorder = CreateRiskBorder(definition.RiskLevel),
            StateBrush = CreateStateBrush(normalizedState, state?.Available ?? false),
            StateBackground = CreateStateBackground(normalizedState, state?.Available ?? false),
            StateBorder = CreateStateBorder(normalizedState, state?.Available ?? false),
            IsEnabled = enabled,
            IsAvailable = state?.Available ?? false,
            RequiresRestart = false,
            StateText = GetStateText(normalizedState, state?.Available ?? false),
            RestartText = AppText.Get("FeatureRestartUnknown")
        };
    }

    private static string GetStateFromInstallState(int installState, bool available)
    {
        if (!available)
        {
            return "Unavailable";
        }

        return installState switch
        {
            1 => "Enabled",
            2 => "Disabled",
            _ => "Unavailable"
        };
    }

    private static string GetStateFromRegistryValue(int intValue, bool available, int enabledValue)
    {
        if (!available)
        {
            return "Unavailable";
        }

        return intValue == enabledValue ? "Enabled" : "Disabled";
    }

    private static string GetRiskLabel(OperationRiskLevel riskLevel)
    {
        return riskLevel switch
        {
            OperationRiskLevel.Low => AppText.Get("SearchRiskLow"),
            OperationRiskLevel.Medium => AppText.Get("SearchRiskMedium"),
            OperationRiskLevel.High => AppText.Get("SearchRiskHigh"),
            _ => AppText.Get("CommonUnknown")
        };
    }

    private static Brush CreateRiskBrush(OperationRiskLevel riskLevel)
    {
        return new SolidColorBrush(riskLevel switch
        {
            OperationRiskLevel.Low => Color.FromArgb(255, 136, 255, 182),
            OperationRiskLevel.Medium => Color.FromArgb(255, 255, 198, 110),
            OperationRiskLevel.High => Color.FromArgb(255, 255, 138, 160),
            _ => Color.FromArgb(255, 201, 201, 201)
        });
    }

    private static Brush CreateRiskBackground(OperationRiskLevel riskLevel)
    {
        return new SolidColorBrush(riskLevel switch
        {
            OperationRiskLevel.Low => Color.FromArgb(255, 14, 34, 23),
            OperationRiskLevel.Medium => Color.FromArgb(255, 43, 27, 8),
            OperationRiskLevel.High => Color.FromArgb(255, 45, 16, 25),
            _ => Color.FromArgb(255, 25, 25, 25)
        });
    }

    private static Brush CreateRiskBorder(OperationRiskLevel riskLevel)
    {
        return new SolidColorBrush(riskLevel switch
        {
            OperationRiskLevel.Low => Color.FromArgb(255, 43, 92, 63),
            OperationRiskLevel.Medium => Color.FromArgb(255, 125, 86, 33),
            OperationRiskLevel.High => Color.FromArgb(255, 135, 52, 72),
            _ => Color.FromArgb(255, 62, 62, 62)
        });
    }

    private static string GetStateText(string state, bool available)
    {
        if (!available || state.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return AppText.Get("FeatureStateUnavailable");
        }

        return state switch
        {
            "Enabled" => AppText.Get("FeatureStateEnabled"),
            "Disabled" => AppText.Get("FeatureStateDisabled"),
            "EnablePending" => AppText.Get("FeatureStateEnablePending"),
            "DisablePending" => AppText.Get("FeatureStateDisablePending"),
            _ => state
        };
    }

    private static Brush CreateStateBrush(string state, bool available)
    {
        if (!available || state.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return new SolidColorBrush(Color.FromArgb(255, 169, 175, 188));
        }

        return state switch
        {
            "Enabled" => new SolidColorBrush(Color.FromArgb(255, 140, 255, 194)),
            "Disabled" => new SolidColorBrush(Color.FromArgb(255, 255, 160, 170)),
            _ => new SolidColorBrush(Color.FromArgb(255, 196, 202, 214))
        };
    }

    private static Brush CreateStateBackground(string state, bool available)
    {
        if (!available || state.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return new SolidColorBrush(Color.FromArgb(255, 23, 25, 31));
        }

        return new SolidColorBrush(state switch
        {
            "Enabled" => Color.FromArgb(255, 11, 33, 22),
            "Disabled" => Color.FromArgb(255, 34, 18, 24),
            _ => Color.FromArgb(255, 18, 22, 30)
        });
    }

    private static Brush CreateStateBorder(string state, bool available)
    {
        if (!available || state.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return new SolidColorBrush(Color.FromArgb(255, 61, 66, 78));
        }

        return new SolidColorBrush(state switch
        {
            "Enabled" => Color.FromArgb(255, 44, 92, 63),
            "Disabled" => Color.FromArgb(255, 112, 56, 68),
            _ => Color.FromArgb(255, 62, 68, 82)
        });
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
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "Windows feature command failed." : stderr.Trim());
        }

        return stdout.Trim();
    }

    private static async Task RunPowerShellElevatedAsync(string script, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            UseShellExecute = true,
            Verb = "runas"
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start elevated PowerShell.");
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

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Windows feature command failed with exit code {process.ExitCode}.");
        }
    }

    private sealed record FeatureDefinition(
        WindowsFeatureKind Kind,
        string Id,
        string TitleKey,
        string DescriptionKey,
        string AudienceKey,
        string SearchKeywords,
        OperationRiskLevel RiskLevel,
        string RegistryPath = "",
        string RegistryName = "",
        int EnabledValue = 1,
        int DisabledValue = 0,
        bool RequiresElevation = false);

    private enum WindowsFeatureKind
    {
        OptionalFeature,
        RegistryDword
    }

    private sealed class FeatureStateDto
    {
        public string? Id { get; set; }

        public string? FeatureName { get; set; }

        public int InstallState { get; set; }

        public int IntValue { get; set; }

        public bool Available { get; set; }
    }
}
