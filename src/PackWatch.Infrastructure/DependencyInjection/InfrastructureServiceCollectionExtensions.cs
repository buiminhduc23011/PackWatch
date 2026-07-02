using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PackWatch.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPackWatchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
