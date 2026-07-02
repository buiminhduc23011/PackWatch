namespace PackWatch.Application.Abstractions.Recording;

public sealed record RecordingOptions(
    string CameraName,
    int FramesPerSecond,
    string Resolution,
    int BitrateKbps,
    string VideoFormat);
