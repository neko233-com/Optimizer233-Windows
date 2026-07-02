namespace Optimizer_Windows.Models;

public sealed class SystemSnapshot
{
    public required string DeviceName { get; init; }

    public required string DeviceModel { get; init; }

    public required string WindowsVersion { get; init; }

    public required string BootTime { get; init; }

    public required string ProcessorName { get; init; }

    public required string GraphicsName { get; init; }

    public required string MemorySummary { get; init; }

    public required double MemoryUsagePercent { get; init; }

    public required string StorageSummary { get; init; }

    public required string NetworkSummary { get; init; }

    public required string InputSummary { get; init; }

    public required IReadOnlyList<DiskUsage> Drives { get; init; }

    public required IReadOnlyList<DetailItem> CpuDetails { get; init; }

    public required IReadOnlyList<DetailItem> GpuDetails { get; init; }

    public required IReadOnlyList<DetailItem> MemoryDetails { get; init; }

    public required IReadOnlyList<DetailItem> StorageDetails { get; init; }

    public required IReadOnlyList<DetailItem> NetworkDetails { get; init; }

    public required IReadOnlyList<DetailItem> InputDetails { get; init; }
}
