using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PackWatch.Application.Abstractions.Cameras;
using PackWatch.Infrastructure.Services;

namespace PackWatch.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPackWatchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ICameraService, WindowsCameraService>();

        return services;
    }
}
