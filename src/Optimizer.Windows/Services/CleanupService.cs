using Optimizer_Windows.Models;
using Optimizer_Windows.Localization;

namespace Optimizer_Windows.Services;

public sealed class CleanupService
{
    public static CleanupService Instance { get; } = new();

    private readonly FileSystemStatsService _fileSystemStatsService = FileSystemStatsService.Instance;

    private CleanupService()
    {
    }

    public IReadOnlyList<CleanupTarget> GetTargets()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        return
        [
            new CleanupTarget
            {
                Id = "user-temp",
                Title = AppText.Get("CleanupTargetUserTempTitle"),
                Description = AppText.Get("CleanupTargetUserTempBody"),
                Path = Path.GetTempPath()
            },
            new CleanupTarget
            {
                Id = "windows-temp",
                Title = AppText.Get("CleanupTargetWindowsTempTitle"),
                Description = AppText.Get("CleanupTargetWindowsTempBody"),
                Path = Path.Combine(windowsDirectory, "Temp")
            },
            new CleanupTarget
            {
                Id = "directx-shader",
                Title = AppText.Get("CleanupTargetDirectXTitle"),
                Description = AppText.Get("CleanupTargetDirectXBody"),
                Path = Path.Combine(localAppData, "D3DSCache")
            },
            new CleanupTarget
            {
                Id = "edge-cache",
                Title = AppText.Get("CleanupTargetEdgeTitle"),
                Description = AppText.Get("CleanupTargetEdgeBody"),
                Path = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data")
            },
            new CleanupTarget
            {
                Id = "chrome-cache",
                Title = AppText.Get("CleanupTargetChromeTitle"),
                Description = AppText.Get("CleanupTargetChromeBody"),
                Path = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache", "Cache_Data")
            }
        ];
    }

    public async Task<IReadOnlyList<CleanupTargetResult>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var targets = GetTargets();
        var results = new List<CleanupTargetResult>(targets.Count);

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stats = await _fileSystemStatsService.ScanDirectoryAsync(target.Path, cancellationToken);
            results.Add(new CleanupTargetResult
            {
                Id = target.Id,
                Title = target.Title,
                Description = target.Description,
                Path = target.Path,
                FileCount = stats.FileCount,
                TotalSizeBytes = stats.TotalSizeBytes
            });
        }

        return results;
    }

    public Task<CleanupRunResult> CleanAsync(IEnumerable<string> targetIds, CancellationToken cancellationToken = default)
    {
        var targetSet = targetIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var targets = GetTargets().Where(target => targetSet.Contains(target.Id)).ToArray();

        return Task.Run(() =>
        {
            long deletedFiles = 0;
            long freedBytes = 0;
            long failedItems = 0;

            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(target.Path))
                {
                    continue;
                }

                var pending = new Stack<string>();
                var discovered = new List<string>();
                pending.Push(target.Path);

                while (pending.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                            cancellationToken.ThrowIfCancellationRequested();

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

                foreach (var directory in discovered.OrderByDescending(path => path.Length))
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

            return new CleanupRunResult
            {
                DeletedFileCount = deletedFiles,
                FreedBytes = freedBytes,
                FailedItemCount = failedItems
            };
        }, cancellationToken);
    }
}
