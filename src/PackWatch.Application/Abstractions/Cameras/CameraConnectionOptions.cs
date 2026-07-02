namespace PackWatch.Application.Abstractions.Cameras;

public sealed record CameraConnectionOptions(
    CameraSourceKind SourceKind,
    string? DeviceName,
    int? DeviceIndex,
    Uri? RtspUri,
    string? Username,
    string? Password);
