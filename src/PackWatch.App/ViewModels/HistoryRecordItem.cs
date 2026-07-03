namespace PackWatch.App.ViewModels;

public sealed class HistoryRecordItem
{
    public required string OrderCode { get; init; }

    public required string BadgeText { get; init; }

    public required string CameraName { get; init; }

    public required string StartedAtText { get; init; }

    public required string EndedAtText { get; init; }

    public required string DurationText { get; init; }

    public required string ArtifactName { get; init; }

    public required string ArtifactPath { get; init; }
}
