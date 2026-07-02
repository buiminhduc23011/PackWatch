using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PackWatch.App.DependencyInjection;
using PackWatch.Application.DependencyInjection;
using PackWatch.Infrastructure.DependencyInjection;
using PackWatch.Persistence.DependencyInjection;
using Serilog;
using System.Windows;

namespace PackWatch.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            })
            .ConfigureServices((context, services) =>
            {
                services
                    .AddPackWatchApplication()
                    .AddPackWatchInfrastructure(context.Configuration)
                    .AddPackWatchPersistence(context.Configuration)
                    .AddPackWatchPresentation();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
