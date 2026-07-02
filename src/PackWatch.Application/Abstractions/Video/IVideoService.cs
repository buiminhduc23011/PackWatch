namespace PackWatch.Application.Abstractions.Video;

public interface IVideoService
{
    Task OpenAsync(string videoPath, CancellationToken cancellationToken);

    Task<string> GenerateThumbnailAsync(
        string videoPath,
        string thumbnailPath,
        CancellationToken cancellationToken);
}
