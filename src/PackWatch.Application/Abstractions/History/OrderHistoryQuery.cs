namespace PackWatch.Application.Abstractions.History;

public sealed record OrderHistoryQuery(
    string? OrderCode,
    DateOnly? FromDate,
    DateOnly? ToDate);
