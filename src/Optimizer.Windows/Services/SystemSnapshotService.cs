using Optimizer_Windows.Helpers;
using Optimizer_Windows.Localization;
using Optimizer_Windows.Models;
using System.Management;
using System.Net.NetworkInformation;

namespace Optimizer_Windows.Services;

public sealed class SystemSnapshotService
{
    public static SystemSnapshotService Instance { get; } = new();

    private SystemSnapshotService()
    {
    }

    public Task<SystemSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Capture(cancellationToken), cancellationToken);
    }

    private static SystemSnapshot Capture(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deviceName = Environment.MachineName;
        var osRow = QueryFirst("SELECT Caption, Version, BuildNumber, LastBootUpTime, TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
        var computerRow = QueryFirst("SELECT Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem");
        var processorRow = QueryFirst("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
        var gpuRows = QueryMany("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");
        var keyboardRows = QueryMany("SELECT Name, Description FROM Win32_Keyboard");
        var mouseRows = QueryMany("SELECT Name, Description, NumberOfButtons FROM Win32_PointingDevice");

        var unknown = AppText.Get("CommonUnknown");
        var unavailable = AppText.Get("CommonUnavailable");
        var windowsCaption = Cleanup(osRow.TryGetValue("Caption", out var caption) ? caption : "Windows");
        var windowsVersion = osRow.TryGetValue("Version", out var version) ? version : unknown;
        var windowsBuild = osRow.TryGetValue("BuildNumber", out var build) ? build : unknown;
        var bootTime = ParseManagementTime(osRow.TryGetValue("LastBootUpTime", out var bootRaw) ? bootRaw : null);

        var manufacturer = Cleanup(computerRow.TryGetValue("Manufacturer", out var manufacturerValue) ? manufacturerValue : unknown);
        var model = Cleanup(computerRow.TryGetValue("Model", out var modelValue) ? modelValue : unknown);
        var deviceModel = $"{manufacturer} {model}".Trim();

        var processorName = Cleanup(processorRow.TryGetValue("Name", out var cpuName) ? cpuName : unknown);
        var coreCount = processorRow.TryGetValue("NumberOfCores", out var coresValue) && int.TryParse(coresValue, out var cores) ? cores : 0;
        var threadCount = processorRow.TryGetValue("NumberOfLogicalProcessors", out var threadsValue) && int.TryParse(threadsValue, out var threads) ? threads : 0;
        var maxClockMHz = processorRow.TryGetValue("MaxClockSpeed", out var clockValue) && int.TryParse(clockValue, out var mhz) ? mhz : 0;

        var graphicsNames = gpuRows
            .Select(row => Cleanup(row.TryGetValue("Name", out var gpuName) ? gpuName : string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToArray();
        var graphicsName = graphicsNames.Length > 0 ? string.Join(" / ", graphicsNames) : unknown;

        var totalMemoryBytes = ParseUInt64(computerRow.TryGetValue("TotalPhysicalMemory", out var totalMemoryValue) ? totalMemoryValue : null);
        if (totalMemoryBytes == 0 &&
            osRow.TryGetValue("TotalVisibleMemorySize", out var totalVisibleMemoryValue) &&
            ulong.TryParse(totalVisibleMemoryValue, out var totalVisibleKb))
        {
            totalMemoryBytes = totalVisibleKb * 1024;
        }

        var freeMemoryBytes = osRow.TryGetValue("FreePhysicalMemory", out var freeMemoryValue) &&
                              ulong.TryParse(freeMemoryValue, out var freeMemoryKb)
            ? freeMemoryKb * 1024
            : 0;
        var usedMemoryBytes = totalMemoryBytes > freeMemoryBytes ? totalMemoryBytes - freeMemoryBytes : 0;
        var memoryUsagePercent = totalMemoryBytes == 0 ? 0 : Math.Round(usedMemoryBytes / (double)totalMemoryBytes * 100, 1);
        var memorySummary = totalMemoryBytes == 0
            ? unavailable
            : $"{FileSizeFormatter.Format((long)usedMemoryBytes)} used / {FileSizeFormatter.Format((long)totalMemoryBytes)} total";

        var drives = DriveInfo.GetDrives()
            .Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed)
            .Select(drive =>
            {
                var usedBytes = drive.TotalSize - drive.AvailableFreeSpace;
                var usagePercent = drive.TotalSize == 0 ? 0 : (int)Math.Round(usedBytes / (double)drive.TotalSize * 100);
                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : $"{drive.Name} {drive.VolumeLabel}";

                return new DiskUsage
                {
                    Name = label.Trim(),
                    Summary = $"{FileSizeFormatter.Format(usedBytes)} used / {FileSizeFormatter.Format(drive.TotalSize)} total",
                    UsagePercent = usagePercent,
                    UsageLabel = $"{usagePercent}%"
                };
            })
            .ToArray();

        var totalStorageBytes = drives.Sum(drive => TryParseBytesFromSummary(drive.Summary, "total"));
        var usedStorageBytes = drives.Sum(drive => TryParseBytesFromSummary(drive.Summary, "used"));
        var storageSummary = drives.Length == 0
            ? AppText.Get("StorageNoFixedDrives")
            : AppText.Format("StorageAcrossDrives", FileSizeFormatter.Format(usedStorageBytes), FileSizeFormatter.Format(totalStorageBytes), drives.Length);

        var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(network =>
                network.OperationalStatus == OperationalStatus.Up &&
                network.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                !network.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var networkSummary = activeInterfaces.Length == 0
            ? AppText.Get("NetworkNoActiveAdapter")
            : $"{activeInterfaces[0].Name} · {FormatLinkSpeed(activeInterfaces[0].Speed)}";

        var keyboardNames = keyboardRows
            .Select(row => Cleanup(row.TryGetValue("Name", out var name) ? name : row.TryGetValue("Description", out var description) ? description : string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToArray();

        var mouseNames = mouseRows
            .Select(row => Cleanup(row.TryGetValue("Name", out var name) ? name : row.TryGetValue("Description", out var description) ? description : string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToArray();

        var inputSummary = $"{keyboardNames.Length} {AppText.Get("HardwareKeyboard")} / {mouseNames.Length} {AppText.Get("HardwareMouse")}";

        var cpuDetails = new List<DetailItem>
        {
            new() { Label = "Name", Value = processorName },
            new() { Label = "Cores", Value = coreCount == 0 ? unknown : coreCount.ToString() },
            new() { Label = "Threads", Value = threadCount == 0 ? unknown : threadCount.ToString() },
            new() { Label = "Boost clock", Value = maxClockMHz == 0 ? unknown : $"{maxClockMHz / 1000d:0.##} GHz" }
        };

        var gpuDetails = gpuRows.Count == 0
            ? [new DetailItem { Label = "Adapter", Value = unavailable }]
            : gpuRows.Select(row => new DetailItem
            {
                Label = Cleanup(row.TryGetValue("Name", out var adapterName) ? adapterName : "Adapter"),
                Value = row.TryGetValue("DriverVersion", out var driverVersion)
                    ? $"Driver {driverVersion}"
                    : unavailable
            }).ToArray();

        var memoryDetails = new List<DetailItem>
        {
            new() { Label = "Installed", Value = totalMemoryBytes == 0 ? unknown : FileSizeFormatter.Format((long)totalMemoryBytes) },
            new() { Label = "In use", Value = FileSizeFormatter.Format((long)usedMemoryBytes) },
            new() { Label = "Available", Value = FileSizeFormatter.Format((long)freeMemoryBytes) },
            new() { Label = "Usage", Value = $"{memoryUsagePercent:0.#}%" }
        };

        var storageDetails = drives.Length == 0
            ? [new DetailItem { Label = "Drives", Value = unavailable }]
            : drives.Select(drive => new DetailItem
            {
                Label = drive.Name,
                Value = $"{drive.Summary} ({drive.UsageLabel})"
            }).ToArray();

        var networkDetails = activeInterfaces.Length == 0
            ? [new DetailItem { Label = "Network", Value = unavailable }]
            : activeInterfaces.Select(network =>
            {
                var addresses = network
                    .GetIPProperties()
                    .UnicastAddresses
                    .Where(address => address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(address => address.Address.ToString())
                    .ToArray();

                return new DetailItem
                {
                    Label = network.Name,
                    Value = $"{FormatLinkSpeed(network.Speed)}{(addresses.Length > 0 ? $" · {string.Join(", ", addresses)}" : string.Empty)}"
                };
            }).ToArray();

        var inputDetails = new List<DetailItem>();
        if (keyboardNames.Length == 0 && mouseNames.Length == 0)
        {
            inputDetails.Add(new DetailItem { Label = AppText.Get("HardwareInput"), Value = unavailable });
        }
        else
        {
            inputDetails.AddRange(keyboardNames.Select(name => new DetailItem
            {
                Label = AppText.Get("HardwareKeyboard"),
                Value = name
            }));

            inputDetails.AddRange(mouseRows.Select(row =>
            {
                var mouseName = Cleanup(row.TryGetValue("Name", out var name) ? name : row.TryGetValue("Description", out var description) ? description : AppText.Get("HardwareMouse"));
                var buttons = row.TryGetValue("NumberOfButtons", out var count) && int.TryParse(count, out var parsed)
                    ? $" · {parsed} buttons"
                    : string.Empty;

                return new DetailItem
                {
                    Label = AppText.Get("HardwareMouse"),
                    Value = $"{mouseName}{buttons}"
                };
            }));
        }

        return new SystemSnapshot
        {
            DeviceName = deviceName,
            DeviceModel = deviceModel,
            WindowsVersion = $"{windowsCaption} ({windowsVersion}, build {windowsBuild})",
            BootTime = AppText.Format("StatusLastBoot", bootTime),
            ProcessorName = processorName,
            GraphicsName = graphicsName,
            MemorySummary = memorySummary,
            MemoryUsagePercent = memoryUsagePercent,
            StorageSummary = storageSummary,
            NetworkSummary = networkSummary,
            InputSummary = inputSummary,
            Drives = drives,
            CpuDetails = cpuDetails,
            GpuDetails = gpuDetails,
            MemoryDetails = memoryDetails,
            StorageDetails = storageDetails,
            NetworkDetails = networkDetails,
            InputDetails = inputDetails
        };
    }

    private static Dictionary<string, string> QueryFirst(string query)
    {
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();
        var first = results.Cast<ManagementObject>().FirstOrDefault();
        return first is null ? [] : ToDictionary(first);
    }

    private static List<Dictionary<string, string>> QueryMany(string query)
    {
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();
        return results.Cast<ManagementObject>().Select(ToDictionary).ToList();
    }

    private static Dictionary<string, string> ToDictionary(ManagementObject managementObject)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyData property in managementObject.Properties)
        {
            dictionary[property.Name] = property.Value?.ToString() ?? string.Empty;
        }

        return dictionary;
    }

    private static ulong ParseUInt64(string? value)
    {
        return ulong.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static string ParseManagementTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(value).ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string Cleanup(string value)
    {
        return value.Replace("(R)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("(TM)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Microsoft ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string FormatLinkSpeed(long speed)
    {
        if (speed <= 0)
        {
            return "Unknown speed";
        }

        if (speed >= 1_000_000_000)
        {
            return $"{speed / 1_000_000_000d:0.##} Gbps";
        }

        return $"{speed / 1_000_000d:0.##} Mbps";
    }

    private static long TryParseBytesFromSummary(string summary, string marker)
    {
        var segment = summary.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.Contains(marker, StringComparison.OrdinalIgnoreCase));

        if (segment is null)
        {
            return 0;
        }

        var numberParts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (numberParts.Length < 2 || !double.TryParse(numberParts[0], out var value))
        {
            return 0;
        }

        var multiplier = numberParts[1].ToUpperInvariant() switch
        {
            "KB" => 1024d,
            "MB" => 1024d * 1024,
            "GB" => 1024d * 1024 * 1024,
            "TB" => 1024d * 1024 * 1024 * 1024,
            "PB" => 1024d * 1024 * 1024 * 1024 * 1024,
            _ => 1d
        };

        return (long)(value * multiplier);
    }
}
