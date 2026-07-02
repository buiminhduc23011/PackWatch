using PackWatch.Domain.Entities;

namespace PackWatch.Tests.Domain;

public sealed class OrderRecordTests
{
    [Fact]
    public void Start_creates_active_order_record()
    {
        var startTime = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

        var record = OrderRecord.Start(
            " SPXVN001 ",
            "Videos/2026-07-02/SPXVN001.mp4",
            "Thumbnails/2026-07-02/SPXVN001.jpg",
            startTime,
            "Packing Camera 1");

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal("SPXVN001", record.OrderCode);
        Assert.Equal(startTime, record.StartTime);
        Assert.Null(record.EndTime);
        Assert.Null(record.Duration);
    }

    [Fact]
    public void Complete_sets_end_time_and_duration()
    {
        var startTime = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
        var record = OrderRecord.Start(
            "SPXVN001",
            "Videos/2026-07-02/SPXVN001.mp4",
            "Thumbnails/2026-07-02/SPXVN001.jpg",
            startTime,
            "Packing Camera 1");

        record.Complete(startTime.AddMinutes(2));

        Assert.Equal(startTime.AddMinutes(2), record.EndTime);
        Assert.Equal(TimeSpan.FromMinutes(2), record.Duration);
    }

    [Fact]
    public void Complete_rejects_end_time_before_start_time()
    {
        var startTime = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
        var record = OrderRecord.Start(
            "SPXVN001",
            "Videos/2026-07-02/SPXVN001.mp4",
            "Thumbnails/2026-07-02/SPXVN001.jpg",
            startTime,
            "Packing Camera 1");

        Assert.Throws<ArgumentOutOfRangeException>(() => record.Complete(startTime.AddSeconds(-1)));
    }
}
