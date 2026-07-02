using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackWatch.App.Navigation;
using PackWatch.Application.Navigation;

namespace PackWatch.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly PageViewModelFactory _pageViewModelFactory;

    [ObservableProperty]
    private ApplicationPage selectedPage;

    [ObservableProperty]
    private ObservableObject currentPageViewModel;

    [ObservableProperty]
    private string statusMessage = "Ready";

    public MainWindowViewModel(
        INavigationService navigationService,
        PageViewModelFactory pageViewModelFactory)
    {
        _navigationService = navigationService;
        _pageViewModelFactory = pageViewModelFactory;
        _navigationService.Navigated += HandleNavigated;

        selectedPage = _navigationService.CurrentPage;
        currentPageViewModel = _pageViewModelFactory.Create(selectedPage);
    }

    [RelayCommand]
    private void Navigate(ApplicationPage page)
    {
        _navigationService.NavigateTo(page);
    }

    private void HandleNavigated(object? sender, NavigationChangedEventArgs e)
    {
        SelectedPage = e.CurrentPage;
        CurrentPageViewModel = _pageViewModelFactory.Create(e.CurrentPage);
        StatusMessage = $"Viewing {e.CurrentPage}";
    }
}
