namespace PackWatch.Application.Abstractions.Storage;

public interface IStorageService
{
    Task<string> CreateVideoPathAsync(
        string orderCode,
        DateOnly businessDate,
        string extension,
        CancellationToken cancellationToken);

    Task<string> CreateThumbnailPathAsync(
        string orderCode,
        DateOnly businessDate,
        CancellationToken cancellationToken);
}
