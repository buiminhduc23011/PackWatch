using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackWatch.App.Navigation;
using PackWatch.App.Services;
using PackWatch.Application.Abstractions.History;
using PackWatch.Application.Abstractions.Settings;
using PackWatch.Domain.Entities;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace PackWatch.App.ViewModels;

public sealed partial class HistoryPageViewModel : ObservableObject, INavigationAware
{
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;
    private readonly IDesktopShellService _desktopShellService;
    private readonly IAppStatusService _appStatusService;

    public ObservableCollection<HistoryRecordItem> Records { get; } = [];

    [ObservableProperty]
    private string searchOrderCode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecords))]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptyStateVisibility))]
    [NotifyPropertyChangedFor(nameof(ResultsSummary))]
    private int resultCount;

    [ObservableProperty]
    private string emptyStateMessage = "No session artifacts yet. Start a validation run on Home to create the first record.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageSummary))]
    private int currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyPropertyChangedFor(nameof(PageSummary))]
    private int totalPages = 1;

    [ObservableProperty]
    private string jumpPageInput = "1";

    private const int PageSize = 10;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PageSummary => $"Page {CurrentPage} of {TotalPages}";

    public HistoryPageViewModel(
        IHistoryService historyService,
        ISettingsService settingsService,
        IDesktopShellService desktopShellService,
        IAppStatusService appStatusService)
    {
        _historyService = historyService;
        _settingsService = settingsService;
        _desktopShellService = desktopShellService;
        _appStatusService = appStatusService;
    }

    public bool HasRecords => ResultCount > 0;

    public Visibility ResultsVisibility => HasRecords ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyStateVisibility => HasRecords ? Visibility.Collapsed : Visibility.Visible;

    public string ResultsSummary => ResultCount == 0
        ? "No local session artifacts yet"
        : $"{ResultCount} session artifact{(ResultCount == 1 ? string.Empty : "s")} found";

    public void OnNavigatedTo()
    {
        _ = RefreshAsync(announceStatus: false);
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        CurrentPage = 1;
        await RefreshAsync(announceStatus: false, cancellationToken);
    }

    [RelayCommand]
    private async Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await RefreshAsync(announceStatus: false, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task NextPageAsync(CancellationToken cancellationToken)
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await RefreshAsync(announceStatus: false, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task JumpToPageAsync(CancellationToken cancellationToken)
    {
        if (int.TryParse(JumpPageInput, out int page) && page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await RefreshAsync(announceStatus: false, cancellationToken);
        }
        else
        {
            JumpPageInput = CurrentPage.ToString();
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);

        if (_desktopShellService.OpenFolder(settings.SaveFolder))
        {
            _appStatusService.SetStatus($"Opened the local session folder: {settings.SaveFolder}");
            return;
        }

        _appStatusService.SetStatus("Could not open the local session folder.");
    }

    [RelayCommand]
    private void OpenArtifact(HistoryRecordItem? item)
    {
        if (item is null)
        {
            _appStatusService.SetStatus("Choose a history row before opening its artifact.");
            return;
        }

        if (_desktopShellService.OpenFile(item.ArtifactPath))
        {
            _appStatusService.SetStatus($"Opened artifact file {item.ArtifactName}.");
            return;
        }

        _appStatusService.SetStatus("Could not open the selected artifact.");
    }

    [RelayCommand]
    private void RevealArtifact(HistoryRecordItem? item)
    {
        if (item is null)
        {
            _appStatusService.SetStatus("Choose a history row before revealing its artifact.");
            return;
        }

        if (_desktopShellService.RevealPath(item.ArtifactPath))
        {
            _appStatusService.SetStatus($"Revealed artifact file {item.ArtifactName} in explorer.");
            return;
        }

        _appStatusService.SetStatus("Could not reveal the selected artifact.");
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(HistoryRecordItem? item, CancellationToken cancellationToken)
    {
        if (item is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete the validation history for order {item.OrderCode}?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _historyService.DeleteAsync(item.ArtifactPath, cancellationToken);
            _appStatusService.SetStatus($"Deleted validation record for order {item.OrderCode}.");
            await RefreshAsync(announceStatus: false, cancellationToken);
        }
    }

    private async Task RefreshAsync(bool announceStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildQuery();
            var records = await _historyService.SearchAsync(query, cancellationToken);

            ResultCount = records.Count;
            TotalPages = (int)Math.Ceiling((double)ResultCount / PageSize);
            if (TotalPages == 0) TotalPages = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;
            JumpPageInput = CurrentPage.ToString();

            var pagedRecords = records
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(MapToViewModel);

            Records.Clear();

            foreach (var record in pagedRecords)
            {
                Records.Add(record);
            }

            EmptyStateMessage = HasRecords
                ? string.Empty
                : "No matching validation sessions were found.";

            if (announceStatus)
            {
                _appStatusService.SetStatus(ResultCount == 0
                    ? "History is empty."
                    : $"Loaded {ResultCount} session artifacts from the local history.");
            }
        }
        catch (Exception exception)
        {
            Records.Clear();
            ResultCount = 0;
            EmptyStateMessage = "History could not be loaded from local storage.";
            _appStatusService.SetStatus($"Failed to load history: {exception.Message}");
        }
    }

    private OrderHistoryQuery BuildQuery()
    {
        return new OrderHistoryQuery(SearchOrderCode, null, null);
    }

    private static HistoryRecordItem MapToViewModel(OrderRecord record)
    {
        return new HistoryRecordItem
        {
            OrderCode = record.OrderCode,
            BadgeText = BuildBadge(record.OrderCode),
            CameraName = record.CameraName,
            StartedAtText = record.StartTime.LocalDateTime.ToString("dd MMM yyyy HH:mm"),
            EndedAtText = record.EndTime?.LocalDateTime.ToString("dd MMM yyyy HH:mm") ?? "-",
            DurationText = record.Duration?.ToString(@"hh\:mm\:ss") ?? "-",
            ArtifactName = Path.GetFileName(record.VideoPath),
            ArtifactPath = record.VideoPath
        };
    }

    private static string BuildBadge(string orderCode)
    {
        var trimmed = orderCode.Trim().ToUpperInvariant();

        if (trimmed.Length <= 4)
        {
            return trimmed;
        }

        return trimmed[^4..];
    }
}
