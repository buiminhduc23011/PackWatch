using Microsoft.Extensions.DependencyInjection;
using PackWatch.App.Navigation;
using PackWatch.App.ViewModels;
using PackWatch.App.Views;
using PackWatch.Application.Navigation;

namespace PackWatch.App.DependencyInjection;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPackWatchPresentation(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<HomePageViewModel>();
        services.AddSingleton<HistoryPageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();

        services.AddTransient<HomePage>();
        services.AddTransient<HistoryPage>();
        services.AddTransient<SettingsPage>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<PageViewModelFactory>();

        return services;
    }
}
