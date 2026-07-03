using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackWatch.App.Navigation;
using PackWatch.App.Services;
using PackWatch.Application.Navigation;

namespace PackWatch.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly PageViewModelFactory _pageViewModelFactory;
    private readonly IAppStatusService _appStatusService;

    [ObservableProperty]
    private ApplicationPage selectedPage;

    [ObservableProperty]
    private ObservableObject currentPageViewModel;

    [ObservableProperty]
    private string statusMessage = "Ready";

    public MainWindowViewModel(
        INavigationService navigationService,
        PageViewModelFactory pageViewModelFactory,
        IAppStatusService appStatusService)
    {
        _navigationService = navigationService;
        _pageViewModelFactory = pageViewModelFactory;
        _appStatusService = appStatusService;

        _navigationService.Navigated += HandleNavigated;
        _appStatusService.StatusChanged += HandleStatusChanged;

        selectedPage = _navigationService.CurrentPage;
        currentPageViewModel = _pageViewModelFactory.Create(selectedPage);
        statusMessage = _appStatusService.CurrentStatus;

        ActivateCurrentPage(selectedPage);
    }

    [RelayCommand]
    private void Navigate(ApplicationPage page)
    {
        _navigationService.NavigateTo(page);
    }

    private void HandleNavigated(object? sender, NavigationChangedEventArgs e)
    {
        ActivateCurrentPage(e.CurrentPage);
    }

    private void HandleStatusChanged(object? sender, string status)
    {
        StatusMessage = status;
    }

    private void ActivateCurrentPage(ApplicationPage page)
    {
        SelectedPage = page;
        CurrentPageViewModel = _pageViewModelFactory.Create(page);

        if (CurrentPageViewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo();
        }
    }
}
