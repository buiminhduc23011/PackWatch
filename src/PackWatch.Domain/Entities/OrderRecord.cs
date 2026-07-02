namespace PackWatch.Domain.Entities;

public sealed class OrderRecord
{
    private OrderRecord()
    {
        OrderCode = string.Empty;
        VideoPath = string.Empty;
        ThumbnailPath = string.Empty;
        CameraName = string.Empty;
    }

    private OrderRecord(
        string orderCode,
        string videoPath,
        string thumbnailPath,
        DateTimeOffset startTime,
        string cameraName)
    {
        Id = Guid.NewGuid();
        OrderCode = RequireValue(orderCode, nameof(orderCode));
        VideoPath = RequireValue(videoPath, nameof(videoPath));
        ThumbnailPath = RequireValue(thumbnailPath, nameof(thumbnailPath));
        StartTime = startTime;
        CameraName = RequireValue(cameraName, nameof(cameraName));
        CreatedTime = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string OrderCode { get; private set; }

    public string VideoPath { get; private set; }

    public string ThumbnailPath { get; private set; }

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset? EndTime { get; private set; }

    public TimeSpan? Duration { get; private set; }

    public string CameraName { get; private set; }

    public DateTimeOffset CreatedTime { get; private set; }

    public static OrderRecord Start(
        string orderCode,
        string videoPath,
        string thumbnailPath,
        DateTimeOffset startTime,
        string cameraName)
    {
        return new OrderRecord(orderCode, videoPath, thumbnailPath, startTime, cameraName);
    }

    public void Complete(DateTimeOffset endTime)
    {
        if (endTime < StartTime)
        {
            throw new ArgumentOutOfRangeException(nameof(endTime), "End time cannot be earlier than start time.");
        }

        EndTime = endTime;
        Duration = EndTime.Value - StartTime;
    }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
