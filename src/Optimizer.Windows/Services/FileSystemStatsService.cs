using Optimizer_Windows.Models;

namespace Optimizer_Windows.Services;

public sealed class FileSystemStatsService
{
    public static FileSystemStatsService Instance { get; } = new();

    private FileSystemStatsService()
    {
    }

    public Task<FolderStats> ScanDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ScanDirectory(path, cancellationToken), cancellationToken);
    }

    private static FolderStats ScanDirectory(string path, CancellationToken cancellationToken)
    {
        long fileCount = 0;
        long totalBytes = 0;

        if (!Directory.Exists(path))
        {
            return new FolderStats
            {
                Path = path,
                FileCount = 0,
                TotalSizeBytes = 0
            };
        }

        var pending = new Stack<string>();
        pending.Push(path);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = pending.Pop();

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var info = new FileInfo(file);
                        totalBytes += info.Length;
                        fileCount++;
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

        return new FolderStats
        {
            Path = path,
            FileCount = fileCount,
            TotalSizeBytes = totalBytes
        };
    }
}
