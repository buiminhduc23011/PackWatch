using PackWatch.Application.Navigation;

namespace PackWatch.App.Navigation;

public sealed class NavigationService : INavigationService
{
    public event EventHandler<NavigationChangedEventArgs>? Navigated;

    public ApplicationPage CurrentPage { get; private set; } = ApplicationPage.Home;

    public void NavigateTo(ApplicationPage page)
    {
        if (CurrentPage == page)
        {
            return;
        }

        var previousPage = CurrentPage;
        CurrentPage = page;
        Navigated?.Invoke(this, new NavigationChangedEventArgs(previousPage, page));
    }
}
