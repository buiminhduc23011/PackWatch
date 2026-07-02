using CommunityToolkit.Mvvm.ComponentModel;

namespace PackWatch.App.ViewModels;

public sealed partial class HistoryPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedDateFilter = "Today";

    [ObservableProperty]
    private string searchOrderCode = string.Empty;

    [ObservableProperty]
    private string emptyStateMessage = "Recorded orders will appear here after the persistence phase.";
}
