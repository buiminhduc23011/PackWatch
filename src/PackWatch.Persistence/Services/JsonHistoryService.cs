using PackWatch.Application.Abstractions.History;
using PackWatch.Domain.Entities;
using System.Text.Json;
using System.Threading;

namespace PackWatch.Persistence.Services;

internal sealed class JsonHistoryService : IHistoryService, IOrderHistoryWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<PersistedOrderRecord>? _cache;

    public async Task<IReadOnlyList<OrderRecord>> SearchAsync(
        OrderHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var records = await GetSnapshotsAsync(cancellationToken);

        var filtered = records
            .Where(record => string.IsNullOrWhiteSpace(query.OrderCode)
                || record.OrderCode.Contains(query.OrderCode, StringComparison.OrdinalIgnoreCase))
            .Where(record => query.FromDate is null
                || DateOnly.FromDateTime(record.StartTime.LocalDateTime) >= query.FromDate.Value)
            .Where(record => query.ToDate is null
                || DateOnly.FromDateTime(record.StartTime.LocalDateTime) <= query.ToDate.Value)
            .OrderByDescending(record => record.StartTime)
            .Select(MapToDomain)
            .ToList();

        return filtered;
    }

    public async Task AppendAsync(OrderRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var snapshots = await GetSnapshotsInternalAsync(cancellationToken);
            snapshots.Add(PersistedOrderRecord.FromDomain(record));
            await SaveSnapshotsAsync(snapshots, cancellationToken);
            _cache = snapshots;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string videoPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var snapshots = await GetSnapshotsInternalAsync(cancellationToken);
            var toRemove = snapshots.FirstOrDefault(s => s.VideoPath.Equals(videoPath, StringComparison.OrdinalIgnoreCase));
            if (toRemove is not null)
            {
                snapshots.Remove(toRemove);
                await SaveSnapshotsAsync(snapshots, cancellationToken);
                _cache = snapshots;

                try
                {
                    if (File.Exists(videoPath))
                    {
                        File.Delete(videoPath);
                    }
                    if (File.Exists(toRemove.ThumbnailPath))
                    {
                        File.Delete(toRemove.ThumbnailPath);
                    }
                }
                catch
                {
                    // Ignored
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<PersistedOrderRecord>> GetSnapshotsAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return new List<PersistedOrderRecord>(_cache);
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await GetSnapshotsInternalAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<PersistedOrderRecord>> GetSnapshotsInternalAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return new List<PersistedOrderRecord>(_cache);
        }

        var snapshots = await LoadSnapshotsFromDiskAsync(cancellationToken);
        _cache = new List<PersistedOrderRecord>(snapshots);
        return snapshots;
    }

    private static async Task<List<PersistedOrderRecord>> LoadSnapshotsFromDiskAsync(CancellationToken cancellationToken)
    {
        var historyFilePath = LocalPackWatchPaths.HistoryFilePath;

        if (!File.Exists(historyFilePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(historyFilePath);
        var snapshots = await JsonSerializer.DeserializeAsync<List<PersistedOrderRecord>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return snapshots ?? [];
    }

    private static async Task SaveSnapshotsAsync(
        List<PersistedOrderRecord> snapshots,
        CancellationToken cancellationToken)
    {
        var historyFilePath = LocalPackWatchPaths.HistoryFilePath;
        await using var stream = File.Create(historyFilePath);
        await JsonSerializer.SerializeAsync(stream, snapshots, SerializerOptions, cancellationToken);
    }

    private static OrderRecord MapToDomain(PersistedOrderRecord snapshot)
    {
        var record = OrderRecord.Start(
            snapshot.OrderCode,
            snapshot.VideoPath,
            snapshot.ThumbnailPath,
            snapshot.StartTime,
            snapshot.CameraName);

        if (snapshot.EndTime is not null)
        {
            record.Complete(snapshot.EndTime.Value);
        }

        return record;
    }

    private sealed record PersistedOrderRecord(
        string OrderCode,
        string VideoPath,
        string ThumbnailPath,
        DateTimeOffset StartTime,
        DateTimeOffset? EndTime,
        string CameraName)
    {
        public static PersistedOrderRecord FromDomain(OrderRecord record)
        {
            return new PersistedOrderRecord(
                record.OrderCode,
                record.VideoPath,
                record.ThumbnailPath,
                record.StartTime,
                record.EndTime,
                record.CameraName);
        }
    }
}
