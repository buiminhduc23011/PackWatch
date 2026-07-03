using PackWatch.Application.Abstractions.History;
using PackWatch.Domain.Entities;
using System.Text.Json;

namespace PackWatch.Persistence.Services;

internal sealed class JsonHistoryService : IHistoryService, IOrderHistoryWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<IReadOnlyList<OrderRecord>> SearchAsync(
        OrderHistoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var records = await LoadSnapshotsAsync(cancellationToken);

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

        var snapshots = await LoadSnapshotsAsync(cancellationToken);
        snapshots.Add(PersistedOrderRecord.FromDomain(record));
        await SaveSnapshotsAsync(snapshots, cancellationToken);
    }

    public async Task DeleteAsync(string videoPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
        {
            return;
        }

        var snapshots = await LoadSnapshotsAsync(cancellationToken);
        var toRemove = snapshots.FirstOrDefault(s => s.VideoPath.Equals(videoPath, StringComparison.OrdinalIgnoreCase));
        if (toRemove is not null)
        {
            snapshots.Remove(toRemove);
            await SaveSnapshotsAsync(snapshots, cancellationToken);

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

    private static async Task<List<PersistedOrderRecord>> LoadSnapshotsAsync(CancellationToken cancellationToken)
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
