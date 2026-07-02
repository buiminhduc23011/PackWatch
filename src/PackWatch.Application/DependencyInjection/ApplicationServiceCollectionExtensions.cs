using Microsoft.Extensions.DependencyInjection;

namespace PackWatch.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddPackWatchApplication(this IServiceCollection services)
    {
        return services;
    }
}
