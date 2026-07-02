using Microsoft.Win32;
using Optimizer_Windows.Models;

namespace Optimizer_Windows.Services;

public sealed class OfficeService
{
    public static OfficeService Instance { get; } = new();

    private OfficeService()
    {
    }

    public IReadOnlyList<OfficeInstallInfo> GetInstalledProducts()
    {
        var results = new List<OfficeInstallInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                CollectUninstallEntries(baseKey, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", results, seen);
            }
        }

        return results
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string GetDownloadUrl() => "https://www.microsoft.com/en-us/microsoft-365/download-office";

    public string GetDeploymentToolUrl() => "https://www.microsoft.com/en-us/download/details.aspx?id=49117";

    private static void CollectUninstallEntries(RegistryKey baseKey, string path, List<OfficeInstallInfo> results, HashSet<string> seen)
    {
        using var uninstallKey = baseKey.OpenSubKey(path);
        if (uninstallKey is null)
        {
            return;
        }

        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
        {
            using var subKey = uninstallKey.OpenSubKey(subKeyName);
            if (subKey is null)
            {
                continue;
            }

            var displayName = subKey.GetValue("DisplayName") as string;
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

            results.Add(new OfficeInstallInfo
            {
                DisplayName = displayName,
                DisplayVersion = subKey.GetValue("DisplayVersion") as string ?? string.Empty,
                Publisher = subKey.GetValue("Publisher") as string ?? string.Empty
            });
        }
    }
}
