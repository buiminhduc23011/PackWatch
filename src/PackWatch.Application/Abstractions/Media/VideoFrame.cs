namespace PackWatch.Application.Abstractions.Media;

public sealed record VideoFrame(
    byte[] Buffer,
    int Width,
    int Height,
    DateTimeOffset CapturedAt);
