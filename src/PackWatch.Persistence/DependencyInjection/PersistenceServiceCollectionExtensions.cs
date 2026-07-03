using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PackWatch.Application.Abstractions.History;
using PackWatch.Application.Abstractions.Recording;
using PackWatch.Application.Abstractions.Settings;
using PackWatch.Application.Abstractions.Storage;
using PackWatch.Persistence.Services;

namespace PackWatch.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPackWatchPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<JsonSettingsService>();
        services.AddSingleton<JsonHistoryService>();
        services.AddSingleton<LocalStorageService>();
        services.AddSingleton<LocalRecordingService>();

        services.AddSingleton<ISettingsService>(provider => provider.GetRequiredService<JsonSettingsService>());
        services.AddSingleton<IHistoryService>(provider => provider.GetRequiredService<JsonHistoryService>());
        services.AddSingleton<IOrderHistoryWriter>(provider => provider.GetRequiredService<JsonHistoryService>());
        services.AddSingleton<IStorageService>(provider => provider.GetRequiredService<LocalStorageService>());
        services.AddSingleton<IRecordingService>(provider => provider.GetRequiredService<LocalRecordingService>());

        return services;
    }
}
