namespace PackWatch.Application.Abstractions.Recording;

public interface IRecordingService
{
    Task<RecordingSession> StartAsync(
        string orderCode,
        RecordingOptions options,
        CancellationToken cancellationToken);

    Task<RecordingSession> StopAsync(
        RecordingSession session,
        CancellationToken cancellationToken);
}
