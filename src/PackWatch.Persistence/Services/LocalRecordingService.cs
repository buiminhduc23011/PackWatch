using PackWatch.Application.Abstractions.Recording;
using PackWatch.Application.Abstractions.Storage;
using PackWatch.Domain.Entities;
using System.Text.Json;

namespace PackWatch.Persistence.Services;

internal sealed class LocalRecordingService : IRecordingService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IStorageService _storageService;
    private readonly IOrderHistoryWriter _historyWriter;
    private readonly Dictionary<Guid, PendingSessionContext> _pendingSessions = [];
    private readonly object _syncRoot = new();

    public LocalRecordingService(IStorageService storageService, IOrderHistoryWriter historyWriter)
    {
        _storageService = storageService;
        _historyWriter = historyWriter;
    }

    public async Task<RecordingSession> StartAsync(
        string orderCode,
        RecordingOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderCode);
        ArgumentNullException.ThrowIfNull(options);

        var startedAt = DateTimeOffset.Now;
        var businessDate = DateOnly.FromDateTime(startedAt.LocalDateTime);
        var artifactPath = await _storageService.CreateVideoPathAsync(
            orderCode,
            businessDate,
            options.VideoFormat,
            cancellationToken);
        var thumbnailPath = await _storageService.CreateThumbnailPathAsync(orderCode, businessDate, cancellationToken);

        var session = new RecordingSession(
            Guid.NewGuid(),
            orderCode.Trim(),
            artifactPath,
            startedAt,
            EndedAt: null);

        lock (_syncRoot)
        {
            _pendingSessions[session.Id] = new PendingSessionContext(options, thumbnailPath);
        }

        return session;
    }

    public async Task<RecordingSession> StopAsync(
        RecordingSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var endedAt = DateTimeOffset.Now;
        PendingSessionContext? context = null;

        lock (_syncRoot)
        {
            if (!_pendingSessions.Remove(session.Id, out context))
            {
                throw new InvalidOperationException("The recording session was not started by the local recorder.");
            }
        }

        if (context is null)
        {
            throw new InvalidOperationException("The recording session context could not be loaded.");
        }

        var completedSession = session with { EndedAt = endedAt };
        await WriteThumbnailNoteAsync(context.ThumbnailPath, completedSession, context.Options, cancellationToken);

        var record = OrderRecord.Start(
            completedSession.OrderCode,
            completedSession.VideoPath,
            context.ThumbnailPath,
            completedSession.StartedAt,
            context.Options.CameraName);

        record.Complete(endedAt);
        await _historyWriter.AppendAsync(record, cancellationToken);

        return completedSession;
    }

    private static Task WriteThumbnailNoteAsync(
        string thumbnailPath,
        RecordingSession session,
        RecordingOptions options,
        CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(thumbnailPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var note = string.Join(
            Environment.NewLine,
            [
                $"Order: {session.OrderCode}",
                $"Camera: {options.CameraName}",
                $"Started: {session.StartedAt:yyyy-MM-dd HH:mm:ss}",
                $"Ended: {session.EndedAt:yyyy-MM-dd HH:mm:ss}",
                $"Target format: {options.VideoFormat}"
            ]);

        return File.WriteAllTextAsync(thumbnailPath, note, cancellationToken);
    }

    private sealed record PendingSessionContext(RecordingOptions Options, string ThumbnailPath);

    private sealed record SessionArtifact(
        Guid SessionId,
        string OrderCode,
        string CameraName,
        string Resolution,
        int FramesPerSecond,
        int BitrateKbps,
        string TargetFormat,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt);
}
