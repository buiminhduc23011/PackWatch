using PackWatch.Application.Abstractions.Media;

namespace PackWatch.Application.Abstractions.Cameras;

public interface ICameraService
{
    event EventHandler<CameraFrameAvailableEventArgs>? FrameAvailable;

    bool IsOpen { get; }

    Task OpenAsync(CameraConnectionOptions options, CancellationToken cancellationToken);

    Task CloseAsync(CancellationToken cancellationToken);

    Task ReconnectAsync(CancellationToken cancellationToken);

    Task<VideoFrame?> SnapshotAsync(CancellationToken cancellationToken);

    Task StartRecordingAsync(string filePath, CancellationToken cancellationToken);

    Task StopRecordingAsync(CancellationToken cancellationToken);
}
