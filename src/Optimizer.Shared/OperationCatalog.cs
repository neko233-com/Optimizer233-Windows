using Optimizer.Shared.Models;

namespace Optimizer.Shared;

public static class OperationCatalog
{
    public static IReadOnlyList<OperationDescriptor> All { get; } =
    [
        new() { Id = "storage-sense", Category = "optimize", Title = "Storage Sense", Description = "Open Windows Storage Sense settings.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec storage-sense" },
        new() { Id = "disk-cleanup", Category = "optimize", Title = "Disk Cleanup", Description = "Open native Disk Cleanup wizard.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec disk-cleanup" },
        new() { Id = "temp-folder", Category = "optimize", Title = "Temp Folder", Description = "Open user temp directory for manual review.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec temp-folder" },
        new() { Id = "startup-apps", Category = "optimize", Title = "Startup Apps", Description = "Open startup applications settings.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec startup-apps" },
        new() { Id = "task-manager", Category = "optimize", Title = "Task Manager", Description = "Open Task Manager.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec task-manager" },
        new() { Id = "windows-update", Category = "optimize", Title = "Windows Update", Description = "Open Windows Update settings.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec windows-update" },
        new() { Id = "power-settings", Category = "optimize", Title = "Power Settings", Description = "Open power and sleep settings.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec power-settings" },
        new() { Id = "cleanup-scan", Category = "cleanup", Title = "Cleanup Scan", Description = "Scan junk and cache targets.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec cleanup-scan --json" },
        new() { Id = "cleanup-run", Category = "cleanup", Title = "Cleanup Run", Description = "Delete supported junk and cache files.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec cleanup-run --json" },
        new() { Id = "startup-toggle", Category = "updates", Title = "Startup Toggle", Description = "Enable or disable launch at startup.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec startup-toggle --enabled true" },
        new() { Id = "update-check", Category = "updates", Title = "Check Updates", Description = "Check release manifest and preferred source.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec update-check --json" },
        new() { Id = "config-export", Category = "restore", Title = "Export Config", Description = "Export Optimizer233-Windows config.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec config-export --path C:\\temp\\optimizer233-config.json" },
        new() { Id = "config-import", Category = "restore", Title = "Import Config", Description = "Import Optimizer233-Windows config.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec config-import --path C:\\temp\\optimizer233-config.json" },
        new() { Id = "config-reset", Category = "restore", Title = "Reset Config", Description = "Reset app preferences and saved selections.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec config-reset" },
        new() { Id = "appx-list", Category = "appx", Title = "List Appx Packages", Description = "Enumerate installed Appx packages.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec appx-list --json" },
        new() { Id = "appx-remove", Category = "appx", Title = "Remove Appx Package", Description = "Remove current-user Appx package.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec appx-remove --package <PackageFullName>" },
        new() { Id = "security-status", Category = "security", Title = "Security Status", Description = "Read Defender and firewall status.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec security-status --json" },
        new() { Id = "office-list", Category = "office", Title = "List Office Products", Description = "Enumerate installed Office products.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec office-list --json" },
        new() { Id = "service-list", Category = "troubleshoot", Title = "List Services", Description = "Enumerate Windows services.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec service-list --json" },
        new() { Id = "service-set-startup", Category = "troubleshoot", Title = "Set Service Startup", Description = "Change Windows service startup type.", RiskLevel = OperationRiskLevel.High, CliExample = "exec service-set-startup --name SysMain --mode Manual" },
        new() { Id = "task-list", Category = "troubleshoot", Title = "List Scheduled Tasks", Description = "Enumerate scheduled tasks.", RiskLevel = OperationRiskLevel.Low, CliExample = "exec task-list --json" },
        new() { Id = "task-set-enabled", Category = "troubleshoot", Title = "Set Task Enabled", Description = "Enable or disable scheduled task.", RiskLevel = OperationRiskLevel.Medium, CliExample = "exec task-set-enabled --name TaskName --path \\ --enabled false" }
    ];

    public static OperationDescriptor? Find(string id)
    {
        return All.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
