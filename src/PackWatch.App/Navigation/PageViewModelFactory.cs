using CommunityToolkit.Mvvm.ComponentModel;
using PackWatch.App.ViewModels;
using PackWatch.Application.Navigation;

namespace PackWatch.App.Navigation;

public sealed class PageViewModelFactory
{
    private readonly HomePageViewModel _homePageViewModel;
    private readonly HistoryPageViewModel _historyPageViewModel;
    private readonly SettingsPageViewModel _settingsPageViewModel;

    public PageViewModelFactory(
        HomePageViewModel homePageViewModel,
        HistoryPageViewModel historyPageViewModel,
        SettingsPageViewModel settingsPageViewModel)
    {
        _homePageViewModel = homePageViewModel;
        _historyPageViewModel = historyPageViewModel;
        _settingsPageViewModel = settingsPageViewModel;
    }

    public ObservableObject Create(ApplicationPage page)
    {
        return page switch
        {
            ApplicationPage.Home => _homePageViewModel,
            ApplicationPage.History => _historyPageViewModel,
            ApplicationPage.Settings => _settingsPageViewModel,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, "Unknown application page.")
        };
    }
}
