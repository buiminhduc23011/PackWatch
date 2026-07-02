using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PackWatch.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPackWatchPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
