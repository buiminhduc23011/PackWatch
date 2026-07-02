namespace PackWatch.Application.Navigation;

public sealed class NavigationChangedEventArgs : EventArgs
{
    public NavigationChangedEventArgs(ApplicationPage previousPage, ApplicationPage currentPage)
    {
        PreviousPage = previousPage;
        CurrentPage = currentPage;
    }

    public ApplicationPage PreviousPage { get; }

    public ApplicationPage CurrentPage { get; }
}
