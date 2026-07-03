using PackWatch.Application.Abstractions.Settings;
using PackWatch.Application.Abstractions.Storage;

namespace PackWatch.Persistence.Services;

internal sealed class LocalStorageService : IStorageService
{
    private readonly ISettingsService _settingsService;

    public LocalStorageService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<string> CreateVideoPathAsync(
        string orderCode,
        DateOnly businessDate,
        string extension,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        var sessionFolder = BuildSessionFolder(settings.SaveFolder, businessDate);
        var safeOrderCode = Sanitize(orderCode);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? "capture" : extension.Trim().TrimStart('.');
        var fileName = $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}_{safeOrderCode}.{safeExtension}.session.json";
        return Path.Combine(sessionFolder, fileName);
    }

    public Task<string> CreateThumbnailPathAsync(
        string orderCode,
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var thumbnailFolder = Path.Combine(LocalPackWatchPaths.SessionDirectory, businessDate.ToString("yyyy-MM-dd"), "thumbnails");
        Directory.CreateDirectory(thumbnailFolder);
        var fileName = $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}_{Sanitize(orderCode)}.txt";
        return Task.FromResult(Path.Combine(thumbnailFolder, fileName));
    }

    private static string BuildSessionFolder(string saveFolder, DateOnly businessDate)
    {
        var root = string.IsNullOrWhiteSpace(saveFolder)
            ? LocalPackWatchPaths.SessionDirectory
            : saveFolder;

        var path = Path.Combine(root, businessDate.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Sanitize(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var cleanedCharacters = value.Trim()
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();

        return new string(cleanedCharacters);
    }
}
