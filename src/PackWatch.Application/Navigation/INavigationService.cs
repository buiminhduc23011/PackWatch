namespace PackWatch.Application.Navigation;

public interface INavigationService
{
    event EventHandler<NavigationChangedEventArgs>? Navigated;

    ApplicationPage CurrentPage { get; }

    void NavigateTo(ApplicationPage page);
}
