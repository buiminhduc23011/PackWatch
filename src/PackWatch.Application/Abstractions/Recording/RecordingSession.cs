namespace PackWatch.Application.Abstractions.Recording;

public sealed record RecordingSession(
    Guid Id,
    string OrderCode,
    string VideoPath,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);
