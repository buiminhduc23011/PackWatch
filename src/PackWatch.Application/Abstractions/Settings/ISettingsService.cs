namespace PackWatch.Application.Abstractions.Settings;

public interface ISettingsService
{
    Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken);
}
